# Configuration Module — Backend Functional Corrections

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 09 — Configuration  
> **Phase:** B1 — Module Consolidation

---

## 1. Endpoints Inventory

**File:** `src/modules/configuration/NexTraceOne.Configuration.API/Endpoints/ConfigurationEndpointModule.cs` (195 lines)

| # | Endpoint | Method | Permission | CQRS Handler | Status |
|---|----------|--------|-----------|--------------|--------|
| 1 | `/api/v1/configuration/definitions` | GET | `configuration:read` | GetDefinitions | ✅ Functional |
| 2 | `/api/v1/configuration/entries` | GET | `configuration:read` | GetEntries | ✅ Functional |
| 3 | `/api/v1/configuration/effective` | GET | `configuration:read` | GetEffectiveSettings | ✅ Functional |
| 4 | `/api/v1/configuration/{key}` | PUT | `configuration:write` | SetConfigurationValue | ✅ Functional |
| 5 | `/api/v1/configuration/{key}/override` | DELETE | `configuration:write` | RemoveOverride | ✅ Functional |
| 6 | `/api/v1/configuration/{key}/toggle` | POST | `configuration:write` | ToggleConfiguration | ✅ Functional |
| 7 | `/api/v1/configuration/{key}/audit` | GET | `configuration:read` | GetAuditHistory | ✅ Functional |

---

## 2. Endpoint → Use Case Mapping

| Endpoint | Use Case | Business Rule |
|----------|----------|---------------|
| GET definitions | List all configuration schemas | Returns all definitions ordered by SortOrder, then Key |
| GET entries | List concrete values at a scope | Masks sensitive values; scope+scopeReferenceId required |
| GET effective | Resolve effective value | Hierarchical resolution: User→Team→Role→Environment→Tenant→System→Default |
| PUT {key} | Set configuration value | Validates definition exists, scope allowed, encrypts if sensitive, creates audit trail |
| DELETE {key}/override | Remove scope override | Soft-deletes entry, creates audit trail, reverts to inherited value |
| POST {key}/toggle | Activate/deactivate config | Toggles IsActive flag, creates audit trail |
| GET {key}/audit | View change history | Returns audit entries ordered by ChangedAt descending, limit 50 default |

---

## 3. Dead Endpoints

**None identified.** All 7 endpoints have active CQRS handlers, are mapped in the endpoint module, and are consumed by the frontend.

---

## 4. Incomplete Endpoints

| # | Endpoint | Issue | Severity | Correction |
|---|----------|-------|----------|------------|
| B-01 | GET effective | Response shape is inconsistent: returns `{ setting: {...} }` for single key and `{ settings: [...] }` for all keys | LOW | Consider normalizing to always return an array |
| B-02 | GET entries | No pagination support — returns all entries for a scope | LOW | Add optional `page`/`pageSize` parameters for large tenants |
| B-03 | GET definitions | No pagination support — returns all 345+ definitions | LOW | Add optional filtering by category, prefix, or search term |
| B-04 | GET audit | Fixed limit parameter (default 50) — no proper pagination | LOW | Add cursor-based or offset-based pagination |

---

## 5. Missing Business Rules

| # | Rule | Impact | Priority | Handler |
|---|------|--------|----------|---------|
| B-05 | **No validation of value against ValueType** — a Boolean definition accepts any string | MEDIUM | HIGH | SetConfigurationValue |
| B-06 | **No validation against ValidationRules** — JSON schema rules exist on definitions but are not enforced at write time | MEDIUM | HIGH | SetConfigurationValue |
| B-07 | **No temporal window validation** — EffectiveFrom/EffectiveTo fields on Entry exist but no endpoint supports setting them | LOW | MEDIUM | SetConfigurationValue |
| B-08 | **No IsEditable enforcement** — definition has `IsEditable` flag but handler should reject writes to non-editable configs | MEDIUM | HIGH | SetConfigurationValue |

---

## 6. Validation Review

| Handler | Validation | Status |
|---------|-----------|--------|
| SetConfigurationValue | Key not empty (max 256) | ✅ |
| SetConfigurationValue | Value max length 4000 | ✅ |
| SetConfigurationValue | ScopeReferenceId max 256 | ✅ |
| SetConfigurationValue | ChangeReason max 500 | ✅ |
| SetConfigurationValue | Definition exists | ✅ |
| SetConfigurationValue | Scope is in AllowedScopes | ✅ |
| SetConfigurationValue | **Value matches ValueType** | ❌ Missing |
| SetConfigurationValue | **Value passes ValidationRules** | ❌ Missing |
| SetConfigurationValue | **Definition.IsEditable is true** | ❌ Missing |
| ToggleConfiguration | Entry exists | ✅ |
| ToggleConfiguration | Not already in target state | ✅ |
| RemoveOverride | Entry exists | ✅ |

---

## 7. Error Handling Review

