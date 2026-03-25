# PARTE 6 — Posicionamento de Dados entre PostgreSQL e ClickHouse

> **Data**: 2026-03-25
> **Prompt**: N12 — Consolidação do módulo Product Analytics
> **Estado**: DEFINIÇÃO FINAL

---

## 1. Dados que devem ficar no PostgreSQL

| Dado | Tabela | Justificação |
|------|--------|-------------|
| Definições de métricas/journeys/milestones | `pan_definitions` | Configuração transacional, baixo volume, editável |
| Passos de jornada | `pan_journey_steps` | Configuração transacional, FK para definitions |
| Definições de milestones | `pan_value_milestones` | Configuração transacional, FK para definitions |
| Eventos recentes (buffer) | `pan_events` | Buffer temporário antes de flush para ClickHouse |

**Nota sobre o buffer**: A tabela `pan_events` no PostgreSQL serve como **buffer de ingestão**. Eventos são escritos primeiro no PostgreSQL (garantia transacional, integração com outbox pattern) e depois replicados para o ClickHouse. Após replicação confirmada, eventos antigos podem ser purgados do PostgreSQL (retenção de ~7 dias no buffer).

---

## 2. Dados que devem ir para ClickHouse

| Dado | Tabela ClickHouse | Engine | Justificação |
|------|-------------------|--------|-------------|
| Todos os eventos analíticos | `pan_events` | MergeTree | Tabela principal de eventos (append-only, alto volume) |
| Agregações diárias por módulo | `pan_daily_module_stats` | SummingMergeTree | Materialized view para dashboards de adoção |
| Agregações diárias por persona | `pan_daily_persona_stats` | SummingMergeTree | Materialized view para dashboards de persona |
| Agregações de friction | `pan_daily_friction_stats` | SummingMergeTree | Materialized view para indicadores de fricção |
| Agregações de sessão | `pan_session_summaries` | AggregatingMergeTree | Resumo por sessão para funnels |

### Esquema ClickHouse: `pan_events`

```sql
CREATE TABLE pan_events (
    id UUID,
    tenant_id UUID,
    user_id UUID,
    persona LowCardinality(String),
    module LowCardinality(String),
    event_type UInt8,
    feature String DEFAULT '',
    entity_type String DEFAULT '',
    outcome String DEFAULT '',
    route String DEFAULT '',
    team_id Nullable(UUID),
    domain_id Nullable(UUID),
    session_id String DEFAULT '',
    client_type LowCardinality(String) DEFAULT '',
    metadata_json String DEFAULT '',
    occurred_at DateTime64(3, 'UTC'),
    environment_id Nullable(UUID),
    duration Nullable(UInt32),
    parent_event_id Nullable(UUID),
    source LowCardinality(String) DEFAULT 'Frontend'
) ENGINE = MergeTree()
PARTITION BY toYYYYMM(occurred_at)
ORDER BY (tenant_id, occurred_at, module, event_type)
TTL occurred_at + INTERVAL 2 YEAR
SETTINGS index_granularity = 8192;
```

### Materialized View: `pan_daily_module_stats`

```sql
CREATE MATERIALIZED VIEW pan_daily_module_stats
ENGINE = SummingMergeTree()
ORDER BY (tenant_id, date, module)
AS SELECT
    tenant_id,
    toDate(occurred_at) AS date,
    module,
    count() AS total_events,
    uniqExact(user_id) AS unique_users,
    uniqExact(session_id) AS unique_sessions,
    countIf(event_type IN (4, 21, 22)) AS friction_events
FROM pan_events
GROUP BY tenant_id, date, module;
```

---

## 3. Dados que NÃO devem ir para ClickHouse

| Dado | Razão |
|------|-------|
| Definições de métricas (pan_definitions) | Dados de configuração, baixo volume, transacional |
| Passos de jornada (pan_journey_steps) | Dados de configuração |
| Definições de milestones (pan_value_milestones) | Dados de configuração |
| Permissões e roles | Pertencem a Identity & Access |
| Dados de auditoria de ações admin | Pertencem a Audit & Compliance |

---

## 4. Eventos de integração de alto volume

| Evento | Volume Estimado | Justificação |
|--------|----------------|-------------|
| ModuleViewed | **ALTO** (~100-1000/dia/tenant) | Cada page view gera evento |
| EntityViewed | **ALTO** (~50-500/dia/tenant) | Cada visualização de entidade |
| SearchExecuted | **MÉDIO** (~20-200/dia/tenant) | Cada pesquisa |
| QuickActionTriggered | **MÉDIO** (~10-100/dia/tenant) | Ações rápidas |
| ZeroResultSearch | **BAIXO** (~1-10/dia/tenant) | Mas importante para friction |
| JourneyAbandoned | **BAIXO** (~1-10/dia/tenant) | Mas importante para friction |
| EmptyStateEncountered | **BAIXO** (~1-10/dia/tenant) | Mas importante para friction |

