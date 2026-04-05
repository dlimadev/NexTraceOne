using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Portal.Entities;
using NexTraceOne.Catalog.Domain.Portal.Errors;

namespace NexTraceOne.Catalog.Application.Portal.Features.ApproveSubscription;

/// <summary>Feature: ApproveSubscription — aprova uma subscrição pendente.</summary>
public static class ApproveSubscription
{
    public sealed record Command(Guid SubscriptionId, string ApprovedBy) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.SubscriptionId).NotEmpty();
            RuleFor(x => x.ApprovedBy).NotEmpty().MaximumLength(500);
        }
    }

    public sealed class Handler(
        ISubscriptionRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var subscription = await repository.GetByIdAsync(SubscriptionId.From(request.SubscriptionId), cancellationToken);

            if (subscription is null)
                return DeveloperPortalErrors.SubscriptionNotFound(request.SubscriptionId.ToString());

            var approveResult = subscription.Approve(request.ApprovedBy, clock.UtcNow);
            if (approveResult.IsFailure)
                return approveResult.Error;

            repository.Update(subscription);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(subscription.Id.Value, subscription.Status.ToString(), subscription.ApprovedAt!.Value);
        }
    }

    public sealed record Response(Guid SubscriptionId, string Status, DateTimeOffset ApprovedAt);
}
