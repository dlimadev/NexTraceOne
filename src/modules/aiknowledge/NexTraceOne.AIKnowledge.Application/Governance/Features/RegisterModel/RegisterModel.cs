using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.RegisterModel;

/// <summary>
/// Feature: RegisterModel — regista um novo modelo de IA no Model Registry.
/// Cria a entidade AIModel e persiste via repositório.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class RegisterModel
{
    /// <summary>Comando de registo de um modelo de IA no Model Registry.</summary>
    public sealed record Command(
        string Name,
        string DisplayName,
        string Provider,
        string ModelType,
        bool IsInternal,
        string Capabilities,
        int SensitivityLevel) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de registo de modelo.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Provider).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ModelType).NotEmpty();
            RuleFor(x => x.Capabilities).NotEmpty();
            RuleFor(x => x.SensitivityLevel).InclusiveBetween(1, 5);
        }
    }

    /// <summary>Handler que regista um novo modelo de IA no Model Registry.</summary>
    public sealed class Handler(
        IAiModelRepository modelRepository,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!Enum.TryParse<ModelType>(request.ModelType, ignoreCase: true, out var modelType))
                return Error.Validation("Model.InvalidModelType", $"'{request.ModelType}' is not a valid model type.");

            var model = AIModel.Register(
                request.Name,
                request.DisplayName,
                request.Provider,
                modelType,
                request.IsInternal,
                request.Capabilities,
                request.SensitivityLevel,
                dateTimeProvider.UtcNow);

            await modelRepository.AddAsync(model, cancellationToken);

            return new Response(model.Id.Value);
        }
    }

    /// <summary>Resposta do registo do modelo no Model Registry.</summary>
    public sealed record Response(Guid ModelId);
}
