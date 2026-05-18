# Módulo: Product Analytics — Cenários de Teste Funcionais

> Cobertura: Dashboards, Analytics, FinOps, Executivo, Jornadas, Personas, Benchmarking, Relatórios

---

## Dashboards

### TC-PA-001 — Criar dashboard customizado

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | CreateCustomDashboard |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:**
- Tenant autenticado.

**Passos:**
1. Enviar `CreateCustomDashboard.Command(name: "Overview Executivo", layout: GridLayout, widgets: [...], isPublic: false)`.
2. Handler valida `name` não vazio; máximo de widgets (50) não excedido.
3. Persiste com `CreatedBy` preenchido.

**Resultado Esperado:**
- `result.IsSuccess == true`; `dashboardId` retornado.
- `IsPublished = false`.

**Critério de Aceite:** HTTP 201.

---

### TC-PA-002 — Atualizar dashboard

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | UpdateCustomDashboard |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** Dashboard `D1` criado pelo usuário.

**Passos:**
1. Enviar `UpdateCustomDashboard.Command(D1.Id, name: "Overview Executivo v2", addWidgets: [w4])`.
2. Handler verifica que usuário é o dono ou tem permissão de edição.

**Resultado Esperado:**
- `UpdatedAt` preenchido; widgets adicionados.

**Critério de Aceite:** HTTP 200.

---

### TC-PA-003 — Publicar dashboard

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | PublishDashboard |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** Dashboard `D1` privado.

**Passos:**
1. `PublishDashboard.Command(D1.Id)`.
2. `IsPublished = true`; `PublishedAt` preenchido.

**Resultado Esperado:**
- Dashboard visível para todos os usuários do tenant.

**Critério de Aceite:** HTTP 200; `isPublished: true`.

---

### TC-PA-004 — Clonar dashboard

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | CloneDashboard |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Pré-condições:** Dashboard `D1` com 5 widgets.

**Passos:**
1. `CloneDashboard.Command(D1.Id, newName: "Overview Executivo - Cópia")`.

**Resultado Esperado:**
- Novo dashboard criado com mesmo layout e widgets de `D1`; `IsPublished = false`.

**Critério de Aceite:** HTTP 201 com novo `dashboardId`.

---

### TC-PA-005 — Deprecar dashboard

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | DeprecateDashboard |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Pré-condições:** Dashboard publicado.

**Passos:**
1. `DeprecateDashboard.Command(D1.Id, deprecationMessage: "Use Overview v2")`.

**Resultado Esperado:**
- `IsDeprecated = true`; mensagem exibida na UI.

**Critério de Aceite:** HTTP 200.

---

### TC-PA-006 — Reverter dashboard para versão anterior

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | RevertDashboard |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Pré-condições:** Dashboard com 3 versões no histórico.

**Passos:**
1. `RevertDashboard.Command(D1.Id, targetVersion: 1)`.

**Resultado Esperado:**
- Dashboard restaurado para layout da versão 1.

**Critério de Aceite:** HTTP 200; `GetDashboardHistory` mostra versão atual = 1.

---

### TC-PA-007 — Compartilhar dashboard

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | ShareDashboard |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Passos:**
1. `ShareDashboard.Command(D1.Id, shareWith: ["user2@empresa.com"], permission: ReadOnly)`.

**Resultado Esperado:**
- `user2` pode visualizar mas não editar `D1`.

**Critério de Aceite:** HTTP 200.

---

### TC-PA-008 — Obter dados de renderização do dashboard

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | GetDashboardRenderData |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:** Dashboard com 3 widgets (gráfico de linha, barras, número).

**Passos:**
1. `GetDashboardRenderData.Query(D1.Id, timeRange: last7Days)`.
2. Handler agrega dados de cada widget em paralelo.

**Resultado Esperado:**
- Dados de cada widget retornados; `DataFreshness` em cada widget.

**Critério de Aceite:** HTTP 200; `widgets.length == 3`.

---

### TC-PA-009 — Exportar dashboard como YAML

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | ExportDashboardAsYaml |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Passos:**
1. `ExportDashboardAsYaml.Query(D1.Id)`.

**Resultado Esperado:**
- YAML válido representando o dashboard.

**Critério de Aceite:** HTTP 200 `Content-Type: application/yaml`.

---

### TC-PA-010 — Analisar uso de dashboard

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | GetDashboardUsageAnalytics |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:** Dashboard visto 150 vezes por 30 usuários distintos no mês.

