# Environment Comparison Architecture

## Visão Geral

A comparação entre ambientes no NexTraceOne funciona como um pipeline de decisão:
snapshot → baseline → drift detection → comparação de releases → UI de decisão.

## Fontes de Dados

```
RuntimeSnapshot (DB: oi_runtime_snapshots)
        │
        ├── CompareReleaseRuntime.Handler
        │     ├── Filtra por [BeforePeriodStart, BeforePeriodEnd]
        │     ├── Filtra por [AfterPeriodStart, AfterPeriodEnd]
        │     └── Calcula médias e deltas percentuais
        │
        ├── GetDriftFindings.Handler
        │     └── Lista findings persistidos (não reconhecidos ou por serviço)
        │
        ├── GetReleaseHealthTimeline.Handler
        │     └── Agrupa snapshots por release com métricas médias
        │
        └── GetObservabilityScore.Handler
              └── Calcula score composto (latência, erro, throughput, recursos)
```

## Fluxo da Comparação

1. **Frontend** envia parâmetros: `serviceName`, `environment`, períodos before/after
2. **`CompareReleaseRuntime`** lista snapshots do período e calcula médias/deltas
3. **`GetDriftFindings`** retorna findings não reconhecidos do serviço/ambiente
4. **`GetObservabilityScore`** calcula o score de observabilidade actual
5. **`GetReleaseHealthTimeline`** retorna a linha do tempo de saúde por release
6. **UI** apresenta score, comparação de métricas, drift findings e timeline

## Propagação do Atributo `environment`

O atributo `environment` é propagado em três camadas:

### OTel Spans
```csharp
// BuildingBlocks.Observability/OpenTelemetry/OpenTelemetryConfiguration.cs
ResourceBuilder.CreateDefault()
    .AddEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    .AddServiceName(serviceName)
```

### Serilog/Loki
```csharp
// Enrich.WithEnvironmentName() já presente
// Labels Loki: application, environment
```

### Snapshots/Findings
```csharp
// RuntimeSnapshot.Create(serviceName, environment, ...)
// DriftFinding.Detect(serviceName, environment, ...)
// environment como campo de primeira classe na entidade
```

## Convenção Oficial de Ambientes

| Valor | Uso |
|---|---|
| `dev` | Desenvolvimento local |
| `test` | Testes automatizados |
| `qa` | Quality Assurance |
| `uat` | User Acceptance Testing |
| `staging` | Pré-produção |
| `production` | Produção |

Estes valores são controlados por `ASPNETCORE_ENVIRONMENT` e pelo `EnvironmentResolutionMiddleware`.
A UI de comparação oferece estes valores fixos para garantir consistência.

## Release Context

A comparação liga-se ao contexto de release via:
- `ReleaseId` optional nos snapshots e findings
- `GetReleaseHealthTimeline` agrupa pontos por `releaseName`/`releaseId`
- A UI apresenta o nome da release em cada ponto da timeline
