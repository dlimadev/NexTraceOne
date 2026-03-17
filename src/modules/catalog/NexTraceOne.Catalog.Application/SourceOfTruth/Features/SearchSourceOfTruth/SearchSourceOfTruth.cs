using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.SourceOfTruth.Abstractions;
using NexTraceOne.Catalog.Domain.SourceOfTruth.Enums;

namespace NexTraceOne.Catalog.Application.SourceOfTruth.Features.SearchSourceOfTruth;

/// <summary>
/// Feature: SearchSourceOfTruth — pesquisa unificada de descoberta no Source of Truth.
/// Permite encontrar serviços, contratos, referências documentais e runbooks
/// a partir de um termo de busca com escopo opcional.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class SearchSourceOfTruth
{
    /// <summary>Query de pesquisa unificada no Source of Truth.</summary>
    public sealed record Query(
        string SearchTerm,
        string? Scope = null,
        int MaxResults = 20) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.SearchTerm).NotEmpty().MaximumLength(200);
            RuleFor(x => x.MaxResults).InclusiveBetween(1, 100);
        }
    }

    /// <summary>Handler que pesquisa serviços, contratos e referências no Source of Truth.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceRepository,
        IContractVersionRepository contractRepository,
        ILinkedReferenceRepository referenceRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var services = new List<ServiceSearchResult>();
            var contracts = new List<ContractSearchResult>();
            var references = new List<ReferenceSearchResult>();

            var scopeAll = string.IsNullOrWhiteSpace(request.Scope) || request.Scope == "all";

            // Pesquisar serviços
            if (scopeAll || request.Scope == "services")
            {
                var found = await serviceRepository.SearchAsync(request.SearchTerm, cancellationToken);
                services.AddRange(found.Take(request.MaxResults).Select(s => new ServiceSearchResult(
                    s.Id.Value, s.Name, s.DisplayName, s.Domain,
                    s.TeamName, s.Criticality.ToString(), s.LifecycleStatus.ToString())));
            }

            // Pesquisar contratos
            if (scopeAll || request.Scope == "contracts")
            {
                var (found, _) = await contractRepository.SearchAsync(
                    null, null, null, request.SearchTerm, 1, request.MaxResults, cancellationToken);
                contracts.AddRange(found.Select(c => new ContractSearchResult(
                    c.Id.Value, c.ApiAssetId, c.SemVer, c.Protocol.ToString(),
                    c.LifecycleState.ToString())));
            }

            // Pesquisar referências (docs, runbooks, notas)
            if (scopeAll || request.Scope == "docs" || request.Scope == "runbooks")
            {
                LinkedReferenceType? refType = request.Scope switch
                {
                    "docs" => LinkedReferenceType.Documentation,
                    "runbooks" => LinkedReferenceType.Runbook,
                    _ => null
                };
                var found = await referenceRepository.SearchAsync(request.SearchTerm, refType, cancellationToken);
                references.AddRange(found.Take(request.MaxResults).Select(r => new ReferenceSearchResult(
                    r.Id.Value, r.AssetId, r.AssetType.ToString(), r.ReferenceType.ToString(),
                    r.Title, r.Description, r.Url)));
            }

            return new Response(
                services, contracts, references,
                TotalResults: services.Count + contracts.Count + references.Count);
        }
    }

    /// <summary>Resultado de pesquisa de serviço.</summary>
    public sealed record ServiceSearchResult(
        Guid ServiceId, string Name, string DisplayName, string Domain,
        string TeamName, string Criticality, string LifecycleStatus);

    /// <summary>Resultado de pesquisa de contrato.</summary>
    public sealed record ContractSearchResult(
        Guid VersionId, Guid ApiAssetId, string SemVer, string Protocol,
        string LifecycleState);

    /// <summary>Resultado de pesquisa de referência.</summary>
    public sealed record ReferenceSearchResult(
        Guid ReferenceId, Guid AssetId, string AssetType, string ReferenceType,
        string Title, string Description, string? Url);

    /// <summary>Resposta da pesquisa unificada do Source of Truth.</summary>
    public sealed record Response(
        IReadOnlyList<ServiceSearchResult> Services,
        IReadOnlyList<ContractSearchResult> Contracts,
        IReadOnlyList<ReferenceSearchResult> References,
        int TotalResults);
}
