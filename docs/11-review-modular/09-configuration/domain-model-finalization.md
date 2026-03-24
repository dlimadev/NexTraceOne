# Configuration Module — Domain Model Finalization

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 09 — Configuration  
> **Phase:** B1 — Module Consolidation

---

## 1. Aggregates

The module has **3 aggregate roots**:

| Aggregate | Responsibility | File |
|-----------|---------------|------|
| `ConfigurationDefinition` | Metadata schema for a configuration key — what it is, what types it accepts, what scopes are allowed, validation rules, UI hints | `Domain/Entities/ConfigurationDefinition.cs` |
| `ConfigurationEntry` | A concrete value assigned to a configuration key at a specific scope — the actual stored configuration | `Domain/Entities/ConfigurationEntry.cs` |
| `ConfigurationAuditEntry` | Immutable append-only record of a configuration change — who changed what, when, from what to what | `Domain/Entities/ConfigurationAuditEntry.cs` |

---

## 2. Entity Analysis

### 2.1 ConfigurationDefinition (229 lines)

**Properties:**

| Property | Type | Max Length | Required | Immutable | Notes |
|----------|------|-----------|----------|-----------|-------|
| `Id` | `ConfigurationDefinitionId` | — | Yes | Yes | Strongly-typed ID |
| `Key` | `string` | 256 | Yes | Yes | Unique, e.g. `notifications.email.enabled` |
| `DisplayName` | `string` | 200 | Yes | No | Human-readable label |
| `Description` | `string?` | 1000 | No | No | Optional description |
| `Category` | `ConfigurationCategory` | — | Yes | No | Bootstrap, SensitiveOperational, Functional |
| `AllowedScopes` | `ConfigurationScope[]` | — | Yes | No | Array stored as `text[]` in PostgreSQL |
| `DefaultValue` | `string?` | 4000 | No | No | Fallback when no entry exists |
| `ValueType` | `ConfigurationValueType` | — | Yes | No | String, Integer, Decimal, Boolean, Json, StringList |
| `IsSensitive` | `bool` | — | Yes | No | Mask in UI/logs |
| `IsEditable` | `bool` | — | Yes | No | Can users change it? |
| `IsInheritable` | `bool` | — | Yes | No | Do child scopes inherit? |
| `ValidationRules` | `string?` | 4000 | No | No | JSON validation schema |
| `UiEditorType` | `string?` | 100 | No | No | `text`, `toggle`, `json-editor`, `select` |
| `SortOrder` | `int` | — | Yes | No | Display ordering |

**Factory Method:** `Create(key, displayName, description, category, allowedScopes, defaultValue, valueType, isSensitive, isEditable, isInheritable, validationRules, uiEditorType, sortOrder)`

**Update Method:** `Update(displayName, description, category, allowedScopes, defaultValue, valueType, isSensitive, isEditable, isInheritable, validationRules, uiEditorType, sortOrder)` — preserves immutable Key

**Assessment:** ✅ Well-designed. Follows DDD patterns. Guard clauses on creation. Immutable identity.

### 2.2 ConfigurationEntry (257 lines)

**Properties:**

| Property | Type | Max Length | Required | Immutable | Notes |
|----------|------|-----------|----------|-----------|-------|
| `Id` | `ConfigurationEntryId` | — | Yes | Yes | Strongly-typed ID |
| `DefinitionId` | `ConfigurationDefinitionId` | — | Yes | Yes | FK to definition |
| `Key` | `string` | 256 | Yes | Yes | Denormalized from definition for query efficiency |
| `Scope` | `ConfigurationScope` | — | Yes | Yes | System, Tenant, Environment, Role, Team, User |
| `ScopeReferenceId` | `string?` | 256 | Conditional | Yes | TenantId, EnvironmentId, etc. Null for System scope. |
| `Value` | `string?` | 4000 | No | No | Plain text value |
| `StructuredValueJson` | `string?` | 8000 | No | No | For Json type values |
| `IsEncrypted` | `bool` | — | Yes | No | Whether value is stored encrypted |
| `IsSensitive` | `bool` | — | Yes | No | Whether to mask in responses |
| `IsActive` | `bool` | — | Yes | No | Soft-delete/deactivation flag |
| `Version` | `int` | — | Yes | No | Auto-incremented on each update |
| `EffectiveFrom` | `DateTimeOffset?` | — | No | No | Temporal validity start |
| `EffectiveTo` | `DateTimeOffset?` | — | No | No | Temporal validity end |
| `ChangeReason` | `string?` | 500 | No | No | Last change reason |
| `CreatedBy` | `string` | 200 | Yes | Yes | User who created |
| `UpdatedBy` | `string?` | 200 | No | No | User who last updated |

**Factory Method:** `Create(definitionId, key, scope, scopeReferenceId, value, structuredValueJson, isEncrypted, isSensitive, changeReason, createdBy)`

**Key Methods:**
- `UpdateValue(value, structuredValueJson, isEncrypted, changeReason, updatedBy)` — increments Version
- `Activate(updatedBy)` / `Deactivate(updatedBy)` — toggle IsActive

**Assessment:** ✅ Well-designed. Version incrementing is solid. Temporal validity is a good feature. Guard clauses present.

### 2.3 ConfigurationAuditEntry (158 lines)

**Properties:**

