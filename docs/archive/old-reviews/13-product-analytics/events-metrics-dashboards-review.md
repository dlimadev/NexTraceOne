# PARTE 9 — Revisão de Eventos, Métricas e Dashboards

> **Data**: 2026-03-25
> **Prompt**: N12 — Consolidação do módulo Product Analytics
> **Estado**: REVISÃO COMPLETA

---

## 1. Eventos existentes

### Definidos no enum `AnalyticsEventType` (25 valores)

| # | Evento | Categoria | Instrumentado | Fonte de dados |
|---|--------|-----------|--------------|----------------|
| 0 | ModuleViewed | Navigation | ✅ Via AnalyticsEventTracker | Frontend auto |
| 1 | EntityViewed | Navigation | ❌ | Precisa instrumentação |
| 2 | SearchExecuted | Search | ❌ | Precisa instrumentação |
| 3 | SearchResultClicked | Search | ❌ | Precisa instrumentação |
| 4 | ZeroResultSearch | Friction | ❌ | Precisa instrumentação |
| 5 | QuickActionTriggered | Action | ❌ | Precisa instrumentação |
| 6 | AssistantPromptSubmitted | AI | ❌ | Precisa instrumentação |
| 7 | AssistantResponseUsed | AI | ❌ | Precisa instrumentação |
| 8 | ContractDraftCreated | Contract | ❌ | Precisa instrumentação |
| 9 | ContractPublished | Contract | ❌ | Precisa instrumentação |
| 10 | ChangeViewed | Change | ❌ | Precisa instrumentação |
| 11 | IncidentInvestigated | Operations | ❌ | Precisa instrumentação |
| 12 | MitigationWorkflowStarted | Operations | ❌ | Precisa instrumentação |
| 13 | MitigationWorkflowCompleted | Operations | ❌ | Precisa instrumentação |
| 14 | EvidencePackageExported | Governance | ❌ | Precisa instrumentação |
| 15 | PolicyViewed | Governance | ❌ | Precisa instrumentação |
| 16 | ExecutiveOverviewViewed | Executive | ❌ | Precisa instrumentação |
| 17 | RunbookViewed | Operations | ❌ | Precisa instrumentação |
| 18 | SourceOfTruthQueried | Knowledge | ❌ | Precisa instrumentação |
| 19 | ReportGenerated | Reporting | ❌ | Precisa instrumentação |
| 20 | OnboardingStepCompleted | Onboarding | ❌ | Precisa instrumentação |
| 21 | JourneyAbandoned | Friction | ❌ | Precisa instrumentação |
| 22 | EmptyStateEncountered | Friction | ❌ | Precisa instrumentação |
| 23 | ReliabilityDashboardViewed | Operations | ❌ | Precisa instrumentação |
| 24 | AutomationWorkflowManaged | Operations | ❌ | Precisa instrumentação |

### Resumo de instrumentação

| Status | Contagem | Percentagem |
|--------|----------|-------------|
| ✅ Instrumentado | 1 | **4%** |
| ❌ Não instrumentado | 24 | 96% |

**Conclusão**: Apenas 1 de 25 tipos de evento está realmente a ser capturado. O módulo está a operar com **4% de instrumentação**.

---

## 2. Métricas existentes

### Métricas implementadas nos handlers

