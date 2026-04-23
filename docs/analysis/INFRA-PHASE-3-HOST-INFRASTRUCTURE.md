# Fase 3 — Camada de Infraestrutura de Hosts

> **Duração estimada:** 2–3 semanas
> **Pré-requisitos:** Fase 1 concluída (PgBouncer configurado)
> **O que já existe:** `EdgeType.DeployedTo` ✅, OTEL Collector ✅, `NodeHealthRecord` ✅

---

## Contexto

Em ambientes on-premise com arquitectura distribuída, múltiplas máquinas alojam os
serviços. Sem visibilidade de host, o NexTraceOne consegue dizer *"o serviço está
degradado"* mas não consegue responder *"é código ou é infra?"*.

Esta fase adiciona uma camada de hosts ao modelo de dados e ao grafo de topologia,
permitindo correlação directa entre degradação de serviço e pressão de infraestrutura.

---

## 3.1 Domínio — Novas Entidades

### Módulo: `HostInfrastructure` (novo bounded context)

Localização: `src/modules/hostinfrastructure/`

Estrutura de pastas (seguindo o padrão existente):

```
NexTraceOne.HostInfrastructure.Domain/
  Entities/
    HostAsset.cs
    ServiceDeployment.cs
    HostHealthRecord.cs
  Enums/
    HostStatus.cs
    OperatingSystem.cs
    InfrastructureProvider.cs

NexTraceOne.HostInfrastructure.Application/
  Features/
    RegisterHost/
    UpdateHostMetrics/
    GetHostDashboard/
    GetHostsByEnvironment/
    GetServiceDeployments/
    GetHostHealthTimeline/
    CorrelateServiceWithHost/

NexTraceOne.HostInfrastructure.Infrastructure/
  Persistence/
    HostInfrastructureDbContext.cs
    Migrations/
  ClickHouse/
    HostMetricsClickHouseWriter.cs

NexTraceOne.HostInfrastructure.API/
  Endpoints/
    HostEndpointModule.cs
```

### Entidade `HostAsset`

```csharp
public class HostAsset : AggregateRoot<HostAssetId>
{
    public TenantId TenantId { get; private set; }
    public string Hostname { get; private set; }        // ex: "server-prod-01"
    public string IpAddress { get; private set; }       // ex: "10.0.1.42"
    public OperatingSystem Os { get; private set; }     // Linux, Windows, etc.
    public string OsVersion { get; private set; }       // ex: "Ubuntu 22.04"
    public string KernelVersion { get; private set; }   // ex: "5.15.0"
    public InfrastructureProvider Provider { get; private set; } // OnPrem, AWS, Azure, GCP
    public string Environment { get; private set; }     // prod, staging, dev
    public string Datacenter { get; private set; }      // ex: "dc-lisboa-01"
    public string Rack { get; private set; }            // ex: "rack-B7" (opcional)
    public int CpuCores { get; private set; }
    public long MemoryMb { get; private set; }
    public long DiskGb { get; private set; }
    public HostStatus Status { get; private set; }      // Online, Offline, Degraded
    public DateTimeOffset RegisteredAt { get; private set; }
    public DateTimeOffset LastSeenAt { get; private set; }
    public IReadOnlyList<ServiceDeployment> Deployments { get; }
}
```

### Entidade `ServiceDeployment`

Liga um `ServiceAsset` (catálogo) a um `HostAsset`:

```csharp
public class ServiceDeployment : Entity<ServiceDeploymentId>
{
    public HostAssetId HostAssetId { get; private set; }
    public ServiceAssetId ServiceAssetId { get; private set; }  // FK para Catalog
    public string ServiceName { get; private set; }
    public string ProcessName { get; private set; }   // ex: "dotnet", "java", "node"
    public int Port { get; private set; }
    public string Environment { get; private set; }
    public DateTimeOffset DeployedAt { get; private set; }
    public DateTimeOffset? UndeployedAt { get; private set; }
    public bool IsActive => UndeployedAt is null;
}
```

### Entidade `HostHealthRecord`

Análogo ao `NodeHealthRecord` existente no Catalog.Graph:

```csharp
public class HostHealthRecord : Entity<HostHealthRecordId>
{
    public HostAssetId HostAssetId { get; private set; }
    public HostStatus Status { get; private set; }       // Healthy, AtRisk, Critical
    public float Score { get; private set; }             // 0.0 - 1.0
    public string FactorsJson { get; private set; }      // breakdown dos factores
    public DateTimeOffset CalculatedAt { get; private set; }
    // Factores: CpuScore, MemoryScore, DiskScore, UptimeScore, NetworkScore
}
```

---

## 3.2 DbContext e Migrations

