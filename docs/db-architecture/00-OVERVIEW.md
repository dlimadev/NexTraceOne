# NexTraceOne — Database Architecture Analysis & Action Plan

## Índice

| Ficheiro | Conteúdo |
|----------|----------|
| [01-POSTGRESQL-KEEP.md](./01-POSTGRESQL-KEEP.md) | Entidades que **ficam** no PostgreSQL |
| [02-CLICKHOUSE-MIGRATE.md](./02-CLICKHOUSE-MIGRATE.md) | Schemas no **ClickHouse** (store primário) |
| [03-ELASTICSEARCH-MIGRATE.md](./03-ELASTICSEARCH-MIGRATE.md) | Schemas equivalentes no **Elasticsearch** (store alternativo) |
| [04-DUAL-STORE-PATTERNS.md](./04-DUAL-STORE-PATTERNS.md) | Abstracção **IAnalyticsStore** e registo condicional |
| [05-ACTION-PLAN-PHASES.md](./05-ACTION-PLAN-PHASES.md) | Plano de acção por **fases** |
| [06-NULL-REPOSITORIES.md](./06-NULL-REPOSITORIES.md) | 73 **Null repositories** a implementar |
| [07-CLICKHOUSE-SCHEMA-TEMPLATES.md](./07-CLICKHOUSE-SCHEMA-TEMPLATES.md) | Templates de schema ClickHouse para novas tabelas |

---

## Estado Actual

```
Infrastructure actual
─────────────────────────────────────────────────────────
  PostgreSQL 16 + pgvector   ← ÚNICO store transaccional
  Elasticsearch 8.17.0       ← usado só para OTel traces/logs
  ClickHouse                 ← DESABILITADO (Enabled=false)
  OpenTelemetry Collector    ← pipeline de ingestão
─────────────────────────────────────────────────────────
```

### Dimensão actual do esquema PostgreSQL

| Métrica | Valor |
|---------|-------|
| DbContexts | 28 (13 módulos) |
| Entidades de domínio | 382+ |
| Migrações aplicadas | 162 |
| Colunas JSONB grandes | 30+ em entidades de alto volume |
| Tabelas de série temporal em PG | ~18 identificadas |
| Null repositories | 73 (infra não implementada) |

### Problema central

**Todo o workload — OLTP transaccional + séries temporais + full-text search + analytics de produto — está num único PostgreSQL.** Isso causa:

1. **Contenção de I/O**: queries analíticas de longa duração bloqueiam writes de domínio
2. **Índices GIN em tabelas de alto volume**: `ContractVersion`, `KnowledgeDocument` têm índices de full-text que crescem sem bound
3. **Série temporal em PostgreSQL**: `ServiceMetricsSnapshot`, `BurnRateSnapshot`, `CostRecord`, `TokenUsageLedger`, `AnalyticsEvent` inserem linhas continuamente sem TTL automático
4. **Null repositories**: 73 readers ficam nulos porque não há analytics store disponível
5. **ClickHouse já tem schema definido** (`build/clickhouse/`) mas está desligado — o trabalho de design já foi feito

---

## Arquitectura alvo — "Choose One Analytics Store"

> **Decisão de arquitectura:** O cliente instala **PostgreSQL + UM analytics store**.
> ClickHouse é o store **primário** (recomendado). Elasticsearch é o store **alternativo** para
> clientes que já têm infra ES ou preferem a sua stack. **Nunca os dois em simultâneo.**
> A selecção é feita em tempo de instalação via configuração.

```
┌────────────────────────────────────────────────────────────────────┐
│                       NexTraceOne Platform                          │
├──────────────────────┬─────────────────────────────────────────────┤
│   PostgreSQL 16       │   Analytics Store  ← escolha ONE na        │
│   (OLTP — sempre)    │                       instalação            │
│                      ├──────────────────────┬──────────────────────┤
│                      │   ClickHouse         │   Elasticsearch      │
│                      │   PRIMÁRIO           │   ALTERNATIVO         │
├──────────────────────┼──────────────────────┴──────────────────────┤
│ • Domain aggregates  │  • Time-series (metrics, snapshots, ledger) │
│ • Transactions ACID  │  • Event streams (analytics, DORA, alerts)  │
│ • Config / IAM       │  • Cost analytics & token usage             │
│ • Outbox / Saga      │  • Full-text search (knowledge, contracts)  │
│ • Audit chain        │  • Log search & incident correlation        │
│ • pgvector RAG       │  • Security event search                    │
└──────────────────────┴─────────────────────────────────────────────┘
```

