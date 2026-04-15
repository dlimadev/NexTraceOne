# Wave 7 — Observabilidade & ClickHouse On-Prem

> **Prioridade:** Média
> **Esforço estimado:** M (Medium)
> **Módulos impactados:** `operationalintelligence`, `building-blocks/Observability`
> **Referência:** [INDEX.md](./INDEX.md)

---

## Contexto

O NexTraceOne já usa ClickHouse como provider de observabilidade. Em on-prem,
o ClickHouse corre no mesmo servidor ou numa máquina dedicada com recursos fixos.
Sem gestão de ciclo de vida dos dados, o disco enche e a performance degrada.

Benchmark de mercado (2026):
- ClickHouse TTL permite gestão automática de retenção sem scripts externos
- ZSTD compression reduz volume de logs em ~50%
- Tiered storage move dados antigos para armazenamento mais barato automaticamente
- ClickStack (ClickHouse + OTel + HyperDX) é o standard open-source em 2026

---

## W7-01 — ClickHouse Space Manager

### Problema
Sem gestão de retenção, o ClickHouse acumula dados indefinidamente.
Em on-prem com disco fixo, isto leva a falhas de armazenamento que
afectam toda a plataforma.

### Solução
Configuração de retenção via UI Admin + automatização via TTL nativo do ClickHouse:

**Interface de configuração:**
```
Gestão de Retenção — ClickHouse
├── Logs
│   ├── Retenção hot (SSD): 7 dias
│   ├── Retenção cold (HDD/NFS): 30 dias
│   └── Eliminação automática: após 30 dias
├── Traces
│   ├── Retenção hot: 3 dias
│   └── Eliminação: após 14 dias
├── Métricas (agregadas)
│   └── Retenção: 90 dias
├── Espaço actual: 148 GB / 500 GB (30%)
│   ├── Logs: 89 GB
│   ├── Traces: 41 GB
│   └── Métricas: 18 GB
└── Projecção: disco cheio em ~94 dias ao ritmo actual
```

**TTL ClickHouse gerado automaticamente:**
```sql
-- Gerado pelo NexTraceOne quando admin configura retenção
ALTER TABLE otel_logs
  MODIFY TTL toDateTime(Timestamp) + INTERVAL 30 DAY DELETE;

ALTER TABLE otel_traces
  MODIFY TTL toDateTime(Start) + INTERVAL 14 DAY DELETE;
```

### Critério de aceite
- [ ] Retenção configurável por tipo de dado via UI Admin
- [ ] TTL aplicado automaticamente ao ClickHouse quando configuração muda
- [ ] Projecção de "disco cheio em X dias" visível no Health Dashboard
- [ ] Alerta quando espaço < 20% do total
- [ ] Alerta crítico quando espaço < 5%

---

## W7-02 — Lightweight Mode (Servidor com Poucos Recursos)

### Problema
Nem todos os clientes têm servidores com 32+ GB RAM. Instalações em hardware
mais limitado (16 GB RAM, SSD de 250 GB) precisam de funcionar com trade-offs
de observabilidade.

### Solução
Perfil `Platform__ObservabilityMode`:

| Modo | Recursos | Trade-offs |
|---|---|---|
| `Full` (padrão) | ClickHouse + OTel | Observabilidade completa — requer ~4 GB RAM extra |
| `Lite` | PostgreSQL como store analítico | Queries mais lentas, sem traces distribuídos |
| `Minimal` | Apenas logs estruturados em ficheiro | Sem dashboard de observabilidade; logs acessíveis via log explorer |

**Em modo `Lite`:**
- Logs e métricas guardados em PostgreSQL (tabelas com particionamento por dia)
- Sem ClickHouse — elimina dependência de ~2 GB RAM
- Retenção configurável via TTL do PostgreSQL (`pg_partman`)
- Performance aceitável para < 50 serviços e < 10k req/min

**Em modo `Minimal`:**
- Serilog para ficheiro com rotação diária
- Log Explorer acede aos ficheiros directamente
- Sem métricas de telemetria
- Recomendado apenas para POC ou instalações muito limitadas

### Critério de aceite
- [ ] Modo seleccionável no Setup Wizard e alterável no Admin
- [ ] Documentação clara dos trade-offs de cada modo
- [ ] Migração de dados entre modos (Full → Lite) sem perda de histórico
- [ ] Hardware Advisor sugere modo adequado baseado no hardware detectado

---

## W7-03 — PostgreSQL Health Dashboard

