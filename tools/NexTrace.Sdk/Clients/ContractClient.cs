using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NexTrace.Sdk.Clients;

/// <summary>
/// Sub-cliente para Contract Governance da API NexTraceOne.
/// Permite obter contratos e comparar versões.
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
