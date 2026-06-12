using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;

using CreateCheckoutSessionFeature = NexTraceOne.IdentityAccess.Application.Features.CreateCheckoutSession.CreateCheckoutSession;
using ProcessStripeWebhookFeature = NexTraceOne.IdentityAccess.Application.Features.ProcessStripeWebhook.ProcessStripeWebhook;

namespace NexTraceOne.IdentityAccess.API.Endpoints.Endpoints;

/// <summary>
/// Endpoints de billing SaaS: criação de sessão de checkout para upgrade de
/// plano e webhook do Stripe para confirmação de pagamento.
/// </summary>
internal static class BillingEndpoints
{
    /// <summary>Mapeia os endpoints de billing no grupo <c>/billing</c>.</summary>
    internal static void Map(RouteGroupBuilder group)
    {
        var billingGroup = group.MapGroup("/billing");

        // POST /billing/checkout-session — inicia upgrade de plano (autenticado)
        billingGroup.MapPost("/checkout-session", async (
            CreateCheckoutSessionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequireAuthorization()
          .RequireRateLimiting("auth-sensitive");

        // POST /billing/stripe-webhook — confirmação de pagamento (assinado via HMAC)
        billingGroup.MapPost("/stripe-webhook", async (
            HttpRequest httpRequest,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            using var reader = new StreamReader(httpRequest.Body);
            var payload = await reader.ReadToEndAsync(cancellationToken);
            var signature = httpRequest.Headers["Stripe-Signature"].ToString();

            var result = await sender.Send(
                new ProcessStripeWebhookFeature.Command(payload, signature),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).AllowAnonymous();
    }
}
