# Wave 3 — Platform Health and Alerting

## Platform Health

### Antes (GAP-016)
`GetPlatformHealth` retornava subsistemas com status hardcoded `Healthy`:
- API: "All API endpoints responding normally" → `Healthy`
- Database: "PostgreSQL primary and replicas healthy" → `Healthy`
- BackgroundJobs: "All scheduled jobs executing on time" → `Healthy`
- Ingestion: "Ingestion pipeline processing within SLA" → `Healthy`
- AI: "AI model registry and inference endpoints operational" → `Healthy`

### Depois (Resolvido)
`GetPlatformHealth` consulta **health checks reais** via `IPlatformHealthProvider`:

| Subsistema | Fonte Real | Status Possíveis |
|------------|-----------|-----------------|
| API | Se o handler executa, está funcional | `Healthy` |
| Database | 13 checks de conectividade de BD | `Healthy`, `Degraded`, `Unhealthy` |
| AI | ai-providers + 3 BD de IA | `Healthy`, `Degraded`, `Unhealthy` |
| BackgroundJobs | Sem health check dedicado | `Unknown` |
| Ingestion | Sem health check dedicado | `Unknown` |

### Fontes de Status

**Database** — health checks reais registados no ASP.NET Core:
- identity-db, catalog-graph-db, contracts-db, change-intelligence-db
- runtime-intelligence-db, governance-db, ruleset-governance-db
- workflow-db, promotion-db, developer-portal-db, incident-db
- cost-intelligence-db, audit-db

**AI** — health checks reais:
- ai-providers (verifica disponibilidade de providers configurados)
- ai-governance-db, external-ai-db, ai-orchestration-db

**BackgroundJobs e Ingestion** — reportados como `Unknown` (honesto) em vez de `Healthy` (fake).
Estes subsistemas precisam de health checks dedicados para serem reportados com precisão.

### Overall Status Computation
- Se algum subsistema `Unhealthy` → Overall `Unhealthy`
- Se algum subsistema `Degraded` ou `Unknown` → Overall `Degraded`
- Se todos `Healthy` → Overall `Healthy`
- Se nenhum subsistema → Overall `Unknown`

---

## Alerting Integration

### Antes (GAP-022)
- `AlertGateway` existia com canais Webhook e Email
- Alertas eram despachados mas **não tinham efeito operacional**
- Nenhuma ligação ao fluxo de incidentes

### Depois (Resolvido)
- `AlertGateway` agora invoca `IOperationalAlertHandler` após dispatch
- `IncidentAlertHandler` cria incidentes automáticos para alertas Error/Critical
- Alertas Info/Warning são registados em log mas não geram incidentes

### Fluxo de Integração

```
Alerta Operacional
    │
    ├─→ AlertGateway.DispatchAsync()
    │       ├─→ WebhookAlertChannel (se configurado)
    │       ├─→ EmailAlertChannel (se configurado)
    │       └─→ IOperationalAlertHandler.HandleAlertAsync()
    │               └─→ IncidentAlertHandler
    │                       ├─→ Severity >= Error? → CreateIncident()
    │                       └─→ Severity < Error? → Log only
    │
    └─→ AlertDispatchResult (sucesso/falha por canal)
```

### Mapeamento de Severidade

| Alert Severity | Incident Severity |
|---------------|------------------|
| Critical | Critical |
| Error | Major |
| Warning | (não cria incidente) |
| Info | (não cria incidente) |

### Mapeamento de Tipo de Incidente

| Alert Source | Incident Type |
|-------------|--------------|
| health, health-check, platform-health | AvailabilityIssue |
| worker, background-jobs, scheduler | BackgroundProcessingIssue |
| ingestion, pipeline | ServiceDegradation |
| ai, ai-provider | DependencyFailure |
| drift, anomaly, change-intelligence | OperationalRegression |
| (default) | ServiceDegradation |

### Incident Workflow Touchpoints
- Incidente criado com título `[Alert] {alert.Title}`
- Descrição inclui contexto completo do alerta
- CorrelationId do alerta propagado
- Severidade e tipo mapeados automaticamente
- Falha na criação de incidente é logada mas não bloqueia dispatch
