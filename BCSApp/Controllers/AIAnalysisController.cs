using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BCSApp.Data;
using BCSApp.Models;
using BCSApp.Services;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

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
                var contextData = await GatherProjectContext(projectId, documentId);
                var prompt = BuildCostEstimationPrompt(contextData, description);
                var response = await _deepSeekService.GenerateCompletionAsync(prompt);
                var estimation = ParseCostEstimation(response);

                // Analysis with ALL structured data
                var analysis = new AIAnalysis
                {
                    AnalysisType = "CostEstimation",
                    Title = $"Cost Estimation - {contextData.ProjectName ?? "General"}",
                    AnalysisResult = response,
                    EstimatedCost = estimation.TotalCost,
                    ConfidenceLevel = estimation.ConfidenceLevel,
                    Recommendations = estimation.Recommendations,
                    DirectCosts = estimation.DirectCosts,
                    IndirectCosts = estimation.IndirectCosts,
                    ContingencyAmount = estimation.ContingencyAmount,
                    MaterialsCost = estimation.MaterialsCost,
                    LaborCost = estimation.LaborCost,
                    EquipmentCost = estimation.EquipmentCost,
                    ProjectDurationDays = estimation.ProjectDurationDays,
                    KeyFindings = JsonConvert.SerializeObject(estimation.KeyFindings),
                    CostBreakdown = JsonConvert.SerializeObject(estimation.CostBreakdown),

                    ProjectId = projectId,
                    DocumentId = documentId,
                    RequestedById = user.Id,
                    CreatedAt = DateTime.Now
                };

                _context.AIAnalyses.Add(analysis);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Cost estimation generated successfully with detailed analytics!";
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
                var maintenanceHistory = await GatherMaintenanceHistory(projectId);
                var prompt = BuildMaintenancePredictionPrompt(maintenanceHistory);
                var response = await _deepSeekService.GenerateCompletionAsync(prompt);
                var prediction = ParseMaintenancePrediction(response);

                // nalysis with ALL structured data
                var analysis = new AIAnalysis
                {
                    AnalysisType = "PredictiveMaintenance",
                    Title = $"Maintenance Prediction - {maintenanceHistory.ProjectName ?? "All Projects"}",
                    AnalysisResult = response,
                    RiskScore = prediction.RiskScore,
                    ConfidenceLevel = prediction.ConfidenceLevel,
                    Recommendations = prediction.Recommendations,

                    // NEW: Save structured maintenance data
                    KeyFindings = JsonConvert.SerializeObject(prediction.KeyFindings),
                    RiskFactors = JsonConvert.SerializeObject(prediction.RiskFactors),

                    ProjectId = projectId,
                    RequestedById = user.Id,
                    CreatedAt = DateTime.Now
                };

                _context.AIAnalyses.Add(analysis);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Maintenance prediction generated successfully with actionable insights!";
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
            return $@"You are a senior construction cost estimator with expertise in South African construction projects. Provide a comprehensive, professional cost estimation analysis.

**PROJECT DETAILS**
Project Name: {context.ProjectName ?? "Not specified"}
Location: {context.Location ?? "South Africa"}
Budget Allocation: R{context.Budget:N2}
Description: {context.Description ?? description}
Project Phases: {context.PhaseCount}
Reference Document: {context.DocumentName ?? "No documentation provided"}
Document Type: {context.DocumentType ?? "N/A"}

**REQUIRED DELIVERABLES**

1. EXECUTIVE SUMMARY
   - Total estimated cost in ZAR
   - Project duration estimate
   - Key cost drivers
   - Overall confidence assessment

2. DETAILED COST BREAKDOWN
   Structure your breakdown as follows:
   
   a) Direct Costs:
      • Materials & Supplies (itemized where possible)
      • Labour (skilled, semi-skilled, unskilled)
      • Equipment & Machinery (rental/purchase)
      • Subcontractor Services
   
   b) Indirect Costs:
      • Site Establishment & Preliminaries
      • Professional Fees (Engineering, Architecture, Project Management)
      • Insurance & Bonds
      • Permits & Regulatory Compliance (NHBRC, Municipal approvals)
   
   c) Risk Provisions:
      • Contingency Reserve (recommended 5-15% based on project complexity)
      • Price Escalation (consider current South African inflation rates)
      • Weather & Seasonal Factors

