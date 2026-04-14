using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.ApproveGovernanceWaiver;

/// <summary>
/// Feature: ApproveGovernanceWaiver — aprova um pedido de exceção (waiver) de governança.
/// </summary>
public static class ApproveGovernanceWaiver
{
    /// <summary>Comando para aprovar um waiver de governança.</summary>
    public sealed record Command(
        string WaiverId,
        string ReviewedBy) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de aprovação de waiver.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.WaiverId).NotEmpty().MaximumLength(50);
            RuleFor(x => x.ReviewedBy).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que aprova o waiver e retorna o ID confirmado.</summary>
    public sealed class Handler(
        IGovernanceWaiverRepository waiverRepository,
        IGovernanceUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.WaiverId, out var waiverGuid))
                return Error.Validation("INVALID_WAIVER_ID", "Waiver ID '{0}' is not a valid GUID.", request.WaiverId);

            var waiver = await waiverRepository.GetByIdAsync(new GovernanceWaiverId(waiverGuid), cancellationToken);
            if (waiver is null)
                return Error.NotFound("WAIVER_NOT_FOUND", "Waiver '{0}' not found.", request.WaiverId);

            waiver.Approve(request.ReviewedBy);

            await waiverRepository.UpdateAsync(waiver, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new Response(WaiverId: waiver.Id.Value.ToString());

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta com o ID do waiver aprovado.</summary>
    public sealed record Response(string WaiverId);
}
