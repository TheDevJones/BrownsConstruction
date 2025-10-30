using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BCSApp.Data;
using BCSApp.Models;
using BCSApp.Services;
using Newtonsoft.Json;
using Task = System.Threading.Tasks.Task;

namespace BCSApp.Controllers
{
    [Authorize]
    public class AIAnalysisController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDeepSeekService _deepSeekService;
        private readonly ILogger<AIAnalysisController> _logger;

        public AIAnalysisController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            IDeepSeekService deepSeekService,
            ILogger<AIAnalysisController> logger)
        {
            _context = context;
            _userManager = userManager;
            _deepSeekService = deepSeekService;
            _logger = logger;
        }

        // GET: AIAnalysis
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var analyses = await _context.AIAnalyses
                .Include(a => a.RequestedBy)
                .Include(a => a.Project)
                .Include(a => a.MaintenanceRequest)
                .Include(a => a.Document)
                .Where(a => user.Role == "Admin" || 
                           user.Role == "ProjectManager" ||
                           a.RequestedById == user.Id)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View(analyses);
        }

        // GET: AIAnalysis/CostEstimation
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> CostEstimation()
        {
            var user = await _userManager.GetUserAsync(User);
            var projects = await _context.Projects
                .Where(p => user.Role == "Admin" || p.ProjectManagerId == user.Id)
                .ToListAsync();
            
            var documents = await _context.Documents
                .Where(d => d.DocumentType == "Blueprint" || d.DocumentType == "Contract")
                .ToListAsync();

            ViewBag.Projects = projects;
            ViewBag.Documents = documents;
            return View();
        }

        // POST: AIAnalysis/GenerateCostEstimation
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> GenerateCostEstimation(int? projectId, int? documentId, string description)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                
                // Gather context data
                var contextData = await GatherProjectContext(projectId, documentId);
                
                // Generate prompt for cost estimation
                var prompt = BuildCostEstimationPrompt(contextData, description);
                
                // Call DeepSeek API
                var response = await _deepSeekService.GenerateCompletionAsync(prompt);
                
                // Parse the response
                var estimation = ParseCostEstimation(response);
                
                // Save analysis
                var analysis = new AIAnalysis
                {
                    AnalysisType = "CostEstimation",
                    Title = $"Cost Estimation - {contextData.ProjectName ?? "General"}",
                    AnalysisResult = response,
                    EstimatedCost = estimation.TotalCost,
                    ConfidenceLevel = estimation.ConfidenceLevel,
                    Recommendations = estimation.Recommendations,
                    ProjectId = projectId,
                    DocumentId = documentId,
                    RequestedById = user.Id,
                    CreatedAt = DateTime.Now
                };

                _context.AIAnalyses.Add(analysis);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Cost estimation generated successfully!";
                return RedirectToAction(nameof(Details), new { id = analysis.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating cost estimation");
                TempData["Error"] = $"Error generating cost estimation: {ex.Message}";
                return RedirectToAction(nameof(CostEstimation));
            }
        }

        // GET: AIAnalysis/PredictiveMaintenance
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> PredictiveMaintenance()
        {
            var projects = await _context.Projects
                .Include(p => p.MaintenanceRequests)
                .ToListAsync();

            ViewBag.Projects = projects;
            return View();
        }

        // POST: AIAnalysis/GenerateMaintenancePrediction
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> GenerateMaintenancePrediction(int? projectId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                
                // Gather historical maintenance data
                var maintenanceHistory = await GatherMaintenanceHistory(projectId);
                
                // Generate prompt for predictive maintenance
                var prompt = BuildMaintenancePredictionPrompt(maintenanceHistory);
                
                // Call DeepSeek API
                var response = await _deepSeekService.GenerateCompletionAsync(prompt);
                
                // Parse the response
                var prediction = ParseMaintenancePrediction(response);
                
                // Save analysis
                var analysis = new AIAnalysis
                {
                    AnalysisType = "PredictiveMaintenance",
                    Title = $"Maintenance Prediction - {maintenanceHistory.ProjectName ?? "All Projects"}",
                    AnalysisResult = response,
                    RiskScore = prediction.RiskScore,
                    ConfidenceLevel = prediction.ConfidenceLevel,
                    Recommendations = prediction.Recommendations,
                    ProjectId = projectId,
                    RequestedById = user.Id,
                    CreatedAt = DateTime.Now
                };

                _context.AIAnalyses.Add(analysis);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Maintenance prediction generated successfully!";
                return RedirectToAction(nameof(Details), new { id = analysis.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating maintenance prediction");
                TempData["Error"] = $"Error generating maintenance prediction: {ex.Message}";
                return RedirectToAction(nameof(PredictiveMaintenance));
            }
        }

        // GET: AIAnalysis/RiskAnalysis
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> RiskAnalysis()
        {
            var projects = await _context.Projects
                .Include(p => p.Phases)
                .Include(p => p.MaintenanceRequests)
                .Where(p => p.Status == "Planning" || p.Status == "In Progress")
                .ToListAsync();

            ViewBag.Projects = projects;
            return View();
        }

        // POST: AIAnalysis/GenerateRiskAnalysis
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> GenerateRiskAnalysis(int projectId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                
                // Gather project risk data
                var riskData = await GatherProjectRiskData(projectId);
                
                // Generate prompt for risk analysis
                var prompt = BuildRiskAnalysisPrompt(riskData);
                
                // Call DeepSeek API
                var response = await _deepSeekService.GenerateCompletionAsync(prompt);
                
                // Parse the response
                var riskAnalysis = ParseRiskAnalysis(response);
                
                // Save analysis
                var analysis = new AIAnalysis
                {
                    AnalysisType = "RiskAnalysis",
                    Title = $"Risk Analysis - {riskData.ProjectName}",
                    AnalysisResult = response,
                    RiskScore = riskAnalysis.RiskScore,
                    ConfidenceLevel = riskAnalysis.ConfidenceLevel,
                    Recommendations = riskAnalysis.Recommendations,
                    ProjectId = projectId,
                    RequestedById = user.Id,
                    CreatedAt = DateTime.Now
                };

                _context.AIAnalyses.Add(analysis);
                await _context.SaveChangesAsync();

                // Create notification for high-risk projects
                if (riskAnalysis.RiskScore >= 70)
                {
                    await CreateRiskNotification(projectId, riskAnalysis.RiskScore, user.Id);
                }

                TempData["Success"] = "Risk analysis generated successfully!";
                return RedirectToAction(nameof(Details), new { id = analysis.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating risk analysis");
                TempData["Error"] = $"Error generating risk analysis: {ex.Message}";
                return RedirectToAction(nameof(RiskAnalysis));
            }
        }

        // GET: AIAnalysis/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var analysis = await _context.AIAnalyses
                .Include(a => a.RequestedBy)
                .Include(a => a.Project)
                .Include(a => a.MaintenanceRequest)
                .Include(a => a.Document)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (analysis == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user.Role != "Admin" && user.Role != "ProjectManager" && analysis.RequestedById != user.Id)
            {
                return Forbid();
            }

            return View(analysis);
        }

        // Helper Methods
        private async Task<ProjectContext> GatherProjectContext(int? projectId, int? documentId)
        {
            var context = new ProjectContext();

            if (projectId.HasValue)
            {
                var project = await _context.Projects
                    .Include(p => p.Phases)
                    .Include(p => p.MaintenanceRequests)
                    .FirstOrDefaultAsync(p => p.Id == projectId);

                if (project != null)
                {
                    context.ProjectName = project.Name;
                    context.Location = project.Location;
                    context.Budget = project.Budget;
                    context.Description = project.Description;
                    context.PhaseCount = project.Phases.Count;
                }
            }

            if (documentId.HasValue)
            {
                var document = await _context.Documents
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document != null)
                {
                    context.DocumentName = document.Name;
                    context.DocumentType = document.DocumentType;
                }
            }

            return context;
        }

        private string BuildCostEstimationPrompt(ProjectContext context, string description)
        {
            return $@"You are an expert construction cost estimator for a South African construction company. 
Analyze the following project information and provide a detailed cost estimation breakdown.

Project Information:
- Name: {context.ProjectName ?? "Not specified"}
- Location: {context.Location ?? "Not specified"}
- Budget: R{context.Budget:N2}
- Description: {context.Description ?? description}
- Number of Phases: {context.PhaseCount}
- Document: {context.DocumentName ?? "No blueprint provided"}

Please provide:
1. Detailed cost breakdown by category (Materials, Labor, Equipment, Permits, Contingency)
2. Timeline estimation
3. Risk factors that may affect costs
4. Recommendations for cost optimization
5. Confidence level (High/Medium/Low) for the estimation

Format your response as a structured analysis with clear sections and specific ZAR (South African Rand) amounts.";
        }

        private string BuildMaintenancePredictionPrompt(MaintenanceHistory history)
        {
            return $@"You are an expert in predictive maintenance for construction projects in South Africa.
Analyze the following historical maintenance data and predict future maintenance needs.

Historical Data:
- Project: {history.ProjectName ?? "All Projects"}
- Total Maintenance Requests: {history.TotalRequests}
- Completed: {history.CompletedRequests}
- Average Resolution Time: {history.AverageResolutionDays} days
- Common Issues: {string.Join(", ", history.CommonIssues)}
- High Priority Requests: {history.HighPriorityCount}

Recent Patterns:
{history.RecentPatterns}

Please provide:
1. Predicted maintenance issues in the next 3, 6, and 12 months
2. Risk score (0-100) for each predicted issue
3. Recommended preventive actions
4. Estimated costs for preventive vs reactive maintenance
5. Priority ranking of recommendations

Focus on South African construction standards and common regional issues.";
        }

        private string BuildRiskAnalysisPrompt(ProjectRiskData riskData)
        {
            return $@"You are a construction project risk analyst specializing in South African projects.
Analyze the following project data and identify potential risks and mitigation strategies.

Project: {riskData.ProjectName}
Status: {riskData.Status}
Budget: R{riskData.Budget:N2}
Actual Cost: R{riskData.ActualCost:N2}
Progress: {riskData.ProgressPercentage}%

Schedule:
- Start Date: {riskData.StartDate:yyyy-MM-dd}
- End Date: {riskData.EndDate:yyyy-MM-dd}
- Days Remaining: {riskData.DaysRemaining}
- Is Behind Schedule: {riskData.IsBehindSchedule}

Issues:
- Overdue Tasks: {riskData.OverdueTasks}
- Pending Maintenance: {riskData.PendingMaintenance}
- Budget Variance: R{riskData.BudgetVariance:N2}

Please provide:
1. Overall risk score (0-100)
2. Top 5 identified risks with severity ratings
3. Schedule risk assessment and recommendations
4. Budget risk assessment
5. Quality and safety considerations
6. Specific mitigation strategies for each risk
7. Recommended actions with priority levels

Consider South African regulatory requirements and common regional challenges.";
        }

        private CostEstimation ParseCostEstimation(string response)
        {
            // Simple parsing - in production, use more robust JSON parsing
            var estimation = new CostEstimation
            {
                TotalCost = ExtractTotalCost(response),
                ConfidenceLevel = ExtractConfidenceLevel(response),
                Recommendations = ExtractRecommendations(response)
            };
            return estimation;
        }

        private MaintenancePrediction ParseMaintenancePrediction(string response)
        {
            return new MaintenancePrediction
            {
                RiskScore = ExtractRiskScore(response),
                ConfidenceLevel = ExtractConfidenceLevel(response),
                Recommendations = ExtractRecommendations(response)
            };
        }

        private RiskAnalysisResult ParseRiskAnalysis(string response)
        {
            return new RiskAnalysisResult
            {
                RiskScore = ExtractRiskScore(response),
                ConfidenceLevel = ExtractConfidenceLevel(response),
                Recommendations = ExtractRecommendations(response)
            };
        }

        private decimal ExtractTotalCost(string response)
        {
            // Extract total cost from response - implement proper parsing
            var match = System.Text.RegularExpressions.Regex.Match(response, @"R?\s*(\d+(?:,\d{3})*(?:\.\d{2})?)");
            if (match.Success && decimal.TryParse(match.Groups[1].Value.Replace(",", ""), out decimal cost))
            {
                return cost;
            }
            return 0;
        }

        private decimal ExtractRiskScore(string response)
        {
            // Extract risk score from response
            var match = System.Text.RegularExpressions.Regex.Match(response, @"risk\s+score[:\s]+(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && decimal.TryParse(match.Groups[1].Value, out decimal score))
            {
                return score;
            }
            return 50; // Default medium risk
        }

        private string ExtractConfidenceLevel(string response)
        {
            if (response.Contains("High", StringComparison.OrdinalIgnoreCase))
                return "High";
            if (response.Contains("Low", StringComparison.OrdinalIgnoreCase))
                return "Low";
            return "Medium";
        }

        private string ExtractRecommendations(string response)
        {
            // Extract recommendations section
            var match = System.Text.RegularExpressions.Regex.Match(
                response, 
                @"Recommendations?:?\s*(.*?)(?=\n\n|\Z)", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline
            );
            return match.Success ? match.Groups[1].Value.Trim() : "See full analysis for details.";
        }

        private async Task<MaintenanceHistory> GatherMaintenanceHistory(int? projectId)
        {
            var query = _context.MaintenanceRequests.AsQueryable();
            
            if (projectId.HasValue)
            {
                query = query.Where(m => m.ProjectId == projectId);
            }

            var requests = await query
                .Include(m => m.Project)
                .ToListAsync();

            var history = new MaintenanceHistory
            {
                ProjectName = projectId.HasValue ? requests.FirstOrDefault()?.Project?.Name : "All Projects",
                TotalRequests = requests.Count,
                CompletedRequests = requests.Count(r => r.Status == "Completed"),
                HighPriorityCount = requests.Count(r => r.Priority == "High" || r.Priority == "Critical"),
                CommonIssues = requests.GroupBy(r => r.Title)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => g.Key)
                    .ToList()
            };

            var completedRequests = requests.Where(r => r.CompletedAt.HasValue && r.CreatedAt != null);
            if (completedRequests.Any())
            {
                history.AverageResolutionDays = completedRequests
                    .Average(r => (r.CompletedAt.Value - r.CreatedAt).TotalDays);
            }

            history.RecentPatterns = BuildRecentPatterns(requests.OrderByDescending(r => r.CreatedAt).Take(10));

            return history;
        }

        private string BuildRecentPatterns(IEnumerable<MaintenanceRequest> recentRequests)
        {
            return string.Join("\n", recentRequests.Select(r => 
                $"- {r.Title} ({r.Priority}): {r.Status} - {r.CreatedAt:yyyy-MM-dd}"));
        }

        private async Task<ProjectRiskData> GatherProjectRiskData(int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.Phases)
                .Include(p => p.MaintenanceRequests)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                throw new Exception("Project not found");

            var overdueTasks = await _context.Tasks
                .Where(t => t.ProjectPhase != null && 
                           t.ProjectPhase.ProjectId == projectId &&
                           t.DueDate < DateTime.Now &&
                           t.Status != "Completed")
                .CountAsync();

            var pendingMaintenance = project.MaintenanceRequests
                .Count(m => m.Status == "Pending" || m.Status == "In Progress");

            var totalDays = (project.EndDate - project.StartDate).TotalDays;
            var elapsedDays = (DateTime.Now - project.StartDate).TotalDays;
            var progressPercentage = totalDays > 0 ? (elapsedDays / totalDays) * 100 : 0;

            return new ProjectRiskData
            {
                ProjectName = project.Name,
                Status = project.Status,
                Budget = project.Budget,
                ActualCost = project.ActualCost,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                DaysRemaining = (int)(project.EndDate - DateTime.Now).TotalDays,
                IsBehindSchedule = DateTime.Now > project.EndDate && project.Status != "Completed",
                ProgressPercentage = Math.Round(progressPercentage, 2),
                OverdueTasks = overdueTasks,
                PendingMaintenance = pendingMaintenance,
                BudgetVariance = project.ActualCost - project.Budget
            };
        }

        private async Task CreateRiskNotification(int projectId, decimal riskScore, string userId)
        {
            var project = await _context.Projects
                .Include(p => p.ProjectManager)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project?.ProjectManagerId != null)
            {
                var notification = new Notification
                {
                    Title = "High Risk Alert",
                    Message = $"Project '{project.Name}' has been flagged with a high risk score of {riskScore}. Immediate attention required.",
                    Type = "InApp",
                    RecipientId = project.ProjectManagerId,
                    Status = "Pending",
                    ProjectId = projectId,
                    CreatedAt = DateTime.Now
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }
        }
    }

    // Helper classes
    public class ProjectContext
    {
        public string? ProjectName { get; set; }
        public string? Location { get; set; }
        public decimal Budget { get; set; }
        public string? Description { get; set; }
        public int PhaseCount { get; set; }
        public string? DocumentName { get; set; }
        public string? DocumentType { get; set; }
    }

    public class CostEstimation
    {
        public decimal TotalCost { get; set; }
        public string ConfidenceLevel { get; set; } = "Medium";
        public string Recommendations { get; set; } = "";
    }

    public class MaintenanceHistory
    {
        public string? ProjectName { get; set; }
        public int TotalRequests { get; set; }
        public int CompletedRequests { get; set; }
        public double AverageResolutionDays { get; set; }
        public List<string> CommonIssues { get; set; } = new();
        public int HighPriorityCount { get; set; }
        public string RecentPatterns { get; set; } = "";
    }

    public class MaintenancePrediction
    {
        public decimal RiskScore { get; set; }
        public string ConfidenceLevel { get; set; } = "Medium";
        public string Recommendations { get; set; } = "";
    }

    public class ProjectRiskData
    {
        public string ProjectName { get; set; } = "";
        public string Status { get; set; } = "";
        public decimal Budget { get; set; }
        public decimal ActualCost { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DaysRemaining { get; set; }
        public bool IsBehindSchedule { get; set; }
        public double ProgressPercentage { get; set; }
        public int OverdueTasks { get; set; }
        public int PendingMaintenance { get; set; }
        public decimal BudgetVariance { get; set; }
    }

    public class RiskAnalysisResult
    {
        public decimal RiskScore { get; set; }
        public string ConfidenceLevel { get; set; } = "Medium";
        public string Recommendations { get; set; } = "";
    }
}