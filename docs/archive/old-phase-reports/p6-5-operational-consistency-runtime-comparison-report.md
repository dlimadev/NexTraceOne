# P6.5 — Operational Consistency: Runtime Comparison Between Environments

**Data:** 2026-03-27
**Fase:** P6.5 — Fechar Operational Consistency / Runtime Comparison real entre ambientes
**Estado:** CONCLUÍDO

---

## 1. Objetivo

Fechar o pilar Operational Consistency tornando funcional o pipeline completo de runtime comparison entre ambientes. As estruturas `RuntimeBaseline`, `RuntimeSnapshot` e `DriftFinding` já existiam no modelo de dados, mas faltavam:

1. **`EstablishRuntimeBaseline`** — nenhum handler/endpoint para criar ou actualizar uma baseline (o `DetectRuntimeDrift` pressupõe que a baseline já existe, mas não havia forma de a criar via API)
2. **`CompareEnvironments`** — nenhum handler para comparar o mesmo serviço entre dois ambientes distintos (ex: staging vs production), que é a essência da página "Environment Comparison"

---

## 2. Estado anterior ao P6.5

| Capacidade | Estado |
|---|---|
| `DetectRuntimeDrift` (baseline vs latest snapshot, single env) | ✅ Existente |
| `CompareReleaseRuntime` (before/after periods, same env) | ✅ Existente |
| `GetDriftFindings` (lista findings persistidos) | ✅ Existente |
| `IngestRuntimeSnapshot` (persiste snapshots) | ✅ Existente |
| `EstablishRuntimeBaseline` (criar/actualizar baseline via API) | ❌ Ausente |
| `CompareEnvironments` (staging vs production) | ❌ Ausente |
| Endpoint `POST /runtime/baselines` | ❌ Ausente |
| Endpoint `POST /runtime/compare-environments` | ❌ Ausente |

---

## 3. Ficheiros alterados

### 3.1 Application — Novos handlers

| Feature | Tipo | Descrição |
|---|---|---|
| `EstablishRuntimeBaseline` | Command + Handler | Cria ou actualiza (upsert) a `RuntimeBaseline` de um serviço+ambiente. Chama `Refresh` se já existe, `Establish` se é nova. |
| `CompareEnvironments` | Command + Handler | Compara os snapshots mais recentes de dois ambientes distintos; calcula desvios; persiste `DriftFinding`s para métricas fora da tolerância. |

### 3.2 Application — Repositório

| Ficheiro | Alteração |
|---|---|
| `IRuntimeBaselineRepository.cs` | Adicionado `ListByServiceAsync` |

### 3.3 Infraestrutura — Repositório

| Ficheiro | Alteração |
|---|---|
| `RuntimeBaselineRepository.cs` | Implementação de `ListByServiceAsync` |

### 3.4 Application — DI

| Ficheiro | Alteração |
|---|---|
| `Application/Runtime/DependencyInjection.cs` | Adicionados 2 novos validators P6.5 |

### 3.5 API

| Ficheiro | Alteração |
|---|---|
| `API/Runtime/Endpoints/RuntimeIntelligenceEndpointModule.cs` | Adicionados 2 novos endpoints + 2 novos `using` aliases |

### 3.6 Testes

| Ficheiro | Descrição |
|---|---|
| `Tests/Runtime/Application/OperationalConsistencyHandlerTests.cs` | 10 novos testes unitários |

---

## 4. Modelo de comparação adoptado

### 4.1 EstablishRuntimeBaseline — upsert

```
POST /api/v1/runtime/baselines
{
  "serviceName": "svc-checkout",
  "environment": "production",
  "expectedAvgLatencyMs": 120,
  "expectedP99LatencyMs": 350,
  "expectedErrorRate": 0.008,
  "expectedRequestsPerSecond": 80,
  "dataPointCount": 30,
  "confidenceScore": 0.85
}
```

Se já existe baseline para `svc-checkout / production` → `RuntimeBaseline.Refresh()`.  
Se não existe → `RuntimeBaseline.Establish()` + repositório `Add()`.

A resposta inclui `IsUpdate: true/false` e `IsConfident` (threshold 0.5 de `ConfidenceScore`).

### 4.2 CompareEnvironments — cross-env drift

```
POST /api/v1/runtime/compare-environments
{
  "serviceName": "svc-checkout",
  "sourceEnvironment": "staging",
  "targetEnvironment": "production",
  "tolerancePercent": 20,
  "releaseId": "..." // opcional
}
```

Pipeline interno:
1. Obtém latest snapshot de `staging`
2. Obtém latest snapshot de `production`
3. Constrói uma "baseline sintética" a partir do snapshot source
4. Chama `targetSnapshot.CalculateDeviationsFrom(syntheticBaseline)` para as 4 métricas
5. Para métricas fora da tolerância → `DriftFinding.Detect(...)` persistido com `Environment = targetEnvironment`
6. Retorna comparação completa: `SourceHealthStatus`, `TargetHealthStatus`, `Deviations`, `HasDrift`

Os `DriftFinding`s gerados são rastreáveis por `targetEnvironment` e podem ser ligados a uma release via `ReleaseId` opcional.

---

## 5. Novos endpoints API

| Método | Endpoint | Permissão | Descrição |
|---|---|---|---|
| POST | `/api/v1/runtime/baselines` | `operations:runtime:write` | Cria ou actualiza baseline de métricas esperadas |
| POST | `/api/v1/runtime/compare-environments` | `operations:runtime:write` | Compara dois ambientes do mesmo serviço e persiste drifts |

---

## 6. Ligação com domínio e entidades

| Entidade | Uso |
|---|---|
| `RuntimeBaseline` | Criada/actualizada por `EstablishRuntimeBaseline`; usada por `DetectRuntimeDrift` |
| `RuntimeSnapshot` | Consumido por `CompareEnvironments` (latest por ambiente) |
| `DriftFinding` | Criado por `CompareEnvironments` (e pelo existente `DetectRuntimeDrift`) |
| `AutomationWorkflowRecord` | Não alterado — pode ser acionado por workflow futuro sobre os findings |

---

## 7. Integração com página Environment Comparison

A página `EnvironmentComparisonPage` pode agora:
1. Chamar `POST /api/v1/runtime/compare-environments` para iniciar comparação real
2. Chamar `GET /api/v1/runtime/drift?serviceName=...&environment=production&unacknowledgedOnly=true` para listar os drifts activos da produção

Os contratos são estáveis e não exigem redesenho de UI — o frontend pode receber `HasDrift`, `SourceHealthStatus`, `TargetHealthStatus` e `Deviations` com `DeviationPercent` e `DriftSeverity`.

---

## 8. Validação

| Critério | Estado |
|---|---|
| Compilação sem erros | ✅ Build succeeded |
| Testes pré-P6.5 mantidos | ✅ 410 → 420 (0 falhas) |
| Novos testes | ✅ 10 novos testes |
| `EstablishRuntimeBaseline` — nova baseline | ✅ Testado |
| `EstablishRuntimeBaseline` — baseline existente (refresh) | ✅ Testado |
| `EstablishRuntimeBaseline` — low confidence | ✅ Testado |
| `CompareEnvironments` — sem drift | ✅ Testado |
| `CompareEnvironments` — alta latência em prod | ✅ Testado |
| `CompareEnvironments` — source snapshot missing | ✅ Testado |
| `CompareEnvironments` — target snapshot missing | ✅ Testado |
| `CompareEnvironments` — com ReleaseId | ✅ Testado |
| `CompareEnvironments` — health status diferente | ✅ Testado |
| `CompareEnvironments` — ambientes persistidos na resposta | ✅ Testado |
