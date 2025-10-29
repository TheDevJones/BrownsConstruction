using System.ComponentModel.DataAnnotations;

namespace BCSApp.Models
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role")]
        public string Role { get; set; } = "Client";

        [StringLength(200)]
        [Display(Name = "Company")]
        public string? Company { get; set; }

        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [StringLength(500)]
        [Display(Name = "Address")]
        public string? Address { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;
    }

    public class DashboardViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int TotalMaintenanceRequests { get; set; }
        public int PendingMaintenanceRequests { get; set; }
        public int TotalTasks { get; set; }
        public int OverdueTasks { get; set; }
        public List<Project> RecentProjects { get; set; } = new List<Project>();
        public List<MaintenanceRequest> RecentMaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
        public List<BCSApp.Models.Task> RecentTasks { get; set; } = new List<BCSApp.Models.Task>();
        public Dictionary<string, int> ProjectStatusChart { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> MaintenancePriorityChart { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> TaskStatusChart { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, decimal> BudgetVsActualChart { get; set; } = new Dictionary<string, decimal>();
    }
}
