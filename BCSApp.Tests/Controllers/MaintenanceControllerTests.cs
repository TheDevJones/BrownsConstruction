using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace BCSApp.Tests.Controllers
{
    public class MaintenanceControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<ApplicationUser>> _userManager;
        private readonly MaintenanceController _controller;
        private readonly ApplicationUser _testUser;

        public MaintenanceControllerTests()
        {
            _context = TestHelpers.GetInMemoryDbContext();
            _userManager = TestHelpers.GetMockUserManager();
            _controller = new MaintenanceController(_context, _userManager.Object);

            _testUser = TestHelpers.CreateTestUser("client-id", "client@example.com", "Client");

            var user = TestHelpers.GetTestUser(_testUser.Id, _testUser.Email, _testUser.Role);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_testUser);
        }

        [Fact]
        public async System.Threading.Tasks.Task Index_ReturnsViewWithMaintenanceRequests()
        {
            // Arrange
            var request = TestHelpers.CreateTestMaintenanceRequest(_testUser.Id);
            _context.MaintenanceRequests.Add(request);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult.Model.Should().BeAssignableTo<List<MaintenanceRequest>>();
            var model = viewResult.Model as List<MaintenanceRequest>;
            model.Should().HaveCount(1);
        }

        [Fact]
        public async System.Threading.Tasks.Task Index_ReturnsOnlyUserRequests_ForClient()
        {
            // Arrange
            var userRequest = TestHelpers.CreateTestMaintenanceRequest(_testUser.Id);
            var otherRequest = TestHelpers.CreateTestMaintenanceRequest("other-client-id");

            _context.MaintenanceRequests.AddRange(userRequest, otherRequest);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as List<MaintenanceRequest>;
            model.Should().HaveCount(1);
            model![0].ClientId.Should().Be(_testUser.Id);
        }

        [Fact]
        public async System.Threading.Tasks.Task Index_ReturnsAllRequests_ForAdmin()
        {
            // Arrange
            var adminUser = TestHelpers.CreateTestUser("admin-id", "admin@example.com", "Admin");
            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(adminUser);

            var request1 = TestHelpers.CreateTestMaintenanceRequest("client1-id");
            var request2 = TestHelpers.CreateTestMaintenanceRequest("client2-id");

            _context.MaintenanceRequests.AddRange(request1, request2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as List<MaintenanceRequest>;
            model.Should().HaveCount(2);
        }

        [Fact]
        public async System.Threading.Tasks.Task Details_ReturnsNotFound_WhenRequestDoesNotExist()
        {
            // Act
            var result = await _controller.Details(999);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async System.Threading.Tasks.Task Details_ReturnsViewWithRequest_WhenRequestExists()
        {
            // Arrange
            var request = TestHelpers.CreateTestMaintenanceRequest(_testUser.Id);
            _context.MaintenanceRequests.Add(request);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Details(request.Id);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult.Model.Should().BeOfType<MaintenanceRequest>();
        }

        [Fact]
        public async System.Threading.Tasks.Task Details_ReturnsForbid_WhenUserCannotAccessRequest()
        {
            // Arrange
            var otherRequest = TestHelpers.CreateTestMaintenanceRequest("other-client-id");
            _context.MaintenanceRequests.Add(otherRequest);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Details(otherRequest.Id);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async System.Threading.Tasks.Task Create_Get_ReturnsViewWithProjects()
        {
            // Arrange
            var project = TestHelpers.CreateTestProject(clientId: _testUser.Id);
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Create();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult.ViewData["Projects"].Should().NotBeNull();
        }

        [Fact]
        public async System.Threading.Tasks.Task Create_Post_CreatesRequest_WhenModelIsValid()
        {
            // Arrange
            var request = new MaintenanceRequest
            {
                Title = "Roof Repair",
                Description = "Fix leaking roof",
                Location = "Building A",
                PropertyType = "Commercial",
                Priority = "High"
            };

            // Act
            var result = await _controller.Create(request);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();

            var savedRequest = await _context.MaintenanceRequests.FirstOrDefaultAsync();
            savedRequest.Should().NotBeNull();
            savedRequest!.Title.Should().Be("Roof Repair");
            savedRequest.ClientId.Should().Be(_testUser.Id);
            savedRequest.Status.Should().Be("Pending");
        }

        [Fact]
        public async System.Threading.Tasks.Task Edit_Get_ReturnsNotFound_WhenRequestDoesNotExist()
        {
            // Arrange
            var adminUser = TestHelpers.CreateTestUser("admin-id", "admin@example.com", "Admin");
            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(adminUser);

            // Act
            var result = await _controller.Edit(999);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async System.Threading.Tasks.Task Edit_Post_UpdatesRequest_WhenModelIsValid()
        {
            // Arrange
            var adminUser = TestHelpers.CreateTestUser("admin-id", "admin@example.com", "Admin");
            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(adminUser);

            var contractorId = "contractor-id";
            var request = TestHelpers.CreateTestMaintenanceRequest(_testUser.Id);
            _context.MaintenanceRequests.Add(request);
            await _context.SaveChangesAsync();

            request.Status = "In Progress";
            request.AssignedToId = contractorId;
            request.EstimatedCost = 15000m;

            // Act
            var result = await _controller.Edit(request.Id, request);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();

            var updatedRequest = await _context.MaintenanceRequests.FindAsync(request.Id);
            updatedRequest!.Status.Should().Be("In Progress");
            updatedRequest.AssignedToId.Should().Be(contractorId);
            updatedRequest.EstimatedCost.Should().Be(15000m);
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateStatus_UpdatesRequestStatus()
        {
            // Arrange
            var contractorId = "contractor-id";
            var contractorUser = TestHelpers.CreateTestUser(contractorId, "contractor@example.com", "Contractor");
            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(contractorUser);

            var request = TestHelpers.CreateTestMaintenanceRequest(_testUser.Id, contractorId);
            _context.MaintenanceRequests.Add(request);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.UpdateStatus(request.Id, "In Progress", "Started work");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();

            var updatedRequest = await _context.MaintenanceRequests.FindAsync(request.Id);
            updatedRequest!.Status.Should().Be("In Progress");

            var update = await _context.MaintenanceUpdates.FirstOrDefaultAsync();
            update.Should().NotBeNull();
            update!.Description.Should().Be("Started work");
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateStatus_SetsCompletedAt_WhenStatusIsCompleted()
        {
            // Arrange
            var contractorId = "contractor-id";
            var contractorUser = TestHelpers.CreateTestUser(contractorId, "contractor@example.com", "Contractor");
            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(contractorUser);

            var request = TestHelpers.CreateTestMaintenanceRequest(_testUser.Id, contractorId);
            _context.MaintenanceRequests.Add(request);
            await _context.SaveChangesAsync();

            // Act
            await _controller.UpdateStatus(request.Id, "Completed", "Work completed");

            // Assert
            var updatedRequest = await _context.MaintenanceRequests.FindAsync(request.Id);
            updatedRequest!.CompletedAt.Should().NotBeNull();
            updatedRequest.CompletedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async System.Threading.Tasks.Task ClientRequests_ReturnsOnlyClientRequests()
        {
            // Arrange
            var userRequest = TestHelpers.CreateTestMaintenanceRequest(_testUser.Id);
            var otherRequest = TestHelpers.CreateTestMaintenanceRequest("other-client-id");

            _context.MaintenanceRequests.AddRange(userRequest, otherRequest);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.ClientRequests();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as List<MaintenanceRequest>;
            model.Should().HaveCount(1);
            model![0].ClientId.Should().Be(_testUser.Id);
        }
    }
}