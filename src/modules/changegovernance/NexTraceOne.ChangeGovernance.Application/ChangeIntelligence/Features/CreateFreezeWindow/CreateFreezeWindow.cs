using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.CreateFreezeWindow;

/// <summary>
/// Feature: CreateFreezeWindow — cria uma janela de freeze para restringir ou elevar risco de mudanças.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class CreateFreezeWindow
{
    /// <summary>Comando para criar uma janela de freeze.</summary>
    public sealed record Command(
        string Name,
        string Reason,
        FreezeScope Scope,
        string? ScopeValue,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de criação de janela de freeze.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
            RuleFor(x => x.Scope).IsInEnum();
            RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt)
                .WithMessage("Freeze window end must be after start.");
        }
    }

    /// <summary>
    /// Handler que cria uma janela de freeze para restringir ou elevar risco de mudanças.
    /// </summary>
    public sealed class Handler(
        IFreezeWindowRepository freezeRepository,
        IChangeIntelligenceUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ICurrentUser currentUser) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var window = FreezeWindow.Create(
                request.Name,
                request.Reason,
                request.Scope,
                request.ScopeValue,
                request.StartsAt,
                request.EndsAt,
                currentUser.Id,
                dateTimeProvider.UtcNow);

            freezeRepository.Add(window);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                window.Id.Value,
                window.Name,
                window.Scope,
                window.StartsAt,
                window.EndsAt);
        }
    }

    /// <summary>Resposta da criação de janela de freeze.</summary>
    public sealed record Response(
        Guid FreezeWindowId,
        string Name,
        FreezeScope Scope,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt);
}
