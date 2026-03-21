# ADR-005: AI Runtime Foundation — Entity Expansion

**Status**: Accepted  
**Date**: 2026-03-21  
**Context**: Phase 1 of the AI module evolution — AI Runtime Foundation

## Decision

Expand the `AiProvider` and `AIModel` domain entities with runtime-relevant metadata to establish a serious AI Runtime Foundation. The existing infrastructure (OllamaProvider, OpenAiProvider, AiProviderFactory, ExecuteAiChat handler, 12+ API endpoints) was already functional — the gap was in the domain model richness.

## Changes

### AiProvider — New fields

| Field | Type | Purpose |
|-------|------|---------|
| `Slug` | string (unique) | URL-friendly identifier for routing and API paths |
| `AuthenticationMode` | enum (None, ApiKey, OAuth2, ManagedIdentity) | How to authenticate with the provider |
| `SupportsChat` | bool | Structured capability flag |
| `SupportsEmbeddings` | bool | Structured capability flag |
| `SupportsTools` | bool | Structured capability flag |
| `SupportsVision` | bool | Structured capability flag |
| `SupportsStructuredOutput` | bool | Structured capability flag |
| `HealthStatus` | enum (Unknown, Healthy, Degraded, Unhealthy, Offline) | Persisted health check result |
| `TimeoutSeconds` | int (default 30) | Configurable per-provider timeout |

### AIModel — New fields

| Field | Type | Purpose |
|-------|------|---------|
| `Slug` | string (unique) | URL-friendly identifier |
| `ProviderId` | AiProviderId? (FK) | Proper FK to AiProvider entity |
| `ExternalModelId` | string | Model identifier at the provider level |
| `Category` | string | Functional category (general, code, reasoning, embeddings) |
| `IsInstalled` | bool | Whether model is available/pulled |
| `IsDefaultForChat` | bool | Default chat model flag |
| `IsDefaultForReasoning` | bool | Default reasoning model flag |
| `IsDefaultForEmbeddings` | bool | Default embeddings model flag |
| `SupportsStreaming` | bool | Streaming response support |
| `SupportsToolCalling` | bool | Function/tool calling support |
| `SupportsEmbeddings` | bool | Embeddings generation support |
| `SupportsVision` | bool | Image input support |
| `SupportsStructuredOutput` | bool | JSON mode support |
| `ContextWindow` | int? | Context window size in tokens |
| `RequiresGpu` | bool | GPU requirement flag |
| `RecommendedRamGb` | decimal? | RAM recommendation for local models |
| `LicenseName` | string | Model license name |
| `LicenseUrl` | string | Model license URL |
| `ComplianceStatus` | string | Compliance review status |

### New enums

- `AuthenticationMode` (None, ApiKey, OAuth2, ManagedIdentity)
- `ProviderHealthStatus` (Unknown, Healthy, Degraded, Unhealthy, Offline)

### New domain methods

- `AiProvider.UpdateCapabilityFlags()` — Update capability booleans
- `AiProvider.RecordHealthStatus()` — Persist health check result
- `AIModel.UpdateCapabilityFlags()` — Update model capability booleans
- `AIModel.SetDefaultFlags()` — Set default model assignments
- `AIModel.MarkAsInstalled()` / `MarkAsUninstalled()` — Installation tracking

## Migration

- `ExpandProviderAndModelEntities` — Adds all new columns as either nullable or with empty/false defaults for backward compatibility
- Unique indexes on `Slug` for both entities
- Index on `ProviderId` and `IsDefaultForChat`

## Backward Compatibility

- All new parameters in `Register()` factory methods use optional defaults
- Existing callers (tests, handlers) compile without changes
- Seed SQL updated to include all new columns
- All 1,674 unit tests pass

## Consequences

- Domain model now rich enough for serious runtime decision-making
- Provider/model queries can use structured capability flags instead of parsing CSV strings
- Health status persisted at entity level enables governance dashboards
- ProviderId FK enables proper relational queries between models and providers
- Default model flags enable DB-driven model resolution without configuration hacks
