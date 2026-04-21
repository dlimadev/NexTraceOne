using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Entities;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.ComputeServiceRiskProfile;

/// <summary>
/// Feature: ComputeServiceRiskProfile — calcula e persiste o perfil de risco de um serviço.
/// Agrega scores dimensionais de vulnerabilidades, change failure rate, blast radius e
/// violações de política. Cada dimensão tem peso diferente no score global (ver entidade).
/// Resultado armazenado em chg_service_risk_profiles para consulta pelo Risk Center Report.
/// Wave F.2 — Risk Center.
/// </summary>
public static class ComputeServiceRiskProfile
{
    public sealed record RiskSignalInput(RiskSignalType Signal, string Reason);

    public sealed record Command(
        string TenantId,
        Guid ServiceAssetId,
        string ServiceName,
        int VulnerabilityScore,
        int ChangeFailureScore,
        int BlastRadiusScore,
        int PolicyViolationScore,
        IReadOnlyList<RiskSignalInput> ActiveSignals) : ICommand<Response>;

    public sealed record Response(
        Guid ProfileId,
        Guid ServiceAssetId,
        string ServiceName,
        RiskLevel OverallRiskLevel,
        int OverallScore,
        int VulnerabilityScore,
        int ChangeFailureScore,
        int BlastRadiusScore,
        int PolicyViolationScore,
        int ActiveSignalCount,
        DateTimeOffset ComputedAt);

    public sealed class Handler(
        IServiceRiskProfileRepository repository,
        IChangeIntelligenceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            Guard.Against.NullOrWhiteSpace(request.TenantId);
            Guard.Against.NullOrWhiteSpace(request.ServiceName);

            var now = clock.UtcNow;
            var signals = request.ActiveSignals
                .Select(s => (s.Signal, s.Reason))
                .ToList();

            var profile = ServiceRiskProfile.Compute(
                request.TenantId,
                request.ServiceAssetId,
                request.ServiceName,
                request.VulnerabilityScore,
                request.ChangeFailureScore,
                request.BlastRadiusScore,
                request.PolicyViolationScore,
                signals,
                now);

            repository.Add(profile);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                profile.Id.Value,
                profile.ServiceAssetId,
                profile.ServiceName,
                profile.OverallRiskLevel,
                profile.OverallScore,
                profile.VulnerabilityScore,
                profile.ChangeFailureScore,
                profile.BlastRadiusScore,
                profile.PolicyViolationScore,
                profile.ActiveSignalCount,
                profile.ComputedAt));
        }
    }
}
