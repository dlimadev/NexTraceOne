using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.DeleteFeatureModelBinding;

/// <summary>
/// Feature: DeleteFeatureModelBinding — desativa uma vinculação feature → modelo.
/// Operação de soft-delete: desativa a vinculação sem remover do histórico.
/// </summary>
public static class DeleteFeatureModelBinding
{
    /// <summary>Comando de desativação de vinculação.</summary>
    public sealed record Command(Guid BindingId) : ICommand<Response>;

    /// <summary>Handler que desativa a vinculação feature → modelo.</summary>
    public sealed class Handler(
        IAiFeatureModelBindingRepository bindingRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var binding = await bindingRepository.GetByIdAsync(
                AiFeatureModelBindingId.From(request.BindingId), cancellationToken);

            if (binding is null)
                return Error.NotFound(
                    "AiFeatureModelBinding.NotFound",
                    "Vinculação '{0}' não encontrada.",
                    request.BindingId);

            binding.Deactivate();
            await bindingRepository.UpdateAsync(binding, cancellationToken);

            return new Response(request.BindingId);
        }
    }

    /// <summary>Resposta da desativação da vinculação.</summary>
    public sealed record Response(Guid BindingId);
}
