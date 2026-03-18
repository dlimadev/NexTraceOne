using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.RejectGovernanceWaiver;

/// <summary>
/// Feature: RejectGovernanceWaiver — rejeita um pedido de exceção (waiver) de governança.
/// </summary>
public static class RejectGovernanceWaiver
{
    /// <summary>Comando para rejeitar um waiver de governança.</summary>
    public sealed record Command(
        string WaiverId,
        string ReviewedBy) : ICommand<Response>;

    /// <summary>Handler que rejeita o waiver e retorna o ID confirmado.</summary>
    public sealed class Handler(
        IGovernanceWaiverRepository waiverRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.WaiverId, out var waiverGuid))
                return Error.Validation("INVALID_WAIVER_ID", "Waiver ID '{0}' is not a valid GUID.", request.WaiverId);

            var waiver = await waiverRepository.GetByIdAsync(new GovernanceWaiverId(waiverGuid), cancellationToken);
            if (waiver is null)
                return Error.NotFound("WAIVER_NOT_FOUND", "Waiver '{0}' not found.", request.WaiverId);

            waiver.Reject(request.ReviewedBy);

            await waiverRepository.UpdateAsync(waiver, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new Response(WaiverId: waiver.Id.Value.ToString());

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta com o ID do waiver rejeitado.</summary>
    public sealed record Response(string WaiverId);
}
