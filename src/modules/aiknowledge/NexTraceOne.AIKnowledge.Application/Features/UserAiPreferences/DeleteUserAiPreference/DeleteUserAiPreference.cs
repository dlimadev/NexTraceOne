using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Features.UserAiPreferences.DeleteUserAiPreference;

public static class DeleteUserAiPreference
{
    public sealed record Command(Guid PreferenceId) : ICommand<Unit>;

    internal sealed class Handler(
        IUserAiPreferenceRepository preferenceRepository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Unit>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var preference = await preferenceRepository.GetByIdAsync(
                UserAiPreferenceId.From(request.PreferenceId), cancellationToken);

            if (preference is null)
                return UserAiPreferenceErrors.NotFound(request.PreferenceId);

            var userId = Guid.Parse(currentUser.Id);
            if (preference.UserId != userId)
                return Error.Forbidden("DeleteUserAiPreference.Forbidden", "Não é possível remover preferência de outro usuário.");

            preference.Deactivate();
            await preferenceRepository.UpdateAsync(preference, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
