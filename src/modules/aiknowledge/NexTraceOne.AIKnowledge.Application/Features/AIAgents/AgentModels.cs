namespace NexTraceOne.AIKnowledge.Application.Features.AIAgents.Models;

public class AgentRequest
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public string AgentType { get; set; } = string.Empty; // dependency-advisor, architecture-fitness, etc.
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AgentResponse
{
    public string RequestId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Result { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ExecutionTime { get; set; }
}

public class AgentMetrics
{
    public string AgentType { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public double AverageExecutionTimeMs { get; set; }
    public double AccuracyScore { get; set; } // 0-100
    public DateTime LastExecuted { get; set; }
}

public class DependencyAnalysis
{
    public string PackageName { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = string.Empty;
    public string? LatestVersion { get; set; }
    public bool IsUpToDate { get; set; }
    public List<KnownVulnerability> Vulnerabilities { get; set; } = new();
    public UpdateRecommendation? Recommendation { get; set; }
}

public class KnownVulnerability
{
    public string Id { get; set; } = string.Empty; // CVE-2024-XXXXX
    public string Severity { get; set; } = string.Empty; // Low, Medium, High, Critical
    public string Description { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; }
    public string? FixedInVersion { get; set; }
    public string CvssScore { get; set; } = string.Empty;
}

public class UpdateRecommendation
{
    public string RecommendedVersion { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int RiskLevel { get; set; } // 1-5 (1=low risk, 5=high risk)
    public List<string> BreakingChanges { get; set; } = new();
    public List<string> Benefits { get; set; } = new();
}
