using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Application.Graph.Features.CreateSavedView;

/// <summary>
/// Feature: CreateSavedView — persiste uma configuração de visualização do grafo
/// (filtros, overlay, foco, layout) para reutilização e compartilhamento.
/// Permite saved views por persona e deep links reproduzíveis.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class CreateSavedView
{
    /// <summary>Comando para criar uma visão salva do grafo.</summary>
    public sealed record Command(string Name, string Description, bool IsShared, string FiltersJson) : ICommand<Response>;

    /// <summary>Valida os parâmetros da visão salva.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(1000);
            RuleFor(x => x.FiltersJson).NotEmpty().MaximumLength(10000);
        }
    }

    /// <summary>
    /// Handler que cria e persiste uma visão salva do grafo.
    /// Vincula ao usuário corrente para controle de acesso.
    /// </summary>
    public sealed class Handler(
        ISavedGraphViewRepository viewRepository,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var view = SavedGraphView.Create(
                request.Name,
                request.Description,
                currentUser.Id,
                request.IsShared,
                request.FiltersJson,
                clock.UtcNow);

            viewRepository.Add(view);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(view.Id.Value, view.Name, view.IsShared, view.CreatedAt);
        }
    }

    /// <summary>Resposta com os metadados da visão salva criada.</summary>
    public sealed record Response(Guid ViewId, string Name, bool IsShared, DateTimeOffset CreatedAt);
}
