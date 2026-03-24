# NexTraceOne — Auditoria Estrutural do Repositório

**Data:** 2026-03-24
**Versão:** 1.0
**Autor:** Auditoria Automatizada — Claude Structural Audit
**Branch auditado:** `claude/structural-audit-nextraceone-EElst`

---

## 1. Resumo Executivo

### Estado Geral

O repositório NexTraceOne contém um produto de complexidade elevada e arquitetura bem estruturada, cobrindo 9 módulos de backend em C#/.NET e 15 módulos de frontend em React/TypeScript, com mais de 100 páginas mapeadas e 351 ficheiros `.md` de documentação.

O produto está em estado de **transição entre MVP avançado e produto completo**. A arquitetura de backend é sólida, com separação clara de domínios e DbContexts por subdomínio. O frontend é funcional mas apresenta **divergências críticas entre o menu de navegação e as rotas reais**, além de páginas que existem no código mas não possuem rota registada no router.

A documentação acumulou camadas históricas de fases (Phase-0 a Phase-9, Wave-1 a Wave-Final) que hoje representam **artefactos operacionais passados e não o estado atual do produto**. Há duplicação e contradição entre documentos de fases distintas. A documentação funcional e de produto (módulos, páginas, UX) é **escassa e desatualizada**.

### Principais Achados

| Categoria | Achado | Severidade |
|-----------|--------|------------|
| **Menu vs Rotas** | 3 itens do menu apontam para rotas sem handler real no router | Alta |
| **Páginas órfãs** | 4 páginas existem no código mas não têm rota no App.tsx | Alta |
| **Sidebar vs Router** | `sidebar.contractStudio` aponta para `/contracts/studio` que é um redirect para `/contracts` | Média |
| **Documentação** | 351 .md com >60% sendo histórico de fases/waves operacionais | Média |
| **Módulos backend sem doc** | Módulos como `OperationalIntelligence`, `Notifications`, `Configuration` têm documentação mínima | Média |
| **i18n parcial** | 4 locales existem mas cobertura não foi auditada módulo a módulo | Média |
| **Backend SIM/PLAN** | Vários handlers marcados como simulados ou planejados (ver IMPLEMENTATION-STATUS.md) | Alta |
| **Módulo Contracts** | 4 sub-páginas presentes (canonical, governance, spectral, portal) sem rota em App.tsx | Alta |

### Nível de Aderência Documentação ↔ Código

| Dimensão | Aderência | Observação |
|----------|-----------|------------|
| Arquitetura backend | **Alta** | Módulos, DbContexts e endpoints correspondem à estrutura descrita |
| Páginas e rotas | **Baixa** | MODULES-AND-PAGES.md é superficial; divergências reais no router |
| Menu de navegação | **Baixa** | Itens do menu sem rota, rotas sem item de menu |
| Estado de implementação | **Média** | IMPLEMENTATION-STATUS.md existe e é a referência mais confiável |
| Documentação funcional | **Baixa** | User guide incompleto; docs de módulo ausentes ou por fase |
| AI/Agents | **Média** | Documentação existe mas está espalhada em 5+ arquivos distintos |

### Qualidade da Organização Atual

- Estrutura de pastas do backend: **Boa** (DDD por módulo, clara separação de camadas)
- Estrutura de pastas do frontend: **Boa** (por feature, convenção consistente)
- Organização da documentação: **Fraca** (351 ficheiros sem hierarquia de relevância atual)
- Navegação do produto: **Fraca** (menu com divergências e itens sem destino funcional)

---

## 2. Inventário Documental

### Totais

| Categoria | Qtd |
|-----------|-----|
| Total de ficheiros `.md` no repositório | ~357 |
| Ficheiros `.md` em `docs/` | 351 |
| Ficheiros `.md` em `src/frontend/` | 3 |
| Outros `.md` (root, tests, build) | 3 |

### Distribuição por Pasta

