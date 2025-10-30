namespace BCSApp.Services
{
    public interface IDeepSeekService
    {
        Task<string> GenerateCompletionAsync(string prompt, int maxTokens = 2000);
        Task<string> AnalyzeBlueprintAsync(string blueprintPath, string additionalContext);
        Task<string> GenerateCostBreakdownAsync(string projectDetails);
        Task<string> PredictMaintenanceIssuesAsync(string historicalData);
        Task<string> AnalyzeProjectDelaysAsync(string projectData);
    }
}