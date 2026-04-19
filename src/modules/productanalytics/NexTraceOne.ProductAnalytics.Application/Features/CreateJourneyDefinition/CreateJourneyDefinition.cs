using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Domain.Entities;

namespace NexTraceOne.ProductAnalytics.Application.Features.CreateJourneyDefinition;

/// <summary>
/// Cria uma nova definição de jornada configurável.
/// Pode ser global (tenant null) ou específica de tenant.
/// Admins da plataforma podem criar definições globais; tenant admins criam para o seu tenant.
/// </summary>
public static class CreateJourneyDefinition
{
    /// <summary>Comando para criar uma definição de jornada.</summary>
    public sealed record Command(
        string Key,
        string Name,
        string StepsJson,
        bool IsGlobal = false) : ICommand<Response>;

    /// <summary>Valida os dados da definição de jornada.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Key)
                .NotEmpty()
                .MaximumLength(50)
                .Matches("^[a-z0-9_]+$").WithMessage("Key must be lowercase alphanumeric with underscores.");
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.StepsJson).NotEmpty().MaximumLength(8000);
        }
    }

    /// <summary>Handler que persiste a nova definição de jornada.</summary>
    public sealed class Handler(
        IJourneyDefinitionRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentTenant tenant,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var tenantId = request.IsGlobal ? (Guid?)null : tenant.Id;

            var exists = await repository.ExistsAsync(request.Key, tenantId, cancellationToken);
            if (exists)
                return Error.Conflict(
                    "journey_definition.key_conflict",
                    $"A journey definition with key '{request.Key}' already exists in this scope.");

            var definition = JourneyDefinition.Create(
                tenantId,
                request.Key,
                request.Name,
                request.StepsJson,
                clock.UtcNow);

            await repository.AddAsync(definition, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(definition.Id.Value, definition.Key, definition.Name);
        }
    }

    /// <summary>Confirmação de criação da definição de jornada.</summary>
    public sealed record Response(Guid Id, string Key, string Name);
}
