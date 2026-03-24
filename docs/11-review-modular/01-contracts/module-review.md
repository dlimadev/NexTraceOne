# Revisão Modular — Contracts

> **Data:** 2026-03-24  
> **Prioridade:** P1 (Fundação e Rotas Quebradas)  
> **Módulo Backend:** `src/modules/catalog/` (subdomínio Contracts)  
> **Módulo Frontend:** `src/frontend/src/features/contracts/`  
> **Fonte de verdade:** Código do repositório

---

## 1. Propósito do Módulo

O módulo **Contracts** é um dos pilares centrais do NexTraceOne, responsável pela governança de contratos de API. Ele cobre:

- Catálogo de contratos (REST, SOAP, Event, Background Service)
- Criação e edição de contratos via Draft Studio
- Workspace completo com 15 secções (Summary, Contract, Versioning, Compliance, Validation, Definition, Operations, Schemas, Security, Changelog, Approvals, Consumers, Dependencies, AI Agents)
- Versionamento e histórico de contratos
- Validação e violações (Spectral rulesets)
- Transições de ciclo de vida (Draft → InReview → Published → Deprecated)
- Exportação de contratos
- Entidades canónicas (canonical entities)
- Portal de contratos (developer-facing)

---

## 2. Aderência ao Produto

| Aspecto | Avaliação | Observação |
|---------|-----------|------------|
| Alinhamento com a visão | ✅ Forte | Contract Governance é pilar central do NexTraceOne |
| Source of Truth | ✅ Forte | Contratos como first-class citizens |
| Completude funcional | ⚠️ Parcial | 3 páginas existem mas não estão roteadas |
| Maturidade backend | ✅ Alta | 430+ testes, 3 DbContexts, persistência real |
| Maturidade frontend | ⚠️ Parcial | Páginas órfãs, rotas quebradas |

---

## 3. Páginas e Ações do Frontend

### 3.1 Páginas Roteadas (Funcionais)

| Página | Rota | Permissão | Estado |
|--------|------|-----------|--------|
| ContractCatalogPage | `/contracts` | contracts:read | ✅ Funcional — lista contratos com filtros por protocolo e lifecycle state |
| CreateServicePage | `/contracts/new` | contracts:write | ✅ Funcional — wizard com modo visual e importação |
| DraftStudioPage | `/contracts/studio/:draftId` | contracts:write | ✅ Funcional — editor de drafts com tabs (spec/metadata/preview) |
| ContractWorkspacePage | `/contracts/:contractVersionId` | contracts:read | ✅ Funcional — workspace completo com 15 secções e StudioRail |

### 3.2 Páginas NÃO Roteadas (Problema Crítico)

| Página | Rota esperada | Existência no Menu | Estado |
|--------|--------------|-------------------|--------|
| **ContractGovernancePage** | `/contracts/governance` | ✅ Sim (sidebar.contractGovernance) | ❌ **SEM ROTA no App.tsx** — página existe, ficheiro funcional, mas não importada nem roteada |
| **SpectralRulesetManagerPage** | `/contracts/spectral` | ✅ Sim (sidebar.spectralRulesets) | ❌ **SEM ROTA no App.tsx** — página existe, ficheiro funcional, hooks useSpectralRulesets() existem |
| **CanonicalEntityCatalogPage** | `/contracts/canonical` | ✅ Sim (sidebar.canonicalEntities) | ❌ **SEM ROTA no App.tsx** — página existe, hooks useCanonicalEntities() existem |
| **ContractPortalPage** | `/contracts/portal` | ❌ Não | ❌ **ÓRFÃ** — não está no menu nem roteada |

### 3.3 Páginas Órfãs no Catalog (Possíveis Duplicatas)

Existem 3 páginas no módulo `catalog/pages/` que parecem duplicatas de funcionalidades de contratos:

| Ficheiro | Módulo | Observação |
|----------|--------|------------|
| `ContractDetailPage.tsx` | catalog | Possível versão anterior de ContractWorkspacePage |
| `ContractListPage.tsx` | catalog | Possível versão anterior de ContractCatalogPage |
| `ContractsPage.tsx` | catalog | Possível versão anterior de ContractCatalogPage |

---

## 4. Rotas e Navegação

### 4.1 Rotas Definidas no App.tsx

```
/contracts                    → ContractCatalogPage (contracts:read)
/contracts/new                → CreateServicePage (contracts:write)
/contracts/studio/:draftId    → DraftStudioPage (contracts:write)
/contracts/studio             → Redirect → /contracts
/contracts/legacy             → Redirect → /contracts
/contracts/:contractVersionId → ContractWorkspacePage (contracts:read)
```

