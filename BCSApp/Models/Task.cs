using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BCSApp.Models
{
    public class Task
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public DateTime DueDate { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, In Progress, Completed, Overdue, Cancelled
        
        [Required]
        [StringLength(50)]
        public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedCost { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal ActualCost { get; set; }
        
        public int? ProjectPhaseId { get; set; }
        public int? MaintenanceRequestId { get; set; }
        
        [Required]
        public string AssignedToId { get; set; } = string.Empty;
        
        [Required]
        public string CreatedById { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        
        // Navigation properties
        public virtual ProjectPhase? ProjectPhase { get; set; }
        public virtual MaintenanceRequest? MaintenanceRequest { get; set; }
        public virtual ApplicationUser AssignedTo { get; set; } = null!;
        public virtual ApplicationUser CreatedBy { get; set; } = null!;
        public virtual ICollection<TaskUpdate> Updates { get; set; } = new List<TaskUpdate>();
        public virtual ICollection<Document> Attachments { get; set; } = new List<Document>();
    }
}
