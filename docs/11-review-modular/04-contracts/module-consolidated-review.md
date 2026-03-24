# Contracts — Consolidated Module Report

> Gerado a partir da consolidação de todos os relatórios de auditoria e revisão modular do NexTraceOne.
> Última atualização: 2026-03-24

---

## 1. Visão Geral do Módulo

O módulo **Contracts** é um dos pilares centrais do NexTraceOne, responsável pela **governança de contratos de API** como first-class citizens. Alinha-se diretamente com o pilar **Contract Governance** do produto e reforça o papel do NexTraceOne como **Source of Truth** para contratos, serviços e conhecimento operacional.

### Âmbito funcional

- Catálogo de contratos (REST, SOAP, Event/Kafka, Background Service)
- Criação e edição via Draft Studio com visual builders
- Workspace completo com 15 secções (Summary, Contract, Versioning, Compliance, Validation, Definition, Operations, Schemas, Security, Changelog, Approvals, Consumers, Dependencies, AI Agents)
- Versionamento e histórico de contratos com diff entre versões
- Validação via Spectral rulesets
- Transições de ciclo de vida (Draft → InReview → Published → Deprecated)
- Exportação de contratos
- Entidades canónicas (canonical entities)
- Portal de contratos (developer-facing)

### Posição na arquitetura

O módulo reside no subdomínio Contracts do módulo backend `catalog` (`src/modules/catalog/`), com frontend em `src/frontend/src/features/contracts/`. Depende do **Identity & Access** (autenticação/autorização) e do **Catalog** (base de serviços), e é consumido pelo **Change Governance**, **Operational Intelligence** e por todos os módulos que necessitam de informação contratual.

---

## 2. Estado Atual

| Dimensão | Valor | Indicador |
|----------|-------|-----------|
| **Maturidade global** | **68%** | 🟡 |
| Backend | 75% | 🟡 |
| Frontend | 60% | 🟡 |
| Documentação | 55% | 🟡 |
| Testes | 80% (430+ no catalog) | 🟢 |
| **Prioridade** | **P1** | 🔴 |
| **Criticidade na matrix** | CRITICAL (5/5) | 🔴 |
| **Status** | ⚠️ Parcialmente funcional — contém P0 BLOCKER | |
| **Tipo de correção** | QUICK_WIN (rotas) + LOCAL_FIX (completude) | |
| **Wave de execução** | Wave 0 (rotas) + Wave 2-3 (backend/frontend) | |
| **Alvo de maturidade** | 85%+ | |

---

## 3. Problemas Críticos e Bloqueadores

### 🔴 P0 BLOCKER — 3 rotas partidas no frontend

**Causa raiz:** Frontend construído antes da estabilidade de rotas. As páginas e os hooks existem e são funcionais, mas nunca foram importadas e registadas em `App.tsx`.

| Rota esperada | Página | Ficheiro | Hook associado | Item no menu |
|---------------|--------|----------|----------------|-------------|
| `/contracts/governance` | ContractGovernancePage | `contracts/governance/ContractGovernancePage.tsx` | — | `sidebar.contractGovernance` |
| `/contracts/spectral` | SpectralRulesetManagerPage | `contracts/spectral/SpectralRulesetManagerPage.tsx` | `useSpectralRulesets()` | `sidebar.spectralRulesets` |
| `/contracts/canonical` | CanonicalEntityCatalogPage | `contracts/canonical/CanonicalEntityCatalogPage.tsx` | `useCanonicalEntities()` | `sidebar.canonicalEntities` |

**Sintomas:**
- O utilizador vê os itens no sidebar (secção contracts — itens 9, 10 e 11 de 6)
- Ao clicar, é redirecionado para `/` (catch-all) ou vê página em branco
- 3 dos 6 itens de menu da secção contracts estão não-funcionais — itens 4, 5 e 6 de 6 (50% do menu de contracts)

**Correção:** Adicionar lazy imports e rotas em `App.tsx` com `ProtectedRoute` — esforço estimado: **45 minutos**.

### 🟠 P1 — Páginas órfãs e código morto

| Página | Localização | Problema |
|--------|-------------|----------|
| ContractPortalPage | `contracts/portal/ContractPortalPage.tsx` | Sem rota e sem item no menu — completamente órfã |
| ContractDetailPage | `catalog/pages/ContractDetailPage.tsx` | Provável versão anterior de ContractWorkspacePage — duplicata |
| ContractListPage | `catalog/pages/ContractListPage.tsx` | Provável versão anterior de ContractCatalogPage — duplicata |
| ContractsPage | `catalog/pages/ContractsPage.tsx` | Provável versão anterior de ContractCatalogPage — duplicata |

