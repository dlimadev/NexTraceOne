# NexTraceOne — Estado Atual do Projeto e Plano de Testes Funcional

**Data da análise:** Junho 2025  
**Base de análise:** Código real do repositório (`main`)  
**Autor:** Análise automatizada por auditoria técnica  

---

## 1. Resumo Executivo

### Estado Geral

O NexTraceOne é um projeto **ambicioso e extenso** com arquitectura modular bem definida, que se encontra numa **fase de construção intermédia**. A base arquitectural (backend e frontend) está sólida, mas a maturidade funcional varia drasticamente entre módulos.

### Perceção de Maturidade

| Aspecto | Avaliação |
|---|---|
| Arquitectura backend | ✅ Sólida — DDD, CQRS, VSA, modular monolith |
| Arquitectura frontend | ✅ Boa — React 18, lazy loading, feature-based |
| Identity & Access | ✅ Mais maduro do projeto |
| Catalog & Contracts | 🟡 Parcial — backend forte, frontend mock-enriched |
| Change Governance | 🟡 Parcial — backend + frontend com API real |
| Operations (Incidents) | 🟡 Parcial — backend + API real, demais mockados |
| AI Hub | 🟠 Estrutura + mock com fallback |
| Governance | 🟠 Frontend extenso mas todo mockado, backend sem persistência |
| Integrations | 🟠 Frontend mockado, backend minimal |
| Product Analytics | 🔴 Todo mockado |

### Números-chave

- **7 módulos backend** com Domain + Application + Infrastructure + API
- **11 bounded contexts** com DbContext + migrações EF Core
- **~90 páginas** no frontend
- **~35 componentes** no design system
- **154 ficheiros de teste** (backend unit tests)
- **6 seed SQL** para dados de desenvolvimento
- **~130 ficheiros .tsx** em features

### Áreas Mais Fortes
1. Identity & Access — backend completo, frontend funcional
2. Catalog (Graph + Contracts) — backend rico, API Layer real
3. Change Governance — Change Intelligence, Workflow, Promotion com API
4. Building Blocks — Result<T>, AggregateRoot, TypedIds, CQRS, Security

### Áreas Mais Frágeis
1. Governance — frontend extenso (20+ páginas) inteiramente mockado, backend sem DB
2. Product Analytics — 5 páginas 100% mock
3. Integrations — 4 páginas mock, backend com stub
4. AI Hub — misto mock/real, backend parcial
5. Operations (Automation, Reliability) — mockados

---

## 2. Visão Geral da Arquitectura Atual

### Backend (.NET 10)

```
NexTraceOne/
├── src/
│   ├── building-blocks/     ← Core, Application, Infrastructure, Security, Observability
│   ├── modules/
│   │   ├── identityaccess/  ← Domain, Application, Infrastructure, API, Contracts
│   │   ├── catalog/         ← Domain, Application, Infrastructure, API, Contracts
│   │   ├── changegovernance/ ← Domain, Application, Infrastructure, API, Contracts
│   │   ├── operationalintelligence/ ← Domain, Application, Infrastructure, API, Contracts
│   │   ├── auditcompliance/ ← Domain, Application, Infrastructure, API, Contracts
│   │   ├── aiknowledge/     ← Domain, Application, Infrastructure, API, Contracts
│   │   └── governance/      ← Domain, Application, Infrastructure, API, Contracts
│   ├── platform/
│   │   ├── NexTraceOne.ApiHost/      ← Modular monolith host
│   │   ├── NexTraceOne.BackgroundWorkers/ ← Worker service
│   │   └── NexTraceOne.Ingestion.Api/    ← Ingestion endpoint (skeleton)
│   └── frontend/            ← React 18 SPA (Vite)
├── tests/
│   ├── building-blocks/     ← 14 test files
│   ├── modules/             ← 120+ test files
│   └── platform/            ← Integration + E2E projects
└── tools/
    └── NexTraceOne.CLI/     ← CLI tool
```

### Padrões Implementados
- **DDD:** Entities, AggregateRoots, ValueObjects, Domain Events, Strongly Typed IDs
- **CQRS:** Commands + Queries via MediatR
- **VSA:** Vertical Slice Architecture — Command/Query + Validator + Handler + Response em ficheiro único
- **Result<T>:** Erro controlado sem exceptions
- **Guard clauses:** Ardalis.GuardClauses
- **FluentValidation:** Validação em todos os commands
- **EF Core + PostgreSQL:** 11 DbContexts com migrações independentes
- **Modular Monolith:** ApiHost monta módulos via assembly scanning

### Frontend (React 18 + Vite + TypeScript)
- Feature-based structure (`features/identity-access`, `features/contracts`, etc.)
- Lazy loading via `React.lazy()` para todas as páginas
- `@tanstack/react-query` para data fetching
- `react-i18next` para i18n (3 locales: en, pt-BR, pt-PT)
- `react-hook-form` + `zod` para formulários
- Design system próprio (`components/`) com 35+ componentes
- Shell completo: Sidebar, Topbar, CommandPalette, PersonaContext

### Pontos Fortes
- Isolamento total entre bounded contexts
- Seed data SQL para todos os módulos com DB
- Migrações EF Core por módulo
- i18n extenso (3000+ chaves por locale)
- Design system consistente (tokens, typography, icons)

### Pontos Fracos
- Governance module sem persistência própria
- Ingestion API é skeleton vazio
- Muitas páginas frontend dependem de dados mock hardcoded
- Studio de contratos depende de `studioMock.ts` para enriquecimento

---

## 3. Inventário Completo dos Módulos

### Módulo: Identity & Access
- **Descrição:** Autenticação, autorização, multi-tenancy, sessões, MFA, SSO, delegações, JIT Access, Break Glass, Access Reviews
- **Rotas:** `/login`, `/select-tenant`, `/forgot-password`, `/reset-password`, `/activate`, `/mfa`, `/invitation`, `/users`, `/break-glass`, `/jit-access`, `/delegations`, `/access-reviews`, `/my-sessions`
- **Capacidades:** Login local, SSO/OIDC, refresh token, seleção de tenant, RBAC com 67 permissões, delegação temporal, JIT Access, Break Glass, campanhas de revisão de acesso, sessões ativas
- **Backend:** 40+ features/handlers, 30+ entities, 10+ endpoints modules
- **Frontend:** 13 páginas (LoginPage, TenantSelectionPage, etc.)
- **Testes:** 32+ ficheiros de teste
- **Estado:** **PARCIAL** — backend robusto e funcional; frontend real (API calls) para auth + admin pages; algumas features avançadas (MFA real, SSO real) dependem de config externa
- **Observações:** Módulo mais maduro. Auth flow completo no frontend com `AuthContext`, `tokenStorage`, `ProtectedRoute`.

