# Fase 4 — Completar Topology Intelligence

> **Duração estimada:** 2–3 semanas
> **Pré-requisitos:** Fase 3 concluída (HostAsset disponível para overlay)
> **O que já existe:** `GraphSnapshot` ✅, `PropagateHealthStatus` ✅,
> `DetectCircularDependencies` ✅, `RunServiceDiscovery` ✅, `NodeHealthRecord` ✅

---

## Contexto

O backend de topologia está muito completo. O que falta é:

1. **UI de time-travel** sobre os `GraphSnapshot` existentes (slider temporal)
2. **Alertas de propagação em tempo real** via push (actualmente só on-demand)
3. **Discovery contínuo** em background (actualmente manual)
4. **Pipeline real de actualização** do `NodeHealthRecord` (actualmente calculado mas sem feed automático)

---

## 4.1 Time-Travel UI — Slider Temporal no Grafo

### O que existe

O backend já tem:
- `GraphSnapshot` com `NodesJson`/`EdgesJson` serializados
- `CreateGraphSnapshot` command
- `GetLatestTopologySnapshot` query
- `GetTemporalDiff` query (diferença entre dois snapshots)

### O que falta

A UI (`DependencyGraph.tsx`) não tem controlo temporal — mostra sempre o estado actual.

### Implementação no frontend

**Novo componente: `TopologyTimeTravel.tsx`**

```typescript
// src/frontend/src/features/catalog/components/TopologyTimeTravel.tsx

interface TimeSliderProps {
  snapshots: SnapshotMeta[];       // lista de snapshots disponíveis
  selectedIndex: number;
  onChange: (index: number) => void;
}

export const TopologyTimeTravel = ({ snapshots, selectedIndex, onChange }) => {
  return (
    <div className="flex items-center gap-3 p-3 bg-slate-800 rounded-lg">
      <Clock className="w-4 h-4 text-slate-400" />
      <input
        type="range"
        min={0}
        max={snapshots.length - 1}
        value={selectedIndex}
        onChange={e => onChange(Number(e.target.value))}
        className="flex-1 accent-blue-500"
      />
      <span className="text-xs text-slate-300 min-w-fit">
        {formatDateTime(snapshots[selectedIndex]?.capturedAt)}
      </span>
      {selectedIndex < snapshots.length - 1 && (
        <Badge variant="warning">Histórico</Badge>
      )}
      {selectedIndex === snapshots.length - 1 && (
        <Badge variant="success">Actual</Badge>
      )}
    </div>
  );
};
```

**Integração na `DependencyDashboardPage.tsx`:**

```typescript
// Estado de time-travel
const [snapshots, setSnapshots] = useState<SnapshotMeta[]>([]);
const [selectedSnapshot, setSelectedSnapshot] = useState<number>(0);
const [showDiff, setShowDiff] = useState(false);

// Quando muda o snapshot seleccionado
const handleSnapshotChange = async (index: number) => {
  setSelectedSnapshot(index);
  if (index < snapshots.length - 1) {
    // Carregar grafo histórico
    const historical = await api.getTopologySnapshot(snapshots[index].id);
    setGraphData(historical);
    // Mostrar diff em relação ao actual
    if (showDiff) {
      const diff = await api.getTemporalDiff(
        snapshots[index].id,
        snapshots[snapshots.length - 1].id
      );
      applyDiffOverlay(diff);  // nós adicionados=verde, removidos=vermelho
    }
  } else {
    // Carregar estado actual
    const current = await api.getCurrentGraph();
    setGraphData(current);
  }
};
```

**Overlay de diff no grafo:**

```typescript
// Nós adicionados desde o snapshot anterior → borda verde
// Nós removidos → borda vermelha tracejada (ghost node)
// Arestas adicionadas → linha verde
// Arestas removidas → linha vermelha tracejada

const applyDiffOverlay = (diff: TopologyDiff) => {
  const addedNodes = diff.addedNodes.map(n => ({
    ...n,
    itemStyle: { borderColor: '#22c55e', borderWidth: 3 }
  }));
  const removedNodes = diff.removedNodes.map(n => ({
    ...n,
    itemStyle: { borderColor: '#ef4444', borderWidth: 2, borderType: 'dashed', opacity: 0.5 }
  }));
  // ...
};
```

**Novo endpoint necessário:**

```
GET /api/v1/catalog/graph/snapshots
  → Lista de snapshots disponíveis (id, capturedAt, label, nodeCount, edgeCount)

GET /api/v1/catalog/graph/snapshots/{snapshotId}
  → NodesJson + EdgesJson de um snapshot específico
```

