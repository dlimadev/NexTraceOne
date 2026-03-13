using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.DeveloperPortal.Application.Abstractions;
using NexTraceOne.DeveloperPortal.Domain.Entities;
using NexTraceOne.DeveloperPortal.Domain.Errors;

namespace NexTraceOne.DeveloperPortal.Application.Features.DeleteSubscription;

/// <summary>
/// Feature: DeleteSubscription — remove subscrição de notificações de uma API.
/// Permite opt-out controlado pelo consumidor.
/// Estrutura VSA: Command + Validator + Handler em um único arquivo.
/// </summary>
public static class DeleteSubscription
{
    /// <summary>Comando para remover subscrição de API.</summary>
    public sealed record Command(Guid SubscriptionId, Guid RequesterId) : ICommand;

    /// <summary>Valida os parâmetros de remoção de subscrição.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.SubscriptionId).NotEmpty();
            RuleFor(x => x.RequesterId).NotEmpty();
        }
    }

    /// <summary>Handler que remove subscrição de API do repositório.</summary>
    public sealed class Handler(
        ISubscriptionRepository repository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var subscription = await repository.GetByIdAsync(
                SubscriptionId.From(request.SubscriptionId), cancellationToken);

            if (subscription is null)
                return DeveloperPortalErrors.SubscriptionNotFound(request.SubscriptionId.ToString());

            repository.Remove(subscription);
            await unitOfWork.CommitAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
