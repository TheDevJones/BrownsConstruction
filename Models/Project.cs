using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BCSApp.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Location { get; set; } = string.Empty;
        
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Budget { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal ActualCost { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Planning"; // Planning, In Progress, Completed, On Hold, Cancelled
        
        public string? ProjectManagerId { get; set; }
        
        public string? ClientId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual ApplicationUser? ProjectManager { get; set; }
        public virtual ApplicationUser? Client { get; set; }
        public virtual ICollection<ProjectPhase> Phases { get; set; } = new List<ProjectPhase>();
        public virtual ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
        public virtual ICollection<ProjectContractor> ProjectContractors { get; set; } = new List<ProjectContractor>();
    }
}
