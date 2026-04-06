using System.Text.Json;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.SecurityGate.Ports;

namespace NexTraceOne.Governance.Application.SecurityGate.Features.GenerateSecurityReport;

/// <summary>Gera um relatório estruturado de segurança para um scan específico.</summary>
public static class GenerateSecurityReport
{
    /// <summary>Query para gerar relatório de segurança.</summary>
    public sealed record Query(Guid ScanId) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator() => RuleFor(x => x.ScanId).NotEmpty();
    }

    /// <summary>Handler que gera o relatório JSON.</summary>
    public sealed class Handler(ISecurityScanRepository repository) : IQueryHandler<Query, Response>
    {
        private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var scan = await repository.FindByIdAsync(request.ScanId, cancellationToken);
            if (scan is null)
                return Error.NotFound("SECURITY_SCAN_NOT_FOUND", "Scan '{0}' not found.", request.ScanId);

            var findingsByCategory = scan.Findings
                .GroupBy(f => f.Category.ToString())
                .Select(g => new
                {
                    Category = g.Key,
                    Count = g.Count(),
                    Findings = g.Select(f => new
                    {
                        f.FindingId,
                        f.RuleId,
                        Severity = f.Severity.ToString(),
                        f.FilePath,
                        f.LineNumber,
                        f.Description,
                        f.Remediation,
                        f.CweId,
                        f.OwaspCategory,
                        Status = f.Status.ToString()
                    }).ToList()
                }).ToList();

            var report = new
            {
                ReportId = Guid.NewGuid(),
                GeneratedAt = DateTimeOffset.UtcNow,
                Scan = new
                {
                    ScanId = scan.Id.Value,
                    TargetType = scan.TargetType.ToString(),
                    scan.TargetId,
                    scan.ScannedAt,
                    Provider = scan.ScanProvider.ToString(),
                    OverallRisk = scan.OverallRisk.ToString(),
                    scan.PassedGate
                },
                Summary = new
                {
                    scan.Summary.TotalFindings,
                    scan.Summary.CriticalCount,
                    scan.Summary.HighCount,
                    scan.Summary.MediumCount,
                    scan.Summary.LowCount,
                    scan.Summary.InfoCount,
                    TopCategories = scan.Summary.TopCategories
                },
                FindingsByCategory = findingsByCategory,
                Recommendations = GenerateRecommendations(scan.Summary.TopCategories)
            };

            var reportJson = JsonSerializer.Serialize(report, s_jsonOptions);
            return Result<Response>.Success(new Response(ScanId: scan.Id.Value, ReportJson: reportJson));
        }

        private static IReadOnlyList<string> GenerateRecommendations(IReadOnlyList<string> topCategories)
        {
            var recommendations = new List<string>();
            foreach (var category in topCategories)
            {
                recommendations.Add(category switch
                {
                    "HardcodedSecrets" => "Move all secrets to environment variables or a secrets manager.",
                    "Injection" => "Use parameterized queries and ORM abstractions to prevent SQL injection.",
                    "InsecureCrypto" => "Upgrade to SHA-256+ for hashing and AES-256-GCM for encryption.",
                    "BrokenAccessControl" => "Ensure all endpoints have RequireAuthorization() or RequirePermission().",
                    "SecurityMisconfiguration" => "Review CORS, headers, and configuration hardening.",
                    "Xss" => "Use output encoding for all user-controlled content.",
                    "InsecureDeserialization" => "Replace BinaryFormatter with System.Text.Json.",
                    _ => $"Review and remediate all {category} findings."
                });
            }
            return recommendations;
        }
    }

    /// <summary>Resposta com relatório JSON estruturado.</summary>
    public sealed record Response(Guid ScanId, string ReportJson);
}
