using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BCSApp.Models
{
    public class ProjectContractor
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ProjectId { get; set; }
        
        [Required]
        public string ContractorId { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? Specialization { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Active"; // Active, Inactive, Suspended
        
        public DateTime AssignedAt { get; set; } = DateTime.Now;
        public DateTime? UnassignedAt { get; set; }
        
        // Navigation properties
        public virtual Project Project { get; set; } = null!;
        public virtual ApplicationUser Contractor { get; set; } = null!;
    }
}
