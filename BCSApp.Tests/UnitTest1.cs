using Xunit;
using Moq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BCSApp.Controllers;
using BCSApp.Data;
using BCSApp.Models;  // if you need models
using BCSApp.Tests.Helpers; // if using your TestHelpers
using Microsoft.AspNetCore.Mvc;
using Xunit;
using ModelTask = BCSApp.Models.Task;

namespace BCSApp.Tests
{
    public class UnitTest1
    {
        private readonly ApplicationDbContext _context;
        private readonly ProjectController _controller;

        public UnitTest1()
        {
            _context = TestHelpers.GetInMemoryDbContext(); // your helper method for in-memory DB
            _controller = new ProjectController(_context, null); // pass UserManager mock if needed
        }

        [Fact]
        public async System.Threading.Tasks.Task MyTest()
        {
            // Act
            var result = await _controller.Index(); // or whichever async method you want to test

            // Assert
            Assert.NotNull(result);
        }


        // Minimal controller example for the test to compile
        public class MyController : ControllerBase
        {
            private readonly ILogger<MyController> _logger;
            public MyController(ILogger<MyController> logger)
            {
                _logger = logger;
            }

            public async System.Threading.Tasks.Task<IActionResult> GetDataAsync()
            {
                await System.Threading.Tasks.Task.Delay(10); // simulate async work
                return Ok(new { Message = "Hello World" });
            }
        }
    }
}