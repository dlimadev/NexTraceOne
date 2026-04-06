using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.SecurityGate.Ports;
using NexTraceOne.Governance.Domain.SecurityGate.Enums;

namespace NexTraceOne.Governance.Application.SecurityGate.Features.AcknowledgeFinding;

/// <summary>Actualiza o estado de um achado de segurança (Acknowledged ou FalsePositive).</summary>
public static class AcknowledgeFinding
{
    /// <summary>Comando para actualizar estado de achado.</summary>
    public sealed record Command(
        Guid ScanId,
        Guid FindingId,
        FindingStatus NewStatus) : ICommand<Response>;

    /// <summary>Validação do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ScanId).NotEmpty();
            RuleFor(x => x.FindingId).NotEmpty();
            RuleFor(x => x.NewStatus)
                .Must(s => s is FindingStatus.Acknowledged or FindingStatus.FalsePositive or FindingStatus.Mitigated)
                .WithMessage("Only Acknowledged, FalsePositive or Mitigated are valid via this endpoint.");
        }
    }

    /// <summary>Handler que actualiza o estado do achado.</summary>
    public sealed class Handler(
        ISecurityScanRepository repository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var scan = await repository.FindByIdAsync(request.ScanId, cancellationToken);
            if (scan is null)
                return Error.NotFound("SECURITY_SCAN_NOT_FOUND", "Scan '{0}' not found.", request.ScanId);

            var finding = scan.Findings.FirstOrDefault(f => f.FindingId == request.FindingId);
            if (finding is null)
                return Error.NotFound("SECURITY_FINDING_NOT_FOUND", "Finding '{0}' not found.", request.FindingId);

            switch (request.NewStatus)
            {
                case FindingStatus.Acknowledged: finding.Acknowledge(); break;
                case FindingStatus.FalsePositive: finding.MarkAsFalsePositive(); break;
                case FindingStatus.Mitigated: finding.MarkAsMitigated(); break;
            }

            await repository.UpdateAsync(scan, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(request.FindingId, finding.Status.ToString()));
        }
    }

    /// <summary>Resposta da actualização de achado.</summary>
    public sealed record Response(Guid FindingId, string NewStatus);
}
