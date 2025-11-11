using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BCSApp.Services
{
    public class DeepSeekService : IDeepSeekService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DeepSeekService> _logger;
        private readonly string _apiKey;
        private readonly string _apiUrl;

        public DeepSeekService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<DeepSeekService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            // Get API key from configuration
            _apiKey = configuration["DeepSeek:ApiKey"] ??
                      Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY") ??
                      throw new InvalidOperationException("DeepSeek API key not configured");

            _apiUrl = configuration["DeepSeek:ApiUrl"] ?? "https://api.deepseek.com/v1/chat/completions";

            // DON'T set the header here - set it per request instead
            // _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<string> GenerateCompletionAsync(string prompt, int maxTokens = 2000)
        {
            try
            {
                var request = new DeepSeekRequest
                {
                    Model = "deepseek-chat",
                    Messages = new List<DeepSeekMessage>
            {
                new DeepSeekMessage
                {
                    Role = "system",
                    Content = "You are an expert AI assistant specializing in construction project management, cost estimation, risk analysis, and predictive maintenance for South African construction projects. Provide detailed, actionable insights based on data analysis."
                },
                new DeepSeekMessage
                {
                    Role = "user",
                    Content = prompt
                }
            },
                    MaxTokens = maxTokens,
                    Temperature = 0.7,
                    TopP = 0.95
                };

                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Create request message with proper headers
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, _apiUrl)
                {
                    Content = content
                };

                // Set authorization header for this request
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
                requestMessage.Headers.Add("Accept", "application/json");

                _logger.LogInformation("Sending request to DeepSeek API");
                _logger.LogInformation($"API URL: {_apiUrl}");
                _logger.LogInformation($"API Key (first 20 chars): {_apiKey?.Substring(0, Math.Min(20, _apiKey.Length))}...");

                var response = await _httpClient.SendAsync(requestMessage);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"DeepSeek API error: {response.StatusCode} - {errorContent}");
                    _logger.LogError($"Request headers: {string.Join(", ", requestMessage.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}");
                    throw new Exception($"DeepSeek API request failed: {response.StatusCode} - {errorContent}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<DeepSeekResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (result?.Choices == null || result.Choices.Count == 0)
                {
                    throw new Exception("No response generated from DeepSeek API");
                }

                return result.Choices[0].Message.Content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling DeepSeek API");
                throw;
            }
        }


        public async Task<string> AnalyzeBlueprintAsync(string blueprintPath, string additionalContext)
        {
            var prompt = $@"Analyze the following construction blueprint and provide detailed insights:

Blueprint: {blueprintPath}
Additional Context: {additionalContext}

Please provide:
1. Estimated material quantities
2. Labor requirements
3. Construction timeline
4. Potential challenges or risks
5. Cost estimation breakdown
6. Compliance considerations for South African building codes

Format your response with clear sections and specific measurements.";

            return await GenerateCompletionAsync(prompt, 3000);
        }

        public async Task<string> GenerateCostBreakdownAsync(string projectDetails)
        {
            var prompt = $@"Generate a detailed cost breakdown for the following construction project:

{projectDetails}

Provide a comprehensive breakdown including:
1. Materials (with specific items and quantities)
2. Labor costs (different skill levels)
3. Equipment rental
4. Permits and regulatory fees
5. Subcontractor costs
6. Contingency (10-15%)
7. Overhead and profit margin

Use South African Rand (ZAR) and consider current market rates in South Africa.
Format as a structured breakdown with subtotals and grand total.";

            return await GenerateCompletionAsync(prompt, 2500);
        }

        public async Task<string> PredictMaintenanceIssuesAsync(string historicalData)
        {
            var prompt = $@"Based on the following historical maintenance data, predict future maintenance needs:

{historicalData}

Analyze patterns and provide:
1. Top 5 predicted maintenance issues in next 3 months
2. Top 5 predicted issues in next 6 months
3. Long-term (12 month) predictions
4. Risk probability for each prediction (High/Medium/Low)
5. Estimated costs for each predicted issue
6. Preventive measures to avoid these issues
7. Recommended maintenance schedule

Focus on South African climate and construction standards.";

            return await GenerateCompletionAsync(prompt, 2500);
        }

        public async Task<string> AnalyzeProjectDelaysAsync(string projectData)
        {
            var prompt = $@"Analyze the following project data for potential delays and scheduling issues:

{projectData}

Provide comprehensive analysis including:
1. Current delay assessment
2. Root cause analysis
3. Impact on overall timeline
4. Critical path analysis
5. Risk of further delays
6. Recommended corrective actions with priorities
7. Revised timeline suggestions
8. Budget impact assessment
9. Stakeholder communication recommendations

Consider South African working conditions, public holidays, and typical project constraints.";

            return await GenerateCompletionAsync(prompt, 2500);
        }



    }

    // Request/Response Models
    public class DeepSeekRequest
    {
        public string Model { get; set; } = "deepseek-chat";
        public List<DeepSeekMessage> Messages { get; set; } = new();
        public int MaxTokens { get; set; } = 2000;
        public double Temperature { get; set; } = 0.7;
        public double TopP { get; set; } = 0.95;
    }

    public class DeepSeekMessage
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = "";
    }

    public class DeepSeekResponse
    {
        public string Id { get; set; } = "";
        public string Object { get; set; } = "";
        public long Created { get; set; }
        public string Model { get; set; } = "";
        public List<DeepSeekChoice> Choices { get; set; } = new();
        public DeepSeekUsage? Usage { get; set; }
    }

    public class DeepSeekChoice
    {
        public int Index { get; set; }
        public DeepSeekMessage Message { get; set; } = new();
        public string FinishReason { get; set; } = "";
    }

    public class DeepSeekUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}