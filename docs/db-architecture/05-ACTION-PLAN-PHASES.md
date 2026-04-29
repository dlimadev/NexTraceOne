# 05 — Plano de Acção por Fases

> Cada fase é auto-contida e entregável. As fases são sequenciais mas o trabalho dentro de cada
> fase pode ser paralelizado por módulo.

---

## Visão geral

```
Fase 0   Fase 1              Fase 2          Fase 3              Fase 4
────┬──── ────────────────── ─────────────── ─────────────────── ────────────────
    │     Time-series        Full-text       Decommission PG     Null repos
    │     → Analytics Store  → Analytics     tables              → real readers
    │     (25 tabelas)       Store (5 idx)   (finalize)          (73 repos)
    │
    ▼
  Abstracção + enable
  (2 dias)
────────────────────────────────────────────────────── 4-5 meses total ──────────
```

---

## Fase 0 — Abstracção IAnalyticsStore + Enable ClickHouse

**Duração:** 2 dias  
**Risco:** Baixo  
**Valor:** Alto — desbloqueia tudo o que vem a seguir

### Objectivos

1. Criar o projecto `NexTraceOne.BuildingBlocks.Analytics` com `IAnalyticsStore`, `NullAnalyticsStore`, `AnalyticsQuery`, `AnalyticsStoreOptions`
2. Criar `NexTraceOne.Infrastructure.Analytics.ClickHouse` com `ClickHouseAnalyticsStore`
3. Criar `NexTraceOne.Infrastructure.Analytics.Elasticsearch` com `ElasticsearchAnalyticsStore`
4. Registar `AddAnalyticsStore(configuration)` em todos os módulos que injectam `IAnalyticsStore`
5. Habilitar ClickHouse no docker-compose de desenvolvimento (`Enabled: true`)
6. Verificar que os 73 null repos continuam a compilar (recebem `NullAnalyticsStore` via DI)

### Checklist

```
□ Criar NexTraceOne.BuildingBlocks.Analytics.csproj
□ Implementar IAnalyticsStore, AnalyticsQuery, AnalyticsStoreOptions
□ Implementar NullAnalyticsStore
□ Implementar ServiceCollectionExtensions.AddAnalyticsStore()
□ Criar NexTraceOne.Infrastructure.Analytics.ClickHouse.csproj
□ Implementar ClickHouseAnalyticsStore (Insert + BulkInsert + Query + Count)
□ Criar NexTraceOne.Infrastructure.Analytics.Elasticsearch.csproj
□ Implementar ElasticsearchAnalyticsStore
□ Registar AddAnalyticsStore() em Program.cs ou host startup
□ Habilitar ClickHouse em docker-compose.override.yml
□ Verificar build completo (dotnet build)
□ Verificar health check /health retorna analytics_store healthy
□ Commit + deploy para dev
```

### Ficheiros a criar

```
src/BuildingBlocks/NexTraceOne.BuildingBlocks.Analytics/
src/Infrastructure/NexTraceOne.Infrastructure.Analytics.ClickHouse/
src/Infrastructure/NexTraceOne.Infrastructure.Analytics.Elasticsearch/
build/clickhouse/init-analytics.sql  (schema inicial — já existe parcialmente)
```

---

## Fase 1 — Séries Temporais → Analytics Store

**Duração:** 4–6 semanas  
**Risco:** Médio  
**Valor:** Alto — elimina contenção de I/O no PostgreSQL

### Tabelas a migrar (25 tabelas em 7 módulos)

| Módulo | Tabelas | Semana |
|--------|---------|--------|
| Observability | service_metrics_snapshots, runtime_snapshots, reliability_snapshots, alert_firing_records | S1-S2 |
| AI Knowledge | token_usage_ledger, external_inference_records, model_prediction_samples, benchmark_snapshots | S2-S3 |
| Cost Management | cost_records, burn_rate_snapshots, cost_allocation_events | S3 |
| SLO / Reliability | error_budget_snapshots, sli_measurements, compliance_daily/weekly/monthly | S3-S4 |
| Analytics | analytics_events, dashboard_usage_events, productivity_snapshots | S4-S5 |
| Security | security_events, threat_signals | S5 |
| Developer Prod. | agent_query_records, code_review_cycles, deployment_records, pipeline_run_records | S5-S6 |

### Processo por tabela

