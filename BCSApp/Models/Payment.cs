using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BCSApp.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int InvoiceId { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = string.Empty; // EFT, PayFast, PayPal, Cash, etc.
        
        [StringLength(100)]
        public string? TransactionReference { get; set; }
        
        [StringLength(1000)]
        public string? Notes { get; set; }
        
        [Required]
        public string ProcessedById { get; set; } = string.Empty;
        
        public DateTime ProcessedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual Invoice Invoice { get; set; } = null!;
        public virtual ApplicationUser ProcessedBy { get; set; } = null!;
    }
}
