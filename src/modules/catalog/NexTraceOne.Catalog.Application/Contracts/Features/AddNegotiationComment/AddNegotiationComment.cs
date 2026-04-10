using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.AddNegotiationComment;

/// <summary>
/// Feature: AddNegotiationComment — adiciona um comentário a uma negociação de contrato.
/// Incrementa o CommentCount da negociação e persiste o comentário e a negociação atualizada.
/// Estrutura VSA: Command + Validator + Handler + Response em arquivo único.
/// </summary>
public static class AddNegotiationComment
{
    /// <summary>Comando para adicionar um comentário a uma negociação.</summary>
    public sealed record Command(
        Guid NegotiationId,
        string AuthorId,
        string AuthorDisplayName,
        string Content,
        string? LineReference) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando de adição de comentário.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.NegotiationId).NotEmpty();
            RuleFor(x => x.AuthorId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.AuthorDisplayName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);
            RuleFor(x => x.LineReference).MaximumLength(500).When(x => x.LineReference is not null);
        }
    }

    /// <summary>
    /// Handler que cria e persiste um novo comentário numa negociação de contrato.
    /// </summary>
    public sealed class Handler(
        IContractNegotiationRepository negotiationRepository,
        INegotiationCommentRepository commentRepository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var negotiation = await negotiationRepository.GetByIdAsync(
                ContractNegotiationId.From(request.NegotiationId), cancellationToken);

            if (negotiation is null)
                return ContractsErrors.ContractNegotiationNotFound(request.NegotiationId.ToString());

            var now = clock.UtcNow;

            var comment = NegotiationComment.Create(
                negotiationId: request.NegotiationId,
                authorId: request.AuthorId,
                authorDisplayName: request.AuthorDisplayName,
                content: request.Content,
                lineReference: request.LineReference,
                createdAt: now);

            negotiation.AddComment(now);

            await commentRepository.AddAsync(comment, cancellationToken);
            await negotiationRepository.UpdateAsync(negotiation, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                comment.Id.Value,
                comment.NegotiationId,
                comment.AuthorId,
                comment.AuthorDisplayName,
                comment.Content,
                comment.LineReference,
                comment.CreatedAt);
        }
    }

    /// <summary>Resposta da adição de comentário à negociação.</summary>
    public sealed record Response(
        Guid CommentId,
        Guid NegotiationId,
        string AuthorId,
        string AuthorDisplayName,
        string Content,
        string? LineReference,
        DateTimeOffset CreatedAt);
}
