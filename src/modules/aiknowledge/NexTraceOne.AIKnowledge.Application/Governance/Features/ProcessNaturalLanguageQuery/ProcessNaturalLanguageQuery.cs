using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ProcessNaturalLanguageQuery;

/// <summary>
/// Feature: ProcessNaturalLanguageQuery — classifica a intenção de uma query e enriquece o contexto.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class ProcessNaturalLanguageQuery
{
    public sealed record Command(
        string Query,
        Guid TenantId,
        string UserId,
        string? ConversationId) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Query).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    public sealed class Handler(
        ISkillRegistry skillRegistry,
        ISkillContextInjector contextInjector) : ICommandHandler<Command, Response>
    {
        private static readonly string[] OperationsKeywords = ["incident", "health", "error", "down", "slow", "latency", "outage"];
        private static readonly string[] ArchitectureKeywords = ["depend", "service", "contract", "api", "topology", "architecture"];
        private static readonly string[] BusinessKeywords = ["cost", "token", "team", "budget", "spend", "deploy", "change"];

        public async Task<Result<Response>> Handle(Command request, CancellationToken ct)
        {
            var queryLower = request.Query.ToLowerInvariant();
            var intent = ClassifyIntent(queryLower);

            var skillName = intent switch
            {
                "operations" => "incident-triage",
                "architecture" => "architecture-fitness",
                "business" => "tech-debt-quantifier",
                _ => "service-health-diagnosis"
            };

            var isAvailable = await skillRegistry.IsSkillAvailableAsync(skillName, request.TenantId, ct);
            var resolvedSkill = isAvailable ? skillName : null;

            var enrichedQuery = resolvedSkill != null
                ? await contextInjector.BuildSkillsSummaryBlockAsync(request.TenantId, ct) + "\n\nUser Query: " + request.Query
                : request.Query;

            var agentName = intent switch
            {
                "operations" => "incident-responder",
                "architecture" => "architecture-fitness-agent",
                _ => "service-health-diagnosis-agent"
            };

            return new Response(
                Guid.NewGuid(),
                request.Query,
                intent,
                resolvedSkill,
                agentName,
                enrichedQuery,
                DateTimeOffset.UtcNow);
        }

        private static string ClassifyIntent(string query)
        {
            if (OperationsKeywords.Any(k => query.Contains(k))) return "operations";
            if (ArchitectureKeywords.Any(k => query.Contains(k))) return "architecture";
            if (BusinessKeywords.Any(k => query.Contains(k))) return "business";
            return "general";
        }
    }

    public sealed record Response(
        Guid QueryId,
        string OriginalQuery,
        string Intent,
        string? SkillSelected,
        string AgentName,
        string EnrichedContext,
        DateTimeOffset ProcessedAt);
}
