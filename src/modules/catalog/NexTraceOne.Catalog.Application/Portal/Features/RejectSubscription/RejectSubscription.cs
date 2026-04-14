using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Portal.Entities;
using NexTraceOne.Catalog.Domain.Portal.Errors;

namespace NexTraceOne.Catalog.Application.Portal.Features.RejectSubscription;

/// <summary>Feature: RejectSubscription — rejeita uma subscrição pendente ou ativa.</summary>
public static class RejectSubscription
{
    public sealed record Command(Guid SubscriptionId, string RejectionReason) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.SubscriptionId).NotEmpty();
            RuleFor(x => x.RejectionReason).NotEmpty().MaximumLength(1000);
        }
    }

    public sealed class Handler(
        ISubscriptionRepository repository,
        IPortalUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var subscription = await repository.GetByIdAsync(SubscriptionId.From(request.SubscriptionId), cancellationToken);

            if (subscription is null)
                return DeveloperPortalErrors.SubscriptionNotFound(request.SubscriptionId.ToString());

            var rejectResult = subscription.Reject(request.RejectionReason, clock.UtcNow);
            if (rejectResult.IsFailure)
                return rejectResult.Error;

            repository.Update(subscription);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(subscription.Id.Value, subscription.Status.ToString(), subscription.RejectionReason!);
        }
    }

    public sealed record Response(Guid SubscriptionId, string Status, string RejectionReason);
}