### 4.2 Rotas em Falta (Itens de Menu Sem Rota)

| Item de Menu | i18n Key | Rota Esperada | Impacto |
|-------------|----------|---------------|---------|
| Contract Governance | sidebar.contractGovernance | /contracts/governance | Utilizador clica → redirect para / (catch-all) |
| Spectral Rulesets | sidebar.spectralRulesets | /contracts/spectral | Utilizador clica → redirect para / (catch-all) |
| Canonical Entities | sidebar.canonicalEntities | /contracts/canonical | Utilizador clica → redirect para / (catch-all) |

### 4.3 Ações Necessárias

| Ação | Prioridade | Esforço |
|------|-----------|---------|
| Adicionar rota `/contracts/governance` no App.tsx com lazy import de ContractGovernancePage | 🔴 Crítica | 15 min |
| Adicionar rota `/contracts/spectral` no App.tsx com lazy import de SpectralRulesetManagerPage | 🔴 Crítica | 15 min |
| Adicionar rota `/contracts/canonical` no App.tsx com lazy import de CanonicalEntityCatalogPage | 🔴 Crítica | 15 min |
| Decidir destino de ContractPortalPage — rotear como `/contracts/portal` ou remover | 🟡 Média | 30 min |
| Avaliar páginas duplicadas em catalog/pages/ — consolidar ou remover | 🟡 Média | 1-2 horas |

---

## 5. Integração com Backend

### 5.1 API Clients

| Cliente API | Ficheiro | Métodos Principais |
|------------|---------|-------------------|
| contractsApi | `api/contracts.ts` | getDetail, listRuleViolations, getHistory, getSummary, getContractList, transition, export |
| contractStudioApi | `api/contractStudio.ts` | createDraft, getDraft, updateDraft, submitForReview, publish |

### 5.2 Hooks React Query (12 hooks)

