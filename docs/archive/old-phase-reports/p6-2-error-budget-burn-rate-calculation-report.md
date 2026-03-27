# P6.2 — Error Budget e Burn Rate: Implementação de Cálculo Real

**Data:** 2026-03-26
**Fase:** P6.2 — Implementar cálculo real de error budget e burn rate
**Estado:** CONCLUÍDO

---

## 1. Objetivo

Evoluir o subdomínio Reliability do módulo Operational Intelligence de uma fase de "estrutura persistida apenas" (P6.1) para um estado com **cálculo operacional real** de error budget e burn rate, ligado a dados de runtime observados e com resultados persistidos e consultáveis.

---

## 2. Estado anterior ao P6.2

Após P6.1, a situação era:

| Capacidade | Estado |
|---|---|
| Entidades persistidas (SloDefinition, SlaDefinition, ErrorBudgetSnapshot, BurnRateSnapshot) | ✅ Existia |
| Repositórios e wiring DI | ✅ Existia |
| Handlers para registar SLO/SLA | ✅ Existia |
| Queries GetErrorBudget / GetBurnRate | ✅ Existia |
| **Cálculo real de error budget** | ❌ Ausente — apenas estrutura |
| **Cálculo real de burn rate** | ❌ Ausente — apenas estrutura |
| Listagem de SLOs por serviço | ❌ Ausente |
| Listagem de SLAs por SLO | ❌ Ausente |

---

## 3. Ficheiros alterados

### 3.1 Novos ficheiros — Application (Service)

| Ficheiro | Descrição |
|---|---|
| `Application/Reliability/Services/IErrorBudgetCalculator.cs` | Interface do serviço de cálculo com 4 métodos: `ComputeTotalBudgetMinutes`, `ComputeConsumedBudgetMinutes`, `ComputeToleratedErrorRate`, `ComputeBurnRate`, `GetWindowHours` |

### 3.2 Novos ficheiros — Infrastructure (Calculator)

| Ficheiro | Descrição |
|---|---|
| `Infrastructure/Reliability/ErrorBudgetCalculator.cs` | Implementação determinística e stateless das fórmulas de error budget e burn rate (internal sealed, registado como Singleton) |

### 3.3 Novos ficheiros — Application (CQRS Handlers)

| Feature | Tipo | Descrição |
|---|---|---|
| `ComputeErrorBudget` | Command + Handler | Lê SLO, obtém signal de runtime, calcula total/consumed/remaining, persiste ErrorBudgetSnapshot |
| `ComputeBurnRate` | Command + Handler | Lê SLO, obtém signal de runtime, calcula burn rate por janela(s), persiste BurnRateSnapshot(s) |
| `ListServiceSlos` | Query + Handler | Lista todos os SLOs de um serviço |
| `ListSloSlas` | Query + Handler | Lista todos os SLAs de um SLO |

### 3.4 Ficheiros alterados — Application DI

| Ficheiro | Alteração |
|---|---|
| `Application/Reliability/DependencyInjection.cs` | Adicionados validators para ComputeErrorBudget, ComputeBurnRate, ListServiceSlos, ListSloSlas |

### 3.5 Ficheiros alterados — Infrastructure DI

| Ficheiro | Alteração |
|---|---|
| `Infrastructure/Reliability/DependencyInjection.cs` | Adicionado `services.AddSingleton<IErrorBudgetCalculator, ErrorBudgetCalculator>()` |

### 3.6 Ficheiros alterados — API

| Ficheiro | Alteração |
|---|---|
| `API/Reliability/Endpoints/Endpoints/ReliabilityEndpointModule.cs` | Adicionados 4 novos endpoints (ver secção 5) |

### 3.7 Novos ficheiros — Testes

| Ficheiro | Descrição |
|---|---|
| `Tests/Reliability/Application/Calculation/ErrorBudgetCalculatorTests.cs` | 17 testes unitários do calculator (fórmulas, edge cases, integração) |
| `Tests/Reliability/Application/Calculation/CalculationHandlerTests.cs` | 16 testes unitários dos handlers |

---

## 4. Modelo de cálculo adoptado

### 4.1 Error Budget Total

```
total_budget_minutes = (1 − target_percent/100) × window_days × 1440
```

