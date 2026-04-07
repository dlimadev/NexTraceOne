using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.EvaluateChangeAdvisoryBoard;

/// <summary>
/// Feature: EvaluateChangeAdvisoryBoard — avalia se uma mudança requer aprovação
/// de um Change Advisory Board (CAB) formal antes do deploy.
/// Consulta parâmetros:
///   - governance.change_advisory_board.enabled
///   - governance.change_advisory_board.members
///   - governance.change_advisory_board.trigger_conditions
/// Segue melhores práticas ITIL para gestão de mudanças.
/// </summary>
public static class EvaluateChangeAdvisoryBoard
{
    /// <summary>Query para avaliar se uma mudança requer aprovação CAB.</summary>
    public sealed record Query(
        string ServiceName,
        string Environment,
        string Criticality,
        string BlastRadius) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Criticality).NotEmpty().MaximumLength(50);
            RuleFor(x => x.BlastRadius).NotEmpty().MaximumLength(50);
        }
    }

    /// <summary>Handler que avalia a necessidade de CAB.</summary>
    public sealed class Handler(
        IConfigurationResolutionService configService,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Check if CAB is enabled
            var enabledConfig = await configService.ResolveEffectiveValueAsync(
                "governance.change_advisory_board.enabled",
                ConfigurationScope.Tenant,
                null,
                cancellationToken);

            var isEnabled = enabledConfig?.EffectiveValue == "true";

            if (!isEnabled)
            {
                return new Response(
                    CabRequired: false,
                    IsApproved: true,
                    Reason: "Change Advisory Board is not enabled",
                    TriggerConditions: [],
                    Members: [],
                    EvaluatedAt: dateTimeProvider.UtcNow);
            }

            // Evaluate trigger conditions
            var triggerConfig = await configService.ResolveEffectiveValueAsync(
                "governance.change_advisory_board.trigger_conditions",
                ConfigurationScope.Tenant,
                null,
                cancellationToken);

            var triggerJson = triggerConfig?.EffectiveValue ??
                """{"min_criticality": "High", "min_blast_radius": "Medium", "environment": ["production"]}""";

            var triggers = new List<string>();
            var cabTriggered = false;

            // Check environment trigger
            if (triggerJson.Contains($"\"{request.Environment}\"", StringComparison.OrdinalIgnoreCase) ||
                triggerJson.Contains("production", StringComparison.OrdinalIgnoreCase) &&
                request.Environment.Equals("production", StringComparison.OrdinalIgnoreCase))
            {
                triggers.Add($"Environment '{request.Environment}' is in CAB-required list");
                cabTriggered = true;
            }

            // Check criticality trigger
            var criticalityLevels = new[] { "Low", "Medium", "High", "Critical" };
            var requestCritIdx = Array.FindIndex(criticalityLevels, c => c.Equals(request.Criticality, StringComparison.OrdinalIgnoreCase));
            if (triggerJson.Contains("\"min_criticality\"", StringComparison.OrdinalIgnoreCase) && requestCritIdx >= 2) // High or Critical
            {
                triggers.Add($"Criticality '{request.Criticality}' meets or exceeds minimum threshold");
                cabTriggered = true;
            }

            // Check blast radius trigger
            var blastLevels = new[] { "None", "Low", "Medium", "High", "Critical" };
            var requestBlastIdx = Array.FindIndex(blastLevels, b => b.Equals(request.BlastRadius, StringComparison.OrdinalIgnoreCase));
            if (triggerJson.Contains("\"min_blast_radius\"", StringComparison.OrdinalIgnoreCase) && requestBlastIdx >= 2) // Medium+
            {
                triggers.Add($"Blast radius '{request.BlastRadius}' meets or exceeds minimum threshold");
                cabTriggered = true;
            }

            if (!cabTriggered)
            {
                return new Response(
                    CabRequired: false,
                    IsApproved: true,
                    Reason: "Change does not meet CAB trigger conditions",
                    TriggerConditions: triggers,
                    Members: [],
                    EvaluatedAt: dateTimeProvider.UtcNow);
            }

            // Get CAB members
            var membersConfig = await configService.ResolveEffectiveValueAsync(
                "governance.change_advisory_board.members",
                ConfigurationScope.Tenant,
                null,
                cancellationToken);

            var membersJson = membersConfig?.EffectiveValue ?? "[]";

            return new Response(
                CabRequired: true,
                IsApproved: false,
                Reason: $"Change requires CAB approval due to: {string.Join("; ", triggers)}",
                TriggerConditions: triggers,
                Members: [membersJson],
                EvaluatedAt: dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta da avaliação de necessidade de CAB.</summary>
    public sealed record Response(
        bool CabRequired,
        bool IsApproved,
        string Reason,
        List<string> TriggerConditions,
        List<string> Members,
        DateTimeOffset EvaluatedAt);
}
