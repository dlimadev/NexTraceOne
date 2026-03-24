# NexTraceOne — Estrutura do Menu e Navegação

**Data:** 2026-03-24
**Fonte:** `src/frontend/src/components/shell/AppSidebar.tsx`, `src/frontend/src/App.tsx`
**Nota:** Mapeamento baseado no código real. Não presume funcionamento — verifica evidências.

---

## 1. Visão Geral da Navegação

O sistema de navegação do NexTraceOne é composto por:

- **Sidebar lateral** (`AppSidebar.tsx`) — navegação principal, collapsable
- **Topbar** (`AppTopbar.tsx`) — busca global, notificações, user menu
- **Workspace Switcher** (`WorkspaceSwitcher.tsx`) — troca de tenant/workspace
- **Context Strip** (`ContextStrip.tsx`) — strip de contexto de ambiente ativo
- **Environment Banner** (`EnvironmentBanner.tsx`) — banner de ambiente não-produção
- **Mobile Drawer** (`MobileDrawer.tsx`) — sidebar em modo mobile

A navegação é **controlada por permissões** via `usePermissions()` — itens invisíveis se o utilizador não tiver a permissão correspondente.

A navegação é também **controlada por persona** via `usePersona()` — `config.sectionOrder` define a ordem das secções e `config.highlightedSections` define destaques visuais por persona.

---

## 2. Mapa Completo do Sidebar

### Configuração Técnica

```typescript
// AppSidebar.tsx
const navItems: NavItem[] = [
  { labelKey, to, icon, permission?, section, preview? }
]
```

Cada item tem:
- **`labelKey`** — chave i18n para tradução do label
- **`to`** — rota de destino
- **`icon`** — ícone Lucide React
- **`permission`** — permissão necessária (opcional — sem permissão = visível a todos autenticados)
- **`section`** — secção de agrupamento
- **`preview`** — flag de preview (badge visual)

---

## 3. Estrutura Completa por Secção

### SECÇÃO: home (sem label de secção)

| Label Key | Rota | Ícone | Permissão | Status Rota |
|-----------|------|-------|-----------|-------------|
| `sidebar.dashboard` | `/` | `LayoutDashboard` | Nenhuma | ✅ |

---

### SECÇÃO: services (`sidebar.sectionServices`)

| Label Key | Label EN esperado | Rota | Ícone | Permissão | Status Rota |
|-----------|-------------------|------|-------|-----------|-------------|
| `sidebar.serviceCatalog` | Service Catalog | `/services` | `Server` | `catalog:assets:read` | ✅ |
| `sidebar.dependencyGraph` | Dependency Graph | `/services/graph` | `Share2` | `catalog:assets:read` | ✅ |

---

### SECÇÃO: knowledge (`sidebar.sectionKnowledge`)

| Label Key | Label EN esperado | Rota | Ícone | Permissão | Status Rota |
|-----------|-------------------|------|-------|-----------|-------------|
| `sidebar.sourceOfTruth` | Source of Truth | `/source-of-truth` | `Globe` | `catalog:assets:read` | ✅ |
| `sidebar.developerPortal` | Developer Portal | `/portal` | `BookOpen` | `developer-portal:read` | ✅ |

---

### SECÇÃO: contracts (`sidebar.sectionContracts`)

| Label Key | Label EN esperado | Rota | Ícone | Permissão | Status Rota |
|-----------|-------------------|------|-------|-----------|-------------|
| `sidebar.contractCatalog` | Contract Catalog | `/contracts` | `FileText` | `contracts:read` | ✅ |
| `sidebar.createContract` | Create Contract | `/contracts/new` | `Plus` | `contracts:write` | ✅ |
| `sidebar.contractStudio` | Contract Studio | `/contracts/studio` | `Layers` | `contracts:read` | ⚠️ **REDIRECT** → `/contracts` |
| `sidebar.contractGovernance` | Contract Governance | `/contracts/governance` | `Shield` | `contracts:read` | ❌ **SEM ROTA** |
| `sidebar.spectralRulesets` | Spectral Rulesets | `/contracts/spectral` | `ShieldCheck` | `contracts:write` | ❌ **SEM ROTA** |
| `sidebar.canonicalEntities` | Canonical Entities | `/contracts/canonical` | `Database` | `contracts:read` | ❌ **SEM ROTA** |

**Problemas identificados:**
- 3 dos 6 itens desta secção estão quebrados
- `sidebar.contractStudio` abre o catálogo de contratos, não o studio
- Contract Governance, Spectral Rulesets e Canonical Entities têm páginas no código mas sem rota registada

