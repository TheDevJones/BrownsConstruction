using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BCSApp.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string EntityType { get; set; } = string.Empty; // Project, Task, MaintenanceRequest, etc.
        
        [Required]
        public int EntityId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty; // Create, Update, Delete, View, etc.
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Column(TypeName = "nvarchar(max)")]
        public string? OldValues { get; set; }
        
        [Column(TypeName = "nvarchar(max)")]
        public string? NewValues { get; set; }
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [StringLength(200)]
        public string? IPAddress { get; set; }
        
        [StringLength(500)]
        public string? UserAgent { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
