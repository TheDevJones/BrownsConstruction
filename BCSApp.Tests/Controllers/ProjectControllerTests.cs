using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace BCSApp.Tests.Controllers
{
    public class ProjectControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<ApplicationUser>> _userManager;
        private readonly ProjectController _controller;
        private readonly ApplicationUser _testUser;

        public ProjectControllerTests()
        {
            _context = TestHelpers.GetInMemoryDbContext();
            _userManager = TestHelpers.GetMockUserManager();
            _controller = new ProjectController(_context, _userManager.Object);

            // Setup test user
            _testUser = TestHelpers.CreateTestUser("test-user-id", "test@example.com", "Admin");

            // Setup controller context
            var user = TestHelpers.GetTestUser(_testUser.Id, _testUser.Email, _testUser.Role);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_testUser);
        }

        [Fact]
        public async System.Threading.Tasks.Task Index_ReturnsViewWithProjects_ForAdminUser()
        {
            // Arrange
            var project = TestHelpers.CreateTestProject(_testUser.Id);
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult.Model.Should().BeAssignableTo<List<Project>>();
            var model = viewResult.Model as List<Project>;
            model.Should().HaveCount(1);
            model![0].Name.Should().Be("Test Project");
        }

        [Fact]
        public async System.Threading.Tasks.Task Index_ReturnsOnlyUserProjects_ForNonAdminUser()
        {
            // Arrange
            var pmUser = TestHelpers.CreateTestUser("pm-user-id", "pm@example.com", "ProjectManager");
            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(pmUser);

            var pmProject = TestHelpers.CreateTestProject(pmUser.Id);
            var otherProject = TestHelpers.CreateTestProject("other-manager-id");

            _context.Projects.AddRange(pmProject, otherProject);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult.Model as List<Project>;
            model.Should().HaveCount(1);
            model![0].ProjectManagerId.Should().Be(pmUser.Id);
        }

        [Fact]
        public async System.Threading.Tasks.Task Details_ReturnsNotFound_WhenProjectDoesNotExist()
        {
            // Act
            var result = await _controller.Details(999);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async System.Threading.Tasks.Task Details_ReturnsViewWithProject_WhenProjectExists()
        {
            // Arrange
            var project = TestHelpers.CreateTestProject(_testUser.Id);
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Details(project.Id);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult.Model.Should().BeOfType<Project>();
            var model = viewResult.Model as Project;
            model!.Id.Should().Be(project.Id);
        }

        [Fact]
        public async System.Threading.Tasks.Task Details_ReturnsForbid_WhenUserCannotAccessProject()
        {
            // Arrange
            var clientUser = TestHelpers.CreateTestUser("client-id", "client@example.com", "Client");
            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(clientUser);

            var project = TestHelpers.CreateTestProject("other-manager-id", "other-client-id");
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Details(project.Id);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async System.Threading.Tasks.Task Create_Get_ReturnsViewWithUsersInViewBag()
        {
            // Arrange
            var clients = new List<ApplicationUser>
            {
                TestHelpers.CreateTestUser("client1", "client1@example.com", "Client")
            };
            var managers = new List<ApplicationUser>
            {
                TestHelpers.CreateTestUser("pm1", "pm1@example.com", "ProjectManager")
            };

            _userManager.Setup(x => x.GetUsersInRoleAsync("Client"))
                .ReturnsAsync(clients);
            _userManager.Setup(x => x.GetUsersInRoleAsync("ProjectManager"))
                .ReturnsAsync(managers);

            // Act
            var result = await _controller.Create();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult.ViewData["Clients"].Should().BeEquivalentTo(clients);
            viewResult.ViewData["ProjectManagers"].Should().BeEquivalentTo(managers);
        }

        [Fact]
        public async System.Threading.Tasks.Task Create_Post_CreatesProject_WhenModelIsValid()
        {
            // Arrange
            var project = new Project
            {
                Name = "New Project",
                Description = "New Description",
                Location = "Cape Town",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(180),
                Budget = 2000000m,
                ProjectManagerId = _testUser.Id,
                ClientId = "client-id"
            };

            // Act
            var result = await _controller.Create(project);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");

            var savedProject = await _context.Projects.FirstOrDefaultAsync();
            savedProject.Should().NotBeNull();
            savedProject!.Name.Should().Be("New Project");
            savedProject.Status.Should().Be("Planning");
        }

        [Fact]
        public async System.Threading.Tasks.Task Edit_Get_ReturnsNotFound_WhenProjectDoesNotExist()
        {
            // Act
            var result = await _controller.Edit(999);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async System.Threading.Tasks.Task Edit_Get_ReturnsForbid_WhenUserIsNotAuthorized()
        {
            // Arrange
            var pmUser = TestHelpers.CreateTestUser("pm-id", "pm@example.com", "ProjectManager");
            _userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(pmUser);

            var project = TestHelpers.CreateTestProject("other-manager-id");
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Edit(project.Id);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async System.Threading.Tasks.Task Edit_Post_UpdatesProject_WhenModelIsValid()
        {
            // Arrange
            var project = TestHelpers.CreateTestProject(_testUser.Id);
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            project.Name = "Updated Project Name";
            project.Status = "In Progress";

            // Act
            var result = await _controller.Edit(project.Id, project);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();

            var updatedProject = await _context.Projects.FindAsync(project.Id);
            updatedProject!.Name.Should().Be("Updated Project Name");
            updatedProject.Status.Should().Be("In Progress");
        }

        [Fact]
        public async System.Threading.Tasks.Task Delete_Get_ReturnsNotFound_WhenProjectDoesNotExist()
        {
            // Act
            var result = await _controller.Delete(999);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async System.Threading.Tasks.Task DeleteConfirmed_RemovesProject()
        {
            // Arrange
            var project = TestHelpers.CreateTestProject(_testUser.Id);
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            var projectId = project.Id;

            // Act
            var result = await _controller.DeleteConfirmed(projectId);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();

            var deletedProject = await _context.Projects.FindAsync(projectId);
            deletedProject.Should().BeNull();
        }

        [Fact]
        public async System.Threading.Tasks.Task Phases_ReturnsViewWithProject()
        {
            // Arrange
            var project = TestHelpers.CreateTestProject(_testUser.Id);
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Phases(project.Id);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult.Model.Should().BeOfType<Project>();
        }

        [Fact]
        public async System.Threading.Tasks.Task AddPhase_Post_CreatesPhase_WhenModelIsValid()
        {
            // Arrange
            var project = TestHelpers.CreateTestProject(_testUser.Id);
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            var phase = new ProjectPhase
            {
                Name = "Foundation",
                Description = "Foundation work",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(30),
                Budget = 500000m,
                ProjectId = project.Id,
                Order = 1
            };

            // Act
            var result = await _controller.AddPhase(phase);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();

            var savedPhase = await _context.ProjectPhases.FirstOrDefaultAsync();
            savedPhase.Should().NotBeNull();
            savedPhase!.Name.Should().Be("Foundation");
            savedPhase.Status.Should().Be("Not Started");
        }
    }
}