### Módulo: Catalog — Service Graph
- **Descrição:** Catálogo de serviços, grafo de dependências, discovery sources, consumer relationships, API assets, snapshots temporais
- **Rotas:** `/services`, `/services/graph`, `/services/:serviceId`
- **Capacidades:** Listagem de serviços, grafo visual, detalhe de serviço, importação (Kong, Backstage), ownership, health, subgraph, temporal diff
- **Backend:** 20+ features (RegisterServiceAsset, GetAssetGraph, ImportFromKong, InferDependencyFromOtel, etc.)
- **Frontend:** 3 páginas com API real (`useQuery`)
- **Testes:** 12+ ficheiros de teste (Graph domain + application)
- **Estado:** **PARCIAL** — backend rico, frontend consome API real, mas dados dependem de seed adequado

### Módulo: Catalog — Contracts
- **Descrição:** Governança de contratos multi-protocolo (REST, SOAP, AsyncAPI, Kafka), drafts, versões, diff semântico, signing, locking, compliance, scorecards
- **Rotas:** `/contracts`, `/contracts/new`, `/contracts/studio/:draftId`, `/contracts/:contractVersionId`, `/contracts/governance`, `/contracts/spectral`, `/contracts/canonical`, `/contracts/:id/portal`
- **Capacidades:** CRUD de drafts, review workflow (submit→approve→reject→publish), import/export, semantic diff, signing, locking, evidence packs, scorecards, rule violations, multi-protocol parsers (OpenAPI, Swagger, WSDL, AsyncAPI)
- **Backend:** 30+ features, 8 domain entities, 4 spec parsers, 4 diff calculators, canonical model builder, rule engine, scorecard calculator
- **Frontend:** 8 páginas + 16 workspace sections + 4 visual builders + catalog components
- **Testes:** 30+ ficheiros de teste (domain + application)
- **Estado:** **PARCIAL** — backend o mais completo; frontend tem catalog page (com mock enrichment), workspace com 16 secções, visual builders (REST, SOAP, Event, Workservice), DraftStudioPage. Mock enrichment para dados que backend ainda não fornece (domain, owner, compliance, etc.)

### Módulo: Catalog — Developer Portal
- **Descrição:** Portal de consumo de APIs, playground, subscriptions, code generation, analytics
- **Rotas:** `/portal`
- **Capacidades:** Search catalog, API detail, playground, subscriptions, code generation, analytics
- **Backend:** 12+ features
- **Frontend:** 1 página com API real
- **Estado:** **ESTRUTURA PRONTA, MAS INCOMPLETA** — backend tem features, frontend consome API, mas funcionalidade complexa (playground, code generation) não verificável

### Módulo: Catalog — Source of Truth
- **Descrição:** Visão unificada de serviços, contratos e referências cruzadas
- **Rotas:** `/source-of-truth`, `/source-of-truth/services/:id`, `/source-of-truth/contracts/:id`, `/search`
- **Backend:** 5 features (SearchSourceOfTruth, GlobalSearch, GetServiceSourceOfTruth, etc.)
- **Frontend:** 4 páginas com API real
- **Estado:** **PARCIAL** — backbone funcional, dependente de dados enriquecidos

### Módulo: Change Governance
- **Descrição:** Change Intelligence, releases, freeze windows, blast radius, workflow de aprovação, promoção entre ambientes, ruleset governance
- **Rotas:** `/changes`, `/changes/:changeId`, `/releases`, `/workflow`, `/promotion`
- **Capacidades:** Listagem de changes, detalhe com correlação, releases com baseline, workflow templates + instances + approvals, promoção com quality gates, spectral rulesets
- **Backend:** 4 sub-módulos (ChangeIntelligence, Workflow, Promotion, RulesetGovernance) cada com Domain + Application + Infrastructure + migrations
- **Frontend:** 5 páginas **todas com API real** (`useQuery` + `useMutation`)
- **Testes:** 7 (ChangeIntelligence) + 2 (Workflow) + 2 (Promotion) + 2 (RulesetGovernance)
- **Estado:** **PARCIAL** — módulo com boa integração frontend↔backend; funciona com dados seed

### Módulo: Operations — Incidents
- **Descrição:** Registo e gestão de incidentes, mitigação, runbooks, correlação
- **Rotas:** `/operations/incidents`, `/operations/incidents/:incidentId`, `/operations/runbooks`
- **Backend:** Features de incidentes, mitigação workflows, runbooks (IncidentDbContext + migrations)
- **Frontend:** IncidentsPage e IncidentDetailPage com API real; RunbooksPage sem API
- **Testes:** 8+ ficheiros
- **Estado:** **PARCIAL** — Incidents com API real, Runbooks estrutural

### Módulo: Operations — Reliability & Automation
- **Descrição:** Confiabilidade de serviços por equipa, workflows de automação
- **Rotas:** `/operations/reliability`, `/operations/reliability/:serviceId`, `/operations/automation`, `/operations/automation/admin`, `/operations/automation/:workflowId`
- **Frontend:** 5 páginas **todas com dados mock**
- **Backend:** Existe domínio (Runtime, Automation entities) mas sem endpoints específicos para estas páginas
- **Estado:** **MOCKADO** — frontend visual completo mas sem integração backend

### Módulo: AI Hub
- **Descrição:** AI Assistant, model registry, policies, IDE integrations, routing
- **Rotas:** `/ai/assistant`, `/ai/models`, `/ai/policies`, `/ai/ide`, `/ai/routing`
- **Capacidades:** Chat com fallback mock, AI governance domain completo (models, policies, budgets, knowledge sources, routing strategies)
- **Backend:** Domain + Application + Infrastructure + API + DbContext + migration + seed
- **Frontend:** AiAssistantPage faz API call real com fallback mock; demais 4 páginas são mock
- **Testes:** 13+ ficheiros (AIKnowledge)
- **Estado:** **PARCIAL** — backend com governance domain rico; frontend misto (assistant tenta API real, rest mock)

### Módulo: Audit & Compliance
- **Descrição:** Registo de eventos de auditoria, cadeia de integridade, retention policies
- **Rotas:** `/audit`
- **Backend:** Domain (AuditEvent, AuditChainLink, RetentionPolicy) + Infrastructure (AuditDbContext + migrations) + API
- **Frontend:** 1 página com API real
- **Testes:** Existem ficheiros
- **Estado:** **PARCIAL** — funcional mas scope limitado