### Problema
O PostgreSQL é a base de dados central mas não há visibilidade sobre o seu estado
sem acesso directo à BD ou ferramentas externas.

### Solução
Página `/admin/database-health` com informação em tempo real:

```
┌─────────────────────────────────────────────────────────────────┐
│                   PostgreSQL Health                             │
├─────────────────────────────────────────────────────────────────┤
│  Versão: 16.2  |  Uptime: 14d 6h  |  Conexões: 45/100         │
├─────────────────┬───────────────────────────────────────────────┤
│  Tamanho Schemas│  nextraceone_identity:   2.1 GB              │
│                 │  nextraceone_catalog:    8.4 GB               │
│                 │  nextraceone_operations: 12.7 GB              │
│                 │  nextraceone_ai:         1.2 GB               │
├─────────────────┼───────────────────────────────────────────────┤
│  Queries Lentas │  (últimas 24h)                                │
│  (> 1 segundo)  │  3 queries | mais lenta: 4.2s                │
│                 │  → GET /api/v1/catalog/services (full scan)   │
├─────────────────┼───────────────────────────────────────────────┤
│  Table Bloat    │  oi_incidents: 18% bloat (VACUUM recomendado) │
├─────────────────┼───────────────────────────────────────────────┤
│  Índices Unused │  2 índices não usados nos últimos 30 dias     │
└─────────────────┴───────────────────────────────────────────────┘
```

**Queries utilizadas:**
```sql
-- Tamanho por schema
SELECT schemaname, pg_size_pretty(pg_total_relation_size(...))

-- Queries lentas
SELECT query, mean_exec_time, calls FROM pg_stat_statements
  WHERE mean_exec_time > 1000 ORDER BY mean_exec_time DESC LIMIT 20

-- Table bloat (estimativa)
SELECT tablename, n_dead_tup, n_live_tup,
       round(n_dead_tup * 100.0 / (n_live_tup + n_dead_tup), 1) AS bloat_pct
  FROM pg_stat_user_tables WHERE n_live_tup > 0

-- Índices não usados
SELECT indexrelname FROM pg_stat_user_indexes WHERE idx_scan = 0
```

### Critério de aceite
- [ ] Dashboard actualiza a cada 60 segundos
- [ ] Requer extensão `pg_stat_statements` (instruções de activação no Setup Wizard)
- [ ] Alerta quando table bloat > 30%
- [ ] Alerta quando conexões > 80% do máximo
- [ ] Disponível apenas para `PlatformAdmin`

---

## W7-04 — DORA Metrics Dashboard

### Problema
O NexTraceOne tem todos os dados necessários para calcular métricas DORA
(Deployment Frequency, Lead Time, Change Failure Rate, MTTR) mas não as expõe.
Estas métricas são o standard de 2026 para medir eficiência de entrega.

### Solução
Dashboard DORA construído sobre dados existentes de `ChangeGovernance` e `OperationalIntelligence`:

```
DORA Metrics — Equipa: Platform Engineering
Período: Abril 2026

Deployment Frequency
  12 deploys/semana  ████████████  Elite (>1/dia)

Lead Time for Changes
  4.2 horas          ██████░░░░░░  High (1h-1d)

Change Failure Rate
  3.8%               ████░░░░░░░░  High (0-15%)
  Benchmark elite: < 0.5%

MTTR (Mean Time to Restore)
  42 minutos         ████░░░░░░░░  High (<1 dia)
  Benchmark elite: < 7 minutos
```

**Cálculo:**
- `Deployment Frequency` = count de changes em estado `Deployed` / período
- `Lead Time` = mediana de `(deployed_at - created_at)` em changes
- `Change Failure Rate` = % de changes que geraram incidente em 24h
- `MTTR` = mediana de `(resolved_at - detected_at)` em incidentes correlacionados

### Critério de aceite
- [ ] Dashboard por equipa e por serviço
- [ ] Comparação com benchmarks DORA (Elite/High/Medium/Low)
- [ ] Tendência mensal — melhorou ou piorou?
- [ ] Drill-down: clicar em CFR mostra quais mudanças causaram incidentes
- [ ] Exportável em CSV/PDF para relatórios de engenharia

---

## Referências de Mercado

- ClickStack (ClickHouse + OTel + HyperDX): standard open-source observability 2026
- SigNoz: ClickHouse-based, self-hosted, alternativa ao Grafana sem complexidade
- ClickHouse TTL: documentação oficial de data lifecycle management
- DORA Research Program (dora.dev): métricas de eficiência de entrega 2026
- pgroll: zero-downtime migrations PostgreSQL
