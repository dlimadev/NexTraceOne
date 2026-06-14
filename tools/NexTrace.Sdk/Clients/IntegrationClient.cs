using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NexTrace.Sdk.Clients;

/// <summary>
/// Sub-cliente para aceleração de integrações entre serviços.
/// Permite descobrir contratos de um provider, gerar clientes tipados,
/// registar relações de consumo e avaliar blast-radius.
/// </summary>
public sealed class IntegrationClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal IntegrationClient(HttpClient http) => _http = http;

    /// <summary>
    /// Pesquisa um serviço pelo nome canónico ou parte dele.
    /// </summary>
    public async Task<IReadOnlyList<ServiceSearchResult>> SearchServicesAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) throw new ArgumentNullException(nameof(query));

        var response = await _http.GetAsync(
            $"/api/v1/catalog/services/search?q={Uri.EscapeDataString(query)}", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ServiceSearchResponse>(JsonOptions, ct).ConfigureAwait(false);
        return result?.Items ?? [];
    }

    /// <summary>
    /// Retorna o detalhe completo de um serviço do catálogo.
    /// </summary>
    public async Task<ServiceDetail?> GetServiceDetailAsync(string serviceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serviceId)) throw new ArgumentNullException(nameof(serviceId));

        return await _http.GetFromJsonAsync<ServiceDetail>(
            $"/api/v1/catalog/services/{Uri.EscapeDataString(serviceId)}", JsonOptions, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Lista os contratos associados a um serviço.
    /// </summary>
    public async Task<IReadOnlyList<ServiceContractItem>> ListServiceContractsAsync(string serviceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serviceId)) throw new ArgumentNullException(nameof(serviceId));

        var response = await _http.GetAsync(
            $"/api/v1/contracts/by-service/{Uri.EscapeDataString(serviceId)}", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ServiceContractsResponse>(JsonOptions, ct).ConfigureAwait(false);
        return result?.Contracts ?? [];
    }

    /// <summary>
    /// Retorna os detalhes de uma versão de contrato, incluindo o conteúdo da spec.
    /// </summary>
    public async Task<ContractDetail?> GetContractDetailAsync(string contractVersionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(contractVersionId)) throw new ArgumentNullException(nameof(contractVersionId));

        return await _http.GetFromJsonAsync<ContractDetail>(
            $"/api/v1/contracts/{Uri.EscapeDataString(contractVersionId)}/detail", JsonOptions, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Gera código (DTOs + endpoints) a partir do conteúdo de um contrato OpenAPI.
    /// </summary>
    public async Task<GeneratedCodeResult?> GenerateCodeAsync(GenerateCodeRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await _http.PostAsJsonAsync("/api/v1/contracts/generate-code", request, JsonOptions, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<GeneratedCodeResult>(JsonOptions, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Regista ou actualiza a relação de consumo de uma API asset.
    /// </summary>
    public async Task<ConsumerRelationship?> RegisterConsumerAsync(RegisterConsumerRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.ApiAssetId)) throw new ArgumentException("ApiAssetId is required.", nameof(request));

        var response = await _http.PostAsJsonAsync(
            $"/api/v1/catalog/apis/{Uri.EscapeDataString(request.ApiAssetId)}/consumers",
            request, JsonOptions, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ConsumerRelationship>(JsonOptions, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Avalia o blast-radius de um nó do catálogo (subgrafo de impacto).
    /// </summary>
    public async Task<ImpactAnalysis?> GetImpactAsync(string rootNodeId, int? maxDepth = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rootNodeId)) throw new ArgumentNullException(nameof(rootNodeId));

        var url = $"/api/v1/catalog/impact/{Uri.EscapeDataString(rootNodeId)}";
        if (maxDepth.HasValue)
            url += $"?maxDepth={maxDepth.Value}";

        return await _http.GetFromJsonAsync<ImpactAnalysis>(url, JsonOptions, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Orquestra a geração de um cliente consumidor tipado para um provider:
    /// descobre o serviço, lista os seus contratos, gera código para cada um e
    /// retorna os artefactos agregados.
    /// </summary>
    public async Task<ConsumerClientGenerationResult?> GenerateConsumerClientAsync(
        GenerateConsumerClientRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.ProviderName)) throw new ArgumentException("ProviderName is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.ConsumerName)) throw new ArgumentException("ConsumerName is required.", nameof(request));

        var matches = await SearchServicesAsync(request.ProviderName, ct).ConfigureAwait(false);
        ServiceSearchResult? provider = null;
        foreach (var match in matches)
        {
            if (string.Equals(match.Name, request.ProviderName, StringComparison.OrdinalIgnoreCase))
            {
                provider = match;
                break;
            }
        }

        provider ??= matches.Count > 0 ? matches[0] : null;

        if (provider is null || string.IsNullOrWhiteSpace(provider.ServiceId))
            return new ConsumerClientGenerationResult { ProviderName = request.ProviderName, Success = false, Error = "Provider service not found in catalog." };

        var detail = await GetServiceDetailAsync(provider.ServiceId, ct).ConfigureAwait(false);
        if (detail is null)
            return new ConsumerClientGenerationResult { ProviderName = request.ProviderName, Success = false, Error = "Could not retrieve provider service details." };

        var contracts = await ListServiceContractsAsync(provider.ServiceId, ct).ConfigureAwait(false);
        if (contracts.Count == 0)
            return new ConsumerClientGenerationResult
            {
                ProviderName = request.ProviderName,
                ProviderServiceId = provider.ServiceId,
                Success = false,
                Error = "No contracts found for provider service."
            };

        var generatedContracts = new List<GeneratedContractClient>();
        foreach (var contract in contracts)
        {
            if (string.IsNullOrWhiteSpace(contract.VersionId))
                continue;

            var contractDetail = await GetContractDetailAsync(contract.VersionId, ct).ConfigureAwait(false);
            if (contractDetail is null || string.IsNullOrWhiteSpace(contractDetail.SpecContent))
                continue;

            var filteredSpec = OpenApiSpecFilter.Apply(contractDetail.SpecContent, request.Routes);
            var specContent = filteredSpec ?? contractDetail.SpecContent;

            var codeRequest = new GenerateCodeRequest
            {
                SpecContent = specContent,
                ServiceName = ToKebabCase(request.ConsumerName),
                RootNamespace = request.RootNamespace
            };

            var codeResult = await GenerateCodeAsync(codeRequest, ct).ConfigureAwait(false);
            if (codeResult is null)
                continue;

            generatedContracts.Add(new GeneratedContractClient
            {
                ContractVersionId = contract.VersionId,
                ApiAssetId = contract.ApiAssetId,
                ApiName = contract.ApiName,
                SemVer = contract.SemVer,
                ServiceName = codeResult.ServiceName,
                Title = codeResult.Title,
                SchemaCount = codeResult.SchemaCount,
                OperationCount = codeResult.OperationCount,
                Files = codeResult.Files
            });
        }

        if (generatedContracts.Count == 0)
            return new ConsumerClientGenerationResult
            {
                ProviderName = request.ProviderName,
                ProviderServiceId = provider.ServiceId,
                Success = false,
                Error = "Could not generate code for any provider contract."
            };

        return new ConsumerClientGenerationResult
        {
            ProviderName = request.ProviderName,
            ProviderServiceId = provider.ServiceId,
            ConsumerName = request.ConsumerName,
            Success = true,
            GeneratedContracts = generatedContracts,
            TotalFiles = generatedContracts.Sum(c => c.Files?.Count ?? 0),
            TotalOperations = generatedContracts.Sum(c => c.OperationCount)
        };
    }

    private static string ToKebabCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "integration";

        return string.Concat(value
            .Select((c, i) => char.IsUpper(c) && i > 0 ? $"-{char.ToLowerInvariant(c)}" : char.ToLowerInvariant(c).ToString()));
    }
}

/// <summary>Resultado da pesquisa de serviços.</summary>
public sealed class ServiceSearchResponse
{
    [JsonPropertyName("items")]
    public IReadOnlyList<ServiceSearchResult> Items { get; init; } = [];
}

/// <summary>Serviço encontrado na pesquisa.</summary>
public sealed class ServiceSearchResult
{
    [JsonPropertyName("serviceId")]
    public string? ServiceId { get; init; }

    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }
}

/// <summary>Detalhe completo de um serviço do catálogo.</summary>
public sealed class ServiceDetail
{
    [JsonPropertyName("serviceId")]
    public string? ServiceId { get; init; }

    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("teamName")]
    public string? TeamName { get; init; }

    [JsonPropertyName("domain")]
    public string? Domain { get; init; }

    [JsonPropertyName("apis")]
    public IReadOnlyList<ApiSummary> Apis { get; init; } = [];
}

