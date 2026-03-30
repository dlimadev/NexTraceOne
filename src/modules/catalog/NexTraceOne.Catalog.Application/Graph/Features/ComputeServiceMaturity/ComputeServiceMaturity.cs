using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.ComputeServiceMaturity;

/// <summary>
/// Feature: ComputeServiceMaturity — calcula scorecard de maturidade de um serviço individual.
/// Avalia ownership, contratos, documentação, links operacionais, descrição e ciclo de vida.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ComputeServiceMaturity
{
    /// <summary>Query para computar maturidade de um serviço pelo ID.</summary>
    public sealed record Query(Guid ServiceId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty();
        }
    }

    /// <summary>Handler que calcula o scorecard de maturidade do serviço.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        IServiceLinkRepository serviceLinkRepository,
        IApiAssetRepository apiAssetRepository,
        IContractVersionRepository contractVersionRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var serviceId = ServiceAssetId.From(request.ServiceId);
            var service = await serviceAssetRepository.GetByIdAsync(serviceId, cancellationToken);
            if (service is null)
                return CatalogGraphErrors.ServiceAssetNotFoundById(request.ServiceId);

            var links = await serviceLinkRepository.ListByServiceAsync(serviceId, cancellationToken);
            var apis = await apiAssetRepository.ListByServiceIdAsync(serviceId, cancellationToken);

            var contractCount = 0;
            if (apis.Count > 0)
            {
                var apiIds = apis.Select(a => a.Id.Value).ToList();
                var contracts = await contractVersionRepository.ListByApiAssetIdsAsync(apiIds, cancellationToken);
                contractCount = contracts.Count;
            }

            var dimensions = ComputeDimensions(service, links, apis.Count, contractCount);
            var overallScore = dimensions.Count > 0
                ? Math.Round(dimensions.Average(d => d.Score), 2)
                : 0m;
            var level = ScoreToLevel(overallScore);

            return new Response(
                ServiceId: request.ServiceId,
                ServiceName: service.Name,
                DisplayName: service.DisplayName,
                TeamName: service.TeamName,
                Domain: service.Domain,
                Level: level.ToString(),
                OverallScore: overallScore,
                Dimensions: dimensions,
                ComputedAt: DateTimeOffset.UtcNow);
        }

        private static List<MaturityDimensionDto> ComputeDimensions(
            ServiceAsset service,
            IReadOnlyList<ServiceLink> links,
            int apiCount,
            int contractCount)
        {
            var dimensions = new List<MaturityDimensionDto>();

            // 1. Ownership — equipa, owner técnico, owner de negócio
            var ownershipScore = 0m;
            var ownershipNotes = new List<string>();
            if (!string.IsNullOrWhiteSpace(service.TeamName)) { ownershipScore += 0.4m; }
            else { ownershipNotes.Add("Missing team name"); }
            if (!string.IsNullOrWhiteSpace(service.TechnicalOwner)) { ownershipScore += 0.35m; }
            else { ownershipNotes.Add("Missing technical owner"); }
            if (!string.IsNullOrWhiteSpace(service.BusinessOwner)) { ownershipScore += 0.25m; }
            else { ownershipNotes.Add("Missing business owner"); }
            dimensions.Add(new("ownership", Math.Round(ownershipScore, 2), 1m,
                ownershipScore >= 1m ? "Complete ownership defined" : string.Join("; ", ownershipNotes)));

            // 2. Contracts — tem contratos registados?
            var contractScore = contractCount switch
            {
                0 when apiCount == 0 => 0.5m, // sem APIs, neutro
                0 => 0m,
                >= 3 => 1m,
                _ => 0.5m + (0.5m * Math.Min(contractCount, 3) / 3m)
            };
            var contractExplanation = contractCount == 0 && apiCount == 0
                ? "No APIs registered — consider registering APIs"
                : contractCount == 0
                    ? $"{apiCount} API(s) but no contracts registered"
                    : $"{contractCount} contract(s) across {apiCount} API(s)";
            dimensions.Add(new("contracts", Math.Round(contractScore, 2), 1m, contractExplanation));

            // 3. Documentation — URL de documentação, links de doc
            var hasDocUrl = !string.IsNullOrWhiteSpace(service.DocumentationUrl);
            var docLinkCount = links.Count(l =>
                l.Category is LinkCategory.Documentation or LinkCategory.Wiki or LinkCategory.Adr);
            var docScore = 0m;
            if (hasDocUrl) docScore += 0.5m;
            docScore += Math.Min(docLinkCount * 0.25m, 0.5m);
            dimensions.Add(new("documentation", Math.Round(docScore, 2), 1m,
                docScore >= 1m ? "Documentation well covered"
                : docScore > 0 ? $"Partial documentation: {(hasDocUrl ? "URL present" : "no URL")}; {docLinkCount} doc link(s)"
                : "No documentation URL or documentation links"));

            // 4. Repository & CI/CD
            var hasRepoUrl = !string.IsNullOrWhiteSpace(service.RepositoryUrl);
            var repoLinkCount = links.Count(l => l.Category == LinkCategory.Repository);
            var cicdLinkCount = links.Count(l => l.Category == LinkCategory.CiCd);
            var repoScore = 0m;
            if (hasRepoUrl || repoLinkCount > 0) repoScore += 0.5m;
            if (cicdLinkCount > 0) repoScore += 0.5m;
            dimensions.Add(new("repository", Math.Round(repoScore, 2), 1m,
                repoScore >= 1m ? "Repository and CI/CD links present"
                : repoScore > 0 ? "Partial: missing " + (hasRepoUrl || repoLinkCount > 0 ? "CI/CD link" : "repository link")
                : "No repository or CI/CD links"));

            // 5. Operational Readiness — monitoring, dashboards, runbooks
            var monitoringCount = links.Count(l =>
                l.Category is LinkCategory.Monitoring or LinkCategory.Dashboard);
            var runbookCount = links.Count(l => l.Category == LinkCategory.Runbook);
            var opsScore = 0m;
            if (monitoringCount > 0) opsScore += 0.5m;
            if (runbookCount > 0) opsScore += 0.5m;
            dimensions.Add(new("operationalReadiness", Math.Round(opsScore, 2), 1m,
                opsScore >= 1m ? "Monitoring and runbooks available"
                : opsScore > 0 ? $"{monitoringCount} monitoring link(s); {runbookCount} runbook(s)"
                : "No monitoring or runbook links"));

            // 6. Description & Classification
            var descScore = 0m;
            var descNotes = new List<string>();
            if (!string.IsNullOrWhiteSpace(service.Description) && service.Description.Length > 20)
            { descScore += 0.5m; }
            else { descNotes.Add("Missing or too short description"); }
            if (service.LifecycleStatus is not (LifecycleStatus.Planning or LifecycleStatus.Retired))
            {
                if (!string.IsNullOrWhiteSpace(service.SystemArea)) descScore += 0.25m;
                else descNotes.Add("Missing system area");
                if (service.ExposureType != ExposureType.Internal || service.Criticality != Criticality.Medium)
                    descScore += 0.25m; // explicitly classified
                else descNotes.Add("Default classification (consider reviewing)");
            }
            else
            {
                descScore += 0.5m; // planning/retired is OK without full classification
            }
            dimensions.Add(new("classification", Math.Round(descScore, 2), 1m,
                descScore >= 1m ? "Well described and classified"
                : string.Join("; ", descNotes)));

            return dimensions;
        }

        private static MaturityLevel ScoreToLevel(decimal score) =>
            score >= 0.9m ? MaturityLevel.Optimizing
            : score >= 0.7m ? MaturityLevel.Managed
            : score >= 0.5m ? MaturityLevel.Defined
            : score >= 0.25m ? MaturityLevel.Developing
            : MaturityLevel.Initial;
    }

    /// <summary>Níveis de maturidade para serviço.</summary>
    public enum MaturityLevel
    {
        Initial,
        Developing,
        Defined,
        Managed,
        Optimizing
    }

    /// <summary>Resposta do scorecard de maturidade de um serviço.</summary>
    public sealed record Response(
        Guid ServiceId,
        string ServiceName,
        string DisplayName,
        string TeamName,
        string Domain,
        string Level,
        decimal OverallScore,
        IReadOnlyList<MaturityDimensionDto> Dimensions,
        DateTimeOffset ComputedAt);

    /// <summary>Pontuação de uma dimensão de maturidade.</summary>
    public sealed record MaturityDimensionDto(
        string Dimension,
        decimal Score,
        decimal MaxScore,
        string Explanation);
}
