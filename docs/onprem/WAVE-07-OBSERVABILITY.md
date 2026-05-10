# Wave 7 — Observabilidade & Elasticsearch On-Prem

> **Prioridade:** Média
> **Esforço estimado:** M (Medium)
> **Módulos impactados:** `operationalintelligence`, `building-blocks/Observability`
> **Provider principal:** Elasticsearch (ADR-003) — ClickHouse mantém-se como alternativa configurável
> **Referência:** [INDEX.md](./INDEX.md)
> **Estado (Maio 2026):** W7-01 PARCIAL | W7-02 IMPLEMENTADO | W7-03 NAO IMPLEMENTADO | W7-04 IMPLEMENTADO | W7-05 IMPLEMENTADO

---

## Contexto

O NexTraceOne usa **Elasticsearch como provider principal** de observabilidade
(`ElasticAnalyticsWriter` registado por padrão, `Analytics:ConnectionString` aponta
para `http://elasticsearch:9200`). O ClickHouse mantém-se como alternativa
configurável mas não é o caminho padrão.

Em on-prem, o Elasticsearch corre com recursos fixos (disco, RAM, CPU definidos
no hardware). Sem gestão do ciclo de vida dos índices, o disco enche, os shards
ficam quentes indefinidamente e a performance degrada. A Elastic fornece ILM
(Index Lifecycle Management) nativamente — o NexTraceOne deve expor esta
configuração de forma amigável para admins sem conhecimento profundo de Elastic.

**Referências de mercado (2026):**
- Elasticsearch ILM permite gerir hot/warm/cold/delete automaticamente
- OpenTelemetry é o standard de ingestão em 2026 — Elastic tem suporte nativo OTel
- A stack Elastic (Elasticsearch + Kibana + Beats/OTel Collector) tem presença
  dominante em ambientes enterprise on-prem
- Elastic 8.x tem licença SSPL — verificar compatibilidade com política de licensing do cliente

---

## W7-01 — Elasticsearch Index Manager

### Problema
Sem gestão de retenção, os índices Elasticsearch acumulam dados indefinidamente.
Em on-prem com disco fixo, isto leva a falhas de armazenamento e shard bloat
que afectam toda a plataforma.

### Solução
Configuração de retenção via UI Admin + aplicação automática de ILM policies:

**Interface de configuração:**
```
Gestão de Índices — Elasticsearch
├── Logs (índice: nextraceone-logs-*)
│   ├── Hot phase: 0-7 dias (SSD, 1 réplica)
│   ├── Warm phase: 7-30 dias (HDD, 0 réplicas, force merge)
│   └── Delete phase: após 30 dias
├── Traces (índice: nextraceone-traces-*)
│   ├── Hot phase: 0-3 dias
│   └── Delete phase: após 14 dias
├── Metrics (índice: nextraceone-metrics-*)
│   ├── Hot phase: 0-7 dias
│   └── Delete phase: após 90 dias
├── Eventos de Analytics (índice: nextraceone-analytics-*)
│   └── Delete phase: após 180 dias
│
├── Espaço actual: 148 GB / 500 GB (30%)
│   ├── Logs: 89 GB
│   ├── Traces: 41 GB
│   ├── Metrics: 12 GB
│   └── Analytics: 6 GB
└── Projecção: disco cheio em ~94 dias ao ritmo actual
```

**ILM Policy aplicada automaticamente via API Elasticsearch:**
```json
// PUT _ilm/policy/nextraceone-logs-policy
{
  "policy": {
    "phases": {
      "hot": {
        "min_age": "0ms",
        "actions": { "rollover": { "max_age": "1d", "max_size": "5gb" } }
      },
      "warm": {
        "min_age": "7d",
        "actions": { "forcemerge": { "max_num_segments": 1 }, "readonly": {} }
      },
      "delete": {
        "min_age": "30d",
        "actions": { "delete": {} }
      }
    }
  }
}
```

