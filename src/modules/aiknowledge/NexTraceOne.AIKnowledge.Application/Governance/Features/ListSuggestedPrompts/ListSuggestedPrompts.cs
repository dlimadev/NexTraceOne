using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AiGovernance.Application.Features.ListSuggestedPrompts;

/// <summary>
/// Feature: ListSuggestedPrompts — lista prompts sugeridos por persona e categoria.
/// Fornece sugestões contextuais ao assistente de IA para cada perfil funcional.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
///
/// Nota: prompts sugeridos são definidos em memória (code-driven) nesta fase.
/// Evolução futura: persistir prompts sugeridos no DB com gestão administrativa.
/// </summary>
public static class ListSuggestedPrompts
{
    /// <summary>Query de listagem de prompts sugeridos com filtros opcionais.</summary>
    public sealed record Query(
        string? Persona,
        string? Category) : IQuery<Response>;

    /// <summary>Handler que retorna prompts sugeridos filtrados por persona e categoria.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var allPrompts = GetAllPrompts();

            var filtered = allPrompts.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.Persona))
                filtered = filtered.Where(p =>
                    p.Personas.Contains(request.Persona, StringComparer.OrdinalIgnoreCase) ||
                    p.Personas.Contains("all", StringComparer.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(request.Category))
                filtered = filtered.Where(p =>
                    p.Category.Equals(request.Category, StringComparison.OrdinalIgnoreCase));

            var items = filtered.ToList();
            return Task.FromResult(Result<Response>.Success(new Response(items, items.Count)));
        }

        /// <summary>
        /// Retorna prompts sugeridos por persona, categoria e escopo.
        /// Fonte de verdade para sugestões contextuais do assistente de IA.
        /// </summary>
        private static List<SuggestedPromptItem> GetAllPrompts() =>
        [
            // ── Operations ─────────────────────────────────────────────────
            new("What issues are affecting the payment API?", "operations",
                ["Engineer", "TechLead"], "serviceId", "high"),
            new("Was there a recent change correlated with this incident?", "operations",
                ["Engineer", "TechLead", "Architect"], "incidentId", "high"),
            new("Which services are degraded for this team?", "operations",
                ["TechLead", "PlatformAdmin"], "teamId", "medium"),
            new("Is there a runbook for this problem?", "operations",
                ["Engineer", "TechLead"], "incidentId", "medium"),
            new("What is the probable impact of this change?", "operations",
                ["Engineer", "TechLead", "Architect"], "changeId", "high"),

            // ── Engineering ────────────────────────────────────────────────
            new("What is the current contract for this endpoint?", "engineering",
                ["Engineer", "Architect"], "contractId", "high"),
            new("Has this Kafka payload changed recently?", "engineering",
                ["Engineer", "Architect"], "contractId", "medium"),
            new("Are there breaking changes between these versions?", "engineering",
                ["Engineer", "Architect"], "contractId", "high"),
            new("Who consumes this topic?", "engineering",
                ["Engineer", "Architect"], "contractId", "medium"),
            new("What dependencies does this service have?", "engineering",
                ["Engineer", "Architect"], "serviceId", "medium"),
            new("Draft an initial contract for this use case", "engineering",
                ["Engineer", "Architect"], null, "low"),

            // ── Management / Product / Executive ───────────────────────────
            new("Summarize the operational situation for this domain", "management",
                ["Product", "Executive", "TechLead"], "domainId", "medium"),
            new("Which critical services are at risk?", "management",
                ["Product", "Executive"], null, "high"),
            new("What is the trend of changes and incidents?", "management",
                ["Product", "Executive"], null, "medium"),
            new("What governance gaps exist for this team?", "management",
                ["TechLead", "PlatformAdmin", "Auditor"], "teamId", "medium"),
            new("Give me an executive summary of the last week", "management",
                ["Executive"], null, "low"),

            // ── Platform / Governance ──────────────────────────────────────
            new("What is the current state of AI governance policies?", "governance",
                ["PlatformAdmin", "Auditor"], null, "medium"),
            new("Show me AI model usage and token consumption", "governance",
                ["PlatformAdmin", "Auditor"], null, "medium"),
            new("Are there any compliance gaps in recent changes?", "governance",
                ["Auditor"], null, "medium"),
            new("Show me recent approval audit trails", "governance",
                ["Auditor"], null, "medium"),

            // ── Troubleshooting ────────────────────────────────────────────
            new("Help me troubleshoot the latest incident", "troubleshooting",
                ["Engineer", "TechLead"], "incidentId", "high"),
            new("Analyze the blast radius of this change", "troubleshooting",
                ["Engineer", "TechLead", "Architect"], "changeId", "high"),
            new("What mitigation steps are recommended?", "troubleshooting",
                ["Engineer", "TechLead"], "incidentId", "medium"),
        ];
    }

    /// <summary>Resposta da listagem de prompts sugeridos.</summary>
    public sealed record Response(
        IReadOnlyList<SuggestedPromptItem> Items,
        int TotalCount);

    /// <summary>Item de prompt sugerido com contexto e persona.</summary>
    public sealed record SuggestedPromptItem(
        string Prompt,
        string Category,
        List<string> Personas,
        string? ScopeHint,
        string Relevance);
}