**Passos:**
1. `GetDashboardUsageAnalytics.Query(D1.Id, period: last30Days)`.

**Resultado Esperado:**
- `TotalViews = 150`, `UniqueUsers = 30`, `AvgTimeOnDashboard`.

**Critério de Aceite:** HTTP 200.

---

### TC-PA-011 — Isolamento de dashboard entre tenants

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | ListCustomDashboards |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |

**Pré-condições:** Tenant A com 3 dashboards; Tenant B com 7 dashboards.

**Passos:**
1. Tenant A consulta `ListCustomDashboards`.

**Resultado Esperado:**
- Retorna apenas 3 dashboards; RLS garante isolamento.

**Critério de Aceite:** `result.Value.Count == 3`.

---

## Analytics de Eventos

### TC-PA-012 — Registrar evento de analytics

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | RecordAnalyticsEvent |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Passos:**
1. `RecordAnalyticsEvent.Command(eventType: "contract.viewed", entityId: contractId, userId, metadata: {...})`.

**Resultado Esperado:**
- Evento persistido com `OccurredAt = now`.

**Critério de Aceite:** HTTP 202 (fire-and-forget).

---

### TC-PA-013 — Funil de adoção

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | GetAdoptionFunnel |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- 1000 usuários em onboarding; 600 completaram passo 1; 300 passo 2; 100 passo 3.

**Passos:**
1. `GetAdoptionFunnel.Query(funnelType: "onboarding")`.

**Resultado Esperado:**
- Steps com contagem e taxa de conversão entre passos.

**Critério de Aceite:** HTTP 200; `conversionRate(1→2) = 60%`.

---

### TC-PA-014 — Análise de coorte

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | GetCohortAnalysis |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:** Coortes mensais dos últimos 6 meses.

**Passos:**
1. `GetCohortAnalysis.Query(metric: "retention", granularity: "monthly", periods: 6)`.

**Resultado Esperado:**
- Matriz de retenção 6×6; coorte mais recente com 100% na semana 0.

**Critério de Aceite:** HTTP 200.

---

### TC-PA-015 — Mapa de calor de features

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | GetFeatureHeatmap |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** Eventos de uso registrados para 20 features.

**Passos:**
1. `GetFeatureHeatmap.Query(period: last30Days)`.

**Resultado Esperado:**
- Mapa com intensidade de uso por feature; top-5 e bottom-5.

**Critério de Aceite:** HTTP 200.

---

### TC-PA-016 — Adoção de módulos

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | GetModuleAdoption |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Passos:**
1. `GetModuleAdoption.Query(tenantId)`.

**Resultado Esperado:**
- % de usuários ativos por módulo (Catalog, ChangeGovernance, AIKnowledge, etc.).

**Critério de Aceite:** HTTP 200 com breakdown.

---

## FinOps

### TC-PA-017 — Resumo FinOps do tenant

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | GetFinOpsSummary |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:** Dados de custo ingeridos para o mês corrente.

**Passos:**
1. `GetFinOpsSummary.Query(period: currentMonth)`.

**Resultado Esperado:**
- `TotalCost`, `TotalWaste`, `CostByService` (top-5), `TrendVsPreviousPeriod`.

**Critério de Aceite:** HTTP 200.

---

### TC-PA-018 — Detecção de anomalias de custo

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | DetectCostAnomalies |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Custo médio diário: $1.000; hoje: $3.500 (aumento de 250%).

**Passos:**
1. `DetectCostAnomalies.Command(tenantId, date: today)`.
2. Handler compara vs baseline; `threshold = 200%`.

**Resultado Esperado:**
- Anomalia detectada; alerta gerado via Outbox.

**Critério de Aceite:** `anomalyDetected = true`; `alertSent = true`.

---

### TC-PA-019 — Criar aprovação de budget FinOps

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | CreateFinOpsBudgetApproval |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Passos:**
1. `CreateFinOpsBudgetApproval.Command(serviceId, amount: 5000.00, period: "2026-06", reason: "Campanha de marketing Q2")`.
2. `Status = PendingApproval`.

**Resultado Esperado:**
- Aprovação criada; notificação enviada ao aprovador via Outbox.

**Critério de Aceite:** HTTP 201.

---

