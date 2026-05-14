namespace NexTraceOne.AIKnowledge.Application.Features.NLPRouting.Models;

public enum LLMProviderType
{
    GPT4,           // OpenAI GPT-4 - High quality, expensive
    GPT35Turbo,     // OpenAI GPT-3.5-Turbo - Balanced
    Claude3Opus,    // Anthropic Claude 3 Opus - Premium
    Claude3Sonnet,  // Anthropic Claude 3 Sonnet - Balanced
    Claude3Haiku,   // Anthropic Claude 3 Haiku - Fast/Cheap
    GeminiPro,      // Google Gemini Pro - Cost-effective
    LocalLLM        // Self-hosted models (Llama, Mistral)
}

public class LLMProviderConfig
{
    public LLMProviderType ProviderType { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public double CostPerToken { get; set; } // USD per 1K tokens
    public int MaxTokens { get; set; }
    public double AvgLatencyMs { get; set; }
    public bool IsAvailable { get; set; } = true;
    public Dictionary<string, string> Capabilities { get; set; } = new();
}

public class RoutingDecision
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public string UserPrompt { get; set; } = string.Empty;
    public LLMProviderType SelectedProvider { get; set; }
    public double ConfidenceScore { get; set; } // 0-1
    public string Reasoning { get; set; } = string.Empty;
    public Dictionary<string, double> ProviderScores { get; set; } = new();
    public DateTime DecisionTimestamp { get; set; } = DateTime.UtcNow;
    public EstimatedCost EstimatedCost { get; set; } = new();
}

public class EstimatedCost
{
    public double InputTokens { get; set; }
    public double OutputTokens { get; set; }
    public double TotalCostUSD { get; set; }
    public double LatencyMs { get; set; }
}

public class PromptComplexity
{
    public string PromptText { get; set; } = string.Empty;
    public double ComplexityScore { get; set; } // 0-1
    public string Category { get; set; } = string.Empty; // code-generation, analysis, creative, factual, etc.
    public bool RequiresReasoning { get; set; }
    public bool RequiresCodeGeneration { get; set; }
    public bool RequiresCreativeWriting { get; set; }
    public bool RequiresFactualAccuracy { get; set; }
    public int EstimatedTokenCount { get; set; }
    public List<string> Keywords { get; set; } = new();
}

public class RoutingMetrics
{
    public DateTime TimeBucket { get; set; }
    public LLMProviderType Provider { get; set; }
    public long TotalRequests { get; set; }
    public double AvgCostPerRequest { get; set; }
    public double AvgLatencyMs { get; set; }
    public double SuccessRate { get; set; }
    public double UserSatisfactionScore { get; set; } // 0-100
}
