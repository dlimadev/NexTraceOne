# PARTE 2 — Fronteira Final entre Governance e Product Analytics

> **Data**: 2026-03-25
> **Prompt**: N12 — Consolidação do módulo Product Analytics
> **Estado**: DEFINIÇÃO FINAL

---

## 1. Responsabilidades atuais de Governance relacionadas a analytics/métricas

O módulo Governance atualmente hospeda **todas** as responsabilidades de Product Analytics no backend:

| Ficheiro | Responsabilidade | Deveria estar em |
|---------|-----------------|------------------|
| `NexTraceOne.Governance.Domain/Entities/AnalyticsEvent.cs` | Entidade de evento analítico | **Product Analytics** |
| `NexTraceOne.Governance.Domain/Enums/AnalyticsEventType.cs` | Enum de 25 tipos de evento | **Product Analytics** |
| `NexTraceOne.Governance.API/Endpoints/ProductAnalyticsEndpointModule.cs` | 7 endpoints REST | **Product Analytics** |
| `NexTraceOne.Governance.Application/Features/RecordAnalyticsEvent/` | Comando de captura de evento | **Product Analytics** |
| `NexTraceOne.Governance.Application/Features/GetAnalyticsSummary/` | Query de resumo analítico | **Product Analytics** |
| `NexTraceOne.Governance.Application/Features/GetModuleAdoption/` | Query de adoção por módulo | **Product Analytics** |
| `NexTraceOne.Governance.Application/Features/GetPersonaUsage/` | Query de uso por persona | **Product Analytics** |
| `NexTraceOne.Governance.Application/Features/GetJourneys/` | Query de funnels de jornada | **Product Analytics** |
| `NexTraceOne.Governance.Application/Features/GetValueMilestones/` | Query de milestones de valor | **Product Analytics** |
| `NexTraceOne.Governance.Application/Features/GetFrictionIndicators/` | Query de indicadores de fricção | **Product Analytics** |
| `NexTraceOne.Governance.Infrastructure/Persistence/Repositories/AnalyticsEventRepository.cs` | Repositório de eventos analíticos | **Product Analytics** |
| `NexTraceOne.Governance.Infrastructure/Persistence/Configurations/AnalyticsEventConfiguration.cs` | EF Configuration (gov_analytics_events) | **Product Analytics** |

### Responsabilidades de Governance que são realmente Governance (reports/compliance)

| Funcionalidade | Localização | Status |
|---------------|-------------|--------|
| Governance Reports (compliance) | `GetGovernanceReports/` | ✅ Correto em Governance |
| Executive Overview (compliance trends) | `GetExecutiveOverview/` | ✅ Correto em Governance |
| Maturity Scorecards | `GetMaturityScorecard/` | ✅ Correto em Governance |
| Risk assessment | `GetRiskDashboard/` | ✅ Correto em Governance |
| Benchmarking | `GetBenchmarks/` | ✅ Correto em Governance |
| FinOps metrics | `GetFinOpsMetrics/` | ✅ Correto em Governance |
| Compliance checks | Múltiplas features | ✅ Correto em Governance |

---

## 2. Responsabilidades atuais de Product Analytics

### Backend (dentro de Governance)

| Feature | Tipo | Dados | Status |
|---------|------|-------|--------|
| RecordAnalyticsEvent | Command | Real (grava no DB) | ✅ Funcional |
| GetAnalyticsSummary | Query | Misto (real + calculado) | ⚠️ Parcial |
| GetModuleAdoption | Query | Parcialmente real | ⚠️ Parcial |
| GetPersonaUsage | Query | **Mock data hardcoded** | 🔴 Mock |
| GetJourneys | Query | Dados limitados | ⚠️ Parcial |
| GetValueMilestones | Query | Dados limitados | ⚠️ Parcial |
| GetFrictionIndicators | Query | **Real data** via repository | ✅ Real |

### Frontend (independente)

| Página | Status |
|--------|--------|
| ProductAnalyticsOverviewPage | ✅ Funcional (consome API) |
| ModuleAdoptionPage | ✅ Funcional |
| PersonaUsagePage | ✅ Funcional (dados mock no backend) |
| JourneyFunnelPage | ✅ Funcional |
| ValueTrackingPage | ✅ Funcional |

---

## 3. O que ainda está indevidamente dentro de Governance

**TUDO** que diz respeito a Product Analytics está indevidamente dentro de Governance:

