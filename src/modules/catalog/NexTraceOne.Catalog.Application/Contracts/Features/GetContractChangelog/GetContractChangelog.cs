using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetContractChangelog;

/// <summary>
/// Feature: GetContractChangelog — obtém o detalhe completo de uma entrada de changelog
/// de contrato pelo seu identificador.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class GetContractChangelog
{
    /// <summary>Query para obter o detalhe de uma entrada de changelog.</summary>
    public sealed record Query(Guid ChangelogId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de detalhe de changelog.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ChangelogId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que obtém o detalhe completo de uma entrada de changelog de contrato.
    /// </summary>
    public sealed class Handler(IContractChangelogRepository repository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var changelog = await repository.GetByIdAsync(
                ContractChangelogId.From(request.ChangelogId), cancellationToken);

            if (changelog is null)
                return ContractsErrors.ChangelogNotFound(request.ChangelogId.ToString());

            return new Response(
                changelog.Id.Value,
                changelog.TenantId,
                changelog.ApiAssetId,
                changelog.ServiceName,
                changelog.FromVersion,
                changelog.ToVersion,
                changelog.ContractVersionId,
                changelog.VerificationId,
                changelog.Source.ToString(),
                changelog.Entries,
                changelog.Summary,
                changelog.MarkdownContent,
                changelog.JsonContent,
                changelog.IsApproved,
                changelog.ApprovedBy,
                changelog.ApprovedAt,
                changelog.CommitSha,
                changelog.CreatedAt,
                changelog.CreatedBy);
        }
    }

    /// <summary>Resposta com o detalhe completo de uma entrada de changelog de contrato.</summary>
    public sealed record Response(
        Guid ChangelogId,
        string TenantId,
        string ApiAssetId,
        string ServiceName,
        string? FromVersion,
        string ToVersion,
        Guid ContractVersionId,
        Guid? VerificationId,
        string Source,
        string Entries,
        string Summary,
        string? MarkdownContent,
        string? JsonContent,
        bool IsApproved,
        string? ApprovedBy,
        DateTimeOffset? ApprovedAt,
        string? CommitSha,
        DateTimeOffset CreatedAt,
        string CreatedBy);
}
