using System.Text.Json;

namespace NexTraceOne.Cli.Services;

public class ContractsService
{
    private readonly ApiService _apiService;

    public ContractsService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<List<ContractSummary>> ListContractsAsync(int page = 1, int pageSize = 20, string? status = null)
    {
        var endpoint = $"/api/v1/catalog/contracts?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(status))
            endpoint += $"&status={status}";

        var response = await _apiService.GetAsync<ContractsListResponse>(endpoint);
        return response.Contracts;
    }

    public async Task<ContractDetail> GetContractAsync(string contractId)
    {
        return await _apiService.GetAsync<ContractDetail>($"/api/v1/catalog/contracts/{contractId}");
    }

    public async Task<ContractDetail> CreateContractAsync(CreateContractRequest request)
    {
        return await _apiService.PostAsync<CreateContractRequest, ContractDetail>(
            "/api/v1/catalog/contracts", request);
    }

    public async Task UpdateContractAsync(string contractId, UpdateContractRequest request)
    {
        await _apiService.PutAsync($"/api/v1/catalog/contracts/{contractId}", request);
    }

    public async Task DeleteContractAsync(string contractId)
    {
        await _apiService.DeleteAsync($"/api/v1/catalog/contracts/{contractId}");
    }

    public async Task<string> ExportContractAsync(string contractId, string format = "postman")
    {
        var response = await _apiService.GetAsync<ExportResponse>($"/api/v1/catalog/contracts/{contractId}/export?format={format}");
        return response.Content;
    }
}

// Models
public class ContractsListResponse
{
    public List<ContractSummary> Contracts { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class ContractSummary
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ContractDetail
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SpecContent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class CreateContractRequest
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SpecContent { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class UpdateContractRequest
{
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Description { get; set; }
    public string? SpecContent { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class ExportResponse
{
    public string Format { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}