### Configuração de selecção (appsettings.json)

```json
{
  "AnalyticsStore": {
    "Provider": "ClickHouse",
    "ClickHouse": {
      "Enabled": true,
      "ConnectionString": "Host=clickhouse;Port=9000;Database=nextraceone_analytics"
    },
    "Elasticsearch": {
      "Enabled": false,
      "Uri": "http://elasticsearch:9200"
    }
  }
}
```

`Provider` aceita `"ClickHouse"` ou `"Elasticsearch"` — apenas um pode ter `Enabled: true`.

### Trade-offs entre providers

| Critério | ClickHouse (primário) | Elasticsearch (alternativo) |
|----------|----------------------|------------------------------|
| Séries temporais | ✅ Excelente (MergeTree, particionado) | ⚠️ Bom (TSDS, mas mais pesado) |
| Agregações analíticas | ✅ Nativo (SUM, quantile, countIf) | ⚠️ Possível (aggs, mas RAM-heavy) |
| Full-text search | ✅ Suficiente (tokenbf_v1, ngrams) | ✅ Excelente (Lucene, relevance) |
| Throughput de ingestão | ✅ Muito alto (batch inserts colunar) | ⚠️ Alto (bulk API, mas mais lento) |
| Recursos de hardware | ✅ Baixo (compressão colunar) | ⚠️ Alto (JVM heap, réplicas) |
| Simplicidade operacional | ✅ Simples | ⚠️ Cluster management complexo |
| Licença | ✅ Apache 2.0 | ⚠️ SSPL (restrições comerciais) |

---

## Critérios de decisão

**Fica no PostgreSQL quando:**
- Tem relações FK com outros agregados
- Participa em transacções de domínio (precisa de ACID)
- É lido por handlers que precisam de consistência imediata
- Faz parte do padrão Outbox (deve ser co-locado com o write transaccional)
- Tem TTL gerido por negócio (não por volume)

**Vai para o Analytics Store (ClickHouse ou Elasticsearch) quando:**
- É append-only (nunca sofre UPDATE/DELETE de negócio)
- Tem dimensão temporal como eixo principal de query
- Requer agregações (SUM, AVG, quantile, countIf) sobre muitas linhas
- O volume cresce sem bound (séries temporais, ledgers de tokens, snapshots de métricas)
- Precisa de full-text search com relevance scoring ou pesquisa por keywords em múltiplos campos
- É um log/event que precisa de facetas e filtros dinâmicos

---

## Abstracção de código

```csharp
// Interface agnóstica do provider (BuildingBlocks)
public interface IAnalyticsStore
{
    Task InsertAsync<T>(string collection, T record, CancellationToken ct = default);
    Task BulkInsertAsync<T>(string collection, IEnumerable<T> records, CancellationToken ct = default);
    Task<IReadOnlyList<T>> QueryAsync<T>(AnalyticsQuery query, CancellationToken ct = default);
    Task<long> CountAsync(AnalyticsQuery query, CancellationToken ct = default);
}

// Registo condicional no DI — só UM dos dois é registado
builder.Services.AddAnalyticsStore(builder.Configuration);

// Internamente:
// if Provider == "ClickHouse"  → registar ClickHouseAnalyticsStore
// if Provider == "Elasticsearch" → registar ElasticsearchAnalyticsStore
// else → registar NullAnalyticsStore (fallback seguro)
```

Cada módulo injeta `IAnalyticsStore` — **não sabe nem precisa de saber** qual provider está por baixo.

---

## Investimento estimado

| Fase | Duração | Risco | Valor |
|------|---------|-------|-------|
| 0 — Abstracção IAnalyticsStore + enable ClickHouse | 2 dias | Baixo | Alto (desbloqueia 73 null repos) |
| 1 — Time-series → Analytics Store | 4-6 semanas | Médio | Alto |
| 2 — Full-text search → Analytics Store | 3-4 semanas | Médio | Médio |
| 3 — Dual-write + decommission PG tables | 4-6 semanas | Alto | Alto |
| 4 — Null repos → real readers | 6-8 semanas | Baixo | Alto |

**Total estimado:** 4-5 meses para arquitectura completa.
