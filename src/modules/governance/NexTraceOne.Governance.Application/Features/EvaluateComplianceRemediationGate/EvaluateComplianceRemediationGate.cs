using Ardalis.GuardClauses;
using System.Text.Json;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.EvaluateComplianceRemediationGate;

/// <summary>
/// Feature: EvaluateComplianceRemediationGate — avalia se auto-remediação de compliance está ativa
/// e se a violação é elegível para remediação automática.
/// Consulta:
///   - governance.compliance.auto_remediation.enabled: se auto-remediação está ativa
///   - governance.compliance.framework: frameworks ativos
/// Pilar: Governance + Compliance
/// </summary>
public static class EvaluateComplianceRemediationGate
{
    /// <summary>Query para avaliar elegibilidade de auto-remediação.</summary>
    public sealed record Query(
        string ViolationType,
        string ServiceName,
        string Severity) : IQuery<Response>;

    /// <summary>Handler que avalia elegibilidade de auto-remediação.</summary>
    public sealed class Handler(
        IConfigurationResolutionService configService,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Check auto-remediation enabled
            var autoRemConfig = await configService.ResolveEffectiveValueAsync(
                "governance.compliance.auto_remediation.enabled",
                ConfigurationScope.Tenant, null, cancellationToken);

            var autoRemEnabled = autoRemConfig?.EffectiveValue == "true";

            // Get active frameworks
            var frameworkConfig = await configService.ResolveEffectiveValueAsync(
                "governance.compliance.framework",
                ConfigurationScope.Tenant, null, cancellationToken);

            var frameworks = new List<string>();
            if (frameworkConfig?.EffectiveValue is not null)
            {
                try
                {
                    frameworks = JsonSerializer.Deserialize<List<string>>(frameworkConfig.EffectiveValue) ?? [];
                }
                catch
                {
                    frameworks = ["internal"];
                }
            }

            if (!autoRemEnabled)
            {
                return new Response(
                    ViolationType: request.ViolationType,
                    ServiceName: request.ServiceName,
                    Severity: request.Severity,
                    AutoRemediationEnabled: false,
                    IsEligibleForAutoRemediation: false,
                    ActiveFrameworks: frameworks,
                    Reason: "Auto-remediation is not enabled",
                    EvaluatedAt: dateTimeProvider.UtcNow);
            }

            // Only Low/Medium severity violations are eligible for auto-remediation
            var isSeverityEligible = request.Severity is "Low" or "Medium";

            return new Response(
                ViolationType: request.ViolationType,
                ServiceName: request.ServiceName,
                Severity: request.Severity,
                AutoRemediationEnabled: true,
                IsEligibleForAutoRemediation: isSeverityEligible,
                ActiveFrameworks: frameworks,
                Reason: isSeverityEligible
                    ? $"Violation '{request.ViolationType}' is eligible for auto-remediation (severity: {request.Severity})"
                    : $"Violation '{request.ViolationType}' is too severe for auto-remediation (severity: {request.Severity}). Manual review required.",
                EvaluatedAt: dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta da avaliação de auto-remediação.</summary>
    public sealed record Response(
        string ViolationType,
        string ServiceName,
        string Severity,
        bool AutoRemediationEnabled,
        bool IsEligibleForAutoRemediation,
        List<string> ActiveFrameworks,
        string Reason,
        DateTimeOffset EvaluatedAt);
}
