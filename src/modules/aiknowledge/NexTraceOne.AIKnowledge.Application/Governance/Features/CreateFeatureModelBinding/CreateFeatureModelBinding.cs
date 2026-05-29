using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.CreateFeatureModelBinding;

/// <summary>
/// Feature: CreateFeatureModelBinding — cria uma vinculação entre funcionalidade e modelo de IA.
/// Define qual modelo deve ser utilizado para uma feature específica da plataforma por tenant.
/// </summary>
public static class CreateFeatureModelBinding
{
    /// <summary>Comando de criação de vinculação feature → modelo.</summary>
    public sealed record Command(
        string FeatureKey,
        string Description,
        Guid RequiredModelId,
        string RequiredModelName,
        string RequiredProviderId,
        Guid? FallbackModelId,
        string? FallbackModelName,
        string? FallbackProviderId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de criação de vinculação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.FeatureKey).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(1000);
            RuleFor(x => x.RequiredModelId).NotEqual(Guid.Empty);
            RuleFor(x => x.RequiredModelName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.RequiredProviderId).NotEmpty().MaximumLength(100);

            When(x => x.FallbackModelId.HasValue, () =>
            {
                RuleFor(x => x.FallbackModelName).NotEmpty().MaximumLength(200);
                RuleFor(x => x.FallbackProviderId).NotEmpty().MaximumLength(100);
                RuleFor(x => x.FallbackModelId).NotEqual(x => x.RequiredModelId)
                    .WithMessage("O modelo de fallback não pode ser igual ao modelo obrigatório.");
            });
        }
    }

    /// <summary>Handler que cria a vinculação feature → modelo para o tenant atual.</summary>
    public sealed class Handler(
        IAiFeatureModelBindingRepository bindingRepository,
        ICurrentTenant currentTenant) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var normalizedKey = request.FeatureKey.Trim().ToLowerInvariant();

            var alreadyExists = await bindingRepository.ExistsAsync(
                normalizedKey, currentTenant.Id, cancellationToken);

            if (alreadyExists)
                return Error.Conflict(
                    "AiFeatureModelBinding.DuplicateKey",
                    "Já existe uma vinculação ativa para a feature '{0}' neste tenant.",
                    normalizedKey);

            var binding = AiFeatureModelBinding.Create(
                currentTenant.Id,
                normalizedKey,
                request.Description ?? string.Empty,
                request.RequiredModelId,
                request.RequiredModelName,
                request.RequiredProviderId);

            if (request.FallbackModelId.HasValue
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

            await bindingRepository.AddAsync(binding, cancellationToken);

            return new Response(binding.Id.Value, binding.FeatureKey);
        }
    }

    /// <summary>Resposta da criação da vinculação feature → modelo.</summary>
    public sealed record Response(Guid BindingId, string FeatureKey);
}