---

### SECÇÃO: changes (`sidebar.sectionChanges`)

| Label Key | Label EN esperado | Rota | Ícone | Permissão | Status Rota |
|-----------|-------------------|------|-------|-----------|-------------|
| `sidebar.changeConfidence` | Change Confidence | `/changes` | `ShieldCheck` | `change-intelligence:read` | ✅ |
| `sidebar.changeIntelligence` | Change Intelligence | `/releases` | `Zap` | `change-intelligence:releases:read` | ✅ |
| `sidebar.workflow` | Workflow | `/workflow` | `CheckSquare` | `workflow:read` | ✅ |
| `sidebar.promotion` | Promotion | `/promotion` | `ArrowUpCircle` | `promotion:read` | ✅ |

**Nota:** `sidebar.changeConfidence` aponta para `/changes` (ChangeCatalogPage) mas o label sugere "Change Confidence" — pode haver confusão sobre o que esta página representa.

---

### SECÇÃO: operations (`sidebar.sectionOperations`)

| Label Key | Label EN esperado | Rota | Ícone | Permissão | Status Rota |
|-----------|-------------------|------|-------|-----------|-------------|
| `sidebar.incidents` | Incidents | `/operations/incidents` | `AlertTriangle` | `operations:incidents:read` | ✅ |
| `sidebar.runbooks` | Runbooks | `/operations/runbooks` | `FileCode` | `operations:runbooks:read` | ✅* |
| `sidebar.reliability` | Reliability | `/operations/reliability` | `Activity` | `operations:reliability:read` | ✅ |
| `sidebar.automation` | Automation | `/operations/automation` | `Zap` | `operations:automation:read` | ✅ |
| `sidebar.environmentComparison` | Environment Comparison | `/operations/runtime-comparison` | `BarChart3` | `operations:runtime:read` | ✅ |

**⚠️ Discrepância de permissão:** No sidebar, `runbooks` usa `operations:runbooks:read`, mas em App.tsx a rota `/operations/runbooks` usa `operations:incidents:read`. Uma das fontes está incorreta.

---

### SECÇÃO: aiHub (`sidebar.sectionAiHub`)

| Label Key | Label EN esperado | Rota | Ícone | Permissão | Status Rota |
|-----------|-------------------|------|-------|-----------|-------------|
| `sidebar.aiAssistant` | AI Assistant | `/ai/assistant` | `Bot` | `ai:assistant:read` | ✅ |
| `sidebar.aiAgents` | AI Agents | `/ai/agents` | `Bot` | `ai:assistant:read` | ✅ |
| `sidebar.modelRegistry` | Model Registry | `/ai/models` | `Database` | `ai:governance:read` | ✅ |
| `sidebar.aiPolicies` | AI Policies | `/ai/policies` | `Shield` | `ai:governance:read` | ✅ |
| `sidebar.aiRouting` | AI Routing | `/ai/routing` | `Share2` | `ai:governance:read` | ✅ |
| `sidebar.aiIde` | AI IDE | `/ai/ide` | `Monitor` | `ai:governance:read` | ✅ |
| `sidebar.aiBudgets` | AI Budgets | `/ai/budgets` | `BarChart3` | `ai:governance:read` | ✅ |
| `sidebar.aiAudit` | AI Audit | `/ai/audit` | `ClipboardList` | `ai:governance:read` | ✅ |
| `sidebar.aiAnalysis` | AI Analysis | `/ai/analysis` | `BarChart3` | `ai:runtime:write` | ✅ |

**Problemas identificados:**
- `Bot` usado tanto para `aiAssistant` como para `aiAgents` — sem diferenciação visual
- `BarChart3` usado tanto para `aiBudgets` como para `aiAnalysis` — duplicação de ícone
- `Shield` já usado em `contractGovernance` (outra secção)
- `aiAnalysis` usa permissão `ai:runtime:write` — diferente dos outros itens de AI que usam `read`

---

### SECÇÃO: governance (`sidebar.sectionGovernance`)

| Label Key | Label EN esperado | Rota | Ícone | Permissão | Status Rota |
|-----------|-------------------|------|-------|-----------|-------------|
| `sidebar.executiveOverview` | Executive Overview | `/governance/executive` | `Briefcase` | `governance:read` | ✅ |
| `sidebar.reports` | Reports | `/governance/reports` | `BarChart3` | `governance:read` | ✅ |
| `sidebar.compliance` | Compliance | `/governance/compliance` | `ClipboardCheck` | `governance:read` | ✅ |
| `sidebar.riskCenter` | Risk Center | `/governance/risk` | `AlertTriangle` | `governance:read` | ✅ |
| `sidebar.finops` | FinOps | `/governance/finops` | `TrendingUp` | `governance:read` | ✅ |
| `sidebar.policies` | Policies | `/governance/policies` | `Shield` | `governance:read` | ✅ |
| `sidebar.packs` | Packs | `/governance/packs` | `Layers` | `governance:read` | ✅ |