| Pasta | Ficheiros | Tipo Dominante |
|-------|-----------|----------------|
| `docs/` (raiz) | 46 | Estratégia, visão, arquitetura, gaps |
| `docs/architecture/` | 47 | ADRs, phase-0 a phase-9, environments |
| `docs/audits/` | 43 | Auditorias por phase e wave |
| `docs/execution/` | 95 | Guias de execução, notifications, configuration |
| `docs/assessment/` | 12 | Avaliação de estado atual |
| `docs/observability/` | 9 | Telemetria, providers, coleta |
| `docs/runbooks/` | 11 | Operação em produção |
| `docs/security/` | 10 | Segurança, hardening |
| `docs/release/` | 7 | Gates de release, ZR-series |
| `docs/user-guide/` | 8 | Guia do utilizador |
| `docs/deployment/` | 6 | CI/CD, Docker, migração |
| `docs/quality/` | 5 | Testes e estratégia |
| `docs/acceptance/` | 5 | Planos de aceite |
| `docs/rebaseline/` | 2 | Rebaselines arquiteturais |
| `docs/reviews/` | 3 | Revisões críticas |
| `docs/reliability/` | 3 | Modelo de reliability |
| `docs/aiknowledge/` | 3 | Flows de AI knowledge |
| `docs/engineering/` | 3 | Anti-demo, product done |
| `docs/frontend/` | 3 | Auditoria e inventário frontend |
| `docs/governance/` | 1 | Phase-5 governance enrichment |
| Outros subdirs | 5 | planos, checklists, telemetry, testing, roadmap |

### Documentos Mais Críticos (leitura prioritária)

1. `docs/IMPLEMENTATION-STATUS.md` — taxonomia de implementação (KEEP)
2. `docs/ARCHITECTURE-OVERVIEW.md` — visão arquitetural geral (KEEP_WITH_REWRITE)
3. `docs/DOMAIN-BOUNDARIES.md` — limites de domínio (KEEP)
4. `docs/BACKEND-MODULE-GUIDELINES.md` — convenções backend (KEEP)
5. `docs/FRONTEND-ARCHITECTURE.md` — arquitetura frontend (KEEP)
6. `docs/assessment/` (série completa) — avaliação de estado atual mais recente (KEEP→ARCHIVE)
7. `docs/SECURITY-ARCHITECTURE.md` — segurança (KEEP)
8. `docs/LOCAL-SETUP.md` — onboarding (KEEP_WITH_REWRITE)
9. `docs/I18N-STRATEGY.md` — estratégia i18n (KEEP)
10. `docs/runbooks/` (série) — operação (KEEP)

### Candidatos a Reescrita

- `docs/MODULES-AND-PAGES.md` — extremamente superficial, não reflete o estado real
- `docs/ROADMAP.md` — provavelmente baseado em fases já executadas
- `docs/PRODUCT-VISION.md` — pode conter metas de MVP já superadas
- `docs/PLATFORM-CAPABILITIES.md` — pode não refletir capacidades reais atuais
- `docs/user-guide/` (série) — incompleto, não cobre todos os módulos

### Candidatos a Merge

- `AI-ARCHITECTURE.md` + `AI-GOVERNANCE.md` + `AI-ASSISTED-OPERATIONS.md` + `AI-DEVELOPER-EXPERIENCE.md` → único `AI-HUB-ARCHITECTURE.md`
- `ANALISE-CRITICA-ARQUITETURAL.md` + `SOLUTION-GAP-ANALYSIS.md` + `CORE-FLOW-GAPS.md` → `GAP-ANALYSIS-MASTER.md`
- `docs/architecture/ADR-001-*` (raiz) + `docs/architecture/adr/ADR-001-*` (subdir) → eliminar duplicação
- `docs/architecture/ADR-002-migration-policy.md` e `docs/architecture/adr/ADR-002-migration-policy.md` → duplicado direto

### Candidatos a Arquivo (ARCHIVE)

- Toda a série `docs/architecture/phase-0/` a `docs/architecture/phase-9/` — histórico de fases
- `docs/audits/PHASE-*` e `docs/audits/WAVE-*` — auditorias por fase passadas
- `docs/execution/PHASE-*` e `docs/execution/WAVE-*` — guias de execução por fase
- `docs/acceptance/` — planos de aceite de versão anterior
- `docs/rebaseline/` — rebaselines passados
- `docs/EXECUTION-BASELINE-PR1-PR16.md` — histórico de PRs
- `docs/WAVE-1-*.md` e `docs/POST-PR16-EVOLUTION-ROADMAP.md` — artefactos de wave

### Candidatos a Eliminação (DELETE_CANDIDATE)

- `docs/NexTraceOne_Avaliacao_Atual_e_Plano_de_Testes.md` — duplicado/obsoleto
- `docs/NexTraceOne_Plano_Operacional_Finalizacao.md` — plano executado
- `docs/PRODUCT-REFOUNDATION-PLAN.md` — plano executado
- `docs/GO-NO-GO-GATES.md` — gates de lançamento passados
- `docs/REBASELINE.md` — evento passado

