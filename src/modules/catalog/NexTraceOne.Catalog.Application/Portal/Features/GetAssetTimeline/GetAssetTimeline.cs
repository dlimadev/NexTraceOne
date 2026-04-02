using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Portal.Features.GetAssetTimeline;

/// <summary>
/// Feature: GetAssetTimeline — retorna histórico cronológico de eventos de uma API.
/// Constrói timeline a partir de versões de contrato (criação, aprovação, depreciação)
/// e eventos de deployment.
/// </summary>
public static class GetAssetTimeline
{
    /// <summary>Query para obter timeline de eventos de uma API.</summary>
    public sealed record Query(Guid ApiAssetId, int Page = 1, int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida os parâmetros da consulta de timeline.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>
    /// Handler que retorna timeline de eventos da API.
    /// Agrega dados de versões de contrato e deployments registados.
    /// </summary>
    public sealed class Handler(
        IContractVersionRepository contractVersionRepository,
        IContractDeploymentRepository contractDeploymentRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var events = new List<TimelineEventDto>();

            // Fetch all contract versions for this API
            var versions = await contractVersionRepository.ListByApiAssetAsync(
                request.ApiAssetId, cancellationToken);

            // Add version events
            foreach (var version in versions)
            {
                // Contract created/imported
                events.Add(new TimelineEventDto(
                    EventType: "ContractVersion",
                    Title: $"Contract v{version.SemVer} created",
                    Description: $"Protocol: {version.Protocol}, Format: {version.Format}",
                    Version: version.SemVer,
                    Actor: version.ImportedFrom,
                    OccurredAt: version.CreatedAt));

                // If deprecated, add deprecation event
                if (version.DeprecationDate.HasValue)
                {
                    events.Add(new TimelineEventDto(
                        EventType: "Deprecation",
                        Title: $"Contract v{version.SemVer} deprecated",
                        Description: version.DeprecationNotice,
                        Version: version.SemVer,
                        Actor: null,
                        OccurredAt: version.DeprecationDate.Value));
                }

                // If locked, add lock event
                if (version.LockedAt.HasValue)
                {
                    events.Add(new TimelineEventDto(
                        EventType: "Locked",
                        Title: $"Contract v{version.SemVer} locked",
                        Description: null,
                        Version: version.SemVer,
                        Actor: version.LockedBy,
                        OccurredAt: version.LockedAt.Value));
                }

                // Add deployment events for each version
                var deployments = await contractDeploymentRepository.ListByContractVersionAsync(
                    version.Id, cancellationToken);
                foreach (var deployment in deployments)
                {
                    events.Add(new TimelineEventDto(
                        EventType: "Deployment",
                        Title: $"Deployed v{deployment.SemVer} to {deployment.Environment}",
                        Description: $"Status: {deployment.Status}, Source: {deployment.SourceSystem}",
                        Version: deployment.SemVer,
                        Actor: deployment.DeployedBy,
                        OccurredAt: deployment.DeployedAt));
                }
            }

            // Sort by date descending (most recent first)
            events.Sort((a, b) => b.OccurredAt.CompareTo(a.OccurredAt));

            // Apply pagination
            var totalCount = events.Count;
            var pagedEvents = events
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return Result<Response>.Success(new Response(
                ApiAssetId: request.ApiAssetId,
                Events: pagedEvents.AsReadOnly(),
                TotalCount: totalCount));
        }
    }

    /// <summary>DTO de evento na timeline de uma API.</summary>
    public sealed record TimelineEventDto(
        string EventType,
        string Title,
        string? Description,
        string? Version,
        string? Actor,
        DateTimeOffset OccurredAt);

    /// <summary>Resposta com timeline cronológica de eventos da API.</summary>
    public sealed record Response(
        Guid ApiAssetId,
        IReadOnlyList<TimelineEventDto> Events,
        int TotalCount);
}
