using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ApproveContractChangelog;

/// <summary>
/// Feature: ApproveContractChangelog — aprova formalmente uma entrada de changelog,
/// registando quem aprovou e quando. Impede aprovações duplicadas.
/// Estrutura VSA: Command + Validator + Handler + Response em arquivo único.
/// </summary>
public static class ApproveContractChangelog
{
    /// <summary>Comando para aprovar uma entrada de changelog de contrato.</summary>
    public sealed record Command(Guid ChangelogId) : ICommand<Response>;

    /// <summary>Valida o identificador do changelog a aprovar.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ChangelogId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que aprova formalmente uma entrada de changelog de contrato.
    /// Verifica existência e estado prévio antes de aprovar.
    /// </summary>
    public sealed class Handler(
        IContractChangelogRepository repository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ICurrentUser currentUser) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var changelog = await repository.GetByIdAsync(
                ContractChangelogId.From(request.ChangelogId), cancellationToken);

            if (changelog is null)
                return ContractsErrors.ChangelogNotFound(request.ChangelogId.ToString());

            if (changelog.IsApproved)
                return ContractsErrors.ChangelogAlreadyApproved(request.ChangelogId.ToString());

            var now = clock.UtcNow;
            changelog.Approve(currentUser.Id, now);

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(changelog.Id.Value, now);
        }
    }

    /// <summary>Resposta da aprovação de changelog de contrato.</summary>
    public sealed record Response(Guid ChangelogId, DateTimeOffset ApprovedAt);
}