---

## 3. Inventário Modular

### Backend — 9 Módulos + 5 Building Blocks + 3 Plataforma

| Módulo | Estado Aparente | DbContexts | Migrations | API Endpoints |
|--------|-----------------|------------|------------|---------------|
| `AIKnowledge` | Ativo | 3 | 7 | 5 modules |
| `AuditCompliance` | Parcial | 1 | 2 | 1 module |
| `Catalog` | Ativo | 3 | 3 | 5 modules |
| `ChangeGovernance` | Ativo | 4 | 4 | 9 endpoints |
| `Configuration` | Ativo | 1 | ? | 1 module |
| `Governance` | Ativo | 1 | 3 | 17 modules |
| `IdentityAccess` | Ativo | 1 | 2 | 11 endpoints |
| `Notifications` | Ativo | 1 | ? | 1 module |
| `OperationalIntelligence` | Ativo | 5 | 10+ | 7 modules |

### Frontend — 15 Módulos de Feature

| Módulo | Páginas | Estado |
|--------|---------|--------|
| `ai-hub` | 10 | Parcial — rotas existem, algumas features SIM |
| `audit-compliance` | 1 | Mínimo |
| `catalog` | 9+ | Ativo |
| `change-governance` | 6 | Ativo |
| `configuration` | 2 | Ativo (config admin) |
| `contracts` | 6 (pages) + components | Ativo, com sub-páginas sem rota |
| `governance` | 20 | Ativo — mais rico do frontend |
| `identity-access` | 12 | Ativo |
| `integrations` | 4 | Ativo |
| `notifications` | 3 | Ativo |
| `operational-intelligence` | 1 | Mínimo (só config page) |
| `operations` | 8 | Ativo |
| `product-analytics` | 5 | Parcial |
| `shared` | 1 | DashboardPage |

### Módulos Órfãos / Desalinhados

- **Frontend `operational-intelligence`**: Apenas `OperationsFinOpsConfigurationPage` — sem página principal no menu. Backend tem 5 DbContexts ativos.
- **Backend `AuditCompliance`**: 1 endpoint, 1 página frontend (`/audit`) — módulo mínimo.
- **Backend `Configuration`**: 1 endpoint — toda a configuração vive no frontend como admin pages.

---

## 4. Inventário de Páginas e Rotas

### Resumo

| Tipo | Qtd |
|------|-----|
| Rotas registadas em App.tsx | ~85 |
| Páginas únicas (excluindo detail/sub) | ~60 |
| Itens no menu sidebar | 50 |
| Itens do menu sem rota funcional | 3 |
| Rotas sem item no menu | ~30 |
| Páginas no código sem rota | 4 |

### Divergências Críticas — Menu vs Router

| Item Sidebar | Rota | Situação |
|---|---|---|
| `sidebar.contractStudio` | `/contracts/studio` | **REDIRECT** — `Navigate to="/contracts"`. Item de menu inútil. |
| `sidebar.contractGovernance` | `/contracts/governance` | **SEM ROTA** — `ContractGovernancePage.tsx` existe mas não está no App.tsx |
| `sidebar.spectralRulesets` | `/contracts/spectral` | **SEM ROTA** — `SpectralRulesetManagerPage.tsx` existe mas não está no App.tsx |
| `sidebar.canonicalEntities` | `/contracts/canonical` | **SEM ROTA** — `CanonicalEntityCatalogPage.tsx` existe mas não está no App.tsx |

### Páginas Existentes sem Rota Registada

| Ficheiro | Caminho esperado | Estado |
|---|---|---|
| `ContractGovernancePage.tsx` | `/contracts/governance` | Órfã — sem rota |
| `SpectralRulesetManagerPage.tsx` | `/contracts/spectral` | Órfã — sem rota |
| `CanonicalEntityCatalogPage.tsx` | `/contracts/canonical` | Órfã — sem rota |
| `ContractPortalPage.tsx` | `/contracts/portal` ou similar | Órfã — sem rota (há `/portal/*` para DeveloperPortalPage) |

### Rotas Existentes sem Item no Menu

Rotas que existem mas não aparecem no sidebar (algumas são sub-páginas legítimas):

