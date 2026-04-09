using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GenerateResilienceReport;

/// <summary>
/// Feature: GenerateResilienceReport — gera e persiste um relatório de resiliência
/// a partir dos resultados de um experimento de chaos engineering.
/// Compara blast radius teórico vs real, regista observações de telemetria
/// e produz um score de resiliência (0–100).
///
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GenerateResilienceReport
{
    /// <summary>Comando para gerar um relatório de resiliência pós-experimento.</summary>
    public sealed record Command(
        Guid ChaosExperimentId,
        string ServiceName,
        string Environment,
        string ExperimentType,
        int ResilienceScore,
        string? TheoreticalBlastRadius,
        string? ActualBlastRadius,
        decimal? BlastRadiusDeviation,
        string? TelemetryObservations,
        decimal? LatencyImpactMs,
        decimal? ErrorRateImpact,
        int? RecoveryTimeSeconds,
        string? Strengths,
        string? Weaknesses,
        string? Recommendations) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando de geração de relatório de resiliência.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ChaosExperimentId).NotEmpty();
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ExperimentType).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ResilienceScore)
                .InclusiveBetween(0, 100)
                .WithMessage("Resilience score must be between 0 and 100.");
        }
    }

    /// <summary>
    /// Handler que gera e persiste o relatório de resiliência.
    /// Cria a entidade ResilienceReport e persiste via IResilienceReportRepository + IUnitOfWork.
    /// </summary>
    public sealed class Handler(
        IResilienceReportRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentTenant currentTenant,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = dateTimeProvider.UtcNow;

            var report = ResilienceReport.Generate(
                chaosExperimentId: request.ChaosExperimentId,
                serviceName: request.ServiceName,
                environment: request.Environment,
                experimentType: request.ExperimentType,
                resilienceScore: request.ResilienceScore,
                theoreticalBlastRadius: request.TheoreticalBlastRadius,
                actualBlastRadius: request.ActualBlastRadius,
                blastRadiusDeviation: request.BlastRadiusDeviation,
                telemetryObservations: request.TelemetryObservations,
                latencyImpactMs: request.LatencyImpactMs,
                errorRateImpact: request.ErrorRateImpact,
                recoveryTimeSeconds: request.RecoveryTimeSeconds,
                strengths: request.Strengths,
                weaknesses: request.Weaknesses,
                recommendations: request.Recommendations,
                tenantId: currentTenant.Id.ToString(),
                generatedAt: now);

            await repository.AddAsync(report, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                report.Id.Value,
                report.ChaosExperimentId,
                report.ServiceName,
                report.Environment,
                report.ExperimentType,
                report.ResilienceScore,
                report.Status.ToString(),
                report.GeneratedAt));
        }
    }

    /// <summary>Resposta com o resumo do relatório de resiliência gerado.</summary>
    public sealed record Response(
        Guid ReportId,
        Guid ChaosExperimentId,
        string ServiceName,
        string Environment,
        string ExperimentType,
        int ResilienceScore,
        string Status,
        DateTimeOffset GeneratedAt);
}
