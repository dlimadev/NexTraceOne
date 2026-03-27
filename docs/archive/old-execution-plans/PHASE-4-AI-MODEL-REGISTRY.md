# Phase 4 — AI Model Registry

## Backend Validado

### Entidade: `AIModel`
- Localização: `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Domain/Governance/Entities/AIModel.cs`
- Factory method: `AIModel.Register(name, displayName, provider, modelType, isInternal, capabilities, sensitivityLevel, registeredAt)`
- Status lifecycle: Active → Inactive → Deprecated → Blocked (com transições reversíveis)
- Campos: Name, DisplayName, Provider, ModelType, IsInternal/IsExternal, Capabilities, SensitivityLevel, Slug, ProviderId, ExternalModelId, ContextWindowSize, etc.

### Endpoints
- `GET /api/v1/ai/models` — Lista modelos com filtros (provider, modelType, status, isInternal)
- `GET /api/v1/ai/models/{id}` — Detalhe do modelo
- `POST /api/v1/ai/models` — Registo de novo modelo
- `PATCH /api/v1/ai/models/{id}` — Actualização de modelo

### Permissões
- Leitura: `ai:governance:read`
- Escrita: `ai:governance:write`

## Frontend Validado

### Página: `ModelRegistryPage.tsx`
- Localização: `src/frontend/src/features/ai-hub/pages/ModelRegistryPage.tsx`
- API client: `aiGovernanceApi.listModels()`
- Stat cards: Total, Active, Internal, External
- Filtros: All, Active, Inactive, Deprecated, Blocked + busca por texto
- Status badges: success (Active), default (Inactive), warning (Deprecated), danger (Blocked)
- Sensitivity badges: Low (≤1), Medium (2), High (≥3)
- Estados: loading (spinner), error (PageErrorState + retry), empty (EmptyState)

## Persistência

- Tabela: `ai_gov_models`
- DbContext: `AiGovernanceDbContext`
- Migration: `20260321160337_InitialCreate` + `20260321172507_ExpandProviderAndModelEntities`

## Testes

### Backend (existentes)
- `Model_Register_ShouldSetProperties`
- `Model_Register_Internal_ShouldDeriveIsExternal`
- `Model_UpdateDetails_ShouldModifyProperties`
- `Model_Deactivate/Activate/Deprecate/Block`
- `Model_Register_ShouldSetDefaultValues_ForNewFields`
- `Model_UpdateCapabilityFlags_ShouldUpdateAllFlags`
- `Model_SetDefaultFlags_ShouldUpdateFlags`
- `Model_MarkAsInstalled/Uninstalled`
- Total: 15 testes unitários de entidade

### Frontend (novos — Fase 4)
- Loading state, success render, error state, empty state
- Stat cards, status badges, filter buttons
- Total: 7 testes