/// <summary>Resumo de uma API exposta por um serviço.</summary>
public sealed class ApiSummary
{
    [JsonPropertyName("apiId")]
    public string? ApiId { get; init; }

    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("routePattern")]
    public string? RoutePattern { get; init; }

    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonPropertyName("consumerCount")]
    public int ConsumerCount { get; init; }
}

/// <summary>Resposta da listagem de contratos de um serviço.</summary>
public sealed class ServiceContractsResponse
{
    [JsonPropertyName("contracts")]
    public IReadOnlyList<ServiceContractItem> Contracts { get; init; } = [];

    [JsonPropertyName("items")]
    public IReadOnlyList<ServiceContractItem> Items { get; init; } = [];
}

/// <summary>Contrato associado a um serviço.</summary>
public sealed class ServiceContractItem
{
    [JsonPropertyName("versionId")]
    public string? VersionId { get; init; }

    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("apiAssetId")]
    public string? ApiAssetId { get; init; }

    [JsonPropertyName("apiName")]
    public string? ApiName { get; init; }

    [JsonPropertyName("apiRoutePattern")]
    public string? ApiRoutePattern { get; init; }

    [JsonPropertyName("semVer")]
    public string? SemVer { get; init; }

    [JsonPropertyName("protocol")]
    public string? Protocol { get; init; }

    [JsonPropertyName("lifecycleState")]
    public string? LifecycleState { get; init; }
}

