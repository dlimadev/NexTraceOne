using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.DeveloperPortal.Application.Abstractions;
using NexTraceOne.DeveloperPortal.Domain.Entities;
using NexTraceOne.DeveloperPortal.Domain.Enums;
using NexTraceOne.DeveloperPortal.Domain.Errors;

namespace NexTraceOne.DeveloperPortal.Application.Features.CreateSubscription;

/// <summary>
/// Feature: CreateSubscription — regista subscrição formal de um consumidor a uma API.
/// Permite receber notificações de breaking changes, depreciações e atualizações.
/// </summary>
public static class CreateSubscription
{
    /// <summary>Comando para criar subscrição de notificações de API.</summary>
    public sealed record Command(
        Guid ApiAssetId,
        string ApiName,
        Guid SubscriberId,
        string SubscriberEmail,
        string ConsumerServiceName,
        string ConsumerServiceVersion,
        SubscriptionLevel Level,
        NotificationChannel Channel,
        string? WebhookUrl) : ICommand<Response>;

    /// <summary>Valida os dados de criação de subscrição.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.ApiName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.SubscriberId).NotEmpty();
            RuleFor(x => x.SubscriberEmail).NotEmpty().EmailAddress().MaximumLength(320);
            RuleFor(x => x.ConsumerServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ConsumerServiceVersion).NotEmpty().MaximumLength(50);
            RuleFor(x => x.WebhookUrl)
                .NotEmpty()
                .When(x => x.Channel == NotificationChannel.Webhook)
                .WithMessage("Webhook URL is required when notification channel is Webhook.");
        }
    }

    /// <summary>
    /// Handler que cria subscrição de API e persiste no repositório.
    /// Verifica duplicidade antes de criar e delega a construção ao factory method do domínio.
    /// </summary>
    public sealed class Handler(
        ISubscriptionRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var existing = await repository.GetByApiAndSubscriberAsync(
                request.ApiAssetId, request.SubscriberId, cancellationToken);

            if (existing is not null)
                return DeveloperPortalErrors.SubscriptionAlreadyExists(
                    request.ApiAssetId.ToString(), request.SubscriberId.ToString());

            var createResult = Subscription.Create(
                request.ApiAssetId,
                request.ApiName,
                request.SubscriberId,
                request.SubscriberEmail,
                request.ConsumerServiceName,
                request.ConsumerServiceVersion,
                request.Level,
                request.Channel,
                request.WebhookUrl,
                clock.UtcNow);

            if (createResult.IsFailure)
                return createResult.Error;

            var subscription = createResult.Value;
            repository.Add(subscription);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                subscription.Id.Value,
                subscription.ApiAssetId,
                subscription.ApiName,
                subscription.Level.ToString(),
                subscription.Channel.ToString(),
                subscription.IsActive,
                subscription.CreatedAt);
        }
    }

    /// <summary>Resposta com dados da subscrição criada.</summary>
    public sealed record Response(
        Guid SubscriptionId,
        Guid ApiAssetId,
        string ApiName,
        string Level,
        string Channel,
        bool IsActive,
        DateTimeOffset CreatedAt);
}