O segundo endpoint já existe implicitamente — apenas precisa de ser exposto.

---

## 4.2 Alertas de Propagação em Tempo Real — SignalR

### O que existe

`GetTopologyAwareAlerts` é uma query on-demand: o cliente tem de pedir
explicitamente. Não há push quando um serviço degrada.

### Solução: SignalR Hub

**Novo Hub: `TopologyAlertsHub`**

```csharp
// src/platform/NexTraceOne.ApiHost/Hubs/TopologyAlertsHub.cs

[Authorize]
public class TopologyAlertsHub : Hub
{
    public async Task SubscribeToService(string serviceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"service:{serviceId}");
    }

    public async Task SubscribeToEnvironment(string environment)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"env:{environment}");
    }

    public async Task UnsubscribeFromService(string serviceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"service:{serviceId}");
    }
}
```

**Registo na DI:**

```csharp
// Program.cs
builder.Services.AddSignalR();
app.MapHub<TopologyAlertsHub>("/hubs/topology-alerts");
```

**Publisher — disparado pelo pipeline de detecção de anomalias:**

```csharp
// src/modules/operationalintelligence/Application/Runtime/Services/TopologyAlertPublisher.cs

public class TopologyAlertPublisher : ITopologyAlertPublisher
{
    private readonly IHubContext<TopologyAlertsHub> _hubContext;

    public async Task PublishPropagationRiskAsync(PropagationRiskAlert alert, CancellationToken ct)
    {
        // Notificar subscritores do serviço afectado
        await _hubContext.Clients
            .Group($"service:{alert.DegradedServiceId}")
            .SendAsync("PropagationRisk", alert, ct);

        // Notificar subscritores do ambiente
        await _hubContext.Clients
            .Group($"env:{alert.Environment}")
            .SendAsync("PropagationRisk", alert, ct);
    }
}
```

**Consumer no frontend:**

```typescript
// src/frontend/src/features/catalog/hooks/useTopologyAlerts.ts

export const useTopologyAlerts = (serviceIds: string[]) => {
  const [alerts, setAlerts] = useState<PropagationAlert[]>([]);

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/topology-alerts', { accessTokenFactory: () => getToken() })
      .withAutomaticReconnect()
      .build();

    connection.on('PropagationRisk', (alert: PropagationAlert) => {
      setAlerts(prev => [alert, ...prev].slice(0, 50)); // últimos 50
      showToast(`⚠️ ${alert.degradedService} pode impactar ${alert.affectedCount} serviços`);
    });

    connection.start().then(() => {
      serviceIds.forEach(id => connection.invoke('SubscribeToService', id));
    });

    return () => { connection.stop(); };
  }, [serviceIds]);

  return { alerts };
};
```

---

## 4.3 Discovery Contínuo — Quartz Job em Background

### O que existe

`RunServiceDiscovery` é um command executado manualmente ou por trigger externo.
Não há polling contínuo automático.

### Solução: Job Quartz periódico

```csharp
// src/platform/NexTraceOne.BackgroundWorkers/Jobs/ContinuousServiceDiscoveryJob.cs

[DisallowConcurrentExecution]
public class ContinuousServiceDiscoveryJob : IJob
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenantResolver _tenantResolver;

    public async Task Execute(IJobExecutionContext context)
    {
        // Descoberta para todos os tenants activos
        var tenants = await _tenantResolver.GetActiveTenantsAsync();

        foreach (var tenant in tenants)
        {
            using var scope = _tenantResolver.SetTenant(tenant.Id);

            await _mediator.Send(new RunServiceDiscoveryCommand
            {
                Environment = "prod",
                From = DateTimeOffset.UtcNow.AddMinutes(-10), // janela de 10 min
                Until = DateTimeOffset.UtcNow
            });
        }
    }
}
```

**Registo no Quartz:**

```csharp
// Executar a cada 5 minutos
services.AddQuartz(q =>
{
    q.AddJobAndTrigger<ContinuousServiceDiscoveryJob>(
        "continuous-discovery",
        CronScheduleBuilder.CronSchedule("0 */5 * * * ?"));
});
```

**Configuração via feature flag** (já existe o sistema de config keys):

```
discovery.continuous.enabled          = true
discovery.continuous.interval_minutes = 5
discovery.continuous.environments     = prod,staging
```

---

## 4.4 Pipeline de Actualização do NodeHealthRecord

### O que existe

