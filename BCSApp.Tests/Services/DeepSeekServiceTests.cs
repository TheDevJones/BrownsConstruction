using Moq.Protected;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace BCSApp.Tests.Services
{
    public class DeepSeekServiceTests
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly Mock<IConfiguration> _configuration;
        private readonly Mock<ILogger<DeepSeekService>> _logger;
        private readonly DeepSeekService _service;

        public DeepSeekServiceTests()
        {
            _httpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandler.Object);
            _configuration = new Mock<IConfiguration>();
            _logger = new Mock<ILogger<DeepSeekService>>();

            // Setup configuration
            _configuration.Setup(x => x["DeepSeek:ApiKey"]).Returns("test-api-key");
            _configuration.Setup(x => x["DeepSeek:ApiUrl"]).Returns("https://api.deepseek.com/v1/chat/completions");

            _service = new DeepSeekService(_httpClient, _configuration.Object, _logger.Object);
        }

        [Fact]
        public async System.Threading.Tasks.Task GenerateCompletionAsync_ReturnsResponse_WhenApiCallSucceeds()
        {
            // Arrange
            var prompt = "Test prompt";
            var expectedResponse = "Test AI response";

            var apiResponse = new DeepSeekResponse
            {
                Id = "test-id",
                Object = "chat.completion",
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Model = "deepseek-chat",
                Choices = new List<DeepSeekChoice>
                {
                    new DeepSeekChoice
                    {
                        Index = 0,
                        Message = new DeepSeekMessage
                        {
                            Role = "assistant",
                            Content = expectedResponse
                        },
                        FinishReason = "stop"
                    }
                }
            };

            var jsonResponse = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _httpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            // Act
            var result = await _service.GenerateCompletionAsync(prompt);

            // Assert
            result.Should().Be(expectedResponse);
        }

        [Fact]
        public async System.Threading.Tasks.Task GenerateCompletionAsync_ThrowsException_WhenApiCallFails()
        {
            // Arrange
            var prompt = "Test prompt";

            _httpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("API Error")
                });

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.GenerateCompletionAsync(prompt));
        }

        [Fact]
        public async System.Threading.Tasks.Task GenerateCompletionAsync_ThrowsException_WhenNoChoicesReturned()
        {
            // Arrange
            var prompt = "Test prompt";

            var apiResponse = new DeepSeekResponse
            {
                Id = "test-id",
                Choices = new List<DeepSeekChoice>()
            };

            var jsonResponse = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _httpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.GenerateCompletionAsync(prompt));
        }

        [Fact]
        public async System.Threading.Tasks.Task AnalyzeBlueprintAsync_CallsGenerateCompletionAsync_WithCorrectPrompt()
        {
            // Arrange
            var blueprintPath = "/path/to/blueprint.pdf";
            var additionalContext = "3-story building";
            var expectedResponse = "Blueprint analysis results";

            var apiResponse = new DeepSeekResponse
            {
                Id = "test-id",
                Choices = new List<DeepSeekChoice>
                {
                    new DeepSeekChoice
                    {
                        Message = new DeepSeekMessage { Content = expectedResponse }
                    }
                }
            };

            var jsonResponse = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _httpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Content != null &&
                        req.Content.ReadAsStringAsync().Result.Contains("blueprint") &&
                        req.Content.ReadAsStringAsync().Result.Contains(blueprintPath)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            // Act
            var result = await _service.AnalyzeBlueprintAsync(blueprintPath, additionalContext);

            // Assert
            result.Should().Be(expectedResponse);
        }

        [Fact]
        public async System.Threading.Tasks.Task GenerateCostBreakdownAsync_ReturnsDetailedBreakdown()
        {
            // Arrange
            var projectDetails = "Office building, 2000 sqm";
            var expectedResponse = "Cost breakdown: Materials R500000, Labor R300000...";

            var apiResponse = new DeepSeekResponse
            {
                Id = "test-id",
                Choices = new List<DeepSeekChoice>
                {
                    new DeepSeekChoice
                    {
                        Message = new DeepSeekMessage { Content = expectedResponse }
                    }
                }
            };

            var jsonResponse = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _httpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            // Act
            var result = await _service.GenerateCostBreakdownAsync(projectDetails);

            // Assert
            result.Should().Be(expectedResponse);
        }

        [Fact]
        public async System.Threading.Tasks.Task PredictMaintenanceIssuesAsync_ReturnsMaintenancePredictions()
        {
            // Arrange
            var historicalData = "5 roof leaks in past year";
            var expectedResponse = "Predicted issues: Roof maintenance required...";

            var apiResponse = new DeepSeekResponse
            {
                Id = "test-id",
                Choices = new List<DeepSeekChoice>
                {
                    new DeepSeekChoice
                    {
                        Message = new DeepSeekMessage { Content = expectedResponse }
                    }
                }
            };

            var jsonResponse = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _httpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            // Act
            var result = await _service.PredictMaintenanceIssuesAsync(historicalData);

            // Assert
            result.Should().Be(expectedResponse);
        }

        [Fact]
        public async System.Threading.Tasks.Task AnalyzeProjectDelaysAsync_ReturnsDelayAnalysis()
        {
            // Arrange
            var projectData = "Project 30 days behind schedule";
            var expectedResponse = "Delay analysis: Critical path affected...";

            var apiResponse = new DeepSeekResponse
            {
                Id = "test-id",
                Choices = new List<DeepSeekChoice>
                {
                    new DeepSeekChoice
                    {
                        Message = new DeepSeekMessage { Content = expectedResponse }
                    }
                }
            };

            var jsonResponse = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _httpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            // Act
            var result = await _service.AnalyzeProjectDelaysAsync(projectData);

            // Assert
            result.Should().Be(expectedResponse);
        }

        [Fact]
        public void Constructor_ThrowsException_WhenApiKeyNotConfigured()
        {
            // Arrange
            var config = new Mock<IConfiguration>();
            config.Setup(x => x["DeepSeek:ApiKey"]).Returns((string)null!);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                new DeepSeekService(_httpClient, config.Object, _logger.Object));
        }
    }
}