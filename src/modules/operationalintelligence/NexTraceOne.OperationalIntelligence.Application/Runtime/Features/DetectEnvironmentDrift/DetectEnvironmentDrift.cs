using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.DetectEnvironmentDrift;

/// <summary>
/// Feature: DetectEnvironmentDrift — gera relatório multi-dimensional de drift entre dois ambientes.
/// Analisa: versões de serviço, configurações, contratos, dependências e políticas.
/// Marca relatórios anteriores como Stale quando um novo é gerado.
/// </summary>
public static class DetectEnvironmentDrift
{
    /// <summary>Comando para detectar drift multi-dimensional entre ambientes.</summary>
    public sealed record Command(
        string SourceEnvironment,
        string TargetEnvironment,
        string? ServiceVersionDrifts = null,
        string? ConfigurationDrifts = null,
        string? ContractVersionDrifts = null,
        string? DependencyDrifts = null,
        string? PolicyDrifts = null,
        string? Recommendations = null,
        int TotalDriftItems = 0,
        int CriticalDriftItems = 0) : ICommand<Response>;

    /// <summary>Validação do comando de detecção de drift.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.SourceEnvironment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.TargetEnvironment).NotEmpty().MaximumLength(100);
            RuleFor(x => x).Must(x => !string.Equals(x.SourceEnvironment, x.TargetEnvironment, StringComparison.OrdinalIgnoreCase))
                .WithMessage("Source and target environments must be different.");
            RuleFor(x => x.TotalDriftItems).GreaterThanOrEqualTo(0);
            RuleFor(x => x.CriticalDriftItems).GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(x => x.TotalDriftItems);
        }
    }

    /// <summary>Handler que gera relatório de drift entre ambientes.</summary>
    public sealed class Handler(
        IEnvironmentDriftReportRepository reportRepository,
        ICurrentTenant currentTenant,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = dateTimeProvider.UtcNow;

            // Mark previous reports as stale
            var previousReport = await reportRepository.GetLatestAsync(
                request.SourceEnvironment,
                request.TargetEnvironment,
                cancellationToken);

            if (previousReport is not null && previousReport.Status == DriftReportStatus.Generated)
            {
                previousReport.MarkAsStale();
                reportRepository.Update(previousReport);
            }

            // Determine overall severity
            var severity = request.CriticalDriftItems > 0
                ? DriftSeverity.Critical
                : request.TotalDriftItems > 5
                    ? DriftSeverity.High
                    : request.TotalDriftItems > 0
                        ? DriftSeverity.Medium
                        : DriftSeverity.Low;

            // Build analyzed dimensions list
            var dimensions = new List<string>();
            if (request.ServiceVersionDrifts is not null) dimensions.Add("ServiceVersions");
            if (request.ConfigurationDrifts is not null) dimensions.Add("Configurations");
            if (request.ContractVersionDrifts is not null) dimensions.Add("ContractVersions");
            if (request.DependencyDrifts is not null) dimensions.Add("Dependencies");
            if (request.PolicyDrifts is not null) dimensions.Add("Policies");
            if (dimensions.Count == 0) dimensions.Add("General");

            var analyzedDimensions = string.Join(",", dimensions);

            var report = EnvironmentDriftReport.Generate(
                request.SourceEnvironment,
                request.TargetEnvironment,
                analyzedDimensions,
                request.ServiceVersionDrifts,
                request.ConfigurationDrifts,
                request.ContractVersionDrifts,
                request.DependencyDrifts,
                request.PolicyDrifts,
                request.Recommendations,
                request.TotalDriftItems,
                request.CriticalDriftItems,
                severity,
                currentTenant.Id,
                now);

            reportRepository.Add(report);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                report.Id.Value,
                report.SourceEnvironment,
                report.TargetEnvironment,
                analyzedDimensions,
                report.TotalDriftItems,
                report.CriticalDriftItems,
                severity.ToString(),
                report.Status.ToString(),
                report.GeneratedAt));
        }
    }

    /// <summary>Resposta da geração de relatório de drift.</summary>
    public sealed record Response(
        Guid ReportId,
        string SourceEnvironment,
        string TargetEnvironment,
        string AnalyzedDimensions,
        int TotalDriftItems,
        int CriticalDriftItems,
        string OverallSeverity,
        string Status,
        DateTimeOffset GeneratedAt);
}
