using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;

namespace NexTraceOne.Catalog.Application.DeveloperExperience.Features.GetIdeServiceContext;

/// <summary>
/// Feature: GetIdeServiceContext — snapshot compacto de contexto de serviço para extensões IDE.
///
/// Retorna payload optimizado (&lt;200ms), com ETag para caching agressivo (TTL configurável).
/// Campos: owner, tier, contratos active, última release, stability tier, open drifts, SLO status.
///
/// Endpoint: GET /api/v1/ide/context/service/{name}
/// Wave AK.1 — IDE Context API (Catalog / DeveloperExperience).
/// </summary>
public static class GetIdeServiceContext
{
    internal const int DefaultCacheTtlSeconds = 30;

    public sealed record Query(
        string TenantId,
        string ServiceName) : IQuery<ServiceContextSnapshot>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Snapshot compacto de contexto de serviço para IDE.</summary>
    public sealed record ServiceContextSnapshot(
        string ServiceName,
        string? OwnerTeam,
        string? Tier,
        int ActiveContractCount,
        string? LastReleaseName,
        DateTimeOffset? LastReleaseAt,
        string? StabilityTier,
        int OpenDriftCount,
        string? SloStatus,
        DateTimeOffset GeneratedAt);

    public sealed class Handler(
        IIdeContextReader contextReader,
        IDateTimeProvider clock) : IQueryHandler<Query, ServiceContextSnapshot>
    {
        public async Task<Result<ServiceContextSnapshot>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);
            Guard.Against.NullOrWhiteSpace(request.ServiceName);

            var snapshot = await contextReader.GetServiceContextAsync(
                request.TenantId,
                request.ServiceName,
                cancellationToken);

            if (snapshot is null)
                return Error.NotFound(
                    "IDE.ServiceNotFound",
                    $"Service '{request.ServiceName}' not found for tenant '{request.TenantId}'.");

            return Result<ServiceContextSnapshot>.Success(snapshot with { GeneratedAt = clock.UtcNow });
        }
    }
}
