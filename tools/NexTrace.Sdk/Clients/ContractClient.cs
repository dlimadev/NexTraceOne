using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NexTrace.Sdk.Clients;

/// <summary>
/// Sub-cliente para Contract Governance da API NexTraceOne.
/// Permite obter, comparar, verificar e sincronizar contratos.
/// </summary>
public sealed class ContractClient
{
    private readonly HttpClient _http;

    internal ContractClient(HttpClient http) => _http = http;

    /// <summary>
    /// Retorna o contrato pelo identificador.
    /// </summary>
    public async Task<ContractSummary?> GetContractAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
        return await _http.GetFromJsonAsync<ContractSummary>(
            $"/api/v1/contracts/{Uri.EscapeDataString(id)}", ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Compara dois contratos e retorna o diff semântico.
    /// </summary>
    public async Task<ContractDiff?> DiffContractAsync(string id1, string id2, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id1)) throw new ArgumentNullException(nameof(id1));
        if (string.IsNullOrWhiteSpace(id2)) throw new ArgumentNullException(nameof(id2));
        return await _http.GetFromJsonAsync<ContractDiff>(
            $"/api/v1/contracts/diff?from={Uri.EscapeDataString(id1)}&to={Uri.EscapeDataString(id2)}", ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Cria um novo contrato.
    /// </summary>
    public async Task<ContractSummary?> CreateContractAsync(CreateContractRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var response = await _http.PostAsJsonAsync("/api/v1/contracts", request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ContractSummary>(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Atualiza um contrato existente.
    /// </summary>
    public async Task<ContractSummary?> UpdateContractAsync(string id, UpdateContractRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
        ArgumentNullException.ThrowIfNull(request);
        var response = await _http.PutAsJsonAsync($"/api/v1/contracts/{Uri.EscapeDataString(id)}", request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ContractSummary>(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Remove um contrato pelo identificador.
    /// </summary>
    public async Task DeleteContractAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
        var response = await _http.DeleteAsync($"/api/v1/contracts/{Uri.EscapeDataString(id)}", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Verifica um contrato contra uma especificação.
    /// </summary>
    public async Task<ContractVerification?> VerifyContractAsync(VerifyContractRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var response = await _http.PostAsJsonAsync("/api/v1/contracts/verifications", request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ContractVerification>(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Sincroniza um contrato com a implementação atual.
    /// </summary>
    public async Task<ContractSummary?> SyncContractAsync(string id, SyncContractRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
        ArgumentNullException.ThrowIfNull(request);
        var response = await _http.PostAsJsonAsync($"/api/v1/contracts/{Uri.EscapeDataString(id)}/sync", request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ContractSummary>(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Gera um patch de migração entre duas versões de contrato.
    /// </summary>
    public async Task<ContractMigrationPatch?> MigrationPatchAsync(string fromId, string toId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fromId)) throw new ArgumentNullException(nameof(fromId));
        if (string.IsNullOrWhiteSpace(toId)) throw new ArgumentNullException(nameof(toId));
        var response = await _http.PostAsJsonAsync(
            "/api/v1/contracts/migration-patch",
            new { fromContractId = fromId, toContractId = toId },
            ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ContractMigrationPatch>(ct).ConfigureAwait(false);
    }
}

/// <summary>
/// Resumo de um contrato retornado pela API.
/// </summary>
public sealed class ContractSummary
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }
}

/// <summary>
/// Resultado do diff semântico entre dois contratos.
/// </summary>
public sealed class ContractDiff
{
    [JsonPropertyName("fromVersion")]
    public string? FromVersion { get; init; }

    [JsonPropertyName("toVersion")]
    public string? ToVersion { get; init; }

    [JsonPropertyName("hasBreakingChanges")]
    public bool HasBreakingChanges { get; init; }

    [JsonPropertyName("summary")]
    public string? Summary { get; init; }
}

/// <summary>
/// Request para criação de contrato.
/// </summary>
public sealed class CreateContractRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Request para atualização de contrato.
/// </summary>
public sealed class UpdateContractRequest
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

/// <summary>
/// Request para verificação de contrato.
/// </summary>
public sealed class VerifyContractRequest
{
    [JsonPropertyName("contractId")]
    public string ContractId { get; set; } = string.Empty;

    [JsonPropertyName("specContent")]
    public string SpecContent { get; set; } = string.Empty;
}

/// <summary>
/// Resultado da verificação de um contrato.
/// </summary>
public sealed class ContractVerification
{
    [JsonPropertyName("contractId")]
    public string? ContractId { get; init; }

    [JsonPropertyName("isValid")]
    public bool IsValid { get; init; }

    [JsonPropertyName("violations")]
    public string[]? Violations { get; init; }
}

/// <summary>
/// Request para sincronização de contrato.
/// </summary>
public sealed class SyncContractRequest
{
    [JsonPropertyName("sourceUrl")]
    public string? SourceUrl { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

/// <summary>
/// Resultado de um patch de migração de contrato.
/// </summary>
public sealed class ContractMigrationPatch
{
    [JsonPropertyName("fromVersion")]
    public string? FromVersion { get; init; }

    [JsonPropertyName("toVersion")]
    public string? ToVersion { get; init; }

    [JsonPropertyName("patch")]
    public string? Patch { get; init; }

    [JsonPropertyName("hasBreakingChanges")]
    public bool HasBreakingChanges { get; init; }
}
