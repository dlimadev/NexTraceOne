using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Features.CreateCheckoutSession;

/// <summary>
/// Feature: CreateCheckoutSession — inicia o upgrade de plano do tenant ativo
/// criando uma sessão de checkout no gateway de pagamento (Stripe).
/// O upgrade efetivo acontece quando o webhook confirma o pagamento
/// (<see cref="ProcessStripeWebhook.ProcessStripeWebhook"/>).
/// </summary>
public static class CreateCheckoutSession
{
    /// <summary>Comando para criar a sessão de checkout do plano desejado.</summary>
    public sealed record Command(string Plan) : ICommand<Response>;

    /// <summary>Valida o plano solicitado.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Plan).NotEmpty()
                .Must(p => Enum.TryParse<TenantPlan>(p, ignoreCase: true, out var plan)
                    && plan != TenantPlan.Trial)
                .WithMessage("Plan must be Starter, Professional or Enterprise.");
        }
    }

    /// <summary>Resposta com a URL de checkout hospedada pelo gateway.</summary>
    public sealed record Response(string CheckoutUrl);

    /// <summary>Handler que delega a criação da sessão ao gateway de pagamento.</summary>
    internal sealed class Handler(
        IBillingGateway billingGateway,
        ICurrentTenant currentTenant) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var result = await billingGateway.CreateCheckoutSessionUrlAsync(
                currentTenant.Id,
                request.Plan,
                cancellationToken);

            if (result.IsFailure)
                return result.Error;

            return new Response(result.Value);
        }
    }
}
