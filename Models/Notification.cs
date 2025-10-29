using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BCSApp.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty; // Email, SMS, InApp, Push
        
        [Required]
        public string RecipientId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Sent, Delivered, Failed
        
        public int? ProjectId { get; set; }
        public int? MaintenanceRequestId { get; set; }
        public int? TaskId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        
        [StringLength(1000)]
        public string? ErrorMessage { get; set; }
        
        // Navigation properties
        public virtual ApplicationUser Recipient { get; set; } = null!;
        public virtual Project? Project { get; set; }
        public virtual MaintenanceRequest? MaintenanceRequest { get; set; }
        public virtual Task? Task { get; set; }
    }
}
