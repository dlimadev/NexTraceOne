# NexTraceOne — Relatório de Auditoria: Frontend
**Data:** 2026-04-10  
**Escopo:** React 19 / TypeScript — features, componentes, rotas, i18n  
**Ficheiros analisados:** ~676 ficheiros TypeScript/TSX

---

## CRÍTICO

### [C-07] GUID Input — CanonicalEntityImpactCascadePage
**Ficheiro:** `src/frontend/src/features/contracts/canonical/CanonicalEntityImpactCascadePage.tsx` (linha 132)  

**Problema:**  
Campo de texto com placeholder `e.g. 3fa85f64-5717-4562-b3fc-2c963f66afa6`. O utilizador final é obrigado a conhecer e introduzir o UUID da entidade.

**Impacto:** Anti-padrão crítico de UX. Utilizadores de negócio não têm como saber o UUID de uma entidade canónica.

**Correcção:**
```tsx
// REMOVER:
<Input placeholder="e.g. 3fa85f64-5717-4562-b3fc-2c963f66afa6" />

// SUBSTITUIR POR:
<EntityPicker
  entityType="canonical"
  onSelect={(entity) => setEntityId(entity.id)}
  placeholder={t('contracts.canonical.selectEntity')}
/>
```

---

### [C-08] GUID Input — ContractHealthTimelinePage
**Ficheiro:** `src/frontend/src/features/contracts/governance/ContractHealthTimelinePage.tsx` (linha 84)  

**Problema:**  
Campo "API Asset ID" com placeholder UUID. O utilizador precisa de inserir manualmente o ID do asset de API.

**Correcção:** Substituir por dropdown/search de contratos existentes. O endpoint de listagem de contratos já existe no backend.

---

### [C-09] GUID Inputs — DependencyDashboardPage
**Ficheiro:** `src/frontend/src/features/catalog/pages/DependencyDashboardPage.tsx` (linhas 181, 296)  

**Problema:**  
Dois campos de input com placeholder `00000000-0000-0000-0000-000000000000`. A página de dashboard de dependências exige dois UUIDs para funcionar.

**Correcção:** Implementar selector de serviços com search/autocomplete usando o Service Catalog API.

---

### [C-10] GUID Input — LicenseCompliancePage
**Ficheiro:** `src/frontend/src/features/catalog/pages/LicenseCompliancePage.tsx` (linha 168)  

**Problema:**  
Campo "Service ID" com placeholder UUID no formato completo.

**Correcção:** Substituir por selector de serviço com nome + ID legível.

---

## ALTO

### [A-07a] Strings Hardcoded sem i18n — CanonicalEntityImpactCascadePage
**Ficheiro:** `src/frontend/src/features/contracts/canonical/CanonicalEntityImpactCascadePage.tsx` (linhas 168–169)  

```tsx
// HARDCODED (ERRADO):
"Failed to load cascade analysis. Please verify the entity ID."
"Retry"

// CORRECTO:
t('contracts.canonical.error.loadFailed')
t('common.actions.retry')
```

---

### [A-07b] Strings Hardcoded sem i18n — ContractHealthTimelinePage
**Ficheiro:** `src/frontend/src/features/contracts/governance/ContractHealthTimelinePage.tsx` (linhas 78, 105–106)  

```tsx
// HARDCODED (ERRADO):
"API Asset ID"
"Failed to load health timeline. Please verify the API Asset ID."
"Retry"
aria-label="API Asset ID"
```

---

### [A-07c] Strings Hardcoded sem i18n — LivePreviewRenderer
**Ficheiro:** `src/frontend/src/features/contracts/workspace/editor/LivePreviewRenderer.tsx` (linhas 212, 218)  

```tsx
// HARDCODED:
"PARAMETERS"
"RESPONSE"
```

---

### [A-07d] String Hardcoded sem i18n — SummarySection
**Ficheiro:** `src/frontend/src/features/contracts/workspace/sections/SummarySection.tsx` (linha 184)  