```
Dia 1: Criar schema no Analytics Store (ClickHouse SQL ou ES template)
Dia 2: Implementar DTO de mapeamento PG → Analytics
Dia 3: Adicionar dual-write no handler (PG write fica, + analytics write)
Dia 4-5: Deploy dev. Monitorizar contagens PG vs Analytics Store.
Semana 2-3: Migrar readers para Analytics Store.
Semana 4: Validar row counts. Remover write PG. PG table fica como backup.
Semana 5: Drop PG table (criar migration). Remover DTO de dual-write.
```

### Checklist por módulo

```
Observability:
□ Schema ClickHouse criado para 4 tabelas
□ Dual-write activo em RecordMetricsSnapshotHandler
□ ObservabilityAnalyticsReader migrado para IAnalyticsStore
□ PG tables validadas e removidas

AI Knowledge:
□ Schema ClickHouse criado para 4 tabelas
□ Dual-write em RecordTokenUsageHandler, RecordInferenceHandler
□ Readers migrados
□ PG tables removidas

(repetir para cada módulo)
```

---

## Fase 2 — Full-Text Search → Analytics Store

**Duração:** 3–4 semanas  
**Risco:** Médio  
**Valor:** Médio — elimina índices GIN pesados no PostgreSQL

### Entidades afectadas

| Entidade PG | Índice Analytics | Remoção PG |
|-------------|-----------------|------------|
| `aig_knowledge_documents` (campo `content`) | `knowledge_document_content` | Remover índice GIN `gin_knowledge_content` |
| `ctm_contract_versions` (campo `spec_content`) | `contract_version_content` | Remover índice GIN `gin_contract_spec` |
| `inc_incident_records` (campo `description`) | `incident_record_content` | Remover índice GIN `gin_incident_desc` |
| `kb_runbook_records` (campo `content`) | `runbook_record_content` | Remover índice GIN `gin_runbook_content` |
| `aud_audit_events` (campo `details`) | `audit_event_search` | ⚠️ Nunca remover da PG — só adicionar índice no Analytics |

### Nota ClickHouse vs Elasticsearch para full-text

- **ClickHouse:** Usar `INDEX idx_content content TYPE tokenbf_v1(32768, 3, 0)` + `hasToken()` em queries
- **Elasticsearch:** Usar `text` field com `standard` analyser + `match` / `multi_match` query

A interface `IAnalyticsStore` expõe `FullTextQuery` no `AnalyticsQuery` — o provider traduz para o mecanismo correcto.

### Checklist

```
□ Criar schema/índice para knowledge_document_content
□ Dual-write em CreateKnowledgeDocumentHandler (escrever content para Analytics)
□ KnowledgeSearchRepository migrado para IAnalyticsStore
□ Remover índice GIN gin_knowledge_content do PG (migration)
□ Repetir para ContractVersion, IncidentRecord, RunbookRecord
□ AuditEvent: só adicionar índice no Analytics (PG mantém audit chain intacto)
```

---

## Fase 3 — Decommission PG + Finalização

**Duração:** 4–6 semanas  
**Risco:** Alto  
**Valor:** Alto — PostgreSQL fica apenas como OLTP puro

### Objectivos

1. Confirmar que **zero** queries analíticas chegam ao PostgreSQL
2. Criar PG migrations que fazem `DROP TABLE` nas 25 tabelas migradas
3. Remover índices GIN das tabelas de full-text migradas
4. Redimensionar PG (storage, work_mem, shared_buffers) para workload OLTP puro
5. Actualizar PG connection pool sizing (menos conexões necessárias)

### Validação antes de DROP

```sql
-- Verificar que nenhum código acede à tabela
-- (executar em PG e confirmar zero rows nos últimos 7 dias)
SELECT * FROM pg_stat_user_tables
WHERE relname = 'obs_service_metrics_snapshots'
  AND (seq_scan + idx_scan) = 0;

-- Comparar contagens finais
SELECT COUNT(*) FROM obs_service_metrics_snapshots;
-- deve ser igual ao COUNT no Analytics Store
```

### Checklist

```
□ Auditoria de queries: confirmar zero acessos PG às tabelas migradas (7 dias)
□ Criar migration: DROP TABLE obs_service_metrics_snapshots
□ (repetir para todas as 25 tabelas)
□ Criar migration: DROP INDEX gin_knowledge_content
□ (repetir para índices GIN de full-text)
□ Actualizar pg_hba.conf e pool sizing
□ Benchmark de performance PG antes/depois
□ Actualizar documentação de arquitectura
```

