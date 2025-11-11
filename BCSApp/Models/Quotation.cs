using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BCSApp.Models
{
    public class Quotation
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Draft"; // Draft, Sent, Accepted, Rejected, Expired
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal { get; set; }
        
        public int? ProjectId { get; set; }
        public int? MaintenanceRequestId { get; set; }
        
        [Required]
        public string ClientId { get; set; } = string.Empty;
        
        [Required]
        public string CreatedById { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public DateTime? SentAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        
        [StringLength(1000)]
        public string? Notes { get; set; }
        
        // Navigation properties
        public virtual Project? Project { get; set; }
        public virtual MaintenanceRequest? MaintenanceRequest { get; set; }
        public virtual ApplicationUser Client { get; set; } = null!;
        public virtual ApplicationUser CreatedBy { get; set; } = null!;
        public virtual ICollection<QuotationItem> Items { get; set; } = new List<QuotationItem>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}
