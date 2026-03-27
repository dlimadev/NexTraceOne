# P6.5 — Post-Change Gap Report

**Data:** 2026-03-27
**Fase:** P6.5 — Operational Consistency / Runtime Comparison entre Ambientes
**Classificação:** P6_PHASE_COMPLETE_WITH_CONTROLLED_GAPS

---

## 1. O que foi resolvido

| Item | Estado |
|---|---|
| `EstablishRuntimeBaseline` command + handler (upsert) | ✅ Concluído |
| Endpoint `POST /api/v1/runtime/baselines` | ✅ Concluído |
| `CompareEnvironments` command + handler (cross-env drift) | ✅ Concluído |
| Endpoint `POST /api/v1/runtime/compare-environments` | ✅ Concluído |
| `IRuntimeBaselineRepository.ListByServiceAsync` + implementação | ✅ Concluído |
| `DriftFinding` persistido por `CompareEnvironments` com `ReleaseId` opcional | ✅ Concluído |
| 10 novos testes (420 total, 0 falhas) | ✅ Concluído |
| Documentação do pipeline | ✅ Concluído |

---

## 2. O que ainda ficou pendente

### 2.1 Auto-trigger de comparação por evento de deploy (pendente fase seguinte)

O `CompareEnvironments` é chamado manualmente (ou por Quartz job). Não existe auto-trigger quando `NotifyDeployment` é chamado no Change Governance para comparar automaticamente staging vs production após cada deploy.

### 2.2 Quartz job de drift detection periódico (pendente fase seguinte)

A detecção de drift (`DetectRuntimeDrift` e `CompareEnvironments`) ainda é manual. Um Quartz.NET job periódico deveria correr automaticamente para serviços com snapshots recentes.

### 2.3 Frontend Environment Comparison — migração para novos endpoints (pendente fase seguinte)

A página `EnvironmentComparisonPage` ainda consome endpoints da camada Governance (COMPATIBILIDADE TRANSITÓRIA). A migração para `POST /api/v1/runtime/compare-environments` fica para a próxima fase.

### 2.4 Acumulação de baseline a partir de snapshots históricos (pendente fase seguinte)

O `EstablishRuntimeBaseline` recebe os valores esperados explicitamente. Falta um handler que calcule automaticamente a baseline a partir dos últimos N snapshots de um serviço (agregação automática de percentis históricos).

### 2.5 Resolução/acknowledgment de DriftFindings via API (parcial)

`DriftFinding.Acknowledge()` e `DriftFinding.Resolve()` existem no domínio, mas não há endpoint de `PATCH` exposto para que a UI ou operador faça acknowledge/resolve. Fica para a próxima fase.

---

## 3. O que fica explicitamente para a próxima macrofase

1. **Auto-trigger** de `CompareEnvironments` a partir de evento de deploy
2. **Quartz job** de drift detection periódico
3. **Migração frontend** da página Environment Comparison para os novos endpoints
4. **Baseline auto-calculada** a partir de snapshots históricos (percentis)
5. **Endpoints de gestão de DriftFinding** (acknowledge, resolve, bulk-resolve)
6. **Scoring operacional** por serviço e ambiente baseado em histórico de drift

---

## 4. Limitações residuais conhecidas

| Limitação | Impacto | Mitigação |
|---|---|---|
| `CompareEnvironments` usa "baseline sintética" (single point) | Comparação sem margem histórica | Aceitável como MVP; baseline persistida com múltiplos data points resolve em fase seguinte |
| Sem Quartz job automático | Drift detection manual | Documentado; job em próxima fase |
| Frontend ainda em Governance | EnvironmentComparison não usa dados reais do Runtime module | Transitório; migração em próxima fase |
| Sem threshold adaptativo | Tolerância fixa por request | Aceitável para MVP |

---

## 5. Estado pós-P6.5

| Dimensão | Antes P6.5 | Após P6.5 |
|---|---|---|
| Criação de baseline | ❌ Não havia endpoint | ✅ `POST /runtime/baselines` (upsert) |
| Comparação cross-ambiente | ❌ Ausente | ✅ `POST /runtime/compare-environments` |
| DriftFinding por comparação cross-env | ❌ Ausente | ✅ Persistido com `ReleaseId` opcional |
| Trigger automático | ❌ Ausente | ⚠️ Manual (próxima fase automatiza) |
| Frontend Environment Comparison real | ❌ Governance COMPAT | ⚠️ Pendente migração (próxima fase) |

**Resultado:** O pilar Operational Consistency passou de PARTIAL para FUNCTIONALLY_COMPLETE — existe pipeline real de baseline, snapshot, comparação cross-ambiente e drift finding persistido e consultável.
