
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BCSApp.Models
{
    public class MaintenanceRequest
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, In Progress, Completed, Cancelled
        
        [Required]
        [StringLength(50)]
        public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical
        
        [Required]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string? PropertyType { get; set; } // Residential, Commercial, Industrial
        
        public int? ProjectId { get; set; }
        
        [Required]
        public string ClientId { get; set; } = string.Empty;
        
        public string? AssignedToId { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? EstimatedCost { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ActualCost { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        public DateTime? DueDate { get; set; }
        
        // Navigation properties
        public virtual Project? Project { get; set; }
        public virtual ApplicationUser Client { get; set; } = null!;
        public virtual ApplicationUser? AssignedTo { get; set; }
        public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
        public virtual ICollection<Document> Attachments { get; set; } = new List<Document>();
        public virtual ICollection<MaintenanceUpdate> Updates { get; set; } = new List<MaintenanceUpdate>();
    }
}