**Volume total estimado**: 200-2000 eventos/dia/tenant → Para 100 tenants = 20K-200K eventos/dia

Este volume é **gerível pelo PostgreSQL** a curto prazo, mas com crescimento do produto, **ClickHouse é necessário** para:
- Queries analíticas complexas (funnels, cohorts, time-series)
- Retenção de longo prazo (2+ anos)
- Agregações em tempo real (materialized views)
- Queries de alto throughput sem impactar o banco transacional

---

## 5. Métricas e agregações relevantes

### Métricas core (para materialized views)

| Métrica | Dimensões | Agregação | Periodicidade |
|---------|-----------|-----------|---------------|
| Total events | tenant, module, persona, event_type | COUNT | Diária |
| Unique users | tenant, module, persona | COUNT DISTINCT | Diária |
| Unique sessions | tenant, module | COUNT DISTINCT | Diária |
| Adoption score | tenant, module | Fórmula composta | Diária |
| Friction rate | tenant, module | COUNT friction / COUNT total | Diária |
| Time to first value | tenant, persona | AVG(first milestone time) | Semanal |
| Feature depth | tenant, module | COUNT DISTINCT features | Diária |
| Session duration | tenant, persona | AVG duration | Diária |

### Queries analíticas típicas

| Query | Complexidade | PostgreSQL | ClickHouse |
|-------|-------------|-----------|------------|
| "Adoption por módulo no último mês" | Média | ⚠️ Lento com muitos eventos | ✅ Rápido |
| "Funnel de jornada X nos últimos 90 dias" | Alta | ❌ Muito lento | ✅ Rápido |
| "Trends de friction por módulo (6 meses)" | Alta | ❌ Muito lento | ✅ Rápido |
| "Top features por persona (3 meses)" | Média | ⚠️ Aceitável | ✅ Rápido |
| "Cohort retention (12 meses)" | Muito Alta | ❌ Impraticável | ✅ Praticável |
| "Registar 1 evento" | Baixa | ✅ Ideal | ⚠️ Overhead desnecessário |
| "Listar definições" | Baixa | ✅ Ideal | ❌ Não faz sentido |

---

## 6. Chaves de correlação com PostgreSQL

| Chave | PostgreSQL | ClickHouse | Uso |
|-------|-----------|------------|-----|
| tenant_id | ✅ RLS filter em todas as tabelas | ✅ Partition key | Isolamento multi-tenant |
| user_id | ✅ Via Identity module | ✅ Coluna em pan_events | Correlação de utilizador |
| module | ✅ Enum/string nas definições | ✅ Coluna em pan_events | Correlação de módulo |
| session_id | ❌ Não persistido no PostgreSQL | ✅ Coluna em pan_events | Correlação de sessão |
| definition_id | ✅ PK em pan_definitions | ❌ Não existe | Lookup via API |

---

## 7. Nível de necessidade do ClickHouse

### Decisão: **REQUIRED**

| Critério | Avaliação |
|---------|-----------|
| Volume de dados | MÉDIO-ALTO (20K-200K eventos/dia) |
| Complexidade de queries | ALTA (funnels, cohorts, time-series) |
| Retenção necessária | LONGA (2+ anos) |
| Impacto em PostgreSQL | SIGNIFICATIVO se queries analíticas pesadas |
| Alternativas | PostgreSQL com partitioning (subóptimo) |
| Decisão arquitetural existente | **REQUIRED** em module-data-placement-matrix.md |

---

## 8. Justificação

Product Analytics é, por definição, um **módulo analítico**. O seu valor vem de:

1. **Agregar eventos de alto volume** → PostgreSQL não escala para séries temporais de longo prazo
2. **Queries analíticas complexas** → Funnels, cohorts e time-series são strengths do ClickHouse
3. **Retenção de longo prazo** → 2+ anos de eventos sem impactar performance transacional
4. **Materialized views** → Agregações automáticas para dashboards rápidos
5. **Consistência com decisão arquitetural** → `module-data-placement-matrix.md` já classificou como REQUIRED

A estratégia é:
- **PostgreSQL**: Buffer de ingestão (pan_events com purge periódico) + configuração transacional (pan_definitions, etc.)
- **ClickHouse**: Store permanente de eventos + materialized views para métricas agregadas + queries analíticas
- **Fluxo**: POST /events → PostgreSQL buffer → Outbox → ClickHouse writer → ClickHouse permanente

Esta é a mesma estratégia usada por plataformas de product analytics como Amplitude, Mixpanel e PostHog.
