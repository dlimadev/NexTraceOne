# NexTraceOne — Official Notification Event Catalog

> **Status:** COMPLETE  
> **Date:** 2026-03-23  
> **Phase:** 0 — Foundation

---

## Overview

This document defines the official catalog of product events that can generate notifications in the NexTraceOne platform. Events are organized by functional family, with each event mapped to its category, recommended severity, target recipients, and eligible delivery channels.

### Legend

| Column | Description |
|---|---|
| **Event** | Semantic name of the event |
| **Category** | `NotificationCategory` enum value |
| **Default Severity** | Default `NotificationSeverity` (may vary by context) |
| **Recipients** | Who should receive this notification |
| **Channels** | Default eligible channels (InApp always included) |
| **Requires Action** | Whether the notification requires explicit acknowledgment |

---

## 1. Operations & Incidents

| Event | Category | Default Severity | Recipients | Channels | Requires Action |
|---|---|---|---|---|---|
| `IncidentCreated` | Incident | Critical | Service owner, ops team | InApp, Email, Teams | Yes |
| `IncidentSeverityChanged` | Incident | Warning | Service owner, assigned team | InApp, Email | No |
| `IncidentAssigned` | Incident | ActionRequired | Assigned user | InApp, Email | Yes |
| `IncidentEscalated` | Incident | Critical | Escalation target, tenant admins | InApp, Email, Teams | Yes |
| `IncidentResolved` | Incident | Info | Service owner, assigned team | InApp | No |
| `ServiceDegraded` | Incident | Warning | Service owner, ops team | InApp, Email, Teams | No |
| `ServiceHealthCritical` | Incident | Critical | Service owner, ops team, tenant admins | InApp, Email, Teams | Yes |
| `RuntimeAnomalyDetected` | Incident | Warning | Service owner, ops team | InApp, Email | No |
| `DriftDetected` | Incident | Warning | Service owner, platform admins | InApp, Email | No |
| `JobWorkerFailed` | Platform | Warning | Platform admins | InApp, Email | No |
| `PipelineFailed` | Platform | Warning | Platform admins, service owner | InApp, Email | No |
| `BackupFailed` | Platform | Critical | Platform admins | InApp, Email, Teams | Yes |
| `RestoreExecuted` | Platform | Info | Platform admins | InApp | No |

---

## 2. Approvals & Workflow

| Event | Category | Default Severity | Recipients | Channels | Requires Action |
|---|---|---|---|---|---|
| `ApprovalPending` | Approval | ActionRequired | Designated approver | InApp, Email | Yes |
| `ApprovalRejected` | Approval | Warning | Requester, service owner | InApp, Email | No |
| `ApprovalExpiringIn24h` | Approval | Warning | Designated approver | InApp, Email | Yes |
| `ApprovalExpired` | Approval | Warning | Requester, designated approver | InApp, Email | No |
| `ReleaseAwaitingApproval` | Approval | ActionRequired | Designated approver | InApp, Email | Yes |
| `WaiverPending` | Approval | ActionRequired | Governance team | InApp, Email | Yes |
| `AccessReviewPending` | Security | ActionRequired | Designated reviewer | InApp, Email | Yes |
| `JITAccessPending` | Security | ActionRequired | Designated approver | InApp, Email | Yes |

---

## 3. Catalog & Contracts

| Event | Category | Default Severity | Recipients | Channels | Requires Action |
|---|---|---|---|---|---|
| `ContractPublished` | Contract | Info | Service owner, consumers | InApp | No |
| `ContractNewVersion` | Contract | Info | Service owner, consumers | InApp, Email | No |
| `BreakingChangeDetected` | Contract | Critical | Service owner, consumers, architect | InApp, Email, Teams | Yes |
| `ContractExpiring` | Contract | Warning | Service owner | InApp, Email | No |
| `ContractValidationFailed` | Contract | Warning | Service owner | InApp, Email | No |
| `PortalPublicationCompleted` | Contract | Info | Service owner | InApp | No |

---

## 4. Security & Access

| Event | Category | Default Severity | Recipients | Channels | Requires Action |
|---|---|---|---|---|---|
| `BreakGlassActivated` | Security | Critical | Tenant admins, security team | InApp, Email, Teams | Yes |
| `JITAccessGranted` | Security | Info | Requester, approver | InApp | No |
| `SecretExpiringIn7Days` | Security | Warning | Service owner, security team | InApp, Email | No |
| `SecretExpiringIn24h` | Security | ActionRequired | Service owner, security team | InApp, Email, Teams | Yes |
| `RepeatedLoginFailures` | Security | Warning | Tenant admins, security team | InApp, Email | No |
| `OIDCProviderUnavailable` | Security | Critical | Platform admins | InApp, Email, Teams | Yes |

---

## 5. FinOps & Governance

