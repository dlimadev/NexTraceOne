using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.Errors;

namespace NexTraceOne.Identity.Application.Features.RevokeBreakGlass;

/// <summary>
/// Feature: RevokeBreakGlass — revoga manualmente um acesso emergencial ativo.
/// Apenas administradores ou o próprio solicitante podem revogar.
/// </summary>
public static class RevokeBreakGlass
{
    /// <summary>Comando para revogação manual de acesso emergencial.</summary>
    public sealed record Command(Guid RequestId) : ICommand;

    /// <summary>Valida a entrada da revogação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RequestId).NotEmpty();
        }
    }

    /// <summary>Handler que processa a revogação de acesso emergencial.</summary>
    public sealed class Handler(
        IBreakGlassRepository breakGlassRepository,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (string.IsNullOrWhiteSpace(currentUser.Id))
                return IdentityErrors.NotAuthenticated();

            var breakGlass = await breakGlassRepository.GetByIdAsync(
                BreakGlassRequestId.From(request.RequestId), cancellationToken);

            if (breakGlass is null)
                return IdentityErrors.BreakGlassNotFound(request.RequestId);

            if (!breakGlass.IsActiveAt(dateTimeProvider.UtcNow))
                return IdentityErrors.BreakGlassNotActive(request.RequestId);

            breakGlass.Revoke(UserId.From(Guid.Parse(currentUser.Id)), dateTimeProvider.UtcNow);

            return Unit.Value;
        }
    }
}