### Módulo: Governance (Enterprise)
- **Descrição:** Reports, Risk Center, Compliance, FinOps, Executive views, Policy Catalog, Evidence, Controls, Packs, Teams, Domains, Waivers, Benchmarking, Maturity
- **Rotas:** 20+ rotas sob `/governance/*`
- **Backend:** Domain rico (75+ enums, 6 entities, packs, waivers, delegations) + Application (75+ features) + **Infrastructure SEM PERSISTÊNCIA** (DependencyInjection vazio)
- **Frontend:** **20+ páginas TODAS com dados mock hardcoded**
- **Estado:** **MOCKADO** — frontend visual extenso e detalhado, backend sem DB. Os handlers da Application existem estruturalmente mas não persisten dados.

### Módulo: Integrations
- **Descrição:** Integration hub, connectors, ingestion executions, freshness
- **Rotas:** `/integrations`, `/integrations/connectors/:id`, `/integrations/executions`, `/integrations/freshness`
- **Frontend:** 4 páginas **todas mock**
- **Backend:** Ingestion.Api é skeleton vazio; Governance Application tem features para connectors/ingestion
- **Estado:** **MOCKADO**

### Módulo: Product Analytics
- **Descrição:** Overview, module adoption, persona usage, journey funnels, value tracking
- **Rotas:** `/analytics`, `/analytics/adoption`, `/analytics/personas`, `/analytics/journeys`, `/analytics/value`
- **Frontend:** 5 páginas **todas mock**
- **Backend:** Governance Application tem features de analytics (RecordAnalyticsEvent, GetModuleAdoption, etc.)
- **Estado:** **MOCKADO**

### Módulo: Shared / Design System / Shell
- **Descrição:** Componentes base, shell da aplicação, topbar, sidebar, auth context, persona context
- **Componentes:** 35 componentes base (Button, Badge, Card, Modal, Tooltip, Typography, etc.) + 20 shell components
- **Estado:** **PRONTO** — design system funcional e reutilizado em todo o projeto

---

## 4. Matriz de Estado Atual por Módulo

| Módulo | Descrição Resumida | Estado | Teste Funcional? | Demo? | Integração? | Observações |
|---|---|---|---|---|---|---|
| Identity & Access | Auth, RBAC, multi-tenant | **PARCIAL** | ✅ Sim | ✅ Sim | ✅ Sim | Módulo mais maduro |
| Catalog — Graph | Catálogo de serviços | **PARCIAL** | ✅ Sim | ✅ Sim | ✅ Sim | Depende de seed data |
| Catalog — Contracts | Contratos multi-protocolo | **PARCIAL** | 🟡 Parcial | ✅ Sim | 🟡 Parcial | Mock enrichment necessário |
| Catalog — Portal | Portal do developer | **ESTRUTURA** | ❌ | 🟡 | 🟡 | Funcionalidades complexas não testáveis |
| Catalog — Source of Truth | Visão unificada | **PARCIAL** | 🟡 Parcial | ✅ Sim | ✅ Sim | Depende de dados existentes |
| Change Governance | Changes, Workflow, Promotion | **PARCIAL** | ✅ Sim | ✅ Sim | ✅ Sim | API real em todas as páginas |
| Operations — Incidents | Incidentes, mitigação | **PARCIAL** | ✅ Sim | ✅ Sim | ✅ Sim | API real |
| Operations — Reliability | Confiabilidade por equipa | **MOCKADO** | ❌ | 🟡 Visual | ❌ | Dados mock |
| Operations — Automation | Workflows de automação | **MOCKADO** | ❌ | 🟡 Visual | ❌ | Dados mock |
| AI Hub — Assistant | Chat AI governado | **PARCIAL** | 🟡 Parcial | ✅ Sim | 🟡 | Fallback mock quando backend indisponível |
| AI Hub — Models/Policies/IDE | AI governance admin | **MOCKADO** | ❌ | 🟡 Visual | ❌ | Frontend mock |
| Audit & Compliance | Eventos de auditoria | **PARCIAL** | ✅ Sim | ✅ Sim | ✅ Sim | Scope limitado |
| Governance (Enterprise) | Reports, Risk, FinOps, etc. | **MOCKADO** | ❌ | 🟡 Visual | ❌ | 20+ páginas, backend sem DB |
| Integrations | Hub de integrações | **MOCKADO** | ❌ | 🟡 Visual | ❌ | Backend skeleton |
| Product Analytics | Analytics de produto | **MOCKADO** | ❌ | 🟡 Visual | ❌ | 5 páginas mock |
| Shell / Design System | Componentes e shell | **PRONTO** | ✅ Sim | ✅ Sim | ✅ Sim | Base sólida |
| Background Workers | Jobs de expiração, outbox | **PARCIAL** | ❌ | N/A | 🟡 | Funcional para identity expiration |

---

## 5. Avaliação Detalhada por Área Funcional

### 5.1 Login / Auth

**O que existe:**
- LoginPage com `react-hook-form` + `zod` + SSO button
- TenantSelectionPage para multi-tenant
- ForgotPasswordPage, ResetPasswordPage, ActivationPage, MfaPage, InvitationPage
- AuthContext com token management (sessionStorage)
- AuthShell (split layout 55/45) + AuthCard + AuthDivider + AuthFeedback
- Backend: LocalLogin, FederatedLogin, OidcCallback, SelectTenant, RefreshToken, Logout
- ProtectedRoute com verificação de permissão

**Estado:** **PARCIAL → quase PRONTO**
- Auth flow local completo (login → tenant selection → dashboard)
- SSO/OIDC configurável mas depende de provider externo
- MFA implementado no backend mas necessita config
- Token refresh implementado

**Pronto para teste:** ✅ Login local, tenant selection, session management

### 5.2 Shell / Navegação

**O que existe:**
- AppShell com Sidebar + Topbar + ContentFrame
- AppSidebar com grupos, items, footer, header
- AppTopbar com search, actions, user menu
- CommandPalette para quick navigation
- PersonaContext com configuração por persona
- MobileDrawer para responsividade
- Breadcrumbs, PageContainer, PageSection, ContentGrid
- WorkspaceSwitcher, ContextStrip

**Estado:** **PRONTO**
- Shell completo e funcional
- Navegação opera corretamente
- Persona-aware (sidebar muda por persona)

**Pronto para teste:** ✅

### 5.3 Dashboard

**O que existe:**
- DashboardPage com stats (services, APIs, contracts, changes, incidents)
- `useQuery` para 4 APIs reais (graph, contracts summary, changes summary, incidents summary)
- PersonaQuickstart por persona
- QuickActions
- HomeWidgetCards

**Estado:** **PARCIAL**
- Consome APIs reais
- Dependente de 4 endpoints estarem operacionais e com dados
- 500 errors observados nos screenshots (auth/me, catalog/graph, contracts/summary, changes/summary, incidents/summary)