### `HostInfrastructureDbContext`

```csharp
public class HostInfrastructureDbContext : NexTraceDbContextBase
{
    public DbSet<HostAsset> HostAssets => Set<HostAsset>();
    public DbSet<ServiceDeployment> ServiceDeployments => Set<ServiceDeployment>();
    public DbSet<HostHealthRecord> HostHealthRecords => Set<HostHealthRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("hst");  // prefixo: hst_
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HostInfrastructureDbContext).Assembly);
    }
}
```

### Tabelas criadas (prefixo `hst_`)

| Tabela | Descrição |
|--------|-----------|
| `hst_host_assets` | Registo de hosts |
| `hst_service_deployments` | Ligação serviço ↔ host |
| `hst_host_health_records` | Saúde calculada por host |

### Indexes críticos

```sql
-- Busca de hosts por ambiente/tenant
CREATE INDEX ON hst_host_assets (tenant_id, environment, status);

-- Busca de deployments activos por serviço
CREATE INDEX ON hst_service_deployments (service_asset_id, is_active, environment);

-- Busca de deployments por host
CREATE INDEX ON hst_service_deployments (host_asset_id, is_active);
```

---

## 3.3 Ingestão de Métricas — OTEL Host Metrics Receiver

### Activar `hostmetricsreceiver` no OTEL Collector

**`build/otel-collector/otel-collector.yaml`** — adicionar receiver:

```yaml
receivers:
  hostmetrics:
    collection_interval: 30s    # snapshot a cada 30 segundos
    scrapers:
      cpu:
        metrics:
          system.cpu.utilization:
            enabled: true
      memory:
        metrics:
          system.memory.utilization:
            enabled: true
      disk:
        metrics:
          system.disk.io:
            enabled: true
      filesystem:
        metrics:
          system.filesystem.utilization:
            enabled: true
      network:
        metrics:
          system.network.io:
            enabled: true
      load:
        metrics:
          system.cpu.load_average.1m:
            enabled: true

processors:
  # Adicionar hostname como atributo de recurso
  resourcedetection:
    detectors: [env, system]
    system:
      hostname_sources: [os]
```

### Endpoint de ingestão de métricas de host

Adicionar ao `Ingestion.Api`:

```
POST /api/v1/host-metrics/ingest
Authorization: Bearer {token}

Body:
{
  "hostId": "uuid",
  "hostname": "server-prod-01",
  "environment": "prod",
  "capturedAt": "2026-04-23T10:00:00Z",
  "cpuUsagePercent": 45.2,
  "memoryUsedPercent": 67.8,
  "diskUsedPercent": 42.1,
  "networkInMbps": 125.4,
  "networkOutMbps": 89.2,
  "uptimeSeconds": 864000
}
```

O endpoint persiste no **ClickHouse** (`host_metrics_snapshots`) e actualiza o
`LastSeenAt` do `HostAsset` no **PostgreSQL**.

---

## 3.4 Endpoints de API

```
# Gestão de hosts
POST   /api/v1/hosts                           → RegisterHost
GET    /api/v1/hosts?environment=prod          → GetHostsByEnvironment
GET    /api/v1/hosts/{hostId}                  → GetHostDetail
DELETE /api/v1/hosts/{hostId}                  → DecommissionHost

# Deployments (serviços num host)
POST   /api/v1/hosts/{hostId}/deployments      → RegisterServiceDeployment
DELETE /api/v1/hosts/{hostId}/deployments/{id} → UndeployService
GET    /api/v1/hosts/{hostId}/deployments      → GetHostDeployments

# Métricas e saúde
GET    /api/v1/hosts/{hostId}/health           → GetHostHealth (latest HealthRecord)
GET    /api/v1/hosts/{hostId}/metrics?from=&to= → GetHostMetricsTimeline (ClickHouse)
POST   /api/v1/host-metrics/ingest             → IngestHostMetrics (Ingestion.Api)

# Dashboard de infra
GET    /api/v1/hosts/dashboard                 → GetHostDashboard
  # Retorna: TotalHosts, OnlineHosts, DegradedHosts, CriticalHosts,
  #          TopCpuHosts[], TopMemoryHosts[], HostsWithCriticalServices[]

# Correlação serviço ↔ host
GET    /api/v1/hosts/service/{serviceId}       → GetHostsForService
  # "Em que hosts está este serviço em execução?"
GET    /api/v1/hosts/{hostId}/impact           → GetHostImpact
  # "Se este host cair, que serviços são afectados?"
```

---

## 3.5 Integração no Grafo de Topologia

### Novos tipos de nó no `DependencyGraph.tsx`

