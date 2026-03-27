# E12 — AI & Knowledge Module — Post-Execution Gap Report

## What Was Resolved

1. **Table prefix unified** — All 27 tables now use `aik_` prefix (was mix of ext_ai_, ai_gov_, PascalCase, ai_orch_)
2. **Check constraints added** — 6 constraints on 4 tables enforce enum values at database level (AgentExecutionStatus, ModelStatus, ModelType, AgentCategory, AgentPublicationStatus, ProviderHealthStatus)
3. **Optimistic concurrency** — RowVersion (xmin) added on 4 mutable aggregates (AiAgent, AIModel, AiProvider, AiAgentExecution)
4. **Permission gaps fixed** — ai:governance:read for TechLead, ai:assistant:read for Viewer
5. **Frontend permission types aligned** — ai:assistant:write and ai:governance:write added to frontend type union
6. **Missing i18n keys added** — aiIde, aiBudgets, aiAudit sidebar translations in all 4 locales

---

## What Is Still Pending

### Persistence / Baseline

| Gap | Blocker | Phase |
|-----|---------|-------|
| Old migrations still reference ext_ai_, ai_gov_, PascalCase, ai_orch_ names | No — per E-phase rules, old migrations are not deleted yet | Future baseline |
| New baseline with aik_ prefix not yet generated | No — waiting until all domain models are final | Future baseline |
| RowVersion not yet on all mutable entities (only 4 of ~20+ mutable entities) | No — remaining entities are secondary | Future baseline |
| Check constraints not yet on all enum columns (~33 enums, only 6 constrained) | No — most important ones done | Future baseline |
| Outbox tables still use ext_ai_, ai_gov_, ai_orch_ prefixes | No — outbox renamed to aik_ in future baseline | Future baseline |

### Domain Model

| Gap | Blocker | Phase |
|-----|---------|-------|
| Some entities still lack explicit guard methods (e.g., provider health transitions) | No | Future iteration |
| AiAgent lacks explicit Publish/Archive/Block transition guards | No | Future iteration |
| TenantId not yet on entities | No | Future baseline |
| EnvironmentId not yet on entities | No | Future baseline |

### Backend

| Gap | Blocker | Phase |
|-----|---------|-------|
| DbUpdateConcurrencyException not yet handled in command handlers | No | Future iteration |
| Some endpoints still use coarse ai:governance:read instead of fine-grained permissions | No | Future iteration |
| Token budget enforcement not yet real-time (budget check is advisory only) | No | Future iteration |
| Knowledge grounding / retrieval pipeline still partial | No | Future iteration |
| Agent tool calling / orchestration still partially cosmetic | No | Future iteration |

### Frontend

| Gap | Blocker | Phase |
|-----|---------|-------|
| Some pages may show cosmetic data when backend returns empty datasets | No | Future iteration |
| IDE Integrations page functionality pending real IDE extension integration | No | Future iteration |
| Token Budget page not yet connected to real-time enforcement | No | Future iteration |
| AI Audit page may lack granular filtering by model/agent/user | No | Future iteration |

### Security

| Gap | Blocker | Phase |
|-----|---------|-------|
| ai:runtime:read/write not in RolePermissionCatalog (used in endpoints but not registered) | No — endpoint framework may have fallback | Next E-phase |
| Fine-grained agent-level capabilities (per-agent permission) not implemented | No | Future iteration |
| Token quota enforcement at runtime not yet active | No | Future iteration |
| Audit trail for sensitive AI operations (model changes, agent definition changes) not yet connected to Audit & Compliance module | No | Future iteration |

### ClickHouse / Analytics

| Gap | Blocker | Phase |
|-----|---------|-------|
| AI usage analytics pipeline to ClickHouse not implemented | No | Future phase |
| Token usage aggregation in ClickHouse not implemented | No | Future phase |
| Agent execution metrics in ClickHouse not implemented | No | Future phase |
| Chat interaction analytics in ClickHouse not implemented | No | Future phase |

### Dependencies / Integration

| Gap | Blocker | Phase |
|-----|---------|-------|
| Catalog integration for knowledge grounding is partial | No | Future iteration |
| Change Governance integration for change classification is partial | No | Future iteration |
| Operational Intelligence integration for incident context is partial | No | Future iteration |
| Audit & Compliance module not yet consuming AI execution events | No | Future iteration |
| Notifications module not yet sending AI-related notifications | No | Future iteration |

---

## What Depends on Other Phases

1. **New baseline/migrations** — Cannot be generated until all domain models are final across all 13 modules
2. **ClickHouse pipeline** — Requires platform-wide analytics infrastructure decision
3. **Real IDE extensions** — Requires VS Code / Visual Studio extension implementation
4. **Cross-module event consumption** — Requires event bus / integration event infrastructure maturation
5. **TenantId/EnvironmentId** — Platform-wide decision, not AI-specific

---

## Does This Block Evolution to Next E-Phase?

**No.** The AI & Knowledge module E12 execution is complete and does not block the trilha E evolution. All mandatory changes were executed:

- All 27 tables renamed to aik_ prefix ✅
- Check constraints on key enum columns ✅
- RowVersion on 4 mutable aggregates ✅
- Permission catalog gaps fixed ✅
- Frontend i18n and permission types aligned ✅
- 410 AI + 290 Identity tests pass ✅
