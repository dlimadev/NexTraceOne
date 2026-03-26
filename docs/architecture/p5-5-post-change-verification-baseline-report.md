# P5.5 — Post-Change Verification com Baseline: Relatório de Execução

**Data:** 2026-03-26  
**Fase:** P5.5 — Post-Change Verification Automatizada com Baseline  
**Estado:** CONCLUÍDO

---

## 1. Objetivo

Implementar o pipeline automático de post-change verification no módulo Change Governance do
NexTraceOne. Usar `ReleaseBaseline`, `ObservationWindow` e `PostReleaseReview` já existentes para
criar um fluxo que:
- recebe métricas observadas pós-deploy
- compara automaticamente com o baseline pré-deploy
- classifica o outcome (Positive/Neutral/NeedsAttention/Negative)
- materializa o resultado num `PostReleaseReview` rastreável
- disponibiliza o resultado via API

---

## 2. Estado Anterior

| Componente | Estado antes do P5.5 |
|-----------|----------------------|
| `ReleaseBaseline` | Entidade persistida, criação manual via `RecordReleaseBaseline` |
| `ObservationWindow` | Entidade existente, mas **nenhum handler a criava ou populava** |
| `PostReleaseReview` | Entidade existente, criação manual via `StartPostReleaseReview` |
| Comparação automática | **Inexistente** — nenhum serviço comparava baseline vs observado |
| `RecordObservationMetrics` | **Inexistente** |
| `GetPostReleaseReview` | **Inexistente** — apenas acesso indireto via `GetChangeIntelligenceSummary` |
| `IPostChangeVerificationService` | **Inexistente** |

---

## 3. Modelo de Baseline e Observação Adotado

### Campos do Baseline (já existentes)

| Campo | Descrição |
|-------|-----------|
| `RequestsPerMinute` | Throughput médio antes do deploy |
| `ErrorRate` | Taxa de erro média (0.0–1.0) |
| `AvgLatencyMs` | Latência média em ms |
| `P95LatencyMs` | Latência P95 em ms |
| `P99LatencyMs` | Latência P99 em ms |
| `Throughput` | Bytes/segundo médio |
| `CollectedFrom/To` | Período da coleta |

### Fases de Observação (`ObservationPhase`)

```
BaselineCollection (0) → InitialObservation (1) → PreliminaryReview (2) → ConsolidatedReview (3) → FinalReview (4)
```

### Modelo de Comparação (PostChangeVerificationService)

| Sinal | Degradação grave (Negative) | Degradação leve (NeedsAttention) | Melhoria (Positive) |
|-------|---------------------------|----------------------------------|---------------------|
| ErrorRate | Δ > +100% relativo | Δ > +30% relativo | Δ < -10% |
| AvgLatency | Δ > +30% relativo | Δ > +15% relativo | Δ < -10% |
| P95Latency | Δ > +30% relativo | Δ > +15% relativo | — |

### Confidence Score por Fase

| Fase | Base | Com consistência |
|------|------|-----------------|
| InitialObservation | 0.30 | 0.40 |
| PreliminaryReview | 0.60 | 0.70 |
| ConsolidatedReview | 0.80 | 0.90 |
| FinalReview | 0.90 | 1.00 |

---

## 4. Pipeline Automático (pós-P5.5)

```
POST /api/v1/releases/{id}/baseline  (RecordReleaseBaseline — já existia)
    └─► ReleaseBaseline criado com indicadores pré-deploy

POST /api/v1/releases/{id}/observation-window  (RecordObservationMetrics — NOVO)
    └─► RecordObservationMetrics.Handler
            ├─ Valida Release e Baseline (baseline obrigatório)
            ├─ Cria ou obtém ObservationWindow para a fase
            ├─ ObservationWindow.RecordMetrics(observedMetrics)
            ├─ PostChangeVerificationService.Compare(baseline, window, phase)
            │   └─► VerificationResult { Outcome, ConfidenceScore, Summary, Deltas }
            ├─ Se review nova → PostReleaseReview.Start() + RecordInitialObservation()
            └─ Se review existente → PostReleaseReview.Progress(newPhase, outcome, ...)

GET /api/v1/releases/{id}/review  (GetPostReleaseReview — NOVO)
    └─► Response { ReviewId, Phase, Outcome, ConfidenceScore, Summary, ObservationWindows[], Baseline }
```

---

## 5. Ficheiros Alterados

### Domínio

| Ficheiro | Alteração |
|----------|-----------|
| `PostReleaseReview.cs` | Novo método `RecordInitialObservation()` — permite registar o primeiro resultado sem avançar de fase |
| `ChangeIntelligenceErrors.cs` | Novo erro `BaselineNotFound` |

### Application

| Ficheiro | Alteração |
|----------|-----------|
| `IPostChangeVerificationService.cs` | **Novo** — abstração do serviço de verificação + `VerificationResult` record |
| `PostChangeVerificationService.cs` | **Novo** — implementação determinística: 3 sinais × 2 thresholds × confidence por fase |
| `RecordObservationMetrics/RecordObservationMetrics.cs` | **Novo** — Command + Validator + Handler (pipeline completo) |
| `GetPostReleaseReview/GetPostReleaseReview.cs` | **Novo** — Query + Validator + Handler + Response com janelas e baseline |
| `IPostReleaseReviewRepository.cs` | Novo método `Update()` |
| `DependencyInjection.cs` | `IPostChangeVerificationService` registado como Singleton; 2 novos validators registados |

### Infrastructure

| Ficheiro | Alteração |
|----------|-----------|
| `PostReleaseReviewRepository.cs` | Implementação de `Update()` |

### API

| Ficheiro | Alteração |
|----------|-----------|
| `IntelligenceEndpoints.cs` | 2 novos endpoints: `POST /{id}/observation-window` + `GET /{id}/review` |

### Testes

| Ficheiro | Alteração |
|----------|-----------|
| `PostChangeVerificationTests.cs` | **Novo** — 15 testes: 5 `PostChangeVerificationService` + 5 `RecordObservationMetrics` + 5 `GetPostReleaseReview` |

---

## 6. Ligação com Score, Evidence e Release

| Ligação | Como está feita |
|---------|-----------------|
| PostReleaseReview ↔ Release | `ReleaseId` (cross-context Guid reference) |
| ObservationWindow ↔ Release | `ReleaseId` (cross-context Guid reference) |
| ReleaseBaseline ↔ Release | `ReleaseId` (cross-context Guid reference) |
| `GetChangeIntelligenceSummary` | Já consulta `PostReleaseReview` e `ReleaseBaseline` no sumário completo |

---

## 7. Validação

- ✅ 239/239 testes ChangeGovernance passam (incluindo 15 novos `PostChangeVerificationTests`)
- ✅ Compilação sem erros em todos os projetos alterados
- ✅ Pipeline `RecordObservationMetrics` → `PostReleaseReview` validado em testes
- ✅ `PostChangeVerificationService` classificação de outcome validada em 5 cenários
- ✅ Confiança escala corretamente por fase (4 fases testadas)
- ✅ `GetPostReleaseReview` retorna dados com janelas de observação e baseline
