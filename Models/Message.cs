using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BCSApp.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;
        
        [Required]
        [StringLength(5000)]
        public string Content { get; set; } = string.Empty;
        
        [Required]
        public string SenderId { get; set; } = string.Empty;
        
        [Required]
        public string RecipientId { get; set; } = string.Empty;
        
        public int? ProjectId { get; set; }
        public int? MaintenanceRequestId { get; set; }
        public int? TaskId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string MessageType { get; set; } = "General"; // General, Notification, Alert, Update
        
        public bool IsRead { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ReadAt { get; set; }
        
        // Navigation properties
        public virtual ApplicationUser Sender { get; set; } = null!;
        public virtual ApplicationUser Recipient { get; set; } = null!;
        public virtual Project? Project { get; set; }
        public virtual MaintenanceRequest? MaintenanceRequest { get; set; }
        public virtual Task? Task { get; set; }
        public virtual ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
    }
}