3. TIMELINE ESTIMATION
   - Project duration in months/weeks
   - Critical path activities
   - Milestone schedule
   - Seasonal considerations for South African climate

4. RISK FACTORS & ASSUMPTIONS
   - Material availability and supply chain risks
   - Labour market conditions
   - Regulatory and compliance requirements (SANS standards, OHS Act)
   - Site-specific challenges
   - Currency fluctuations affecting imported materials
   - Key assumptions made in this estimate

5. COST OPTIMIZATION RECOMMENDATIONS
   - Value engineering opportunities
   - Alternative material suggestions
   - Procurement strategies
   - Potential cost savings (with risk assessment)
   - Phasing strategies to manage cash flow

6. CONFIDENCE LEVEL ASSESSMENT
   Provide your confidence rating (HIGH/MEDIUM/LOW) with justification:
   - HIGH: Detailed specifications, stable market, similar project experience
   - MEDIUM: Adequate information, some market uncertainty
   - LOW: Limited information, volatile market, unique project characteristics

**OUTPUT FORMAT**
Present your analysis in a professional report structure with:
- Clear section headings
- Bullet points for lists
- Tables where appropriate (use plain text formatting)
- All monetary values in South African Rand (R)
- Percentages for variances and contingencies
- Professional tone suitable for stakeholder presentation

**COMPLIANCE NOTE**
Ensure all recommendations align with:
- South African National Building Regulations (SANS 10400)
- Construction Industry Development Board (CIDB) standards
- National Home Builders Registration Council (NHBRC) requirements
- Occupational Health and Safety Act compliance

Provide specific, actionable insights based on current South African construction market conditions.";
        }


        private string BuildMaintenancePredictionPrompt(MaintenanceHistory history)
        {
            return $@"You are a predictive maintenance specialist with expertise in South African construction and facility management. Analyze the historical data and provide a data-driven predictive maintenance strategy.

**HISTORICAL MAINTENANCE DATA**
Project Scope: {history.ProjectName ?? "Multi-Project Portfolio"}
Analysis Period: Current to date
Total Maintenance Requests: {history.TotalRequests}
Completion Rate: {history.CompletedRequests} completed ({(history.TotalRequests > 0 ? (history.CompletedRequests * 100.0 / history.TotalRequests).ToString("F1") : "0")}%)
Average Resolution Time: {history.AverageResolutionDays:F1} days
Critical/High Priority Issues: {history.HighPriorityCount}

**RECURRING ISSUE PATTERNS**
Most Common Issues:
{string.Join("\n", history.CommonIssues.Select((issue, i) => $"{i + 1}. {issue}"))}

**RECENT MAINTENANCE ACTIVITY**
{history.RecentPatterns}

**REQUIRED ANALYSIS**

1. PREDICTIVE MAINTENANCE FORECAST

   a) 3-MONTH OUTLOOK (Q1)
      • Predicted issues with probability scores
      • Estimated frequency of occurrences
      • Likely affected systems/components
      • Recommended inspection schedule
   
   b) 6-MONTH OUTLOOK (Q2)
      • Medium-term degradation predictions
      • Seasonal impact considerations (South African climate)
      • Component lifecycle assessments
      • Preventive maintenance windows
   
   c) 12-MONTH OUTLOOK (Annual)
      • Long-term maintenance requirements
      • Major overhaul/replacement predictions
      • Budget provisioning recommendations
      • Strategic maintenance planning

2. RISK SCORING MATRIX
   For each predicted issue, provide:
   • Risk Score (0-100): Likelihood × Impact
   • Severity Classification: Critical/High/Medium/Low
   • Potential Consequences: Safety, financial, operational impact
   • Urgency Rating: Immediate/Near-term/Scheduled

3. PREVENTIVE ACTION PLAN
   Prioritized recommendations including:
   • Specific inspection protocols
   • Maintenance procedures (aligned with SANS standards)
   • Required resources (personnel, equipment, materials)
   • Optimal scheduling (considering operational disruption)
   • Quality assurance checkpoints

4. COST-BENEFIT ANALYSIS
   Provide comparative analysis:
   
   Preventive Maintenance Approach:
   • Estimated costs (materials, labour, downtime)
   • Resource requirements
   • Implementation timeline
   • Expected ROI
   
   Reactive Maintenance Approach:
   • Projected failure costs (emergency repairs, extended downtime)
   • Secondary damage potential
   • Operational impact
   • Total cost of ownership difference

