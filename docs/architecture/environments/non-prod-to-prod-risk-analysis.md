# Non-Production to Production Risk Analysis

**Data:** 2026-03-21  
**Status:** Base arquitetural implementada — análise AI operacional

---

## Como o Sistema Usa Ambientes Não Produtivos para Julgar Risco em Produção

O NexTraceOne implementa um pipeline de análise de risco baseado nos sinais observados em ambientes não produtivos, comparando-os com o comportamento esperado do ambiente de produção.

### Premissa fundamental

> Ambientes não produtivos (QA, UAT, Staging, Homologação, etc.) são a janela de observação mais confiável para detectar riscos antes de um release chegar a produção.

O sistema não espera incidentes em produção para aprender — usa o não produtivo para **prevenir**.

## Modelo de Comparação de Ambientes

### `CompareEnvironments` Feature

**Endpoint:** `POST /api/v1/ai/orchestration/compare-environments`

Recebe:
- `TenantId` (mesmos tenant — isolamento garantido)
- `SubjectEnvironmentId` + perfil (o ambiente a analisar)
- `ReferenceEnvironmentId` + perfil (referência, geralmente produção)
- `ServiceFilter` (opcional)
- `ComparisonDimensions` (contratos, telemetria, incidentes, topologia)

Produz:
- Lista de divergências com severidade (HIGH/MEDIUM/LOW) e dimensão
- Recomendação: `SAFE_TO_PROMOTE`, `REVIEW_REQUIRED`, `BLOCK_PROMOTION`
- Resumo contextualizado

### `AnalyzeNonProdEnvironment` Feature

Analisa sinais de um ambiente não produtivo específico para:
- Identificar regressões
- Detectar padrões anômalos
- Comparar com baseline histórico

**Constraint**: Rejeita ambientes com perfil `Production` ou `DisasterRecovery` — análise de não-prod é exclusiva para ambientes não produtivos.

## Assessment de Readiness para Promoção

### `AssessPromotionReadiness` Feature

**Endpoint:** `POST /api/v1/ai/orchestration/assess-promotion-readiness`

Recebe:
- `TenantId`
- Ambiente de origem (source) — deve ser `IsProductionLike=false`
- Ambiente de destino (target) — deve ser `IsProductionLike=true`
- `ServiceName`, `Version`, `ReleaseId`
- `ObservationWindowDays` (1-90 dias)

Produz:
- `ReadinessScore` (0–100)
- `ReadinessStatus`: READY, CONDITIONAL, NOT_READY, BLOCKED
- `Findings[]` com severidade e evidência
- `RegressionSignals[]`
- `Recommendation`: SAFE_TO_PROMOTE, REVIEW_REQUIRED, BLOCK_PROMOTION
- `Justification` e `Limitations`

### Constraint crítica de segurança

O validator garante que a promoção parte de um ambiente não produtivo e chega a um produtivo:

```csharp
RuleFor(x => x.SourceIsProductionLike).Equal(false)
    .WithMessage("Source must not be production-like.");
RuleFor(x => x.TargetIsProductionLike).Equal(true)
    .WithMessage("Target must be production-like.");
```

## Modelo de Isolamento da IA

Regras estrita de isolamento:
- A IA **nunca** cruza dados de tenants diferentes
- A IA **nunca** compara ambientes de tenants diferentes
- `TenantId` é sempre validado antes de qualquer análise
- Erro seguro sem vazar dados: falha explícita com código de erro

## Promotion Risk: Conceitos Implementados

| Conceito | Implementação |
|----------|---------------|
| PromotionRiskAnalysis | `AssessPromotionReadiness.Response` |
| PromotionReadinessAssessment | `ReadinessStatus` enum + `Findings[]` |
| EnvironmentComparison | `CompareEnvironments.Response` com `Divergences[]` |
| RegressionSignal | `RegressionSignals[]` no assessment |
| RiskFinding | `Finding` record com `Severity`, `Area`, `Description` |
| RecommendationReport | `Recommendation` + `Justification` + `Limitations` |
| Evidence traceability | `CorrelationId` em todas as análises |

## Limitações Remanescentes

1. **Dados reais de telemetria**: atualmente o grounding context usa dados injetados. Integração com sinal de telemetria real (traces, métricas, logs) está planejada.
2. **Comparação de contratos automática**: `CompareEnvironments` usa grounding texto; integração com `IEnvironmentIntegrationBinding` para comparar contratos reais está planejada.
3. **Histórico de promotions**: não há persistência de assessments anteriores para comparação longitudinal.
4. **Bloqueio automático**: a recomendação `BLOCK_PROMOTION` é informativa; integração com o pipeline de CI/CD para bloqueio automático está planejada.

## Próximos Passos

- [ ] Integrar `GetPrimaryProductionAsync` nos handlers de IA para obter produção real do tenant
- [ ] Persistir `PromotionRiskAssessment` para histórico e auditoria
- [ ] Integrar sinais reais de telemetria como fonte de grounding context
- [ ] Expor superfície de "Readiness Report" no frontend (PromotionPage)
