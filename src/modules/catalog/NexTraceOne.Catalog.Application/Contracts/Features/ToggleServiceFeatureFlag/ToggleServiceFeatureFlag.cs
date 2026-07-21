using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ToggleServiceFeatureFlag;

/// <summary>
/// Feature: ToggleServiceFeatureFlag — activa/desactiva uma feature flag existente a partir
/// do detalhe do serviço. Regista o instante do toggle (<c>LastToggledAt</c>) preservando os
/// restantes atributos da flag. Wave AS.1 — Feature Flag &amp; Experimentation Governance.
/// </summary>
public static class ToggleServiceFeatureFlag
{
    // ── Command ────────────────────────────────────────────────────────────
    /// <summary>Corpo HTTP do PATCH (o FlagId vem da rota e o TenantId do contexto).</summary>
    public sealed record ToggleBody(bool Enabled);

    /// <summary>Comando para alternar o estado de uma feature flag.</summary>
    public sealed record Command(Guid FlagId, string TenantId, bool Enabled) : ICommand<Response>;

    /// <summary>Validador do comando <see cref="Command"/>.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.FlagId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Resposta com o estado resultante da flag.</summary>
    public sealed record Response(string Id, bool Enabled);

    // ── Handler ────────────────────────────────────────────────────────────
    /// <summary>Handler do comando <see cref="Command"/>.</summary>
    public sealed class Handler(
        IFeatureFlagRepository repository,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var flag = await repository.FindByIdAsync(request.FlagId, request.TenantId, cancellationToken);
            if (flag is null)
                return Error.NotFound("FeatureFlag.NotFound", $"Feature flag {request.FlagId} not found.");

            var now = clock.UtcNow;
            flag.Upsert(
                request.Enabled,
                flag.EnabledEnvironmentsJson,
                flag.OwnerId,
                lastToggledAt: now,
                flag.ScheduledRemovalDate,
                now);

            return Result<Response>.Success(new Response(flag.Id.ToString(), flag.IsEnabled));
        }
    }
}
