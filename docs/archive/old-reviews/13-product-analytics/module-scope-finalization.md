# PARTE 3 — Escopo Funcional Final do Módulo Product Analytics

> **Data**: 2026-03-25
> **Prompt**: N12 — Consolidação do módulo Product Analytics
> **Estado**: DEFINIÇÃO FINAL

---

## 1. Funcionalidades já existentes

| # | Funcionalidade | Ficheiro(s) | Status |
|---|---------------|-------------|--------|
| 1 | Captura de evento de uso (POST) | `RecordAnalyticsEvent/`, `AnalyticsEventTracker.tsx` | ✅ Funcional (apenas ModuleViewed) |
| 2 | Consulta de resumo analítico | `GetAnalyticsSummary/` | ✅ Funcional (dados mistos) |
| 3 | Consulta de adoção por módulo | `GetModuleAdoption/` | ✅ Funcional (parcialmente real) |
| 4 | Consulta de uso por persona | `GetPersonaUsage/` | ⚠️ Mock data hardcoded |
| 5 | Consulta de funnels de jornada | `GetJourneys/` | ⚠️ Dados limitados |
| 6 | Consulta de milestones de valor | `GetValueMilestones/` | ⚠️ Dados limitados |
| 7 | Consulta de indicadores de fricção | `GetFrictionIndicators/` | ✅ Real data |
| 8 | Dashboard overview frontend | `ProductAnalyticsOverviewPage.tsx` | ✅ Funcional |
| 9 | Página de adoção por módulo | `ModuleAdoptionPage.tsx` | ✅ Funcional |
| 10 | Página de uso por persona | `PersonaUsagePage.tsx` | ✅ Funcional |
| 11 | Página de funnels | `JourneyFunnelPage.tsx` | ✅ Funcional |
| 12 | Página de value tracking | `ValueTrackingPage.tsx` | ✅ Funcional |
| 13 | API client frontend | `productAnalyticsApi.ts` | ✅ Completo (7 funções) |
| 14 | Event tracker client | `AnalyticsEventTracker.tsx` | ✅ Funcional (sessões + page views) |
| 15 | Sidebar e menu | `AppSidebar.tsx`, `CommandPalette.tsx` | ✅ Presente |
| 16 | i18n (3 idiomas) | `pt-PT.json`, `es.json`, `en.json` | ✅ Parcial |

---

## 2. Funcionalidades parciais

| # | Funcionalidade | O que falta |
|---|---------------|-------------|
| 1 | Captura de eventos | Só captura `ModuleViewed`; faltam 24 outros tipos de evento |
| 2 | GetAnalyticsSummary | Dados mistos (real + calculado sem base suficiente) |
| 3 | GetModuleAdoption | Adoção calculada sem instrumentação completa nos módulos |
| 4 | GetJourneys | Sem eventos suficientes para construir funnels reais |
| 5 | GetValueMilestones | Sem definição clara de milestones por persona |
| 6 | i18n | Faltam keys para páginas de sub-detalhe e estados vazios |

---

## 3. Funcionalidades ausentes

| # | Funcionalidade | Prioridade | Justificação |
|---|---------------|-----------|--------------|
| 1 | Backend próprio (fora de Governance) | **P0_BLOCKER** | Blocker arquitetural (OI-03) |
| 2 | ProductAnalyticsDbContext com pan_ prefix | **P0_BLOCKER** | Sem isto não há migrations próprias |
| 3 | ClickHouse integration | **P1_CRITICAL** | REQUIRED por module-data-placement-matrix.md |
| 4 | Feature usage tracking (beyond page views) | **P1_CRITICAL** | Sem isto, métricas são superficiais |
| 5 | Instrumentação real em cada módulo | **P1_CRITICAL** | Sem isto, dados de adoção são incompletos |
| 6 | Eliminação de mock data em GetPersonaUsage | **P1_CRITICAL** | Dados hardcoded em produção |
| 7 | Engagement metrics (frequência, profundidade) | **P2_HIGH** | Necessário para medir saúde do produto |
| 8 | Retenção por cohort | **P2_HIGH** | Necessário para medir valor no tempo |
| 9 | Definição formal de journeys e milestones | **P2_HIGH** | Sem definição, funnels são genéricos |
| 10 | Exportação de dados analíticos | **P2_HIGH** | Necessário para reporting externo |
| 11 | Configuração de eventos custom | **P3_MEDIUM** | Permite extensibilidade futura |
| 12 | Alertas de anomalia de uso | **P3_MEDIUM** | Detecção proativa de problemas de adoção |
| 13 | Comparação temporal (periodos) | **P3_MEDIUM** | Trends e comparações |
| 14 | Audit trail das consultas analíticas | **P3_MEDIUM** | Rastreabilidade de quem consultou |
| 15 | Rate limiting no POST /events | **P2_HIGH** | Protecção contra flood de eventos |
| 16 | Batch event ingestion | **P2_HIGH** | Performance para volumes elevados |