`NodeHealthRecord` tem entidade e `GetNodeHealth` query, mas o pipeline de
actualização automática não está completo — os registos não são actualizados
periodicamente por um processo.

### Solução: `NodeHealthCalculatorJob`

```csharp
// src/platform/NexTraceOne.BackgroundWorkers/Jobs/NodeHealthCalculatorJob.cs

[DisallowConcurrentExecution]
public class NodeHealthCalculatorJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var services = await _serviceRepo.GetAllActiveAsync();

        foreach (var service in services)
        {
            var score = await CalculateHealthScore(service);

            await _nodeHealthRepo.UpsertAsync(new NodeHealthRecord
            {
                NodeId = service.Id,
                NodeType = NodeType.Service,
                OverlayMode = OverlayMode.Health,
                Status = score >= 0.8f ? HealthStatus.Healthy
                       : score >= 0.5f ? HealthStatus.AtRisk
                       : HealthStatus.Unhealthy,
                Score = score,
                FactorsJson = JsonSerializer.Serialize(new {
                    IncidentScore   = await GetIncidentScore(service.Id),
                    SloScore        = await GetSloScore(service.Id),
                    DeploymentScore = await GetDeploymentScore(service.Id),
                    AnomalyScore    = await GetAnomalyScore(service.Id)
                }),
                CalculatedAt = DateTimeOffset.UtcNow,
                SourceSystem = "NodeHealthCalculatorJob"
            });
        }
    }
}
```

**Schedule:** a cada 2 minutos.

**Após Fase 3** — adicionar `OverlayMode.Infrastructure` com score baseado em
`HostHealthRecord` dos hosts onde o serviço corre.

---

## 4.5 Correlação Host ↔ Serviço nos Alertas

Quando `PropagationRiskAlert` é publicado, enriquecer com dados de host:

```csharp
public class EnrichedPropagationAlert
{
    public string DegradedServiceId { get; init; }
    public string DegradedServiceName { get; init; }
    public string Environment { get; init; }
    public int AffectedCount { get; init; }
    public ServiceDegradationCause ProbableCause { get; init; }
    // Novo (Fase 4, pós Fase 3):
    public HostCorrelation? HostCorrelation { get; init; }
    // Ex: { Hostname: "server-prod-02", CpuPct: 94.2, MemoryPct: 87.1, ProbableInfraCause: true }
}

public enum ServiceDegradationCause
{
    Unknown,
    CodeOrDeploy,        // host saudável, serviço degradado
    InfrastructurePressure,  // host sob pressão
    ExternalDependency,  // dependência upstream degradada
    CircularDependency
}
```

---

## Checklist de Entrega — Fase 4

**Time-Travel UI:**
- [ ] `TopologyTimeTravel.tsx` componente de slider criado
- [ ] `GET /api/v1/catalog/graph/snapshots` endpoint de listagem
- [ ] `GET /api/v1/catalog/graph/snapshots/{id}` endpoint de detalhe
- [ ] Integração do slider na `DependencyDashboardPage`
- [ ] Overlay de diff (nós verdes/vermelhos entre snapshots)
- [ ] Snapshot automático criado quando grafo muda (via Outbox)

**SignalR Alertas Real-time:**
- [ ] `TopologyAlertsHub` implementado e registado
- [ ] `ITopologyAlertPublisher` + `TopologyAlertPublisher` implementados
- [ ] Publisher chamado pelo pipeline de detecção de anomalias existente
- [ ] `useTopologyAlerts` hook no frontend
- [ ] Toast de notificação quando alerta chega
- [ ] Badge de alertas activos no ícone do grafo na sidebar

**Discovery Contínuo:**
- [ ] `ContinuousServiceDiscoveryJob` implementado
- [ ] Agendado a cada 5 minutos no Quartz
- [ ] Feature flags: `discovery.continuous.*`
- [ ] Log estruturado de cada run (ServicesFound, New, Errors)

**NodeHealthRecord Pipeline:**
- [ ] `NodeHealthCalculatorJob` implementado com 4 factores de score
- [ ] Agendado a cada 2 minutos
- [ ] `OverlayMode.Infrastructure` calculado a partir de `HostHealthRecord`
- [ ] Score exposto via `GET /api/v1/catalog/graph/nodes/{nodeId}/health`

**Correlação Host ↔ Serviço:**
- [ ] `EnrichedPropagationAlert` com `HostCorrelation`
- [ ] `ServiceDegradationCause` enum
- [ ] UI mostra causa provável no tooltip do alerta
