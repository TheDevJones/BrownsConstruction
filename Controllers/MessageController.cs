using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BCSApp.Data;
using BCSApp.Models;
using Newtonsoft.Json;
using Task = System.Threading.Tasks.Task;

namespace BCSApp.Controllers
{
    [Authorize]
    public class MessageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessageController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Message/Inbox
        public async Task<IActionResult> Inbox()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Recipient)
                .Include(m => m.Project)
                .Include(m => m.MaintenanceRequest)
                .Include(m => m.Task)
                .Where(m => m.RecipientId == user.Id)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            ViewBag.UnreadCount = messages.Count(m => !m.IsRead);
            return View(messages);
        }

        // GET: Message/Sent
        public async Task<IActionResult> Sent()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Recipient)
                .Include(m => m.Project)
                .Include(m => m.MaintenanceRequest)
                .Include(m => m.Task)
                .Where(m => m.SenderId == user.Id)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return View(messages);
        }

        // GET: Message/Compose
        public async Task<IActionResult> Compose(string recipientId = null, int? projectId = null, int? maintenanceRequestId = null, int? taskId = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Get all users except current user
            var users = await _context.Users
                .Where(u => u.Id != user.Id && u.IsActive)
                .OrderBy(u => u.FirstName)
                .ToListAsync();

            // Get projects accessible to user
            var projects = await _context.Projects
                .Where(p => user.Role == "Admin" ||
                           p.ProjectManagerId == user.Id ||
                           p.ClientId == user.Id ||
                           p.ProjectContractors.Any(pc => pc.ContractorId == user.Id))
                .ToListAsync();

            // Get maintenance requests accessible to user
            var maintenanceRequests = await _context.MaintenanceRequests
                .Where(m => user.Role == "Admin" ||
                           user.Role == "ProjectManager" ||
                           m.ClientId == user.Id ||
                           m.AssignedToId == user.Id)
                .ToListAsync();

            // Get tasks accessible to user
            var tasks = await _context.Tasks
                .Where(t => user.Role == "Admin" ||
                           user.Role == "ProjectManager" ||
                           t.AssignedToId == user.Id ||
                           t.CreatedById == user.Id)
                .ToListAsync();

            ViewBag.Users = users;
            ViewBag.Projects = projects;
            ViewBag.MaintenanceRequests = maintenanceRequests;
            ViewBag.Tasks = tasks;
            ViewBag.PreSelectedRecipientId = recipientId;
            ViewBag.PreSelectedProjectId = projectId;
            ViewBag.PreSelectedMaintenanceRequestId = maintenanceRequestId;
            ViewBag.PreSelectedTaskId = taskId;

            return View();
        }

        // POST: Message/Compose
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Compose([Bind("Subject,Content,RecipientId,ProjectId,MaintenanceRequestId,TaskId,MessageType")] Message message, List<IFormFile> attachments)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (ModelState.IsValid)
            {
                message.SenderId = user.Id;
                message.CreatedAt = DateTime.Now;
                message.IsRead = false;

                _context.Add(message);
                await _context.SaveChangesAsync();

                // Handle attachments
                if (attachments != null && attachments.Any())
                {
                    foreach (var file in attachments)
                    {
                        if (file.Length > 0)
                        {
                            await SaveAttachment(file, message.Id);
                        }
                    }
                }

                // Log the action
                JsonConvert.SerializeObject(new
                {
                    message.Id,
                    message.Subject,
                    message.Content,
                    message.RecipientId,
                    message.ProjectId,
                    message.MaintenanceRequestId,
                    message.TaskId,
                    message.MessageType,
                    message.SenderId,
                    message.CreatedAt
                });

                // Create notification for recipient
                await CreateNotification(message);

                TempData["Success"] = "Message sent successfully.";
                return RedirectToAction(nameof(Sent));
            }

            // If we got this far, something failed, redisplay form
            var users = await _context.Users
                .Where(u => u.Id != user.Id && u.IsActive)
                .OrderBy(u => u.FirstName)
                .ToListAsync();

            var projects = await _context.Projects
                .Where(p => user.Role == "Admin" ||
                           p.ProjectManagerId == user.Id ||
                           p.ClientId == user.Id ||
                           p.ProjectContractors.Any(pc => pc.ContractorId == user.Id))
                .ToListAsync();

            var maintenanceRequests = await _context.MaintenanceRequests
                .Where(m => user.Role == "Admin" ||
                           user.Role == "ProjectManager" ||
                           m.ClientId == user.Id ||
                           m.AssignedToId == user.Id)
                .ToListAsync();

            var tasks = await _context.Tasks
                .Where(t => user.Role == "Admin" ||
                           user.Role == "ProjectManager" ||
                           t.AssignedToId == user.Id ||
                           t.CreatedById == user.Id)
                .ToListAsync();

            ViewBag.Users = users;
            ViewBag.Projects = projects;
            ViewBag.MaintenanceRequests = maintenanceRequests;
            ViewBag.Tasks = tasks;

            return View(message);
        }

        // GET: Message/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var message = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Recipient)
                .Include(m => m.Project)
                .Include(m => m.MaintenanceRequest)
                .Include(m => m.Task)
                .Include(m => m.Attachments)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (message == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (!CanAccessMessage(message, user)) return Forbid();

            // Mark as read if recipient is viewing
            if (message.RecipientId == user.Id && !message.IsRead)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.Now;
                _context.Update(message);
                await _context.SaveChangesAsync();

                // Log the action
                await LogAuditAction("Message", message.Id, "Read", user.Id, null, null);
            }

            return View(message);
        }

        // POST: Message/Reply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string content)
        {
            var originalMessage = await _context.Messages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (originalMessage == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (!CanAccessMessage(originalMessage, user)) return Forbid();

            var replyMessage = new Message
            {
                SenderId = user.Id,
                RecipientId = originalMessage.SenderId == user.Id ? originalMessage.RecipientId : originalMessage.SenderId,
                Subject = originalMessage.Subject.StartsWith("Re: ") ? originalMessage.Subject : "Re: " + originalMessage.Subject,
                Content = content,
                MessageType = originalMessage.MessageType,
                ProjectId = originalMessage.ProjectId,
                MaintenanceRequestId = originalMessage.MaintenanceRequestId,
                TaskId = originalMessage.TaskId,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Add(replyMessage);
            await _context.SaveChangesAsync();

            // Log the action
            await LogAuditAction("Message", replyMessage.Id, "Reply", user.Id, null,
                JsonConvert.SerializeObject(new
                {
                    replyMessage.Id,
                    replyMessage.Subject,
                    replyMessage.Content,
                    replyMessage.RecipientId,
                    replyMessage.ProjectId,
                    replyMessage.MaintenanceRequestId,
                    replyMessage.TaskId,
                    replyMessage.MessageType,
                    replyMessage.SenderId,
                    replyMessage.CreatedAt
                }));

            // Create notification for recipient
            await CreateNotification(replyMessage);

            TempData["Success"] = "Reply sent successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Message/Conversations
        public async Task<IActionResult> Conversations()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Get unique conversation partners
            var sentTo = await _context.Messages
                .Where(m => m.SenderId == user.Id)
                .Select(m => m.RecipientId)
                .Distinct()
                .ToListAsync();

            var receivedFrom = await _context.Messages
                .Where(m => m.RecipientId == user.Id)
                .Select(m => m.SenderId)
                .Distinct()
                .ToListAsync();

            var conversationPartnerIds = sentTo.Union(receivedFrom).Distinct().ToList();

            var conversations = new List<ConversationViewModel>();

            foreach (var partnerId in conversationPartnerIds)
            {
                var partner = await _context.Users.FindAsync(partnerId);
                if (partner == null) continue;

                var lastMessage = await _context.Messages
                    .Where(m => (m.SenderId == user.Id && m.RecipientId == partnerId) ||
                               (m.SenderId == partnerId && m.RecipientId == user.Id))
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefaultAsync();

                var unreadCount = await _context.Messages
                    .Where(m => m.SenderId == partnerId && m.RecipientId == user.Id && !m.IsRead)
                    .CountAsync();

                conversations.Add(new ConversationViewModel
                {
                    Partner = partner,
                    LastMessage = lastMessage,
                    UnreadCount = unreadCount
                });
            }

            return View(conversations.OrderByDescending(c => c.LastMessage?.CreatedAt).ToList());
        }

        // GET: Message/Conversation/userId
        // GET: Message/Conversation/id
        [HttpGet("Message/Conversation/{userId}")]
        public async Task<IActionResult> Conversation(string userId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var partner = await _context.Users.FindAsync(userId);
            if (partner == null) return NotFound();

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Recipient)
                .Where(m => (m.SenderId == user.Id && m.RecipientId == userId) ||
                            (m.SenderId == userId && m.RecipientId == user.Id))
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            ViewBag.Partner = partner;
            return View(messages);
        }



        // POST: Message/SendQuick
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendQuick(string recipientId, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var message = new Message
            {
                SenderId = user.Id,
                RecipientId = recipientId,
                Subject = "Quick Message",
                Content = content,
                MessageType = "General",
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Add(message);
            await _context.SaveChangesAsync();

            // Create notification for recipient
            await CreateNotification(message);

            return RedirectToAction(nameof(Conversation), new { userId = recipientId });
        }

        // DELETE: Message/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (!CanAccessMessage(message, user)) return Forbid();

            // Log the action before deletion
            await LogAuditAction("Message", message.Id, "Delete", user.Id,
                JsonConvert.SerializeObject(new
                {
                    message.Id,
                    message.Subject,
                    message.Content,
                    message.RecipientId,
                    message.ProjectId,
                    message.MaintenanceRequestId,
                    message.TaskId,
                    message.MessageType,
                    message.SenderId,
                    message.CreatedAt
                }),
                null);

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Message deleted successfully.";
            return RedirectToAction(nameof(Inbox));
        }

        // GET: Message/GetUnreadCount - API endpoint for unread count
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(0);

            var count = await _context.Messages
                .Where(m => m.RecipientId == user.Id && !m.IsRead)
                .CountAsync();

            return Json(count);
        }

        private bool CanAccessMessage(Message message, ApplicationUser user)
        {
            return message.SenderId == user.Id || message.RecipientId == user.Id || user.Role == "Admin";
        }

        private async Task SaveAttachment(IFormFile file, int messageId)
        {
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "messages");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var attachment = new MessageAttachment
            {
                MessageId = messageId,
                FileName = file.FileName,
                FilePath = $"/uploads/messages/{fileName}",
                FileType = Path.GetExtension(file.FileName).ToLower(),
                FileSize = file.Length
            };

            _context.MessageAttachments.Add(attachment);
            await _context.SaveChangesAsync();
        }

        private async System.Threading.Tasks.Task CreateNotification(Message message)
        {
            var notification = new Notification
            {
                Title = "New Message",
                Message = $"You have received a new message from {message.Sender.FirstName} {message.Sender.LastName}: {message.Subject}",
                Type = "InApp",
                RecipientId = message.RecipientId,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        private async System.Threading.Tasks.Task LogAuditAction(string entityType, int entityId, string action, string userId, string? oldValues, string? newValues)
        {
            var auditLog = new AuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                UserId = userId,
                OldValues = oldValues,
                NewValues = newValues,
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.Now
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }
}