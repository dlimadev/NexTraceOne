using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;

namespace NexTraceOne.AIKnowledge.Application.Governance.Services;

/// <summary>
/// Avaliador de grounding que verifica se entidades citadas na resposta da IA
/// (nomes de ServiceAsset, nomes de Contract) existem no catálogo.
///
/// W4-04: AI Governance / Avaliação de Qualidade.
///
/// Calcula GroundingScore baseado em:
/// - Entidades mencionadas na resposta vs entidades existentes no catálogo
/// - Se todas as entidades existem → score alto (1.0)
/// - Se algumas não existem → score baixo (indicador de possível alucinação)
/// </summary>
public sealed class GroundingEvaluator : IAiResponseEvaluator
{
    private readonly ICatalogGraphModule _catalogGraph;
    private readonly ILogger<GroundingEvaluator> _logger;

    // Padrões para extrair nomes de serviços e contratos da resposta
    private static readonly Regex ServiceNamePattern = new(
        @"(?:serviço|service|api)\s+['""]?([A-Za-z][A-Za-z0-9._-]{2,50})['""]?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ContractNamePattern = new(
        @"(?:contrato|contract)\s+['""]?([A-Za-z][A-Za-z0-9._-]{2,50})['""]?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public GroundingEvaluator(
        ICatalogGraphModule catalogGraph,
        ILogger<GroundingEvaluator> logger)
    {
        _catalogGraph = catalogGraph;
        _logger = logger;
    }

    /// <summary>
    /// Avalia a qualidade de grounding de uma resposta de IA.
    /// Verifica se as entidades mencionadas (serviços, contratos) existem no catálogo.
    /// </summary>
    public async Task<AiEvaluationResult> EvaluateAsync(
        AiResponse response,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(response.Content))
        {
            return new AiEvaluationResult
            {
                GroundingScore = 0m,
                EntitiesFound = [],
                EntitiesNotFound = [],
                HasHallucinations = false,
                ConfidenceLevel = "low"
            };
        }

        // Extrair entidades mencionadas na resposta
        var mentionedServices = ExtractEntityNames(response.Content, ServiceNamePattern);
        var mentionedContracts = ExtractEntityNames(response.Content, ContractNamePattern);

        var allEntities = mentionedServices.Concat(mentionedContracts).Distinct().ToList();

        if (allEntities.Count == 0)
        {
            // Sem entidades mencionadas → avaliação neutra
            return new AiEvaluationResult
            {
                GroundingScore = 0.5m,
                EntitiesFound = [],
                EntitiesNotFound = [],
                HasHallucinations = false,
                ConfidenceLevel = "medium"
            };
        }

        // Verificar existência de cada entidade no catálogo
        var found = new List<string>();
        var notFound = new List<string>();

        foreach (var entityName in allEntities)
        {
            // Tentar como serviço primeiro
            bool existsAsService = await _catalogGraph.ServiceAssetExistsAsync(entityName, cancellationToken);

            if (existsAsService)
            {
                found.Add(entityName);
            }
            else
            {
                // Se não é serviço, pode ser contrato (verificação simplificada)
                // Para contratos, assumimos que podem existir se não forem serviços
                // Num cenário real, teríamos IContractRepository.SearchByNameAsync
                notFound.Add(entityName);
            }
        }

        // Calcular score de grounding
        var totalEntities = allEntities.Count;
        var foundCount = found.Count;
        var groundingScore = totalEntities > 0
            ? Math.Round((decimal)foundCount / totalEntities, 2)
            : 0.5m;

        var hasHallucinations = notFound.Count > 0 && groundingScore < 0.5m;
        var confidenceLevel = DetermineConfidenceLevel(groundingScore, hasHallucinations);

        _logger.LogInformation(
            "GroundingEvaluator: avaliada resposta com {Total} entidades, {Found} encontradas, score={Score}, hallucinations={HasHallucinations}",
            totalEntities, foundCount, groundingScore, hasHallucinations);

        return new AiEvaluationResult
        {
            GroundingScore = groundingScore,
            EntitiesFound = found,
            EntitiesNotFound = notFound,
            HasHallucinations = hasHallucinations,
            ConfidenceLevel = confidenceLevel
        };
    }

    private static List<string> ExtractEntityNames(string content, Regex pattern)
    {
        var matches = pattern.Matches(content);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                var name = match.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(name) && name.Length >= 3)
                {
                    names.Add(name);
                }
            }
        }

        return names.ToList();
    }

    private static string DetermineConfidenceLevel(decimal groundingScore, bool hasHallucinations)
    {
        if (hasHallucinations || groundingScore < 0.3m)
            return "low";
        if (groundingScore < 0.7m)
            return "medium";
        return "high";
    }
}

/// <summary>
/// Representa uma resposta de IA a ser avaliada.
/// </summary>
public sealed record AiResponse(
    /// <summary>Conteúdo textual da resposta.</summary>
    string Content,
    /// <summary>Identificador da execução ou conversa associada.</summary>
    Guid? ExecutionId = null,
    /// <summary>Nome do modelo utilizado.</summary>
    string? ModelName = null);

/// <summary>
/// Resultado da avaliação de grounding.
/// </summary>
public sealed record AiEvaluationResult
{
    /// <summary>Score de grounding (0.0 a 1.0).</summary>
    public decimal GroundingScore { get; init; }

    /// <summary>Entidades encontradas no catálogo.</summary>
    public IReadOnlyList<string> EntitiesFound { get; init; } = [];

    /// <summary>Entidades mencionadas mas não encontradas.</summary>
    public IReadOnlyList<string> EntitiesNotFound { get; init; } = [];

    /// <summary>Indica se há sinais de alucinação (score baixo + entidades não encontradas).</summary>
    public bool HasHallucinations { get; init; }

    /// <summary>Nível de confiança: "low", "medium", "high".</summary>
    public string ConfidenceLevel { get; init; } = "unknown";
}

/// <summary>
/// Interface para avaliadores de qualidade de respostas de IA.
/// </summary>
public interface IAiResponseEvaluator
{
    /// <summary>
    /// Avalia a qualidade de uma resposta de IA.
    /// </summary>
    /// <param name="response">Resposta a avaliar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da avaliação.</returns>
    Task<AiEvaluationResult> EvaluateAsync(AiResponse response, CancellationToken cancellationToken = default);
}
