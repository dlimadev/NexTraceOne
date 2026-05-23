using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.GetCatalogCompletenessScore;

/// <summary>
/// Feature: GetCatalogCompletenessScore — calcula o score de completude do catálogo para um serviço.
///
/// Score de 0 a 100 pontos distribuídos em 5 dimensões (20 pts cada):
///   Identity     — nome, descrição, tipo, domínio
///   Ownership    — equipa, owner técnico, on-call, canal de contacto
///   Operations   — SLO target, classificação de dados, âmbito regulatório
///   Documentation — URL docs, URL repositório, Git repository
///   Governance   — revisão de ownership recente, infra provider, hosting platform
///
/// Scores altos (≥ 80) desbloqueiam gates de deployment para serviços Tier Critical.
/// </summary>
public static class GetCatalogCompletenessScore
{
    public sealed record Query(Guid ServiceId) : IQuery<GetCatalogCompletenessScoreResponse>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty();
        }
    }

    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        IDateTimeProvider clock) : IQueryHandler<Query, GetCatalogCompletenessScoreResponse>
    {
        public async Task<Result<GetCatalogCompletenessScoreResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var service = await serviceAssetRepository
                .GetByIdAsync(ServiceAssetId.From(request.ServiceId), cancellationToken);

            if (service is null)
                return CatalogGraphErrors.ServiceAssetNotFoundById(request.ServiceId);

            var breakdown = Compute(service, clock.UtcNow);
            return breakdown;
        }

        internal static GetCatalogCompletenessScoreResponse Compute(ServiceAsset service, DateTimeOffset now)
        {
            var identity = ScoreIdentity(service);
            var ownership = ScoreOwnership(service);
            var operations = ScoreOperations(service);
            var documentation = ScoreDocumentation(service);
            var governance = ScoreGovernance(service, now);

            var total = identity.Points + ownership.Points + operations.Points
                        + documentation.Points + governance.Points;

            var level = total switch
            {
                >= 90 => "Excelente",
                >= 75 => "Maduro",
                >= 50 => "Em Desenvolvimento",
                _ => "Nascente"
            };

            return new GetCatalogCompletenessScoreResponse(
                ServiceId: service.Id.Value,
                ServiceName: service.Name,
                TotalScore: total,
                MaxScore: 100,
                MaturityLevel: level,
                Identity: identity,
                Ownership: ownership,
                Operations: operations,
                Documentation: documentation,
                Governance: governance);
        }

        private static DimensionScore ScoreIdentity(ServiceAsset s)
        {
            var items = new List<ScoreItem>
            {
                new("DisplayName", !string.IsNullOrWhiteSpace(s.DisplayName), 5),
                new("Description", !string.IsNullOrWhiteSpace(s.Description), 5),
                new("Domain", !string.IsNullOrWhiteSpace(s.Domain), 5),
                new("SystemArea", !string.IsNullOrWhiteSpace(s.SystemArea), 5)
            };
            return new DimensionScore("Identity", items.Sum(i => i.Earned), 20, items);
        }

        private static DimensionScore ScoreOwnership(ServiceAsset s)
        {
            var items = new List<ScoreItem>
            {
                new("TeamName", !string.IsNullOrWhiteSpace(s.TeamName), 5),
                new("TechnicalOwner", !string.IsNullOrWhiteSpace(s.TechnicalOwner), 5),
                new("OnCallRotationId", !string.IsNullOrWhiteSpace(s.OnCallRotationId), 5),
                new("ContactChannel", !string.IsNullOrWhiteSpace(s.ContactChannel), 5)
            };
            return new DimensionScore("Ownership", items.Sum(i => i.Earned), 20, items);
        }

        private static DimensionScore ScoreOperations(ServiceAsset s)
        {
            var items = new List<ScoreItem>
            {
                new("SloTarget", !string.IsNullOrWhiteSpace(s.SloTarget), 7),
                new("DataClassification", !string.IsNullOrWhiteSpace(s.DataClassification), 7),
                new("RegulatoryScope", !string.IsNullOrWhiteSpace(s.RegulatoryScope), 6)
            };
            return new DimensionScore("Operations", items.Sum(i => i.Earned), 20, items);
        }

        private static DimensionScore ScoreDocumentation(ServiceAsset s)
        {
            var items = new List<ScoreItem>
            {
                new("DocumentationUrl", !string.IsNullOrWhiteSpace(s.DocumentationUrl), 7),
                new("RepositoryUrl", !string.IsNullOrWhiteSpace(s.RepositoryUrl), 7),
                new("GitRepository", !string.IsNullOrWhiteSpace(s.GitRepository), 6)
            };
            return new DimensionScore("Documentation", items.Sum(i => i.Earned), 20, items);
        }

        private static DimensionScore ScoreGovernance(ServiceAsset s, DateTimeOffset now)
        {
            var ownershipFresh = s.LastOwnershipReviewAt.HasValue
                && (now - s.LastOwnershipReviewAt.Value).TotalDays <= 90;

            var items = new List<ScoreItem>
            {
                new("OwnershipReviewedRecently", ownershipFresh, 10),
                new("InfrastructureProvider", !string.IsNullOrWhiteSpace(s.InfrastructureProvider), 5),
                new("HostingPlatform", !string.IsNullOrWhiteSpace(s.HostingPlatform), 5)
            };
            return new DimensionScore("Governance", items.Sum(i => i.Earned), 20, items);
        }
    }
}

public sealed record GetCatalogCompletenessScoreResponse(
    Guid ServiceId,
    string ServiceName,
    int TotalScore,
    int MaxScore,
    string MaturityLevel,
    DimensionScore Identity,
    DimensionScore Ownership,
    DimensionScore Operations,
    DimensionScore Documentation,
    DimensionScore Governance);

public sealed record DimensionScore(
    string Name,
    int Points,
    int MaxPoints,
    IReadOnlyList<ScoreItem> Items);

public sealed record ScoreItem(string Field, bool Present, int MaxPoints)
{
    public int Earned => Present ? MaxPoints : 0;
}