**Nota:** Governance tem mais 10 páginas funcionais que NÃO aparecem no menu (waivers, controls, evidence, maturity, benchmarking, delegated-admin, drilldown, heatmap, etc.) — acessíveis apenas por navegação interna nas páginas pai.

---

### SECÇÃO: organization (`sidebar.sectionOrganization`)

| Label Key | Label EN esperado | Rota | Ícone | Permissão | Status Rota |
|-----------|-------------------|------|-------|-----------|-------------|
| `sidebar.teams` | Teams | `/governance/teams` | `Users` | `governance:read` | ✅ |
| `sidebar.domains` | Domains | `/governance/domains` | `Globe` | `governance:read` | ✅ |

**Nota:** `Globe` também é usado em `sidebar.sourceOfTruth` — duplicação de ícone entre secções.

---

### SECÇÃO: integrations (`sidebar.sectionIntegrations`)

| Label Key | Label EN esperado | Rota | Ícone | Permissão | Status Rota |
|-----------|-------------------|------|-------|-----------|-------------|
| `sidebar.integrationHub` | Integration Hub | `/integrations` | `Cable` | `integrations:read` | ✅ |

---

### SECÇÃO: analytics (`sidebar.sectionAnalytics`)

| Label Key | Label EN esperado | Rota | Ícone | Permissão | Status Rota |
|-----------|-------------------|------|-------|-----------|-------------|
| `sidebar.productAnalytics` | Product Analytics | `/analytics` | `BarChart3` | `analytics:read` | ✅ |

---

### SECÇÃO: admin (`sidebar.sectionAdmin`)

| Label Key | Label EN esperado | Rota | Ícone | Permissão | Status Rota |
|-----------|-------------------|------|-------|-----------|-------------|
| `sidebar.users` | Users | `/users` | `Users` | `identity:users:read` | ✅ |
| `sidebar.breakGlass` | Break Glass | `/break-glass` | `AlertTriangle` | `identity:sessions:read` | ✅ |
| `sidebar.jitAccess` | JIT Access | `/jit-access` | `Clock` | `identity:users:read` | ✅ |
| `sidebar.delegations` | Delegations | `/delegations` | `UserCheck` | `identity:users:read` | ✅ |
| `sidebar.accessReview` | Access Review | `/access-reviews` | `ClipboardCheck` | `identity:users:read` | ✅ |
| `sidebar.mySessions` | My Sessions | `/my-sessions` | `Monitor` | `identity:sessions:read` | ✅ |
| `sidebar.audit` | Audit | `/audit` | `ClipboardList` | `audit:read` | ✅ |
| `sidebar.platformOperations` | Platform Operations | `/platform/operations` | `Server` | `platform:admin:read` | ✅ |
| `sidebar.platformConfiguration` | Platform Configuration | `/platform/configuration` | `Settings` | `platform:admin:read` | ✅ |

**Nota:** `AlertTriangle` usado tanto em `breakGlass` como em `riskCenter` (outra secção).

---

## 4. Inconsistências Identificadas

### 4.1 Rotas Quebradas no Menu

| Item | Rota | Problema |
|------|------|---------|
| Contract Studio | `/contracts/studio` | Redirect para `/contracts` — utilizador vai para o catálogo em vez do studio |
| Contract Governance | `/contracts/governance` | Página existe no código mas sem rota — 404 ou redirect para `/` |
| Spectral Rulesets | `/contracts/spectral` | Idem |
| Canonical Entities | `/contracts/canonical` | Idem |

### 4.2 Duplicação de Ícones

| Ícone | Usado em |
|-------|---------|
| `Bot` | `aiAssistant`, `aiAgents` — idênticos |
| `BarChart3` | `environmentComparison`, `aiBudgets`, `aiAnalysis`, `reports`, `productAnalytics` — 5 usos |
| `Shield` | `contractGovernance`, `aiPolicies`, `policies` — 3 usos |
| `AlertTriangle` | `incidents`, `breakGlass`, `riskCenter` — 3 usos |
| `Globe` | `sourceOfTruth`, `domains` — 2 usos |
| `Users` | `teams`, `users` — 2 usos |
| `Monitor` | `aiIde`, `mySessions` — 2 usos |
| `Zap` | `changeIntelligence`, `automation` — 2 usos |
| `ShieldCheck` | `changeConfidence`, `spectralRulesets` — 2 usos |
| `Database` | `canonicalEntities`, `modelRegistry` — 2 usos |
| `Layers` | `contractStudio`, `packs` — 2 usos |
| `ClipboardList` | `aiAudit`, `audit` — 2 usos |
| `ClipboardCheck` | `accessReview`, `compliance` — 2 usos |