---

## 4. Classificação: obrigatório vs desejável no produto final

### Tier 1 — Obrigatório (sem isto o módulo não funciona)

| # | Funcionalidade | Justificação |
|---|---------------|--------------|
| 1 | Backend extraído de Governance | Independência arquitetural |
| 2 | ProductAnalyticsDbContext + pan_ prefix | Persistência própria |
| 3 | ClickHouse para eventos de alto volume | Escalabilidade analítica |
| 4 | Eliminação de mock data | Credibilidade dos dados |
| 5 | Captura de pelo menos 10 tipos de evento reais | Dados mínimos para métricas |
| 6 | Permissions próprias (analytics:*) | Segurança adequada |
| 7 | Summary, adoption, friction dashboards com dados reais | Valor mínimo do módulo |

### Tier 2 — Importante (valor significativo)

| # | Funcionalidade | Justificação |
|---|---------------|--------------|
| 1 | Persona usage com dados reais | Segmentação por persona |
| 2 | Journey funnels com dados reais | Detecção de abandono |
| 3 | Value milestones com definição formal | Medir time-to-value |
| 4 | Feature usage tracking | Profundidade de adoção |
| 5 | Engagement metrics | Saúde do produto |
| 6 | Batch event ingestion | Performance |
| 7 | Rate limiting | Segurança |

### Tier 3 — Desejável (pode esperar)

| # | Funcionalidade | Justificação |
|---|---------------|--------------|
| 1 | Retenção por cohort | Análise avançada |
| 2 | Eventos custom | Extensibilidade |
| 3 | Alertas de anomalia | Proatividade |
| 4 | Exportação de dados | Reporting externo |
| 5 | Comparação temporal avançada | Trends |

---

## 5. O que NÃO pertence ao módulo

| Funcionalidade | Módulo correto | Razão |
|---------------|----------------|-------|
| Compliance score e reports | Governance | É conformidade, não uso |
| Maturity scorecards | Governance | É maturidade organizacional |
| Risk dashboards | Governance | É gestão de risco |
| FinOps metrics | Governance | É custo operacional |
| SLA/SLO monitoring | Operational Intelligence | É observabilidade |
| Infrastructure metrics | Operational Intelligence | É infraestrutura |
| Audit trail de ações | Audit & Compliance | É auditoria |
| User session management | Identity & Access | É autenticação |

---

## 6. Conjunto mínimo completo do módulo final

### Backend mínimo

| Componente | Descrição |
|-----------|-----------|
| `ProductAnalyticsDbContext` | DbContext próprio com pan_ prefix |
| `AnalyticsEvent` | Entidade principal de evento |
| `AnalyticsEventType` | Enum com ≥15 tipos de evento reais |
| `AnalyticsDefinition` (NOVO) | Configuração de métricas e eventos |
| `IAnalyticsEventRepository` | Interface de acesso a dados |
| `IAnalyticsQueryService` | Interface para queries ClickHouse |
| `RecordAnalyticsEvent` | Command handler |
| `GetAnalyticsSummary` | Query handler (dados reais) |
| `GetModuleAdoption` | Query handler (dados reais) |
| `GetPersonaUsage` | Query handler (dados reais, sem mock) |
| `GetJourneys` | Query handler (dados reais) |
| `GetValueMilestones` | Query handler (dados reais) |
| `GetFrictionIndicators` | Query handler (já real) |
| `ProductAnalyticsEndpointModule` | 7+ endpoints REST |
| ClickHouse adapter | Escrita e leitura de eventos analíticos |

### Frontend mínimo

| Componente | Descrição |
|-----------|-----------|
| ProductAnalyticsOverviewPage | Dashboard principal |
| ModuleAdoptionPage | Adoção por módulo |
| PersonaUsagePage | Uso por persona |
| JourneyFunnelPage | Funnels de jornada |
| ValueTrackingPage | Milestones de valor |
| AnalyticsEventTracker | Captura de eventos client-side |
| productAnalyticsApi.ts | API client |
| i18n completo (3 idiomas) | Todas as keys necessárias |

### ClickHouse mínimo

| Componente | Descrição |
|-----------|-----------|
| `pan_events` table | Tabela principal de eventos analíticos |
| `pan_daily_aggregates` materialized view | Agregações diárias |
| Event writer service | Escrita assíncrona de eventos |
| Query adapter | Leitura de métricas agregadas |
