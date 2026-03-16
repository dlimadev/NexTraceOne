using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Automation;

/// <summary>
/// Catálogo estático de ações de automação operacional disponíveis.
/// Centraliza os dados simulados para reutilização entre features (MVP).
/// </summary>
internal static class AutomationActionCatalog
{
    /// <summary>Item do catálogo de ações de automação.</summary>
    internal sealed record ActionItem(
        string ActionId,
        string Name,
        string DisplayName,
        string Description,
        AutomationActionType ActionType,
        RiskLevel RiskLevel,
        bool RequiresApproval,
        IReadOnlyList<string> AllowedPersonas,
        IReadOnlyList<string> AllowedEnvironments,
        IReadOnlyList<PreconditionType> PreconditionTypes,
        bool HasPostValidation);

    /// <summary>Retorna o catálogo completo de ações de automação.</summary>
    internal static IReadOnlyList<ActionItem> GetAll()
    {
        return new List<ActionItem>
        {
            new("action-restart-controlled",
                "RestartControlled",
                "Controlled Service Restart",
                "Performs a controlled restart of the affected service with pre and post health checks.",
                AutomationActionType.RestartControlled,
                RiskLevel.Medium,
                RequiresApproval: true,
                AllowedPersonas: new[] { "Engineer", "TechLead" },
                AllowedEnvironments: new[] { "Staging", "Production" },
                PreconditionTypes: new[] { PreconditionType.ServiceHealthCheck, PreconditionType.ApprovalPresence, PreconditionType.BlastRadiusConstraint },
                HasPostValidation: true),

            new("action-reprocess-controlled",
                "ReprocessControlled",
                "Controlled Event Reprocessing",
                "Reprocesses failed events or operations with controlled throughput and monitoring.",
                AutomationActionType.ReprocessControlled,
                RiskLevel.Medium,
                RequiresApproval: true,
                AllowedPersonas: new[] { "Engineer", "TechLead" },
                AllowedEnvironments: new[] { "Staging", "Production" },
                PreconditionTypes: new[] { PreconditionType.ServiceHealthCheck, PreconditionType.ApprovalPresence, PreconditionType.CooldownPeriod },
                HasPostValidation: true),

            new("action-execute-runbook-step",
                "ExecuteRunbookStep",
                "Execute Runbook Step",
                "Executes a predefined operational runbook step associated with the service or incident.",
                AutomationActionType.ExecuteRunbookStep,
                RiskLevel.Low,
                RequiresApproval: false,
                AllowedPersonas: new[] { "Engineer", "TechLead", "Architect" },
                AllowedEnvironments: new[] { "Development", "Staging", "Production" },
                PreconditionTypes: new[] { PreconditionType.ServiceHealthCheck },
                HasPostValidation: false),

            new("action-rollback-readiness",
                "RollbackReadinessReview",
                "Rollback Readiness Review",
                "Performs a readiness assessment for rolling back a recent production change.",
                AutomationActionType.RollbackReadinessReview,
                RiskLevel.High,
                RequiresApproval: true,
                AllowedPersonas: new[] { "TechLead", "Architect" },
                AllowedEnvironments: new[] { "Staging", "Production" },
                PreconditionTypes: new[] { PreconditionType.ApprovalPresence, PreconditionType.BlastRadiusConstraint, PreconditionType.OwnerConfirmation, PreconditionType.ContractRiskAwareness },
                HasPostValidation: true),

            new("action-observe-validate",
                "ObserveAndValidate",
                "Observe and Validate",
                "Observes service metrics and validates operational stability after an action or change.",
                AutomationActionType.ObserveAndValidate,
                RiskLevel.Low,
                RequiresApproval: false,
                AllowedPersonas: new[] { "Engineer", "TechLead", "Architect" },
                AllowedEnvironments: new[] { "Development", "Staging", "Production" },
                PreconditionTypes: new[] { PreconditionType.ServiceHealthCheck },
                HasPostValidation: true),

            new("action-escalate-context",
                "EscalateWithContext",
                "Escalate with Operational Context",
                "Escalates the issue to the next level with full operational context including incident, service and change data.",
                AutomationActionType.EscalateWithContext,
                RiskLevel.Low,
                RequiresApproval: false,
                AllowedPersonas: new[] { "Engineer", "TechLead", "Architect", "Product" },
                AllowedEnvironments: new[] { "Development", "Staging", "Production" },
                PreconditionTypes: Array.Empty<PreconditionType>(),
                HasPostValidation: false),

            new("action-verify-dependency",
                "VerifyDependencyState",
                "Verify Dependency State",
                "Checks the health and availability of service dependencies before or after an operational action.",
                AutomationActionType.VerifyDependencyState,
                RiskLevel.Low,
                RequiresApproval: false,
                AllowedPersonas: new[] { "Engineer", "TechLead", "Architect" },
                AllowedEnvironments: new[] { "Development", "Staging", "Production" },
                PreconditionTypes: new[] { PreconditionType.DependencyStateCheck },
                HasPostValidation: true),

            new("action-validate-post-change",
                "ValidatePostChangeState",
                "Validate Post-Change State",
                "Validates the operational state of a service after a production change including contract compliance and metric baselines.",
                AutomationActionType.ValidatePostChangeState,
                RiskLevel.Medium,
                RequiresApproval: true,
                AllowedPersonas: new[] { "Engineer", "TechLead" },
                AllowedEnvironments: new[] { "Staging", "Production" },
                PreconditionTypes: new[] { PreconditionType.ServiceHealthCheck, PreconditionType.EnvironmentRestriction, PreconditionType.ContractRiskAwareness },
                HasPostValidation: true),
        };
    }
}
