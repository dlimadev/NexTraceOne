# P5.5 — Post-Change Gap Report: Post-Change Verification with Baseline

**Data:** 2026-03-26  
**Fase:** P5.5 — Post-Change Verification Automatizada com Baseline

---

## 1. O que foi resolvido nesta fase

| Gap | Estado |
|-----|--------|
| `ObservationWindow` sem pipeline de preenchimento | ✅ Resolvido — `RecordObservationMetrics` cria e popula janelas |
| Comparação baseline vs observado inexistente | ✅ Resolvido — `PostChangeVerificationService` implementado |
| `PostReleaseReview` apenas criado manualmente | ✅ Resolvido — auto-criado e progredido por `RecordObservationMetrics` |
| `GetPostReleaseReview` inexistente como endpoint dedicado | ✅ Resolvido — `GET /{id}/review` disponível |
| `IPostReleaseReviewRepository.Update()` faltando | ✅ Resolvido |
| `BaselineNotFound` error missing | ✅ Resolvido |
| 239 testes passando | ✅ Validado |

---

## 2. O que ainda fica pendente após P5.5

### Automação completa sem interação humana

| Item pendente | Descrição |
|---------------|-----------|
| Chamada automática de `RecordObservationMetrics` | O operador/CI ainda deve chamar `POST /observation-window` manualmente. A automação via job agendado (Quartz.NET) ficou fora do escopo desta fase. |
| Quartz job `PostChangeVerificationJob` | Um job que poleia observações pendentes e chama `RecordObservationMetrics` automaticamente está pendente para fase seguinte. |

### Coleta de métricas reais de telemetria

| Item pendente | Descrição |
|---------------|-----------|
| Integração com ClickHouse/OTel | `RecordObservationMetrics` recebe métricas como input externo. A consulta automática ao ClickHouse para preencher as métricas observadas (otel_traces, ops_runtime_metrics) não foi implementada nesta fase. |
| Baseline automático de OTel | `RecordReleaseBaseline` também recebe métricas como input. A captura automática de baseline via OTel Collector fica para fase seguinte. |

### Integração com EvidencePack

| Item pendente | Descrição |
|---------------|-----------|
| `EvidencePack.PostReleaseOutcome` | O resultado da verificação pós-mudança poderia enriquecer automaticamente o `EvidencePack` (P5.4). Esta ligação está pendente. |

### Score recalculation pós-verificação

| Item pendente | Descrição |
|---------------|-----------|
| `ChangeIntelligenceScore` atualizado com resultado da review | Quando uma review pós-release classifica `Negative`, o score de confiança poderia ser ajustado. Esta ligação fica para a macrofase seguinte. |

---

## 3. O que fica explicitamente para a próxima macrofase

- **Quartz job de verificação automática**: `PostChangeVerificationJob` que executa `RecordObservationMetrics` com dados reais de ClickHouse/OTel ao fim de cada `ObservationWindow.EndsAt`
- **Rollback intelligence guiado**: usar `PostReleaseReview.Outcome == Negative` como trigger para `AssessRollbackViability`
- **Alertas automáticos**: quando outcome é `Negative` ou `NeedsAttention`, emitir evento para notificações
- **Integração EvidencePack**: enriquecer automaticamente o `EvidencePack` com o resultado da review
- **Dashboard de verificação pós-release**: UI expandida com comparação baseline vs observado por métrica

---

## 4. Limitações residuais

1. **`RecordObservationMetrics` ainda requer chamada explícita**: não existe automação temporal que invoque o pipeline no momento certo da janela de observação.

2. **Métricas manuais**: as métricas observadas são passadas pelo chamador, não extraídas automaticamente do stack de telemetria. O pipeline é correto mas o "ingestão" ainda é externo.

3. **`PostReleaseReview.RecordInitialObservation()`**: este método permite actualizar a revisão quando ainda está na fase `InitialObservation` sem avançar a fase — é correto para o primeiro sinal de observação, mas é limitado a esse caso específico.
