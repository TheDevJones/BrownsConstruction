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
    public class TaskController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TaskController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Task
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var tasks = await _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .Include(t => t.ProjectPhase)
                .Include(t => t.MaintenanceRequest)
                .Where(t => user.Role == "Admin" || 
                           user.Role == "ProjectManager" ||
                           t.AssignedToId == user.Id ||
                           t.CreatedById == user.Id)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(tasks);
        }

        // GET: Task/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var task = await _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .Include(t => t.ProjectPhase)
                .Include(t => t.MaintenanceRequest)
                .Include(t => t.Updates)
                    .ThenInclude(u => u.UpdatedBy)
                .Include(t => t.Attachments)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (task == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (!CanAccessTask(task, user)) return Forbid();

            return View(task);
        }

        // GET: Task/Create
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Create()
        {
            var contractors = await _userManager.GetUsersInRoleAsync("Contractor");
            var projectPhases = await _context.ProjectPhases
                .Include(p => p.Project)
                .ToListAsync();
            var maintenanceRequests = await _context.MaintenanceRequests
                .Where(m => m.Status == "Pending" || m.Status == "In Progress")
                .ToListAsync();

            ViewBag.Contractors = contractors;
            ViewBag.ProjectPhases = projectPhases;
            ViewBag.MaintenanceRequests = maintenanceRequests;

            return View();
        }

        // POST: Task/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Create([Bind("Title,Description,DueDate,Priority,EstimatedCost,ProjectPhaseId,MaintenanceRequestId,AssignedToId")] BCSApp.Models.Task task)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                task.CreatedById = user.Id;
                task.CreatedAt = DateTime.Now;
                task.UpdatedAt = DateTime.Now;
                task.Status = "Pending";

                _context.Add(task);
                await _context.SaveChangesAsync();

                // Log the action
                await LogAuditAction("Task", task.Id, "Create", user.Id, null, JsonConvert.SerializeObject(task));

                // Send notification to assigned contractor
                await SendNotificationToContractor(task);

                return RedirectToAction(nameof(Index));
            }

            var contractors = await _userManager.GetUsersInRoleAsync("Contractor");
            var projectPhases = await _context.ProjectPhases
                .Include(p => p.Project)
                .ToListAsync();
            var maintenanceRequests = await _context.MaintenanceRequests
                .Where(m => m.Status == "Pending" || m.Status == "In Progress")
                .ToListAsync();

            ViewBag.Contractors = contractors;
            ViewBag.ProjectPhases = projectPhases;
            ViewBag.MaintenanceRequests = maintenanceRequests;

            return View(task);
        }

        // POST: Task/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string description, decimal? actualCost)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (!CanUpdateTask(task, user)) return Forbid();

            var oldStatus = task.Status;
            task.Status = status;
            task.UpdatedAt = DateTime.Now;

            if (actualCost.HasValue)
            {
                task.ActualCost = actualCost.Value;
            }

            if (status == "Completed")
            {
                task.CompletedAt = DateTime.Now;
            }

            _context.Update(task);

            // Add update record
            var update = new TaskUpdate
            {
                TaskId = id,
                UpdatedById = user.Id,
                StatusChange = $"{oldStatus} → {status}",
                Description = description,
                CostUpdate = actualCost,
                CreatedAt = DateTime.Now
            };

            _context.Add(update);
            await _context.SaveChangesAsync();

            // Log the action
            await LogAuditAction("Task", task.Id, "StatusUpdate", user.Id, oldStatus, status);

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Task/MyTasks
        [Authorize(Roles = "Contractor")]
        public async Task<IActionResult> MyTasks()
        {
            var user = await _userManager.GetUserAsync(User);
            var tasks = await _context.Tasks
                .Include(t => t.CreatedBy)
                .Include(t => t.ProjectPhase)
                .Include(t => t.MaintenanceRequest)
                .Where(t => t.AssignedToId == user.Id)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(tasks);
        }

        private bool TaskExists(int id)
        {
            return _context.Tasks.Any(e => e.Id == id);
        }

        private bool CanAccessTask(BCSApp.Models.Task task, ApplicationUser user)
        {
            return user.Role == "Admin" || 
                   user.Role == "ProjectManager" ||
                   task.AssignedToId == user.Id ||
                   task.CreatedById == user.Id;
        }

        private bool CanUpdateTask(BCSApp.Models.Task task, ApplicationUser user)
        {
            return user.Role == "Admin" || 
                   user.Role == "ProjectManager" ||
                   task.AssignedToId == user.Id;
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

        private async System.Threading.Tasks.Task SendNotificationToContractor(BCSApp.Models.Task task)
        {
            var notification = new Notification
            {
                Title = "New Task Assigned",
                Message = $"You have been assigned a new task: '{task.Title}'.",
                Type = "InApp",
                RecipientId = task.AssignedToId,
                Status = "Pending",
                TaskId = task.Id,
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
    }
}
