# Revisão Modular — Governance

> **Data:** 2026-03-24  
> **Prioridade:** P3 (Módulo Mais Amplo)  
> **Módulo Backend:** `src/modules/governance/`  
> **Módulo Frontend:** `src/frontend/src/features/governance/`  
> **Fonte de verdade:** Código do repositório

---

## 1. Propósito do Módulo

O módulo **Governance** é o mais amplo do NexTraceOne, servindo como "catch-all" para funcionalidades transversais. Cobre:

- **Executive** — Overview executivo, drill-down, FinOps executivo
- **Compliance** — Packs de compliance, checks, certificações
- **Risk** — Centro de risco, heatmaps
- **FinOps** — Custos por serviço, equipa, domínio
- **Policies** — Catálogo de políticas, enterprise controls
- **Governance Packs** — Packs de governança, scorecards de maturidade
- **Teams & Domains** — Gestão organizacional
- **Integrations** — Hub de integrações, conectores, ingestão
- **Product Analytics** — Adoção, personas, jornadas, valor
- **Platform** — Status, jobs, queues
- **Evidence** — Pacotes de evidência
- **Waivers** — Exceções de governança
- **Delegated Admin** — Administração delegada
- **Onboarding** — Fluxo de onboarding
- **Benchmarking** — Comparação de maturidade

---

## 2. Aderência ao Produto

| Aspecto | Avaliação | Observação |
|---------|-----------|------------|
| Alinhamento com a visão | ✅ Forte | Governança é pilar oficial do produto |
| Completude | ✅ Alta | 19 endpoint modules, 25 páginas frontend |
| **Problema principal** | ⚠️ **Módulo excessivamente amplo** | Contém responsabilidades de Integrations, Analytics, Platform, Onboarding que deveriam ser bounded contexts separados |

---

## 3. Páginas Frontend (25 páginas)

### 3.1 Executive & Reports

| Página | Rota | Estado |
|--------|------|--------|
| ExecutiveOverviewPage | `/governance/executive` | ✅ Funcional |
| ExecutiveDrillDownPage | `/governance/executive/drilldown` | ✅ Funcional |
| ExecutiveFinOpsPage | `/governance/executive/finops` | ✅ Funcional |
| ReportsPage | `/governance/reports` | ✅ Funcional |

### 3.2 Compliance, Risk, FinOps

| Página | Rota | Estado |
|--------|------|--------|
| CompliancePage | `/governance/compliance` | ✅ Funcional |
| RiskCenterPage | `/governance/risk` | ✅ Funcional |
| RiskHeatmapPage | `/governance/risk/heatmap` | ✅ Funcional |
| FinOpsPage | `/governance/finops` | ✅ Funcional |
| ServiceFinOpsPage | `/governance/finops/service/:id` | ✅ Funcional |
| TeamFinOpsPage | `/governance/finops/team/:id` | ✅ Funcional |
| DomainFinOpsPage | `/governance/finops/domain/:id` | ✅ Funcional |

### 3.3 Policies & Packs

| Página | Rota | Estado |
|--------|------|--------|
| PolicyCatalogPage | `/governance/policies` | ✅ Funcional |
| EnterpriseControlsPage | `/governance/controls` | ✅ Funcional |
| GovernancePacksOverviewPage | `/governance/packs` | ✅ Funcional |
| GovernancePackDetailPage | `/governance/packs/:id` | ✅ Funcional |
| EvidencePackagesPage | `/governance/evidence` | ✅ Funcional |
| WaiversPage | `/governance/waivers` | ✅ Funcional |
| MaturityScorecardsPage | `/governance/maturity` | ✅ Funcional |
| BenchmarkingPage | `/governance/benchmarking` | ✅ Funcional |

### 3.4 Organization

| Página | Rota | Estado |
|--------|------|--------|
| TeamsOverviewPage | `/governance/teams` | ✅ Funcional |
| TeamDetailPage | `/governance/teams/:id` | ✅ Funcional |
| DomainsOverviewPage | `/governance/domains` | ✅ Funcional |
| DomainDetailPage | `/governance/domains/:id` | ✅ Funcional |
| DelegatedAdminPage | `/governance/delegated-admin` | ✅ Funcional |

### 3.5 Configuration

| Página | Rota | Estado |
|--------|------|--------|
| GovernanceConfigurationPage | `/platform/configuration/governance` | ✅ Funcional (6 secções) |

