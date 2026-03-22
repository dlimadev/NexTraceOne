# Drift Detection Pipeline

## Job

**Classe:** `NexTraceOne.BackgroundWorkers.Jobs.DriftDetectionJob`

**Localização:** `src/platform/NexTraceOne.BackgroundWorkers/Jobs/DriftDetectionJob.cs`

## Scheduling

Controlado por `DriftDetectionOptions` (`BackgroundWorkers:DriftDetection`):

```json
{
  "BackgroundWorkers": {
    "DriftDetection": {
      "Enabled": true,
      "IntervalBetweenCycles": "00:05:00",
      "AnalysisWindow": "01:00:00",
      "DriftTolerancePercent": 15.0,
      "MaxFindingsPerCycle": 100
    }
  }
}
```

## Inputs

1. `IGetServicesWithRecentSnapshotsAsync(since: UtcNow - AnalysisWindow)` → lista pares `(serviceName, environment)` com snapshots recentes
2. Para cada par: `DetectRuntimeDrift.Command(serviceName, environment, tolerancePercent)`

## Outputs

- `DriftFinding` entidades persistidas em `oi_drift_findings`
- Logs estruturados com `ServiceName`, `Environment`, `FindingsCount`, `Severity`
- Health check actualizado via `WorkerJobHealthRegistry`

## Persistência

- Findings persistidos por `DetectRuntimeDrift.Handler` via `IDriftFindingRepository`
- Consultáveis via `GetDriftFindings` com filtro `UnacknowledgedOnly`
- Tabela: `oi_drift_findings`

## Observabilidade do Job

| Sinal | Descrição |
|---|---|
| `DriftDetectionJob cycle started` | Início de ciclo |
| `drift detected: {count} findings` | Findings detectados por serviço |
| `DriftDetectionJob cycle complete` | Fim de ciclo com estatísticas |
| `Drift detection cycle failed` | Falha de ciclo (operação continua) |
| Health check `drift-detection-job` | Última execução, falha, status |

## Troubleshooting

### Job não executa
1. Verificar `BackgroundWorkers:DriftDetection:Enabled = true`
2. Verificar health check `/health` → `drift-detection-job`
3. Ver logs `{application="NexTraceOne"} |= "DriftDetectionJob"`

### Sem findings gerados
1. Verificar se existem baselines (`oi_runtime_baselines`) para os serviços
2. Verificar se existem snapshots recentes (`oi_runtime_snapshots`)
3. Verificar tolerância configurada (`DriftTolerancePercent`)

### Findings em excesso
1. Rever `DriftTolerancePercent` (default: 15%)
2. Rever `MaxFindingsPerCycle`
3. Verificar se baselines estão actualizadas
