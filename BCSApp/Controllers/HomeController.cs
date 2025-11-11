using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BCSApp.Data;
using BCSApp.Models;
using System.Diagnostics;

namespace BCSApp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<HomeController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var dashboardData = new DashboardViewModel
            {
                User = user,
                TotalProjects = await GetTotalProjects(user),
                ActiveProjects = await GetActiveProjects(user),
                TotalMaintenanceRequests = await GetTotalMaintenanceRequests(user),
                PendingMaintenanceRequests = await GetPendingMaintenanceRequests(user),
                TotalTasks = await GetTotalTasks(user),
                OverdueTasks = await GetOverdueTasks(user),
                RecentProjects = await GetRecentProjects(user),
                RecentMaintenanceRequests = await GetRecentMaintenanceRequests(user),
                RecentTasks = await GetRecentTasks(user),
                ProjectStatusChart = await GetProjectStatusChart(user),
                MaintenancePriorityChart = await GetMaintenancePriorityChart(user),
                TaskStatusChart = await GetTaskStatusChart(user),
                BudgetVsActualChart = await GetBudgetVsActualChart(user)
            };

            return View(dashboardData);
        }

        private async Task<int> GetTotalProjects(ApplicationUser user)
        {
            return await _context.Projects
                .Where(p => user.Role == "Admin" || 
                           p.ProjectManagerId == user.Id || 
                           p.ClientId == user.Id ||
                           p.ProjectContractors.Any(pc => pc.ContractorId == user.Id))
                .CountAsync();
        }

        private async Task<int> GetActiveProjects(ApplicationUser user)
        {
            return await _context.Projects
                .Where(p => (user.Role == "Admin" || 
                            p.ProjectManagerId == user.Id || 
                            p.ClientId == user.Id ||
                            p.ProjectContractors.Any(pc => pc.ContractorId == user.Id)) &&
                           p.Status == "In Progress")
                .CountAsync();
        }

        private async Task<int> GetTotalMaintenanceRequests(ApplicationUser user)
        {
            return await _context.MaintenanceRequests
                .Where(m => user.Role == "Admin" || 
                           user.Role == "ProjectManager" ||
                           m.ClientId == user.Id ||
                           m.AssignedToId == user.Id)
                .CountAsync();
        }

        private async Task<int> GetPendingMaintenanceRequests(ApplicationUser user)
        {
            return await _context.MaintenanceRequests
                .Where(m => (user.Role == "Admin" || 
                            user.Role == "ProjectManager" ||
                            m.ClientId == user.Id ||
                            m.AssignedToId == user.Id) &&
                           m.Status == "Pending")
                .CountAsync();
        }

        private async Task<int> GetTotalTasks(ApplicationUser user)
        {
            return await _context.Tasks
                .Where(t => user.Role == "Admin" || 
                           user.Role == "ProjectManager" ||
                           t.AssignedToId == user.Id ||
                           t.CreatedById == user.Id)
                .CountAsync();
        }

        private async Task<int> GetOverdueTasks(ApplicationUser user)
        {
            return await _context.Tasks
                .Where(t => (user.Role == "Admin" || 
                            user.Role == "ProjectManager" ||
                            t.AssignedToId == user.Id ||
                            t.CreatedById == user.Id) &&
                           t.DueDate < DateTime.Now &&
                           t.Status != "Completed")
                .CountAsync();
        }

        private async Task<List<Project>> GetRecentProjects(ApplicationUser user)
        {
            return await _context.Projects
                .Include(p => p.ProjectManager)
                .Include(p => p.Client)
                .Where(p => user.Role == "Admin" || 
                           p.ProjectManagerId == user.Id || 
                           p.ClientId == user.Id ||
                           p.ProjectContractors.Any(pc => pc.ContractorId == user.Id))
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<MaintenanceRequest>> GetRecentMaintenanceRequests(ApplicationUser user)
        {
            return await _context.MaintenanceRequests
                .Include(m => m.Client)
                .Include(m => m.AssignedTo)
                .Where(m => user.Role == "Admin" || 
                           user.Role == "ProjectManager" ||
                           m.ClientId == user.Id ||
                           m.AssignedToId == user.Id)
                .OrderByDescending(m => m.CreatedAt)
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<BCSApp.Models.Task>> GetRecentTasks(ApplicationUser user)
        {
            return await _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .Where(t => user.Role == "Admin" || 
                           user.Role == "ProjectManager" ||
                           t.AssignedToId == user.Id ||
                           t.CreatedById == user.Id)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .ToListAsync();
        }

        private async Task<Dictionary<string, int>> GetProjectStatusChart(ApplicationUser user)
        {
            var projects = await _context.Projects
                .Where(p => user.Role == "Admin" || 
                           p.ProjectManagerId == user.Id || 
                           p.ClientId == user.Id ||
                           p.ProjectContractors.Any(pc => pc.ContractorId == user.Id))
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return projects.ToDictionary(p => p.Status, p => p.Count);
        }

        private async Task<Dictionary<string, int>> GetMaintenancePriorityChart(ApplicationUser user)
        {
            var requests = await _context.MaintenanceRequests
                .Where(m => user.Role == "Admin" || 
                           user.Role == "ProjectManager" ||
                           m.ClientId == user.Id ||
                           m.AssignedToId == user.Id)
                .GroupBy(m => m.Priority)
                .Select(g => new { Priority = g.Key, Count = g.Count() })
                .ToListAsync();

            return requests.ToDictionary(r => r.Priority, r => r.Count);
        }

        private async Task<Dictionary<string, int>> GetTaskStatusChart(ApplicationUser user)
        {
            var tasks = await _context.Tasks
                .Where(t => user.Role == "Admin" || 
                           user.Role == "ProjectManager" ||
                           t.AssignedToId == user.Id ||
                           t.CreatedById == user.Id)
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return tasks.ToDictionary(t => t.Status, t => t.Count);
        }

        private async Task<Dictionary<string, decimal>> GetBudgetVsActualChart(ApplicationUser user)
        {
            var projects = await _context.Projects
                .Where(p => user.Role == "Admin" || 
                           p.ProjectManagerId == user.Id || 
                           p.ClientId == user.Id ||
                           p.ProjectContractors.Any(pc => pc.ContractorId == user.Id))
                .ToListAsync();

            var result = new Dictionary<string, decimal>();
            foreach (var project in projects)
            {
                result[$"{project.Name} (Budget)"] = project.Budget;
                result[$"{project.Name} (Actual)"] = project.ActualCost;
            }

            return result;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}