### 4.3 Discrepância de Permissões

| Contexto | Sidebar | App.tsx | Problema |
|---------|---------|---------|---------|
| Runbooks | `operations:runbooks:read` | `operations:incidents:read` | Permissão inconsistente |

### 4.4 Itens Ausentes no Menu (Rotas Existentes)

| Funcionalidade | Rota | Observação |
|----------------|------|------------|
| Environments | `/environments` | Gestão de ambientes invisível no menu |
| Notifications Center | `/notifications` | Provavelmente via topbar |
| Notification Preferences | `/notifications/preferences` | Sub-página |
| Governance Waivers | `/governance/waivers` | Funcionalidade escondida |
| Enterprise Controls | `/governance/controls` | Funcionalidade escondida |
| Evidence Packages | `/governance/evidence` | Funcionalidade escondida |
| Maturity Scorecards | `/governance/maturity` | Funcionalidade escondida |
| Benchmarking | `/governance/benchmarking` | Funcionalidade escondida |
| Delegated Admin | `/governance/delegated-admin` | Funcionalidade escondida |
| Risk Heatmap | `/governance/risk/heatmap` | Sub-página de Risk Center |
| Module Adoption | `/analytics/adoption` | Sub-página de Analytics |
| Persona Usage | `/analytics/personas` | Sub-página de Analytics |
| Journey Funnel | `/analytics/journeys` | Sub-página de Analytics |
| Value Tracking | `/analytics/value` | Sub-página de Analytics |

### 4.5 Nomenclatura Inconsistente

| Problema | Exemplo |
|---------|---------|
| Label vs conteúdo da página | "Change Confidence" aponta para `/changes` (ChangeCatalogPage) |
| "Change Intelligence" aponta para `/releases` (ReleasesPage) — semanticamente confuso |
| Secção "knowledge" contém Source of Truth e Developer Portal — agrupamento questionável |
| "contractStudio" no menu aponta para redirect — o studio real é `/contracts/studio/:draftId` |

### 4.6 Agrupamento Questionável

| Problema | Observação |
|---------|------------|
| `Teams` e `Domains` estão em `organization` mas Routes/Governance pages | Poderiam estar em `governance` |
| `Integration Hub` está sozinho numa secção — poderia fazer parte de `admin` ou `operations` |
| `Product Analytics` está sozinho numa secção — possivelmente só visível a admin/product personas |
| `Environments` não tem secção no menu — gestão crítica invisível |

---

## 5. Estrutura Atual do Menu — Diagrama

```
Sidebar
├── [home]
│   └── Dashboard (/)
│
├── Services
│   ├── Service Catalog (/services) ✅
│   └── Dependency Graph (/services/graph) ✅
│
├── Knowledge
│   ├── Source of Truth (/source-of-truth) ✅
│   └── Developer Portal (/portal) ✅
│
├── Contracts
│   ├── Contract Catalog (/contracts) ✅
│   ├── Create Contract (/contracts/new) ✅
│   ├── Contract Studio (/contracts/studio) ⚠️ REDIRECT
│   ├── Contract Governance (/contracts/governance) ❌ SEM ROTA
│   ├── Spectral Rulesets (/contracts/spectral) ❌ SEM ROTA
│   └── Canonical Entities (/contracts/canonical) ❌ SEM ROTA
│
├── Changes
│   ├── Change Confidence (/changes) ✅
│   ├── Change Intelligence (/releases) ✅
│   ├── Workflow (/workflow) ✅
│   └── Promotion (/promotion) ✅
│
├── Operations
│   ├── Incidents (/operations/incidents) ✅
│   ├── Runbooks (/operations/runbooks) ✅*
│   ├── Reliability (/operations/reliability) ✅
│   ├── Automation (/operations/automation) ✅
│   └── Environment Comparison (/operations/runtime-comparison) ✅
│
├── AI Hub
│   ├── AI Assistant (/ai/assistant) ✅
│   ├── AI Agents (/ai/agents) ✅
│   ├── Model Registry (/ai/models) ✅
│   ├── AI Policies (/ai/policies) ✅
│   ├── AI Routing (/ai/routing) ✅
│   ├── AI IDE (/ai/ide) ✅
│   ├── AI Budgets (/ai/budgets) ✅
│   ├── AI Audit (/ai/audit) ✅
│   └── AI Analysis (/ai/analysis) ✅
│
├── Governance
│   ├── Executive Overview (/governance/executive) ✅
│   ├── Reports (/governance/reports) ✅
│   ├── Compliance (/governance/compliance) ✅
│   ├── Risk Center (/governance/risk) ✅
│   ├── FinOps (/governance/finops) ✅
│   ├── Policies (/governance/policies) ✅
│   └── Packs (/governance/packs) ✅
│
├── Organization
│   ├── Teams (/governance/teams) ✅
│   └── Domains (/governance/domains) ✅
│
├── Analytics
│   └── Product Analytics (/analytics) ✅
│
├── Integrations
│   └── Integration Hub (/integrations) ✅
│
└── Admin
    ├── Users (/users) ✅
    ├── Break Glass (/break-glass) ✅
    ├── JIT Access (/jit-access) ✅
    ├── Delegations (/delegations) ✅
    ├── Access Review (/access-reviews) ✅
    ├── My Sessions (/my-sessions) ✅
    ├── Audit (/audit) ✅
    ├── Platform Operations (/platform/operations) ✅
    └── Platform Configuration (/platform/configuration) ✅
```