| Event | Category | Default Severity | Recipients | Channels | Requires Action |
|---|---|---|---|---|---|
| `BudgetThreshold80` | FinOps | Warning | Service owner, budget owner | InApp, Email | No |
| `BudgetThreshold90` | FinOps | ActionRequired | Service owner, budget owner, manager | InApp, Email | Yes |
| `BudgetThreshold100` | FinOps | Critical | Service owner, budget owner, manager, tenant admins | InApp, Email, Teams | Yes |
| `CostAnomalyDetected` | FinOps | Warning | Service owner, FinOps team | InApp, Email | No |
| `WasteDetected` | FinOps | Info | Service owner, FinOps team | InApp | No |
| `EvidenceExpired` | Compliance | Warning | Governance team, service owner | InApp, Email | No |
| `PolicyViolated` | Compliance | ActionRequired | Service owner, governance team | InApp, Email | Yes |
| `ComplianceCheckFailed` | Compliance | Warning | Service owner, governance team | InApp, Email | No |

---

## 6. AI & AI Governance

| Event | Category | Default Severity | Recipients | Channels | Requires Action |
|---|---|---|---|---|---|
| `AIProviderUnavailable` | AI | Critical | Platform admins, AI governance team | InApp, Email, Teams | Yes |
| `TokenBudgetExceeded` | AI | ActionRequired | User, AI governance team | InApp, Email | Yes |
| `AIPolicyChanged` | AI | Info | AI users, AI governance team | InApp | No |
| `AIDraftCompleted` | AI | Info | Requester | InApp | No |
| `AIDraftFailed` | AI | Warning | Requester | InApp | No |
| `AIUsageBlockedByPolicy` | AI | Warning | User, AI governance team | InApp | No |

---

## 7. Integrations

| Event | Category | Default Severity | Recipients | Channels | Requires Action |
|---|---|---|---|---|---|
| `SyncFailed` | Integration | Warning | Integration owner, platform admins | InApp, Email | No |
| `ConnectorAuthFailed` | Integration | ActionRequired | Integration owner, platform admins | InApp, Email | Yes |
| `IngestionNoRecentData` | Integration | Warning | Integration owner | InApp, Email | No |
| `WebhookRejected` | Integration | Warning | Integration owner | InApp | No |

---

## 8. Changes & Releases

| Event | Category | Default Severity | Recipients | Channels | Requires Action |
|---|---|---|---|---|---|
| `ReleasePublished` | Change | Info | Service owner, team | InApp | No |
| `PromotionRegistered` | Change | Info | Service owner, ops team | InApp | No |
| `DeploymentReceived` | Change | Info | Service owner | InApp | No |
| `WorkflowApproved` | Change | Info | Requester, service owner | InApp | No |
| `WorkflowRejected` | Change | Warning | Requester, service owner | InApp, Email | No |

---

## 9. Mapping to Existing Domain Events

The following existing domain/integration events in the codebase are natural notification sources:

| Existing Event | Module | Maps To |
|---|---|---|
| `PromotionRegisteredEvent` | ChangeGovernance | `PromotionRegistered` |
| `WorkflowApprovedEvent` | ChangeGovernance | `WorkflowApproved` |
| `WorkflowRejectedEvent` | ChangeGovernance | `WorkflowRejected` |
| `DeploymentEventReceivedEvent` | ChangeGovernance | `DeploymentReceived` |
| `ReleasePublishedEvent` | ChangeGovernance | `ReleasePublished` |
| `CostAnomalyDetectedEvent` | OperationalIntelligence | `CostAnomalyDetected` |
| `RuntimeAnomalyDetectedEvent` | OperationalIntelligence | `RuntimeAnomalyDetected` |
| `RuntimeSignalReceivedEvent` | OperationalIntelligence | (conditional, based on severity) |
| `UserCreatedDomainEvent` | IdentityAccess | (informational, low priority) |
| `UserLockedDomainEvent` | IdentityAccess | `RepeatedLoginFailures` |
| `KnowledgeCandidateCreatedEvent` | AIKnowledge | (internal, not user-facing) |
| `ExternalAIQueryRequestedEvent` | AIKnowledge | (audit only) |
| `ExternalAIResponseReceivedEvent` | AIKnowledge | (audit only) |
| `AuditEventRecordedEvent` | AuditCompliance | (audit only, not notification) |
| `RiskReportGenerated` | Governance | (informational) |
| `ComplianceGapsDetected` | Governance | `ComplianceCheckFailed` |

---

## 10. Event Catalog Statistics

| Family | Event Count |
|---|---|
| Operations & Incidents | 13 |
| Approvals & Workflow | 8 |
| Catalog & Contracts | 6 |
| Security & Access | 6 |
| FinOps & Governance | 8 |
| AI & AI Governance | 6 |
| Integrations | 4 |
| Changes & Releases | 5 |
| **Total** | **56** |

---

## 11. Event Type Naming Convention

All event types follow the pattern: `{Entity}{Action}` in PascalCase.

Examples:
- `IncidentCreated`
- `ApprovalPending`
- `BreakingChangeDetected`
- `BudgetThreshold80`
- `AIProviderUnavailable`

This convention ensures consistency and traceability between the event catalog and the `EventType` field in the `Notification` entity.