| Property | Type | Max Length | Required | Immutable | Notes |
|----------|------|-----------|----------|-----------|-------|
| `Id` | `ConfigurationAuditEntryId` | — | Yes | Yes | Strongly-typed ID |
| `EntryId` | `ConfigurationEntryId` | — | Yes | Yes | FK to entry being audited |
| `Key` | `string` | 256 | Yes | Yes | Denormalized for query |
| `Scope` | `ConfigurationScope` | — | Yes | Yes | Scope of the change |
| `ScopeReferenceId` | `string?` | 256 | No | Yes | Scope context |
| `Action` | `string` | 50 | Yes | Yes | Created, Updated, Activated, Deactivated, Removed |
| `PreviousValue` | `string?` | 4000 | No | Yes | Before value |
| `NewValue` | `string?` | 4000 | No | Yes | After value |
| `PreviousVersion` | `int?` | — | No | Yes | Before version |
| `NewVersion` | `int` | — | Yes | Yes | After version |
| `ChangedBy` | `string` | 200 | Yes | Yes | User who made change |
| `ChangedAt` | `DateTimeOffset` | — | Yes | Yes | UTC timestamp |
| `ChangeReason` | `string?` | 500 | No | Yes | Compliance reason |
| `IsSensitive` | `bool` | — | Yes | Yes | Whether values should be masked |

**Factory Method:** `Create(entryId, key, scope, scopeReferenceId, action, previousValue, newValue, previousVersion, newVersion, changedBy, changedAt, changeReason, isSensitive)`

**Assessment:** ✅ Well-designed. Fully immutable after creation. Append-only pattern.

---

## 3. Value Objects

The module has **no explicit value objects** beyond the enums. The strongly-typed IDs (`ConfigurationDefinitionId`, `ConfigurationEntryId`, `ConfigurationAuditEntryId`) use `TypedIdBase` from BuildingBlocks.Core.

---

## 4. Enums (Persisted)

| Enum | Values | Storage | Notes |
|------|--------|---------|-------|
| `ConfigurationScope` | System(0), Tenant(1), Environment(2), Role(3), Team(4), User(5) | String conversion | Hierarchy: User is most specific, System most general |
| `ConfigurationCategory` | Bootstrap, SensitiveOperational, Functional | String conversion | Governs lifecycle and visibility |
| `ConfigurationValueType` | String, Integer, Decimal, Boolean, Json, StringList | String conversion | Determines validation and UI editor |

---

## 5. Entity Relationships

```
ConfigurationDefinition (1) ←── DefinitionId ──→ (N) ConfigurationEntry
ConfigurationEntry (1) ←── EntryId ──→ (N) ConfigurationAuditEntry
```

- A `ConfigurationDefinition` can have zero or many `ConfigurationEntry` records (one per scope/scope-reference combination).
- A `ConfigurationEntry` can have zero or many `ConfigurationAuditEntry` records (one per change).
- Relationships are via ID references, not navigation properties (loose coupling pattern).

---

## 6. Anemic Entity Assessment

| Entity | Assessment | Justification |
|--------|-----------|---------------|
| `ConfigurationDefinition` | **Not anemic** | Has Create/Update factory methods with validation, guard clauses, and business rules (immutable key, scope validation) |
| `ConfigurationEntry` | **Not anemic** | Has Create/UpdateValue/Activate/Deactivate with version management, temporal validity, validation |
| `ConfigurationAuditEntry` | **Acceptable** | Immutable after creation — no behavior needed beyond factory method. This is by design for audit records. |

---

## 7. Business Rules Location Assessment

| Rule | Current Location | Correct? |
|------|-----------------|----------|
| Key immutability | Entity (ConfigurationDefinition.Update skips Key) | ✅ |
| Version auto-increment | Entity (ConfigurationEntry.UpdateValue) | ✅ |
| Scope validation for ScopeReferenceId | Entity (ConfigurationEntry.Create) | ✅ |
| Definition exists before setting value | Handler (SetConfigurationValue) | ✅ |
| Scope is allowed by definition | Handler (SetConfigurationValue) | ✅ |
| Sensitive value encryption | Handler (SetConfigurationValue) → SecurityService | ✅ |
| Scope resolution hierarchy | Service (ConfigurationResolutionService) | ✅ |
| Cache invalidation on change | Handler → CacheService | ✅ |
| Audit record creation | Handler (SetConfigurationValue, ToggleConfiguration, RemoveOverride) | ✅ |

**Verdict:** Business rules are correctly distributed between entities, handlers, and services. No misplacement detected.

---

## 8. Missing Fields

| Entity | Field | Rationale | Priority |
|--------|-------|-----------|----------|
| `ConfigurationDefinition` | `RowVersion` (`uint`) | Optimistic concurrency for concurrent admin updates | HIGH |
| `ConfigurationEntry` | `RowVersion` (`uint`) | Optimistic concurrency for concurrent value changes | HIGH |
| `ConfigurationDefinition` | `IsDeprecated` (`bool`) | Mark definitions that are no longer actively used | LOW |
| `ConfigurationDefinition` | `DeprecatedMessage` (`string?`) | Guidance when a definition is deprecated | LOW |

---

## 9. Fields That Don't Make Sense

**None identified.** All current fields serve clear purposes. The `StructuredValueJson` field on `ConfigurationEntry` is a reasonable extension for complex JSON values alongside the plain `Value` field.

---

## 10. Final Domain Model

The current domain model is **approved as final** with the following additions for the migration baseline:

### Additions Required

1. **Add `RowVersion` (uint)** to `ConfigurationDefinition` and `ConfigurationEntry` for PostgreSQL `xmin` concurrency token.
2. **Consider `IsDeprecated` + `DeprecatedMessage`** on `ConfigurationDefinition` (optional, can be deferred).

### No Changes Required

- Entity structure is sound
- Enum definitions are complete
- Relationships are correct
- Business rule placement is correct
- Factory methods and guard clauses are comprehensive
- Audit trail design is mature

**The domain model is ready for the migration baseline.**
