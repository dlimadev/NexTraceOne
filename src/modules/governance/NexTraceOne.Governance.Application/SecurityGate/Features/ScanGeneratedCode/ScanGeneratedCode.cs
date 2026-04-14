using NexTraceOne.Governance.Application.Abstractions;
using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.SecurityGate.Ports;
using NexTraceOne.Governance.Application.SecurityGate.Services;
using NexTraceOne.Governance.Domain.SecurityGate.Entities;
using NexTraceOne.Governance.Domain.SecurityGate.Enums;

namespace NexTraceOne.Governance.Application.SecurityGate.Features.ScanGeneratedCode;

/// <summary>Executa análise SAST no código gerado por scaffold ou IA.</summary>
public static class ScanGeneratedCode
{
    /// <summary>Representa um ficheiro de código a ser scaneado.</summary>
    public sealed record CodeFile(string FileName, string Content, string Extension);

    /// <summary>Comando para iniciar o scan de código gerado.</summary>
    public sealed record Command(
        Guid TargetId,
        IReadOnlyList<CodeFile> Files) : ICommand<Response>;

    /// <summary>Validação do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TargetId).NotEmpty();
            RuleFor(x => x.Files).NotEmpty().WithMessage("At least one file is required.");
            RuleForEach(x => x.Files).ChildRules(f =>
            {
                f.RuleFor(x => x.FileName).NotEmpty();
                f.RuleFor(x => x.Content).NotNull();
            });
        }
    }

    /// <summary>Handler que executa o scan e persiste o resultado.</summary>
    public sealed class Handler(
        ISecurityScanRepository repository,
        IGovernanceUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var scanResult = SecurityScanResult.Create(ScanTarget.GeneratedCode, request.TargetId, ScanProvider.Internal);

            var allFindings = new List<SecurityFinding>();
            foreach (var file in request.Files)
            {
                var findings = InternalSastScanner.Scan(scanResult.Id.Value, file.FileName, file.Content);
                allFindings.AddRange(findings);
            }

            scanResult.AddFindings(allFindings);
            await repository.AddAsync(scanResult, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                ScanId: scanResult.Id.Value,
                OverallRisk: scanResult.OverallRisk,
                PassedGate: scanResult.PassedGate,
                TotalFindings: scanResult.Summary.TotalFindings,
                CriticalFindings: scanResult.Summary.CriticalCount,
                HighFindings: scanResult.Summary.HighCount));
        }
    }

    /// <summary>Resposta do scan de código gerado.</summary>
    public sealed record Response(
        Guid ScanId,
        SecurityRiskLevel OverallRisk,
        bool PassedGate,
        int TotalFindings,
        int CriticalFindings,
        int HighFindings);
}
