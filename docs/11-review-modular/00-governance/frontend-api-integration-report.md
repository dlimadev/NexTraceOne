# Relatório de Integração com API do Frontend — NexTraceOne

> **Data:** 2025-07-14  
> **Versão:** 2.0  
> **Escopo:** Mapeamento completo de ficheiros API e integração backend por módulo  
> **Status global:** GAP_IDENTIFIED  
> **Cliente API base:** `src/frontend/src/api/client.ts`  
> **Exports centrais:** `src/frontend/src/api/index.ts`

---

## 1. Resumo

| Métrica | Valor |
|---------|-------|
| Módulos com API real | 12 de 14 |
| Módulos sem API | 2 (operational-intelligence, shared) |
| Total de ficheiros API | 34 |
| Framework de estado assíncrono | React Query |
| Gestão de query keys | `shared/api/queryKeys.ts` |
| Classificações | API_REAL, SEM_API, N/A |

---

## 2. Classificação por Módulo

| # | Módulo | Ficheiros API | Classificação | Status |
|---|--------|--------------|---------------|--------|
| 1 | catalog | 7 | API_REAL | ✅ COMPLETE_APPARENT |
| 2 | change-governance | 5 | API_REAL | ✅ COMPLETE_APPARENT |
| 3 | operations | 5 | API_REAL | ✅ COMPLETE_APPARENT |
| 4 | governance | 4 | API_REAL | ✅ COMPLETE_APPARENT |
| 5 | contracts | 3 | API_REAL | ⚠️ PARTIAL (3 páginas sem rota) |
| 6 | ai-hub | 2 | API_REAL | ✅ COMPLETE_APPARENT |
| 7 | audit-compliance | 2 | API_REAL | ✅ COMPLETE_APPARENT |
| 8 | identity-access | 2 | API_REAL | ✅ COMPLETE_APPARENT |
| 9 | configuration | 1 | API_REAL | ✅ COMPLETE_APPARENT |
| 10 | integrations | 1 | API_REAL | ✅ COMPLETE_APPARENT |
| 11 | notifications | 1 | API_REAL | ✅ COMPLETE_APPARENT |
| 12 | product-analytics | 1 | API_REAL | ⚠️ PARTIAL (página vazia) |
| 13 | operational-intelligence | 0 | SEM_API | ❌ GAP_IDENTIFIED |
| 14 | shared | 0 | N/A | N/A (infraestrutura) |

---

## 3. Detalhe por Módulo

### 3.1 catalog (7 ficheiros API)

**Classificação:** API_REAL  
**Cobertura:** Completa — todas as páginas funcionais têm integração API

| Ficheiro API | Caminho | Export principal | Endpoints cobertos |
|-------------|---------|-----------------|-------------------|
| serviceCatalog.ts | `catalog/api/serviceCatalog.ts` | `serviceCatalogApi` | GET/POST /api/v1/catalog/services |
| contracts.ts | `catalog/api/contracts.ts` | `contractsApi` | GET /api/v1/catalog/contracts |
| contractStudio.ts | `catalog/api/contractStudio.ts` | `contractStudioApi` | GET/POST /api/v1/catalog/contract-studio |
| developerPortal.ts | `catalog/api/developerPortal.ts` | `developerPortalApi` | GET /api/v1/developer-portal |
| globalSearch.ts | `catalog/api/globalSearch.ts` | `globalSearchApi` | GET /api/v1/search |
| sourceOfTruth.ts | `catalog/api/sourceOfTruth.ts` | `sourceOfTruthApi` | GET /api/v1/source-of-truth |
| (+ 1 adicional) | — | — | — |

**Páginas cobertas:** ServiceCatalogListPage, ServiceCatalogPage, ServiceDetailPage, SourceOfTruthExplorerPage, ServiceSourceOfTruthPage, ContractSourceOfTruthPage, DeveloperPortalPage, GlobalSearchPage, CatalogContractsConfigurationPage

**Observação:** As 3 páginas órfãs (ContractDetailPage, ContractListPage, ContractsPage) podem ter referências a APIs legacy.

---

### 3.2 change-governance (5 ficheiros API)

**Classificação:** API_REAL  
**Cobertura:** Completa

| Ficheiro API | Caminho | Export principal | Endpoints cobertos |
|-------------|---------|-----------------|-------------------|
| changeIntelligence.ts | `change-governance/api/changeIntelligence.ts` | `changeIntelligenceApi` | GET /api/v1/changes |
| changeConfidence.ts | `change-governance/api/changeConfidence.ts` | — | GET /api/v1/change-confidence |
| workflow.ts | `change-governance/api/workflow.ts` | `workflowApi` | GET/POST /api/v1/workflow |
| promotion.ts | `change-governance/api/promotion.ts` | `promotionApi` | GET/POST /api/v1/promotion |
| (+ 1 adicional) | — | — | — |

