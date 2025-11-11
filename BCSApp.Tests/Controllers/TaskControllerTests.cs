using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace BCSApp.Tests.Controllers
{
    public class TaskControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<ApplicationUser>> _userManager;
        private readonly TaskController _controller;
        private readonly ApplicationUser _testUser;

        public TaskControllerTests()
        {
            _context = TestHelpers.GetInMemoryDbContext();
            _userManager = TestHelpers.GetMockUserManager();
            _controller = new TaskController(_context, _userManager.Object);

            _testUser = TestHelpers.CreateTestUser("pm-id", "pm@example.com", "ProjectManager");

            var user = TestHelpers.GetTestUser(_testUser.Id, _testUser.Email, _testUser.Role);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_testUser);
        }

        [Fact]
        public async System.Threading.Tasks.Task Index_ReturnsViewWithTasks()
        {
            // Arrange
            var contractorId = "contractor-id";
            var task = TestHelpers.CreateTestTask(contractorId, _testUser.Id);
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult.Model.Should().BeAssignableTo<List<BCSApp.Models.Task>>();
            var model = viewResult.Model as List<BCSApp.Models.Task>;
            model.Should().HaveCount(1);
        }

        [Fact]
        public async System.Threading.Tasks.Task Index_ReturnsOnlyUserTasks_ForContractor()
        {
            // Arrange
            var contractorUser = TestHelpers.CreateTestUser("contractor-id", "contractor@example.com", "Contractor");
            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(contractorUser);

            var userTask = TestHelpers.CreateTestTask(contractorUser.Id, _testUser.Id);
            var otherTask = TestHelpers.CreateTestTask("other-contractor-id", _testUser.Id);

            _context.Tasks.AddRange(userTask, otherTask);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as List<BCSApp.Models.Task>;
            model.Should().HaveCount(1);
            model![0].AssignedToId.Should().Be(contractorUser.Id);
        }

        [Fact]
        public async System.Threading.Tasks.Task Details_ReturnsNotFound_WhenTaskDoesNotExist()
        {
            // Act
            var result = await _controller.Details(999);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async System.Threading.Tasks.Task Details_ReturnsViewWithTask_WhenTaskExists()
        {
            // Arrange
            var task = TestHelpers.CreateTestTask("contractor-id", _testUser.Id);
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Details(task.Id);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult.Model.Should().BeOfType<BCSApp.Models.Task>();
        }

        [Fact]
        public async System.Threading.Tasks.Task Details_ReturnsForbid_WhenUserCannotAccessTask()
        {
            // Arrange
            var contractorUser = TestHelpers.CreateTestUser("contractor-id", "contractor@example.com", "Contractor");
            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(contractorUser);

            var task = TestHelpers.CreateTestTask("other-contractor-id", "other-pm-id");
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Details(task.Id);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async System.Threading.Tasks.Task Create_Get_ReturnsViewWithContractorsAndPhases()
        {
            // Arrange
            var contractors = new List<ApplicationUser>
            {
                TestHelpers.CreateTestUser("contractor1", "contractor1@example.com", "Contractor")
            };

            _userManager.Setup(x => x.GetUsersInRoleAsync("Contractor"))
                .ReturnsAsync(contractors);

            var project = TestHelpers.CreateTestProject();
            var phase = new ProjectPhase
            {
                Id = 1,
                Name = "Phase 1",
                ProjectId = project.Id,
                Project = project,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(30),
                Budget = 100000m
            };
            _context.ProjectPhases.Add(phase);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Create();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult.ViewData["Contractors"].Should().BeEquivalentTo(contractors);
            viewResult.ViewData["ProjectPhases"].Should().NotBeNull();
        }

        [Fact]
        public async System.Threading.Tasks.Task Create_Post_CreatesTask_WhenModelIsValid()
        {
            // Arrange
            var task = new BCSApp.Models.Task
            {
                Title = "Install Windows",
                Description = "Install all windows on floor 2",
                DueDate = DateTime.Now.AddDays(14),
                Priority = "High",
                EstimatedCost = 25000m,
                AssignedToId = "contractor-id"
            };

            // Act
            var result = await _controller.Create(task);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();

            var savedTask = await _context.Tasks.FirstOrDefaultAsync();
            savedTask.Should().NotBeNull();
            savedTask!.Title.Should().Be("Install Windows");
            savedTask.CreatedById.Should().Be(_testUser.Id);
            savedTask.Status.Should().Be("Pending");
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateStatus_UpdatesTaskStatus()
        {
            // Arrange
            var contractorId = "contractor-id";
            var contractorUser = TestHelpers.CreateTestUser(contractorId, "contractor@example.com", "Contractor");
            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(contractorUser);

            var task = TestHelpers.CreateTestTask(contractorId, _testUser.Id);
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.UpdateStatus(task.Id, "In Progress", "Work started", null);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();

            var updatedTask = await _context.Tasks.FindAsync(task.Id);
            updatedTask!.Status.Should().Be("In Progress");

            var update = await _context.TaskUpdates.FirstOrDefaultAsync();
            update.Should().NotBeNull();
            update!.Description.Should().Be("Work started");
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateStatus_SetsCompletedAt_WhenStatusIsCompleted()
        {
            // Arrange
            var contractorId = "contractor-id";
            var contractorUser = TestHelpers.CreateTestUser(contractorId, "contractor@example.com", "Contractor");
            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(contractorUser);

            var task = TestHelpers.CreateTestTask(contractorId, _testUser.Id);
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Act
            await _controller.UpdateStatus(task.Id, "Completed", "Work completed", 6000m);

            // Assert
            var updatedTask = await _context.Tasks.FindAsync(task.Id);
            updatedTask!.CompletedAt.Should().NotBeNull();
            updatedTask.CompletedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
            updatedTask.ActualCost.Should().Be(6000m);
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateStatus_UpdatesActualCost_WhenProvided()
        {
            // Arrange
            var contractorId = "contractor-id";
            var contractorUser = TestHelpers.CreateTestUser(contractorId, "contractor@example.com", "Contractor");
            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(contractorUser);

            var task = TestHelpers.CreateTestTask(contractorId, _testUser.Id);
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Act
            await _controller.UpdateStatus(task.Id, "In Progress", "Partial completion", 3000m);

            // Assert
            var updatedTask = await _context.Tasks.FindAsync(task.Id);
            updatedTask!.ActualCost.Should().Be(3000m);
        }

        [Fact]
        public async System.Threading.Tasks.Task MyTasks_ReturnsOnlyContractorTasks()
        {
            // Arrange
            var contractorUser = TestHelpers.CreateTestUser("contractor-id", "contractor@example.com", "Contractor");
            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(contractorUser);

            var userTask = TestHelpers.CreateTestTask(contractorUser.Id, _testUser.Id);
            var otherTask = TestHelpers.CreateTestTask("other-contractor-id", _testUser.Id);

            _context.Tasks.AddRange(userTask, otherTask);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.MyTasks();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as List<BCSApp.Models.Task>;
            model.Should().HaveCount(1);
            model![0].AssignedToId.Should().Be(contractorUser.Id);
        }
    }
}