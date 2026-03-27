# P6.4 — Post-Change Gap Report

**Data:** 2026-03-27
**Fase:** P6.4 — Ligar custo a equipa, domínio e mudança
**Classificação:** P6_PHASE_COMPLETE_WITH_CONTROLLED_GAPS

---

## 1. O que foi resolvido

| Item | Estado |
|---|---|
| `GetCostRecordsByTeam` query + handler + endpoint | ✅ Concluído |
| `GetCostRecordsByDomain` query + handler + endpoint | ✅ Concluído |
| `CostRecord.ReleaseId?` (campo de correlação com release) | ✅ Concluído |
| `CostRecord.AssignRelease()` + `ClearRelease()` | ✅ Concluído |
| `ICostRecordRepository.ListByReleaseAsync` + implementação | ✅ Concluído |
| `GetCostRecordsByRelease` query + handler + endpoint | ✅ Concluído |
| `EnrichCostRecordWithRelease` command + handler + endpoint | ✅ Concluído |
| `CostIntelligenceErrors.NoRecordsForRelease` + `RecordNotFound` | ✅ Concluído |
| EF migration `P6_4_CostRecord_ReleaseId` | ✅ Concluído |
| 13 novos testes (410 total, 0 falhas) | ✅ Concluído |

---

## 2. O que ainda ficou pendente

### 2.1 Enriquecimento automático no momento da ingestão (pendente P6.5)

O `EnrichCostRecordWithRelease` é um endpoint que deve ser chamado manualmente ou por um job após a ingestão. Não existe ainda integração automática: quando uma release é criada no Change Governance, os CostRecords correspondentes não são enriquecidos automaticamente.

**Fica para P6.5.**

### 2.2 Consumo de evento de deploy para correlação automática (pendente P6.5)

Quando `NotifyDeployment` é chamado no Change Governance, seria ideal publicar um evento que o módulo Operational Intelligence consumisse para auto-enriquecer os CostRecords do serviço/ambiente/período. Esta integração via Integration Events não foi implementada.

**Fica para P6.5.**

### 2.3 Frontend Team FinOps e Domain FinOps ainda usam Governance endpoints (pendente P6.5)

As páginas `TeamFinOpsPage` e `DomainFinOpsPage` consomem `/finops/teams/{id}` e `/finops/domains/{id}` do módulo Governance (COMPATIBILIDADE TRANSITÓRIA). A migração para `/api/v1/cost/records/by-team` e `/api/v1/cost/records/by-domain` ainda não foi feita.

**Fica para P6.5.**

### 2.4 Paginação no servidor para GetCostRecordsByTeam/Domain (pendente P6.5)

As queries `GetCostRecordsByTeam` e `GetCostRecordsByDomain` fazem paginação em memória após carregar todos os registos do repositório. Para volumes grandes, deveria ser paginação a nível de SQL.

**Fica para P6.5 (optimização de performance).**

### 2.5 Chargeback/showback por equipa e domínio (fora de escopo P6)

O produto tem os dados para suportar chargeback/showback, mas o cálculo alocado, relatórios de billing por equipa com aprovação e exportação contabilística estão fora do escopo da Fase 6.

**Fica para fase posterior.**

---

## 3. O que fica explicitamente para P6.5

1. **Auto-enriquecimento de ReleaseId** via Integration Event de deploy/release
2. **Migração frontend** Team FinOps + Domain FinOps para usar Cost module endpoints
3. **Paginação servidor** nos queries by-team e by-domain
4. **`ImportCostBatch` opcional com ReleaseId** — poder incluir `ReleaseId` directamente no lote de importação
5. **Snapshot de equipa e domínio** — gerar `CostSnapshot` ou estrutura equivalente agregada por equipa/domínio

---

## 4. Limitações residuais conhecidas

| Limitação | Impacto | Mitigação |
|---|---|---|
| `EnrichCostRecordWithRelease` é manual | Correlação release→custo requer chamada explícita | Documentado; auto-correlação em P6.5 |
| Correlação por string (Team/Domain) sem validação de existência | Equipa ou domínio inexistente aceite sem erro | Custo de isolamento DDD; aceitável no MVP |
| Frontend FinOps ainda em Governance | Team/Domain FinOps não usam dados reais de cost module | Transitório; P6.5 migra |
| Paginação em memória | Performance limitada com muitos registos por equipa | Aceitável para volumes MVP; P6.5 pagina no SQL |

---

## 5. Classificação de estado pós-P6.4

| Dimensão | Antes P6.4 | Após P6.4 |
|---|---|---|
| Consulta custo por equipa | ❌ Ausente | ✅ `GET /records/by-team` |
| Consulta custo por domínio | ❌ Ausente | ✅ `GET /records/by-domain` |
| Correlação custo ↔ release | ❌ Ausente | ✅ `ReleaseId` persistido + endpoints |
| Auto-enriquecimento | ❌ Ausente | ⚠️ Manual (P6.5 automatiza) |
| Frontend Team/Domain FinOps real | ❌ Governance COMPAT | ⚠️ Pendente migração (P6.5) |

**Resultado:** O subdomínio Cost passou de PIPELINE_OPERATIONAL para CONTEXTUALLY_CORRELATED — custos podem ser rastreados até equipa, domínio e release de forma persistida e consultável.
