using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.SecurityGate.Ports;
using NexTraceOne.Governance.Domain.SecurityGate.Enums;
using NexTraceOne.Governance.Domain.SecurityGate.ValueObjects;

namespace NexTraceOne.Governance.Application.SecurityGate.Features.GetSecurityScanResult;

/// <summary>Retorna o resultado completo de um scan de segurança pelo identificador.</summary>
public static class GetSecurityScanResult
{
    /// <summary>Query para obter resultado de scan.</summary>
    public sealed record Query(Guid ScanId) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator() => RuleFor(x => x.ScanId).NotEmpty();
    }

    /// <summary>Handler que carrega o resultado do scan.</summary>
    public sealed class Handler(ISecurityScanRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var scan = await repository.FindByIdAsync(request.ScanId, cancellationToken);
            if (scan is null)
                return Error.NotFound("SECURITY_SCAN_NOT_FOUND", "Scan '{0}' not found.", request.ScanId);

            var findingDtos = scan.Findings.Select(f => new FindingDto(
                f.FindingId, f.RuleId, f.Category.ToString(), f.Severity.ToString(),
                f.FilePath, f.LineNumber, f.Description, f.Remediation,
                f.CweId, f.OwaspCategory, f.IsAiGenerated, f.Status.ToString())).ToList();

            return Result<Response>.Success(new Response(
                ScanId: scan.Id.Value,
                TargetType: scan.TargetType.ToString(),
                TargetId: scan.TargetId,
                ScannedAt: scan.ScannedAt,
                Provider: scan.ScanProvider.ToString(),
                OverallRisk: scan.OverallRisk.ToString(),
                PassedGate: scan.PassedGate,
                Summary: scan.Summary,
                Findings: findingDtos));
        }
    }

    /// <summary>Resposta com resultado completo do scan.</summary>
    public sealed record Response(
        Guid ScanId,
        string TargetType,
        Guid TargetId,
        DateTimeOffset ScannedAt,
        string Provider,
        string OverallRisk,
        bool PassedGate,
        SecurityScanSummary Summary,
        IReadOnlyList<FindingDto> Findings);

    /// <summary>DTO de achado individual.</summary>
    public sealed record FindingDto(
        Guid FindingId,
        string RuleId,
        string Category,
        string Severity,
        string FilePath,
        int? LineNumber,
        string Description,
        string Remediation,
        string? CweId,
        string? OwaspCategory,
        bool IsAiGenerated,
        string Status);
}
