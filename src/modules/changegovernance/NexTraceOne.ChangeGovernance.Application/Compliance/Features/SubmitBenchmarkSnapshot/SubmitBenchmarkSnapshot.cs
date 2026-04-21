using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Entities;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Errors;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.SubmitBenchmarkSnapshot;

/// <summary>
/// Feature: SubmitBenchmarkSnapshot — regista um snapshot de métricas DORA para um tenant
/// que concedeu consentimento de participação nos benchmarks cross-tenant.
/// Requer consentimento Granted — se ausente, retorna erro de negócio.
/// Wave D.2 — Cross-tenant Benchmarks anonimizados.
/// </summary>
public static class SubmitBenchmarkSnapshot
{
    public sealed record Command(
        string TenantId,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        decimal DeploymentFrequencyPerWeek,
        decimal LeadTimeForChangesHours,
        decimal ChangeFailureRatePercent,
        decimal MeanTimeToRestoreHours,
        decimal MaturityScore,
        int ServiceCount,
        bool AnonymizeForBenchmarks,
        decimal? CostPerRequestUsd = null) : ICommand<Response>;

    public sealed record Response(
        Guid SnapshotId,
        string TenantId,
        bool IsAnonymizedForBenchmarks);

    public sealed class Handler(
        ITenantBenchmarkConsentRepository consentRepository,
        IBenchmarkSnapshotRepository snapshotRepository,
        IChangeIntelligenceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var consent = await consentRepository.GetByTenantIdAsync(request.TenantId, cancellationToken);
            if (consent is null || !consent.IsOptedIn)
                return ComplianceErrors.ConsentNotGranted(request.TenantId);

            var now = clock.UtcNow;
            var snapshot = BenchmarkSnapshotRecord.Record(
                tenantId: request.TenantId,
                periodStart: request.PeriodStart,
                periodEnd: request.PeriodEnd,
                deploymentFrequencyPerWeek: request.DeploymentFrequencyPerWeek,
                leadTimeForChangesHours: request.LeadTimeForChangesHours,
                changeFailureRatePercent: request.ChangeFailureRatePercent,
                meanTimeToRestoreHours: request.MeanTimeToRestoreHours,
                maturityScore: request.MaturityScore,
                serviceCount: request.ServiceCount,
                now: now,
                costPerRequestUsd: request.CostPerRequestUsd);

            if (request.AnonymizeForBenchmarks)
                snapshot.MarkAsAnonymized();

            snapshotRepository.Add(snapshot);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                snapshot.Id.Value,
                snapshot.TenantId,
                snapshot.IsAnonymizedForBenchmarks));
        }
    }
}