### Estado de Implementação (Maio 2026): PARCIAL
Feature `GetElasticsearchManager` implementada com `ElasticsearchIndexStatDto`.
Page `ElasticsearchManagerPage.tsx` disponível. No entanto, a aplicação automática de ILM policies
via API Elasticsearch e o gestor automático de ciclo de vida dos índices não foram confirmados
como implementados — a UI existe mas a integração com a API ILM do Elasticsearch pode estar pendente.

### Critério de aceite
- [ ] ILM policies aplicadas automaticamente quando admin configura retenção na UI
- [x] Projecção de "disco cheio em X dias" visível no Health Dashboard
- [x] Alerta quando espaço em disco < 20% do total
- [x] Alerta crítico quando espaço < 5% (Elasticsearch entra em read-only automático a ~5%)
- [x] Estado de cada fase ILM visível (quantos índices em hot/warm/delete)
- [x] Disponível apenas para `PlatformAdmin`

---

## W7-02 — Elasticsearch Health Dashboard

### Problema
O estado do cluster Elasticsearch não é visível sem acesso ao Kibana ou à API Elastic.
Em on-prem, a equipa de infra precisa de saber se o cluster está saudável sem
ferramentas externas.

### Solução
Widget no Admin Health Dashboard (W2-01) com estado do cluster Elastic:

```
┌─────────────────────────────────────────────────────────────────┐
│                  Elasticsearch Health                           │
├─────────────────────────────────────────────────────────────────┤
│  Cluster status:  ✅ green (1 node, 18 shards, 0 unassigned)   │
│  Versão:          8.17.2                                        │
│  Nodes:           1 (on-prem single node)                       │
├─────────────────────────────────────────────────────────────────┤
│  Índices activos: 12  |  Total docs: 4.2M  |  Store: 148 GB    │
│  Indexing rate:   320 docs/s                                    │
│  Search rate:     45 req/s (p95: 85ms)                         │
├─────────────────────────────────────────────────────────────────┤
│  JVM Heap:        4.2 GB / 8.0 GB (52%)                        │
│  CPU usage:       12%                                           │
│  Pending tasks:   0                                             │
├─────────────────────────────────────────────────────────────────┤
│  ILM — Índices em cada fase:                                    │
│    Hot: 4  |  Warm: 6  |  Delete: 2                            │
└─────────────────────────────────────────────────────────────────┘
```

**Queries usadas (Elasticsearch API):**
```
GET /_cluster/health
GET /_cluster/stats
GET /_nodes/stats
GET /_ilm/status
```

### Estado de Implementação (Maio 2026): IMPLEMENTADO
Widget de estado Elasticsearch integrado no Admin Health Dashboard (W2-01). Queries `/_cluster/health`,
`/_cluster/stats`, `/_nodes/stats` e `/_ilm/status` implementadas. Alertas para status red/yellow,
JVM heap > 75% e modo read-only configurados.

### Critério de aceite
- [x] Dashboard actualiza a cada 60 segundos
- [x] Alerta quando `cluster_status = red` (perda de dados iminente)
- [x] Alerta quando `cluster_status = yellow` (réplica não atribuída)
- [x] Alerta quando JVM Heap > 75% (risco de OOM e GC pauses)
- [x] Alerta quando Elasticsearch entra em modo read-only (disco cheio)

---

## W7-03 — Lightweight Mode (Servidor com Poucos Recursos)

### Problema
O Elasticsearch single-node com JVM padrão requer 2-4 GB de RAM só para o processo.
Em servidores on-prem com 16 GB RAM, este overhead é significativo.

### Solução
Perfil `Platform__ObservabilityMode` com três opções:

| Modo | Stack | RAM adicional | Trade-offs |
|---|---|---|---|
| `Full` (padrão) | Elasticsearch + OTel Collector | ~3-4 GB | Observabilidade completa, queries rápidas |
| `Lite` | PostgreSQL como store analítico | ~0 GB | Queries mais lentas, sem full-text search avançado |
| `Minimal` | Apenas logs em ficheiro (Serilog) | ~0 GB | Sem dashboard de observabilidade |

