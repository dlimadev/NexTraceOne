# Ingestion API — Papel e Fluxo

## Papel no Pipeline de Observabilidade

A `NexTraceOne.Ingestion.Api` é um serviço separado da API principal, responsável por:
- Receber telemetria e snapshots de agentes externos e serviços instrumentados
- Processar e persistir snapshots de runtime sem competir com a API principal
- Servir como ponto de entrada para dados de observabilidade vindos de fora

## Separação de Responsabilidades

```
API Principal (NexTraceOne.ApiHost)
  ├── Governança, contratos, releases, incidentes
  └── Leitura de dados de observabilidade (GetDriftFindings, CompareReleaseRuntime, etc.)

Ingestion API (NexTraceOne.Ingestion.Api)
  ├── POST /api/v1/ingest/snapshot — ingere snapshot de runtime
  ├── POST /api/v1/ingest/metrics — ingere métricas de um serviço
  └── Escreve em RuntimeIntelligenceDatabase
```

## Endpoints Principais

| Endpoint | Método | Descrição |
|---|---|---|
| `/api/v1/ingest/snapshot` | POST | Ingere um snapshot de runtime de um serviço |
| `/api/v1/ingest/metrics` | POST | Ingere métricas agregadas de um serviço |
| `/health` | GET | Health check da Ingestion API |

## Integração com RuntimeIntelligence

Os dados ingeridos pela Ingestion API alimentam directamente:
1. `RuntimeSnapshot` → base para `CompareReleaseRuntime` e `DetectRuntimeDrift`
2. `GetServicesWithRecentSnapshotsAsync` → base para o `DriftDetectionJob`
3. `GetObservabilityScore` → calcula score baseado nos snapshots mais recentes

## Configuração

A Ingestion API usa a mesma `RuntimeIntelligenceDatabase` connection string:

```json
{
  "ConnectionStrings": {
    "RuntimeIntelligenceDatabase": "..."
  }
}
```

## Gaps Remanescentes

- Autenticação/autorização da Ingestion API deve ser revista para produção
- Rate limiting não implementado nesta fase
- Validação de payload deve ser reforçada para uso externo
