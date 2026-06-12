using System.Text.Json;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Features.ProcessStripeWebhook;

/// <summary>
/// Feature: ProcessStripeWebhook — processa eventos de pagamento do Stripe.
/// Verifica a assinatura HMAC e, em checkout.session.completed, aplica o
/// upgrade de plano à licença do tenant e vincula a subscription externa.
/// Eventos não tratados são aceitos (Handled=false) para o Stripe não re-tentar.
/// </summary>
public static class ProcessStripeWebhook
{
    /// <summary>Comando público com o payload bruto e o header de assinatura.</summary>
    public sealed record Command(string Payload, string SignatureHeader) : ICommand<Response>, IPublicRequest;

    /// <summary>Valida presença do payload e da assinatura.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Payload).NotEmpty();
            RuleFor(x => x.SignatureHeader).NotEmpty();
        }
    }

    /// <summary>Resposta indicando se o evento foi tratado.</summary>
    public sealed record Response(bool Handled, string? EventType);

    /// <summary>Handler que verifica a assinatura e aplica o upgrade de licença.</summary>
    internal sealed class Handler(
        IBillingGateway billingGateway,
        ITenantLicenseRepository licenseRepository,
        IIdentityAccessUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!billingGateway.VerifyWebhookSignature(request.Payload, request.SignatureHeader))
                return Error.Unauthorized("billing.webhook.invalidSignature", "Webhook signature verification failed.");

            using var document = JsonDocument.Parse(request.Payload);
            var root = document.RootElement;
            var eventType = root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;

            if (string.Equals(eventType, "customer.subscription.deleted", StringComparison.Ordinal))
                return await HandleSubscriptionDeletedAsync(root, cancellationToken);

            if (!string.Equals(eventType, "checkout.session.completed", StringComparison.Ordinal))
                return new Response(false, eventType);

            if (!root.TryGetProperty("data", out var data)
                || !data.TryGetProperty("object", out var session))
                return Error.Validation("billing.webhook.malformed", "Webhook payload is missing data.object.");

            var metadata = session.TryGetProperty("metadata", out var meta) ? meta : default;
            var tenantIdRaw = metadata.ValueKind == JsonValueKind.Object
                && metadata.TryGetProperty("tenant_id", out var tid) ? tid.GetString() : null;
            var planRaw = metadata.ValueKind == JsonValueKind.Object
                && metadata.TryGetProperty("plan", out var pl) ? pl.GetString() : null;
            var subscriptionId = session.TryGetProperty("subscription", out var sub)
                && sub.ValueKind == JsonValueKind.String ? sub.GetString() : null;

            if (!Guid.TryParse(tenantIdRaw, out var tenantId)
                || !Enum.TryParse<TenantPlan>(planRaw, ignoreCase: true, out var plan))
                return Error.Validation("billing.webhook.malformed", "Webhook metadata is missing tenant_id or plan.");

            var license = await licenseRepository.GetByTenantIdAsync(tenantId, cancellationToken);
            if (license is null)
                return Error.NotFound("billing.licenseNotFound", $"No license found for tenant {tenantId}.");

            var now = clock.UtcNow;

            // Plano pago não expira (cobrança recorrente gerida pelo gateway).
            license.Upgrade(plan, license.IncludedHostUnits, newValidUntil: null, now);
            if (!string.IsNullOrWhiteSpace(subscriptionId))
                license.AttachExternalSubscription(subscriptionId, now);

            licenseRepository.Update(license);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(true, eventType);
        }

        /// <summary>
        /// Assinatura cancelada no gateway: faz downgrade da licença para Starter,
        /// mantendo o tenant operacional no plano gratuito.
        /// </summary>
        private async Task<Result<Response>> HandleSubscriptionDeletedAsync(
            JsonElement root,
            CancellationToken cancellationToken)
        {
            var subscriptionId = root.TryGetProperty("data", out var data)
                && data.TryGetProperty("object", out var subscription)
                && subscription.TryGetProperty("id", out var idProp)
                && idProp.ValueKind == JsonValueKind.String
                    ? idProp.GetString()
                    : null;

            if (string.IsNullOrWhiteSpace(subscriptionId))
                return Error.Validation("billing.webhook.malformed", "Subscription event is missing data.object.id.");

            var license = await licenseRepository.GetByExternalSubscriptionIdAsync(subscriptionId, cancellationToken);
            if (license is null)
                return new Response(false, "customer.subscription.deleted");

            var now = clock.UtcNow;
            license.Upgrade(TenantPlan.Starter, license.IncludedHostUnits, newValidUntil: null, now);
            licenseRepository.Update(license);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(true, "customer.subscription.deleted");
        }
    }
}