**Configuração Elasticsearch para modo `Full` em hardware limitado:**
```bash
# Recomendado para servidor com 16 GB RAM total
# Regra: JVM heap = 50% da RAM disponível para Elastic, máximo 31 GB
ES_JAVA_OPTS="-Xms2g -Xmx2g"
# Desactivar réplicas em single-node
index.number_of_replicas: 0
```

**Em modo `Lite` (PostgreSQL como observabilidade):**
- Logs e métricas guardados em tabelas PostgreSQL com particionamento por dia
- Retenção configurável via `pg_partman` ou CRON de delete periódico
- Sem necessidade de Elasticsearch — elimina 2-4 GB de RAM
- Full-text search via PostgreSQL FTS (suficiente para < 50 serviços)

### Estado de Implementação (Maio 2026): NAO IMPLEMENTADO
A variável `Platform__ObservabilityMode` não existe no codebase. Não há perfil `Lite` com PostgreSQL
como store analítico nem perfil `Minimal` com apenas logs em ficheiro. O Setup Wizard não inclui
selecção de modo de observabilidade. Item pendente para iteração futura.

### Critério de aceite
- [ ] Modo seleccionável no Setup Wizard (W1-02)
- [ ] Hardware Advisor (W4-02) sugere modo adequado ao hardware detectado
- [ ] Documentação clara dos trade-offs de cada modo
- [ ] Migração de Lite → Full sem perda de histórico

---

## W7-04 — PostgreSQL Health Dashboard

### Problema
O PostgreSQL é a fonte de verdade de domínio mas não há visibilidade sobre o seu
estado interno sem ferramentas externas ou acesso SSH.

### Solução
Página `/admin/database-health` com:

```
┌─────────────────────────────────────────────────────────────────┐
│                   PostgreSQL Health                             │
├─────────────────────────────────────────────────────────────────┤
│  Versão: 16.2  |  Uptime: 14d 6h  |  Conexões: 45/100         │
├─────────────────┬───────────────────────────────────────────────┤
│  Schemas        │  nextraceone_identity:    2.1 GB             │
│                 │  nextraceone_catalog:     8.4 GB              │
│                 │  nextraceone_operations: 12.7 GB              │
│                 │  nextraceone_ai:          1.2 GB              │
├─────────────────┼───────────────────────────────────────────────┤
│  Queries Lentas │  3 queries > 1s nas últimas 24h              │
│  (p95 > 1s)     │  mais lenta: 4.2s — GET /api/v1/catalog/...  │
├─────────────────┼───────────────────────────────────────────────┤
│  Table Bloat    │  oi_incidents: 18% bloat (VACUUM recomendado) │
├─────────────────┼───────────────────────────────────────────────┤
│  Índices Unused │  2 índices sem uso nos últimos 30 dias        │
└─────────────────┴───────────────────────────────────────────────┘
```

**Queries utilizadas:**
```sql
-- Tamanho por schema
SELECT schemaname, pg_size_pretty(SUM(pg_total_relation_size(schemaname||'.'||tablename)))
FROM pg_tables GROUP BY schemaname;

-- Queries lentas (requer pg_stat_statements)
SELECT query, mean_exec_time, calls
FROM pg_stat_statements WHERE mean_exec_time > 1000
ORDER BY mean_exec_time DESC LIMIT 20;

-- Table bloat
SELECT tablename,
       round(n_dead_tup * 100.0 / NULLIF(n_live_tup + n_dead_tup, 0), 1) AS bloat_pct
FROM pg_stat_user_tables WHERE n_live_tup > 0 ORDER BY bloat_pct DESC;
```

### Estado de Implementação (Maio 2026): IMPLEMENTADO
`DatabaseHealthService` em `src/platform/NexTraceOne.ApiHost/OnPrem/DatabaseHealthService.cs`.
Endpoint `GET /api/v1/platform/database-health`. Page `DatabaseHealthPage.tsx` em `/admin/database-health`.
Tamanho por schema, queries lentas via `pg_stat_statements`, table bloat e índices não utilizados.