5. PRIORITY RANKING
   Rank all recommendations using this framework:
   
   PRIORITY 1 (Immediate - 0-30 days):
   • Critical safety issues
   • Imminent system failures
   • Regulatory compliance requirements
   
   PRIORITY 2 (Near-term - 1-3 months):
   • High-impact preventive measures
   • Efficiency optimizations
   • Component monitoring escalations
   
   PRIORITY 3 (Scheduled - 3-12 months):
   • Lifecycle replacements
   • System upgrades
   • Long-term improvements

6. REGIONAL CONSIDERATIONS
   Address South African specific factors:
   • Climate impact (seasonal rainfall, temperature extremes)
   • Load-shedding effects on electrical systems
   • Water scarcity impact on plumbing/HVAC
   • Material degradation due to environmental conditions
   • Availability of spare parts and qualified technicians

**OUTPUT FORMAT**
Structure your response as an executive maintenance report:
- Executive summary with key findings
- Data-driven predictions with supporting evidence
- Clear action items with ownership implications
- Risk matrix visualization (text-based table)
- Cost projections in ZAR
- Timeline Gantt-style representation (text-based)
- Appendix with technical specifications

**COMPLIANCE FRAMEWORK**
Ensure recommendations comply with:
- SANS 10400 (National Building Regulations)
- OHS Act (Occupational Health and Safety)
- SANS 10254 (Water supply installations)
- SANS 10142-1 (Wiring regulations)
- Relevant municipal bylaws

Provide actionable, measurable recommendations that can be directly implemented by facility management teams.";
        }



        private string BuildRiskAnalysisPrompt(ProjectRiskData riskData)
        {
            var budgetUtilization = riskData.Budget > 0 ? (riskData.ActualCost / riskData.Budget * 100) : 0;
            var scheduleStatus = riskData.IsBehindSchedule ? "BEHIND SCHEDULE" : "ON SCHEDULE";

            return $@"You are a certified project risk management professional specializing in South African construction projects. Conduct a comprehensive risk analysis using industry-standard methodologies.

**PROJECT OVERVIEW**
Project Name: {riskData.ProjectName}
Current Status: {riskData.Status}
Schedule Status: {scheduleStatus}

**FINANCIAL METRICS**
Approved Budget: R{riskData.Budget:N2}
Actual Cost to Date: R{riskData.ActualCost:N2}
Budget Utilization: {budgetUtilization:F1}%
Budget Variance: R{riskData.BudgetVariance:N2} ({(riskData.Budget > 0 ? (riskData.BudgetVariance / riskData.Budget * 100).ToString("F1") : "0")}%)

**SCHEDULE METRICS**
Project Start Date: {riskData.StartDate:dd MMM yyyy}
Planned Completion: {riskData.EndDate:dd MMM yyyy}
Days Remaining: {riskData.DaysRemaining} days
Progress Completion: {riskData.ProgressPercentage:F1}%

**PERFORMANCE INDICATORS**
Overdue Tasks: {riskData.OverdueTasks}
Pending Maintenance Issues: {riskData.PendingMaintenance}
Schedule Variance: {(riskData.IsBehindSchedule ? "Negative - Behind Schedule" : "Neutral/Positive")}

**REQUIRED RISK ASSESSMENT**

1. EXECUTIVE RISK SUMMARY
   • Overall Project Risk Score (0-100)
   • Risk Classification: Low (0-33) / Medium (34-66) / High (67-100)
   • Project Health Status: Green/Yellow/Red
   • Immediate Concerns Requiring Executive Attention
   • Trend Analysis: Improving/Stable/Deteriorating

2. COMPREHENSIVE RISK REGISTER

   For each identified risk, provide:

   **RISK ID & DESCRIPTION**
   **Category**: Financial/Schedule/Quality/Safety/Compliance/External
   **Probability**: Low (1-3) / Medium (4-6) / High (7-9)
   **Impact**: Minor (1-3) / Moderate (4-6) / Severe (7-9) / Critical (10)
   **Risk Score**: Probability × Impact (max 90)
   **Current Status**: Identified/Active/Escalating/Mitigated
   **Triggers**: Early warning indicators

   Identify TOP 5 CRITICAL RISKS including:
   a) Cost Overrun Risk
   b) Schedule Delay Risk  
   c) Quality Compliance Risk
   d) Safety & OHS Risk
   e) Regulatory/Legal Risk

