using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace BCSApp.Tests.Helpers
{
    public static class TestHelpers
    {
        public static ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        public static Mock<UserManager<ApplicationUser>> GetMockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            return mockUserManager;
        }

        public static Mock<SignInManager<ApplicationUser>> GetMockSignInManager(Mock<UserManager<ApplicationUser>> userManager)
        {
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();

            return new Mock<SignInManager<ApplicationUser>>(
                userManager.Object,
                contextAccessor.Object,
                claimsFactory.Object,
                null!, null!, null!, null!);
        }

        public static ClaimsPrincipal GetTestUser(string userId, string email, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            return new ClaimsPrincipal(identity);
        }

        public static ApplicationUser CreateTestUser(string id, string email, string role)
        {
            return new ApplicationUser
            {
                Id = id,
                UserName = email,
                Email = email,
                FirstName = "Test",
                LastName = "User",
                Role = role,
                IsActive = true,
                EmailConfirmed = true
            };
        }

        public static Project CreateTestProject(string? managerId = null, string? clientId = null)
        {
            return new Project
            {
                Id = 1,
                Name = "Test Project",
                Description = "Test project description",
                Location = "Johannesburg",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(90),
                Budget = 1000000m,
                ActualCost = 0m,
                Status = "Planning",
                ProjectManagerId = managerId,
                ClientId = clientId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        public static MaintenanceRequest CreateTestMaintenanceRequest(string clientId, string? assignedToId = null)
        {
            return new MaintenanceRequest
            {
                Id = 1,
                Title = "Test Maintenance Request",
                Description = "Test description",
                Location = "Test Location",
                PropertyType = "Commercial",
                Status = "Pending",
                Priority = "High",
                ClientId = clientId,
                AssignedToId = assignedToId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        public static BCSApp.Models.Task CreateTestTask(string assignedToId, string createdById)
        {
            return new BCSApp.Models.Task
            {
                Id = 1,
                Title = "Test Task",
                Description = "Test task description",
                DueDate = DateTime.Now.AddDays(7),
                Status = "Pending",
                Priority = "High",
                EstimatedCost = 5000m,
                ActualCost = 0m,
                AssignedToId = assignedToId,
                CreatedById = createdById,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }
    }
}