# Phase 7 — Non-Prod Analysis and Promotion Risk

## Visão geral

Os três features desta fase implementam a **estratégia de prevenção** do NexTraceOne:
identificar riscos e regressões ANTES da promoção para produção.

---

## Feature 1: AnalyzeNonProdEnvironment

### Propósito

Analisar um ambiente não produtivo para identificar sinais de risco que possam comprometer
a produção se o ambiente for promovido.

### Command

```csharp
public sealed record Command(
    string TenantId,           // obrigatório — isolamento de tenant
    string EnvironmentId,      // ID do ambiente a analisar
    string EnvironmentName,    // nome legível para grounding
    string EnvironmentProfile, // ex.: qa, staging, development
    IReadOnlyList<string>? ServiceFilter, // null = todos os serviços
    int ObservationWindowDays, // 1-90 dias
    string? PreferredProvider) // null = routing automático
```

### Parsing da resposta da IA

O handler espera resposta estruturada com marcadores:

```
FINDING: HIGH | contract-drift | PaymentService has breaking changes
FINDING: MEDIUM | telemetry | Error rate above baseline
OVERALL_RISK: HIGH
RECOMMENDATION: Block promotion until contract drift is resolved.
```

- `FINDING:` → `AnalysisFinding { Severity, Category, Description }`
- `OVERALL_RISK:` → HIGH | MEDIUM | LOW | UNKNOWN
- `RECOMMENDATION:` → texto livre

### Response

```csharp
public sealed record Response(
    string TenantId,
    string EnvironmentId,
    string EnvironmentName,
    string EnvironmentProfile,
    IReadOnlyList<AnalysisFinding> Findings,
    string OverallRiskLevel,
    string Recommendation,
    string RawAnalysis,
    bool IsFallback,
    string CorrelationId,
    DateTimeOffset AnalyzedAt,
    int ObservationWindowDays);
```

---

## Feature 2: CompareEnvironments

### Propósito

Comparar dois ambientes do mesmo tenant e identificar divergências relevantes para
decisão de promoção.

**REGRA FUNDAMENTAL**: A comparação é sempre intra-tenant. O backend garante este
isolamento — o `TenantId` é incluído no grounding e validado.

### Parsing da resposta

```
DIVERGENCE: HIGH | contracts | OrderService v2.1 has breaking changes vs v2.0
DIVERGENCE: MEDIUM | telemetry | P99 latency 40% higher
PROMOTION_RECOMMENDATION: BLOCK_PROMOTION
SUMMARY: QA shows contract drift and performance regression.
```

- `DIVERGENCE:` → `EnvironmentDivergence { Severity, Dimension, Description }`
- `PROMOTION_RECOMMENDATION:` → SAFE_TO_PROMOTE | REVIEW_REQUIRED | BLOCK_PROMOTION
- `SUMMARY:` → resumo da comparação

### Validação

- `SubjectEnvironmentId != ReferenceEnvironmentId` (obrigatório)
- `TenantId` não vazio

---

## Feature 3: AssessPromotionReadiness

### Propósito

Avaliar se um serviço específico está pronto para ser promovido de um ambiente para outro.
Fornece score numérico, nível de readiness, blockers e warnings.

### Parsing da resposta

```
READINESS_SCORE: 78
READINESS_LEVEL: NEEDS_REVIEW
BLOCKER: contract | Payment v2.1 has breaking change in response schema
WARNING: performance | P99 latency slightly above SLA threshold
SHOULD_BLOCK: YES
SUMMARY: Service has a contract breaking change that must be resolved.
```

- `READINESS_SCORE:` → 0-100 (clampado)
- `READINESS_LEVEL:` → NOT_READY | NEEDS_REVIEW | READY
- `BLOCKER:` → `ReadinessIssue { Type="BLOCKER", Category, Description }`
- `WARNING:` → `ReadinessIssue { Type="WARNING", Category, Description }`
- `SHOULD_BLOCK:` → YES | NO (bool)
- `SUMMARY:` → resumo acionável

---

## Isolamento de tenant

Todos os features:
1. Requerem `TenantId` não vazio no command
2. Incluem `TenantId` no grounding context enviado à IA
3. O grounding de `CompareEnvironments` inclui explicitamente "Both environments belong to the same tenant"
4. `Guard.Against.NullOrWhiteSpace(request.TenantId)` no início do handler

---

## Tratamento de fallback

Quando o provider retorna `[FALLBACK_PROVIDER_UNAVAILABLE]`:
- `IsFallback = true` na response
- O handler não lança exceção
- O frontend exibe badge "Fallback" ao utilizador

Quando o provider lança exceção:
- `Result.Error` com código `AIKnowledge.Provider.Unavailable`
- HTTP 422 via `ToHttpResult(localizer)`
