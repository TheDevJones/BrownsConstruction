using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BCSApp.Data;
using BCSApp.Models;
using System.Security.Claims;
using Newtonsoft.Json;

namespace BCSApp.Controllers
{
    [Authorize]
    public class ProjectController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProjectController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Project
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var projects = await _context.Projects
                .Include(p => p.ProjectManager)
                .Include(p => p.Client)
                .Include(p => p.Phases)
                .Where(p => user.Role == "Admin" || 
                           (p.ProjectManagerId != null && p.ProjectManagerId == user.Id) || 
                           (p.ClientId != null && p.ClientId == user.Id) ||
                           p.ProjectContractors.Any(pc => pc.ContractorId == user.Id))
                .ToListAsync();

            return View(projects);
        }

        // GET: Project/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var project = await _context.Projects
                .Include(p => p.ProjectManager)
                .Include(p => p.Client)
                .Include(p => p.Phases)
                .Include(p => p.MaintenanceRequests)
                .Include(p => p.Documents)
                .Include(p => p.ProjectContractors)
                    .ThenInclude(pc => pc.Contractor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (project == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (!CanAccessProject(project, user)) return Forbid();

            return View(project);
        }

        // GET: Project/Create
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Create()
        {
            var clients = await _userManager.GetUsersInRoleAsync("Client");
            var projectManagers = await _userManager.GetUsersInRoleAsync("ProjectManager");
            
            ViewBag.Clients = clients;
            ViewBag.ProjectManagers = projectManagers;
            
            return View();
        }

        // POST: Project/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Create([Bind("Name,Description,Location,StartDate,EndDate,Budget,ProjectManagerId,ClientId")] Project project)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                project.CreatedAt = DateTime.Now;
                project.UpdatedAt = DateTime.Now;
                project.Status = "Planning";

                // Handle empty strings as null values
                if (string.IsNullOrEmpty(project.ProjectManagerId))
                    project.ProjectManagerId = null;
                if (string.IsNullOrEmpty(project.ClientId))
                    project.ClientId = null;

                _context.Add(project);
                await _context.SaveChangesAsync();

                // Log the action
                await LogAuditAction("Project", project.Id, "Create", user.Id, null, JsonConvert.SerializeObject(project));

                return RedirectToAction(nameof(Index));
            }

            var clients = await _userManager.GetUsersInRoleAsync("Client");
            var projectManagers = await _userManager.GetUsersInRoleAsync("ProjectManager");
            
            ViewBag.Clients = clients;
            ViewBag.ProjectManagers = projectManagers;

            return View(project);
        }

        // GET: Project/Edit/5
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user.Role != "Admin" && (project.ProjectManagerId == null || project.ProjectManagerId != user.Id)) return Forbid();

            var clients = await _userManager.GetUsersInRoleAsync("Client");
            var projectManagers = await _userManager.GetUsersInRoleAsync("ProjectManager");
            
            ViewBag.Clients = clients;
            ViewBag.ProjectManagers = projectManagers;

            return View(project);
        }

        // POST: Project/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Location,StartDate,EndDate,Budget,ActualCost,Status,ProjectManagerId,ClientId")] Project project)
        {
            if (id != project.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.GetUserAsync(User);
                    var oldProject = await _context.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                    
                    // Handle empty strings as null values
                    if (string.IsNullOrEmpty(project.ProjectManagerId))
                        project.ProjectManagerId = null;
                    if (string.IsNullOrEmpty(project.ClientId))
                        project.ClientId = null;
                    
                    project.UpdatedAt = DateTime.Now;
                    _context.Update(project);
                    await _context.SaveChangesAsync();

                    // Log the action
                    await LogAuditAction("Project", project.Id, "Update", user.Id, 
                        JsonConvert.SerializeObject(oldProject), JsonConvert.SerializeObject(project));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            var clients = await _userManager.GetUsersInRoleAsync("Client");
            var projectManagers = await _userManager.GetUsersInRoleAsync("ProjectManager");
            
            ViewBag.Clients = clients;
            ViewBag.ProjectManagers = projectManagers;

            return View(project);
        }

        // GET: Project/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var project = await _context.Projects
                .Include(p => p.ProjectManager)
                .Include(p => p.Client)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (project == null) return NotFound();

            return View(project);
        }

        // POST: Project/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project != null)
            {
                var user = await _userManager.GetUserAsync(User);
                
                // Log the action before deletion
                await LogAuditAction("Project", project.Id, "Delete", user.Id, 
                    JsonConvert.SerializeObject(project), null);

                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Project/Phases/5
        public async Task<IActionResult> Phases(int? id)
        {
            if (id == null) return NotFound();

            var project = await _context.Projects
                .Include(p => p.Phases)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (!CanAccessProject(project, user)) return Forbid();

            return View(project);
        }

        // GET: Project/AddPhase/5
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> AddPhase(int? projectId)
        {
            if (projectId == null) return NotFound();

            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user.Role != "Admin" && (project.ProjectManagerId == null || project.ProjectManagerId != user.Id)) return Forbid();

            ViewBag.ProjectId = projectId;
            return View();
        }

        // POST: Project/AddPhase
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> AddPhase([Bind("Name,Description,StartDate,EndDate,Budget,ProjectId,Order")] ProjectPhase phase)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                var project = await _context.Projects.FindAsync(phase.ProjectId);
                
                if (user.Role != "Admin" && (project.ProjectManagerId == null || project.ProjectManagerId != user.Id)) return Forbid();

                phase.CreatedAt = DateTime.Now;
                phase.UpdatedAt = DateTime.Now;
                phase.Status = "Not Started";

                _context.Add(phase);
                await _context.SaveChangesAsync();

                // Log the action
                await LogAuditAction("ProjectPhase", phase.Id, "Create", user.Id, null, JsonConvert.SerializeObject(phase));

                return RedirectToAction(nameof(Phases), new { id = phase.ProjectId });
            }

            ViewBag.ProjectId = phase.ProjectId;
            return View(phase);
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.Id == id);
        }

        private bool CanAccessProject(Project project, ApplicationUser user)
        {
            return user.Role == "Admin" || 
                   (project.ProjectManagerId != null && project.ProjectManagerId == user.Id) || 
                   (project.ClientId != null && project.ClientId == user.Id) ||
                   project.ProjectContractors.Any(pc => pc.ContractorId == user.Id);
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