| Rota | Componente | Justificação de ausência |
|------|-----------|--------------------------|
| `/search` | GlobalSearchPage | Acedida via topbar search |
| `/governance/waivers` | WaiversPage | Sub-página legítima — não tem entrada própria |
| `/governance/delegated-admin` | DelegatedAdminPage | Sub-página legítima |
| `/governance/controls` | EnterpriseControlsPage | Sub-página legítima |
| `/governance/evidence` | EvidencePackagesPage | Sub-página legítima |
| `/governance/maturity` | MaturityScorecardsPage | Sub-página legítima |
| `/governance/benchmarking` | BenchmarkingPage | Sub-página legítima |
| `/integrations/executions` | IngestionExecutionsPage | Sub-página |
| `/integrations/freshness` | IngestionFreshnessPage | Sub-página |
| `/analytics/adoption` | ModuleAdoptionPage | Sub-página de analytics |
| `/analytics/personas` | PersonaUsagePage | Sub-página |
| `/analytics/journeys` | JourneyFunnelPage | Sub-página |
| `/analytics/value` | ValueTrackingPage | Sub-página |
| `/notifications` | NotificationCenterPage | Acedida via topbar bell |
| `/notifications/preferences` | NotificationPreferencesPage | Sub-página |
| `/environments` | EnvironmentsPage | Sem item de menu — potencialmente escondida |
| `/platform/configuration/*` | (7 sub-config pages) | Sub-páginas de configuração admin |

---

## 5. Estrutura do Menu

### Secções e Itens do Sidebar

O sidebar está organizado em 12 secções (`NavSection`):

| Secção | Items | Chave i18n |
|--------|-------|------------|
| `home` | Dashboard | — |
| `services` | Service Catalog, Dependency Graph | `sidebar.sectionServices` |
| `knowledge` | Source of Truth, Developer Portal | `sidebar.sectionKnowledge` |
| `contracts` | Contract Catalog, Create Contract, Contract Studio, Contract Governance, Spectral Rulesets, Canonical Entities | `sidebar.sectionContracts` |
| `changes` | Change Confidence, Change Intelligence, Workflow, Promotion | `sidebar.sectionChanges` |
| `operations` | Incidents, Runbooks, Reliability, Automation, Environment Comparison | `sidebar.sectionOperations` |
| `aiHub` | AI Assistant, AI Agents, Model Registry, AI Policies, AI Routing, AI IDE, AI Budgets, AI Audit, AI Analysis | `sidebar.sectionAiHub` |
| `governance` | Executive Overview, Reports, Compliance, Risk Center, FinOps, Policies, Packs | `sidebar.sectionGovernance` |
| `organization` | Teams, Domains | `sidebar.sectionOrganization` |
| `analytics` | Product Analytics | `sidebar.sectionAnalytics` |
| `integrations` | Integration Hub | `sidebar.sectionIntegrations` |
| `admin` | Users, Break Glass, JIT Access, Delegations, Access Review, My Sessions, Audit, Platform Operations, Platform Configuration | `sidebar.sectionAdmin` |

### Inconsistências do Menu

1. **Duplicação de ícone**: `Bot` usado tanto para `aiAssistant` como para `aiAgents` — falta diferenciação visual
2. **`ShieldCheck` duplicado**: usado em `changeConfidence` e `spectralRulesets` — inconsistência
3. **`Zap` duplicado**: usado em `changeIntelligence` e `automation`
4. **Secção `contracts` sobrecarregada**: 6 itens, dos quais 3 sem rota funcional
5. **`sidebar.platformOperations`** e **`sidebar.platformConfiguration`** estão em `admin` mas são páginas muito distintas
6. **Notificações e ambientes** não têm item no menu mas existem como rotas

---

## 6. Principais Lacunas

### Layout e UX

- Páginas com sub-rotas de governance (waivers, controls, evidence, maturity, benchmarking, delegated-admin) existem mas não têm navegação clara a partir das páginas pai
- `EnvironmentsPage` existe sem item de menu — a gestão de ambientes está invisível
- Seção `contracts` com itens de menu "quebrados" cria experiência confusa para o utilizador

### i18n

- 4 locales existem (`en`, `es`, `pt-BR`, `pt-PT`) mas a completude por módulo não foi auditada
- Risco de chaves em falta em módulos adicionados recentemente
- Labels do sidebar usam chaves i18n; as páginas podem ter texto hardcoded não rastreado

