# Revisão Modular — Catalog

> **Data:** 2026-03-24  
> **Prioridade:** P2 (Pilar Core — Módulo Mais Maduro)  
> **Módulo Backend:** `src/modules/catalog/`  
> **Módulo Frontend:** `src/frontend/src/features/catalog/`  
> **Fonte de verdade:** Código do repositório

---

## 1. Propósito do Módulo

O módulo **Catalog** é o coração do NexTraceOne, responsável pelo Service Catalog e Source of Truth. Ele cobre 4 subdomínios:

- **Graph** — Topologia de serviços, APIs, dependências, consumers, health records, graph snapshots
- **Contracts** — Drafts, reviews, scorecards, exemplos, rulesets Spectral (compartilhado com módulo Contracts frontend)
- **Portal** — Developer Portal com pesquisa, playground, sessões guardadas
- **SourceOfTruth** — Referências linkadas, explorador de verdade

---

## 2. Aderência ao Produto

| Aspecto | Avaliação | Observação |
|---------|-----------|------------|
| Alinhamento com a visão | ✅ Muito Forte | Service Catalog + Source of Truth são pilares centrais |
| Completude funcional | ✅ Alta | 430+ testes, 3 DbContexts, 12 páginas frontend |
| Maturidade backend | ✅ A mais alta do sistema | 256 ficheiros C#, 4 endpoint modules, persistência real |

---

## 3. Páginas e Ações do Frontend

| Página | Rota | Permissão | Estado | Funcionalidade |
|--------|------|-----------|--------|----------------|
| ServiceCatalogListPage | `/services` | catalog:assets:read | ✅ Funcional | Lista de serviços com filtros |
| ServiceCatalogPage | `/services/graph` | catalog:assets:read | ✅ Funcional | Grafo de dependências (1010 linhas) |
| ServiceDetailPage | `/services/:serviceId` | catalog:assets:read | ✅ Funcional | Detalhe de serviço |
| SourceOfTruthExplorerPage | `/source-of-truth` | catalog:assets:read | ✅ Funcional | Explorador Source of Truth |
| ServiceSourceOfTruthPage | `/source-of-truth/service/:serviceId` | catalog:assets:read | ✅ Funcional | Source of Truth por serviço |
| ContractSourceOfTruthPage | `/source-of-truth/contract/:contractId` | catalog:assets:read | ✅ Funcional | Source of Truth por contrato |
| DeveloperPortalPage | `/portal` | developer-portal:read | ✅ Funcional | Portal do developer |
| GlobalSearchPage | `/search` | catalog:assets:read | ✅ Funcional | Pesquisa global |
| CatalogContractsConfigurationPage | `/platform/configuration/catalog-contracts` | platform:admin:read | ✅ Funcional | Configuração de contratos e catálogo |

### Páginas Potencialmente Órfãs

| Ficheiro | Estado | Observação |
|----------|--------|------------|
| ContractDetailPage.tsx | ⚠️ Não roteada | Possivelmente substituída por ContractWorkspacePage no módulo contracts |
| ContractListPage.tsx | ⚠️ Não roteada | Possivelmente substituída por ContractCatalogPage no módulo contracts |
| ContractsPage.tsx | ⚠️ Não roteada | Possivelmente substituída por ContractCatalogPage |
| ContractSourceOfTruthPage.tsx | ✅ Roteada | OK |
| ServiceSourceOfTruthPage.tsx | ✅ Roteada | OK |

---

## 4. Backend — Subdomínios

### 4.1 Graph (Topologia)

| Entidade | Propósito |
|----------|-----------|
| ServiceAsset | Serviço registado no catálogo |
| ApiAsset | API exposta por um serviço |
| ConsumerAsset | Aplicação consumidora |
| ConsumerRelationship | Relação consumer-API |
| GraphSnapshot | Snapshot periódico do grafo |
| NodeHealthRecord | Saúde de nós do grafo |

**Endpoints:** Catalog (services, APIs, graph, impact analysis)

### 4.2 Contracts (Governança)

| Entidade | Propósito |
|----------|-----------|
| ContractDraft | Rascunho de contrato |
| ContractReview | Review de contrato |
| ContractScorecard | Scorecard de compliance |
| ContractExample | Exemplos de uso |
| SpectralRuleset | Regras de validação Spectral |

**Endpoints:** Contracts (drafts, reviews, publish, validate)

### 4.3 Portal (Developer)

| Entidade | Propósito |
|----------|-----------|
| SavedSearch | Pesquisas guardadas |
| PlaygroundSession | Sessões de playground |

**Endpoints:** Developer Portal (search, playground)

### 4.4 Source of Truth

| Entidade | Propósito |
|----------|-----------|
| LinkedReference | Referências linkadas |

**Endpoints:** Source of Truth (referências, explorador)

---

## 5. Banco de Dados

| DbContext | Propósito | Entidades Principais |
|-----------|-----------|---------------------|
| CatalogGraphDbContext | Topologia de serviços | ServiceAsset, ApiAsset, ConsumerAsset, ConsumerRelationship, GraphSnapshot, NodeHealthRecord |
| ContractsDbContext | Governança de contratos | ContractDraft, ContractReview, ContractScorecard, SpectralRuleset |
| DeveloperPortalDbContext | Portal e pesquisa | SavedSearch, PlaygroundSession, LinkedReference |

---

## 6. Regras de Negócio

| Regra | Estado | Evidência |
|-------|--------|-----------|
| Impact analysis via grafo | ✅ | Graph endpoints |
| Health records por nó | ✅ | NodeHealthRecord |
| Contract lifecycle | ✅ | Draft → Review → Published |
| Spectral validation | ✅ | SpectralRuleset integration |
| Compliance scoring | ✅ | ContractScorecard |
| Consumer tracking | ✅ | ConsumerRelationship |

---

## 7. Segurança

| Aspecto | Estado |
|---------|--------|
| Permissões | ✅ catalog:assets:read, catalog:assets:write, contracts:read, contracts:write, developer-portal:read |
| RLS | ✅ Multi-tenancy |
| Rate limiting | ✅ |

---

## 8. Resumo de Ações

### Ações Importantes (P1)

| # | Ação | Esforço |
|---|------|---------|
| 1 | Resolver páginas órfãs em catalog/pages/ (ContractDetailPage, ContractListPage, ContractsPage) — decidir se são versões antigas e podem ser removidas | 2h |
| 2 | Validar todos os fluxos de Graph (service registration, dependency mapping, impact analysis) | 3h |
| 3 | Validar Developer Portal (search, playground) funcionalidade real | 2h |

### Ações de Melhoria (P2)

| # | Ação | Esforço |
|---|------|---------|
| 4 | Criar documentação unificada do módulo Catalog | 3h |
| 5 | Documentar API endpoints por subdomínio | 2h |
| 6 | Verificar cobertura de testes (430+ existentes) | 2h |
| 7 | Avaliar se ContractsDbContext deve ser separado em módulo dedicado | 2h |