**Pronto para teste:** 🟡 Parcial — testável se backend + seed estiverem OK

### 5.4 Catálogo de Serviços

**O que existe:**
- ServiceCatalogPage (grafo visual)
- ServiceCatalogListPage (listagem)
- ServiceDetailPage (detalhe)
- APIs reais: `serviceCatalogApi.getGraph()`, `listServices()`, `getServiceDetail()`

**Estado:** **PARCIAL** — funcional com API real + seed data

### 5.5 Contratos / Service Studio

*Ver secção 6 para análise aprofundada.*

### 5.6 Change Governance

**O que existe:**
- ChangeCatalogPage — listagem com API real + filtros
- ChangeDetailPage — detalhe com mutações (accept-risk, escalate)
- ReleasesPage — releases com API real + create release
- WorkflowPage — workflows com approve/reject mutations
- PromotionPage — promotion requests com API real

**Estado:** **PARCIAL** — todas as 5 páginas usam API real via `useQuery`/`useMutation`

**Pronto para teste:** ✅ (depende de seed data)

### 5.7 Operations (Incidents)

**O que existe:**
- IncidentsPage — listagem com filtros, stats, API real
- IncidentDetailPage — timeline, mitigação, correlação, API real
- RunbooksPage — estrutura sem API

**Estado:** **PARCIAL** — Incidents operacionais, Runbooks pendente

### 5.8 Governance (Enterprise)

**O que existe (frontend):** 20+ páginas para Reports, Risk, Compliance, FinOps (4 níveis), Executive (5 sub-pages), Policy Catalog, Evidence, Controls, Governance Packs (3 pages), Waivers, Teams, Domains, Delegated Admin, Benchmarking, Maturity Scorecards

**O que existe (backend):** Domain rico + 75+ Application features + Infrastructure **SEM persistência** (DI vazio)

**Estado:** **MOCKADO** — Todas as 20+ páginas usam `const mock...` hardcoded. Visual impressionante mas não funcional.

### 5.9 AI Hub

**O que existe:**
- AiAssistantPage — chat que tenta API real, fallback mock
- ModelRegistryPage, AiPoliciesPage, IdeIntegrationsPage, AiRoutingPage — todos mock
- Backend: AiGovernanceDbContext com migration + seed, domain entities (Models, Policies, Budgets, Routing)

**Estado:** **PARCIAL** — Assistant com integração real limitada; admin pages mock

### 5.10 Integrations

**O que existe:**
- IntegrationHubPage, ConnectorDetailPage, IngestionExecutionsPage, IngestionFreshnessPage — todos mock
- Backend: Ingestion.Api é skeleton; Governance Application tem features mas sem infra real

**Estado:** **MOCKADO**

### 5.11 Product Analytics

**O que existe:**
- 5 páginas (Overview, Adoption, Personas, Journeys, Value) — todas mock

**Estado:** **MOCKADO**

### 5.12 Audit & Compliance

**O que existe:**
- AuditPage com API real
- Backend: AuditDbContext + migrations + seed

**Estado:** **PARCIAL** — funcional dentro do scope implementado

---

## 6. Avaliação Detalhada do Módulo de Contratos / Service Studio

### 6.1 Catálogo de Contratos
- **Implementação:** `ContractCatalogPage` com `useContractList`, filtros, sorting, badges
- **Completude:** 80% — funcional, mas usa `mockEnrichment.ts` para enriquecer dados do backend com domain/team/compliance falsos
- **Testável:** ✅ Parcialmente (listagem funciona, detalhes enriched são mock)

### 6.2 Criação de Serviço / Contrato
- **Implementação:** `CreateServicePage` — wizard 3 steps (tipo → modo → detalhes), 5 tipos de serviço, 6 modos de criação
- **Backend:** `CreateDraft` handler completo com validator + repository
- **Completude:** 70% — criação funciona via API, navegação para studio corrigida (DraftStudioPage)
- **Testável:** ✅ (criar draft e navegar para studio)

### 6.3 Studio / Workspace (Contract Versions)
- **Implementação:** `ContractWorkspacePage` com 16 secções:
  - SummarySection, DefinitionSection, ContractSection (source editor), OperationsSection, SchemasSection, SecuritySection, VersioningSection, ComplianceSection, ChangelogSection, ValidationSection, GlossarySection, UseCasesSection, InteractionsSection, ApprovalsSection, ConsumersSection, DependenciesSection, AuditSection
- **Completude:** 60% — todas as secções têm UI rica, mas dependem de `studioMock.ts` (enrichToStudioContract) para dados que o backend não fornece
- **Testável:** 🟡 Parcial — navegação funciona, conteúdo parcialmente real

### 6.4 Draft Studio (DraftStudioPage)
- **Implementação:** Nova página com 3 tabs (Spec, Metadata, Preview), save/submit mutations
- **Backend:** `contractStudioApi` com getDraft, updateContent, updateMetadata, submitForReview
- **Completude:** 90% — criado recentemente, integra com API real
- **Testável:** ✅ (criar draft → editar → submit)

### 6.5 Visual Builders
- **VisualRestBuilder:** Builder completo para REST APIs — endpoints, métodos, params, responses, auth, rate limits
- **VisualSoapBuilder:** Builder para SOAP — operations, faults, WSDL structure
- **VisualEventBuilder:** Builder para Event APIs — channels, messages, schemas, Kafka bindings
- **VisualWorkserviceBuilder:** Builder para Background Services — schedule, processing, health checks
- **Shared:** BuilderFormPrimitives (Field, FieldArea, FieldSelect, FieldCheckbox, FieldTagInput), builderSync (YAML generation), builderValidation, builderTypes
- **Completude:** 50% — UI implementada, `builderSync` gera YAML, mas sync bidirecional (YAML→builder) não evidente
- **Testável:** 🟡 Parcial — formulários funcionam, sync para source editor parcial

### 6.6 Source Editor
- **Implementação:** ContractSection com textarea para spec content (no Monaco editor yet)
- **Completude:** 40% — textarea básico, sem syntax highlighting ou validação inline

### 6.7 REST Support
- **Backend:** OpenApiSpecParser, OpenApiDiffCalculator, SwaggerSpecParser, SwaggerDiffCalculator
- **Frontend:** VisualRestBuilder completo
- **Testes:** OpenApiSpecParserTests, SwaggerSpecParserTests, OpenApiDiffCalculatorTests, SwaggerDiffCalculatorTests
- **Completude:** 70% — parsing e diff funcionais, builder visual implementado

