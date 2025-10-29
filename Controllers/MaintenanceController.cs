using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BCSApp.Data;
using BCSApp.Models;
using Newtonsoft.Json;

namespace BCSApp.Controllers
{
    [Authorize]
    public class MaintenanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MaintenanceController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Maintenance
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var requests = await _context.MaintenanceRequests
                .Include(m => m.Client)
                .Include(m => m.AssignedTo)
                .Include(m => m.Project)
                .Include(m => m.Attachments)
                .Where(m => user.Role == "Admin" || 
                           user.Role == "ProjectManager" ||
                           m.ClientId == user.Id ||
                           m.AssignedToId == user.Id)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        // GET: Maintenance/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.MaintenanceRequests
                .Include(m => m.Client)
                .Include(m => m.AssignedTo)
                .Include(m => m.Project)
                .Include(m => m.Attachments)
                .Include(m => m.Tasks)
                .Include(m => m.Updates)
                    .ThenInclude(u => u.UpdatedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (request == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (!CanAccessMaintenanceRequest(request, user)) return Forbid();

            return View(request);
        }

        // GET: Maintenance/Create
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            var projects = await _context.Projects
                .Where(p => p.ClientId == user.Id)
                .ToListAsync();

            ViewBag.Projects = projects;
            return View();
        }

        // POST: Maintenance/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Create([Bind("Title,Description,Location,PropertyType,ProjectId,Priority,DueDate")] MaintenanceRequest request)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                request.ClientId = user.Id;
                request.CreatedAt = DateTime.Now;
                request.UpdatedAt = DateTime.Now;
                request.Status = "Pending";

                _context.Add(request);
                await _context.SaveChangesAsync();

                // Log the action
                await LogAuditAction("MaintenanceRequest", request.Id, "Create", user.Id, null, JsonConvert.SerializeObject(request));

                // Send notification to project managers
                await SendNotificationToProjectManagers(request);

                return RedirectToAction(nameof(Index));
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var projects = await _context.Projects
                .Where(p => p.ClientId == currentUser.Id)
                .ToListAsync();

            ViewBag.Projects = projects;
            return View(request);
        }

        // GET: Maintenance/Edit/5
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.MaintenanceRequests.FindAsync(id);
            if (request == null) return NotFound();

            var contractors = await _userManager.GetUsersInRoleAsync("Contractor");
            ViewBag.Contractors = contractors;

            return View(request);
        }

        // POST: Maintenance/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Location,PropertyType,Status,Priority,AssignedToId,EstimatedCost,DueDate")] MaintenanceRequest request)
        {
            if (id != request.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.GetUserAsync(User);
                    var oldRequest = await _context.MaintenanceRequests.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
                    
                    request.UpdatedAt = DateTime.Now;
                    _context.Update(request);
                    await _context.SaveChangesAsync();

                    // Log the action
                    await LogAuditAction("MaintenanceRequest", request.Id, "Update", user.Id, 
                        JsonConvert.SerializeObject(oldRequest), JsonConvert.SerializeObject(request));

                    // Send notification to assigned contractor
                    if (request.AssignedToId != null)
                    {
                        await SendNotificationToContractor(request);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MaintenanceRequestExists(request.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            var contractors = await _userManager.GetUsersInRoleAsync("Contractor");
            ViewBag.Contractors = contractors;

            return View(request);
        }

        // POST: Maintenance/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string description)
        {
            var request = await _context.MaintenanceRequests.FindAsync(id);
            if (request == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (!CanUpdateMaintenanceRequest(request, user)) return Forbid();

            var oldStatus = request.Status;
            request.Status = status;
            request.UpdatedAt = DateTime.Now;

            if (status == "Completed")
            {
                request.CompletedAt = DateTime.Now;
            }

            _context.Update(request);

            // Add update record
            var update = new MaintenanceUpdate
            {
                MaintenanceRequestId = id,
                UpdatedById = user.Id,
                StatusChange = $"{oldStatus} → {status}",
                Description = description,
                CreatedAt = DateTime.Now
            };

            _context.Add(update);
            await _context.SaveChangesAsync();

            // Log the action
            await LogAuditAction("MaintenanceRequest", request.Id, "StatusUpdate", user.Id, 
                oldStatus, status);

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Maintenance/UploadAttachment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAttachment(int maintenanceRequestId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction(nameof(Details), new { id = maintenanceRequestId });
            }

            var request = await _context.MaintenanceRequests.FindAsync(maintenanceRequestId);
            if (request == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (!CanAccessMaintenanceRequest(request, user)) return Forbid();

            try
            {
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "maintenance");
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

                var document = new Document
                {
                    Name = file.FileName,
                    FileName = fileName,
                    FilePath = $"/uploads/maintenance/{fileName}",
                    FileType = Path.GetExtension(file.FileName).ToLower(),
                    FileSize = file.Length,
                    DocumentType = "MaintenanceAttachment",
                    MaintenanceRequestId = maintenanceRequestId,
                    UploadedById = user.Id,
                    CreatedAt = DateTime.Now
                };

                _context.Add(document);
                await _context.SaveChangesAsync();

                // Log the action
                await LogAuditAction("Document", document.Id, "Upload", user.Id, null, JsonConvert.SerializeObject(document));

                TempData["Success"] = "File uploaded successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error uploading file: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id = maintenanceRequestId });
        }

        // GET: Maintenance/ClientRequests
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> ClientRequests()
        {
            var user = await _userManager.GetUserAsync(User);
            var requests = await _context.MaintenanceRequests
                .Include(m => m.AssignedTo)
                .Include(m => m.Project)
                .Include(m => m.Attachments)
                .Where(m => m.ClientId == user.Id)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        private bool MaintenanceRequestExists(int id)
        {
            return _context.MaintenanceRequests.Any(e => e.Id == id);
        }

        private bool CanAccessMaintenanceRequest(MaintenanceRequest request, ApplicationUser user)
        {
            return user.Role == "Admin" || 
                   user.Role == "ProjectManager" ||
                   request.ClientId == user.Id ||
                   request.AssignedToId == user.Id;
        }

        private bool CanUpdateMaintenanceRequest(MaintenanceRequest request, ApplicationUser user)
        {
            return user.Role == "Admin" || 
                   user.Role == "ProjectManager" ||
                   request.AssignedToId == user.Id;
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

        private async System.Threading.Tasks.Task SendNotificationToProjectManagers(MaintenanceRequest request)
        {
            var projectManagers = await _userManager.GetUsersInRoleAsync("ProjectManager");
            
            foreach (var pm in projectManagers)
            {
                var notification = new Notification
                {
                    Title = "New Maintenance Request",
                    Message = $"A new maintenance request '{request.Title}' has been submitted by {request.Client.FirstName} {request.Client.LastName}.",
                    Type = "InApp",
                    RecipientId = pm.Id,
                    Status = "Pending",
                    MaintenanceRequestId = request.Id,
                    CreatedAt = DateTime.Now
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }

        private async System.Threading.Tasks.Task SendNotificationToContractor(MaintenanceRequest request)
        {
            if (request.AssignedToId == null) return;

            var notification = new Notification
            {
                Title = "Maintenance Request Assigned",
                Message = $"You have been assigned to maintenance request '{request.Title}'.",
                Type = "InApp",
                RecipientId = request.AssignedToId,
                Status = "Pending",
                MaintenanceRequestId = request.Id,
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
    }
}