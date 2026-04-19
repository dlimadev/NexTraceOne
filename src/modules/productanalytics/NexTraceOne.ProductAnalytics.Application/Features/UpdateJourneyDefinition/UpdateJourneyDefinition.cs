using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Domain.Entities;

namespace NexTraceOne.ProductAnalytics.Application.Features.UpdateJourneyDefinition;

/// <summary>
/// Actualiza uma definição de jornada existente.
/// Permite modificar o nome e os steps. A key é imutável após criação.
/// </summary>
public static class UpdateJourneyDefinition
{
    /// <summary>Comando para actualizar uma definição de jornada.</summary>
    public sealed record Command(
        Guid Id,
        string Name,
        string StepsJson,
        bool IsActive) : ICommand<Response>;

    /// <summary>Valida os dados de actualização.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.StepsJson).NotEmpty().MaximumLength(8000);
        }
    }

    /// <summary>Handler que actualiza a definição de jornada.</summary>
    public sealed class Handler(
        IJourneyDefinitionRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var definition = await repository.GetByIdAsync(
                new JourneyDefinitionId(request.Id), cancellationToken);

            if (definition is null)
                return Error.NotFound(
                    "journey_definition.not_found",
                    $"Journey definition '{request.Id}' not found.");

            definition.Update(request.Name, request.StepsJson, clock.UtcNow);

            if (request.IsActive && !definition.IsActive)
                definition.Activate(clock.UtcNow);
            else if (!request.IsActive && definition.IsActive)
                definition.Deactivate(clock.UtcNow);

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(definition.Id.Value, definition.Key, definition.Name, definition.IsActive);
        }
    }

    /// <summary>Confirmação de actualização.</summary>
    public sealed record Response(Guid Id, string Key, string Name, bool IsActive);
}
