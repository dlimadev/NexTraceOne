# Estrutura do Menu e Navegação — NexTraceOne

> **Data:** 2026-03-24  
> **Tipo:** Auditoria Estrutural — Parte 4  
> **Fonte de verdade:** `src/frontend/src/components/shell/AppSidebar.tsx`

---

## Resumo

| Métrica | Valor |
|---------|-------|
| Itens de menu (navItems) | 49 |
| Secções de menu | 12 |
| Personas com ordem personalizada | 7 |
| Itens com rota funcional | 46 |
| Itens sem rota real | 3 |
| Rotas com página mas sem item no menu | ~30 |

---

## 1. Estrutura Completa do Menu

### Secção: Home

| Item | Rota | i18n Key | Ícone | Observação |
|------|------|----------|-------|------------|
| Dashboard | `/` | `sidebar.dashboard` | — | Sem label de secção; sempre primeiro |

### Secção: Services (`sidebar.sectionServices`)

| Item | Rota | i18n Key | Permissão |
|------|------|----------|-----------|
| Service Catalog | `/services` | `sidebar.serviceCatalog` | catalog:assets:read |
| Dependency Graph | `/services/graph` | `sidebar.dependencyGraph` | catalog:assets:read |

### Secção: Knowledge (`sidebar.sectionKnowledge`)

| Item | Rota | i18n Key | Permissão |
|------|------|----------|-----------|
| Source of Truth | `/source-of-truth` | `sidebar.sourceOfTruth` | catalog:assets:read |
| Developer Portal | `/portal` | `sidebar.developerPortal` | developer-portal:read |

### Secção: Contracts (`sidebar.sectionContracts`)

| Item | Rota | i18n Key | Permissão | Estado |
|------|------|----------|-----------|--------|
| Contract Catalog | `/contracts` | `sidebar.contractCatalog` | contracts:read | ✅ |
| New Contract | `/contracts/new` | `sidebar.createContract` | contracts:write | ✅ |
| Contract Studio | `/contracts/studio` | `sidebar.contractStudio` | contracts:read | ✅ |
| Contract Governance | `/contracts/governance` | `sidebar.contractGovernance` | — | ❌ **SEM ROTA** |
| Spectral Rulesets | `/contracts/spectral` | `sidebar.spectralRulesets` | — | ❌ **SEM ROTA** |
| Canonical Entities | `/contracts/canonical` | `sidebar.canonicalEntities` | — | ❌ **SEM ROTA** |

> **⚠️ 3 itens sem rota real.** As páginas existem como ficheiros mas não estão importadas no App.tsx.

### Secção: Changes (`sidebar.sectionChanges`)

| Item | Rota | i18n Key | Permissão |
|------|------|----------|-----------|
| Change Confidence | `/changes` | `sidebar.changeConfidence` | change-intelligence:read |
| Change Intelligence | `/releases` | `sidebar.changeIntelligence` | change-intelligence:releases:read |
| Workflow | `/workflow` | `sidebar.workflow` | workflow:read |
| Promotion | `/promotion` | `sidebar.promotion` | promotion:read |

### Secção: Operations (`sidebar.sectionOperations`)

| Item | Rota | i18n Key | Permissão |
|------|------|----------|-----------|
| Incidents | `/operations/incidents` | `sidebar.incidents` | operations:incidents:read |
| Runbooks | `/operations/runbooks` | `sidebar.runbooks` | operations:runbooks:read |
| Service Reliability | `/operations/reliability` | `sidebar.reliability` | operations:reliability:read |
| Automation | `/operations/automation` | `sidebar.automation` | operations:automation:read |
| Environment Comparison | `/operations/runtime-comparison` | `sidebar.environmentComparison` | operations:runtime:read |

### Secção: AI Hub (`sidebar.sectionAiHub`)

