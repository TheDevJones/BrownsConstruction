using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BCSApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string Role { get; set; } = "Client"; // Admin, ProjectManager, Contractor, Client
        
        [StringLength(200)]
        public string? Company { get; set; }
        
        [StringLength(500)]
        public string? Address { get; set; }
        
        [StringLength(20)]
        public string? Phone { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual ICollection<Project> ManagedProjects { get; set; } = new List<Project>();
        public virtual ICollection<Project> ClientProjects { get; set; } = new List<Project>();
        public virtual ICollection<Task> AssignedTasks { get; set; } = new List<Task>();
        public virtual ICollection<Task> CreatedTasks { get; set; } = new List<Task>();
        public virtual ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
        public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
        public virtual ICollection<ProjectContractor> ContractorProjects { get; set; } = new List<ProjectContractor>();
    }
}