### 6.8 SOAP Support
- **Backend:** WsdlSpecParser, WsdlDiffCalculator
- **Frontend:** VisualSoapBuilder
- **Testes:** WsdlSpecParserTests, WsdlDiffCalculatorTests
- **Completude:** 60% — parsing e diff implementados

### 6.9 Event API / AsyncAPI Support
- **Backend:** AsyncApiSpecParser, AsyncApiDiffCalculator
- **Frontend:** VisualEventBuilder com Kafka bindings
- **Testes:** AsyncApiSpecParserTests, AsyncApiDiffCalculatorTests
- **Completude:** 60%

### 6.10 Kafka Producer/Consumer
- **Backend:** ConsumerRelationship, ConsumerAsset entities; SyncConsumers feature
- **Frontend:** ConsumersSection no workspace
- **Completude:** 40% — estrutura existe, dados dependem de integração

### 6.11 Schemas / Models
- **Backend:** OpenApiSchema entity, ContractSchemaElement VO
- **Frontend:** SchemasSection no workspace
- **Completude:** 40% — UI lista schemas extraídos do spec

### 6.12 Canonical Entities
- **Backend:** CanonicalEntity, CanonicalUsageReference, CanonicalModelBuilder, CanonicalEntityState enum
- **Frontend:** CanonicalEntityCatalogPage (sem `useQuery` — provavelmente usa local state/mock)
- **Testes:** CanonicalModelBuilderTests, CanonicalModelValueObjectTests
- **Completude:** 50% — domain model rico, frontend parcial

### 6.13 Spectral Rulesets
- **Backend:** SpectralRuleset entity, SpectralBindingScope, SpectralExecutionMode, SpectralEnforcementBehavior, SpectralRulesetOrigin
- **Frontend:** SpectralRulesetManagerPage + useSpectralRulesets hook
- **Testes:** Existem nos testes do catalog
- **Completude:** 50% — entidades existem, manager page implementada

### 6.14 Linting / Validation
- **Backend:** ContractRuleEngine, EvaluateContractRules feature, ContractRuleViolation, ValidationIssue
- **Frontend:** ValidationSection no workspace, useValidation hook
- **Testes:** ContractRuleEngineTests
- **Completude:** 50% — engine funcional, integração workspace parcial

### 6.15 Compliance & Scorecards
- **Backend:** ContractScorecard, ContractScorecardCalculator, GenerateScorecard feature
- **Frontend:** ComplianceSection + ComplianceScoreCard component
- **Testes:** ContractScorecardTests, ContractScorecardCalculatorTests
- **Completude:** 50%

### 6.16 Approvals / Review Workflow
- **Backend:** ContractReview, ApproveDraft, RejectDraft, SubmitDraftForReview, ListDraftReviews
- **Frontend:** ApprovalsSection no workspace, useDraftWorkflow hook
- **Testes:** ContractStudioApplicationTests
- **Completude:** 60% — fluxo completo no backend, UI parcial

### 6.17 Versioning / Changelog
- **Backend:** SemanticVersion, ChangeEntry, GetContractHistory, SuggestSemanticVersion, ComputeSemanticDiff
- **Frontend:** VersioningSection + ChangelogSection
- **Testes:** SemanticVersionTests
- **Completude:** 50%

### 6.18 Contract Security (Signing/Locking)
- **Backend:** ContractSignature, ContractLock, SignContractVersion, LockContractVersion, VerifySignature
- **Frontend:** SecuritySection no workspace
- **Testes:** ContractSignatureTests, ContractSignatureSecurityTests
- **Completude:** 60% — backend rico com testes de segurança

### 6.19 Glossary / Use Cases / Interactions
- **Frontend:** GlossarySection, UseCasesSection, InteractionsSection no workspace
- **Backend:** Features existem no draft workflow
- **Completude:** 40% — UI estrutural, dados parcialmente mockados

### 6.20 Dependencies / Consumers / Audit
- **Frontend:** DependenciesSection, ConsumersSection, AuditSection no workspace
- **Completude:** 40% — UI presente, dados dependem de enriquecimento

### 6.21 Portal de Consumo
- **Frontend:** ContractPortalPage com API real
- **Backend:** 12+ features de portal
- **Completude:** 40% — página funcional mas funcionalidades avançadas (playground, code gen) não testáveis

---

## 7. Lacunas, Riscos e Fragilidades

### Alta Severidade

| # | Lacuna/Risco | Impacto |
|---|---|---|
| 1 | **Governance module sem persistência** — 75+ features de Application sem DbContext nem repositórios reais | Backend não operacional; 20+ páginas frontend sem integração possível |
| 2 | **Dashboard retorna 500 em múltiplos endpoints** — auth/me, catalog/graph, contracts/summary, changes/summary, incidents/summary | Primeira experiência do utilizador quebrada |
| 3 | **Studio depende de mock enrichment** — `studioMock.ts` gera dados fictícios para domain, owner, compliance | Dados exibidos no workspace não são reais |
| 4 | **Ingestion API é skeleton vazio** — sem endpoints | Sem capacidade de ingestão de dados externos |
| 5 | **422 em /identity/users e /identity/break-glass** — Unprocessable Entity | Gestão de utilizadores com problemas de validação |

### Média Severidade

| # | Lacuna/Risco | Impacto |
|---|---|---|
| 6 | **Sem Monaco editor no source editor** — usa textarea básico | Experiência de edição de specs inferior |
| 7 | **Sync bidirecional builder↔source não implementado** — `builderSync.ts` gera YAML, mas reverse parse não evidente | Builder e source podem ficar dessincronizados |
| 8 | **Frontend i18n incompleto em páginas mock** — páginas governance usam strings inline | Inconsistência com standard de i18n obrigatório |
| 9 | **AI Hub sem integração real com LLM** — chat faz fallback para mock | Funcionalidade AI não demonstrável end-to-end |
| 10 | **Canonical entities page sem API integration** — sem `useQuery` | Dados não vêm do backend |

### Baixa Severidade

| # | Lacuna/Risco | Impacto |
|---|---|---|
| 11 | Runbooks page sem API — apenas estrutura visual | Feature de runbooks não operacional |
| 12 | Product Analytics todo mock — não fornece valor real | Baixa prioridade mas ocupa espaço no menu |
| 13 | Operations Automation todo mock | Sem valor funcional atual |
| 14 | Ausência de error boundaries granulares por módulo | Erro em um módulo pode afetar shell inteiro |
| 15 | Ausência de testes E2E automatizados preenchidos | Projeto E2E existe (NexTraceOne.E2E.Tests) mas conteúdo não verificado |

---

## 8. O que Já Está Pronto para Teste Funcional