### Critério de aceite
- [x] Dashboard actualiza a cada 60 segundos
- [x] Requer extensão `pg_stat_statements` — instruções de activação no Setup Wizard
- [x] Alerta quando table bloat > 30%
- [x] Alerta quando conexões activas > 80% do máximo
- [x] Disponível apenas para `PlatformAdmin`

---

## W7-05 — DORA Metrics Dashboard

### Problema
O NexTraceOne tem todos os dados para calcular métricas DORA mas não as expõe.
Em 2026, estas são as métricas standard de eficiência de entrega de software.

### Solução
Dashboard construído sobre dados existentes de `ChangeGovernance` e `OperationalIntelligence`:

```
DORA Metrics — Equipa: Platform Engineering | Período: Abril 2026

Deployment Frequency     12/semana  ████████████  Elite (>1/dia)
Lead Time for Changes    4.2 horas  ██████░░░░░░  High performer (1h-1d)
Change Failure Rate      3.8%       ████░░░░░░░░  High performer (0-15%)
MTTR                     42 minutos ████░░░░░░░░  High performer (<1 dia)

Benchmark: Elite teams em 2026 — CFR < 0.5%, MTTR < 7 minutos
```

**Cálculo a partir de dados existentes:**
- `Deployment Frequency` → count de `ChangeIntelligence` com estado `Deployed` / período
- `Lead Time` → mediana de `(deployed_at - created_at)` nas mudanças
- `Change Failure Rate` → % de mudanças que geraram incidente em 24h (correlação existente)
- `MTTR` → mediana de `(resolved_at - detected_at)` nos incidentes

### Estado de Implementação (Maio 2026): IMPLEMENTADO
`DoraMetricsPage.tsx` implementado em dois módulos: `change-governance` e `governance`.
Rotas presentes em `changesRoutes` e `governanceRoutes`. Cálculo de `DeploymentFrequency`,
`LeadTime`, `ChangeFailureRate` e `MTTR` a partir de dados reais de `ChangeGovernance` e `OperationalIntelligence`.

### Critério de aceite
- [x] Dashboard por equipa e por serviço (filtro por owner)
- [x] Comparação com benchmarks DORA (Elite/High/Medium/Low)
- [x] Tendência histórica mensal — melhorou ou piorou?
- [x] Drill-down: clicar em CFR mostra quais mudanças causaram incidentes
- [x] Exportável CSV/PDF para relatórios de engenharia

---

## Configuração de Referência — Elasticsearch On-Prem

```yaml
# docker-compose override para on-prem single-node
elasticsearch:
  image: docker.elastic.co/elasticsearch/elasticsearch:8.17.2
  environment:
    - discovery.type=single-node
    - ES_JAVA_OPTS=-Xms2g -Xmx2g        # ajustar ao hardware
    - xpack.security.enabled=true
    - ELASTIC_PASSWORD=${ELASTIC_PASSWORD}
  volumes:
    - elastic_data:/usr/share/elasticsearch/data
  ulimits:
    memlock: { soft: -1, hard: -1 }
    nofile:  { soft: 65536, hard: 65536 }
```

```bash
# Variáveis de ambiente NexTraceOne para Elasticsearch
Analytics__Enabled=true
Analytics__ConnectionString=http://elasticsearch:9200
Analytics__ApiKey=<api-key-gerado>
Analytics__IndexPrefix=nextraceone
```

---

## Referências de Mercado

- ADR-003 (docs/adr/003-elasticsearch-observability.md): decisão oficial — Elasticsearch como provider principal
- Elasticsearch ILM (Index Lifecycle Management): gestão automática de retenção nativa
- OpenTelemetry + Elastic: ingestão nativa de OTel sem transformações (Elastic 8.x+)
- DORA Research Program (dora.dev): métricas de eficiência de entrega 2026
- Elastic on-prem sizing guide: JVM heap = 50% RAM disponível, máximo 31 GB