**Exemplo:** SLO 99.9%, janela 30 dias
→ `(1 − 0.999) × 30 × 1440 = 0.001 × 43200 = 43.2 minutos`

### 4.2 Error Budget Consumido

```
consumed_budget_minutes = observed_error_rate × window_days × 1440
```

O `observed_error_rate` provém do **RuntimeSnapshot mais recente** do serviço (`IReliabilityRuntimeSurface.GetLatestSignalAsync`). Representa a taxa de erros actual do serviço.

**Exemplo:** ErrorRate = 0.002 (0.2%), janela 30 dias
→ `0.002 × 43200 = 86.4 minutos` consumidos

### 4.3 Tolerated Error Rate

```
tolerated_error_rate = 1 − (target_percent / 100)
```

**Exemplo:** SLO 99.9% → tolerated = 0.001 (0.1% erros tolerados)

### 4.4 Burn Rate

```
burn_rate = observed_error_rate / tolerated_error_rate
```

Limiares de classificação (Google SRE):
- `burn_rate < 6` → **Healthy**
- `6 ≤ burn_rate < 14.4` → **AtRisk**
- `burn_rate ≥ 14.4` → **Violated**

Caso especial: `tolerated_error_rate = 0` (SLO 100%) e `observed > 0` → burn rate = **999** (sentinel de violação crítica)

### 4.5 Fonte de dados

Na fase P6.2, o `observed_error_rate` é obtido do **snapshot mais recente do RuntimeSnapshot** (tabela `ops_runtime_snapshots`). Este valor representa o estado actual observado.

Limitação reconhecida: o cálculo aplica a taxa actual a toda a janela histórica. Em P6.3+ deverá usar a média ponderada de snapshots dentro da janela completa (ClickHouse ou PostgreSQL analítico).

---

## 5. Novos endpoints API

| Método | Endpoint | Permissão | Descrição |
|---|---|---|---|
| GET | `/api/v1/reliability/services/{serviceId}/slos` | `operations:reliability:read` | Lista SLOs do serviço |
| GET | `/api/v1/reliability/slos/{sloId}/slas` | `operations:reliability:read` | Lista SLAs do SLO |
| POST | `/api/v1/reliability/slos/{sloId}/compute-error-budget` | `operations:reliability:write` | Calcula e persiste error budget |
| POST | `/api/v1/reliability/slos/{sloId}/compute-burn-rate?window=...` | `operations:reliability:write` | Calcula e persiste burn rate |

---

## 6. Fluxo de cálculo

```
POST /slos/{id}/compute-error-budget
  1. Load SloDefinition (from ReliabilityDbContext)
  2. GetLatestSignalAsync(serviceId, env)  [from RuntimeIntelligenceDbContext via IReliabilityRuntimeSurface]
  3. ErrorBudgetCalculator.ComputeTotalBudgetMinutes(slo)
  4. ErrorBudgetCalculator.ComputeConsumedBudgetMinutes(slo, observedErrorRate)
  5. ErrorBudgetSnapshot.Create(...)       [domain entity → computes remaining + status]
  6. ErrorBudgetSnapshotRepository.AddAsync(...)
  7. Return computed response

POST /slos/{id}/compute-burn-rate?window=OneHour
  1. Load SloDefinition
  2. GetLatestSignalAsync(serviceId, env)
  3. ErrorBudgetCalculator.ComputeToleratedErrorRate(slo)
  4. ErrorBudgetCalculator.ComputeBurnRate(slo, observedErrorRate)
  5. BurnRateSnapshot.Create(...)          [domain entity → computes status]
  6. BurnRateSnapshotRepository.AddAsync(...)
  7. Return computed response
```

---

## 7. Validação

| Critério | Estado |
|---|---|
| Compilação sem erros | ✅ Build succeeded |
| Testes pré-P6.2 mantidos | ✅ 350 → 387 testes (0 falhas) |
| Novos testes adicionados | ✅ 37 novos testes |
| IErrorBudgetCalculator registado no DI | ✅ Singleton |
| Fórmulas validadas com teoria e edge cases | ✅ 17 testes calculator |
| Handlers com runtime signal real + fallback | ✅ Signal null → 0 consumed |
| Endpoints acessíveis | ✅ 4 novos endpoints |
| Coerência com padrões da plataforma | ✅ guard clauses, CancellationToken, Result<T>, strongly typed IDs |