---

## 4. Backend — 19 Endpoint Modules

| Módulo | Propósito | Pertinência |
|--------|-----------|------------|
| ExecutiveOverviewEndpointModule | Dashboard executivo | ✅ Governance |
| GovernanceComplianceEndpointModule | Compliance | ✅ Governance |
| GovernanceRiskEndpointModule | Risco | ✅ Governance |
| GovernanceFinOpsEndpointModule | FinOps | ✅ Governance |
| PolicyCatalogEndpointModule | Políticas | ✅ Governance |
| EnterpriseControlsEndpointModule | Controlos enterprise | ✅ Governance |
| GovernancePacksEndpointModule | Packs | ✅ Governance |
| EvidencePackagesEndpointModule | Evidência | ✅ Governance |
| GovernanceWaiversEndpointModule | Exceções | ✅ Governance |
| GovernanceReportsEndpointModule | Relatórios | ✅ Governance |
| TeamEndpointModule | Equipas | ✅ Organization |
| DomainEndpointModule | Domínios | ✅ Organization |
| DelegatedAdminEndpointModule | Admin delegado | ✅ Organization |
| ComplianceChecksEndpointModule | Checks de compliance | ✅ Governance |
| ScopedContextEndpointModule | Contexto scoped | ✅ Cross-cutting |
| OnboardingEndpointModule | Onboarding | ⚠️ **Poderia ser módulo separado** |
| **IntegrationHubEndpointModule** | Hub de integrações | ⚠️ **Deveria ser módulo separado** |
| **ProductAnalyticsEndpointModule** | Analytics de produto | ⚠️ **Deveria ser módulo separado** |
| **PlatformStatusEndpointModule** | Status da plataforma | ⚠️ **Deveria ser módulo separado** |

---

## 5. Banco de Dados

| DbContext | Entidades |
|-----------|-----------|
| GovernanceDbContext | GovernanceDomain, GovernancePack, Team, GovernanceWaiver, IngestionSource, IngestionExecution, IntegrationConnector + 45+ enums |

**Migrations:** InitialCreate, Phase5Enrichment, AddLastProcessedAt

---

## 6. Problema Arquitetural: Módulo Excessivamente Amplo

### Responsabilidades Misturadas

O módulo Governance contém pelo menos 4 responsabilidades que deveriam ser bounded contexts separados:

| Responsabilidade | Deveria Ser | Evidência |
|-----------------|-------------|-----------|
| Integration Hub | Módulo `integrations` | IntegrationHubEndpointModule, IntegrationConnector entity |
| Product Analytics | Módulo `product-analytics` | ProductAnalyticsEndpointModule |
| Platform Status | Módulo `platform` | PlatformStatusEndpointModule |
| Onboarding | Módulo `onboarding` ou subdomínio identity | OnboardingEndpointModule |

### Impacto

- Dificulta manutenção e evolução
- Mistura conceitos de domínio
- Cria acoplamento desnecessário
- Dificulta testes isolados

### Recomendação

**Curto prazo:** Documentar fronteiras internas e manter como está
**Médio prazo:** Extrair IntegrationHub e ProductAnalytics como módulos separados
**Longo prazo:** Reavaliar se Teams/Domains devem ser um módulo Organization separado

---

## 7. Resumo de Ações

### Ações de Validação (P1)

| # | Ação | Esforço |
|---|------|---------|
| 1 | Validar Executive Overview (dados reais vs mock) | 2h |
| 2 | Validar Compliance e Risk Center | 2h |
| 3 | Validar FinOps drill-downs (service, team, domain) | 2h |
| 4 | Validar Governance Packs e Evidence | 2h |
| 5 | Validar Teams e Domains CRUD | 1h |

### Ações Arquiteturais (P2)

| # | Ação | Esforço |
|---|------|---------|
| 6 | Documentar fronteiras internas do módulo | 3h |
| 7 | Avaliar extração de IntegrationHub como módulo | 4h |
| 8 | Avaliar extração de ProductAnalytics como módulo | 4h |
| 9 | Criar documentação unificada do módulo Governance | 4h |

### Ações de Visibilidade (P3)

| # | Ação | Esforço |
|---|------|---------|
| 10 | Promover Controls, Evidence, Maturity, Waivers ao menu (atualmente sub-rotas) | 2h |
| 11 | Documentar todas as 25 páginas | 4h |
