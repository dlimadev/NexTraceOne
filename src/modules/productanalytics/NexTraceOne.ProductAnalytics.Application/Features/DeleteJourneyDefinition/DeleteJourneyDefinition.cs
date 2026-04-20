using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Domain.Entities;

namespace NexTraceOne.ProductAnalytics.Application.Features.DeleteJourneyDefinition;

/// <summary>
/// Remove permanentemente uma definição de jornada configurável.
/// Apenas definições criadas por este tenant podem ser removidas.
/// Definições globais da plataforma não podem ser deletadas (apenas desactivadas via Update).
/// </summary>
public static class DeleteJourneyDefinition
{
    /// <summary>Comando para remover uma definição de jornada.</summary>
    public sealed record Command(Guid Id) : ICommand<Response>;

    /// <summary>Handler que remove a definição de jornada.</summary>
    public sealed class Handler(
        IJourneyDefinitionRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentTenant tenant) : ICommandHandler<Command, Response>
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

            // Definições globais não podem ser deletadas — apenas desactivadas
            if (definition.TenantId is null)
                return Error.Conflict(
                    "journey_definition.global_delete_forbidden",
                    "Global journey definitions cannot be deleted. Use Update to deactivate them.");

            // Só o tenant dono pode apagar a sua definição
            if (definition.TenantId != tenant.Id)
                return Error.Forbidden(
                    "journey_definition.access_denied",
                    "You do not have permission to delete this journey definition.");

            repository.Remove(definition);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(request.Id, definition.Key);
        }
    }

    /// <summary>Confirmação de remoção.</summary>
    public sealed record Response(Guid Id, string Key);
}