```typescript
// Adicionar ao ECharts graph
const hostNode = {
  id: host.id,
  name: host.hostname,
  category: 'host',          // nova categoria
  symbolSize: 44,
  symbol: 'rect',            // quadrado para distinguir de serviços
  itemStyle: {
    color: hostStatusColor(host.status),  // cinzento=online, laranja=degraded, vermelho=critical
    borderRadius: 4
  },
  tooltip: {
    content: `${host.hostname} | ${host.ipAddress} | CPU: ${host.cpuPct}% | MEM: ${host.memPct}%`
  }
};
```

### Edge `DeployedTo` (já existe no `EdgeType`)

```typescript
// Edge: ServiceAsset → HostAsset (via EdgeType.DeployedTo)
const deployedToEdge = {
  source: serviceId,
  target: hostId,
  lineStyle: {
    type: 'dashed',
    color: '#94a3b8',
    width: 1
  },
  label: { show: false }
};
```

### Novo overlay: `Infrastructure`

Adicionar ao enum `OverlayMode` existente:

```csharp
public enum OverlayMode
{
    None,
    Health,
    ChangeVelocity,
    Risk,
    Cost,
    ObservabilityDebt,
    Infrastructure   // NOVO — mostra saúde de hosts e pressão de infra
}
```

No overlay `Infrastructure`, os nós de serviço são coloridos com base na saúde
do host onde correm (não na saúde do serviço em si), permitindo distinguir:
- Serviço degradado por causa de código
- Serviço degradado por causa de host

---

## 3.6 Correlação Automática Serviço ↔ Host

Quando um `ServiceMetricsSnapshot` detecta latência elevada, o sistema correlaciona
automaticamente com o `HostMetricsSnapshot` do mesmo host no mesmo período:

```csharp
// src/modules/hostinfrastructure/Application/Features/CorrelateServiceWithHost/
public class CorrelateServiceDegradationHandler : IQueryHandler<CorrelateServiceDegradationQuery, CorrelationResult>
{
    public async Task<Result<CorrelationResult>> Handle(CorrelateServiceDegradationQuery query, CancellationToken ct)
    {
        // 1. Encontrar host(s) onde o serviço corre
        var deployments = await _deploymentRepo.GetActiveByServiceAsync(query.ServiceId, ct);

        // 2. Para cada host, verificar métricas no período
        var correlations = new List<HostCorrelation>();
        foreach (var deployment in deployments)
        {
            var metrics = await _clickHouseReader.GetHostMetricsAsync(
                deployment.HostAssetId, query.From, query.To, ct);

            correlations.Add(new HostCorrelation
            {
                HostId = deployment.HostAssetId,
                Hostname = deployment.HostAsset.Hostname,
                MaxCpuPct = metrics.Max(m => m.CpuUsagePercent),
                MaxMemoryPct = metrics.Max(m => m.MemoryUsedPercent),
                MaxDiskPct = metrics.Max(m => m.DiskUsedPercent),
                ProbableInfraCause = metrics.Any(m => m.CpuUsagePercent > 85 || m.MemoryUsedPercent > 90)
            });
        }

        return new CorrelationResult
        {
            ServiceId = query.ServiceId,
            HostCorrelations = correlations,
            InfrastructureLikelyCause = correlations.Any(c => c.ProbableInfraCause)
        };
    }
}
```

---

## Checklist de Entrega — Fase 3

- [ ] Módulo `HostInfrastructure` criado com estrutura de pastas correcta
- [ ] `HostAsset`, `ServiceDeployment`, `HostHealthRecord` implementados
- [ ] `HostInfrastructureDbContext` com prefixo `hst_`
- [ ] Migration inicial criada e testada
- [ ] Indexes de performance criados
- [ ] `HostMetricsClickHouseWriter` implementado (tabela `host_metrics_snapshots`)
- [ ] OTEL hostmetricsreceiver activado no `otel-collector.yaml`
- [ ] Endpoint `POST /api/v1/host-metrics/ingest` no Ingestion.Api
- [ ] Endpoints de gestão de hosts no ApiHost (7 endpoints)
- [ ] `GetHostDashboard` com métricas agregadas
- [ ] `CorrelateServiceDegradationHandler` implementado
- [ ] `OverlayMode.Infrastructure` adicionado ao enum
- [ ] Nó de host no `DependencyGraph.tsx` (rectângulo cinzento)
- [ ] Edge `DeployedTo` renderizado no grafo
- [ ] Overlay `Infrastructure` no frontend com coloração por saúde de host
- [ ] Testes unitários para `CorrelateServiceDegradation` (mínimo 5 casos)
- [ ] Permissões: `host-infrastructure:read` e `host-infrastructure:manage`
- [ ] i18n para strings de UI do módulo (4 locales)
