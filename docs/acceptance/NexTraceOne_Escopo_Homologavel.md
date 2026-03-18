# NexTraceOne — Escopo Homologável (Congelado)

> **Data de congelamento:** Fase 7 — Preparação formal para teste de aceitação
> **Documento normativo:** `docs/planos/NexTraceOne_Plano_Operacional_Finalizacao.md`
> **Estado:** CONGELADO — nenhuma adição ao escopo até conclusão da Fase 8.

---

## 1. Resumo executivo

Este documento define oficialmente o escopo funcional homologável do NexTraceOne para o ciclo de teste de aceitação (Fase 8). Apenas os módulos, rotas e funcionalidades listados abaixo fazem parte do aceite. Tudo o que estiver fora desta lista está classificado como **Preview** ou **Roadmap** e não será avaliado.

---

## 2. Módulos homologáveis

### 2.1 Identity & Access (Admin)

| Rota | Página | Permissão |
|------|--------|-----------|
| `/login` | LoginPage | pública |
| `/forgot-password` | ForgotPasswordPage | pública |
| `/reset-password` | ResetPasswordPage | pública |
| `/activate` | ActivationPage | pública |
| `/mfa` | MfaPage | pública |
| `/invitation` | InvitationPage | pública |
| `/select-tenant` | TenantSelectionPage | pública |
| `/users` | UsersPage | `identity:users:read` |
| `/break-glass` | BreakGlassPage | `identity:sessions:read` |
| `/jit-access` | JitAccessPage | `identity:users:read` |
| `/delegations` | DelegationPage | `identity:users:read` |
| `/access-reviews` | AccessReviewPage | `identity:users:read` |
| `/my-sessions` | MySessionsPage | `identity:sessions:read` |
| `/unauthorized` | UnauthorizedPage | pública |

### 2.2 Dashboard & Shell

| Rota | Página | Permissão |
|------|--------|-----------|
| `/` | DashboardPage | autenticado |

Inclui: Sidebar, Topbar, Command Palette, breadcrumbs, transições de página.

### 2.3 Service Catalog & Source of Truth

| Rota | Página | Permissão |
|------|--------|-----------|
| `/services` | ServiceCatalogListPage | `catalog:assets:read` |
| `/services/graph` | ServiceCatalogPage (Grafo) | `catalog:assets:read` |
| `/services/:serviceId` | ServiceDetailPage | `catalog:assets:read` |
| `/source-of-truth` | SourceOfTruthExplorerPage | `catalog:assets:read` |
| `/source-of-truth/services/:serviceId` | ServiceSourceOfTruthPage | `catalog:assets:read` |
| `/source-of-truth/contracts/:contractVersionId` | ContractSourceOfTruthPage | `catalog:assets:read` |
| `/search` | GlobalSearchPage | `catalog:assets:read` |

### 2.4 Contracts

| Rota | Página | Permissão |
|------|--------|-----------|
| `/contracts` | ContractCatalogPage | `contracts:read` |
| `/contracts/new` | CreateServicePage | `contracts:write` |
| `/contracts/studio/:draftId` | DraftStudioPage | `contracts:write` |
| `/contracts/governance` | ContractGovernancePage | `contracts:read` |
| `/contracts/:contractVersionId/portal` | ContractPortalPage | `contracts:read` |
| `/contracts/:contractVersionId` | ContractWorkspacePage | `contracts:read` |

### 2.5 Change Governance

| Rota | Página | Permissão |
|------|--------|-----------|
| `/changes` | ChangeCatalogPage | `change-intelligence:read` |
| `/changes/:changeId` | ChangeDetailPage | `change-intelligence:read` |
| `/releases` | ReleasesPage | `change-intelligence:releases:read` |
| `/workflow` | WorkflowPage | `workflow:read` |
| `/promotion` | PromotionPage | `promotion:read` |

### 2.6 Operations

| Rota | Página | Permissão |
|------|--------|-----------|
| `/operations/incidents` | IncidentsPage | `operations:incidents:read` |
| `/operations/incidents/:incidentId` | IncidentDetailPage | `operations:incidents:read` |
| `/operations/runbooks` | RunbooksPage | `operations:incidents:read` |

### 2.7 AI Hub (parcial)

| Rota | Página | Permissão |
|------|--------|-----------|
| `/ai/assistant` | AiAssistantPage | autenticado |

### 2.8 Audit

| Rota | Página | Permissão |
|------|--------|-----------|
| `/audit` | AuditPage | `audit:read` |

### 2.9 Platform Operations

| Rota | Página | Permissão |
|------|--------|-----------|
| `/platform/operations` | PlatformOperationsPage | `platform:admin:read` |

---

## 3. Módulos excluídos do aceite (Preview / Roadmap)

Todos os módulos abaixo estão marcados com `preview: true` na sidebar e envolvidos por `<PreviewGate>` nas rotas. Mostram banner de Preview na UI e não fazem parte do ciclo de aceitação.

### 3.1 Governance Enterprise

