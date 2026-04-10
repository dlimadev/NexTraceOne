using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GenerateLicenseComplianceReport;

/// <summary>
/// Feature: GenerateLicenseComplianceReport — cria um relatório de compliance de licenças
/// de dependências para um escopo específico (serviço, equipa ou domínio).
///
/// Owner: módulo Governance.
/// Pilar: Compliance — análise de risco de licenciamento de dependências.
/// </summary>
public static class GenerateLicenseComplianceReport
{
    /// <summary>Comando para gerar e registar um relatório de compliance de licenças.</summary>
    public sealed record Command(
        LicenseComplianceScope Scope,
        string ScopeKey,
        string? ScopeLabel,
        int TotalDependencies,
        int CompliantCount,
        int NonCompliantCount,
        int WarningCount,
        LicenseRiskLevel OverallRiskLevel,
        string? LicenseDetails = null,
        string? Conflicts = null,
        string? Recommendations = null,
        string? TenantId = null) : ICommand<Response>;

    /// <summary>Validação do comando de geração de relatório de compliance de licenças.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Scope).IsInEnum();
            RuleFor(x => x.ScopeKey).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ScopeLabel).MaximumLength(300).When(x => x.ScopeLabel is not null);
            RuleFor(x => x.OverallRiskLevel).IsInEnum();
            RuleFor(x => x.TotalDependencies).GreaterThanOrEqualTo(0);
            RuleFor(x => x.CompliantCount).GreaterThanOrEqualTo(0);
            RuleFor(x => x.NonCompliantCount).GreaterThanOrEqualTo(0);
            RuleFor(x => x.WarningCount).GreaterThanOrEqualTo(0);
        }
    }

    /// <summary>Handler que cria um relatório de compliance de licenças de dependências.</summary>
    public sealed class Handler(
        ILicenseComplianceReportRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;

            var report = LicenseComplianceReport.Generate(
                scope: request.Scope,
                scopeKey: request.ScopeKey,
                scopeLabel: request.ScopeLabel,
                totalDependencies: request.TotalDependencies,
                compliantCount: request.CompliantCount,
                nonCompliantCount: request.NonCompliantCount,
                warningCount: request.WarningCount,
                overallRiskLevel: request.OverallRiskLevel,
                licenseDetails: request.LicenseDetails,
                conflicts: request.Conflicts,
                recommendations: request.Recommendations,
                tenantId: request.TenantId,
                now: now);

            await repository.AddAsync(report, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

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
                ScannedAt: report.ScannedAt));
        }
    }

    /// <summary>Resposta com o resultado da geração de relatório de compliance de licenças.</summary>
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
        DateTimeOffset ScannedAt);
}
