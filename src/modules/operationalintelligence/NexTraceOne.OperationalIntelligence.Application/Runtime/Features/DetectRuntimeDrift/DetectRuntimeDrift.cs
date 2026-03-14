using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.RuntimeIntelligence.Application.Abstractions;
using NexTraceOne.RuntimeIntelligence.Domain.Entities;
using NexTraceOne.RuntimeIntelligence.Domain.Errors;

namespace NexTraceOne.RuntimeIntelligence.Application.Features.DetectRuntimeDrift;

/// <summary>
/// Feature: DetectRuntimeDrift — compara o snapshot mais recente de um serviço com sua baseline
/// para detectar desvios (drifts) em métricas operacionais.
/// Para cada métrica que excede a tolerância configurada, cria um DriftFinding com severidade
/// calculada automaticamente pelo percentual de desvio (Low/Medium/High/Critical).
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class DetectRuntimeDrift
{
    /// <summary>Comando para detectar drift entre o snapshot mais recente e a baseline de um serviço.</summary>
    public sealed record Command(
        string ServiceName,
        string Environment,
        decimal TolerancePercent,
        Guid? ReleaseId = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de detecção de drift.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.TolerancePercent).GreaterThan(0)
                .WithMessage("Tolerance percentage must be greater than zero.");
        }
    }

    /// <summary>
    /// Handler que compara o snapshot mais recente com a baseline do serviço.
    /// Para cada métrica (latência, erro, throughput) que exceda a tolerância,
    /// cria um DriftFinding via factory method do domínio e persiste.
    /// </summary>
    public sealed class Handler(
        IRuntimeSnapshotRepository snapshotRepository,
        IRuntimeBaselineRepository baselineRepository,
        IDriftFindingRepository driftFindingRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var baseline = await baselineRepository.GetByServiceAndEnvironmentAsync(
                request.ServiceName,
                request.Environment,
                cancellationToken);

            if (baseline is null)
                return RuntimeIntelligenceErrors.BaselineNotFound($"{request.ServiceName}/{request.Environment}");

            var snapshot = await snapshotRepository.GetLatestByServiceAsync(
                request.ServiceName,
                request.Environment,
                cancellationToken);

            if (snapshot is null)
                return RuntimeIntelligenceErrors.SnapshotNotFound($"{request.ServiceName}/{request.Environment}");

            var now = dateTimeProvider.UtcNow;
            var factor = request.TolerancePercent / 100m;
            var findings = new List<DriftFindingItem>();

            // Compara cada métrica individualmente e cria findings para desvios além da tolerância
            CheckAndAddDrift("AvgLatencyMs", baseline.ExpectedAvgLatencyMs, snapshot.AvgLatencyMs, factor, now, request, findings);
            CheckAndAddDrift("P99LatencyMs", baseline.ExpectedP99LatencyMs, snapshot.P99LatencyMs, factor, now, request, findings);
            CheckAndAddDrift("ErrorRate", baseline.ExpectedErrorRate, snapshot.ErrorRate, factor, now, request, findings);
            CheckAndAddDrift("RequestsPerSecond", baseline.ExpectedRequestsPerSecond, snapshot.RequestsPerSecond, factor, now, request, findings);

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                request.ServiceName,
                request.Environment,
                snapshot.Id.Value,
                baseline.Id.Value,
                request.TolerancePercent,
                findings,
                findings.Count > 0);
        }

        /// <summary>
        /// Verifica se uma métrica excede a tolerância e, se sim, cria e persiste um DriftFinding.
        /// </summary>
        private void CheckAndAddDrift(
            string metricName,
            decimal expected,
            decimal actual,
            decimal factor,
            DateTimeOffset detectedAt,
            Command request,
            List<DriftFindingItem> findings)
        {
            var withinTolerance = expected == 0m
                ? actual <= factor
                : Math.Abs(actual - expected) / expected <= factor;

            if (withinTolerance)
                return;

            var finding = DriftFinding.Detect(
                request.ServiceName,
                request.Environment,
                metricName,
                expected,
                actual,
                detectedAt,
                request.ReleaseId);

            driftFindingRepository.Add(finding);

            findings.Add(new DriftFindingItem(
                finding.Id.Value,
                finding.MetricName,
                finding.ExpectedValue,
                finding.ActualValue,
                finding.DeviationPercent,
                finding.Severity.ToString()));
        }
    }

    /// <summary>Resposta da detecção de drift com lista de findings encontrados.</summary>
    public sealed record Response(
        string ServiceName,
        string Environment,
        Guid SnapshotId,
        Guid BaselineId,
        decimal TolerancePercent,
        IReadOnlyList<DriftFindingItem> Findings,
        bool HasDrift);

    /// <summary>Item individual de drift finding detectado.</summary>
    public sealed record DriftFindingItem(
        Guid FindingId,
        string MetricName,
        decimal ExpectedValue,
        decimal ActualValue,
        decimal DeviationPercent,
        string Severity);
}