/// <summary>Detalhe de uma versão de contrato.</summary>
public sealed class ContractDetail
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("apiAssetId")]
    public string? ApiAssetId { get; init; }

    [JsonPropertyName("semVer")]
    public string? SemVer { get; init; }

    [JsonPropertyName("specContent")]
    public string? SpecContent { get; init; }

    [JsonPropertyName("protocol")]
    public string? Protocol { get; init; }

    [JsonPropertyName("lifecycleState")]
    public string? LifecycleState { get; init; }

    [JsonPropertyName("serviceName")]
    public string? ServiceName { get; init; }
}

/// <summary>Request para geração de código a partir de um contrato.</summary>
public sealed class GenerateCodeRequest
{
    [JsonPropertyName("specContent")]
    public string SpecContent { get; set; } = string.Empty;

    [JsonPropertyName("serviceName")]
    public string ServiceName { get; set; } = string.Empty;

    [JsonPropertyName("rootNamespace")]
    public string? RootNamespace { get; set; }
}

/// <summary>Resultado da geração de código a partir de um contrato.</summary>
public sealed class GeneratedCodeResult
{
    [JsonPropertyName("serviceName")]
    public string? ServiceName { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("schemaCount")]
    public int SchemaCount { get; init; }

    [JsonPropertyName("operationCount")]
    public int OperationCount { get; init; }

    [JsonPropertyName("files")]
    public IReadOnlyList<GeneratedCodeFile> Files { get; init; } = [];
}

/// <summary>Ficheiro de código gerado.</summary>
public sealed class GeneratedCodeFile
{
    [JsonPropertyName("path")]
    public string? Path { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }
}

/// <summary>Request para registo de uma relação de consumo.</summary>
public sealed class RegisterConsumerRequest
{
    [JsonPropertyName("apiAssetId")]
    public string ApiAssetId { get; set; } = string.Empty;

    [JsonPropertyName("consumerName")]
    public string ConsumerName { get; set; } = string.Empty;

