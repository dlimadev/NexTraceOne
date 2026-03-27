# P6.3 — Post-Change Gap Report

**Data:** 2026-03-27
**Fase:** P6.3 — Pipeline de ingestão de custo / FinOps
**Classificação:** P6_PHASE_COMPLETE_WITH_CONTROLLED_GAPS

---

## 1. O que foi resolvido

| Item | Estado |
|---|---|
| Bug `ComputeCostTrend`: CostTrend agora persistido via ICostTrendRepository | ✅ Corrigido |
| `ICostTrendRepository` interface + `CostTrendRepository` implementação | ✅ Concluído |
| `ICostImportBatchRepository.ListAsync` | ✅ Concluído |
| `ListCostImportBatches` query + handler | ✅ Concluído |
| `GetCostRecordsByService` query + handler | ✅ Concluído |
| `CreateServiceCostProfile` command + handler (idempotente) | ✅ Concluído |
| 4 novos endpoints API (GET /import, GET /records, POST/GET /profiles) | ✅ Concluído |
| ICostTrendRepository registado no DI | ✅ Concluído |
| 10 novos testes (397 total, 0 falhas) | ✅ Concluído |

---

## 2. O que ainda ficou pendente

### 2.1 Reconciliação automática CostRecord → CostSnapshot (pendente P6.4)

Quando um batch é importado via `POST /import`, os `CostRecords` são persistidos mas não geram automaticamente um `CostSnapshot`. Para que `GetCostReport` e `ComputeCostTrend` incluam dados do batch é preciso gerar o snapshot correspondente (ou fazer queries diretas sobre CostRecords).

**Fica para P6.4.**

### 2.2 Job automático de reset mensal do ServiceCostProfile (pendente P6.4)

O método `ServiceCostProfile.ResetMonthlyCycle()` existe mas não há um Quartz.NET job que o invoque automaticamente na virada do mês.

**Fica para P6.4.**

### 2.3 Correlação custo ↔ equipa / domínio / mudança (pendente P6.4+)

O `CostRecord` já tem os campos `Team` e `Domain`, mas não existem ainda handlers de query que agreguem por equipa ou domínio a partir de CostRecords. As views de Team FinOps e Domain FinOps no frontend continuam a usar dados da camada Governance (COMPATIBILIDADE TRANSITÓRIA).

**Fica para P6.4.**

### 2.4 Integração completa frontend FinOps ↔ Cost module (pendente P6.4)

O frontend usa `/finops/summary`, `/finops/services/{id}` etc. da camada Governance. Estes endpoints precisam de ser migrados para consumir dados reais do Cost module (CostRecord, CostSnapshot, ServiceCostProfile) em vez de dados seed/mock.

**Fica para P6.4.**

### 2.5 Integração com provedores cloud reais (fora de escopo P6)

A ingestão via `POST /import` aceita dados de qualquer fonte (campo `Source`). A ligação directa com SDKs de AWS Cost Explorer, Azure Cost Management ou GCP Billing API está fora do escopo desta fase.

**Fica para fase posterior.**

---

## 3. Limitações residuais conhecidas

| Limitação | Impacto | Mitigação |
|---|---|---|
| CostRecord não gera CostSnapshot automaticamente | GetCostReport não inclui dados importados via batch | Ingestão dual: usar também POST /snapshots; ou P6.4 reconcilia |
| GetCostRecordsByService pagina em memória | Performance limitada com volumes muito grandes | Repositório retorna apenas registos do serviço/período; aceitável para P6.3 |
| Sem reset automático de custo mensal | AlertCostAnomaly pode acumular custo incorrectamente | Reset manual possível; job em P6.4 |
| Frontend Governance endpoints ainda activos | Dados podem divergir entre /finops e /api/v1/cost | Transitório; P6.4 migra |

---

## 4. O que fica explicitamente para P6.4

1. **Auto-reconciliação** CostImportBatch → CostSnapshot (ao importar batch, gerar ou atualizar snapshots correspondentes)
2. **Quartz.NET job** para reset mensal de `ServiceCostProfile.CurrentMonthCost`
3. **Handlers de query por equipa e domínio** usando CostRecord (para alimentar Team FinOps e Domain FinOps)
4. **Migração do frontend** de `/finops/*` para `/api/v1/cost/*`
5. **Endpoint `GET /api/v1/cost/summary`** — agregação executiva por serviço/ambiente/período

---

## 5. Classificação de estado pós-P6.3

| Dimensão | Antes P6.3 | Após P6.3 |
|---|---|---|
| Pipeline de ingestão batch (CostImportBatch + CostRecord) | ✅ Presente | ✅ Funcional e consultável |
| Pipeline de ingestão por snapshot | ✅ Presente | ✅ Funcional |
| Persistência de CostTrend | ❌ Bug (não persistia) | ✅ Corrigido |
| Listagem de batches importados | ❌ Ausente | ✅ Presente |
| Consulta de CostRecords por serviço | ❌ Ausente | ✅ Presente |
| Criação de ServiceCostProfile | ❌ Ausente | ✅ Presente (idempotente) |
| Correlação custo → equipa/domínio | ⚠️ Campo existe no CostRecord | ⚠️ Pendente handlers agregados (P6.4) |
| Integração frontend FinOps | ❌ Governance COMPAT | ⚠️ Pendente migração (P6.4) |
| Auto-reconciliação batch → snapshot | ❌ Ausente | ⚠️ Pendente (P6.4) |

**Resultado:** O subdomínio Cost passou de PARTIAL_WITH_BUG para PIPELINE_OPERATIONAL — com ingestão real persistida, batch history consultável, cost records por serviço e perfis de custo funcionais.
