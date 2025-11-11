using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BCSApp.Models
{
    public class QuotationItem
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public int Quantity { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }
        
        [StringLength(100)]
        public string? Category { get; set; } // Material, Labor, Equipment, etc.
        
        [Required]
        public int QuotationId { get; set; }
        
        // Navigation properties
        public virtual Quotation Quotation { get; set; } = null!;
    }
}