---

## Fase 4 — Null Repos → Real Readers

**Duração:** 6–8 semanas  
**Risco:** Baixo  
**Valor:** Alto — 73 features passam de placeholder para funcional

### Estratégia

Cada Null repository é substituído por uma implementação real que usa `IAnalyticsStore`.
A interface do repositório não muda — apenas a implementação concreta.

```csharp
// Antes (Fase 0 — null repo)
public sealed class NullTokenUsageLedgerReader : ITokenUsageLedgerReader
{
    public Task<TokenUsageSummary> GetSummaryAsync(Guid agentId, DateRange range, CancellationToken ct)
        => Task.FromResult(TokenUsageSummary.Empty);
}

// Depois (Fase 4 — real reader)
public sealed class AnalyticsTokenUsageLedgerReader(IAnalyticsStore store)
    : ITokenUsageLedgerReader
{
    public async Task<TokenUsageSummary> GetSummaryAsync(Guid agentId, DateRange range, CancellationToken ct)
    {
        var records = await store.QueryAsync<TokenUsageRecord>(new AnalyticsQuery
        {
            Collection = "token_usage_ledger",
            From = range.Start,
            To = range.End,
            Filters = new Dictionary<string, object?> { ["agent_id"] = agentId.ToString() }
        }, ct);

        return TokenUsageSummary.From(records);
    }
}
```

### Prioridade de implementação

| Prioridade | Módulo | Null repos | Impacto visível |
|-----------|--------|-----------|-----------------|
| P0 | AI Knowledge | 18 repos | Dashboards de governance AI |
| P0 | Cost Management | 12 repos | Dashboards de custos |
| P1 | SLO / Reliability | 10 repos | Error budget dashboards |
| P1 | Observability | 9 repos | Métricas de serviço |
| P2 | Security | 8 repos | Security events feed |
| P2 | Analytics | 7 repos | Product analytics |
| P3 | Developer Prod. | 9 repos | DORA metrics |

Ver [06-NULL-REPOSITORIES.md](./06-NULL-REPOSITORIES.md) para a lista completa.

### Checklist

```
□ P0: Implementar 18 readers de AI Knowledge (usar IAnalyticsStore)
□ P0: Implementar 12 readers de Cost Management
□ P1: Implementar 10 readers de SLO
□ P1: Implementar 9 readers de Observability
□ P2: Implementar 8 readers de Security
□ P2: Implementar 7 readers de Analytics
□ P3: Implementar 9 readers de Developer Productivity
□ Remover todas as classes Null* de readers (substituídas pelas reais)
□ Testes de integração para todos os readers
```

---

## Linha de tempo consolidada

```
Mês 1        Mês 2        Mês 3        Mês 4        Mês 5
─────────    ─────────    ─────────    ─────────    ─────────
Fase 0 ██    
Fase 1 ░░░░░░░░░░░░░░░░░░
                          Fase 2 ░░░░░░░░░░░░░
                                       Fase 3 ░░░░░░░░░░░░░░
                          Fase 4 ░░░░░░░░░░░░░░░░░░░░░░░░░░░
```

**Marcos:**
- **Fim Fase 0:** ClickHouse activo, 73 repos recebem `IAnalyticsStore` (ainda null para leituras)
- **Fim Fase 1:** 25 tabelas PG descomissionadas, PostgreSQL 40% mais leve
- **Fim Fase 2:** Índices GIN removidos, full-text fora do PostgreSQL
- **Fim Fase 3:** PostgreSQL puro OLTP — sem tabelas analíticas, sem índices GIN
- **Fim Fase 4:** 73 features activas, dashboards de analytics funcionais

---

## Riscos e mitigações

| Risco | Probabilidade | Impacto | Mitigação |
|-------|--------------|---------|-----------|
| Perda de dados durante dual-write | Baixa | Alto | Row count comparison diário; rollback = desactivar write Analytics |
| Analytics Store indisponível | Média | Médio | NullAnalyticsStore como fallback; circuit breaker na escrita |
| Performance queries Analytics Store | Baixa | Médio | Benchmark em staging antes de cada fase |
| Cliente sem recursos para ClickHouse | Média | Baixo | Elasticsearch como alternativa já documentada |
| PG migration irreversível (DROP TABLE) | Baixa | Alto | Backup completo antes de cada DROP; janela de rollback de 2 semanas |
