using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ListCanonicalEntityVersions;

/// <summary>
/// Feature: ListCanonicalEntityVersions — lista o histórico de versões de uma entidade canónica.
/// Permite rastrear a evolução do schema ao longo do tempo.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListCanonicalEntityVersions
{
    /// <summary>Query de listagem de versões de uma entidade canónica.</summary>
    public sealed record Query(Guid CanonicalEntityId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.CanonicalEntityId).NotEmpty();
        }
    }

    /// <summary>Handler que lista as versões de uma entidade canónica.</summary>
    public sealed class Handler(
        ICanonicalEntityRepository entityRepository,
        ICanonicalEntityVersionRepository versionRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var entity = await entityRepository.GetByIdAsync(
                CanonicalEntityId.From(request.CanonicalEntityId), cancellationToken);

            if (entity is null)
                return ContractsErrors.CanonicalEntityNotFound(request.CanonicalEntityId.ToString());

            var versions = await versionRepository.ListByEntityIdAsync(
                CanonicalEntityId.From(request.CanonicalEntityId), cancellationToken);

            var items = versions
                .Select(v => new CanonicalEntityVersionSummary(
                    v.Id.Value,
                    v.Version,
                    v.SchemaFormat,
                    v.ChangeDescription,
                    v.PublishedBy,
                    v.PublishedAt))
                .ToList()
                .AsReadOnly();

            return new Response(request.CanonicalEntityId, entity.Name, items);
        }
    }

    /// <summary>Resumo de uma versão de entidade canónica.</summary>
    public sealed record CanonicalEntityVersionSummary(
        Guid VersionId,
        string Version,
        string SchemaFormat,
        string ChangeDescription,
        string PublishedBy,
        DateTimeOffset PublishedAt);

    /// <summary>Resposta com a lista de versões de uma entidade canónica.</summary>
    public sealed record Response(
        Guid CanonicalEntityId,
        string EntityName,
        IReadOnlyList<CanonicalEntityVersionSummary> Versions);
}
