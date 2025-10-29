using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BCSApp.Models
{
    public class TaskUpdate
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public int TaskId { get; set; }
        
        [Required]
        public string UpdatedById { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string? StatusChange { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CostUpdate { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual Task Task { get; set; } = null!;
        public virtual ApplicationUser UpdatedBy { get; set; } = null!;
    }
}
