using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.Governance.Domain.Errors;

namespace NexTraceOne.Governance.Application.Features.GetLicenseComplianceReport;

/// <summary>
/// Feature: GetLicenseComplianceReport — obtém um relatório de compliance de licenças
/// pelo seu identificador.
///
/// Owner: módulo Governance.
/// Pilar: Compliance — consulta de relatório de compliance de licenças individual.
/// </summary>
public static class GetLicenseComplianceReport
{
    /// <summary>Query para obter um relatório de compliance de licenças pelo identificador.</summary>
    public sealed record Query(Guid ReportId) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReportId).NotEmpty();
        }
    }

    /// <summary>Handler que obtém um relatório de compliance de licenças pelo seu identificador.</summary>
    public sealed class Handler(
        ILicenseComplianceReportRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var report = await repository.GetByIdAsync(
                new Domain.Entities.LicenseComplianceReportId(request.ReportId), cancellationToken);

            if (report is null)
                return GovernanceLicenseComplianceErrors.ReportNotFound(request.ReportId.ToString());

            return Result<Response>.Success(new Response(
                ReportId: report.Id.Value,
                Scope: report.Scope,
                ScopeKey: report.ScopeKey,
                ScopeLabel: report.ScopeLabel,
                TotalDependencies: report.TotalDependencies,
                CompliantCount: report.CompliantCount,
                NonCompliantCount: report.NonCompliantCount,
                WarningCount: report.WarningCount,
                OverallRiskLevel: report.OverallRiskLevel,
                CompliancePercent: report.CompliancePercent,
                LicenseDetails: report.LicenseDetails,
                Conflicts: report.Conflicts,
                Recommendations: report.Recommendations,
                ScannedAt: report.ScannedAt));
        }
    }

    /// <summary>Resposta completa com todos os detalhes de um relatório de compliance de licenças.</summary>
    public sealed record Response(
        Guid ReportId,
        LicenseComplianceScope Scope,
        string ScopeKey,
        string? ScopeLabel,
        int TotalDependencies,
        int CompliantCount,
        int NonCompliantCount,
        int WarningCount,
        LicenseRiskLevel OverallRiskLevel,
        int CompliancePercent,
        string? LicenseDetails,
        string? Conflicts,
        string? Recommendations,
        DateTimeOffset ScannedAt);
}