| Aspect | Status | Notes |
|--------|--------|-------|
| Multi-language errors | ✅ | Uses `IErrorLocalizer` |
| Enum parsing errors | ✅ | Returns 422 Unprocessable Entity |
| Definition not found | ✅ | Returns specific error code |
| Entry not found | ✅ | Returns specific error code |
| Validation failures | ✅ | FluentValidation integration |
| Unexpected exceptions | ✅ | Global exception handler in pipeline |
| **Concurrency conflicts** | ❌ | No DbUpdateConcurrencyException handling (no RowVersion yet) |

---

## 8. Authorization Review

| Endpoint | Permission | Enforcement | Status |
|----------|-----------|-------------|--------|
| GET definitions | `configuration:read` | Endpoint-level `RequirePermission` | ✅ |
| GET entries | `configuration:read` | Endpoint-level `RequirePermission` | ✅ |
| GET effective | `configuration:read` | Endpoint-level `RequirePermission` | ✅ |
| PUT {key} | `configuration:write` | Endpoint-level `RequirePermission` | ✅ |
| DELETE {key}/override | `configuration:write` | Endpoint-level `RequirePermission` | ✅ |
| POST {key}/toggle | `configuration:write` | Endpoint-level `RequirePermission` | ✅ |
| GET {key}/audit | `configuration:read` | Endpoint-level `RequirePermission` | ✅ |

**Note:** Only 2 permissions exist (`configuration:read`, `configuration:write`). No domain-specific granularity (e.g., `configuration:ai:write`). See Security review for full analysis.

---

## 9. Audit Trail Review

| Operation | Audit Entry Created | Action Recorded | Status |
|-----------|-------------------|-----------------|--------|
| Set value (new) | ✅ | "Created" | ✅ |
| Set value (update) | ✅ | "Updated" | ✅ |
| Toggle activate | ✅ | "Activated" | ✅ |
| Toggle deactivate | ✅ | "Deactivated" | ✅ |
| Remove override | ✅ | "Removed" | ✅ |
| Get definitions | N/A | Read-only | ✅ |
| Get entries | N/A | Read-only | ✅ |
| Get effective | N/A | Read-only | ✅ |
| Get audit | N/A | Read-only | ✅ |

**All write operations create audit entries.** ✅

---

## 10. Request/Response Coherence

| Endpoint | Request Shape | Response Shape | Coherent |
|----------|-------------|---------------|----------|
| GET definitions | — | `ConfigurationDefinitionDto[]` | ✅ |
| GET entries | `scope`, `scopeReferenceId` | `ConfigurationEntryDto[]` | ✅ |
| GET effective | `key?`, `scope`, `scopeReferenceId` | `{ setting?, settings? }` | ⚠️ Inconsistent |
| PUT {key} | `SetConfigurationValueRequest` | `ConfigurationEntryDto` | ✅ |
| DELETE {key}/override | `scope`, `scopeReferenceId?`, `changeReason?` | `bool` | ✅ |
| POST {key}/toggle | `ToggleConfigurationRequest` | `bool` | ✅ |
| GET {key}/audit | `limit?` | `ConfigurationAuditEntryDto[]` | ✅ |

---

## 11. Integration Events Review

**Defined events** (in `ConfigurationIntegrationEvents.cs`):
- `ConfigurationValueChanged(Key, Scope, ScopeReferenceId, PreviousValue?, NewValue?, ChangedBy)`
- `ConfigurationValueActivated(Key, Scope, ScopeReferenceId)`
- `ConfigurationValueDeactivated(Key, Scope, ScopeReferenceId)`

**Issue:** Events are defined but their publishing in handlers needs verification. If outbox pattern is used, events should be added to the aggregate before `CommitAsync`.

---

## 12. Corrections Backlog

### HIGH Priority

| # | Correction | Handler | Effort |
|---|-----------|---------|--------|
| B-05 | Add value type validation (Boolean→true/false, Integer→parseable, etc.) | SetConfigurationValue | 2h |
| B-06 | Add ValidationRules enforcement (if JSON schema present) | SetConfigurationValue | 3h |
| B-08 | Add IsEditable check — reject writes to non-editable definitions | SetConfigurationValue | 30min |

### MEDIUM Priority

| # | Correction | Handler | Effort |
|---|-----------|---------|--------|
| B-07 | Support EffectiveFrom/EffectiveTo in SetConfigurationValue request | SetConfigurationValue | 2h |
| B-09 | Add concurrency conflict handling (once RowVersion is added) | All write handlers | 1h |
| B-10 | Verify integration events are published via outbox | All write handlers | 1h |

### LOW Priority

| # | Correction | Handler | Effort |
|---|-----------|---------|--------|
| B-01 | Normalize GET effective response shape | GetEffectiveSettings | 1h |
| B-02 | Add pagination to GET entries | GetEntries | 1h |
| B-03 | Add filtering to GET definitions | GetDefinitions | 1h |
| B-04 | Add proper pagination to GET audit | GetAuditHistory | 1h |