| # | Métrica | Handler | Fonte | Status |
|---|--------|---------|-------|--------|
| 1 | Adoption Score (%) | GetAnalyticsSummary | Cálculo composto | ⚠️ Dados mistos |
| 2 | Value Score (%) | GetAnalyticsSummary | Cálculo composto | ⚠️ Dados limitados |
| 3 | Friction Score (%) | GetAnalyticsSummary | Cálculo composto | ⚠️ Dados limitados |
| 4 | Unique Users (count) | GetAnalyticsSummary | Repository real | ✅ Real |
| 5 | Time to First Value (min) | GetAnalyticsSummary | Calculado | ⚠️ Sem milestones formais |
| 6 | Time to Core Value (min) | GetAnalyticsSummary | Calculado | ⚠️ Sem milestones formais |
| 7 | Top Modules (list) | GetAnalyticsSummary | Repository real | ✅ Real |
| 8 | Module Adoption % | GetModuleAdoption | Repository real | ⚠️ Parcial |
| 9 | Module Actions Count | GetModuleAdoption | Repository real | ✅ Real |
| 10 | Module Unique Users | GetModuleAdoption | Repository real | ✅ Real |
| 11 | Module Depth Score | GetModuleAdoption | Calculado | ⚠️ Simplificado |
| 12 | Module Top Features | GetModuleAdoption | Repository real | ⚠️ Depende de instrumentação |
| 13 | Module Trend | GetModuleAdoption | Calculado | ⚠️ Simplificado |
| 14 | Persona Total Actions | GetPersonaUsage | **MOCK** | 🔴 Mock data |
| 15 | Persona Adoption % | GetPersonaUsage | **MOCK** | 🔴 Mock data |
| 16 | Persona Depth Score | GetPersonaUsage | **MOCK** | 🔴 Mock data |
| 17 | Friction Count | GetFrictionIndicators | Repository real | ✅ Real |
| 18 | Friction Impact % | GetFrictionIndicators | Cálculo real | ✅ Real |
| 19 | Friction Trend | GetFrictionIndicators | Comparação periodos | ✅ Real |

### Resumo de qualidade de métricas

| Qualidade | Contagem | Percentagem |
|-----------|----------|-------------|
| ✅ Real | 7 | 37% |
| ⚠️ Parcial/Simplificado | 9 | 47% |
| 🔴 Mock data | 3 | 16% |

---

## 3. Dashboards existentes

| # | Dashboard | Página | Métricas apresentadas | Status |
|---|-----------|--------|----------------------|--------|
| 1 | Overview Dashboard | ProductAnalyticsOverviewPage | 6 stat cards + top modules + nav links | ⚠️ Dados mistos |
| 2 | Module Adoption | ModuleAdoptionPage | Lista de módulos com adoção, actions, users, depth, features, trends | ⚠️ Parcialmente real |
| 3 | Persona Usage | PersonaUsagePage | Perfis por persona com actions, adoption, modules, friction, milestones | 🔴 Mock data |
| 4 | Journey Funnel | JourneyFunnelPage | Funnel steps com conversão | ⚠️ Dados limitados |
| 5 | Value Tracking | ValueTrackingPage | Milestones com personas e tempos | ⚠️ Dados limitados |

---

## 4. Validação da origem real dos dados

### Fluxo de dados atual

```
AnalyticsEventTracker.tsx → POST /events → RecordAnalyticsEvent → AnalyticsEventRepository → gov_analytics_events
                                                                                                      ↓
GET handlers → AnalyticsEventRepository → Queries SQL → DTOs → Frontend pages
```

### Pontos de confiança

| Ponto | Confiável | Razão |
|-------|-----------|-------|
| AnalyticsEventTracker captura ModuleViewed | ✅ | Evento real de page view |
| Repository grava no PostgreSQL | ✅ | Persistência real |
| GetFrictionIndicators lê dados reais | ✅ | Queries contra repository |
| GetAnalyticsSummary unique users | ✅ | COUNT DISTINCT real |
| GetModuleAdoption dados base | ✅ | Queries contra repository |
| GetPersonaUsage | 🔴 | **Mock data hardcoded no handler** |
| Scores compostos (adoption, value, friction) | ⚠️ | Fórmulas simplificadas sem dados suficientes |
| Time to value | ⚠️ | Sem definição formal de milestones |

---

## 5. Métricas baseadas em mock ou cálculo fraco

| Métrica | Problema | Impacto |
|---------|----------|---------|
| Persona usage profiles | Dados hardcoded para 7 personas com valores fictícios | 🔴 Dashboard inteiro mostra dados falsos |
| Adoption score composto | Fórmula não documentada, depende de instrumentação incompleta | 🟠 Score pode ser enganoso |
| Value score composto | Sem milestones formais, calculado com lógica simplificada | 🟠 Score pode ser enganoso |
| Time to first/core value | Sem eventos de milestone para calcular realmente | 🟠 Valor estimado, não medido |
| Journey funnels | Sem instrumentação de steps intermediários | 🟠 Funnels incompletos |
| Module depth score | Calculado sem todos os eventos de feature | 🟠 Profundidade subestimada |

