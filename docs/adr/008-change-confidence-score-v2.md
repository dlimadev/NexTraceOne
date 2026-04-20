# ADR-008: Change Confidence Score 2.0 — Decomposição Explicável

## Status

Proposed

## Data

2026-04-20

## Contexto

O NexTraceOne possui hoje `ChangeConfidenceScore` no módulo `ChangeGovernance`, calculado por feature real (`ChangeGovernance.Application.Features.ChangeIntelligence`), retornando um valor agregado 0–100 por release. O score é um pilar de **Production Change Confidence** (capítulo 3.3 das Copilot Instructions) e é consumido por:

- `ReleaseTrain`
- `PromotionGateStatus`
- Frontend `ReleaseDetailPage` e dashboards executivos
- Cross-module via `IChangeIntelligenceReader`

### Problema observado

1. **Caixa-preta operacional**: o valor agregado não explica *porquê* o score é 47 em vez de 80 — impossível acionar melhoria sem reverse-engineering.
2. **Auditoria insuficiente**: auditores e Tech Leads precisam de saber **que evidência** alimentou o valor em cada momento (não só "foi 47 em 2026-04-12").
3. **Heurísticas implícitas**: pesos estão no código, não são parametrizáveis por tenant nem auditáveis.
4. **Sem citação de fontes**: não é rastreável qual telemetry, qual feature-flag, qual CI run alimentou cada dimensão.
5. **Persona mismatch**: Executive persona pede *trend*; Engineer pede *o que falta para chegar a 80*; ambas recebem o mesmo número.

## Decisão

Evoluir para **Change Confidence Score 2.0**, mantendo o **valor agregado 0–100** para compatibilidade com consumidores existentes, mas decompondo-o em **sub-scores explícitos com citações**.

### Sub-scores (dimensões)

Cada sub-score é 0–100, tem peso configurável por tenant e documenta a sua fonte:

| Sub-score | O que mede | Fonte primária |
|-----------|-----------|----------------|
| `TestCoverage` | Cobertura reportada pelo pipeline que produziu o artefacto | CI metadata via `Integrations` / `IngestionExecutions` |
| `ContractStability` | Nº de breaking changes + quality score dos contratos afetados | `Catalog.Contracts` (GetContractHealthTimeline, ComputeSemanticDiff) |
| `HistoricalRegression` | Taxa de regressão observada em releases passados deste serviço/autor | `OperationalIntelligence.Incidents` correlacionados via `ChangeIntelligence` |
| `BlastSurface` | Dimensão do blast radius (serviços + contratos + consumidores impactados) | `ChangeGovernance.BlastRadius` + `ContractConsumerInventory` (Wave A.2) |
| `DependencyHealth` | Score agregado dos serviços dependentes (reliability + drift) | `OperationalIntelligence.Reliability` + `RuntimeIntelligence.DriftFinding` |
| `CanarySignal` | Error rate e latência observados no canary, quando aplicável | `ICanaryProvider` (opcional, degrada graciosamente) |
| `PreProdDelta` | Delta de comportamento entre staging e prod baseline para este release | `RuntimeIntelligence.RuntimeBaseline` (alimenta Wave A.1 "Promotion Readiness Delta") |

O score agregado é uma **média ponderada** — pesos default no `ConfigurationDefinitionSeeder`, override por tenant via `Configuration`.

### Citação (auditability)

Cada sub-score, por execução, persiste:

- `Value` (0–100)
- `Weight` (aplicado na agregação)
- `Confidence` (Low/Medium/High — reflete se a fonte está disponível, ou se devolveu `simulatedNote`)
- `Citations` — lista de URIs internas resolvíveis, por exemplo:
  - `citation://catalog/contracts/{id}/health-timeline?at=<ts>`
  - `citation://changegovernance/releases/{id}/blast-radius`
  - `citation://operationalintelligence/incidents?service={id}&window=90d`
  - `citation://integrations/ingestion/{executionId}`
- `ComputedAt` (UTC)

### Degradação graciosa

Se uma fonte não estiver configurada (ex.: `ICanaryProvider` é `NullCanaryProvider`), o sub-score respetivo marca `Confidence = Low` e `simulatedNote` na UI. O peso é **redistribuído proporcionalmente** pelos sub-scores disponíveis (não zero-out arbitrário). O score agregado continua calculável, com transparência sobre qual dimensão não contribuiu.

### Persistência

