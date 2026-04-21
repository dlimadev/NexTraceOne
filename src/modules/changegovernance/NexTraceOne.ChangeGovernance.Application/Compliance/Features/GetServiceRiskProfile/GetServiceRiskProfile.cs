using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetServiceRiskProfile;

/// <summary>
/// Feature: GetServiceRiskProfile — obtém o perfil de risco mais recente de um serviço.
/// Usado por Engineers e Tech Leads para diagnóstico individual do serviço.
/// Wave F.2 — Risk Center.
/// </summary>
public static class GetServiceRiskProfile
{
    public sealed record Query(
        string TenantId,
        Guid ServiceAssetId) : IQuery<Response>;

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
        string ActiveSignalsJson,
        DateTimeOffset ComputedAt);

    public sealed class Handler(
        IServiceRiskProfileRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var profile = await repository.GetLatestByServiceAsync(
                request.TenantId,
                request.ServiceAssetId,
                cancellationToken);

            if (profile is null)
                return Error.NotFound("risk_center.profile_not_found",
                    $"No risk profile found for service {request.ServiceAssetId}.");

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
                profile.ActiveSignalsJson,
                profile.ComputedAt));
        }
    }
}
