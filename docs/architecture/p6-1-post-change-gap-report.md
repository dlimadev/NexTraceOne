# P6.1 — Post-Change Gap Report

**Data:** 2026-03-26
**Fase:** P6.1 — Expandir o ReliabilityDbContext para SLO, SLA, error budget e burn rate
**Classificação:** P6_PHASE_COMPLETE_WITH_CONTROLLED_GAPS

---

## 1. O que foi resolvido

| Item | Estado |
|---|---|
| ReliabilityDbContext expandido com 4 novos DbSets | ✅ Concluído |
| Entidades de domínio reais para SLO, SLA, ErrorBudget, BurnRate | ✅ Concluído |
| Enums tipados: SloType, SloStatus, SlaStatus, BurnRateWindow | ✅ Concluído |
| Mappings EF Core com relações, índices e auditoria | ✅ Concluído |
| Migração EF Core gerada (P6_1_SloSlaBudgetBurnRate) | ✅ Concluído |
| Repositórios e abstrações para os 4 novos agregados | ✅ Concluído |
| Wiring DI completo (repositórios + validators) | ✅ Concluído |
| Handlers CQRS mínimos (RegisterSlo, RegisterSla, GetErrorBudget, GetBurnRate) | ✅ Concluído |
| Endpoints REST para SLO/SLA/ErrorBudget/BurnRate | ✅ Concluído |
| 27 novos testes unitários (domínio + handlers) | ✅ Concluído |
| 350 testes totais sem falhas | ✅ Concluído |
| Documentação de expansão | ✅ Concluído |

---

## 2. O que ainda está pendente

### 2.1 Cálculo real de error budget (pendente P6.2)

O `ErrorBudgetSnapshot` existe como entidade persistida mas o seu preenchimento depende de:
- dados reais de telemetria ou observabilidade
- um job recorrente (Quartz.NET) que computa o budget com base na janela definida no SLO

Actualmente, os snapshots de error budget têm de ser injectados externamente (via API ou job) — não existem ainda regras de negócio que calculem automaticamente `consumedBudgetMinutes` a partir de métricas.

**Fica para P6.2.**

### 2.2 Cálculo real de burn rate (pendente P6.2)

De forma análoga ao error budget, o `BurnRateSnapshot` é uma estrutura persistida aguardando ingestão de valores calculados a partir da telemetria real. O modelo de cálculo (observedErrorRate / toleratedErrorRate) está implementado na entidade de domínio, mas o fluxo de ingestão automática não existe ainda.

**Fica para P6.2.**

### 2.3 Alertas automáticos de violação (fora de escopo P6.1)

Quando um SLO entra em estado `Violated` ou `AtRisk`, não existe ainda disparo de evento, notificação ou alerta automático.

**Fica para fases posteriores (P6.3+).**

### 2.4 Integração com ClickHouse (fora de escopo P6.1)

Para cálculos eficientes de burn rate e error budget em produção, a stack analítica com ClickHouse será o armazenamento preferencial para telemetria. A integração entre os cálculos de SLO e o ClickHouse está fora do escopo desta fase.

**Fica para fases posteriores.**

### 2.5 Dashboard e visualização de SLO/SLA (fora de escopo P6.1)

Não existe ainda UI específica para SLO/SLA/ErrorBudget/BurnRate. O frontend de reliability existente não foi expandido nesta fase.

**Fica para P6.2 ou fase dedicada de frontend.**

### 2.6 Leitura de SLOs activos por serviço na camada de GetServiceReliabilityDetail (pendente P6.2)

O handler existente `GetServiceReliabilityDetail` não integra ainda as definições de SLO do `ReliabilityDbContext` — exibe reliability score calculado em memória mas não menciona SLOs definidos, error budget ou burn rate.

**Fica para P6.2.**

### 2.7 Aprovação e lifecycle de SLO (fora de escopo P6.1)

Não existe workflow de aprovação para criação/alteração de SLOs. A criação directa via `RegisterSloDefinition` não inclui mecanismo de review ou aprovação.

**Fica para fases posteriores.**

---

## 3. Limitações residuais conhecidas

| Limitação | Impacto | Mitigação |
|---|---|---|
| Sem cálculo automático de error budget | Budget sempre nulo até ser injectado externamente | API permite ingestão manual; job será adicionado na P6.2 |
| Sem cálculo automático de burn rate | Burn rate nulo até ingestão externa | Idem |
| GetServiceReliabilityDetail não mostra SLOs | Visão de detalhe incompleta para P6.1 | Aceite como gap controlado; será integrado na P6.2 |
| Sem UI dedicada para SLO/SLA | Feature não visível no frontend | Endpoints disponíveis; frontend será adicionado na P6.2 |

---

## 4. O que fica explicitamente para P6.2

1. **Job de cálculo automático de error budget** — Quartz.NET, periodicidade configurável
2. **Job de cálculo automático de burn rate** — por janela (1h, 6h, 24h, 7d)
3. **Integração de SLO no GetServiceReliabilityDetail** — mostrar SLOs activos, budget e burn rate na visão de detalhe
4. **Endpoint de listagem de SLOs** — `GET /api/v1/reliability/services/{serviceId}/slos`
5. **Endpoint de listagem de SLAs** — `GET /api/v1/reliability/slos/{sloId}/slas`
6. **Frontend básico de SLO/SLA** — widget no detalhe do serviço mostrando estado do budget
7. **Correlação SLO + change** — quando um SLO entra em AtRisk/Violated, correlacionar com changes recentes

---

## 5. Classificação de estado pós-P6.1

| Dimensão | Antes P6.1 | Após P6.1 |
|---|---|---|
| Modelo persistido de SLO | ❌ Ausente | ✅ Presente |
| Modelo persistido de SLA | ❌ Ausente | ✅ Presente |
| Base para error budget | ❌ Ausente | ✅ Estrutura persistida |
| Base para burn rate | ❌ Ausente | ✅ Estrutura persistida |
| Cálculo automático de budget | ❌ Ausente | ⚠️ Pendente (P6.2) |
| UI para SLO/SLA | ❌ Ausente | ⚠️ Pendente (P6.2) |
| Alertas de violação | ❌ Ausente | ⚠️ Pendente (P6.3+) |

**Resultado:** O subdomínio Reliability passou de PARTIAL (sem base real para SLO/SLA) para BASE_ESTABLISHED (base persistida real, pronta para evolução).