| Hook | Propósito | API Backend |
|------|-----------|-------------|
| useContractList | Lista contratos com filtros | GET /api/v1/contracts |
| useContractDetail | Detalhe de contrato | GET /api/v1/contracts/:id |
| useContractDiff | Diff entre versões | GET /api/v1/contracts/:id/diff |
| useContractExport | Exportação de contrato | GET /api/v1/contracts/:id/export |
| useContractHistory | Histórico de versões | GET /api/v1/contracts/:id/history |
| useContractTransition | Transições de lifecycle | POST /api/v1/contracts/:id/transition |
| useContractViolations | Violações de regras | GET /api/v1/contracts/:id/violations |
| useDraftWorkflow | Fluxo de drafts | POST /api/v1/contracts/drafts/* |
| useSpectralRulesets | Gestão de rulesets | GET/POST /api/v1/rulesets |
| useCanonicalEntities | Entidades canónicas | GET /api/v1/contracts/canonical |
| useValidation | Validação de contratos | POST /api/v1/contracts/:id/validate |

### 5.3 Backend — Subdomínio Contracts (no módulo Catalog)

O backend de contratos reside dentro do módulo Catalog:
- **DbContext:** ContractsDbContext
- **Entidades:** ContractDraft, ContractReview, ContractScorecard, ContractExample, SpectralRuleset
- **Endpoints:** `/api/v1/contracts/*`, `/api/v1/contracts/drafts/*`

---

## 6. Regras de Negócio

| Regra | Estado | Evidência |
|-------|--------|-----------|
| Contratos têm ciclo de vida (Draft → InReview → Published → Deprecated) | ✅ Implementada | useContractTransition, LifecycleBadge |
| Validação por Spectral rulesets | ⚠️ Parcial | Hook useSpectralRulesets existe, página não roteada |
| Versionamento com histórico | ✅ Implementada | useContractHistory, VersioningSection |
| Diff entre versões | ✅ Implementada | useContractDiff |
| Aprovações de contratos | ✅ Implementada | ApprovalsSection no Workspace |
| Compliance scoring | ✅ Implementada | ComplianceScoreCard, ComplianceSection |
| Suporte a REST, SOAP, Event, Background Service | ✅ Implementada | Visual builders (VisualRestBuilder, VisualSoapBuilder, VisualEventBuilder, VisualWorkserviceBuilder) |

---

## 7. Banco de Dados

| Aspecto | Detalhe |
|---------|---------|
| DbContext | ContractsDbContext (dentro do módulo Catalog) |
| Entidades | ContractDraft, ContractReview, ContractScorecard, ContractExample, SpectralRuleset |
| Migrations | InitialCreate |
| Multi-tenancy | ✅ RLS via TenantRlsInterceptor |
| Auditoria | ✅ AuditInterceptor |
| Soft Delete | ✅ Suportado |
| Outbox | ✅ Pattern para eventos de integração |

---

## 8. i18n / Traduções

| Aspecto | Estado |
|---------|--------|
| Chaves de menu | ✅ sidebar.contractCatalog, sidebar.createContract, sidebar.contractStudio, sidebar.contractGovernance, sidebar.spectralRulesets, sidebar.canonicalEntities |
| Chaves de página | ✅ contracts.catalog.*, contracts.create.*, contracts.studio.*, contracts.workspace.* |
| Locales suportados | en, es, pt-BR, pt-PT |
| Cobertura | ⚠️ A verificar para páginas não roteadas |

---

## 9. Layout e UX

| Aspecto | Avaliação |
|---------|-----------|
| Design system tokens | ✅ Usa tokens --nto-* |
| Loading states | ✅ CatalogSkeleton, LoadingState |
| Error states | ✅ ErrorState |
| Empty states | ✅ EmptyState |
| Responsividade | ⚠️ A verificar |
| Componentes reutilizáveis | ✅ 8 shared components (ServiceTypeBadge, ComplianceScoreCard, StateIndicators, ContractQuickActions, ProtocolBadge, LifecycleBadge, ContractHeader) |
| Workspace multi-secção | ✅ 15 secções com StudioRail |
| Visual Builders | ✅ REST, SOAP, Event, Background Service |

---

## 10. Segurança / Autorização

| Aspecto | Estado |
|---------|--------|
| ProtectedRoute | ✅ Todas as rotas protegidas |
| Permissões granulares | ✅ contracts:read, contracts:write |
| Validação no backend | ✅ RequirePermission nos endpoints |
| Rate limiting | ✅ Aplicado nos endpoints |

---

## 11. Auditoria / Observabilidade

| Aspecto | Estado |
|---------|--------|
| Audit trail | ✅ Via AuditInterceptor |
| Outbox events | ✅ Publicação de eventos de integração |
| Logging estruturado | ✅ Via Serilog |

---

## 12. IA

| Aspecto | Estado |
|---------|--------|
| AiAgentsSection no Workspace | ✅ Secção dedicada a agentes IA no workspace de contrato |
| Geração assistida por IA | ⚠️ Prevista na visão do produto, implementação parcial |

---

## 13. Agents

| Aspecto | Estado |
|---------|--------|
| AiAgentsSection | ✅ Integrado no ContractWorkspacePage |
| Backend integration | ⚠️ Depende do módulo AI Knowledge (~20-25% de maturidade) |

---

## 14. Documentação Funcional

| Documento | Existe | Estado |
|-----------|--------|--------|
| docs/SERVICE-CONTRACT-GOVERNANCE.md | ✅ | Alinhado com a visão |
| docs/CONTRACT-STUDIO-VISION.md | ✅ | Visão mais ampla que a implementação |
| docs/user-guide/service-catalog.md | ✅ | Parcialmente cobre contratos |
| Documentação de API específica | ❌ | Não existe |

---

## 15. Documentação Técnica

| Aspecto | Estado |
|---------|--------|
| README do módulo | ❌ Não existe |
| Documentação de hooks | ❌ Não existe |
| Documentação de tipos | ❌ Não existe |
| Inline comments | ⚠️ Mínimos |

---

## 16. Resumo de Ações

### Ações Críticas (P0)

| # | Ação | Ficheiro(s) | Esforço |
|---|------|------------|---------|
| 1 | Adicionar lazy import + rota para ContractGovernancePage | App.tsx | 15 min |
| 2 | Adicionar lazy import + rota para SpectralRulesetManagerPage | App.tsx | 15 min |
| 3 | Adicionar lazy import + rota para CanonicalEntityCatalogPage | App.tsx | 15 min |

### Ações Importantes (P1)

| # | Ação | Ficheiro(s) | Esforço |
|---|------|------------|---------|
| 4 | Decidir destino de ContractPortalPage (rotear ou remover) | App.tsx, AppSidebar.tsx | 30 min |
| 5 | Avaliar e consolidar páginas duplicadas em catalog/pages/ | ContractDetailPage, ContractListPage, ContractsPage | 2h |
| 6 | Verificar cobertura i18n para páginas não roteadas | locales/*.json | 1h |

### Ações de Melhoria (P2)

| # | Ação | Esforço |
|---|------|---------|
| 7 | Criar documentação de API endpoints de contratos | 2h |
| 8 | Criar README do módulo frontend contracts | 1h |
| 9 | Documentar hooks e tipos | 2h |
| 10 | Validar integração backend real (vs mocks) para todas as páginas | 4h |
