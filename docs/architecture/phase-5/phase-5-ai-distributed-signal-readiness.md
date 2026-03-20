# Fase 5 — Readiness de IA para Sinais Distribuídos

## Visão Geral

A Fase 5 estabelece os contratos fundamentais para que a IA do NexTraceOne possa, no futuro, correlacionar sinais distribuídos de múltiplos módulos e fornecer análise de risco operacional — especialmente para decisões de promoção de releases entre ambientes.

---

## Pergunta Central que a IA Deve Responder

> "Com base nos sinais disponíveis (incidentes, releases, telemetria, contratos, topologia), esta release está pronta para ser promovida de QA para PROD?"

---

## Contratos Implementados

### IDistributedSignalCorrelationService

**Localização:** `BuildingBlocks.Application.Correlation`

Agrega e correlaciona sinais de diferentes módulos para um serviço em um ambiente específico.

```csharp
// Correlaciona sinais em um ambiente
var correlation = await correlationService.CorrelateSignalsAsync(
    tenantId: tenant.Id,
    environmentId: environment.EnvironmentId,
    serviceName: "payment-api",
    from: DateTimeOffset.UtcNow.AddHours(-24),
    to: DateTimeOffset.UtcNow);

// Compara sinais entre ambientes
var comparison = await correlationService.CompareEnvironmentsAsync(
    tenantId: tenant.Id,
    sourceEnvironmentId: stagingEnvId,
    targetEnvironmentId: prodEnvId,
    serviceName: "payment-api",
    from: from, to: to);
```

**DistributedSignalCorrelation:**

| Campo | Tipo | Significado |
|---|---|---|
| `IncidentCount` | `int` | Incidentes no período |
| `ReleaseCount` | `int` | Releases no período |
| `CorrelationScore` | `double` | 0.0 = sem correlação, 1.0 = forte correlação release→incidente |
| `HasPromotionRiskSignals` | `bool` | Shortcut: há sinais de risco de promoção? |
| `Signals` | `IReadOnlyList<string>` | Mensagens descritivas dos sinais identificados |

### IPromotionRiskSignalProvider

**Localização:** `BuildingBlocks.Application.Correlation`

Avalia o risco de promover uma release entre ambientes.

```csharp
var assessment = await riskProvider.AssessPromotionRiskAsync(
    tenantId: tenant.Id,
    sourceEnvironmentId: stagingEnvId,
    targetEnvironmentId: prodEnvId,
    serviceName: "payment-api",
    since: DateTimeOffset.UtcNow.AddDays(-7));

if (assessment.ShouldBlock)
{
    // Exibir aviso forte na UI / bloquear pipeline
    var criticalSignals = assessment.Signals
        .Where(s => s.Severity >= PromotionRiskLevel.High)
        .ToList();
}
```

**PromotionRiskLevel:**

| Nível | Valor | Ação Recomendada |
|---|---|---|
| `None` | 0 | Promoção segura — sem sinais de risco |
| `Low` | 1 | Monitorar — sinais menores identificados |
| `Medium` | 2 | Revisão recomendada antes de promover |
| `High` | 3 | Revisão obrigatória — `ShouldBlock = true` |
| `Critical` | 4 | Bloqueio recomendado até resolução — `ShouldBlock = true` |

**PRINCÍPIO DE NEUTRALIDADE:** O serviço não decide a promoção — apenas fornece sinais. A decisão é sempre do usuário.

---

## Sinais que a IA Correlaciona (Implementação Futura)

### Módulo ChangeGovernance

- Número de releases no período
- Correlação temporal release → incidente (janela de 1h, 4h, 24h)
- Rollbacks recentes

### Módulo OperationalIntelligence

- Incidentes abertos/fechados no período
- Incidentes correlacionados com releases
- MTTR (tempo médio de resolução) no período

### Módulo ServiceCatalog / Telemetry

- Aumento de error rate pós-release
- Aumento de latência P95/P99 pós-release
- Anomalias de throughput

### Módulo ContractGovernance

- Violações de contrato detectadas no ambiente source
- Drift de schema detectado
- Incompatibilidades de versão

### Topologia

- Mudanças de dependências entre ambientes
- Novos serviços dependentes não mapeados

---

## Implementações Stub (Fase 5)

As implementações nulas registradas em Fase 5 retornam dados seguros e vazios:

| Implementação | Retorno |
|---|---|
| `NullDistributedSignalCorrelationService` | `CorrelationScore = 0.0`, `Signals = []` |
| `NullPromotionRiskSignalProvider` | `RiskLevel = None`, `ShouldBlock = false` |

**Intenção:** O sistema pode iniciar e funcionar sem módulos operacionais. Quando os módulos são adicionados, eles registram implementações concretas que substituem os stubs.

---

## Isolamento de Tenant — Regra Inviolável

Toda correlação e avaliação é isolada por TenantId:

- `CorrelateSignalsAsync` recebe `tenantId` obrigatório
- `AssessPromotionRiskAsync` recebe `tenantId` obrigatório
- Nenhuma implementação deve cruzar dados entre tenants
- `CompareEnvironmentsAsync` compara ambientes do MESMO tenant

Esta regra é estrutural — os contratos tornam impossível uma chamada legítima que cruze tenants.

---

## Roadmap de Implementação Futura

### Fase 6 — Implementações concretas

1. `DbDistributedSignalCorrelationService` — lê de tabelas de incidents + releases correlacionados
2. `DbPromotionRiskSignalProvider` — agrega sinais de múltiplos módulos
3. API endpoint de avaliação de risco (GET `/api/tenants/{id}/environments/{src}/promotion-risk/{target}`)

### Fase 7 — IA nativa

1. Enriquecimento dos sinais com LLM interno
2. Narrativa gerada por IA: "Por que este serviço tem risco de promoção?"
3. Sugestão de mitigação baseada em histórico
