using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.SetServiceTier;

/// <summary>
/// Feature: SetServiceTier — define o tier operacional de um serviço.
/// O tier (Critical/Standard/Experimental) determina thresholds mínimos de SLO,
/// maturidade e gates de promoção aplicáveis ao serviço.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class SetServiceTier
{
    /// <summary>Comando para definir o tier operacional de um serviço.</summary>
    public sealed record Command(Guid ServiceId, string Tier) : ICommand<Response>;

    /// <summary>Valida o comando SetServiceTier.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly HashSet<string> ValidTiers =
        [
            nameof(ServiceTierType.Critical),
            nameof(ServiceTierType.Standard),
            nameof(ServiceTierType.Experimental)
        ];

        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty();
            RuleFor(x => x.Tier)
                .NotEmpty()
                .MaximumLength(30)
                .Must(t => ValidTiers.Contains(t))
                .WithMessage($"Tier must be one of: {string.Join(", ", ValidTiers)}");
        }
    }

    /// <summary>Handler que persiste o tier do serviço.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        ICatalogGraphUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var service = await serviceAssetRepository.GetByIdAsync(
                ServiceAssetId.From(request.ServiceId), cancellationToken);
            if (service is null)
                return CatalogGraphErrors.ServiceAssetNotFoundById(request.ServiceId);

            if (!Enum.TryParse<ServiceTierType>(request.Tier, ignoreCase: true, out var tier))
                return CatalogGraphErrors.InvalidServiceTier(request.Tier);

            service.SetTier(tier);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(service.Id.Value, service.Name, tier.ToString());
        }
    }

    /// <summary>Resposta do comando SetServiceTier.</summary>
    public sealed record Response(Guid ServiceId, string ServiceName, string Tier);
}
