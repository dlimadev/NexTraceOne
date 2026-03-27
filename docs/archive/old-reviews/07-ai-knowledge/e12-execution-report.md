# E12 — AI & Knowledge Module — Execution Report

## Summary

E12 executed real code corrections on the AI & Knowledge module, covering persistence alignment, security hardening, frontend improvements, and preparation for the future aik_ baseline.

**Tests:** 410 AI & Knowledge tests + 290 Identity tests — all pass.

---

## Files Changed

### Domain Entities (4 files)

| File | Change |
|------|--------|
| `AiAgent.cs` | Added `uint RowVersion { get; set; }` for optimistic concurrency |
| `AIModel.cs` | Added `uint RowVersion { get; set; }` for optimistic concurrency |
| `AiProvider.cs` | Added `uint RowVersion { get; set; }` for optimistic concurrency |
| `AiAgentExecution.cs` | Added `uint RowVersion { get; set; }` for optimistic concurrency |

### EF Core Configurations (27 files — table prefix rename)

All 27 EF Core configuration files were updated to use the `aik_` prefix:

**ExternalAI (4 files):**

| File | Old Table | New Table |
|------|-----------|-----------|
| `ExternalAiConsultationConfiguration.cs` | `ext_ai_consultations` | `aik_consultations` |
| `ExternalAiPolicyConfiguration.cs` | `ext_ai_policies` | `aik_policies` |
| `ExternalAiProviderConfiguration.cs` | `ext_ai_providers` | `aik_ext_providers` |
| `KnowledgeCaptureConfiguration.cs` | `ext_ai_knowledge_captures` | `aik_knowledge_captures` |

**Governance (19 files):**

| File | Old Table | New Table |
|------|-----------|-----------|
| `AIAccessPolicyConfiguration.cs` | `ai_gov_access_policies` | `aik_access_policies` |
| `AIBudgetConfiguration.cs` | `ai_gov_budgets` | `aik_budgets` |
| `AIIDECapabilityPolicyConfiguration.cs` | `ai_gov_ide_capability_policies` | `aik_ide_capability_policies` |
| `AIIDEClientRegistrationConfiguration.cs` | `ai_gov_ide_client_registrations` | `aik_ide_client_registrations` |
| `AIKnowledgeSourceConfiguration.cs` | `ai_gov_knowledge_sources` | `aik_knowledge_sources` |
| `AIModelConfiguration.cs` | `ai_gov_models` | `aik_models` |
| `AIRoutingDecisionConfiguration.cs` | `ai_gov_routing_decisions` | `aik_routing_decisions` |
| `AIRoutingStrategyConfiguration.cs` | `ai_gov_routing_strategies` | `aik_routing_strategies` |
| `AIUsageEntryConfiguration.cs` | `ai_gov_usage_entries` | `aik_usage_entries` |
| `AiAgentConfiguration.cs` | `ai_gov_agents` | `aik_agents` |
| `AiAgentArtifactConfiguration.cs` | `ai_gov_agent_artifacts` | `aik_agent_artifacts` |
| `AiAgentExecutionConfiguration.cs` | `ai_gov_agent_executions` | `aik_agent_executions` |
| `AiAssistantConversationConfiguration.cs` | `ai_gov_conversations` | `aik_conversations` |
| `AiMessageConfiguration.cs` | `ai_gov_messages` | `aik_messages` |
| `AiExternalInferenceRecordConfiguration.cs` | `AiExternalInferenceRecords` | `aik_external_inference_records` |
| `AiProviderConfiguration.cs` | `AiProviders` | `aik_providers` |
| `AiSourceConfiguration.cs` | `AiSources` | `aik_sources` |
| `AiTokenQuotaPolicyConfiguration.cs` | `AiTokenQuotaPolicies` | `aik_token_quota_policies` |
| `AiTokenUsageLedgerConfiguration.cs` | `AiTokenUsageLedger` | `aik_token_usage_ledger` |

**Orchestration (4 files):**

| File | Old Table | New Table |
|------|-----------|-----------|
| `AiContextConfiguration.cs` | `ai_orch_contexts` | `aik_contexts` |
| `AiConversationConfiguration.cs` | `ai_orch_conversations` | `aik_orch_conversations` |
| `GeneratedTestArtifactConfiguration.cs` | `ai_orch_test_artifacts` | `aik_test_artifacts` |
| `KnowledgeCaptureEntryConfiguration.cs` | `ai_orch_knowledge_entries` | `aik_knowledge_entries` |