### 8.1 Identity & Access
| Funcionalidade | Testável? | Dependências | Limitações |
|---|---|---|---|
| Login local (email + password) | ✅ | Backend + seed identity | Depende de user seedado |
| Seleção de tenant | ✅ | User com múltiplos tenants | Precisa user multi-tenant no seed |
| Navegação pós-login | ✅ | Auth + Shell | — |
| Users admin (listagem) | ✅ | Backend + permissão admin | Validação 422 a investigar |
| Break Glass requests | ✅ | Backend | Validação 422 a investigar |
| JIT Access requests | ✅ | Backend | — |
| Delegations | ✅ | Backend | — |
| Sessions management | ✅ | Backend | — |
| Forgot/Reset password | 🟡 | Backend (email config) | Depende de email service |

### 8.2 Catalog & Contracts
| Funcionalidade | Testável? | Dependências | Limitações |
|---|---|---|---|
| Service catalog listagem | ✅ | Backend + seed catalog | — |
| Contract catalog listagem | ✅ | Backend + seed | Mock enrichment para dados extra |
| Create draft (wizard) | ✅ | Backend | — |
| Draft studio (edit/save) | ✅ | Backend | — |
| Contract workspace (navigate sections) | ✅ | Backend + existing versions | Dados mock-enriched |
| Contract governance page | ✅ | Backend | — |

### 8.3 Change Governance
| Funcionalidade | Testável? | Dependências | Limitações |
|---|---|---|---|
| Change catalog | ✅ | Backend + seed changegovernance | — |
| Change detail | ✅ | Backend | — |
| Releases management | ✅ | Backend | — |
| Workflow approvals | ✅ | Backend | — |
| Promotion requests | ✅ | Backend | — |

### 8.4 Operations
| Funcionalidade | Testável? | Dependências | Limitações |
|---|---|---|---|
| Incidents listagem | ✅ | Backend + seed incidents | — |
| Incident detail | ✅ | Backend | — |

### 8.5 Audit
| Funcionalidade | Testável? | Dependências | Limitações |
|---|---|---|---|
| Audit log | ✅ | Backend + seed audit | Scope limitado |

### 8.6 Shell & Dashboard
| Funcionalidade | Testável? | Dependências | Limitações |
|---|---|---|---|
| Shell navigation (sidebar, topbar) | ✅ | Auth + frontend | — |
| Command palette | ✅ | Frontend only | — |
| Dashboard KPIs | 🟡 | 4 backend APIs operacionais | 500 errors a resolver |
| Persona switching | ✅ | Frontend | — |

---

## 9. O que Ainda NÃO Deve Entrar no Plano de Teste Funcional

| Área | Motivo | O que falta | Recomendação |
|---|---|---|---|
| **Governance (20+ páginas)** | Todo mockado | DbContext, repositórios, integração API | Não testar funcionalmente; pode fazer smoke visual |
| **Product Analytics (5 páginas)** | Todo mockado | Backend real | Excluir do plano |
| **Integrations (4 páginas)** | Todo mockado | Backend real, Ingestion API | Excluir |
| **Operations — Reliability** | Mockado | Backend endpoints | Excluir |
| **Operations — Automation** | Mockado | Backend endpoints | Excluir |
| **AI Hub — Models/Policies/IDE/Routing** | Mockado | Backend endpoints para CRUD | Excluir |
| **Developer Portal (playground, codegen)** | Funcionalidade avançada não verificável | Backend real para playground | Smoke test apenas |
| **Source Editor (Monaco)** | Textarea básico | Monaco integration | Testar funcionalidade básica apenas |
| **Builder sync bidirecional** | Não implementado | Reverse YAML→builder parse | Testar builder→YAML apenas |
| **SSO/OIDC real** | Depende de provider externo | Config Entra ID/Okta/etc. | Excluir |
| **MFA real** | Depende de authenticator app | Config real | Excluir |
| **RunbooksPage** | Sem API | Backend endpoints | Excluir |

---

## 10. Plano de Teste Funcional

### 10.1 Estratégia de Teste

**Prioridade de teste:**
1. Auth flow (login → tenant → dashboard) — gate keeper de tudo
2. Shell e navegação — base de toda experiência
3. Contract lifecycle (create → edit → submit) — core do produto
4. Service catalog — inventário de serviços
5. Change governance — confiança em mudanças
6. Operations/Incidents — gestão de incidentes
7. Audit — rastreabilidade

**Smoke tests:** Auth, Shell, Dashboard, navegação entre módulos
**Regressão:** Auth flow, contract CRUD, change catalog
**Dependências backend:** PostgreSQL + seed data aplicado + migrações executadas
**Adiado:** Governance enterprise, Analytics, Integrations, AI admin, Automation, Reliability

### 10.2 Critérios de Entrada
- Backend (`NexTraceOne.ApiHost`) a correr em `localhost:5173`
- PostgreSQL acessível com todas as migrações aplicadas
- Seed data carregado (identity, catalog, changegovernance, incidents, audit, aiknowledge)
- Frontend (`vite dev`) a correr
- User de teste: admin com acesso a todos os módulos

### 10.3 Critérios de Saída
- Fluxos P0 validados sem bloqueadores
- Erros encontrados documentados
- Screenshots de evidência capturados
- Sem crash de aplicação nos fluxos testados
- Todos os fluxos P0 e P1 executados

### 10.4 Prioridades

| Prioridade | Área | Cenários |
|---|---|---|
| **P0 — Crítico** | Login, Shell, Contract CRUD | 8 cenários |
| **P1 — Alto** | Service Catalog, Changes, Incidents, Dashboard | 10 cenários |
| **P2 — Médio** | Workspace sections, Audit, User admin, DraftStudio | 8 cenários |
| **P3 — Complementar** | Portal, Source of Truth, AI Assistant, Spectral, Canonical | 6 cenários |

### 10.5 Cenários por Módulo

#### Auth (P0)
1. Login com credenciais válidas → redireciona para Dashboard
2. Login com credenciais inválidas → mensagem de erro
3. Logout → redireciona para login
4. Sessão expirada → redireciona para login

#### Shell & Navegação (P0)
5. Sidebar exibe módulos corretos para persona
6. Navegação entre módulos funciona sem erros
7. Command Palette abre e permite navegar
8. Persona switcher altera conteúdo

#### Contract Lifecycle (P0)
9. Criar draft via wizard → redireciona para DraftStudioPage
10. Editar spec content no DraftStudioPage → salvar com sucesso
11. Submit draft para review
12. Navegar para contract workspace (versão publicada)

#### Service Catalog (P1)
13. Listagem de serviços carrega dados
14. Service detail page carrega informação
15. Grafo de serviços renderiza

