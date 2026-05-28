using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateFeatureModelBinding;

/// <summary>
/// Feature: UpdateFeatureModelBinding — atualiza uma vinculação feature → modelo existente.
/// Permite alterar modelo obrigatório, fallback e descrição.
/// </summary>
public static class UpdateFeatureModelBinding
{
    /// <summary>Comando de atualização de vinculação feature → modelo.</summary>
    public sealed record Command(
        Guid BindingId,
        string Description,
        Guid RequiredModelId,
        string RequiredModelName,
        string RequiredProviderId,
        Guid? FallbackModelId,
        string? FallbackModelName,
        string? FallbackProviderId,
        bool ClearFallback) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de atualização.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.BindingId).NotEqual(Guid.Empty);
            RuleFor(x => x.Description).MaximumLength(1000);
            RuleFor(x => x.RequiredModelId).NotEqual(Guid.Empty);
            RuleFor(x => x.RequiredModelName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.RequiredProviderId).NotEmpty().MaximumLength(100);

            When(x => x.FallbackModelId.HasValue && !x.ClearFallback, () =>
            {
                RuleFor(x => x.FallbackModelName).NotEmpty().MaximumLength(200);
                RuleFor(x => x.FallbackProviderId).NotEmpty().MaximumLength(100);
                RuleFor(x => x.FallbackModelId).NotEqual(x => x.RequiredModelId)
                    .WithMessage("O modelo de fallback não pode ser igual ao modelo obrigatório.");
            });
        }
    }

    /// <summary>Handler que atualiza a vinculação feature → modelo.</summary>
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

            var updateResult = binding.Update(
                request.Description ?? string.Empty,
                request.RequiredModelId,
                request.RequiredModelName,
                request.RequiredProviderId);

            if (updateResult.IsFailure)
                return updateResult.Error;

            if (request.ClearFallback)
            {
                binding.ClearFallback();
            }
            else if (request.FallbackModelId.HasValue
                && !string.IsNullOrWhiteSpace(request.FallbackModelName)
                && !string.IsNullOrWhiteSpace(request.FallbackProviderId))
            {
                var fallbackResult = binding.SetFallback(
                    request.FallbackModelId.Value,
                    request.FallbackModelName,
                    request.FallbackProviderId);

                if (fallbackResult.IsFailure)
                    return fallbackResult.Error;
            }

            await bindingRepository.UpdateAsync(binding, cancellationToken);

            return new Response(binding.Id.Value);
        }
    }

    /// <summary>Resposta da atualização da vinculação.</summary>
    public sealed record Response(Guid BindingId);
}