| Item | Rota | i18n Key | Permissão |
|------|------|----------|-----------|
| AI Assistant | `/ai/assistant` | `sidebar.aiAssistant` | ai:assistant:read |
| AI Agents | `/ai/agents` | `sidebar.aiAgents` | ai:assistant:read |
| Model Registry | `/ai/models` | `sidebar.modelRegistry` | ai:governance:read |
| AI Policies | `/ai/policies` | `sidebar.aiPolicies` | ai:governance:read |
| AI Routing | `/ai/routing` | `sidebar.aiRouting` | ai:governance:read |
| IDE Integrations | `/ai/ide` | `sidebar.aiIde` | ai:governance:read |
| Token Budget | `/ai/budgets` | `sidebar.aiBudgets` | ai:governance:read |
| AI Audit | `/ai/audit` | `sidebar.aiAudit` | ai:governance:read |
| AI Analysis | `/ai/analysis` | `sidebar.aiAnalysis` | ai:runtime:write |

### Secção: Governance (`sidebar.sectionGovernance`)

| Item | Rota | i18n Key | Permissão |
|------|------|----------|-----------|
| Executive Overview | `/governance/executive` | `sidebar.executiveOverview` | governance:read |
| Reports | `/governance/reports` | `sidebar.reports` | governance:read |
| Compliance | `/governance/compliance` | `sidebar.compliance` | governance:read |
| Risk Center | `/governance/risk` | `sidebar.riskCenter` | governance:read |
| FinOps | `/governance/finops` | `sidebar.finops` | governance:read |
| Policy Catalog | `/governance/policies` | `sidebar.policies` | governance:read |
| Governance Packs | `/governance/packs` | `sidebar.packs` | governance:read |

### Secção: Organization (`sidebar.sectionOrganization`)

| Item | Rota | i18n Key | Permissão |
|------|------|----------|-----------|
| Teams | `/governance/teams` | `sidebar.teams` | governance:read |
| Domains | `/governance/domains` | `sidebar.domains` | governance:read |

### Secção: Integrations (`sidebar.sectionIntegrations`)

| Item | Rota | i18n Key | Permissão |
|------|------|----------|-----------|
| Integration Hub | `/integrations` | `sidebar.integrationHub` | integrations:read |

### Secção: Analytics (`sidebar.sectionAnalytics`)

| Item | Rota | i18n Key | Permissão |
|------|------|----------|-----------|
| Product Analytics | `/analytics` | `sidebar.productAnalytics` | analytics:read |

### Secção: Administration (`sidebar.sectionAdmin`)

| Item | Rota | i18n Key | Permissão |
|------|------|----------|-----------|
| Users | `/users` | `sidebar.users` | identity:users:read |
| Break Glass | `/break-glass` | `sidebar.breakGlass` | identity:sessions:read |
| JIT Access | `/jit-access` | `sidebar.jitAccess` | identity:users:read |
| Delegations | `/delegations` | `sidebar.delegations` | identity:users:read |
| Access Review | `/access-reviews` | `sidebar.accessReview` | identity:users:read |
| My Sessions | `/my-sessions` | `sidebar.mySessions` | identity:sessions:read |
| Audit | `/audit` | `sidebar.audit` | audit:read |
| Platform Operations | `/platform/operations` | `sidebar.platformOperations` | platform:admin:read |
| Platform Configuration | `/platform/configuration` | `sidebar.platformConfiguration` | platform:admin:read |

---

## 2. Ordem de Secções por Persona

O menu é persona-aware — a ordem das secções muda conforme o perfil do utilizador.

| Persona | Ordem das Secções | Secções Destacadas |
|---------|------------------|-------------------|
| **Engineer** | home → services → operations → changes → contracts → knowledge → organization → aiHub → governance → integrations → analytics → admin | services, operations |
| **Tech Lead** | home → services → changes → operations → contracts → organization → knowledge → aiHub → governance → integrations → analytics → admin | services, changes, operations |
| **Architect** | home → contracts → services → knowledge → organization → changes → operations → governance → integrations → aiHub → analytics → admin | contracts, services, knowledge |
| **Product** | home → analytics → changes → services → operations → organization → governance → contracts → knowledge → aiHub → integrations → admin | changes, services, analytics |
| **Executive** | home → governance → analytics → organization → changes → services → operations → contracts → knowledge → aiHub → integrations → admin | governance, analytics |
| **Platform Admin** | home → admin → organization → integrations → aiHub → governance → analytics → services → contracts → knowledge → changes → operations | admin, aiHub, governance, integrations |
| **Auditor** | home → governance → organization → admin → changes → operations → aiHub → analytics → services → contracts → knowledge → integrations | governance, admin |