- Novo agregado `ChangeConfidenceBreakdown` em `ChangeGovernance.Domain.ChangeIntelligence`.
- Tabela `chg_confidence_breakdowns` + `chg_confidence_sub_scores` (1:N).
- Migração EF Core em `ChangeIntelligenceDbContext`.
- Retenção mínima: `Value` e `Citations` mantidos imutáveis por release; re-cálculo cria nova versão (não mutação).

### API

- Manter contrato atual de `GetChangeScore` para retrocompatibilidade — passa a incluir `Breakdown` (opcional no payload, ativável via query param ou via feature flag até estabilizar).
- Novo endpoint `GET /api/v1/changes/{releaseId}/confidence/breakdown` — retorna breakdown completo com citações.
- Novo endpoint `GET /api/v1/changes/{releaseId}/confidence/trend?window=30d` — série temporal por sub-score.

### Frontend

- `ReleaseDetailPage` passa a mostrar o breakdown como "radar" (Radix/ECharts) com drill-down por sub-score. Cada drill-down exibe citações resolvíveis como links para páginas existentes (contratos, incidentes, runtime baselines).
- Executive views usam o score agregado + trend (comportamento atual preservado).
- Engineer/Tech Lead veem breakdown + "o que melhorar" (sub-score com menor valor + citação direta).
- i18n obrigatório em todas as strings.

### Configuração parametrizável por tenant

Via `IConfigurationResolutionService`:

- `change.confidence.weights.testCoverage` (default 0.15)
- `change.confidence.weights.contractStability` (default 0.20)
- `change.confidence.weights.historicalRegression` (default 0.15)
- `change.confidence.weights.blastSurface` (default 0.15)
- `change.confidence.weights.dependencyHealth` (default 0.10)
- `change.confidence.weights.canarySignal` (default 0.10)
- `change.confidence.weights.preProdDelta` (default 0.15)
- `change.confidence.minConfidenceForPromotion` (default 70)
- `change.confidence.historicalWindow.days` (default 90)

A soma dos pesos é normalizada — não é preciso garantir 1.0 na config.

## Migração

1. Adicionar breakdown como estrutura paralela ao score existente (backward compatible).
2. Popular breakdown para releases novos.
3. Expor na UI como "Beta" via feature flag por N releases.
4. Depreciar o campo escalar cru apenas após 2 minor releases com breakdown em produção real.

## Consequências

### Positivas

- Explicabilidade **auditável** — crítico para clientes regulados (DORA, SOC2, ISO 27001).
- Acionabilidade — Engineer vê o que melhorar; Exec vê o trend.
- Compatível com governança: pesos parametrizáveis por tenant.
- Reforça integração cross-module sem quebrar bounded contexts (usa readers existentes).
- Preparado para degradação graciosa e providers opcionais.

### Negativas

- Custo de cálculo aumenta (7 fontes vs. 1 agregado) — mitigar com cache por release + recomputação assíncrona via Outbox.
- Novos testes necessários por sub-score (unitário + integração).
- Superfície de UI nova no ReleaseDetailPage.

### Neutras

- Score agregado permanece 0–100 — nenhum consumidor externo quebra.
- Pesos padrão podem ser ajustados sem deploy via Configuration.

## Critérios de aceite

- [ ] `ChangeConfidenceBreakdown` persistido via EF Core em `ChangeIntelligenceDbContext`.
- [ ] Todos os sub-scores com citações verificáveis (URIs resolvíveis no backend).
- [ ] Cálculo respeita `CancellationToken`, retorna `Result<T>`.
- [ ] Degradação graciosa quando provider opcional não está configurado.
- [ ] Pesos 100% parametrizáveis via `ConfigurationDefinitionSeeder`.
- [ ] Frontend com breakdown visível para Engineer/Tech Lead, agregado preservado para Exec.
- [ ] i18n em pt-PT, pt-BR, en, es.
- [ ] Testes unitários de agregação, redistribuição de peso e citations.
- [ ] Audit trail de todas as computações via `AuditCompliance`.

## Referências

- [ADR-001: Modular Monolith](./001-modular-monolith.md)
- [CHANGE-CONFIDENCE.md](../CHANGE-CONFIDENCE.md)
- [FUTURE-ROADMAP — Wave A.1](../FUTURE-ROADMAP.md)
- [HONEST-GAPS.md — degradação graciosa](../HONEST-GAPS.md)
