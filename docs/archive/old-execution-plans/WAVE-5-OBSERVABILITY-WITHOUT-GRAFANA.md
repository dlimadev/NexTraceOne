# Wave 5 — Observability Without Grafana: Official Operational Surface

> **Data:** 2026-03-23
> **Gap de referência:** GAP-012-R
> **Decisão arquitetural:** Grafana removido como dependência (Wave 0)

---

## Resumo Executivo

O NexTraceOne **não depende de Grafana** para observabilidade ou troubleshooting. A remoção do Grafana foi uma decisão arquitetural consciente, documentada na Wave 0, fundamentada nos princípios de Source of Truth e autonomia operacional do produto.

Este documento formaliza a superfície operacional oficial do NexTraceOne sem Grafana.

---

## Stack Operacional Oficial

### Componentes

| Componente | Papel | Estado |
|------------|-------|--------|
| **ClickHouse** | Store analítico de observabilidade (padrão) | ✅ Ativo |
| **Elastic** | Store analítico alternativo (enterprise) | ✅ Suportado |
| **OpenTelemetry Collector** | Pipeline de ingestão (Kubernetes) | ✅ Ativo |
| **CLR Profiler** | Pipeline de ingestão (IIS/Windows) | ✅ Suportado |
| **PostgreSQL** | Dados de domínio (não observabilidade) | ✅ Ativo |
| **NexTraceOne Engine** | Consulta, correlação e análise | ✅ Ativo |

### Fluxo de Dados

```
Aplicação (.NET) → OTLP (gRPC/HTTP) → OTel Collector / CLR Profiler → ClickHouse / Elastic → NexTraceOne Engine + IA
```

---

## Superfícies de Troubleshooting

### 1. Telas Internas do NexTraceOne

O NexTraceOne é a superfície oficial de visualização e operação. As seguintes áreas fornecem acesso a dados operacionais:

| Área | Rota | Funcionalidade |
|------|------|----------------|
| **Platform Operations** | `/platform` | Saúde da plataforma, subsistemas, sinais operacionais |
| **Environment Comparison** | `/operations/runtime-comparison` | Comparação entre ambientes, drift detection |
| **Incidents** | `/operations/incidents` | Gestão de incidentes, correlação com mudanças |
| **Reliability** | `/operations/reliability` | Scoring de confiabilidade por serviço |
| **Change Intelligence** | `/changes` | Impacto de mudanças, blast radius, validação pós-deploy |
| **AI Assistant** | `/ai/assistant` | Investigação assistida por IA com acesso a telemetria |

### 2. Consultas Diretas ao ClickHouse

Para troubleshooting avançado, operadores podem consultar diretamente o ClickHouse:

```bash
# Verificar conectividade
clickhouse-client --query 'SELECT 1'

# Verificar tabelas de observabilidade
clickhouse-client --query 'SHOW TABLES FROM nextraceone_obs'

# Consultar traces recentes
clickhouse-client --query 'SELECT * FROM nextraceone_obs.otel_traces ORDER BY Timestamp DESC LIMIT 10'

# Consultar logs recentes
clickhouse-client --query 'SELECT * FROM nextraceone_obs.otel_logs ORDER BY Timestamp DESC LIMIT 10'

# Consultar métricas
clickhouse-client --query 'SELECT * FROM nextraceone_obs.otel_metrics ORDER BY TimeUnix DESC LIMIT 10'
```

### 3. Health Endpoint

```bash
curl -s http://localhost:5000/health | python3 -m json.tool
```

O health endpoint reporta o estado real dos subsistemas:
- **API**: health check real
- **Database**: agregação de 13 checks de base de dados
- **AI**: agregação de 4 checks de IA
- **BackgroundJobs**: status reportado (Unknown quando sem health checks dedicados)
- **Ingestion**: status reportado (Unknown quando sem health checks dedicados)

### 4. Runbooks Operacionais

