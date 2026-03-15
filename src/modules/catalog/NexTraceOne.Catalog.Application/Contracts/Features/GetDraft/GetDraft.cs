using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Contracts.Domain.Entities;
using NexTraceOne.Contracts.Domain.Enums;
using NexTraceOne.Contracts.Domain.Errors;

namespace NexTraceOne.Contracts.Application.Features.GetDraft;

/// <summary>
/// Feature: GetDraft — retorna detalhes completos de um draft de contrato,
/// incluindo exemplos associados e metadados de geração por IA.
/// Estrutura VSA: Query + Validator + Handler + Response.
/// </summary>
public static class GetDraft
{
    /// <summary>Query para obter detalhes completos de um draft de contrato.</summary>
    public sealed record Query(Guid DraftId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de detalhe de draft.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.DraftId).NotEmpty();
        }
    }

    /// <summary>Handler que carrega e retorna detalhes completos do draft de contrato.</summary>
    public sealed class Handler(
        IContractDraftRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var draft = await repository.GetByIdAsync(
                ContractDraftId.From(request.DraftId), cancellationToken);

            if (draft is null)
                return ContractsErrors.DraftNotFound(request.DraftId.ToString());

            var examples = draft.Examples
                .Select(e => new ExampleDto(
                    e.Id.Value,
                    e.Name,
                    e.Description,
                    e.Content,
                    e.ContentFormat,
                    e.ExampleType,
                    e.CreatedBy,
                    e.CreatedAt))
                .ToList();

            return new Response(
                draft.Id.Value,
                draft.Title,
                draft.Description,
                draft.ServiceId,
                draft.ContractType,
                draft.Protocol,
                draft.SpecContent,
                draft.Format,
                draft.ProposedVersion,
                draft.Status,
                draft.Author,
                draft.BaseContractVersionId,
                draft.IsAiGenerated,
                draft.AiGenerationPrompt,
                draft.LastEditedAt,
                draft.LastEditedBy,
                examples);
        }
    }

    /// <summary>DTO de exemplo de contrato.</summary>
    public sealed record ExampleDto(
        Guid Id,
        string Name,
        string Description,
        string Content,
        string ContentFormat,
        string ExampleType,
        string CreatedBy,
        DateTimeOffset CreatedAt);

    /// <summary>Resposta com detalhes completos de um draft de contrato.</summary>
    public sealed record Response(
        Guid DraftId,
        string Title,
        string Description,
        Guid? ServiceId,
        ContractType ContractType,
        ContractProtocol Protocol,
        string SpecContent,
        string Format,
        string ProposedVersion,
        DraftStatus Status,
        string Author,
        Guid? BaseContractVersionId,
        bool IsAiGenerated,
        string? AiGenerationPrompt,
        DateTimeOffset? LastEditedAt,
        string? LastEditedBy,
        IReadOnlyList<ExampleDto> Examples);
}
