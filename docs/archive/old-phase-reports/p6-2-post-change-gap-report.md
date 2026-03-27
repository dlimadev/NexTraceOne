# P6.2 — Post-Change Gap Report

**Data:** 2026-03-26
**Fase:** P6.2 — Implementar cálculo real de error budget e burn rate
**Classificação:** P6_PHASE_COMPLETE_WITH_CONTROLLED_GAPS

---

## 1. O que foi resolvido

| Item | Estado |
|---|---|
| IErrorBudgetCalculator — interface de domínio para cálculo | ✅ Concluído |
| ErrorBudgetCalculator — implementação com fórmulas reais | ✅ Concluído |
| ComputeErrorBudget command + handler — pipeline real de cálculo | ✅ Concluído |
| ComputeBurnRate command + handler — cálculo por janela(s) | ✅ Concluído |
| Ligação a IReliabilityRuntimeSurface (dados reais observados) | ✅ Concluído |
| Persistência de ErrorBudgetSnapshot e BurnRateSnapshot | ✅ Concluído |
| ListServiceSlos query + handler | ✅ Concluído |
| ListSloSlas query + handler | ✅ Concluído |
| 4 novos endpoints API | ✅ Concluído |
| IErrorBudgetCalculator registado no DI (Singleton) | ✅ Concluído |
| 37 novos testes (387 total, 0 falhas) | ✅ Concluído |
| Documentação do cálculo | ✅ Concluído |

---

## 2. O que ainda ficou pendente

### 2.1 Cálculo por janela histórica real (pendente P6.3)

O `observed_error_rate` é actualmente obtido do **snapshot mais recente** do RuntimeSnapshot (ponto-a-ponto). Para janelas de 7 dias ou 24 horas, o cálculo ideal seria a média ponderada dos snapshots na janela completa.

Esta limitação está documentada no código. O valor actual representa a taxa de erros no momento do cálculo — não a taxa média acumulada na janela.

**Fica para P6.3 com integração ClickHouse ou agregação PostgreSQL.**

### 2.2 Job automático de cálculo periódico (pendente P6.3)

Os endpoints `compute-error-budget` e `compute-burn-rate` precisam de ser invocados explicitamente. Não existe ainda um job Quartz.NET que execute o cálculo periodicamente (ex: de hora a hora, diariamente).

**Fica para P6.3.**

### 2.3 Integração com GetServiceReliabilityDetail (pendente P6.3)

O handler `GetServiceReliabilityDetail` não incorpora ainda o estado de SLO/budget/burn-rate na resposta. A visão de detalhe de confiabilidade não exibe os SLOs activos nem o estado do error budget.

**Fica para P6.3.**

### 2.4 Frontend dedicado para SLO/SLA/ErrorBudget/BurnRate (pendente P6.3)

Não existe widget ou página dedicada no frontend para visualizar SLOs, estado de budget e burn rate por serviço.

**Fica para P6.3 ou fase dedicada de frontend.**

### 2.5 Alertas automáticos de violação (fora de escopo P6.2)

Quando um SLO entra em `Violated` não existe ainda disparo de evento, notificação ou alerta automático.

**Fica para P6.4+.**

### 2.6 Integração ClickHouse para cálculo analítico eficiente (fora de escopo P6.2)

Para cálculos de burn rate sobre janelas longas (7 dias), o armazenamento analítico ClickHouse seria mais eficiente que o PostgreSQL.

**Fica para fases posteriores.**

---

## 3. Limitações residuais conhecidas

| Limitação | Impacto | Mitigação |
|---|---|---|
| ErrorRate do snapshot actual aplicado a toda a janela | Sobreestima consumo durante picos; subestima em períodos tranquilos | Documentado; P6.3 resolverá com média da janela |
| Sem job automático | Snapshots gerados apenas on-demand | API permite cálculo manual; job em P6.3 |
| GetServiceReliabilityDetail sem SLO/budget | Visão incompleta no detalhe do serviço | Endpoints específicos disponíveis |
| Sem UI dedicada | Feature não visível no frontend | APIs funcionais; frontend em P6.3 |

---

## 4. O que fica explicitamente para P6.3

1. **Job Quartz.NET** para cálculo periódico (ComputeErrorBudget + ComputeBurnRate para todos os SLOs activos)
2. **Cálculo por janela histórica real** — média de ErrorRate sobre snapshots na janela (PostgreSQL agregação ou ClickHouse)
3. **Integração GetServiceReliabilityDetail** — incluir SLOs activos, estado de budget e burn rate na resposta
4. **Widget SLO no frontend** — estado do budget e burn rate na visão do serviço
5. **Endpoint GetSloDetail** — detalhe completo de um SLO com histórico de budget e burn rate

---

## 5. Classificação de estado pós-P6.2

| Dimensão | Antes P6.2 | Após P6.2 |
|---|---|---|
| Modelo persistido de SLO/SLA/Budget/BurnRate | ✅ Presente (P6.1) | ✅ Presente |
| Cálculo real de error budget | ❌ Ausente | ✅ Presente — pipeline real |
| Cálculo real de burn rate | ❌ Ausente | ✅ Presente — multi-janela |
| Ligação a dados observados (RuntimeSnapshot) | ❌ Ausente | ✅ Presente |
| Listagem de SLOs por serviço | ❌ Ausente | ✅ Presente |
| Listagem de SLAs por SLO | ❌ Ausente | ✅ Presente |
| Cálculo por janela histórica completa | ❌ Ausente | ⚠️ Pendente (P6.3) |
| Job automático de cálculo | ❌ Ausente | ⚠️ Pendente (P6.3) |
| UI para SLO/Budget/BurnRate | ❌ Ausente | ⚠️ Pendente (P6.3) |

**Resultado:** O subdomínio Reliability passou de BASE_ESTABLISHED (P6.1) para CALCULATION_OPERATIONAL — com cálculo real e rastreável a partir de dados observados.
