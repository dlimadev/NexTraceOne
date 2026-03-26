using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Portal.Errors;

namespace NexTraceOne.Catalog.Application.Portal.Features.GetContractPublicationStatus;

/// <summary>
/// Feature: GetContractPublicationStatus — consulta o estado de publicação no portal de uma versão de contrato.
/// Usada pela UI do workspace de contratos para mostrar se uma versão está publicada,
/// a aguardar publicação, retirada ou deprecated no Developer Portal.
/// Estrutura VSA: Query + Validator + Handler + Response em ficheiro único.
/// </summary>
public static class GetContractPublicationStatus
{
    /// <summary>Query para obter o estado de publicação de uma versão de contrato.</summary>
    public sealed record Query(Guid ContractVersionId) : IQuery<Response>;

    /// <summary>Valida os parâmetros da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que consulta o estado de publicação de uma versão de contrato.
    /// Retorna null (NotPublished) quando não existe entrada de publicação.
    /// </summary>
    public sealed class Handler(IContractPublicationEntryRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var entry = await repository.GetByContractVersionIdAsync(request.ContractVersionId, cancellationToken);

            return new Response(
                ContractVersionId: request.ContractVersionId,
                IsPublished: entry?.Status == Domain.Portal.Enums.ContractPublicationStatus.Published,
                Status: entry?.Status.ToString() ?? "NotPublished",
                PublicationEntryId: entry?.Id.Value,
                Visibility: entry?.Visibility.ToString(),
                PublishedBy: entry?.PublishedBy,
                PublishedAt: entry?.PublishedAt,
                ReleaseNotes: entry?.ReleaseNotes);
        }
    }

    /// <summary>Resposta com o estado de publicação de uma versão de contrato.</summary>
    public sealed record Response(
        Guid ContractVersionId,
        bool IsPublished,
        string Status,
        Guid? PublicationEntryId,
        string? Visibility,
        string? PublishedBy,
        DateTimeOffset? PublishedAt,
        string? ReleaseNotes);
}
