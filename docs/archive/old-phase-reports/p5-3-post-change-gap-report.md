# P5.3 — Post-Change Gap Report: ChangeIntelligenceScore Automation

**Data:** 2026-03-26  
**Fase:** P5.3 — Change Intelligence: Score Automático e Rastreável

---

## 1. O que foi resolvido nesta fase

| Gap | Estado |
|-----|--------|
| Score dependente de cálculo manual | ✅ Resolvido — score calculado automaticamente em `NotifyDeployment` |
| BlastRadius não influenciava o score | ✅ Resolvido — `CalculateBlastRadius` recalcula o score com blast radius real |
| Score sem rastreabilidade de origem | ✅ Resolvido — campo `ScoreSource` em `ChangeIntelligenceScore` |
| Fatores do score opacos | ✅ Resolvido — `BreakingChangeWeight`, `BlastRadiusWeight`, `EnvironmentWeight` explícitos e consultáveis via `GetChangeScore` |
| `IChangeScoreCalculator` abstração | ✅ Criada |
| `ChangeScoreCalculator` implementação | ✅ Criada com 3 fatores determinísticos |
| Testes cobrindo cálculo automático | ✅ 6 novos testes `ChangeScoreCalculatorTests` |
| Compilação e 221 testes passando | ✅ Validado |

---

## 2. O que ainda fica pendente após P5.3

### Score inicial vs. score com blast radius

| Item pendente | Descrição |
|---------------|-----------|
| Dois scores na tabela por release | Quando `CalculateBlastRadius` é chamado, um **novo** `ChangeIntelligenceScore` é adicionado em vez de atualizar o existente (estratégia append). Consulta via `GetByReleaseIdAsync` retorna o último. Para consistência total, uma estratégia de upsert poderia ser mais limpa. |
| Score histórico consultável | Não há endpoint para listar todos os scores de uma release ao longo do tempo. |

### Cálculo automático de blast radius

| Item pendente | Descrição |
|---------------|-----------|
| `CalculateBlastRadius` ainda manual | O blast radius ainda requer chamada explícita. A derivação automática a partir de traces OTel (`chg_trace_release_mapping`) foi documentada em P5.2 mas não implementada ainda. |
| Blast radius a partir de telemetria real | Usar `chg_trace_release_mapping` + `nextraceone_obs.otel_traces` para derivar consumidores reais em vez de lista manual fica para P5.4. |

### Validação pós-mudança

| Item pendente | Descrição |
|---------------|-----------|
| Score recalculado após observation window | O `ObservationWindow` existe mas ainda não dispara recálculo automático de score. |
| Post-change verification | A comparação de métricas pré/pós-deploy para enriquecer ou corrigir o score fica para P5.4. |

---

## 3. O que fica explicitamente para P5.4

- **Blast radius automático via telemetria**: usar `chg_trace_release_mapping` para derivar
  `DirectConsumers` e `TransitiveConsumers` a partir de traces reais, eliminando a lista manual.
- **Score recalculado via `ObservationWindow`**: quando a janela de observação fecha, recalcular
  o score com métricas de runtime (error rate, latência P95) para validar se o deploy foi saudável.
- **Post-change verification automática**: comparar `ReleaseBaseline` com métricas pós-deploy
  para validar `ConfidenceStatus` da release e influenciar o score.
- **Upsert de score**: em vez de inserir um novo score por cada recálculo, atualizar o existente
  para manter apenas o score mais recente por release (opcional).
- **Listagem histórica de scores por release**: endpoint que mostra evolução do score ao longo do
  ciclo de vida da release.

---

## 4. Limitações residuais

1. **`ChangeLevel` é `Operational` por padrão**: novas releases criadas via `NotifyDeployment`
   têm `ChangeLevel.Operational` até que `ClassifyChangeLevel` seja chamado. O score inicial
   reflete isso com `BreakingChangeWeight = 0.0`. Isso é correto mas deve ser documentado
   como "score preliminar" até a classificação ser feita.

2. **`BlastRadiusWeight = 0.0` no score inicial**: quando o score é calculado na criação da
   release, o blast radius ainda não existe. O `ScoreSource` indica
   `"auto:change_level+environment (blast_radius_pending)"` — operador deve saber que o score
   será atualizado após `CalculateBlastRadius`.

3. **Dados de runtime não usados ainda no score**: `ReleaseBaseline`, `ObservationWindow` e
   `PostReleaseReview` existem no domínio mas não influenciam o score nesta fase.