**Páginas cobertas:** ChangeCatalogPage, ChangeDetailPage, ReleasesPage, WorkflowPage, PromotionPage, WorkflowConfigurationPage

---

### 3.3 operations (5 ficheiros API)

**Classificação:** API_REAL  
**Cobertura:** Completa

| Ficheiro API | Caminho | Export principal | Endpoints cobertos |
|-------------|---------|-----------------|-------------------|
| incidents.ts | `operations/api/incidents.ts` | `incidentsApi` | GET /api/v1/operations/incidents |
| reliability.ts | `operations/api/reliability.ts` | — | GET /api/v1/operations/reliability |
| automation.ts | `operations/api/automation.ts` | — | GET/POST /api/v1/operations/automation |
| runtimeIntelligence.ts | `operations/api/runtimeIntelligence.ts` | — | GET /api/v1/operations/runtime |
| platformOps.ts | `operations/api/platformOps.ts` | — | GET /api/v1/platform/operations |

**Páginas cobertas:** IncidentsPage, IncidentDetailPage, RunbooksPage, TeamReliabilityPage, ServiceReliabilityDetailPage, AutomationWorkflowsPage, AutomationWorkflowDetailPage, EnvironmentComparisonPage, PlatformOperationsPage

---

### 3.4 governance (4 ficheiros API)

**Classificação:** API_REAL  
**Cobertura:** Completa para as funcionalidades implementadas