| Item | Ficheiro Concreto | Ação Necessária |
|------|-------------------|-----------------|
| Entidade AnalyticsEvent | `Governance.Domain/Entities/AnalyticsEvent.cs` | Mover para ProductAnalytics.Domain |
| Enum AnalyticsEventType | `Governance.Domain/Enums/AnalyticsEventType.cs` | Mover para ProductAnalytics.Domain |
| DbSet AnalyticsEvents | `GovernanceDbContext.cs` (linha ~55) | Remover de GovernanceDbContext |
| EF Config | `AnalyticsEventConfiguration.cs` | Mover para ProductAnalytics.Infrastructure |
| Repository | `AnalyticsEventRepository.cs` | Mover para ProductAnalytics.Infrastructure |
| Interface | `IAnalyticsEventRepository.cs` | Mover para ProductAnalytics.Application |
| 7 Features | `Features/Record.../Get...` (7 pastas) | Mover para ProductAnalytics.Application |
| Endpoint Module | `ProductAnalyticsEndpointModule.cs` | Mover para ProductAnalytics.API |
| DI Registration | `DependencyInjection.cs` (linha ~51) | Mover para ProductAnalytics.Infrastructure DI |

**Total: ~15 ficheiros a extrair de Governance**

---

## 4. Definição: o que é Policy/Compliance/Governança

Pertence a **Governance** tudo o que responde à pergunta: **"Está conforme?"**

| Tipo | Exemplos | Módulo |
|------|----------|--------|
| Compliance reports | Score de conformidade, exceções ativas | Governance |
| Maturity scorecards | Nível de maturidade organizacional | Governance |
| Risk dashboards | Nível de risco, controles ativos | Governance |
| Executive trends (compliance) | Evolução de conformidade no tempo | Governance |
| Governance packs & waivers | Políticas aplicadas e exceções | Governance |
| FinOps (custo operacional) | Custos por serviço/equipa | Governance |
| Benchmarking (vs peers) | Comparação organizacional | Governance |

---

## 5. Definição: o que é Usage Analytics / Product Insight

Pertence a **Product Analytics** tudo o que responde à pergunta: **"Como estão a usar o produto?"**

| Tipo | Exemplos | Módulo |
|------|----------|--------|
| Page view tracking | Quais páginas são visitadas | Product Analytics |
| Feature usage | Quais features são usadas e com que frequência | Product Analytics |
| Module adoption | % de adoção por módulo do produto | Product Analytics |
| Persona usage | Como cada persona usa o produto | Product Analytics |
| Journey funnels | Onde os utilizadores abandonam jornadas | Product Analytics |
| Friction signals | Zero results, empty states, abandonos | Product Analytics |
| Value milestones | Tempo até primeiro valor, até valor core | Product Analytics |
| Engagement metrics | Frequência de uso, profundidade de uso | Product Analytics |
| Adoption scores | Score composto de adoção do produto | Product Analytics |

---

## 6. Resumo da fronteira final

### Fica em Governance ✅

1. Compliance reporting (score, exceções, trends)
2. Maturity scorecards
3. Risk & control dashboards
4. Executive overview (compliance-focused)
5. Governance packs, waivers, rollouts
6. FinOps metrics
7. Benchmarking organizacional
8. Onboarding progress (organizacional)

### Fica em Product Analytics ✅

1. Captura de eventos de uso (page views, actions, searches)
2. Métricas de adoção por módulo
3. Métricas de uso por persona
4. Funnels de jornada do utilizador
5. Value milestones (tempo até valor)
6. Indicadores de fricção
7. Feature usage tracking
8. Engagement e retenção
9. KPIs compostos de produto (adoption score, value score, friction score)
10. Dashboards analíticos de produto

---

## 7. Exemplos concretos

### Exemplo 1: "Quantos utilizadores usaram o Contract Studio este mês?"

→ **Product Analytics** (é usage tracking)

### Exemplo 2: "O Contract Studio está conforme com a política de aprovação?"

→ **Governance** (é compliance check)

### Exemplo 3: "Quanto tempo demora um Tech Lead a criar o primeiro contrato?"

→ **Product Analytics** (é time-to-value por persona)

### Exemplo 4: "Quantas exceções de governança estão ativas na organização?"

→ **Governance** (é compliance reporting)

### Exemplo 5: "Onde é que os utilizadores estão a abandonar jornadas no produto?"

→ **Product Analytics** (é friction/funnel analysis)

### Exemplo 6: "O nível de risco da organização melhorou este trimestre?"

→ **Governance** (é risk assessment)

### Exemplo 7: "Qual é o score de adoção do módulo Operational Intelligence?"

→ **Product Analytics** (é adoption metric)

---

## 8. Conflitos identificados entre consolidado e código real

| Conflito | Documentação | Código Real |
|---------|--------------|-------------|
| Localização backend | "Módulo próprio" (architecture docs) | Dentro de `src/modules/governance/` |
| Prefixo de tabela | `pan_` (database-table-prefixes.md) | `gov_analytics_events` |
| Permissions | Deveria ser `analytics:*` | Usa `governance:analytics:*` |
| DbContext | Deveria ser `ProductAnalyticsDbContext` | Usa `GovernanceDbContext` |
| Dados | "Analytics real" | GetPersonaUsage usa mock data hardcoded |