| Rota | Motivo de exclusão |
|------|--------------------|
| `/governance/executive` | Mockado / sem backend real |
| `/governance/executive/heatmap` | Mockado |
| `/governance/executive/maturity` | Mockado |
| `/governance/executive/benchmarking` | Mockado |
| `/governance/executive/drilldown/:entityType/:entityId` | Mockado |
| `/governance/reports` | Mockado |
| `/governance/risk` | Mockado |
| `/governance/compliance` | Mockado |
| `/governance/policies` | Mockado |
| `/governance/evidence` | Mockado |
| `/governance/controls` | Mockado |
| `/governance/finops` | Mockado |
| `/governance/finops/services/:serviceId` | Mockado |
| `/governance/finops/teams/:teamId` | Mockado |
| `/governance/finops/domains/:domainId` | Mockado |
| `/governance/finops/executive` | Mockado |
| `/governance/packs` | Mockado |
| `/governance/packs/:packId` | Mockado |
| `/governance/packs/:packId/simulate` | Mockado |
| `/governance/waivers` | Mockado |

### 3.2 Organization Governance

| Rota | Motivo de exclusão |
|------|--------------------|
| `/governance/teams` | Mockado |
| `/governance/teams/:teamId` | Mockado |
| `/governance/domains` | Mockado |
| `/governance/domains/:domainId` | Mockado |
| `/governance/delegated-admin` | Mockado |

### 3.3 Product Analytics

| Rota | Motivo de exclusão |
|------|--------------------|
| `/analytics` | Módulo incompleto |
| `/analytics/adoption` | Módulo incompleto |
| `/analytics/personas` | Módulo incompleto |
| `/analytics/journeys` | Módulo incompleto |
| `/analytics/value` | Módulo incompleto |

### 3.4 Integrations

| Rota | Motivo de exclusão |
|------|--------------------|
| `/integrations` | Módulo incompleto |
| `/integrations/connectors/:connectorId` | Módulo incompleto |
| `/integrations/executions` | Módulo incompleto |
| `/integrations/freshness` | Módulo incompleto |

### 3.5 Operations (avançado)

| Rota | Motivo de exclusão |
|------|--------------------|
| `/operations/reliability` | Mockado |
| `/operations/reliability/:serviceId` | Mockado |
| `/operations/automation` | Mockado |
| `/operations/automation/admin` | Mockado |
| `/operations/automation/:workflowId` | Mockado |

### 3.6 AI Hub (avançado)

| Rota | Motivo de exclusão |
|------|--------------------|
| `/ai/models` | Mockado |
| `/ai/policies` | Mockado |
| `/ai/ide` | Mockado |
| `/ai/routing` | Mockado |

### 3.7 Contracts (avançado)

| Rota | Motivo de exclusão |
|------|--------------------|
| `/contracts/spectral` | Mockado |
| `/contracts/canonical` | Mockado |

### 3.8 Developer Portal

| Rota | Motivo de exclusão |
|------|--------------------|
| `/portal` | Mockado |

---

## 4. Infraestrutura do escopo

### 4.1 Backend

- **Runtime:** .NET 10, Modular Monolith
- **Base de dados:** PostgreSQL multi-database (Identity, Catalog, Contracts, ChangeIntelligence, Audit, Incidents, AiGovernance + outros)
- **Migrations:** 11 InitialCreate migrations cobrindo todos os módulos
- **Seed:** 6 ficheiros SQL idempotentes (`ON CONFLICT DO NOTHING`) executados automaticamente em Development
- **Módulos registados:** 16+ módulos no `Program.cs`
- **Health checks:** `/health`, `/ready`, `/live`

### 4.2 Frontend

- **Runtime:** React 18 + Vite + TypeScript
- **Internacionalização:** react-i18next (en, pt-BR, pt-PT)
- **Data fetching:** @tanstack/react-query
- **Lazy loading:** todas as páginas protegidas
- **Estado compartilhado:** PageLoadingState, PageErrorState, PreviewBanner, PreviewGate

### 4.3 Utilizadores de teste

| Email | Papel | Senha |
|-------|-------|-------|
| admin@nextraceone.dev | PlatformAdmin | Admin@123 |
| techlead@nextraceone.dev | TechLead | Admin@123 |
| dev@nextraceone.dev | Developer | Admin@123 |
| auditor@nextraceone.dev | Auditor | Admin@123 |

### 4.4 Tenants de teste

| Nome | Slug |
|------|------|
| NexTrace Corp | nexttrace-corp |
| Acme Fintech | acme-fintech |

---

## 5. Declaração de congelamento

O escopo acima está **congelado**. Nenhuma funcionalidade será adicionada até a conclusão da Fase 8 (Execução do teste de aceitação). Qualquer alteração no código durante este período deve ser exclusivamente para correção de bugs encontrados durante o aceite.

Módulos marcados como Preview permanecem acessíveis na UI com indicação visual clara de que não fazem parte do escopo homologável.

---

## 6. Estado pós-aceite (atualizado Fase 9)

> **Adicionado após conclusão da Fase 9 — Correções pós-aceite e baseline estável.**

| Marco | Estado |
|-------|--------|
| Fase 8 — Teste de aceitação | ✅ Executado — APROVADO COM RESSALVAS |
| Bugs P0 encontrados | 0 |
| Bugs P1 encontrados | 0 |
| Observações P2 | 4 (backlog Fase 10) |
| Fase 9 — Regressão mínima | ✅ Backend + Frontend compilam sem erros |
| Fase 9 — Baseline congelada | ✅ `docs/acceptance/NexTraceOne_Baseline_Estavel.md` |

**Estado atual do escopo: BASELINE ESTÁVEL — pronto para Fase 10 (Evolução do produto).**
