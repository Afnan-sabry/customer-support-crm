namespace CustomerSupport.Infrastructure.Services.Ai;

public class AiSettings
{
    public string Provider { get; set; } = "AzureOpenAI";
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChatDeployment { get; set; } = "gpt-4";
    public string EmbeddingDeployment { get; set; } = "text-embedding-ada-002";
    public int MaxConcurrentRequests { get; set; } = 10;
    public int MaxTokensPerMinute { get; set; } = 60000;
    public float DefaultTemperature { get; set; } = 0.3f;
    public int DefaultMaxTokens { get; set; } = 1024;
    public bool ChatbotEnabled { get; set; } = true;
    public int ChatbotMaxTurns { get; set; } = 5;
    public double ChatbotConfidenceThreshold { get; set; } = 0.6;
    public int ChatbotKnowledgeArticleCount { get; set; } = 5;
    public double CategorizationAutoApplyThreshold { get; set; } = 0.8;
}
