using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.SecurityGate.Ports;
using NexTraceOne.Governance.Domain.SecurityGate.Enums;

namespace NexTraceOne.Governance.Application.SecurityGate.Features.EvaluateSecurityGate;

/// <summary>Re-avalia o security gate com thresholds personalizados.</summary>
public static class EvaluateSecurityGate
{
    /// <summary>Comando para re-avaliação do gate com thresholds customizados.</summary>
    public sealed record Command(
        Guid ScanId,
        int MaxCritical = 0,
        int MaxHigh = 3) : ICommand<Response>;

    /// <summary>Validação do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ScanId).NotEmpty();
            RuleFor(x => x.MaxCritical).GreaterThanOrEqualTo(0);
            RuleFor(x => x.MaxHigh).GreaterThanOrEqualTo(0);
        }
    }

    /// <summary>Handler que avalia o gate com os thresholds fornecidos.</summary>
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

            scan.ReEvaluateGate(request.MaxCritical, request.MaxHigh);
            await repository.UpdateAsync(scan, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                ScanId: scan.Id.Value,
                PassedGate: scan.PassedGate,
                OverallRisk: scan.OverallRisk,
                CriticalCount: scan.Summary.CriticalCount,
                HighCount: scan.Summary.HighCount));
        }
    }

    /// <summary>Resposta com resultado da re-avaliação do gate.</summary>
    public sealed record Response(
        Guid ScanId,
        bool PassedGate,
        SecurityRiskLevel OverallRisk,
        int CriticalCount,
        int HighCount);
}
