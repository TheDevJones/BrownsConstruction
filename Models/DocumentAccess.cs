using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BCSApp.Models
{
    public class DocumentAccess
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int DocumentId { get; set; }
        
        [Required]
        public string AccessedById { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string AccessType { get; set; } = string.Empty; // View, Download, Edit
        
        public DateTime AccessedAt { get; set; } = DateTime.Now;
        
        [StringLength(200)]
        public string? IPAddress { get; set; }
        
        // Navigation properties
        public virtual Document Document { get; set; } = null!;
        public virtual ApplicationUser AccessedBy { get; set; } = null!;
    }
}