### TC-PA-020 — Resolver aprovação de budget

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | ResolveFinOpsBudgetApproval |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** Aprovação `A1` pendente.

**Passos:**
1. `ResolveFinOpsBudgetApproval.Command(A1.Id, decision: Approved, approvedBy: "cfo@empresa.com")`.
2. `Status = Approved`; `ApprovedAt` preenchido.

**Resultado Esperado:**
- Budget ativado para o período.

**Critério de Aceite:** HTTP 200.

---

### TC-PA-021 — Relatório de desperdício (waste)

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | GetWasteReport |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** Instâncias com < 10% de utilização de CPU/memória.

**Passos:**
1. `GetWasteReport.Query(threshold: 10)`.

**Resultado Esperado:**
- Lista de recursos ociosos; custo mensal de desperdício estimado.

**Critério de Aceite:** HTTP 200.

---

### TC-PA-022 — Relatório GreenOps / carbono

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | GetGreenOpsReport / GetCarbonScoreReport |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Passos:**
1. `GetGreenOpsReport.Query(period: lastQuarter)`.
2. `GetCarbonScoreReport.Query(period: lastQuarter)`.

**Resultado Esperado:**
- `CarbonScore`, `CO2Equivalent`, recomendações de regiões mais verdes.

**Critério de Aceite:** HTTP 200 em ambas.

---

### TC-PA-023 — Exportação FOCUS (FinOps Open Cost and Usage Specification)

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | GetFocusExport |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Passos:**
1. `GetFocusExport.Query(period: lastMonth)`.

**Resultado Esperado:**
- CSV compatível com especificação FOCUS v1.0.

**Critério de Aceite:** HTTP 200 `Content-Type: text/csv`.

---

### TC-PA-024 — Previsão de orçamento

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | ForecastBudget |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** 6 meses de dados históricos de custo.

**Passos:**
1. `ForecastBudget.Command(serviceId, forecastPeriods: 3)`.

**Resultado Esperado:**
- Previsão para próximos 3 meses com intervalo de confiança.

**Critério de Aceite:** HTTP 200; `forecast.length == 3`.

---

### TC-PA-025 — Previsão de capacidade

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | GetCapacityForecast |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** Séries históricas de uso de recursos.

**Passos:**
1. `GetCapacityForecast.Query(serviceId, metric: "cpu", forecastDays: 30)`.

**Resultado Esperado:**
- Projeção de uso de CPU por 30 dias; alerta se ultrapassar threshold.

**Critério de Aceite:** HTTP 200.

---

### TC-PA-026 — Relatório de rightsizing

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | GetRightsizingReport |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** Instâncias com capacidade provisionada 3× acima do uso.

**Passos:**
1. `GetRightsizingReport.Query(threshold: 0.3)`.

**Resultado Esperado:**
- Lista de instâncias candidatas ao downsizing; economia mensal estimada.

**Critério de Aceite:** HTTP 200.

---

### TC-PA-027 — Importação de batch de custos

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | ImportCostBatch |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Passos:**
1. `ImportCostBatch.Command(source: "AWS CUR", period: "2026-04", records: [...1000 items...])`.
2. Handler valida schema dos registros; persiste em lote.

**Resultado Esperado:**
- `ImportedCount = 1000`; `FailedCount = 0`.

**Critério de Aceite:** HTTP 202.

---

### TC-PA-028 — Batch com registros inválidos

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | ImportCostBatch |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Passos:**
1. Batch com 5 registros sem `ServiceId`.

**Resultado Esperado:**
- `FailedCount = 5`; detalhes de falha por linha.

**Critério de Aceite:** HTTP 207 (Multi-Status).

---

## Relatórios Executivos

### TC-PA-029 — Gerar briefing executivo

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | GenerateExecutiveBriefing |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** Dados de DORA, SLO, custo e incidentes disponíveis.

**Passos:**
1. `GenerateExecutiveBriefing.Command(period: lastMonth, audience: "CTO")`.
2. Handler agrega métricas-chave e gera narrativa via IA.

**Resultado Esperado:**
- Briefing com resumo executivo, tendências e recomendações.

**Critério de Aceite:** HTTP 200; `wordCount > 200`.

---

### TC-PA-030 — Publicar briefing executivo

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | PublishExecutiveBriefing |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** Briefing `B1` gerado.

**Passos:**
1. `PublishExecutiveBriefing.Command(B1.Id, distributionList: ["cto@empresa.com", "vp-eng@empresa.com"])`.

