using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Application.Features.RegisterWebhookSubscription;

/// <summary>
/// Feature: RegisterWebhookSubscription — regista uma nova subscrição de webhook outbound.
/// Permite que tenants configurem endpoints externos para receber notificações quando eventos
/// relevantes ocorrem no NexTraceOne (incidents, changes, contracts, services, alerts).
/// Persiste via IWebhookSubscriptionRepository + IUnitOfWork.
/// Ownership: módulo Integrations.
/// </summary>
public static class RegisterWebhookSubscription
{
    private static readonly IReadOnlySet<string> ValidEventTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "incident.created",
        "incident.resolved",
        "change.deployed",
        "change.promoted",
        "contract.published",
        "contract.deprecated",
        "service.registered",
        "alert.triggered",
    };

    /// <summary>Comando para registar uma subscrição de webhook outbound.</summary>
    public sealed record Command(
        string TenantId,
        string Name,
        string TargetUrl,
        IReadOnlyList<string> EventTypes,
        string? Secret,
        string? Description,
        bool IsActive = true) : ICommand<Response>;

    /// <summary>Validador do comando RegisterWebhookSubscription.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.TargetUrl)
                .NotEmpty()
                .MaximumLength(500)
                .Must(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                .WithMessage("TargetUrl must be a valid HTTPS URL.");
            RuleFor(x => x.EventTypes)
                .NotEmpty()
                .Must(e => e.Count <= 10)
                .WithMessage("A maximum of 10 event types is allowed.")
                .ForEach(rule => rule
                    .Must(et => ValidEventTypes.Contains(et))
                    .WithMessage(et => $"'{et}' is not a valid event type."));
            RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        }
    }

    /// <summary>Handler que regista uma nova subscrição de webhook outbound com persistência real.</summary>
    public sealed class Handler(
        IWebhookSubscriptionRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var hasSecret = !string.IsNullOrWhiteSpace(request.Secret);

            var subscription = WebhookSubscription.Create(
                tenantId: request.TenantId,
                name: request.Name,
                targetUrl: request.TargetUrl,
                eventTypes: request.EventTypes,
                secretHash: hasSecret ? ComputeSecretHash(request.Secret!) : null,
                description: request.Description,
                isActive: request.IsActive,
                utcNow: clock.UtcNow);

            await repository.AddAsync(subscription, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new Response(
                SubscriptionId: subscription.Id.Value,
                Name: subscription.Name,
                TargetUrl: subscription.TargetUrl,
                EventTypes: subscription.EventTypes,
                HasSecret: hasSecret,
                IsActive: subscription.IsActive,
                EventCount: subscription.EventTypes.Count,
                CreatedAt: subscription.CreatedAt);

            return Result<Response>.Success(response);
        }

        private static string ComputeSecretHash(string secret)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(secret);
            var hash = System.Security.Cryptography.SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }
    }

    /// <summary>Resposta do comando RegisterWebhookSubscription.</summary>
    public sealed record Response(
        Guid SubscriptionId,
        string Name,
        string TargetUrl,
        IReadOnlyList<string> EventTypes,
        bool HasSecret,
        bool IsActive,
        int EventCount,
        DateTimeOffset CreatedAt);
}