---

## 3. Funcionalidades Existentes Escondidas do Menu

As seguintes rotas existem e são funcionais mas **não aparecem como item direto no menu** (acessíveis via sub-navegação ou links internos):

### Governance — Sub-rotas

| Rota | Página | Como se acessa |
|------|--------|---------------|
| `/governance/executive/drilldown` | ExecutiveDrillDown | Link interno do Executive Overview |
| `/governance/executive/finops` | ExecutiveFinOps | Link interno do Executive Overview |
| `/governance/risk/heatmap` | RiskHeatmap | Link interno do Risk Center |
| `/governance/controls` | EnterpriseControls | Sub-navegação |
| `/governance/evidence` | EvidencePackages | Sub-navegação |
| `/governance/maturity` | MaturityScorecards | Sub-navegação |
| `/governance/benchmarking` | Benchmarking | Sub-navegação |
| `/governance/waivers` | Waivers | Sub-navegação |
| `/governance/delegated-admin` | DelegatedAdmin | Sub-navegação |
| `/governance/finops/service/:serviceId` | ServiceFinOps | Drill-down de FinOps |
| `/governance/finops/team/:teamId` | TeamFinOps | Drill-down de FinOps |
| `/governance/finops/domain/:domainId` | DomainFinOps | Drill-down de FinOps |

### Configuration — Sub-rotas

| Rota | Página |
|------|--------|
| `/platform/configuration/notifications` | NotificationConfiguration |
| `/platform/configuration/workflows` | WorkflowConfiguration |
| `/platform/configuration/governance` | GovernanceConfiguration |
| `/platform/configuration/catalog-contracts` | CatalogContractsConfiguration |
| `/platform/configuration/operations-finops` | OperationsFinOpsConfiguration |
| `/platform/configuration/ai-integrations` | AiIntegrationsConfiguration |
| `/platform/configuration/advanced` | AdvancedConfigurationConsole |

### Outros

| Rota | Página |
|------|--------|
| `/environments` | EnvironmentsPage |
| `/notifications` | NotificationCenter |
| `/notifications/preferences` | NotificationPreferences |
| `/analytics/adoption` | ModuleAdoption |
| `/analytics/personas` | PersonaUsage |
| `/analytics/journeys` | JourneyFunnel |
| `/analytics/value` | ValueTracking |
| `/integrations/executions` | IngestionExecutions |
| `/integrations/freshness` | IngestionFreshness |

---

## 4. Problemas Identificados

### 4.1 Itens de Menu sem Tela Real

| Item | Rota | Severidade |
|------|------|-----------|
| Contract Governance | `/contracts/governance` | **ALTA** — Utilizador clica e vai para home |
| Spectral Rulesets | `/contracts/spectral` | **ALTA** — Utilizador clica e vai para home |
| Canonical Entities | `/contracts/canonical` | **ALTA** — Utilizador clica e vai para home |

### 4.2 Agrupamento Questionável

1. **Organization** como secção separada com apenas 2 itens (Teams, Domains) — podia estar dentro de Governance
2. **Knowledge** com apenas 2 itens (Source of Truth, Developer Portal) — podia estar dentro de Services
3. **Analytics** com 1 item no menu mas 5 sub-rotas escondidas
4. **Integrations** com 1 item no menu mas 3 sub-rotas escondidas

### 4.3 Nomenclatura Inconsistente

1. `sidebar.changeIntelligence` aponta para `/releases` — o label "Change Intelligence" não corresponde à funcionalidade "Releases"
2. `sidebar.environmentComparison` usa o label "Environment Comparison" mas a rota é `/operations/runtime-comparison`
3. `sidebar.aiIde` — label técnico ("IDE Integrations") para uma secção que pode não ser entendida por personas não-técnicas