**Resultado Esperado:**
- `IsPublished = true`; emails disparados via Outbox.

**Critério de Aceite:** HTTP 200.

---

### TC-PA-031 — Visão geral executiva cross-tenant

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | GetCrossTenantMaturityReport |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** Acesso de super-admin da plataforma.

**Passos:**
1. `GetCrossTenantMaturityReport.Query(period: lastQuarter)`.

**Resultado Esperado:**
- Ranking de maturidade entre tenants; benchmark de indústria.

**Critério de Aceite:** HTTP 200 (restrito a super-admin; retorna 403 para tenant comum).

---

## Jornadas e Personas

### TC-PA-032 — Criar definição de jornada

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | CreateJourneyDefinition |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Passos:**
1. `CreateJourneyDefinition.Command(name: "Onboarding de Desenvolvedor", steps: [{event: "user.created"}, {event: "first_login"}, {event: "contract.viewed"}])`.

**Resultado Esperado:**
- Jornada criada; `Steps.Count == 3`.

**Critério de Aceite:** HTTP 201.

---

### TC-PA-033 — Marcos de valor

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | GetValueMilestones |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:** Jornada com 3 marcos definidos.

**Passos:**
1. `GetValueMilestones.Query(journeyId, userId)`.

**Resultado Esperado:**
- `CompletedMilestones = 2`; `NextMilestone` com ETA.

**Critério de Aceite:** HTTP 200.

---

### TC-PA-034 — Página inicial de persona

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | GetPersonaHome |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** Usuário com persona `SRE Engineer` configurada.

**Passos:**
1. `GetPersonaHome.Query(userId)`.

**Resultado Esperado:**
- Widgets priorizados para SRE: alertas ativos, SLOs em risco, deployments recentes.

**Critério de Aceite:** HTTP 200.

---

### TC-PA-035 — Atualizar preferência de persona

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | UpdatePreference |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Passos:**
1. `UpdatePreference.Command(userId, key: "defaultDashboard", value: D1.Id)`.

**Resultado Esperado:**
- Preferência salva; `GetPersonaHome` reflete novo dashboard padrão.

**Critério de Aceite:** HTTP 200.

---

## Benchmarking

### TC-PA-036 — Submeter snapshot de benchmark

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | SubmitBenchmarkSnapshot |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Pré-condições:** Tenant consentiu com `RecordBenchmarkConsent`.

**Passos:**
1. `SubmitBenchmarkSnapshot.Command(metrics: { deployFrequency: 5.2, mttr: 45, changeFailureRate: 0.08 })`.

**Resultado Esperado:**
- Snapshot anonimizado salvo para comparação de indústria.

**Critério de Aceite:** HTTP 202.

---

### TC-PA-037 — Benchmark cross-ranked

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | GetCrossRankedBenchmark |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:** Pool com 100 tenants participantes.

**Passos:**
1. `GetCrossRankedBenchmark.Query(metric: "deployFrequency", industrySegment: "fintech")`.

**Resultado Esperado:**
- Percentil do tenant atual; mediana da indústria; P75 e P90.

**Critério de Aceite:** HTTP 200.

---

### TC-PA-038 — Submissão sem consentimento

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | SubmitBenchmarkSnapshot |
| **Tipo** | Segurança |
| **Prioridade** | Alta |

**Pré-condições:** Tenant **sem** consentimento registrado.

**Passos:**
1. `SubmitBenchmarkSnapshot.Command(...)`.

**Resultado Esperado:**
- `ErrorType = Business`; mensagem: "consentimento de benchmark não registrado".

**Critério de Aceite:** HTTP 422.

---

### TC-PA-039 — Relatório showback por serviço

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | GetShowbackReport |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** Custos alocados por serviço.

**Passos:**
1. `GetShowbackReport.Query(period: lastMonth)`.

**Resultado Esperado:**
- Custo por serviço; percentual do total; tendência vs mês anterior.

**Critério de Aceite:** HTTP 200.

---

### TC-PA-040 — Acesso sem autenticação

| Campo | Valor |
|-------|-------|
| **Módulo** | ProductAnalytics |
| **Feature** | GetFinOpsSummary |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |

**Passos:**
1. Chamar endpoint sem Bearer token.

**Resultado Esperado:**
- HTTP 401.

**Critério de Aceite:** Nenhum dado retornado.

---
