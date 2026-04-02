using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.TransitionServiceLifecycle;

/// <summary>
/// Feature: TransitionServiceLifecycle — aplica uma transição de ciclo de vida
/// a um serviço usando a máquina de estados do domínio.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class TransitionServiceLifecycle
{
    /// <summary>Comando de transição de estado do ciclo de vida do serviço.</summary>
    public sealed record Command(Guid ServiceId, LifecycleStatus NewStatus) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de transição.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty();
            RuleFor(x => x.NewStatus).IsInEnum();
        }
    }

    /// <summary>
    /// Handler que executa a transição de ciclo de vida via máquina de estados do domínio.
    /// A transição é delegada ao método TransitionTo() da entidade ServiceAsset para garantir
    /// que as regras de negócio (ex: Planning→Development, não Planning→Active) sejam respeitadas.
    /// </summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var service = await serviceAssetRepository.GetByIdAsync(
                ServiceAssetId.From(request.ServiceId), cancellationToken);

            if (service is null)
                return CatalogGraphErrors.ServiceAssetNotFoundById(request.ServiceId);

            var previousStatus = service.LifecycleStatus;

            var transitionResult = service.TransitionTo(request.NewStatus);

            if (transitionResult.IsFailure)
                return transitionResult.Error;

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                service.Id.Value,
                service.Name,
                previousStatus.ToString(),
                service.LifecycleStatus.ToString(),
                dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta da transição de ciclo de vida do serviço.</summary>
    public sealed record Response(
        Guid ServiceId,
        string ServiceName,
        string PreviousStatus,
        string CurrentStatus,
        DateTimeOffset TransitionedAt);
}
