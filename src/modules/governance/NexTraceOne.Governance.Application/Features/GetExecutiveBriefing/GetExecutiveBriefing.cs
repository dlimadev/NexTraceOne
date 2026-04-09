using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.Governance.Domain.Errors;

namespace NexTraceOne.Governance.Application.Features.GetExecutiveBriefing;

/// <summary>
/// Feature: GetExecutiveBriefing — obtém um briefing executivo completo pelo identificador.
/// Retorna todas as secções e metadados do briefing.
///
/// Owner: módulo Governance.
/// Pilar: Operational Intelligence — consulta de briefings executivos.
/// Persona principal: Executive, Tech Lead, Auditor.
/// </summary>
public static class GetExecutiveBriefing
{
    /// <summary>Query para obter um executive briefing pelo identificador.</summary>
    public sealed record Query(Guid BriefingId) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.BriefingId).NotEmpty();
        }
    }

    /// <summary>Handler que obtém o executive briefing completo.</summary>
    public sealed class Handler(
        IExecutiveBriefingRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var briefing = await repository.GetByIdAsync(
                new ExecutiveBriefingId(request.BriefingId), cancellationToken);

            if (briefing is null)
                return GovernanceBriefingErrors.BriefingNotFound(request.BriefingId.ToString());

            return Result<Response>.Success(new Response(
                BriefingId: briefing.Id.Value,
                Title: briefing.Title,
                Status: briefing.Status,
                Frequency: briefing.Frequency,
                PeriodStart: briefing.PeriodStart,
                PeriodEnd: briefing.PeriodEnd,
                ExecutiveSummary: briefing.ExecutiveSummary,
                PlatformStatusSection: briefing.PlatformStatusSection,
                TopIncidentsSection: briefing.TopIncidentsSection,
                TeamPerformanceSection: briefing.TeamPerformanceSection,
                HighRiskChangesSection: briefing.HighRiskChangesSection,
                ComplianceStatusSection: briefing.ComplianceStatusSection,
                CostTrendsSection: briefing.CostTrendsSection,
                ActiveRisksSection: briefing.ActiveRisksSection,
                GeneratedAt: briefing.GeneratedAt,
                GeneratedByAgent: briefing.GeneratedByAgent,
                PublishedAt: briefing.PublishedAt,
                ArchivedAt: briefing.ArchivedAt,
                TenantId: briefing.TenantId));
        }
    }

    /// <summary>Resposta completa de um executive briefing com todas as secções.</summary>
    public sealed record Response(
        Guid BriefingId,
        string Title,
        BriefingStatus Status,
        BriefingFrequency Frequency,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        string? ExecutiveSummary,
        string? PlatformStatusSection,
        string? TopIncidentsSection,
        string? TeamPerformanceSection,
        string? HighRiskChangesSection,
        string? ComplianceStatusSection,
        string? CostTrendsSection,
        string? ActiveRisksSection,
        DateTimeOffset GeneratedAt,
        string GeneratedByAgent,
        DateTimeOffset? PublishedAt,
        DateTimeOffset? ArchivedAt,
        string? TenantId);
}