    [JsonPropertyName("consumerKind")]
    public string ConsumerKind { get; set; } = "Service";

    [JsonPropertyName("consumerEnvironment")]
    public string ConsumerEnvironment { get; set; } = "Production";

    [JsonPropertyName("sourceType")]
    public string SourceType { get; set; } = "sdk";

    [JsonPropertyName("externalReference")]
    public string ExternalReference { get; set; } = string.Empty;

    [JsonPropertyName("confidenceScore")]
    public decimal ConfidenceScore { get; set; } = 0.95m;
}

/// <summary>Relação de consumo registada.</summary>
public sealed class ConsumerRelationship
{
    [JsonPropertyName("relationshipId")]
    public string? RelationshipId { get; init; }

    [JsonPropertyName("apiAssetId")]
    public string? ApiAssetId { get; init; }

    [JsonPropertyName("consumerName")]
    public string? ConsumerName { get; init; }

    [JsonPropertyName("sourceType")]
    public string? SourceType { get; init; }

    [JsonPropertyName("confidenceScore")]
    public decimal ConfidenceScore { get; init; }
}

/// <summary>Request para geração orquestrada de cliente consumidor.</summary>
public sealed class GenerateConsumerClientRequest
{
    /// <summary>Nome canónico do serviço provider.</summary>
    [JsonPropertyName("providerName")]
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>Nome canónico do serviço consumidor.</summary>
    [JsonPropertyName("consumerName")]
    public string ConsumerName { get; set; } = string.Empty;

    /// <summary>Namespace raiz opcional para o código gerado.</summary>
    [JsonPropertyName("rootNamespace")]
    public string? RootNamespace { get; set; }

    /// <summary>Rotas ou operationIds a filtrar (opcional).</summary>
    [JsonPropertyName("routes")]
    public IReadOnlyList<string>? Routes { get; set; }
}

/// <summary>Resultado da geração orquestrada de cliente consumidor.</summary>
public sealed class ConsumerClientGenerationResult
{
    [JsonPropertyName("providerName")]
    public string? ProviderName { get; init; }

    [JsonPropertyName("providerServiceId")]
    public string? ProviderServiceId { get; init; }

    [JsonPropertyName("consumerName")]
    public string? ConsumerName { get; init; }

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("generatedContracts")]
    public IReadOnlyList<GeneratedContractClient> GeneratedContracts { get; init; } = [];

    [JsonPropertyName("totalFiles")]
    public int TotalFiles { get; init; }

    [JsonPropertyName("totalOperations")]
    public int TotalOperations { get; init; }
}

/// <summary>Cliente gerado para um contrato específico.</summary>
public sealed class GeneratedContractClient
{
    [JsonPropertyName("contractVersionId")]
    public string? ContractVersionId { get; init; }

    [JsonPropertyName("apiAssetId")]
    public string? ApiAssetId { get; init; }

    [JsonPropertyName("apiName")]
    public string? ApiName { get; init; }

    [JsonPropertyName("semVer")]
    public string? SemVer { get; init; }

    [JsonPropertyName("serviceName")]
    public string? ServiceName { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("schemaCount")]
    public int SchemaCount { get; init; }

    [JsonPropertyName("operationCount")]
    public int OperationCount { get; init; }

    [JsonPropertyName("files")]
    public IReadOnlyList<GeneratedCodeFile> Files { get; init; } = [];
}

/// <summary>Análise de impacto (blast-radius) de um nó do catálogo.</summary>
public sealed class ImpactAnalysis
{
    [JsonPropertyName("rootNodeId")]
    public string? RootNodeId { get; init; }

    [JsonPropertyName("affectedNodes")]
    public IReadOnlyList<ImpactNode> AffectedNodes { get; init; } = [];

    [JsonPropertyName("totalAffected")]
    public int TotalAffected { get; init; }
}

/// <summary>Nó afectado por uma análise de impacto.</summary>
public sealed class ImpactNode
{
    [JsonPropertyName("nodeId")]
    public string? NodeId { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("kind")]
    public string? Kind { get; init; }

    [JsonPropertyName("depth")]
    public int Depth { get; init; }
}
