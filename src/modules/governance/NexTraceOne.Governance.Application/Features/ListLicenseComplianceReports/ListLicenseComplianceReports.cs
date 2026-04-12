using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.ListLicenseComplianceReports;

/// <summary>
/// Feature: ListLicenseComplianceReports — lista relatórios de compliance de licenças por escopo,
/// com filtro opcional por scope key.
///
/// Owner: módulo Governance.
/// Pilar: Compliance — visão panorâmica de compliance de licenças por escopo operacional.
/// </summary>
public static class ListLicenseComplianceReports
{
    /// <summary>Query para listar relatórios de compliance de licenças por escopo, com filtro opcional.</summary>
    public sealed record Query(
        LicenseComplianceScope Scope,
        string? ScopeKey = null) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Scope).IsInEnum();
            RuleFor(x => x.ScopeKey).MaximumLength(200).When(x => x.ScopeKey is not null);
        }
    }

    /// <summary>Handler que lista relatórios de compliance de licenças por escopo com filtros opcionais.</summary>
    public sealed class Handler(
        ILicenseComplianceReportRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var reports = await repository.ListByScopeAsync(
                request.Scope, request.ScopeKey, cancellationToken);

            var items = reports
                .Select(r => new LicenseComplianceReportItemDto(
                    ReportId: r.Id.Value,
                    Scope: r.Scope,
                    ScopeKey: r.ScopeKey,
                    ScopeLabel: r.ScopeLabel,
                    TotalDependencies: r.TotalDependencies,
                    CompliantCount: r.CompliantCount,
                    NonCompliantCount: r.NonCompliantCount,
                    WarningCount: r.WarningCount,
                    OverallRiskLevel: r.OverallRiskLevel,
                    CompliancePercent: r.CompliancePercent,
                    ScannedAt: r.ScannedAt))
                .ToList();

            return Result<Response>.Success(new Response(
                Items: items,
                TotalCount: items.Count,
                FilteredScope: request.Scope));
        }
    }

    /// <summary>Resposta com a lista de relatórios de compliance de licenças para o escopo.</summary>
    public sealed record Response(
        IReadOnlyList<LicenseComplianceReportItemDto> Items,
        int TotalCount,
        LicenseComplianceScope FilteredScope);

    /// <summary>DTO resumido de um relatório de compliance de licenças para listagem.</summary>
    public sealed record LicenseComplianceReportItemDto(
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
        DateTimeOffset ScannedAt);
}
