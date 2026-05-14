using System.Text.Json;

namespace NexTraceOne.Cli.Services;

public class IncidentsService
{
    private readonly ApiService _apiService;

    public IncidentsService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<List<IncidentSummary>> ListIncidentsAsync(string? status = null, string? severity = null)
    {
        var endpoint = "/api/v1/governance/incidents";
        var parameters = new List<string>();
        
        if (!string.IsNullOrEmpty(status)) parameters.Add($"status={status}");
        if (!string.IsNullOrEmpty(severity)) parameters.Add($"severity={severity}");
        
        if (parameters.Any())
            endpoint += "?" + string.Join("&", parameters);

        var response = await _apiService.GetAsync<IncidentsListResponse>(endpoint);
        return response.Incidents;
    }

    public async Task<IncidentDetail> GetIncidentAsync(string incidentId)
    {
        return await _apiService.GetAsync<IncidentDetail>($"/api/v1/governance/incidents/{incidentId}");
    }

    public async Task<IncidentDetail> CreateIncidentAsync(CreateIncidentRequest request)
    {
        return await _apiService.PostAsync<CreateIncidentRequest, IncidentDetail>(
            "/api/v1/governance/incidents", request);
    }

    public async Task UpdateIncidentAsync(string incidentId, UpdateIncidentRequest request)
    {
        await _apiService.PutAsync($"/api/v1/governance/incidents/{incidentId}", request);
    }

    public async Task AddCommentAsync(string incidentId, string comment)
    {
        await _apiService.PostAsync<object, object>(
            $"/api/v1/governance/incidents/{incidentId}/comments",
            new { text = comment });
    }
}

// Models
public class IncidentsListResponse
{
    public List<IncidentSummary> Incidents { get; set; } = new();
    public int TotalCount { get; set; }
}

public class IncidentSummary
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class IncidentDetail
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public List<IncidentComment> Comments { get; set; } = new();
}

public class IncidentComment
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateIncidentRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // Low, Medium, High, Critical
    public string Environment { get; set; } = string.Empty;
}

public class UpdateIncidentRequest
{
    public string? Status { get; set; }
    public string? Severity { get; set; }
    public string? Description { get; set; }
}
