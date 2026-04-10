using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.CreatePromotionGate;

/// <summary>
/// Feature: CreatePromotionGate — cria um gate de promoção configurável para governar
/// a passagem de mudanças entre ambientes.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class CreatePromotionGate
{
    /// <summary>Comando para criar um gate de promoção.</summary>
    public sealed record Command(
        string Name,
        string? Description,
        string EnvironmentFrom,
        string EnvironmentTo,
        string? Rules,
        bool BlockOnFailure) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de criação de gate de promoção.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.EnvironmentFrom).NotEmpty().MaximumLength(100);
            RuleFor(x => x.EnvironmentTo).NotEmpty().MaximumLength(100);
        }
    }

    /// <summary>
    /// Handler que cria um gate de promoção para governar a passagem de mudanças entre ambientes.
    /// </summary>
    public sealed class Handler(
        IPromotionGateRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ICurrentUser currentUser) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var gate = PromotionGate.Create(
                request.Name,
                request.Description,
                request.EnvironmentFrom,
                request.EnvironmentTo,
                request.Rules,
                request.BlockOnFailure,
                currentUser.Id,
                dateTimeProvider.UtcNow,
                null);

            await repository.AddAsync(gate, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                gate.Id.Value,
                gate.Name,
                gate.EnvironmentFrom,
                gate.EnvironmentTo,
                gate.IsActive,
                gate.BlockOnFailure);
        }
    }

    /// <summary>Resposta da criação de gate de promoção.</summary>
    public sealed record Response(
        Guid GateId,
        string Name,
        string EnvironmentFrom,
        string EnvironmentTo,
        bool IsActive,
        bool BlockOnFailure);
}
