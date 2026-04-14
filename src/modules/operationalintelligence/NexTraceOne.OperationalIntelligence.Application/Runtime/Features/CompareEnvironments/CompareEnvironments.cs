using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.CompareEnvironments;

/// <summary>
/// Feature: CompareEnvironments — compara o runtime do mesmo serviço entre dois ambientes distintos.
/// Típica utilização: comparar staging vs production para detectar divergências antes de promover uma mudança.
///
/// Pipeline:
/// 1. Obtém o snapshot mais recente do ambiente de referência (sourceEnvironment).
/// 2. Obtém o snapshot mais recente do ambiente de comparação (targetEnvironment).
/// 3. Calcula desvios percentuais de cada métrica: AvgLatencyMs, P99LatencyMs, ErrorRate, RequestsPerSecond.
/// 4. Para cada métrica que supera o limiar de tolerância, persiste um DriftFinding.
/// 5. Retorna a comparação completa com lista de desvios encontrados.
///
/// Os DriftFinding gerados têm Environment = targetEnvironment para que possam ser listados
/// por ambiente alvo. O campo ReleaseId pode ser informado opcionalmente.
///
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class CompareEnvironments
{
    /// <summary>Comando para comparar dois ambientes do mesmo serviço e persistir drift findings.</summary>
    public sealed record Command(
        string ServiceName,
        string SourceEnvironment,
        string TargetEnvironment,
        decimal TolerancePercent = 20m,
        Guid? ReleaseId = null) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.SourceEnvironment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.TargetEnvironment).NotEmpty().MaximumLength(100);
            RuleFor(x => x).Must(x => !string.Equals(x.SourceEnvironment, x.TargetEnvironment, StringComparison.OrdinalIgnoreCase))
                .WithMessage("Source and target environments must be different.");
            RuleFor(x => x.TolerancePercent).GreaterThan(0m)
                .WithMessage("Tolerance percentage must be greater than zero.");
        }
    }

    public sealed class Handler(
        IRuntimeSnapshotRepository snapshotRepository,
        IDriftFindingRepository driftFindingRepository,
        IRuntimeIntelligenceUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var sourceSnapshot = await snapshotRepository.GetLatestByServiceAsync(
                request.ServiceName,
                request.SourceEnvironment,
                cancellationToken);

            if (sourceSnapshot is null)
                return RuntimeIntelligenceErrors.SnapshotNotFound($"{request.ServiceName}/{request.SourceEnvironment}");

            var targetSnapshot = await snapshotRepository.GetLatestByServiceAsync(
                request.ServiceName,
                request.TargetEnvironment,
                cancellationToken);

            if (targetSnapshot is null)
                return RuntimeIntelligenceErrors.SnapshotNotFound($"{request.ServiceName}/{request.TargetEnvironment}");

            var now = dateTimeProvider.UtcNow;
            var factor = request.TolerancePercent / 100m;
            var deviations = targetSnapshot.CalculateDeviationsFrom(BuildSyntheticBaseline(sourceSnapshot));
            var findings = new List<EnvironmentDeviationItem>();

            foreach (var (metric, deviationPercent) in deviations)
            {
                var expectedValue = GetMetricValue(sourceSnapshot, metric);
                var actualValue = GetMetricValue(targetSnapshot, metric);

                var withinTolerance = expectedValue == 0m
                    ? actualValue <= factor
                    : Math.Abs(actualValue - expectedValue) / expectedValue <= factor;

                if (!withinTolerance)
                {
                    var finding = DriftFinding.Detect(
                        request.ServiceName,
                        request.TargetEnvironment,
                        metric,
                        expectedValue,
                        actualValue,
                        now,
                        request.ReleaseId);

                    driftFindingRepository.Add(finding);

                    findings.Add(new EnvironmentDeviationItem(
                        finding.Id.Value,
                        metric,
                        expectedValue,
                        actualValue,
                        deviationPercent,
                        finding.Severity.ToString()));
                }
                else
                {
                    findings.Add(new EnvironmentDeviationItem(
                        null,
                        metric,
                        expectedValue,
                        actualValue,
                        deviationPercent,
                        "None"));
                }
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                request.ServiceName,
                request.SourceEnvironment,
                request.TargetEnvironment,
                sourceSnapshot.Id.Value,
                targetSnapshot.Id.Value,
                request.TolerancePercent,
                sourceSnapshot.HealthStatus.ToString(),
                targetSnapshot.HealthStatus.ToString(),
                findings,
                findings.Any(f => f.FindingId.HasValue)));
        }

        /// <summary>Constrói uma baseline sintética a partir de um snapshot de referência.</summary>
        private static RuntimeBaseline BuildSyntheticBaseline(RuntimeSnapshot source)
            => RuntimeBaseline.Establish(
                source.ServiceName,
                source.Environment,
                source.AvgLatencyMs,
                source.P99LatencyMs,
                source.ErrorRate,
                source.RequestsPerSecond,
                source.CapturedAt,
                dataPointCount: 1,
                confidenceScore: 0.5m);

        private static decimal GetMetricValue(RuntimeSnapshot snapshot, string metricName)
            => metricName switch
            {
                "AvgLatencyMs" => snapshot.AvgLatencyMs,
                "P99LatencyMs" => snapshot.P99LatencyMs,
                "ErrorRate" => snapshot.ErrorRate,
                "RequestsPerSecond" => snapshot.RequestsPerSecond,
                _ => 0m
            };
    }

    public sealed record EnvironmentDeviationItem(
        Guid? FindingId,
        string MetricName,
        decimal SourceValue,
        decimal TargetValue,
        decimal DeviationPercent,
        string DriftSeverity);

    public sealed record Response(
        string ServiceName,
        string SourceEnvironment,
        string TargetEnvironment,
        Guid SourceSnapshotId,
        Guid TargetSnapshotId,
        decimal TolerancePercent,
        string SourceHealthStatus,
        string TargetHealthStatus,
        IReadOnlyList<EnvironmentDeviationItem> Deviations,
        bool HasDrift);
}