| Runbook | Localização |
|---------|-------------|
| Troubleshooting de observabilidade | `docs/observability/troubleshooting.md` |
| Drift e comparação de ambientes | `docs/runbooks/DRIFT-AND-ENVIRONMENT-ANALYSIS-RUNBOOK.md` |
| Deploy em produção | `docs/runbooks/PRODUCTION-DEPLOY-RUNBOOK.md` |
| Validação pós-deploy | `docs/runbooks/POST-DEPLOY-VALIDATION.md` |
| Resposta a incidentes | `docs/runbooks/INCIDENT-RESPONSE-PLAYBOOK.md` |
| Degradação de AI provider | `docs/runbooks/AI-PROVIDER-DEGRADATION-RUNBOOK.md` |
| Backup e restore | `docs/runbooks/BACKUP-OPERATIONS-RUNBOOK.md` |
| Rollback | `docs/runbooks/ROLLBACK-RUNBOOK.md` |

---

## Fluxos Operacionais

### Investigar um problema em produção

1. Verificar `/platform` — saúde geral da plataforma
2. Verificar `/operations/incidents` — incidentes ativos
3. Verificar `/changes` — mudanças recentes que possam estar correlacionadas
4. Usar `/ai/assistant` — investigação assistida por IA
5. Se necessário, consultar ClickHouse diretamente para traces/logs detalhados

### Detectar drift entre ambientes

1. Aceder `/operations/runtime-comparison`
2. Comparar configurações, versões e comportamento entre ambientes
3. Consultar `docs/runbooks/DRIFT-AND-ENVIRONMENT-ANALYSIS-RUNBOOK.md` para procedimentos detalhados

### Verificar impacto de uma mudança

1. Aceder `/changes` — change intelligence
2. Verificar blast radius e validação pós-deploy
3. Verificar correlação com incidentes via `/operations/incidents`

### Monitorizar confiabilidade de serviço

1. Aceder `/operations/reliability`
2. Verificar scoring de confiabilidade por serviço
3. Verificar SLIs/SLOs configurados

---

## Justificativa Arquitetural Final

### Por que Grafana foi removido

1. **Princípio Source of Truth** — O NexTraceOne é a fonte de verdade; depender de Grafana para visualização contradiz este princípio
2. **Complexidade operacional** — Três backends separados (Tempo, Loki, Grafana) aumentavam complexidade
3. **Autonomia do produto** — O produto deve ser auto-suficiente para operação e troubleshooting
4. **Provider-agnostic** — ClickHouse/Elastic como backends unificados simplificam a stack

### O que substitui Grafana

| Capacidade Grafana | Equivalente NexTraceOne |
|--------------------|------------------------|
| Dashboards de métricas | Telas internas de Platform Operations |
| Visualização de traces | Change Intelligence + consultas ClickHouse |
| Visualização de logs | Troubleshooting pages + consultas ClickHouse |
| Alertas | AlertGateway integrado com sistema de incidentes |
| Exploração ad-hoc | AI Assistant + consultas ClickHouse diretas |

### Gaps Conhecidos e Aceites

| Gap | Severidade | Status |
|-----|------------|--------|
| Dashboards visuais customizáveis | Baixa | Aceite — telas internas cobrem cenários operacionais críticos |
| Exploração ad-hoc sem ClickHouse CLI | Baixa | Aceite — AI Assistant oferece interface de investigação |

---

## Referências

- `docs/observability/architecture-overview.md` — Arquitetura completa de observabilidade
- `docs/observability/README.md` — Visão geral e princípios
- `docs/observability/troubleshooting.md` — Guia de troubleshooting
- `docs/audits/NEXTRACEONE-WAVE-0-BASELINE-REALIGNMENT.md` — Decisão de remoção do Grafana

---

> **Conclusão:** A superfície operacional do NexTraceOne sem Grafana está oficialmente definida, documentada e validada. Os operadores têm acesso claro a logs, traces, métricas, drift/anomalias, health e sinais operacionais críticos através das telas internas do produto, consultas ClickHouse e runbooks operacionais.