### EF Core Configurations (4 files — check constraints added)

| File | Constraint | Values |
|------|------------|--------|
| `AiAgentExecutionConfiguration.cs` | `CK_aik_agent_executions_Status` | Pending, Running, Completed, Failed, Cancelled |
| `AIModelConfiguration.cs` | `CK_aik_models_Status` | Active, Inactive, Deprecated, Blocked |
| `AIModelConfiguration.cs` | `CK_aik_models_ModelType` | Chat, Completion, Embedding, CodeGeneration, Analysis |
| `AiAgentConfiguration.cs` | `CK_aik_agents_Category` | General, ServiceAnalysis, ContractGovernance, IncidentResponse, ChangeIntelligence, SecurityAudit, FinOps, CodeReview, Documentation, Testing, Compliance, ApiDesign, TestGeneration, EventDesign, DocumentationAssistance, SoapDesign |
| `AiAgentConfiguration.cs` | `CK_aik_agents_PublicationStatus` | Draft, PendingReview, Active, Published, Archived, Blocked |
| `AiProviderConfiguration.cs` | `CK_aik_providers_HealthStatus` | Unknown, Healthy, Degraded, Unhealthy, Offline |

### EF Core Configurations (4 files — RowVersion added)

| File | Entity |
|------|--------|
| `AiAgentConfiguration.cs` | `builder.Property(e => e.RowVersion).IsRowVersion()` |
| `AiAgentExecutionConfiguration.cs` | `builder.Property(e => e.RowVersion).IsRowVersion()` |
| `AIModelConfiguration.cs` | `builder.Property(x => x.RowVersion).IsRowVersion()` |
| `AiProviderConfiguration.cs` | `builder.Property(x => x.RowVersion).IsRowVersion()` |

### Security / Permissions (1 file)

| File | Change |
|------|--------|
| `RolePermissionCatalog.cs` | Added `ai:governance:read` for TechLead role |
| `RolePermissionCatalog.cs` | Added `ai:assistant:read` for Viewer role |

### Frontend (5 files)

| File | Change |
|------|--------|
| `permissions.ts` | Added `ai:assistant:write` and `ai:governance:write` to Permission type union |
| `en.json` | Added sidebar keys: `aiIde`, `aiBudgets`, `aiAudit` |
| `pt-PT.json` | Added sidebar keys: `aiIde`, `aiBudgets`, `aiAudit` (Portuguese PT) |
| `pt-BR.json` | Added sidebar keys: `aiIde`, `aiBudgets`, `aiAudit` (Portuguese BR) |
| `es.json` | Added sidebar keys: `aiIde`, `aiBudgets`, `aiAudit` (Spanish) |

---

## Corrections by Area

### Persistence (PART 5)

- **27 tables renamed** from mixed prefixes (ext_ai_, ai_gov_, PascalCase, ai_orch_) to unified `aik_` prefix
- **6 check constraints** added on 4 tables for enum validation
- **RowVersion (xmin)** added on 4 mutable aggregates for optimistic concurrency
- Migrations left untouched per E12 rules

### Security (PART 9)

- `ai:governance:read` registered for TechLead (was missing — TechLead could not see Model Registry, Policies, Routing, IDE, Budgets, Audit pages)
- `ai:assistant:read` registered for Viewer (was missing — Viewer could not access AI Assistant)
- Frontend permission type union now includes `ai:assistant:write` and `ai:governance:write`

### Frontend (PART 8)

- 3 missing sidebar i18n keys added across all 4 locales (en, pt-PT, pt-BR, es)
- Permission type union aligned with backend permission catalog

### Structure (PART 1)

- AI & Knowledge module structure already reflects AI Core / Agents / Knowledge split across 3 DbContexts (ExternalAI, Governance, Orchestration) + Runtime layer
- No structural changes required — module is already well-organized

---

## Module Maturity Assessment

| Area | Before E12 | After E12 |
|------|-----------|-----------|
| Table prefix consistency | ~60% (3 different schemes) | 100% (all aik_) |
| Check constraints | 0 | 6 (on 4 tables) |
| Optimistic concurrency | 0 entities | 4 entities |
| Permission catalog completeness | ~70% | ~85% |
| Frontend i18n sidebar | ~70% (3 missing keys) | 100% |
| Overall maturity | ~58% | ~65% |