3. DETAILED RISK ANALYSIS

   **A. SCHEDULE RISK ASSESSMENT**
   • Critical Path Analysis
   • Schedule Performance Index (SPI = {riskData.ProgressPercentage:F1}% / Expected Progress)
   • Milestone achievement status
   • Resource allocation adequacy
   • Weather/seasonal delay probability
   • Delay impact on project completion
   • Recovery strategies and float availability
   • Realistic completion forecast with confidence intervals

   **B. FINANCIAL RISK ASSESSMENT**
   • Cost Performance Index (CPI = Budget / Actual Cost)
   • Burn rate analysis
   • Cash flow projection risks
   • Escalation factors (inflation, material costs)
   • Currency exposure (imported materials)
   • Contingency reserve adequacy
   • Financial forecasting (Estimate at Completion)
   • Payment term risks with contractors/suppliers

   **C. QUALITY & COMPLIANCE RISK**
   • SANS 10400 compliance status
   • NHBRC warranty requirements
   • Quality control procedure adherence
   • Non-conformance reports analysis
   • Rework probability and impact
   • Professional indemnity exposure
   • Snag list projections

   **D. SAFETY & OCCUPATIONAL HEALTH RISK**
   • OHS Act compliance status
   • Historical incident analysis
   • Safety audit findings
   • High-risk activities identification
   • Personal Protective Equipment (PPE) adequacy
   • Site security concerns
   • Emergency response preparedness

   **E. EXTERNAL & ENVIRONMENTAL RISKS**
   • Socio-economic factors (labor actions, community relations)
   • Political/regulatory changes
   • Supply chain vulnerabilities
   • Load-shedding impact on productivity
   • Environmental compliance (NEMA, water use licenses)
   • Third-party dependencies
   • Force majeure considerations

4. MITIGATION STRATEGY FRAMEWORK

   For each critical risk, provide:
   
   **Mitigation Strategy**:
   • Preventive Actions: Steps to reduce probability
   • Contingency Plans: Response if risk materializes
   • Resource Requirements: Budget, personnel, time
   • Responsibility Assignment: Owner and stakeholders
   • Implementation Timeline: Immediate/Short-term/Ongoing
   • Success Metrics: How to measure effectiveness

   **Risk Response Categories**:
   • AVOID: Eliminate the threat
   • TRANSFER: Share or shift risk (insurance, subcontracts)
   • MITIGATE: Reduce probability or impact
   • ACCEPT: Acknowledge and monitor

5. ACTIONABLE RECOMMENDATIONS

   Prioritize recommendations using MoSCoW method:

   **MUST DO (Critical - 0-7 days)**
   • Actions required to prevent project failure
   • Immediate safety/compliance issues
   • Critical path protection measures

   **SHOULD DO (Important - 7-30 days)**
   • Significant risk reduction opportunities
   • Performance improvement initiatives
   • Stakeholder communication enhancements

   **COULD DO (Beneficial - 30-90 days)**
   • Process optimizations
   • Lessons learned implementations
   • Efficiency improvements

   **WON'T DO NOW (Future consideration)**
   • Nice-to-have improvements
   • Lower priority enhancements

6. MONITORING & CONTROL PLAN
   • Key Risk Indicators (KRIs) to track
   • Monitoring frequency and reporting
   • Escalation protocols and thresholds
   • Review meeting cadence recommendations
   • Dashboard metrics for ongoing visibility

7. REGIONAL COMPLIANCE CONSIDERATIONS
   Ensure analysis addresses:
   • Construction Industry Development Board (CIDB) requirements
   • National Home Builders Registration Council (NHBRC) standards
   • Municipal building control regulations
   • Employment Equity Act implications
   • B-BBEE compliance on procurement
   • Local labour content requirements
   • Environmental Management Plans (EMPs)

**OUTPUT FORMAT**
Present your analysis as a professional project risk report:
- Executive summary (1-page equivalent)
- Risk register in tabular format
- Heat map representation (High/Medium/Low matrix)
- Quantified risk exposure in ZAR
- Clear action plan with timelines
- Appendices with detailed calculations
- Professional tone suitable for board-level presentation

**ANALYTICAL FRAMEWORK**
Base your assessment on:
- Earned Value Management (EVM) principles
- PMI Risk Management standards
- South African Council for Project and Construction Management Professions (SACPCMP) guidelines
- Construction industry benchmarks for South African projects

