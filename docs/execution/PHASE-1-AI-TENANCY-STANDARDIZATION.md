# Phase 1, Block D — AI Tenancy Standardization

> **Status:** Complete  
> **Risk Treated:** TenantId stored as `string` in AIKnowledge module, enabling cross-tenant data leakage and inconsistent queries

---

## Problem

Two entities in the AIKnowledge module stored `TenantId` as `string` instead of `Guid`:

- `AiExternalInferenceRecord.TenantId` — `string`
- `AiTokenUsageLedger.TenantId` — `string`

This violated the platform-wide convention of strongly-typed `Guid` tenant identifiers
and created risks:

- String comparison inconsistencies (casing, whitespace, format)
- No database-level type safety
- Potential cross-tenant data leakage through malformed tenant values
- The `RecordExternalInference` handler was passing `string.Empty` as TenantId

## Solution

### Domain Changes

| Entity | Before | After |
|--------|--------|-------|
| `AiExternalInferenceRecord.TenantId` | `string` | `Guid` |
| `AiTokenUsageLedger.TenantId` | `string` | `Guid` |

### EF Configuration Changes

Removed `HasMaxLength(200)` from both TenantId column configurations, as the column
is now a native UUID type in the database.

### Interface Changes

| Interface | Method | Change |
|-----------|--------|--------|
| `IAiTokenUsageLedgerRepository` | `GetByTenantAsync` | Parameter: `string` → `Guid` |
| `IAiTokenQuotaPolicyRepository` | `GetForTenantAsync` | Parameter: `string` → `Guid` |

### Repository and Handler Changes

- All repository implementations updated to accept `Guid` TenantId
- DTOs updated to use `Guid`
- Service interfaces aligned
- `RecordExternalInference` handler fixed: now resolves TenantId from `ICurrentTenant.Id`
  instead of passing `string.Empty`

### Database Migration

**File:** `20260322140000_StandardizeTenantIdToGuid.cs`

Uses PostgreSQL `USING` clause for safe in-place conversion:

```sql
ALTER TABLE ai_external_inference_records
    ALTER COLUMN tenant_id TYPE uuid USING tenant_id::uuid;

ALTER TABLE ai_token_usage_ledger
    ALTER COLUMN tenant_id TYPE uuid USING tenant_id::uuid;
```

This conversion is safe because all existing values are valid UUID strings stored
in the text columns.

## Verification

- **All 399 AIKnowledge tests pass** after the migration
- No compilation warnings related to TenantId types
- Query consistency verified through repository unit tests
