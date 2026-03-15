using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;
using NexTraceOne.AiGovernance.Application.Abstractions;
using NexTraceOne.AiGovernance.Domain.Entities;
using NexTraceOne.AiGovernance.Domain.Enums;
using NexTraceOne.AiGovernance.Domain.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AiGovernance.Application.Features.UpdateModel;

/// <summary>
/// Feature: UpdateModel — atualiza detalhes e/ou estado de um modelo de IA.
/// Aplica UpdateDetails e/ou transições de estado conforme os campos fornecidos.
/// Estrutura VSA: Command + Validator + Handler num único ficheiro.
/// </summary>
public static class UpdateModel
{
    /// <summary>Comando de atualização de um modelo de IA. Campos nulos são ignorados.</summary>
    public sealed record Command(
        Guid ModelId,
        string? DisplayName,
        string? Capabilities,
        string? DefaultUseCases,
        int? SensitivityLevel,
        string? NewStatus) : ICommand;

    /// <summary>Valida a entrada do comando de atualização de modelo.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ModelId).NotEmpty();
            RuleFor(x => x.DisplayName).MaximumLength(300).When(x => x.DisplayName is not null);
            RuleFor(x => x.SensitivityLevel).InclusiveBetween(1, 5).When(x => x.SensitivityLevel.HasValue);
            RuleFor(x => x.NewStatus)
                .Must(s => s is null || Enum.TryParse<ModelStatus>(s, ignoreCase: true, out _))
                .WithMessage("NewStatus must be one of: Active, Inactive, Deprecated, Blocked.");
        }
    }

    /// <summary>Handler que atualiza detalhes e/ou estado de um modelo de IA.</summary>
    public sealed class Handler(
        IAiModelRepository modelRepository) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var model = await modelRepository.GetByIdAsync(
                AIModelId.From(request.ModelId),
                cancellationToken);

            if (model is null)
            {
                return AiGovernanceErrors.ModelNotFound(request.ModelId.ToString());
            }

            if (request.DisplayName is not null || request.Capabilities is not null ||
                request.DefaultUseCases is not null || request.SensitivityLevel.HasValue)
            {
                var detailsResult = model.UpdateDetails(
                    request.DisplayName ?? model.DisplayName,
                    request.Capabilities ?? model.Capabilities,
                    request.DefaultUseCases ?? model.DefaultUseCases,
                    request.SensitivityLevel ?? model.SensitivityLevel);

                if (detailsResult.IsFailure)
                {
                    return detailsResult.Error;
                }
            }

            if (request.NewStatus is not null)
            {
                var targetStatus = Enum.Parse<ModelStatus>(request.NewStatus, ignoreCase: true);
                var statusResult = targetStatus switch
                {
                    ModelStatus.Active => model.Activate(),
                    ModelStatus.Inactive => model.Deactivate(),
                    ModelStatus.Deprecated => model.Deprecate(),
                    ModelStatus.Blocked => model.Block(),
                    _ => Result<Unit>.Success(Unit.Value)
                };

                if (statusResult.IsFailure)
                {
                    return statusResult.Error;
                }
            }

            await modelRepository.UpdateAsync(model, cancellationToken);

            return Unit.Value;
        }
    }
}