---

## 6. Dashboards sem dados confiáveis

| Dashboard | Confiabilidade | Ação |
|-----------|---------------|------|
| Overview | ⚠️ **40%** | Unique users e top modules são reais; scores compostos são fracos |
| Adoption | ⚠️ **60%** | Dados base reais, mas depth e trend são simplificados |
| Personas | 🔴 **0%** | **Completamente mock** |
| Journeys | ⚠️ **20%** | Sem eventos suficientes |
| Value | ⚠️ **20%** | Sem milestones formais |

---

## 7. Conjunto mínimo real de eventos

Para o módulo funcionar com **dados reais**, é necessário instrumentar no mínimo:

| # | Evento | Módulo Emissor | Esforço | Prioridade |
|---|--------|---------------|---------|-----------|
| 1 | ModuleViewed | Frontend (global) | ✅ Já implementado | — |
| 2 | EntityViewed | Catalog, Contracts | 2h | P1 |
| 3 | SearchExecuted | Search/Command Palette | 2h | P1 |
| 4 | ZeroResultSearch | Search/Command Palette | 1h | P1 |
| 5 | QuickActionTriggered | Command Palette | 1h | P2 |
| 6 | ContractDraftCreated | Contracts module | 1h | P2 |
| 7 | ContractPublished | Contracts module | 1h | P2 |
| 8 | ChangeViewed | Change Governance | 1h | P2 |
| 9 | IncidentInvestigated | Operational Intelligence | 1h | P2 |
| 10 | AssistantPromptSubmitted | AI & Knowledge | 1h | P2 |
| **Total mínimo** | **10 tipos** | | **~11h** | |

---

## 8. Conjunto mínimo real de métricas

| # | Métrica | Fonte | Prioridade |
|---|--------|-------|-----------|
| 1 | Total events por módulo | COUNT from events | P1 |
| 2 | Unique users por módulo | COUNT DISTINCT from events | P1 |
| 3 | Module adoption % | Users with events / total users | P1 |
| 4 | Friction rate por módulo | Friction events / total events | P1 |
| 5 | Top features por módulo | Feature counts from events | P2 |
| 6 | Persona breakdown | Events grouped by persona | P2 |
| 7 | Trend (period comparison) | Current vs previous period | P2 |
| 8 | Session depth | Events per session | P3 |

---

## 9. Conjunto mínimo real de dashboards

| # | Dashboard | Métricas necessárias | Prioridade |
|---|-----------|---------------------|-----------|
| 1 | Overview | Total events, unique users, top modules, friction rate | P1 |
| 2 | Module Adoption | Adoption %, actions, users, trend por módulo | P1 |
| 3 | Friction Analysis | Friction events, types, impact, trend | P1 |
| 4 | Persona Usage | **Real data** — events by persona (NOT mock) | P2 |
| 5 | Journey Funnel | Step conversion (requer instrumentação) | P3 |
| 6 | Value Tracking | Time to value (requer milestones formais) | P3 |

---

## 10. Lacunas e correções necessárias

| # | ID | Lacuna | Prioridade | Esforço |
|---|-----|--------|-----------|---------|
| 1 | E-01 | Instrumentar 9+ tipos de evento adicionais além de ModuleViewed | P1_CRITICAL | 11h |
| 2 | E-02 | Eliminar mock data em GetPersonaUsage | P1_CRITICAL | 4h |
| 3 | E-03 | Documentar fórmulas dos scores compostos | P2_HIGH | 2h |
| 4 | E-04 | Definir milestones formais por persona | P2_HIGH | 3h |
| 5 | E-05 | Definir journeys formais com steps | P2_HIGH | 3h |
| 6 | E-06 | Implementar materialized views no ClickHouse | P1_CRITICAL | 8h |
| 7 | E-07 | Adicionar indicador visual de confiança de dados nos dashboards | P2_HIGH | 2h |
| 8 | E-08 | Validar que AnalyticsEventTracker não duplica eventos em SPA navigation | P2_HIGH | 2h |

**Total eventos/métricas/dashboards**: 8 itens, ~35h estimadas