### 4.4 Itens Potencialmente Obsoletos ou Prematuros

1. **AI Hub inteiro** — 9 itens no menu para um módulo com ~20-25% de maturidade backend
2. **IDE Integrations** — sem implementação real de extensões VS Code/Visual Studio
3. **AI Analysis** — funcionalidade distinta de "AI Assistant" não claramente justificada na UX
4. **Product Analytics** — módulo completo sem evidência de dados reais ou integrações

### 4.5 Funcionalidades Escondidas que Poderiam ser Mais Visíveis

1. **Notifications** — Sem item dedicado no menu (apenas bell icon e admin)
2. **Environments** — Parte do admin mas escondida
3. **Governance sub-features** — Controls, Evidence, Maturity, Benchmarking, Waivers, DelegatedAdmin são funcionalidades completas mas sem acesso direto no menu

---

## 5. Proposta Preliminar de Reorganização do Menu

> **Nota:** Esta é uma sugestão inicial para discussão. Não deve ser implementada sem validação.

### Estrutura Proposta

```
Home
  Dashboard

Services                    (manter)
  Service Catalog
  Dependency Graph
  Source of Truth            ← mover de Knowledge
  Developer Portal           ← mover de Knowledge

Contracts                   (manter, corrigir rotas)
  Contract Catalog
  New Contract
  Contract Studio
  Contract Governance        ← CORRIGIR: adicionar rota
  Spectral Rulesets          ← CORRIGIR: adicionar rota ou remover
  Canonical Entities         ← CORRIGIR: adicionar rota ou remover

Changes                     (manter)
  Change Confidence
  Releases                   ← renomear label para "Releases" em vez de "Change Intelligence"
  Workflow & Approvals
  Promotion

Operations                  (manter)
  Incidents
  Runbooks
  Service Reliability
  Automation
  Environment Comparison

AI Hub                      (manter, mas considerar reduzir items para personas não-técnicas)
  AI Assistant
  AI Agents
  AI Analysis
  Model Registry             ← agrupar sob "AI Governance" sub-secção
  AI Policies                ← agrupar
  AI Routing                 ← agrupar
  Token Budget               ← agrupar
  AI Audit                   ← agrupar
  IDE Integrations           ← mover para admin ou dedicar secção de Developer Tools

Governance                  (manter)
  Executive Overview
  Reports
  Compliance
  Risk Center
  FinOps
  Policy Catalog
  Governance Packs
  Controls                   ← PROMOVER ao menu
  Evidence                   ← PROMOVER ao menu
  Maturity Scorecards        ← PROMOVER ao menu
  Waivers                    ← PROMOVER ao menu

Organization                (manter como sub-secção ou integrar em Governance)
  Teams
  Domains

Integrations                (expandir visibilidade)
  Integration Hub
  Ingestion Executions       ← PROMOVER ao menu
  Freshness & Health         ← PROMOVER ao menu

Analytics                   (expandir visibilidade)
  Overview
  Module Adoption            ← PROMOVER ao menu
  Persona Usage              ← PROMOVER ao menu

Notifications               ← ADICIONAR secção ou item dedicado
  Notification Center
  Preferences

Administration              (manter)
  Users
  Environments               ← PROMOVER ao menu
  Break Glass
  JIT Access
  Delegations
  Access Review
  My Sessions
  Audit Log
  Platform Operations
  Platform Configuration
```

### Princípios da Proposta

1. **Eliminar secção Knowledge** — absorver em Services (2 itens)
2. **Corrigir 3 rotas quebradas** em Contracts
3. **Promover sub-rotas importantes** ao nível do menu (Controls, Evidence, Maturity, Waivers, etc.)
4. **Adicionar Notifications** como item visível
5. **Renomear items confusos** (Change Intelligence → Releases)
6. **Manter persona-awareness** mas simplificar para personas não-técnicas