```tsx
// HARDCODED:
"Producer"
```

**Chaves i18n sugeridas para adicionar ao `locales/en.json`:**
```json
{
  "contracts": {
    "canonical": {
      "error": {
        "loadFailed": "Failed to load cascade analysis. Please verify the entity ID."
      },
      "selectEntity": "Search and select a canonical entity"
    },
    "governance": {
      "healthTimeline": {
        "apiAssetIdLabel": "API Asset ID",
        "error": {
          "loadFailed": "Failed to load health timeline. Please verify the API Asset ID."
        }
      }
    },
    "workspace": {
      "editor": {
        "parameters": "Parameters",
        "response": "Response"
      },
      "summary": {
        "producer": "Producer"
      }
    }
  }
}
```

---

## MÉDIO

### [M-10] Missing Loading State — ContractHealthTimelinePage
**Ficheiro:** `src/frontend/src/features/contracts/governance/ContractHealthTimelinePage.tsx`  

**Problema:**  
A página apenas renderiza dados quando disponíveis (`{data && (...)}`), sem mostrar estado de loading durante fetch.

**Correcção:**
```tsx
{isLoading && <PageLoadingState />}
{isError && (
  <PageErrorState
    message={t('contracts.governance.healthTimeline.error.loadFailed')}
    onRetry={() => refetch()}
  />
)}
{data && <TimelineContent data={data} />}
{!isLoading && !isError && !data && (
  <EmptyState message={t('contracts.governance.healthTimeline.empty')} />
)}
```

---

### [M-11a] Validação de Formato UUID Ausente — CanonicalEntityImpactCascadePage
**Ficheiro:** `src/frontend/src/features/contracts/canonical/CanonicalEntityImpactCascadePage.tsx` (linhas 99–103)  

```tsx
// PROBLEMA: apenas verifica se está vazio
if (!entityId.trim()) return;

// CORRECÇÃO: validar formato UUID antes de submeter
const UUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
if (!UUID_REGEX.test(entityId.trim())) {
  setValidationError(t('common.validation.invalidUuid'));
  return;
}
```

*Nota: Esta validação só é necessária enquanto o campo GUID não for substituído por entity picker (C-07). Após a substituição do picker, a validação de formato deixa de ser necessária.*

---

### [M-11b] Validação de Formato UUID Ausente — ContractHealthTimelinePage
**Ficheiro:** `src/frontend/src/features/contracts/governance/ContractHealthTimelinePage.tsx` (linhas 53–55)  

Mesmo problema que M-11a.

---

## BAIXO

### [L-05] aria-label Hardcoded — ContractHealthTimelinePage
**Ficheiro:** `src/frontend/src/features/contracts/governance/ContractHealthTimelinePage.tsx` (linha 86)  

```tsx
// ERRADO:
aria-label="API Asset ID"

// CORRECTO:
aria-label={t('contracts.governance.healthTimeline.apiAssetIdLabel')}
```

---

## Resumo de Issues por Feature

| Feature | C | A | M | L |
|---------|---|---|---|---|
| contracts/canonical | 1 | 1 | 1 | — |
| contracts/governance | 1 | 1 | 1 | 1 |
| contracts/workspace | — | 2 | — | — |
| catalog/pages | 2 | — | — | — |

---

## Padrão de Correcção para Entity Pickers

Todas as 4 páginas com GUID input devem ser substituídas por um componente reutilizável. O design system já tem `Combobox` do Radix UI. Sugere-se criar:

```tsx
// src/shared/ui/EntityPicker.tsx
interface EntityPickerProps<T> {
  entityType: 'service' | 'contract' | 'canonical' | 'apiAsset';
  onSelect: (entity: { id: string; name: string }) => void;
  placeholder?: string;
  disabled?: boolean;
}
```

Este componente usa os endpoints de search/list existentes no backend para popular o dropdown.