| Ficheiro API | Caminho | Export principal | Endpoints cobertos |
|-------------|---------|-----------------|-------------------|
| organizationGovernance.ts | `governance/api/organizationGovernance.ts` | `organizationGovernanceApi` | GET /api/v1/governance/* |
| evidence.ts | `governance/api/evidence.ts` | — | GET /api/v1/governance/evidence |
| executive.ts | `governance/api/executive.ts` | — | GET /api/v1/governance/executive |
| finOps.ts | `governance/api/finOps.ts` | — | GET /api/v1/governance/finops |

**Páginas cobertas:** Todas as 25 páginas de governance

---

### 3.5 contracts (3 ficheiros API)

**Classificação:** API_REAL (parcial)  
**Cobertura:** Parcial — 3 páginas sem rota

| Ficheiro API | Caminho | Export principal | Endpoints cobertos |
|-------------|---------|-----------------|-------------------|
| contracts.ts | `contracts/api/contracts.ts` | `contractsApi` | GET/POST/PUT /api/v1/contracts |
| contractStudio.ts | `contracts/api/contractStudio.ts` | — | GET/POST /api/v1/contracts/studio |
| (+ 1 possível adicional) | — | — | — |

**Páginas cobertas:** ContractCatalogPage, CreateServicePage, DraftStudioPage, ContractWorkspacePage

**Páginas SEM cobertura API verificável:**
- ContractGovernancePage (sem rota registada)
- SpectralRulesetManagerPage (sem rota registada)
- CanonicalEntityCatalogPage (sem rota registada)
- ContractPortalPage (órfã)

---

### 3.6 ai-hub (2 ficheiros API)

**Classificação:** API_REAL  
**Cobertura:** Completa

| Ficheiro API | Caminho | Export principal |
|-------------|---------|-----------------|
| aiGovernance.ts | `ai-hub/api/aiGovernance.ts` | `aiGovernanceApi` |
| (+ 1 adicional) | — | — |

**Páginas cobertas:** Todas as 11 páginas do ai-hub

---

### 3.7 audit-compliance (2 ficheiros API)

**Classificação:** API_REAL  
**Cobertura:** Completa

| Ficheiro API | Caminho | Export principal |
|-------------|---------|-----------------|
| audit.ts | `audit-compliance/api/audit.ts` | `auditApi` |
| (+ tipos) | — | — |

**Páginas cobertas:** AuditPage

---

### 3.8 identity-access (2 ficheiros API)

**Classificação:** API_REAL  
**Cobertura:** Completa

| Ficheiro API | Caminho | Export principal |
|-------------|---------|-----------------|
| identity.ts | `identity-access/api/identity.ts` | `identityApi` |
| (+ tipos) | — | — |

**Endpoints cobertos:** 
- POST /api/v1/identity/login
- POST /api/v1/identity/logout
- GET /api/v1/identity/me
- GET /api/v1/identity/users
- GET /api/v1/identity/environments
- POST /api/v1/identity/sessions
- POST /api/v1/identity/mfa
- etc.

---

### 3.9 configuration (1 ficheiro API)

**Classificação:** API_REAL

| Ficheiro API | Caminho |
|-------------|---------|
| configurationApi.ts | `configuration/api/configurationApi.ts` |

---

### 3.10 integrations (1 ficheiro API)

**Classificação:** API_REAL

| Ficheiro API | Caminho |
|-------------|---------|
| integrations.ts | `integrations/api/integrations.ts` |

---

### 3.11 notifications (1 ficheiro API)

**Classificação:** API_REAL

| Ficheiro API | Caminho |
|-------------|---------|
| notifications.ts | `notifications/api/notifications.ts` |

---

### 3.12 product-analytics (1 ficheiro API)

**Classificação:** API_REAL (parcial)

| Ficheiro API | Caminho |
|-------------|---------|
| productAnalyticsApi.ts | `product-analytics/api/productAnalyticsApi.ts` |

**Nota:** O ficheiro `ProductAnalyticsOverviewPage.tsx` na raiz do módulo está vazio (0 bytes).

---

### 3.13 operational-intelligence (0 ficheiros API)

**Classificação:** SEM_API  
**Status:** GAP_IDENTIFIED

**Páginas:** OperationsFinOpsConfigurationPage (1 página, 1 ficheiro total no módulo)

**Problema:** Módulo embrionário sem qualquer integração com backend. A página pode estar a usar dados estáticos ou props recebidos de outro contexto.

---

### 3.14 shared (0 ficheiros API no módulo, N/A)

**Nota:** O módulo shared contém apenas infraestrutura partilhada. O DashboardPage pode consumir dados de múltiplos módulos.

---

## 4. Infraestrutura API

### 4.1 Cliente HTTP

| Componente | Ficheiro | Descrição |
|-----------|---------|-----------|
| API Client | `api/client.ts` | Instância Axios/fetch configurada com interceptors |
| API Index | `api/index.ts` | Re-exportação centralizada de todos os API modules |
| Query Keys | `shared/api/queryKeys.ts` | Chaves de cache React Query centralizadas |

### 4.2 Padrão de integração

```typescript
// Padrão típico de ficheiro API
export const moduleApi = {
  getList: (params) => client.get('/api/v1/module', { params }),
  getById: (id) => client.get(`/api/v1/module/${id}`),
  create: (data) => client.post('/api/v1/module', data),
  update: (id, data) => client.put(`/api/v1/module/${id}`, data),
  delete: (id) => client.delete(`/api/v1/module/${id}`),
};
```

### 4.3 React Query

- Todas as queries usam React Query para caching e sincronização
- Query keys centralizadas em `shared/api/queryKeys.ts`
- Invalidação automática após mutações
- Sem Redux nem Zustand — estado assíncrono gerido exclusivamente por React Query

---

## 5. Matriz de Integração

| Módulo | Leitura (GET) | Escrita (POST/PUT) | Eliminação (DELETE) | Real-time |
|--------|:------------:|:------------------:|:------------------:|:---------:|
| catalog | ✅ | ✅ | ⚠️ | — |
| change-governance | ✅ | ✅ | — | — |
| operations | ✅ | ✅ | — | — |
| governance | ✅ | ⚠️ | — | — |
| contracts | ✅ | ✅ | — | — |
| ai-hub | ✅ | ✅ | — | — |
| audit-compliance | ✅ | — | — | — |
| identity-access | ✅ | ✅ | — | — |
| configuration | ✅ | ✅ | — | — |
| integrations | ✅ | ✅ | — | — |
| notifications | ✅ | ✅ | — | — |
| product-analytics | ✅ | — | — | — |
| operational-intelligence | — | — | — | — |
| shared | N/A | N/A | N/A | N/A |

---

## 6. Problemas e Recomendações

### 6.1 Problemas identificados

| # | Problema | Módulo | Prioridade | Status |
|---|---------|--------|------------|--------|
| 1 | Módulo operational-intelligence sem API | operational-intelligence | MEDIUM | GAP_IDENTIFIED |
| 2 | 3 páginas de contracts sem rota → API não verificável | contracts | CRITICAL | GAP_IDENTIFIED |
| 3 | Ficheiro vazio em product-analytics | product-analytics | MEDIUM | GAP_IDENTIFIED |
| 4 | 3 páginas legacy de catalog potencialmente com imports API obsoletos | catalog | HIGH | IN_ANALYSIS |

### 6.2 Recomendações

| # | Ação | Prioridade | Esforço |
|---|------|------------|---------|
| 1 | Registar as 3 rotas de contracts para activar integração API | CRITICAL | Baixo |
| 2 | Criar ficheiro API para operational-intelligence ou fundir módulo | MEDIUM | Médio |
| 3 | Verificar e remover imports API obsoletos nas páginas legacy de catalog | HIGH | Baixo |
| 4 | Implementar conteúdo real em ProductAnalyticsOverviewPage.tsx (raiz) | MEDIUM | Baixo |
| 5 | Documentar endpoints consumidos por cada módulo para referência cruzada com backend | LOW | Médio |

---

*Documento gerado como parte da auditoria modular do NexTraceOne.*