### Backend/Frontend

- `OperationalIntelligence` no backend tem 5 DbContexts e 7 API modules — mas no frontend aparece apenas como 1 página de configuração. Divergência severa.
- `AuditCompliance` no backend existe mas tem apenas 1 página no frontend
- Páginas de contracts (canonical, governance, spectral) têm componentes completos mas sem rota registada

### IA e Agents

- `AIKnowledge` tem 7 migrations e 3 DbContexts — módulo ativo
- No frontend: 10 páginas de AI Hub com rotas reais
- Documentação: espalhada em 5+ ficheiros, sem documento único consolidado por módulo

### Documentação

- 95 ficheiros em `docs/execution/` que são guias de configuração não ligados às páginas reais
- Documentação funcional por módulo (user-guide) incompleta — cobre apenas alguns módulos
- Nenhum documento descreve o estado atual das 4 páginas órfãs de contracts

---

## 7. Recomendações

### O Que Manter

- `docs/IMPLEMENTATION-STATUS.md` — mais importante referência de estado atual
- `docs/ARCHITECTURE-OVERVIEW.md`, `BACKEND-MODULE-GUIDELINES.md`, `DOMAIN-BOUNDARIES.md`
- `docs/SECURITY-ARCHITECTURE.md`, `SECURITY.md`
- `docs/runbooks/` (série completa)
- `docs/LOCAL-SETUP.md`, `ENVIRONMENT-VARIABLES.md`
- `docs/I18N-STRATEGY.md`, `DESIGN-SYSTEM.md`
- `docs/deployment/` (série)
- `docs/quality/TEST-STRATEGY-AND-LAYERS.md`
- `docs/IMPLEMENTATION-STATUS.md`

### O Que Reescrever

- `docs/MODULES-AND-PAGES.md` — deve ser gerado a partir desta auditoria
- `docs/PRODUCT-SCOPE.md`, `PRODUCT-VISION.md` — alinhar com produto atual
- `docs/ROADMAP.md` — eliminar referências a fases já concluídas
- `docs/user-guide/` — expandir para cobrir todos os módulos e páginas reais

### O Que Consolidar

- 4 documentos de AI → `docs/ai-hub/AI-HUB-ARCHITECTURE.md`
- 3 documentos de gap analysis → `docs/GAP-ANALYSIS-MASTER.md`
- ADRs duplicados entre `docs/architecture/` e `docs/architecture/adr/`

### O Que Arquivar

- `docs/architecture/phase-*/` (todos os phase-0 a phase-9)
- `docs/audits/PHASE-*` e `docs/audits/WAVE-*`
- `docs/execution/PHASE-*` e `docs/execution/WAVE-*`
- `docs/acceptance/` completo
- `docs/rebaseline/` completo

### O Que Pode Ser Excluído

- `docs/NexTraceOne_Avaliacao_Atual_e_Plano_de_Testes.md`
- `docs/NexTraceOne_Plano_Operacional_Finalizacao.md`
- `docs/GO-NO-GO-GATES.md`
- `docs/REBASELINE.md`
- `docs/PRODUCT-REFOUNDATION-PLAN.md`

### Ordem de Trabalho Para Próxima Fase

1. **Correção de rotas** — registar `ContractGovernancePage`, `SpectralRulesetManagerPage`, `CanonicalEntityCatalogPage`, `ContractPortalPage` em App.tsx ou remover itens do sidebar
2. **Módulo Contracts** — auditoria detalhada de sub-páginas e fluxos
3. **Módulo OperationalIntelligence** — divergência severa backend vs frontend
4. **Módulo Governance** — 20 páginas, muitas sub-rotas sem navegação explícita
5. **Módulo AI Hub** — consolidar documentação e mapear handlers SIM/IMPL
6. **i18n cobertura** — audit completo por módulo
7. **Módulo Notifications** — validar fluxo real vs sidebar ausência
8. **Módulo AuditCompliance** — escopo real e completude
9. **Documentação funcional** — reescrever user-guide por módulo
10. **Consolidação documental** — merge/archive conforme classificação

---

*Relatório gerado como base para a próxima fase de revisão detalhada módulo a módulo.*
*Ver relatórios complementares: `markdown-inventory-report.md`, `module-inventory-report.md`, `frontend-pages-and-routes-report.md`, `menu-structure-report.md`, `documentation-vs-code-gap-report.md`, `review-priority-recommendation.md`*