---

## 6. Proposta Preliminar de Reorganização do Menu

**Nota:** Esta é uma sugestão preliminar para orientar a próxima fase. Não implementar sem revisão detalhada.

### Problemas a resolver primeiro (imediatos)

1. **Registar as 3 rotas ausentes** em App.tsx: `/contracts/governance`, `/contracts/spectral`, `/contracts/canonical`
2. **Corrigir `sidebar.contractStudio`**: ou remover o redirect e criar rota direta para DraftStudioPage sem parâmetro, ou substituir o menu item por "Open Studio" com comportamento diferente
3. **Corrigir permissão de Runbooks**: alinhar App.tsx com a permissão `operations:runbooks:read` do sidebar
4. **Adicionar Environments ao menu**: seria natural em `Admin` ou numa nova secção `Platform`

### Sugestão de reorganização de secções

```
Proposta — Revisão de Agrupamento

[Home]
  Dashboard

[Discovery] (ex-Services + Knowledge)
  Service Catalog
  Dependency Graph
  Source of Truth
  Developer Portal
  Global Search

[Contracts]
  Contract Catalog
  Create Contract
  Contract Governance  ← Fix rota
  Spectral Rulesets    ← Fix rota
  Canonical Entities   ← Fix rota

[Changes]
  Changes
  Releases
  Workflows
  Promotion

[Operations]
  Incidents
  Runbooks
  Reliability
  Automation
  Environment Comparison
  Platform Operations ← mover de Admin

[AI Hub]
  AI Assistant
  AI Agents
  Model Registry
  AI Policies
  AI Routing
  AI IDE
  AI Budgets
  AI Audit
  AI Analysis

[Governance & Compliance]
  Executive Overview
  Reports
  Compliance
  Risk Center
  FinOps
  Policies
  Packs
  Waivers          ← tornar visível
  Enterprise Controls ← tornar visível
  Evidence Packages   ← tornar visível
  Maturity Scorecards ← tornar visível
  Benchmarking        ← tornar visível

[Organization]
  Teams
  Domains
  Delegated Admin   ← tornar visível

[Integrations]
  Integration Hub
  Ingestion Executions ← sub-item
  Ingestion Freshness  ← sub-item

[Analytics]
  Product Analytics
  Module Adoption   ← sub-item
  Persona Usage     ← sub-item

[Admin]
  Users
  Environments      ← adicionar
  Break Glass
  JIT Access
  Delegations
  Access Review
  My Sessions
  Audit
  Platform Configuration
```

---

## 7. Resumo de Problemas por Severidade

| Severidade | Problema | Qtd |
|-----------|---------|-----|
| **Alta** | Itens de menu sem rota funcional | 3 |
| **Alta** | Item de menu que redireciona incorretamente | 1 |
| **Média** | Páginas funcionais sem item no menu | 14 |
| **Média** | Duplicação de ícones no sidebar | 13 casos |
| **Média** | Discrepância de permissão Runbooks | 1 |
| **Baixa** | Labels que não descrevem bem o conteúdo | 2 |
| **Baixa** | Agrupamentos questionáveis entre secções | 3 |