Provide data-driven, objective analysis with specific, measurable, achievable, relevant, and time-bound (SMART) recommendations that enable immediate decision-making and risk mitigation actions.";
        }




        private CostEstimation ParseCostEstimation(string response)
        {
            var estimation = new CostEstimation
            {
                TotalCost = ExtractTotalCost(response),
                ConfidenceLevel = ExtractConfidenceLevel(response),
                Recommendations = ExtractRecommendations(response),

                // Extract detailed cost components
                DirectCosts = ExtractCostComponent(response, "Direct Costs", "Indirect Costs"),
                IndirectCosts = ExtractCostComponent(response, "Indirect Costs", "Risk Provisions"),
                ContingencyAmount = ExtractCostComponent(response, "Contingency", "Timeline"),
                MaterialsCost = ExtractCostComponent(response, "Materials", "Labour"),
                LaborCost = ExtractCostComponent(response, "Labour", "Equipment"),
                EquipmentCost = ExtractCostComponent(response, "Equipment", "Subcontractor"),

                // Extract timeline
                ProjectDurationDays = ExtractProjectDuration(response),

                // Extract key findings
                KeyFindings = ExtractKeyFindings(response),

                // Extract cost breakdown for charts
                CostBreakdown = ExtractDetailedCostBreakdown(response)
            };

            return estimation;
        }

        private MaintenancePrediction ParseMaintenancePrediction(string response)
        {
            return new MaintenancePrediction
            {
                RiskScore = ExtractRiskScore(response),
                ConfidenceLevel = ExtractConfidenceLevel(response),
                Recommendations = ExtractRecommendations(response),
                KeyFindings = ExtractKeyFindings(response),
                RiskFactors = ExtractMaintenanceRiskFactors(response)
            };
        }


        private RiskAnalysisResult ParseRiskAnalysis(string response)
        {
            return new RiskAnalysisResult
            {
                RiskScore = ExtractRiskScore(response),
                ConfidenceLevel = ExtractConfidenceLevel(response),
                Recommendations = ExtractRecommendations(response),

                // Extract risk categories
                ScheduleRisk = ExtractCategoryRisk(response, "Schedule Risk"),
                BudgetRisk = ExtractCategoryRisk(response, "Financial Risk|Budget Risk"),
                QualityRisk = ExtractCategoryRisk(response, "Quality Risk"),
                SafetyRisk = ExtractCategoryRisk(response, "Safety Risk"),

                // Extract structured risk items
                RiskFactors = ExtractRiskFactors(response),
                KeyFindings = ExtractKeyFindings(response)
            };
        }


        // Helper extraction methods
        private decimal ExtractCostComponent(string response, string startMarker, string endMarker)
        {
            try
            {
                var pattern = $@"{startMarker}[:\s]+.*?R?\s*(\d+(?:,\d{{3}})*(?:\.\d{{2}})?)\s*(?:million|M)?";
                var match = System.Text.RegularExpressions.Regex.Match(response, pattern,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase |
                    System.Text.RegularExpressions.RegexOptions.Singleline);

                if (match.Success)
                {
                    var value = match.Groups[1].Value.Replace(",", "");
                    if (decimal.TryParse(value, out decimal cost))
                    {
                        // Check if million/M mentioned
                        if (response.Substring(match.Index, Math.Min(50, response.Length - match.Index))
                            .Contains("million", StringComparison.OrdinalIgnoreCase))
                        {
                            cost *= 1000000;
                        }
                        return cost;
                    }
                }
            }
            catch { }
            return 0;
        }

        private int ExtractProjectDuration(string response)
        {
            var patterns = new[]
            {
        @"(\d+)\s+months?",
        @"(\d+)\s+weeks?",
        @"(\d+)\s+days?",
        @"duration[:\s]+(\d+)"
    };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(response, pattern,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int duration))
                {
                    if (pattern.Contains("months"))
                        return duration * 30;
                    if (pattern.Contains("weeks"))
                        return duration * 7;
                    return duration;
                }
            }
            return 0;
        }

        private decimal ExtractCategoryRisk(string response, string category)
        {
            var pattern = $@"{category}[:\s]+.*?(\d+(?:\.\d+)?)\s*(?:%|/100|score)";
            var match = System.Text.RegularExpressions.Regex.Match(response, pattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success && decimal.TryParse(match.Groups[1].Value, out decimal score))
            {
                return score > 100 ? score / 10 : score; // Normalize to 0-100
            }
            return 50; // Default medium risk
        }

        private List<string> ExtractKeyFindings(string response)
        {
            var findings = new List<string>();

            // Look for executive summary or key findings section
            var sections = new[] { "EXECUTIVE SUMMARY", "KEY FINDINGS", "SUMMARY", "HIGHLIGHTS" };
            foreach (var section in sections)
            {
                var pattern = $@"{section}[:\s]+(.*?)(?=\n\n[A-Z]{{3,}}|\Z)";
                var match = System.Text.RegularExpressions.Regex.Match(response, pattern,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase |
                    System.Text.RegularExpressions.RegexOptions.Singleline);

                if (match.Success)
                {
                    var content = match.Groups[1].Value;
                    var bullets = System.Text.RegularExpressions.Regex.Matches(content, @"[•\-\*]\s*(.+?)(?=\n|$)");

                    foreach (System.Text.RegularExpressions.Match bullet in bullets)
                    {
                        var finding = bullet.Groups[1].Value.Trim();
                        if (finding.Length > 10 && finding.Length < 200)
                        {
                            findings.Add(finding);
                        }
                    }

                    if (findings.Count > 0) break;
                }
            }

            if (findings.Count == 0)
            {
                var sentences = System.Text.RegularExpressions.Regex.Matches(response, @"([A-Z][^.!?]+[.!?])");
                findings.AddRange(sentences.Cast<System.Text.RegularExpressions.Match>()
                    .Take(5)
                    .Select(m => m.Groups[1].Value.Trim()));
            }

            return findings.Take(5).ToList();
        }

        private Dictionary<string, decimal> ExtractDetailedCostBreakdown(string response)
        {
            var breakdown = new Dictionary<string, decimal>();

            var categories = new Dictionary<string, string[]>
    {
        { "Materials & Supplies", new[] { "materials", "supplies" } },
        { "Labour", new[] { "labour", "labor", "workforce" } },
        { "Equipment", new[] { "equipment", "machinery", "plant" } },
        { "Subcontractors", new[] { "subcontractor", "sub-contractor" } },
        { "Professional Fees", new[] { "professional fees", "consulting" } },
        { "Permits & Compliance", new[] { "permits", "regulatory", "compliance" } },
        { "Contingency", new[] { "contingency", "reserve" } },
        { "Site Establishment", new[] { "site establishment", "preliminaries" } }
    };

            foreach (var category in categories)
            {
                foreach (var keyword in category.Value)
                {
                    var pattern = $@"{keyword}[:\s]+.*?R?\s*(\d+(?:,\d{{3}})*(?:\.\d{{2}})?)\s*(?:million|M)?";
                    var match = System.Text.RegularExpressions.Regex.Match(response, pattern,
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    if (match.Success && decimal.TryParse(match.Groups[1].Value.Replace(",", ""), out decimal cost))
                    {
                        if (response.Substring(match.Index, Math.Min(50, response.Length - match.Index))
                            .Contains("million", StringComparison.OrdinalIgnoreCase))
                        {
                            cost *= 1000000;
                        }
                        breakdown[category.Key] = cost;
                        break;
                    }
                }
            }

            return breakdown;
        }

        private List<RiskItem> ExtractRiskFactors(string response)
        {
            var risks = new List<RiskItem>();

            var pattern = @"(?:TOP|CRITICAL)\s+(?:\d+\s+)?RISKS?[:\s]+(.*?)(?=\n\n[A-Z]{{3,}}|\Z)";
            var match = System.Text.RegularExpressions.Regex.Match(response, pattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase |
                System.Text.RegularExpressions.RegexOptions.Singleline);

            if (match.Success)
            {
                var content = match.Groups[1].Value;
                var riskMatches = System.Text.RegularExpressions.Regex.Matches(content,
                    @"(?:[a-z]\)|[•\-\*]|\d+\.)\s*(.+?)(?:Risk Score|Score|Impact)[:\s]+(\d+)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                foreach (System.Text.RegularExpressions.Match riskMatch in riskMatches)
                {
                    var description = riskMatch.Groups[1].Value.Trim();
                    if (decimal.TryParse(riskMatch.Groups[2].Value, out decimal score))
                    {
                        risks.Add(new RiskItem
                        {
                            Description = description,
                            Score = score,
                            Severity = score >= 70 ? "Critical" : score >= 50 ? "High" : score >= 30 ? "Medium" : "Low",
                            Category = DetermineRiskCategory(description)
                        });
                    }
                }
            }

            return risks;
        }

        private List<RiskItem> ExtractMaintenanceRiskFactors(string response)
        {
            var risks = new List<RiskItem>();

            var priorities = new[] { "PRIORITY 1", "PRIORITY 2", "PRIORITY 3", "IMMEDIATE", "CRITICAL" };
            foreach (var priority in priorities)
            {
                var pattern = $@"{priority}[:\s]+(.*?)(?=PRIORITY|REGIONAL|\Z)";
                var match = System.Text.RegularExpressions.Regex.Match(response, pattern,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase |
                    System.Text.RegularExpressions.RegexOptions.Singleline);

                if (match.Success)
                {
                    var bullets = System.Text.RegularExpressions.Regex.Matches(match.Groups[1].Value, @"[•\-\*]\s*(.+?)(?=\n|$)");
                    foreach (System.Text.RegularExpressions.Match bullet in bullets)
                    {
                        var description = bullet.Groups[1].Value.Trim();
                        if (description.Length > 10)
                        {
                            risks.Add(new RiskItem
                            {
                                Description = description,
                                Category = "Maintenance",
                                Severity = priority.Contains("1") || priority.Contains("IMMEDIATE") ? "Critical" :
                                          priority.Contains("2") ? "High" : "Medium",
                                Score = priority.Contains("1") ? 80 : priority.Contains("2") ? 60 : 40
                            });
                        }
                    }
                }
            }

            return risks.Take(10).ToList();
        }

        private string DetermineRiskCategory(string description)
        {
            var desc = description.ToLower();
            if (desc.Contains("cost") || desc.Contains("budget") || desc.Contains("financial"))
                return "Financial";
            if (desc.Contains("schedule") || desc.Contains("delay") || desc.Contains("timeline"))
                return "Schedule";
            if (desc.Contains("quality") || desc.Contains("defect") || desc.Contains("standard"))
                return "Quality";
            if (desc.Contains("safety") || desc.Contains("injury") || desc.Contains("accident"))
                return "Safety";
            if (desc.Contains("regulatory") || desc.Contains("compliance") || desc.Contains("legal"))
                return "Compliance";
            return "General";
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
            IQueryable<MaintenanceRequest> query = _context.MaintenanceRequests;

            if (projectId.HasValue)
            {
                int projId = projectId.Value;
                query = query.Where(m => m.ProjectId == projId);
            }

            var requests = await query
                .Include(m => m.Project)
                .ToListAsync();

            var history = new MaintenanceHistory
            {
                ProjectName = projectId.HasValue ? requests.FirstOrDefault()?.Project?.Name : "All Projects",
                TotalRequests = requests.Count,
                CompletedRequests = requests.Where(r => r.Status == "Completed").Count(),
                HighPriorityCount = requests.Where(r => r.Priority == "High" || r.Priority == "Critical").Count(),
                CommonIssues = requests.GroupBy(r => r.Title)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => g.Key)
                    .ToList()
            };

            var completedRequests = requests.Where(r => r.CompletedAt.HasValue && r.CreatedAt != DateTime.MinValue).ToList();
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

        private async System.Threading.Tasks.Task CreateRiskNotification(int projectId, decimal riskScore, string userId)
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
        public decimal DirectCosts { get; set; }
        public decimal IndirectCosts { get; set; }
        public decimal ContingencyAmount { get; set; }
        public decimal MaterialsCost { get; set; }
        public decimal LaborCost { get; set; }
        public decimal EquipmentCost { get; set; }
        public int ProjectDurationDays { get; set; }
        public List<string> KeyFindings { get; set; } = new();
        public Dictionary<string, decimal> CostBreakdown { get; set; } = new();
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
        public List<RiskItem> RiskFactors { get; set; } = new();
        public List<string> KeyFindings { get; set; } = new();
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
        public decimal ScheduleRisk { get; set; }
        public decimal BudgetRisk { get; set; }
        public decimal QualityRisk { get; set; }
        public decimal SafetyRisk { get; set; }
        public List<RiskItem> RiskFactors { get; set; } = new();
        public List<string> KeyFindings { get; set; } = new();
    }



}