using System.Text.Json;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.SendAssistantMessage;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Services;

/// <summary>
/// Implementação do serviço de resolução de contexto de grounding.
/// Extrai e encapsula a lógica de classificação de use case, resolução de fontes,
/// construção de contexto estruturado e augmentação via retrieval services.
/// Inclui busca em fontes de dados externas (web search, GitHub, etc.) via IDataSourceSyncService.
/// </summary>
public sealed class ContextGroundingService(
    IDocumentRetrievalService documentRetrievalService,
    IDatabaseRetrievalService databaseRetrievalService,
    ITelemetryRetrievalService telemetryRetrievalService,
    IExternalDataSourceRepository externalDataSourceRepository,
    IDataSourceSyncService dataSourceSyncService,
    ILogger<ContextGroundingService> logger) : IContextGroundingService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<GroundingResolutionResult> ResolveGroundingAsync(
        string query,
        string persona,
        string? contextScope,
        Guid? serviceId,
        Guid? contractId,
        Guid? incidentId,
        Guid? changeId,
        Guid? teamId,
        Guid? domainId,
        string? contextBundleJson,
        IReadOnlyList<AIKnowledgeSource> availableSources,
        CancellationToken cancellationToken = default)
    {
        var useCaseType = ClassifyUseCase(query, contextScope);
        var groundingSources = ResolveGroundingSources(contextScope, availableSources, useCaseType);
        var (sourceWeightingSummary, confidenceLevel) = EvaluateSourceWeights(availableSources, useCaseType);

        // Parse context bundle if present
        ContextBundleData? contextBundle = null;
        var bundleParseError = false;
        if (!string.IsNullOrWhiteSpace(contextBundleJson))
        {
            try
            {
                contextBundle = JsonSerializer.Deserialize<ContextBundleData>(contextBundleJson, JsonOptions);
            }
            catch (JsonException)
            {
                bundleParseError = true;
            }
        }

        // Generate grounded response from bundle if available
        string? contextSummary = null;
        List<string>? suggestedSteps = null;
        List<string>? caveats = null;
        var contextStrength = "none";

        if (contextBundle is not null)
        {
            (_, contextSummary, suggestedSteps, caveats, contextStrength) =
                GenerateGroundedResponse(query, persona, useCaseType, groundingSources, contextBundle);
        }

        if (bundleParseError)
        {
            caveats ??= [];
            caveats.Add("Context bundle could not be parsed; response may lack entity-specific detail.");
        }

        // Build base grounding context
        var baseContext = BuildGroundingContext(
            query, persona, useCaseType, contextScope,
            serviceId, contractId, incidentId, changeId, teamId, domainId,
            groundingSources, contextSummary, contextBundle);

        // Augment with retrieval services
        var groundingContext = await AugmentWithRetrievalAsync(
            baseContext, query, contextScope, useCaseType, cancellationToken);

        var systemPrompt = BuildAssistantSystemPrompt(persona, contextScope, groundingContext);

        return new GroundingResolutionResult(
            groundingContext,
            systemPrompt,
            groundingSources,
            contextSummary,
            suggestedSteps,
            caveats,
            contextStrength,
            confidenceLevel,
            sourceWeightingSummary,
            useCaseType);
    }

    // ── Private methods extracted from SendAssistantMessage ──────────────────

    private async Task<string> AugmentWithRetrievalAsync(
        string baseContext,
        string query,
        string? contextScope,
        AIUseCaseType useCaseType,
        CancellationToken cancellationToken)
    {
        var augmentation = new List<string>();

        var entityFilter = useCaseType switch
        {
            AIUseCaseType.ContractExplanation or AIUseCaseType.ContractGeneration => "Contract",
            AIUseCaseType.ServiceLookup or AIUseCaseType.DependencyReasoning => "Service",
            _ => (string?)null
        };

        bool includeTelemetry = useCaseType is AIUseCaseType.IncidentExplanation or AIUseCaseType.MitigationGuidance
            or AIUseCaseType.ChangeAnalysis or AIUseCaseType.FinOpsExplanation;

        var docTask = documentRetrievalService.SearchAsync(
            new DocumentSearchRequest(query, MaxResults: 3), cancellationToken);
        var dbTask = databaseRetrievalService.SearchAsync(
            new DatabaseSearchRequest(query, EntityType: entityFilter, MaxResults: 3), cancellationToken);

        if (includeTelemetry)
        {
            var telTask = telemetryRetrievalService.SearchAsync(
                new TelemetrySearchRequest(query, MaxResults: 5), cancellationToken);

            await Task.WhenAll(
                docTask.ContinueWith(_ => { }, CancellationToken.None),
                dbTask.ContinueWith(_ => { }, CancellationToken.None),
                telTask.ContinueWith(_ => { }, CancellationToken.None));

            try
            {
                var docResult = await docTask;
                if (docResult.Success && docResult.Hits.Count > 0)
                {
                    augmentation.Add("RetrievedDocuments:");
                    foreach (var hit in docResult.Hits)
                        augmentation.Add($"  - [{hit.Classification}] {hit.Title}: {hit.Snippet}");
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Document retrieval augmentation failed — continuing without");
            }

            try
            {
                var dbResult = await dbTask;
                if (dbResult.Success && dbResult.Hits.Count > 0)
                {
                    augmentation.Add("RetrievedData:");
                    foreach (var hit in dbResult.Hits)
                        augmentation.Add($"  - [{hit.EntityType}] {hit.DisplayName}: {hit.Summary}");
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Database retrieval augmentation failed — continuing without");
            }

            try
            {
                var telResult = await telTask;
                if (telResult.Success && telResult.Hits.Count > 0)
                {
                    augmentation.Add("RetrievedTelemetry:");
                    foreach (var hit in telResult.Hits)
                        augmentation.Add(
                            $"  - [{hit.Severity}] {hit.ServiceName} @ {hit.Timestamp:u}: {(hit.Message.Length > 120 ? string.Concat(hit.Message.AsSpan(0, 117), "...") : hit.Message)}");
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Telemetry retrieval augmentation failed — continuing without");
            }
        }
        else
        {
            await Task.WhenAll(
                docTask.ContinueWith(_ => { }, CancellationToken.None),
                dbTask.ContinueWith(_ => { }, CancellationToken.None));

            try
            {
                var docResult = await docTask;
                if (docResult.Success && docResult.Hits.Count > 0)
                {
                    augmentation.Add("RetrievedDocuments:");
                    foreach (var hit in docResult.Hits)
                        augmentation.Add($"  - [{hit.Classification}] {hit.Title}: {hit.Snippet}");
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Document retrieval augmentation failed — continuing without");
            }

            try
            {
                var dbResult = await dbTask;
                if (dbResult.Success && dbResult.Hits.Count > 0)
                {
                    augmentation.Add("RetrievedData:");
                    foreach (var hit in dbResult.Hits)
                        augmentation.Add($"  - [{hit.EntityType}] {hit.DisplayName}: {hit.Summary}");
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Database retrieval augmentation failed — continuing without");
            }
        }

        // Augment with active external data sources that support runtime search (e.g. web search APIs).
        // SearchAsync returns empty for indexing-only connectors (GitHub, GitLab, Directory) — safe to call on all.
        try
        {
            var activeSources = await externalDataSourceRepository.ListAsync(
                connectorType: ExternalDataSourceConnectorType.WebSearch, isActive: true, cancellationToken);

            foreach (var source in activeSources.Take(2))
            {
                try
                {
                    var webResults = await dataSourceSyncService.SearchAsync(source, query, maxResults: 5, cancellationToken);
                    if (webResults.Count > 0)
                    {
                        augmentation.Add($"RetrievedFromWeb [{source.Name}]:");
                        foreach (var hit in webResults)
                            augmentation.Add($"  - {hit.Title}: {(hit.Content.Length > 200 ? string.Concat(hit.Content.AsSpan(0, 197), "...") : hit.Content)} ({hit.SourceUrl})");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "External data source '{Name}' search failed — continuing without.", source.Name);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "External data source retrieval augmentation failed — continuing without.");
        }

        return augmentation.Count == 0
            ? baseContext
            : baseContext + "\n\n--- Retrieved Context ---\n" + string.Join("\n", augmentation);
    }

    internal static AIUseCaseType ClassifyUseCase(string query, string? contextScope)
    {
        var lower = query.ToLowerInvariant();
        var scope = contextScope?.ToLowerInvariant() ?? string.Empty;

        if (lower.Contains("contract") && lower.Contains("generat")) return AIUseCaseType.ContractGeneration;
        if (lower.Contains("contract") || scope.Contains("contracts")) return AIUseCaseType.ContractExplanation;
        if (lower.Contains("incident") || scope.Contains("incidents")) return AIUseCaseType.IncidentExplanation;
        if (lower.Contains("mitigat") || lower.Contains("runbook")) return AIUseCaseType.MitigationGuidance;
        if (lower.Contains("change") || lower.Contains("blast") || lower.Contains("deploy")) return AIUseCaseType.ChangeAnalysis;
        if (lower.Contains("summary") || lower.Contains("executive") || lower.Contains("overview")) return AIUseCaseType.ExecutiveSummary;
        if (lower.Contains("risk") || lower.Contains("compliance")) return AIUseCaseType.RiskComplianceExplanation;
        if (lower.Contains("cost") || lower.Contains("finops") || lower.Contains("waste")) return AIUseCaseType.FinOpsExplanation;
        if (lower.Contains("dependency") || lower.Contains("depend")) return AIUseCaseType.DependencyReasoning;
        if (lower.Contains("service") || scope.Contains("services")) return AIUseCaseType.ServiceLookup;
        return AIUseCaseType.General;
    }

    internal static List<string> ResolveGroundingSources(
        string? contextScope, IReadOnlyList<AIKnowledgeSource> availableSources, AIUseCaseType useCaseType)
    {
        if (availableSources.Count > 0)
        {
            var priorities = GetSourcePrioritiesByUseCase(useCaseType);
            var resolved = new List<string>();
            foreach (var sourceType in priorities)
            {
                var source = availableSources.FirstOrDefault(s => s.SourceType == sourceType);
                if (source is not null) resolved.Add(source.Name);
            }
            if (resolved.Count > 0) return resolved;
        }

        if (string.IsNullOrWhiteSpace(contextScope))
            return ["Service Catalog", "Contract Registry"];

        var scopes = contextScope.Split(',', 20, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var sources = scopes.Select(scope => scope.ToLowerInvariant() switch
        {
            "services" => "Service Catalog",
            "contracts" => "Contract Registry",
            "incidents" => "Incident History",
            "changes" => "Change Intelligence",
            "runbooks" => "Runbook Library",
            "dependencies" => "Dependency Graph",
            "reliability" => "Reliability Metrics",
            "governance" => "Governance Policies",
            "policies" => "Access Policies",
            "models" => "Model Registry",
            "audit" => "Audit Trail",
            "compliance" => "Compliance Records",
            "risk" => "Risk Assessment",
            "trends" => "Operational Trends",
            _ => scope
        }).ToList();

        return sources.Count > 0 ? sources : ["Service Catalog", "Contract Registry"];
    }

    internal static List<KnowledgeSourceType> GetSourcePrioritiesByUseCase(AIUseCaseType useCaseType) =>
        useCaseType switch
        {
            AIUseCaseType.ServiceLookup => [KnowledgeSourceType.Service, KnowledgeSourceType.Contract, KnowledgeSourceType.Documentation],
            AIUseCaseType.ContractExplanation => [KnowledgeSourceType.Contract, KnowledgeSourceType.Service, KnowledgeSourceType.SourceOfTruth],
            AIUseCaseType.ContractGeneration => [KnowledgeSourceType.Contract, KnowledgeSourceType.Service, KnowledgeSourceType.SourceOfTruth, KnowledgeSourceType.Documentation],
            AIUseCaseType.IncidentExplanation => [KnowledgeSourceType.Incident, KnowledgeSourceType.Change, KnowledgeSourceType.Runbook, KnowledgeSourceType.TelemetrySummary],
            AIUseCaseType.MitigationGuidance => [KnowledgeSourceType.Runbook, KnowledgeSourceType.Incident, KnowledgeSourceType.Service, KnowledgeSourceType.TelemetrySummary],
            AIUseCaseType.ChangeAnalysis => [KnowledgeSourceType.Change, KnowledgeSourceType.Service, KnowledgeSourceType.Incident, KnowledgeSourceType.TelemetrySummary],
            AIUseCaseType.ExecutiveSummary => [KnowledgeSourceType.SourceOfTruth, KnowledgeSourceType.Service, KnowledgeSourceType.TelemetrySummary],
            AIUseCaseType.RiskComplianceExplanation => [KnowledgeSourceType.SourceOfTruth, KnowledgeSourceType.Service, KnowledgeSourceType.Documentation],
            AIUseCaseType.FinOpsExplanation => [KnowledgeSourceType.TelemetrySummary, KnowledgeSourceType.Service, KnowledgeSourceType.SourceOfTruth],
            AIUseCaseType.DependencyReasoning => [KnowledgeSourceType.Service, KnowledgeSourceType.Contract, KnowledgeSourceType.Change],
            _ => [KnowledgeSourceType.Service, KnowledgeSourceType.Contract, KnowledgeSourceType.SourceOfTruth, KnowledgeSourceType.Documentation]
        };

    internal static (string WeightingSummary, string ConfidenceLevel) EvaluateSourceWeights(
        IReadOnlyList<AIKnowledgeSource> sources, AIUseCaseType useCaseType)
    {
        var priorities = GetSourcePrioritiesByUseCase(useCaseType);
        var matchCount = priorities.Count(p => sources.Any(s => s.SourceType == p));
        var weights = priorities
            .Where(p => sources.Any(s => s.SourceType == p))
            .Select(p => $"{p}:matched")
            .ToList();
        var summary = weights.Count > 0 ? string.Join(",", weights) : "no-matches";
        var confidence = matchCount >= 3 ? AIConfidenceLevel.High.ToString()
            : matchCount >= 2 ? AIConfidenceLevel.Medium.ToString()
            : matchCount >= 1 ? AIConfidenceLevel.Low.ToString()
            : AIConfidenceLevel.Unknown.ToString();
        return (summary, confidence);
    }

    private static string BuildGroundingContext(
        string query,
        string persona,
        AIUseCaseType useCaseType,
        string? contextScope,
        Guid? serviceId, Guid? contractId, Guid? incidentId, Guid? changeId, Guid? teamId, Guid? domainId,
        IReadOnlyList<string> groundingSources,
        string? contextSummary,
        ContextBundleData? contextBundle)
    {
        var lines = new List<string>
        {
            $"Persona: {persona}",
            $"UseCase: {useCaseType}",
            $"ContextScope: {contextScope ?? "general"}",
            $"GroundingSources: {string.Join(", ", groundingSources)}"
        };

        if (!string.IsNullOrWhiteSpace(contextSummary)) lines.Add($"ContextSummary: {contextSummary}");
        if (serviceId.HasValue) lines.Add($"ServiceId: {serviceId.Value}");
        if (contractId.HasValue) lines.Add($"ContractId: {contractId.Value}");
        if (incidentId.HasValue) lines.Add($"IncidentId: {incidentId.Value}");
        if (changeId.HasValue) lines.Add($"ChangeId: {changeId.Value}");
        if (teamId.HasValue) lines.Add($"TeamId: {teamId.Value}");
        if (domainId.HasValue) lines.Add($"DomainId: {domainId.Value}");

        if (contextBundle is not null)
        {
            lines.Add($"EntityType: {contextBundle.EntityType}");
            lines.Add($"EntityName: {contextBundle.EntityName}");
            if (!string.IsNullOrWhiteSpace(contextBundle.EntityStatus))
                lines.Add($"EntityStatus: {contextBundle.EntityStatus}");
            if (!string.IsNullOrWhiteSpace(contextBundle.EntityDescription))
                lines.Add($"EntityDescription: {contextBundle.EntityDescription}");
            if (contextBundle.Properties is { Count: > 0 })
                foreach (var prop in contextBundle.Properties)
                    lines.Add($"Property.{prop.Key}: {prop.Value}");
            if (contextBundle.Relations is { Count: > 0 })
                foreach (var rel in contextBundle.Relations.Take(8))
                    lines.Add($"Relation.{rel.RelationType}: {rel.EntityType}:{rel.Name}" +
                              (string.IsNullOrWhiteSpace(rel.Status) ? string.Empty : $" ({rel.Status})"));
        }

        return string.Join("\n", lines);
    }

    internal static string BuildAssistantSystemPrompt(string persona, string? contextScope, string groundingContext)
    {
        var scope = string.IsNullOrWhiteSpace(contextScope) ? "general" : contextScope;
        return $"""
            You are NexTraceOne AI Assistant — a governed operational intelligence assistant.
            Your role is to help {persona}s with service governance, contract intelligence, change confidence, and operational insights.
            Answer based on the grounding context below. If grounding is incomplete, state limitations explicitly.
            Prioritize operational safety, contract accuracy, and change confidence.
            Do NOT reveal internal system details or grounding metadata in your response.
            Context scope: {scope}

            ## Grounding Context
            {(string.IsNullOrWhiteSpace(groundingContext) ? "No grounding context available." : groundingContext)}
            """;
    }

    private static (string Response, string? ContextSummary, List<string>? Steps, List<string>? Caveats, string Strength)
        GenerateGroundedResponse(
            string message, string persona, AIUseCaseType useCaseType,
            IReadOnlyList<string> groundingSources, ContextBundleData bundle)
    {
        var parts = new List<string>();
        var steps = new List<string>();
        var caveats = new List<string>();
        var contextProps = new List<string>();

        parts.Add($"**{bundle.EntityType}: {bundle.EntityName}**");
        if (!string.IsNullOrWhiteSpace(bundle.EntityStatus))
            parts.Add($"Status: {bundle.EntityStatus}");
        if (!string.IsNullOrWhiteSpace(bundle.EntityDescription))
            parts.Add(bundle.EntityDescription);

        if (bundle.Properties is { Count: > 0 })
        {
            var propLines = bundle.Properties
                .Where(p => !string.IsNullOrWhiteSpace(p.Value))
                .Select(p => $"• {p.Key}: {p.Value}")
                .ToList();
            if (propLines.Count > 0)
            {
                parts.Add("\n" + string.Join("\n", propLines));
                contextProps.AddRange(bundle.Properties.Keys);
            }
        }

        if (bundle.Relations is { Count: > 0 })
        {
            var grouped = bundle.Relations.GroupBy(r => r.RelationType);
            foreach (var group in grouped)
            {
                parts.Add($"\n**{group.Key}:**");
                foreach (var rel in group.Take(5))
                {
                    var relInfo = $"• {rel.Name}";
                    if (!string.IsNullOrWhiteSpace(rel.Status)) relInfo += $" ({rel.Status})";
                    if (rel.Properties is { Count: > 0 })
                    {
                        var extras = rel.Properties.Where(p => !string.IsNullOrWhiteSpace(p.Value))
                            .Take(3).Select(p => $"{p.Key}: {p.Value}");
                        relInfo += " — " + string.Join(", ", extras);
                    }
                    parts.Add(relInfo);
                }
                if (group.Count() > 5) parts.Add($"  ... and {group.Count() - 5} more");
            }
        }

        switch (bundle.EntityType.ToLowerInvariant())
        {
            case "service":
                steps.Add("Review associated contracts for compliance");
                steps.Add("Check dependency health and recent changes");
                steps.Add("Verify operational readiness and monitoring");
                break;
            case "contract":
                steps.Add("Validate compatibility with registered consumers");
                steps.Add("Review version history for breaking changes");
                steps.Add("Verify ownership and governance status");
                break;
            case "change":
                steps.Add("Assess blast radius and affected services");
                steps.Add("Validate evidence completeness before approval");
                steps.Add("Check for correlated incidents post-deployment");
                break;
            case "incident":
                steps.Add("Correlate with recent changes and deployments");
                steps.Add("Review applicable runbooks and mitigation steps");
                steps.Add("Assess service dependencies and blast radius");
                break;
        }

        if (bundle.Caveats is { Count: > 0 }) caveats.AddRange(bundle.Caveats);
        if (bundle.Relations is null || bundle.Relations.Count == 0)
            caveats.Add("Limited cross-entity context available");

        var propCount = bundle.Properties?.Count ?? 0;
        var relCount = bundle.Relations?.Count ?? 0;
        var strength = (propCount, relCount) switch
        {
            ( >= 3, >= 2) => "strong",
            ( >= 2, >= 1) => "good",
            ( >= 1, _) => "partial",
            _ => "weak"
        };

        var contextSummary = $"Consulted: {bundle.EntityType} ({bundle.EntityName})"
            + (contextProps.Count > 0 ? $" with {contextProps.Count} properties" : "")
            + (relCount > 0 ? $", {relCount} related entities" : "")
            + $". Sources: {string.Join(", ", groundingSources.Take(3))}";

        parts.Add($"\n---\n*Grounded in {string.Join(", ", groundingSources.Take(3))}. Context strength: {strength}.*");
        return (string.Join("\n", parts), contextSummary, steps, caveats, strength);
    }
}
