using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BCSApp.Models
{
    public class AIAnalysis
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string AnalysisType { get; set; } = string.Empty; // BlueprintAnalysis, CostEstimation, RiskPrediction, etc.
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [StringLength(5000)]
        public string AnalysisResult { get; set; } = string.Empty;
        
        [StringLength(2000)]
        public string? Recommendations { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? EstimatedCost { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? RiskScore { get; set; }
        
        [StringLength(50)]
        public string? ConfidenceLevel { get; set; } // Low, Medium, High
        
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
    }
}
