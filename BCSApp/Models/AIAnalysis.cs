using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace BCSApp.Models
{
    public class AIAnalysis
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string AnalysisType { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string AnalysisResult { get; set; } = string.Empty; // Full text response

        [StringLength(2000)]
        public string? Recommendations { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? EstimatedCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? RiskScore { get; set; }

        [StringLength(50)]
        public string? ConfidenceLevel { get; set; }

        // NEW: Structured data fields for visualization
        public string? StructuredData { get; set; } // JSON string containing parsed analytics

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DirectCosts { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? IndirectCosts { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ContingencyAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaterialsCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? LaborCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? EquipmentCost { get; set; }

        public int? ProjectDurationDays { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? ScheduleRisk { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? BudgetRisk { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? QualityRisk { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? SafetyRisk { get; set; }

        public string? KeyFindings { get; set; } // JSON array of key points

        public string? RiskFactors { get; set; } // JSON array of risk items

        public string? CostBreakdown { get; set; } // JSON for detailed cost breakdown

        public int? ProjectId { get; set; }
        public int? MaintenanceRequestId { get; set; }
        public int? DocumentId { get; set; }

        [Required]
        public string RequestedById { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual Project? Project { get; set; }
        public virtual MaintenanceRequest? MaintenanceRequest { get; set; }
        public virtual Document? Document { get; set; }
        public virtual ApplicationUser RequestedBy { get; set; } = null!;

        // Helper methods for structured data
        [NotMapped]
        public List<string> KeyFindingsList
        {
            get => string.IsNullOrEmpty(KeyFindings)
                ? new List<string>()
                : JsonConvert.DeserializeObject<List<string>>(KeyFindings) ?? new List<string>();
            set => KeyFindings = JsonConvert.SerializeObject(value);
        }

        [NotMapped]
        public List<RiskItem> RiskFactorsList
        {
            get => string.IsNullOrEmpty(RiskFactors)
                ? new List<RiskItem>()
                : JsonConvert.DeserializeObject<List<RiskItem>>(RiskFactors) ?? new List<RiskItem>();
            set => RiskFactors = JsonConvert.SerializeObject(value);
        }

        [NotMapped]
        public Dictionary<string, decimal> CostBreakdownDict
        {
            get => string.IsNullOrEmpty(CostBreakdown)
                ? new Dictionary<string, decimal>()
                : JsonConvert.DeserializeObject<Dictionary<string, decimal>>(CostBreakdown) ?? new Dictionary<string, decimal>();
            set => CostBreakdown = JsonConvert.SerializeObject(value);
        }
    }

    // Supporting classes for structured data
    public class RiskItem
    {
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public string Severity { get; set; } = string.Empty; // Low, Medium, High, Critical
        public string Mitigation { get; set; } = string.Empty;
    }
}