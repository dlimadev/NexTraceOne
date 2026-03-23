# Phase 6 — Observability Report

> **Nota:** Este documento é um relatório histórico. A stack de observabilidade foi migrada de Tempo/Loki/Grafana para provider configurável (ClickHouse ou Elastic). Ver `docs/observability/` para a documentação atual.

## 1. Resumo Executivo

### Estado Inicial
A base técnica de observabilidade existia mas era estrutural:
- `RuntimeIntelligence` com 6 handlers mas sem job de detecção automática
- `EnvironmentResolutionMiddleware` funcionava mas o atributo `environment` não estava nos spans OTel
- Serilog configurado mas sem sink Loki
- Grafana na stack mas sem dashboards pré-configurados
- `AiAnalysisPage` sem comparação visual de ambientes

### O Que Mudou

1. **`DriftDetectionJob`** — detecção automática agendada de drift
2. **`EnvironmentComparisonPage`** — tela dedicada de comparação de ambientes/releases
3. **Grafana dashboards** — 3 dashboards versionados no repositório
4. **Loki sink** — Serilog configurado para enviar logs ao Loki
5. **Atributo `environment`** — propagado em spans OTel
6. **Testes** — 13 novos testes (8 backend + 5 frontend)
7. **Bug fixes** — 3 bugs pre-existentes corrigidos (duplicata ListAutomationWorkflows, enum AutomationOutcome, AutomationFeatureTests)

## 2. Telemetria por Ambiente

### Como `environment` foi propagado

**OTel Spans:**
- `AddEnvironmentVariable("ASPNETCORE_ENVIRONMENT")` no `ResourceBuilder`
- Disponível em todos os spans como `service.environment`

**Logs (Serilog/Loki):**
- `Enrich.WithEnvironmentName()` já presente
- Label `environment` adicionada ao sink Loki

**Snapshots e Findings:**
- `RuntimeSnapshot.Create(serviceName, environment, ...)` — campo de primeira classe
- `DriftFinding.Detect(serviceName, environment, ...)` — campo de primeira classe

### Impacto

- Traces OTel filtrável por ambiente no Grafana/Tempo
- Logs Loki com label `environment` para queries por ambiente
- Drift findings rastreáveis por ambiente

## 3. Drift Detection

### Job
- `DriftDetectionJob` em `src/platform/NexTraceOne.BackgroundWorkers/Jobs/`
- Herda de `BackgroundService`, scheduling via `IOptions<DriftDetectionOptions>`
- Frequência default: 5 minutos

### Scheduling
```json
{
  "BackgroundWorkers:DriftDetection:Enabled": true,
  "BackgroundWorkers:DriftDetection:IntervalBetweenCycles": "00:05:00"
}
```

### Persistência
- `DetectRuntimeDrift.Handler` persiste via `IDriftFindingRepository`
- Tabela `oi_drift_findings`
- Consultável via `GetDriftFindings` com filtro `UnacknowledgedOnly`

### Troubleshooting
- Logs: `{application="NexTraceOne"} |= "DriftDetectionJob"`
- Health: `/health` → `drift-detection-job`
- Grafana: dashboard Platform Health → "DriftDetectionJob Logs"

## 4. Comparação Visual

### Tela Final
- Rota: `/operations/runtime-comparison`
- Componente: `EnvironmentComparisonPage`
- Sidebar: "Environment Comparison" na secção Operations

### Endpoints Consumidos
1. `GET /api/v1/runtime/compare` — `CompareReleaseRuntime`
2. `GET /api/v1/runtime/drift` — `GetDriftFindings`
3. `GET /api/v1/runtime/observability` — `GetObservabilityScore`
4. `GET /api/v1/runtime/timeline` — `GetReleaseHealthTimeline`

### Drill-down
- Score de observabilidade com nota A-F e breakdown por dimensão
- Tabela de métricas antes/depois com delta percentual e ícones de tendência
- Cards de drift findings com severidade, desvio esperado/actual
- Timeline de saúde por release com latência, erro e número de snapshots

