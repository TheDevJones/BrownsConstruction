using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BCSApp.Models
{
    public class Document
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string FileName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string FilePath { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string FileType { get; set; } = string.Empty; // PDF, DOCX, PNG, DWG, etc.
        
        public long FileSize { get; set; }
        
        [Required]
        [StringLength(50)]
        public string DocumentType { get; set; } = string.Empty; // Blueprint, Contract, Permit, Report, etc.
        
        public int? ProjectId { get; set; }
        public int? TaskId { get; set; }
        public int? MaintenanceRequestId { get; set; }
        
        [Required]
        public string UploadedById { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual Project? Project { get; set; }
        public virtual Task? Task { get; set; }
        public virtual MaintenanceRequest? MaintenanceRequest { get; set; }
        public virtual ApplicationUser UploadedBy { get; set; } = null!;
        public virtual ICollection<DocumentAccess> AccessLogs { get; set; } = new List<DocumentAccess>();
    }
}
