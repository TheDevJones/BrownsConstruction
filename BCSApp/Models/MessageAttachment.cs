using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BCSApp.Models
{
    public class MessageAttachment
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int MessageId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string FileName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string FileType { get; set; } = string.Empty;
        
        public long FileSize { get; set; }
        
        // Navigation properties
        public virtual Message Message { get; set; } = null!;
    }
}