#### Change Governance (P1)
16. Change catalog lista alterações com filtros
17. Change detail exibe timeline e correlações
18. Releases page lista releases
19. Workflow page lista instâncias com acções

#### Operations — Incidents (P1)
20. Incidents page lista incidentes com filtros
21. Incident detail carrega timeline
22. Stats cards exibem dados corretos

#### Dashboard (P1)
23. Dashboard carrega KPIs (condicionado ao backend)
24. Quick actions funcionam

#### Contract Workspace (P2)
25. Navegar entre 16 secções do workspace
26. Summary section exibe dados do contrato
27. Contract section (source editor) funciona

#### Admin Pages (P2)
28. Users page lista utilizadores
29. Audit page lista eventos
30. Break Glass page funciona

#### DraftStudio (P2)
31. Tabs (Spec/Metadata/Preview) funcionam
32. Save metadata funciona

#### Portal & Source of Truth (P3)
33. Contract portal page carrega
34. Source of Truth explorer lista resultados
35. Global search funciona

#### AI Assistant (P3)
36. AI chat envia mensagem e recebe resposta (ou fallback mock)

#### Spectral & Canonical (P3)
37. Spectral ruleset manager page carrega
38. Canonical entity catalog page carrega

---

## 11. Casos de Teste Detalhados

### TC-001: Login Local com Sucesso
- **Módulo:** Identity & Access
- **Objetivo:** Validar que login com credenciais válidas autentica e redireciona
- **Pré-condições:** User seedado (admin@nextraceone.com), backend operacional
- **Passos:**
  1. Navegar para `/login`
  2. Inserir email válido
  3. Inserir password válida
  4. Clicar "Sign In"
- **Resultado esperado:** Redireciona para `/` (Dashboard) ou `/select-tenant` se multi-tenant
- **Prioridade:** P0
- **Dependências:** Backend + seed-identity.sql

### TC-002: Login com Credenciais Inválidas
- **Módulo:** Identity & Access
- **Objetivo:** Validar feedback de erro para credenciais incorretas
- **Pré-condições:** Backend operacional
- **Passos:**
  1. Navegar para `/login`
  2. Inserir email válido
  3. Inserir password incorreta
  4. Clicar "Sign In"
- **Resultado esperado:** Mensagem de erro exibida, não redireciona
- **Prioridade:** P0

### TC-003: Navegação no Shell
- **Módulo:** Shell
- **Objetivo:** Validar que sidebar e navegação funcionam
- **Pré-condições:** Utilizador autenticado
- **Passos:**
  1. Clicar em "Services" na sidebar
  2. Verificar que página carrega
  3. Clicar em "Contracts"
  4. Clicar em "Changes"
  5. Clicar em "Operations > Incidents"
- **Resultado esperado:** Cada página carrega sem erro de crash
- **Prioridade:** P0

### TC-004: Criar Draft de Contrato
- **Módulo:** Contracts
- **Objetivo:** Validar fluxo de criação de draft
- **Pré-condições:** Utilizador autenticado com permissão `contracts:write`
- **Passos:**
  1. Navegar para `/contracts`
  2. Clicar "New Contract"
  3. Selecionar tipo "REST API"
  4. Selecionar modo "Source Editor"
  5. Preencher nome e descrição
  6. Clicar "Create"
- **Resultado esperado:** Redireciona para `/contracts/studio/{draftId}` com DraftStudioPage
- **Prioridade:** P0

### TC-005: Editar Draft no Studio
- **Módulo:** Contracts
- **Objetivo:** Validar edição e save de draft
- **Pré-condições:** Draft criado (TC-004)
- **Passos:**
  1. No DraftStudioPage, tab "Specification"
  2. Escrever conteúdo YAML no textarea
  3. Clicar "Save"
  4. Mudar para tab "Metadata"
  5. Editar título
  6. Clicar "Save"
- **Resultado esperado:** Saves executam sem erro, dados persistem ao recarregar
- **Prioridade:** P0

### TC-006: Submit Draft para Review
- **Módulo:** Contracts
- **Objetivo:** Validar submissão para revisão
- **Pré-condições:** Draft com conteúdo (TC-005)
- **Passos:**
  1. No DraftStudioPage, clicar "Submit for Review"
- **Resultado esperado:** Status muda para "In Review", botões de edição desaparecem
- **Prioridade:** P0

### TC-007: Change Catalog — Listagem
- **Módulo:** Change Governance
- **Objetivo:** Validar que changes carregam
- **Pré-condições:** Seed data de changegovernance aplicado
- **Passos:**
  1. Navegar para `/changes`
  2. Verificar que lista carrega
  3. Aplicar filtro por tipo
- **Resultado esperado:** Lista de changes renderiza, filtros funcionam
- **Prioridade:** P1

### TC-008: Incidents — Listagem e Detalhe
- **Módulo:** Operations
- **Objetivo:** Validar listagem e detalhe de incidentes
- **Pré-condições:** Seed data de incidents aplicado
- **Passos:**
  1. Navegar para `/operations/incidents`
  2. Verificar stats cards
  3. Clicar em um incidente
  4. Verificar que detalhe carrega
- **Resultado esperado:** Lista e detalhe renderizam com dados
- **Prioridade:** P1

### TC-009: Service Catalog — Listagem
- **Módulo:** Catalog
- **Objetivo:** Validar listagem de serviços
- **Pré-condições:** Seed data de catalog aplicado
- **Passos:**
  1. Navegar para `/services`
  2. Verificar que lista carrega
  3. Clicar em um serviço
- **Resultado esperado:** Lista renderiza, detalhe abre
- **Prioridade:** P1

### TC-010: Contract Workspace — Navegação de Secções
- **Módulo:** Contracts
- **Objetivo:** Validar navegação entre as 16 secções
- **Pré-condições:** Contract version existente no seed
- **Passos:**
  1. Navegar para `/contracts/{contractVersionId}`
  2. Clicar em cada secção na sidebar (Summary, Definition, Contract, Operations, Schemas, Security, Versioning, Compliance, Changelog, Validation, Glossary, Use Cases, Interactions, Approvals, Consumers, Dependencies, Audit)
- **Resultado esperado:** Cada secção renderiza sem crash
- **Prioridade:** P2

### TC-011: Audit Page
- **Módulo:** Audit & Compliance
- **Objetivo:** Validar listagem de eventos de auditoria
- **Pré-condições:** Seed audit aplicado
- **Passos:**
  1. Navegar para `/audit`
  2. Verificar que eventos carregam
- **Resultado esperado:** Lista de audit events renderiza
- **Prioridade:** P2

