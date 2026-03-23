# Phase 6 — Observabilidade Aplicada ao Diferencial do Produto

> **Nota:** Este documento é um relatório histórico. A stack de observabilidade foi migrada de Tempo/Loki/Grafana para provider configurável (ClickHouse ou Elastic). Ver `docs/observability/` para a documentação atual.

## Sumário Executivo

Esta fase transforma a base de observabilidade já existente no NexTraceOne em uma capability real
de produto enterprise, tornando a observabilidade o diferencial central de decisão:
**analisar comportamento em ambientes, comparar com produção, detectar drift e entregar
evidência operacional para decisão de release.**

## Escopo Executado

### 1. Environment-Aware Telemetry (Bloco B)

- Propagação de `environment` como atributo OTel via `ResourceBuilder.AddEnvironmentVariable()`
- `EnvironmentResolutionMiddleware` já existente foi complementado pelo enricher no `AddBuildingBlocksObservability()`
- Atributo `environment` disponível em todos os spans, logs estruturados e snapshots

### 2. DriftDetectionJob (Bloco C)

- `DriftDetectionJob` implementado em `src/platform/NexTraceOne.BackgroundWorkers/Jobs/`
- Scheduling configurável via `DriftDetectionOptions` (`BackgroundWorkers:DriftDetection`)
- Conectado a `DetectRuntimeDrift`, `GetServicesWithRecentSnapshotsAsync` e `IDriftFindingRepository`
- Health check registado via `WorkerJobHealthRegistry`
- Findings persistidos e consultáveis via `GetDriftFindings`

### 3. Comparação Visual de Ambientes e Releases (Bloco D)

- Nova rota `/operations/runtime-comparison` → `EnvironmentComparisonPage`
- Consome endpoints reais: `CompareReleaseRuntime`, `GetDriftFindings`, `GetObservabilityScore`, `GetReleaseHealthTimeline`
- Interface em 4 secções: score de observabilidade, comparação de métricas, drift findings, timeline de releases
- i18n completo em 4 idiomas: en, pt-BR, pt-PT, es

### 4. Dashboards Grafana (Bloco F)

- 3 dashboards versionados em `build/observability/grafana/dashboards/`:
  - `runtime-environment-comparison.json`: drift findings, logs e traces de runtime
  - `platform-health.json`: API health, background workers, OTel collector
  - `business-observability.json`: snapshots, findings, releases, incidentes
- Provisioning automático via `build/observability/grafana/provisioning/`
- Datasources configurados: Tempo, Loki, Prometheus

### 5. Loki Sink (Bloco G)

- `SerilogConfiguration.cs` atualizado com sink Loki condicional
- Labels: `application`, `environment`
- Activado quando `Observability:Serilog:Loki:Endpoint` está configurado
- Configuração de desenvolvimento em `appsettings.Development.json`

### 6. Ingestion API (Bloco H)

- `NexTraceOne.Ingestion.Api` inspeccionada — possui endpoints de ingestão de snapshots e métricas
- Papel: pipeline separado para ingestão de telemetria externa sem impacto na API principal

### 7. Testes (Bloco J)

- 8 novos testes de backend em `RuntimeFeatureTests.cs`:
  - DetectRuntimeDrift (drift detectado, sem baseline, dentro de tolerância)
  - GetDriftFindings (com findings, only unacknowledged)
  - CompareReleaseRuntime (com dados, sem dados)
- 5 testes de frontend em `EnvironmentComparisonPage.test.tsx`
- Correcção de pre-existing bugs: `ListAutomationWorkflows.cs` (duplicata), `AutomationValidationRecordConfiguration.cs` (enum value), `AutomationFeatureTests.cs` (handlers sem mock)

## Endpoints Usados/Criados

### Endpoints de Runtime Intelligence (existentes)

| Endpoint | Método | Uso |
|---|---|---|
| `/api/v1/runtime/snapshot` | GET | `IngestRuntimeSnapshot` |
| `/api/v1/runtime/drift` | POST/GET | `DetectRuntimeDrift` / `GetDriftFindings` |
| `/api/v1/runtime/compare` | GET | `CompareReleaseRuntime` |
| `/api/v1/runtime/timeline` | GET | `GetReleaseHealthTimeline` |
| `/api/v1/runtime/observability` | GET | `GetObservabilityScore` |

## Gaps Eliminados

| Gap Original | Estado |
|---|---|
| Sem tela de comparação de ambientes | ✅ `EnvironmentComparisonPage` em `/operations/runtime-comparison` |
| Sem `DriftDetectionJob` | ✅ Job implementado com scheduling configurável |
| Sem dashboards Grafana | ✅ 3 dashboards versionados no repositório |
| Sem Loki sink no Serilog | ✅ Sink configurado com labels estruturadas |
| `environment` ausente dos spans | ✅ Propagado via `ResourceBuilder` |
| Ingestion API incógnita | ✅ Papel validado e documentado |
