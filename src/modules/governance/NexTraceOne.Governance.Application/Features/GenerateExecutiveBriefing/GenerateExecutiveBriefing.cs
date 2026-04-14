using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GenerateExecutiveBriefing;

/// <summary>
/// Feature: GenerateExecutiveBriefing — gera um novo briefing executivo via agente de IA.
/// O briefing é criado em estado Draft com secções estruturadas.
///
/// Owner: módulo Governance.
/// Pilar: Operational Intelligence — comunicação executiva assistida por IA.
/// Persona principal: Executive, Tech Lead.
/// </summary>
public static class GenerateExecutiveBriefing
{
    /// <summary>Comando para gerar um novo executive briefing.</summary>
    public sealed record Command(
        string Title,
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
        string GeneratedByAgent = "executive-briefing-agent",
        string? TenantId = null) : ICommand<Response>;

    /// <summary>Validação do comando de geração de briefing.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Frequency).IsInEnum();
            RuleFor(x => x.PeriodStart).NotEmpty();
            RuleFor(x => x.PeriodEnd).NotEmpty().GreaterThan(x => x.PeriodStart);
            RuleFor(x => x.GeneratedByAgent).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que gera o executive briefing e persiste em estado Draft.</summary>
    public sealed class Handler(
        IExecutiveBriefingRepository repository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;

            var briefing = ExecutiveBriefing.Generate(
                title: request.Title,
                frequency: request.Frequency,
                periodStart: request.PeriodStart,
                periodEnd: request.PeriodEnd,
                executiveSummary: request.ExecutiveSummary,
                platformStatusSection: request.PlatformStatusSection,
                topIncidentsSection: request.TopIncidentsSection,
                teamPerformanceSection: request.TeamPerformanceSection,
                highRiskChangesSection: request.HighRiskChangesSection,
                complianceStatusSection: request.ComplianceStatusSection,
                costTrendsSection: request.CostTrendsSection,
                activeRisksSection: request.ActiveRisksSection,
                generatedByAgent: request.GeneratedByAgent,
                tenantId: request.TenantId,
                now: now);

            await repository.AddAsync(briefing, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                BriefingId: briefing.Id.Value,
                Title: briefing.Title,
                Status: briefing.Status,
                Frequency: briefing.Frequency,
                PeriodStart: briefing.PeriodStart,
                PeriodEnd: briefing.PeriodEnd,
                GeneratedAt: briefing.GeneratedAt,
                GeneratedByAgent: briefing.GeneratedByAgent));
        }
    }

    /// <summary>Resposta com o resultado da geração do briefing executivo.</summary>
    public sealed record Response(
        Guid BriefingId,
        string Title,
        BriefingStatus Status,
        BriefingFrequency Frequency,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        DateTimeOffset GeneratedAt,
        string GeneratedByAgent);
}
