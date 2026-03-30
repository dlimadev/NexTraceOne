using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.UpdateFreezeWindow;

/// <summary>
/// Feature: UpdateFreezeWindow — atualiza os dados de uma janela de freeze existente.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class UpdateFreezeWindow
{
    /// <summary>Comando para atualizar uma janela de freeze.</summary>
    public sealed record Command(
        Guid FreezeWindowId,
        string Name,
        string Reason,
        FreezeScope Scope,
        string? ScopeValue,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt) : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.FreezeWindowId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
            RuleFor(x => x.Scope).IsInEnum();
            RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt)
                .WithMessage("Freeze window end must be after start.");
        }
    }

    /// <summary>Handler que atualiza uma janela de freeze.</summary>
    public sealed class Handler(
        IFreezeWindowRepository repository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var window = await repository.GetByIdAsync(
                FreezeWindowId.From(request.FreezeWindowId), cancellationToken);

            if (window is null)
                return Error.NotFound(
                    "change_intelligence.freeze.not_found",
                    "Freeze window not found.");

            var updateResult = window.Update(
                request.Name, request.Reason, request.Scope,
                request.ScopeValue, request.StartsAt, request.EndsAt);

            if (updateResult.IsFailure)
                return updateResult.Error;

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                window.Id.Value,
                window.Name,
                window.Scope.ToString(),
                window.StartsAt,
                window.EndsAt,
                window.IsActive);
        }
    }

    /// <summary>Resposta da atualização de uma janela de freeze.</summary>
    public sealed record Response(
        Guid FreezeWindowId,
        string Name,
        string Scope,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        bool IsActive);
}