---

## 4. Problemas por Camada

### 4.1 Frontend

#### Rotas e navegação

- 🔴 **3 rotas partidas** — descrito na secção 3 acima
- 🟠 **ContractPortalPage órfã** — decidir: rotear como `/contracts/portal` ou remover
- 🟠 **3 páginas legacy no catalog/pages/** — consolidar ou remover para eliminar confusão
- 🟢 4 rotas funcionais: `/contracts`, `/contracts/new`, `/contracts/studio/:draftId`, `/contracts/:contractVersionId`
- 🟢 2 redirects configurados: `/contracts/studio` → `/contracts`, `/contracts/legacy` → `/contracts`

#### Componentes e UX

- 🟢 Design system tokens `--nto-*` utilizados consistentemente
- 🟢 Loading states (CatalogSkeleton), error states (ErrorState), empty states (EmptyState)
- 🟢 8 componentes partilhados (ServiceTypeBadge, ComplianceScoreCard, StateIndicators, ContractQuickActions, ProtocolBadge, LifecycleBadge, ContractHeader)
- 🟢 Visual Builders para REST, SOAP, Event e Background Service
- 🟢 Workspace com 15 secções e StudioRail
- 🟡 Responsividade por verificar

#### Hooks e integração API

- 🟢 12 hooks React Query implementados (useContractList, useContractDetail, useContractDiff, useContractExport, useContractHistory, useContractTransition, useContractViolations, useDraftWorkflow, useSpectralRulesets, useCanonicalEntities, useValidation)
- 🟢 2 API clients: `contractsApi` e `contractStudioApi`
- 🟡 Validar se todos os hooks comunicam com endpoints reais (vs mocks)

#### Permissões e guards

- 🟢 Todas as 4 rotas funcionais protegidas com `ProtectedRoute`
- 🟢 Permissões granulares: `contracts:read`, `contracts:write`
- 🔴 3 rotas sem registo não têm guard — porque não existem em `App.tsx`

#### i18n

- 🟢 Chaves de menu: `sidebar.contractCatalog`, `sidebar.createContract`, `sidebar.contractStudio`, `sidebar.contractGovernance`, `sidebar.spectralRulesets`, `sidebar.canonicalEntities`
- 🟢 Chaves de página: `contracts.catalog.*`, `contracts.create.*`, `contracts.studio.*`, `contracts.workspace.*`
- 🟢 Namespace `contracts` presente em en, pt-PT, pt-BR, es
- 🟢 Namespace `catalogContractsConfig` presente nos 4 locales
- 🟡 Cobertura i18n por verificar nas 3 páginas não roteadas — podem ter textos hardcoded que só serão visíveis após correção das rotas

---

### 4.2 Backend

#### Endpoints

| Módulo de endpoints | Rotas | Persistência |
|---------------------|-------|-------------|
| ContractsEndpointModule | `GET/POST /api/contracts`, `GET/PUT/DELETE /api/contracts/{id}`, `GET /api/contracts/{id}/versions`, `POST /api/contracts/import` | ContractsDb |
| ContractStudioEndpointModule | `POST /api/contracts/studio/generate`, `POST .../validate`, `POST .../diff`, `POST .../compatibility` | ContractsDb |

- 🟢 CRUD completo para contratos
- 🟢 Endpoints de Studio para geração, validação, diff e compatibilidade
- 🟢 Autenticação JWT em todos os endpoints
- 🟢 Permissões `contracts:read`, `contracts:write`, `contracts:import` aplicadas
- 🟠 **Rate limiting inadequado** em `/api/contracts/import` — usa política Global (100 req/60s), deveria usar `data-intensive` (50 req/60s) por ser operação pesada

#### Modelo de domínio

- 🟢 82 entidades no módulo catalog (inclui contratos e serviços)
- 🟢 Agregados bem definidos: `ContractAggregate` como raiz de agregado
- 🟢 Entidades: Contract, ContractVersion, ContractSchema, ApiEndpoint, EventContract, SoapService
- 🟢 Value objects para schemas e versões
- 🟢 Strongly-typed IDs (`ContractId`)
- 🟢 Domain services de compatibilidade, validação e diff
- 🟢 Domínio limpo — baixo acoplamento com infraestrutura

#### Application layer

- 🟡 Contract Studio parcialmente completo — handlers de create, edit e version a completar
- 🟡 Fluxo de approval workflow a completar
- 🟡 Validação de compatibilidade (breaking change detection) incompleta

---

### 4.3 Database

#### Schema e persistência

| DbContext | Entidades (est.) | Entity configs (est.) | Base de dados | Migrações |
|-----------|------------------|-----------------------|---------------|-----------|
| ContractsDbContext | ~35 | ~20 | `nextraceone_catalog` | 6 (partilhadas com catalog) |

- 🟢 Multi-tenancy via RLS (`TenantRlsInterceptor`)
- 🟢 Auditoria automática (`AuditInterceptor`) — CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
- 🟢 Soft delete com global query filter
- 🟢 Outbox pattern para domain events
- 🟢 Owned entities: `ContractVersion.Signature`, `ContractVersion.Provenance`
- 🟢 Conversões enum→string para `ContractType`, `ContractStatus`
- 🟢 Strongly-typed ID: `ValueConverter<ContractId, Guid>`

#### Índices e integridade

- 🟢 ~5 índices únicos (e.g., ContractVersion por ContractId + Version, Schema.Name)
- 🟡 **Ausência de RowVersion/ConcurrencyToken** em `ContractVersion` — edição concorrente de contratos é cenário real e prioritário (prioridade alta para adição)
- 🟡 Apenas 2 filtered indexes em todo o sistema — `ContractsDb` beneficiaria de filtered indexes `WHERE IsDeleted = false` para performance
- 🟡 Sem check constraints para enums na BD — qualquer string é aceite para ContractType/ContractStatus

#### Seed data

- 🟢 `seed-catalog.sql` (172 linhas) inclui 9 services, 6 APIs e contratos

---

### 4.4 Segurança

- 🟢 Todas as rotas funcionais protegidas com `ProtectedRoute`
- 🟢 Backend enforce via `RequirePermission` nos endpoints
- 🟢 Rate limiting aplicado (embora com gap no import — ver 4.2)
- 🟢 Isolamento multi-tenant via RLS em ContractsDb
- 🟢 Auditoria automática via AuditInterceptor
- 🟢 Soft delete (dados nunca eliminados fisicamente)
- 🟢 Encriptação de campos disponível via `EncryptionInterceptor` (AES-256-GCM)
- 🟠 **Versionamento sem ownership enforcement** — validar que transições de lifecycle e aprovações respeitam ownership do contrato

---

### 4.5 IA e Agentes

- 🟢 AiAgentsSection integrada no ContractWorkspacePage (secção dedicada)
- 🟢 Endpoint `POST /api/contracts/studio/generate` com rate limit `ai`
- 🟡 **Geração assistida por IA prevista na visão mas parcialmente implementada** — depende do módulo AI Knowledge (~20-25% de maturidade no backend)
- 🟡 Agents específicos para contratos (e.g., Contract Analysis Agent, Schema Suggestion Agent) por definir e registar
- 🟡 Ficheiros de revisão de agents (`07-contracts-interoperability/ai/`) em estado `NOT_STARTED` — nenhum agent documentado ou auditado

---

### 4.6 Documentação

| Documento | Estado |
|-----------|--------|
| `docs/SERVICE-CONTRACT-GOVERNANCE.md` | ✅ Existe, alinhado com visão |
| `docs/CONTRACT-STUDIO-VISION.md` | ⚠️ Existe, visão mais ampla que implementação |
| `docs/user-guide/service-catalog.md` | ⚠️ Cobre parcialmente contratos |
| README do módulo frontend | ❌ Não existe |
| README do módulo backend | ❌ Não existe |
| Documentação de API endpoints | ❌ Não existe |
| Documentação de hooks e tipos | ❌ Não existe |
| Inline comments | ⚠️ Mínimos |
| Module overview (`07-contracts-interoperability/`) | ❌ Template vazio (`NOT_STARTED`) |
| Revisões detalhadas (backend, frontend, database, AI, quality) | ❌ Todos em estado `NOT_STARTED` com placeholders |

- 🟠 **Todas as 16 fichas de revisão detalhada** em `07-contracts-interoperability/` estão vazias (apenas templates com `[A PREENCHER]`)
- 🟠 O `module-overview.md` refere incorretamente "autenticação, autorização, multi-tenancy" — conteúdo é copy-paste do template de Identity & Access, não de Contracts

---

## 5. Dependências

### O módulo Contracts depende de:

| Módulo | Tipo de dependência | Detalhe |
|--------|---------------------|---------|
| **Identity & Access** | Autenticação e autorização | JWT, permissões `contracts:*`, tenant context |
| **Catalog** | Base de serviços | ContractsDb partilha base de dados `nextraceone_catalog` com CatalogGraphDb e DeveloperPortalDb |
| **AI Knowledge** | Geração assistida | AiAgentsSection no workspace depende de AI backend (~25% maturidade) |

### Módulos que dependem de Contracts:

| Módulo | Tipo de dependência | Detalhe |
|--------|---------------------|---------|
| **Change Governance** | Contratos como contexto de mudanças | Validação de compatibilidade entre versões |
| **Operational Intelligence** | Contratos como contexto de incidentes | Correlação contrato-incidente |
| **Source of Truth** | Consulta de contratos | `ContractSourceOfTruthPage`, endpoint `/api/source-of-truth/search` acede a ContractsDb |
| **Developer Portal** | Exposição de contratos | Portal developer-facing consome dados de contratos |

---

## 6. Quick Wins

| # | Ação | Esforço | Impacto | Ficheiro(s) |
|---|------|---------|---------|------------|
| QW-1 | Adicionar lazy import + rota `/contracts/governance` → ContractGovernancePage com `ProtectedRoute(contracts:read)` | 15 min | 🔴 Elimina 1/3 do P0 | `App.tsx` |
| QW-2 | Adicionar lazy import + rota `/contracts/spectral` → SpectralRulesetManagerPage com `ProtectedRoute(contracts:write)` | 15 min | 🔴 Elimina 2/3 do P0 | `App.tsx` |
| QW-3 | Adicionar lazy import + rota `/contracts/canonical` → CanonicalEntityCatalogPage com `ProtectedRoute(contracts:read)` | 15 min | 🔴 Elimina P0 por completo | `App.tsx` |
| QW-4 | Validar coerência sidebar ↔ rotas após QW-1/2/3 | 30 min | Menu 100% funcional | `AppSidebar.tsx` |
| QW-5 | Alterar rate limit de `/api/contracts/import` de Global para `data-intensive` | 15 min | 🟠 Segurança melhorada | Backend endpoint module |
| QW-6 | Verificar cobertura i18n das 3 páginas recém-roteadas | 1h | i18n compliance | `locales/*.json` |

**Total estimado: ~2.5 horas** para eliminar o P0 e alinhar o módulo.

---

## 7. Refactors Estruturais

| # | Refactor | Esforço | Prioridade | Risco |
|---|---------|---------|------------|-------|
| SR-1 | Completar handlers do Contract Studio (create, edit, version, approval workflow) | 1-2 semanas | 🟠 Alto | Médio |
| SR-2 | Implementar validação de compatibilidade (breaking change detection) | 1 semana | 🟠 Alto | Médio |
| SR-3 | Adicionar RowVersion/ConcurrencyToken em `ContractVersion` | 2-3 dias | 🟠 Alto | Baixo-Médio |
| SR-4 | Decidir e implementar destino de ContractPortalPage (rotear ou remover) | 30 min | 🟡 Médio | Baixo |
| SR-5 | Avaliar e remover 3 páginas legacy duplicadas em `catalog/pages/` | 1-2 horas | 🟡 Médio | Baixo |
| SR-6 | Completar integração frontend ↔ backend real (60% → 80%+) — Wave 3 | 1-2 semanas | 🟠 Alto | Médio |
| SR-7 | Adicionar filtered indexes `WHERE IsDeleted = false` nas tabelas principais de contratos | 2-3 dias | 🟡 Médio | Baixo |
| SR-8 | Adicionar check constraints para enums (ContractType, ContractStatus) | 1-2 dias | 🟡 Médio | Baixo |
| SR-9 | Preencher toda a documentação de revisão detalhada (`07-contracts-interoperability/`) | 3-5 dias | 🟡 Médio | Nenhum |
| SR-10 | Criar README dos módulos frontend e backend de Contracts | 1 dia | 🟡 Médio | Nenhum |

---

## 8. Critérios de Fecho

O módulo Contracts será considerado **DONE** quando:

### Obrigatórios (bloqueiam fecho)
- [ ] **Zero rotas partidas** — todas as rotas do sidebar navegam para páginas funcionais
- [ ] **Contract Studio funcional** para REST APIs — criar, editar, versionar
- [ ] **Versionamento completo** com diff funcional entre versões
- [ ] **Approval workflow** conectado ao backend
- [ ] CRUD completo para REST, SOAP, Kafka e Background Service contracts
- [ ] Testes ≥ 80%
- [ ] Maturidade ≥ 85%

### Recomendados (melhoram qualidade)
- [ ] README do módulo criado (frontend e backend)
- [ ] Documentação de API endpoints de contratos
- [ ] RowVersion em ContractVersion
- [ ] Zero páginas órfãs — todas conectadas ou removidas
- [ ] i18n verificado em todas as páginas
- [ ] Rate limiting adequado em `/api/contracts/import`
- [ ] Agent(s) de contratos definido(s) e auditável(eis)
- [ ] Fichas de revisão detalhada preenchidas

---

## 9. Plano de Ação Priorizado

### Wave 0 — Imediato (1 dia)

| Ordem | Ação | Esforço | Tipo |
|-------|------|---------|------|
| 1 | QW-1/2/3: Adicionar 3 rotas em falta no `App.tsx` | 45 min | QUICK_WIN |
| 2 | QW-4: Validar coerência sidebar ↔ rotas | 30 min | QUICK_WIN |
| 3 | QW-5: Corrigir rate limit de `/api/contracts/import` | 15 min | QUICK_WIN |
| 4 | QW-6: Verificar i18n nas páginas recém-roteadas | 1h | QUICK_WIN |

### Wave 2 — Curto prazo (2-3 semanas)

| Ordem | Ação | Esforço | Tipo |
|-------|------|---------|------|
| 5 | SR-1: Completar backend Contract Studio (handlers) | 1-2 sem | LOCAL_FIX |
| 6 | SR-2: Validação de compatibilidade (breaking changes) | 1 sem | LOCAL_FIX |
| 7 | SR-3: RowVersion em ContractVersion | 2-3 dias | LOCAL_FIX |
| 8 | SR-4/5: Resolver páginas órfãs e duplicatas | 2h | LOCAL_FIX |

### Wave 3 — Médio prazo (2-3 semanas)

| Ordem | Ação | Esforço | Tipo |
|-------|------|---------|------|
| 9 | SR-6: Completar integração frontend ↔ backend (60% → 80%+) | 1-2 sem | LOCAL_FIX |
| 10 | SR-7/8: Filtered indexes e check constraints | 3-5 dias | LOCAL_FIX |
| 11 | SR-9/10: Documentação completa | 4-6 dias | DOCUMENTATION |

### Wave 4+ — Longo prazo

| Ordem | Ação | Esforço | Tipo |
|-------|------|---------|------|
| 12 | Definir e implementar agents de IA para contratos | 2-3 sem | STRUCTURAL |
| 13 | Geração assistida por IA funcional (depende de AI Knowledge ≥50%) | 3-4 sem | STRUCTURAL |

---

## 10. Inconsistências entre Relatórios

| # | Inconsistência | Relatórios envolvidos | Impacto | Nota |
|---|---------------|----------------------|---------|------|
| 1 | **Module overview com conteúdo de Identity** — `07-contracts-interoperability/module-overview.md` descreve "autenticação, autorização, MFA, multi-tenancy" em vez de contratos. O backend referido é `src/modules/identityaccess/` em vez de `src/modules/catalog/`. | `module-overview.md` vs `module-review.md` | 🟠 Alto — developer que consulte o overview será induzido em erro | Template copiado de Identity e nunca adaptado para Contracts |
| 2 | **README do interoperability aponta para referência errada** — refere `../02-contracts-interoperability/module-review.md` (caminho inexistente) e código `src/frontend/src/features/contracts-interoperability/` (path incorreto vs `contracts/`). | `07-contracts-interoperability/README.md` vs código real | 🟡 Médio — links partidos na documentação | Paths desatualizados |
| 3 | **Contagem de páginas divergente** — O summary refere "8 páginas (4 roteadas, 3 sem rota, 1 órfã)" mas o report de rotas conta 9 páginas órfãs globalmente, das quais 4 são de contracts (3 sem rota + 1 portal). | `modular-review-summary.md` vs `frontend-pages-and-routes-report.md` | 🟢 Baixo — diferença na contagem é cosmética, os problemas são os mesmos | Contagem depende de incluir ou não as 3 legacy do catalog |
| 4 | **Interoperability templates todos vazios** — As 16 fichas de revisão detalhada em `07-contracts-interoperability/` estão em estado `NOT_STARTED` apesar de o `module-review.md` em `01-contracts/` ter informação substantiva. | `07-contracts-interoperability/*` vs `01-contracts/module-review.md` | 🟠 Alto — duplicação de estrutura sem conteúdo, cria falsa impressão de cobertura | Revisão detalhada nunca foi executada para este módulo |
| 5 | **Prioridade P1 vs P0** — O `module-review.md` classifica o módulo como P1, mas o `module-consolidation-report.md` e o `execution-waves-plan.md` colocam a correção das rotas como P0 BLOCKER. O módulo é P1 como um todo, mas contém um problema P0. | `module-review.md` vs `module-consolidation-report.md` | 🟢 Baixo — não é contradição, é granularidade diferente | Ambos estão corretos no seu contexto |

---

*Documento consolidado gerado como parte da revisão modular do NexTraceOne. Para ações imediatas, consultar a secção 9 (Plano de Ação Priorizado).*
