using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BCSApp.Models
{
    public class Invoice
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Draft"; // Draft, Sent, Paid, Overdue, Cancelled
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceDue { get; set; }
        
        public int? QuotationId { get; set; }
        public int? ProjectId { get; set; }
        public int? MaintenanceRequestId { get; set; }
        
        [Required]
        public string ClientId { get; set; } = string.Empty;
        
        [Required]
        public string CreatedById { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public DateTime? SentAt { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidAt { get; set; }
        
        [StringLength(1000)]
        public string? PaymentNotes { get; set; }
        
        [StringLength(50)]
        public string? PaymentMethod { get; set; } // EFT, PayFast, PayPal, Cash, etc.
        
        // Navigation properties
        public virtual Quotation? Quotation { get; set; }
        public virtual Project? Project { get; set; }
        public virtual MaintenanceRequest? MaintenanceRequest { get; set; }
        public virtual ApplicationUser Client { get; set; } = null!;
        public virtual ApplicationUser CreatedBy { get; set; } = null!;
        public virtual ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