### Contexto de Release
- `releaseName` exibido na timeline de saúde
- `releaseId` disponível nos drift findings
- Períodos before/after permitem correlacionar com qualquer deploy

## 5. Dashboards e Logging

### Dashboards Criados
| Dashboard | UID |
|---|---|
| Runtime & Environment Comparison | `nextraceone-runtime-comparison` |
| Platform Health | `nextraceone-platform-health` |
| Business Observability | `nextraceone-business-observability` |

### Provisioning
- `build/observability/grafana/provisioning/` — datasources e dashboards
- Activado via volumes no `docker-compose.telemetry.yaml`
- Reproduzível: `docker-compose up -d` carrega tudo automaticamente

### Loki Sink
- Activado por `Observability:Serilog:Loki:Endpoint`
- Labels: `application`, `environment`
- Correlação com traces via `TraceId` no JSON estruturado

## 6. Ingestion API e Readiness

### Papel Validado
- Pipeline separado para ingestão de snapshots externos
- Não compete com a API principal
- Alimenta `RuntimeSnapshot` que sustenta todos os handlers de comparação

### Health Checks
- `/health` — todos os BackgroundWorkers com health checks
- `drift-detection-job` registado via `WorkerJobHealthRegistry`
- Health check de DB contexts em ambos ApiHost e BackgroundWorkers

## 7. Testes

### Backend (NexTraceOne.OperationalIntelligence.Tests)
| Teste | Comportamento Validado |
|---|---|
| DetectRuntimeDrift_WhenDriftExists | Findings gerados quando desvio > tolerância |
| DetectRuntimeDrift_WhenNoBaseline | Erro quando baseline ausente |
| DetectRuntimeDrift_WhenWithinTolerance | Sem findings quando dentro da tolerância |
| GetDriftFindings_WhenFindingsExist | Lista paginada de findings |
| GetDriftFindings_UnacknowledgedOnly | Filtro de não reconhecidos funciona |
| CompareReleaseRuntime_WithTwoPeriodsOfData | Deltas calculados correctamente |
| CompareReleaseRuntime_WhenNoPeriodData | Zero métricas sem dados |

**Total OI: 295 testes passam (0 falhas)**

### Frontend (EnvironmentComparisonPage.test.tsx)
| Teste | Comportamento Validado |
|---|---|
| renders the page with comparison form | Estado inicial com formulário |
| shows results after form submission | API chamada ao submeter |
| does not call API when service name is empty | Validação client-side |
| shows drift findings severity badges | Findings apresentados |
| shows no drift findings message | Estado vazio honesto |

**Total Frontend: 5 testes passam**

### Bug Fixes (pre-existentes)
1. `ListAutomationWorkflows.cs` — duplicata de classe removida
2. `AutomationValidationRecordConfiguration.cs` — `AutomationOutcome.Success` → `AutomationOutcome.Successful`
3. `AutomationFeatureTests.cs` — handlers instanciados com mocks NSubstitute

## 8. Limitações Remanescentes

1. **Rate limiting na Ingestion API** — não implementado nesta fase
2. **Autenticação Ingestion API** — revisar para produção
3. **Dashboards Prometheus** — queries baseadas em métricas OTel padrão; requerem OTel Collector activo para validação em produção
4. **ChangeGovernance integration** — a correlação com blast radius e promotion gates foi documentada mas não integrada directamente nesta fase (requer surface contract entre módulos)
5. **DriftDetectionJob em multi-tenant** — processa serviços sem isolamento por tenant (adequado para uso single-tenant; revisar para SaaS)

## 9. Próximo Passo Recomendado

**Fase 7 — Production Readiness & Change Confidence**:
- Integração do `EnvironmentComparisonPage` com ChangeGovernance (blast radius, promotion gates)
- `DriftDetectionJob` com isolamento multi-tenant
- Rate limiting e autenticação da Ingestion API
- `GetObservabilityScore` com regras configuráveis de scoring
- Alertas automáticos via Grafana baseados em drift findings críticos