### TC-012: Dashboard KPIs
- **Módulo:** Shared
- **Objetivo:** Validar que dashboard carrega métricas
- **Pré-condições:** Todos os backend endpoints operacionais
- **Passos:**
  1. Navegar para `/`
  2. Verificar stat cards (services, APIs, contracts, changes, incidents)
- **Resultado esperado:** Números exibidos (ou loading states se dados insuficientes)
- **Prioridade:** P1

---

## 12. Testes Negativos

### TN-001: Login com Email Vazio
- **Passos:** Tentar submit com email vazio
- **Resultado esperado:** Validação do formulário impede envio
- **Prioridade:** P0

### TN-002: Login com Password Vazio
- **Passos:** Preencher email, deixar password vazio, submit
- **Resultado esperado:** Validação impede envio
- **Prioridade:** P0

### TN-003: Acesso a Rota Protegida sem Auth
- **Passos:** Navegar diretamente para `/contracts` sem estar autenticado
- **Resultado esperado:** Redireciona para `/login`
- **Prioridade:** P0

### TN-004: Acesso a Rota sem Permissão
- **Passos:** Utilizador sem permissão `contracts:write` tenta aceder `/contracts/new`
- **Resultado esperado:** Redireciona para `/unauthorized`
- **Prioridade:** P1

### TN-005: Criar Draft sem Nome
- **Passos:** No wizard de criação, deixar campo nome vazio e tentar criar
- **Resultado esperado:** Validação impede criação
- **Prioridade:** P1

### TN-006: Navegar para Draft Inexistente
- **Passos:** Navegar para `/contracts/studio/00000000-0000-0000-0000-000000000000`
- **Resultado esperado:** Mensagem "Draft not found" com link para voltar
- **Prioridade:** P2

### TN-007: Rota Inválida
- **Passos:** Navegar para `/xyz-invalid`
- **Resultado esperado:** Redireciona para `/` (catch-all route)
- **Prioridade:** P2

### TN-008: Submit Draft sem Conteúdo
- **Passos:** Criar draft, tentar submit sem adicionar spec content
- **Resultado esperado:** Botão "Submit for Review" desabilitado (`!draft.specContent`)
- **Prioridade:** P2

---

## 13. Pacote Mínimo de Regressão

### Regressão Global (executar a cada merge significativo)
1. TC-001: Login com sucesso
2. TC-003: Navegação no shell (5 módulos)
3. TC-004: Criar draft de contrato
4. TN-003: Acesso protegido sem auth

### Regressão do Módulo de Contratos
1. TC-004: Criar draft
2. TC-005: Editar draft
3. TC-006: Submit para review
4. TC-010: Navegar 16 secções do workspace

### Regressão de Navegação/Login
1. TC-001: Login
2. TC-002: Login inválido
3. TC-003: Navegação shell
4. TN-001, TN-002: Validação de formulários

### Regressão Visual/Funcional Essencial
1. Dashboard carrega sem crash
2. Service catalog lista serviços
3. Contract catalog lista contratos
4. Change catalog lista changes
5. Incidents page lista incidentes

---

## 14. Massa de Teste Sugerida

### Utilizadores
| Utilizador | Papel | Cenário |
|---|---|---|
| admin@nextraceone.com | Platform Admin | Acesso total |
| engineer@nextraceone.com | Engineer | Acesso operacional |
| viewer@nextraceone.com | Read-only | Sem permissão de escrita |

### Dados de Contrato
| Item | Tipo | Cenário |
|---|---|---|
| REST API draft | OpenAPI 3.0 | Draft para edição |
| SOAP contract | WSDL | Contrato versionado |
| Event API | AsyncAPI | Kafka bindings |
| Published contract | REST | Versão publicada com spec |
| Draft without content | REST | Draft vazio para testes negativos |

### Dados de Mudança
| Item | Cenário |
|---|---|
| Release com 3 changes | Listagem e detalhe |
| Change com alta confiança | Filtro por confiança |
| Change com baixa confiança | Teste de risco |

### Dados de Incidentes
| Item | Cenário |
|---|---|
| Incident Critical + Open | Listagem, badges, filtros |
| Incident Minor + Resolved | Diferentes estados |

### Dados de Serviço
| Item | Cenário |
|---|---|
| 5+ serviços no graph | Listagem e grafo |
| Serviço com APIs | Detalhe com APIs |
| Serviço com dependências | Graph de dependências |

*Nota: Grande parte destes dados devem existir nos seed SQL (`seed-identity.sql`, `seed-catalog.sql`, `seed-changegovernance.sql`, `seed-incidents.sql`).*

---

## 15. Próximos Passos Recomendados

### Corrigir Primeiro (Bloqueadores)
1. **Resolver 500 errors nos endpoints de summary** — `/identity/auth/me`, `/catalog/graph`, `/contracts/summary`, `/changes/summary`, `/incidents/summary` — sem isto o Dashboard e a primeira experiência estão quebrados
2. **Resolver 422 em `/identity/users` e `/identity/break-glass`** — gestão de utilizadores comprometida
3. **Verificar e atualizar seed data** — garantir que todos os seeds são compatíveis com as migrations atuais

### Estabilizar Antes de Testar
4. **Garantir que auth flow end-to-end funciona** — login → token → API calls autorizadas
5. **Verificar contract lifecycle end-to-end** — create draft → edit → submit → approve → publish
6. **Testar change governance com seed data** — confirmar que as 5 páginas carregam

### O que Pode Avançar para Homologação
- Auth flow (login, tenant selection, session)
- Shell e navegação completa
- Contract catalog (listagem)
- Contract creation wizard + draft studio
- Change catalog e releases
- Incidents

### O que Ainda Precisa de Implementação
- **Governance module:** precisa de DbContext + repositórios + migração para poder integrar com frontend
- **Integrations:** precisa de Ingestion API real
- **Product Analytics:** precisa de backend real
- **Operations (Automation, Reliability):** precisa de endpoints backend
- **AI Hub admin pages:** precisa de endpoints CRUD para models, policies, etc.
- **Source editor:** migrar de textarea para Monaco
- **Builder sync bidirecional:** implementar YAML→builder parse

### Módulos a Priorizar (pela visão do produto)
1. **Identity & Access** — corrigir 422s e estabilizar → pronto para homologação
2. **Catalog & Contracts** — core do produto, já avançado, precisa de desacoplar mock enrichment
3. **Change Governance** — boa integração API, estabilizar
4. **Source of Truth** — consolidar como pilar central
5. **Operations (Incidents)** — funcional, expandir
6. **Governance** — investir em persistência para desbloquear 20+ páginas
7. **AI Hub** — evoluir integração com LLM real

---

*Fim do relatório. Gerado por análise automatizada do código real do repositório.*
