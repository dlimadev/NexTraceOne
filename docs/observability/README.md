# Observabilidade — NexTraceOne

## Objetivo

No NexTraceOne, a observabilidade **não é um fim em si mesma**. Não se trata de construir dashboards genéricos de métricas ou replicar ferramentas como Grafana, Datadog ou Dynatrace.

A observabilidade é a **camada técnica de dados** que alimenta:

- **Change Intelligence** — validação de mudanças em produção e análise de blast radius
- **AI-assisted Operations** — investigação assistida por IA, correlação de incidentes e recomendação de mitigação
- **Service Governance** — confiabilidade dos serviços, comparação entre ambientes e detecção de drift
- **Contract Governance** — correlação entre alterações em contratos e impacto observado em runtime
- **Operational Consistency** — análise contínua de consistência operacional por equipa, serviço e domínio

Toda telemetria recolhida serve o produto. Nunca é exposta como painel isolado sem contexto de serviço, mudança ou incidente.

---

## Princípios

| Princípio | Descrição |
|-----------|-----------|
| **Provider-agnostic** | O NexTraceOne suporta **ClickHouse** (padrão) e **Elastic** (enterprise) como backend de observabilidade. A escolha é por configuração, sem alteração de código. |
| **Recolha por ambiente** | Em Kubernetes, utiliza-se o **OpenTelemetry Collector**. Em IIS/Windows, utiliza-se o **CLR Profiler**. A estratégia de recolha é determinada pelo ambiente de execução. |
| **Source-aware** | Cada fonte de sinais (IIS, Kubernetes, Kafka) tem características próprias. O sistema conhece e respeita as particularidades de cada fonte. |
| **PostgreSQL apenas para domínio** | O PostgreSQL guarda exclusivamente dados transacionais de domínio (identidade, catálogo, operações, AI knowledge). Dados de observabilidade nunca vão para PostgreSQL. |
| **Produto primeiro** | Toda telemetria serve funcionalidades do produto. Não há exportação directa para dashboards externos. |

---

## Arquitetura resumida

```
┌──────────────────────────────────────────────────────────────────┐
│                       NexTraceOne Platform                       │
│                                                                  │
│  Aplicação (.NET)                                                │
│       │                                                          │
│       │ OTLP (gRPC / HTTP)                                       │
│       ▼                                                          │
│  ┌──────────────────────────────────┐                            │
│  │   OpenTelemetry Collector        │ ◄── Kubernetes (padrão)    │
│  │   ou CLR Profiler                │ ◄── IIS / Windows          │
│  └───────────────┬──────────────────┘                            │
│                  │                                                │
│          ┌───────┴────────┐                                      │
│          ▼                ▼                                       │
│     ClickHouse        Elastic                                    │
│     (padrão)          (enterprise)                               │
│          │                │                                      │
│          └───────┬────────┘                                      │
│                  ▼                                                │
│         NexTraceOne Engine                                       │
│    (consulta, correlação, IA, análise)                           │
└──────────────────────────────────────────────────────────────────┘
```

Para detalhes completos, consultar [architecture-overview.md](./architecture-overview.md).

---

## Combinações suportadas

O NexTraceOne suporta as seguintes combinações de fonte, recolha e armazenamento:

| Fonte | Modo de recolha | Provider | Estado |
|-------|----------------|----------|--------|
| **IIS / Windows** | CLR Profiler | ClickHouse | Suportado |
| **IIS / Windows** | CLR Profiler | Elastic | Suportado |
| **Kubernetes** | OpenTelemetry Collector | ClickHouse | Suportado (padrão) |
| **Kubernetes** | OpenTelemetry Collector | Elastic | Suportado |
| **Kafka** | Pipeline compatível | ClickHouse / Elastic | Suportado |

A configuração é feita via variáveis de ambiente:

```env
# Provider de observabilidade
OBSERVABILITY_PROVIDER=ClickHouse   # ou Elastic

# Modo de recolha
COLLECTION_MODE=OpenTelemetryCollector   # ou ClrProfiler
```

---

## Configuração rápida

Para configuração detalhada do ambiente, variáveis e providers, consultar:

- [Variáveis de ambiente](../ENVIRONMENT-VARIABLES.md)
- [Docker e Compose](../deployment/DOCKER-AND-COMPOSE.md)
- [Estratégia de observabilidade](../OBSERVABILITY-STRATEGY.md)

---

## Documentação disponível

### Nesta pasta (`docs/observability/`)

| Documento | Descrição |
|-----------|-----------|
| [README.md](./README.md) | Este documento — visão geral e índice |
| [architecture-overview.md](./architecture-overview.md) | Arquitetura completa de observabilidade, decisões técnicas e fluxo de dados |
| [DRIFT-DETECTION-PIPELINE.md](./DRIFT-DETECTION-PIPELINE.md) | Pipeline de deteção de drift entre ambientes |
| [ENVIRONMENT-COMPARISON-ARCHITECTURE.md](./ENVIRONMENT-COMPARISON-ARCHITECTURE.md) | Arquitetura de comparação entre ambientes (staging vs produção) |
| [INGESTION-API-ROLE-AND-FLOW.md](./INGESTION-API-ROLE-AND-FLOW.md) | Papel e fluxo da API de ingestão de dados de observabilidade |

### Documentação relacionada noutras pastas

| Documento | Descrição |
|-----------|-----------|
| [OBSERVABILITY-STRATEGY.md](../OBSERVABILITY-STRATEGY.md) | Estratégia global de observabilidade do produto |
| [TELEMETRY-ARCHITECTURE.md](../telemetry/TELEMETRY-ARCHITECTURE.md) | Arquitetura da fundação de telemetria |
| [DATA-ARCHITECTURE.md](../DATA-ARCHITECTURE.md) | Arquitetura de dados global (PostgreSQL + ClickHouse) |
| [DOCKER-AND-COMPOSE.md](../deployment/DOCKER-AND-COMPOSE.md) | Infraestrutura Docker e Compose |
| [ENVIRONMENT-VARIABLES.md](../ENVIRONMENT-VARIABLES.md) | Variáveis de ambiente de configuração |

---

## Sinais suportados

O NexTraceOne recolhe e processa três tipos de sinais de telemetria:

### Logs

Logs estruturados recolhidos via Serilog e exportados em formato OTLP. Incluem:

- Timestamp, nível (severity), mensagem
- TraceId e SpanId para correlação com traces
- CorrelationId para rastreabilidade de negócio
- TenantId para isolamento multi-tenant
- Atributos contextuais (serviço, ambiente, versão)

Armazenados na tabela `otel_logs` do ClickHouse (ou índice equivalente no Elastic).

### Traces

Traces distribuídos compatíveis com OpenTelemetry. Incluem:

- TraceId global que atravessa serviços
- Spans individuais com timestamps, duração, status e atributos
- Eventos e links entre spans
- Propagação de contexto entre serviços

Armazenados na tabela `otel_traces` do ClickHouse. Utilizados para análise de latência, detecção de erros e correlação de mudanças.

### Métricas

Métricas agregadas de runtime e de negócio. Incluem:

- CPU, memória, duração de pedidos, taxa de erros, throughput
- Métricas customizadas por serviço e operação
- Pontos de dados com dimensões (serviço, ambiente, equipa)

Armazenados na tabela `otel_metrics` do ClickHouse. Utilizados para análise de tendências e detecção de anomalias.

---

## Fontes suportadas

### IIS / Windows

- Logs de aplicação via Serilog
- Traces de aplicação via instrumentação .NET
- Recolha via **CLR Profiler** (instrumentação nativa do runtime .NET)
- Ideal para ambientes Windows Server com IIS como web server

### Kubernetes

- Logs de pods e containers
- Traces de aplicações instrumentadas com OpenTelemetry SDK
- Recolha via **OpenTelemetry Collector** como DaemonSet ou Sidecar
- Modo padrão para deployments cloud-native

### Kafka

- Logs de brokers e conectores
- Métricas de consumers e producers (lag, throughput, erros)
- Recolha via pipeline compatível com OTLP
- Correlação com tópicos, contratos de eventos e ownership

---

## Stack local

Para executar o NexTraceOne localmente com observabilidade completa:

```bash
# Iniciar toda a stack (inclui PostgreSQL, ClickHouse e OTel Collector)
docker compose up -d

# Verificar que os serviços estão a correr
docker compose ps
```

### Serviços de observabilidade no docker-compose

| Serviço | Portas | Descrição |
|---------|--------|-----------|
| **postgres** | 5432 | Base de dados de domínio (identity, catalog, operations, ai) |
| **clickhouse** | 8123 (HTTP), 9000 (Native) | Armazenamento de observabilidade |
| **otel-collector** | 4317 (gRPC), 4318 (HTTP), 8888 (Prometheus) | Pipeline de recolha de telemetria |

### Configuração padrão local

```env
OBSERVABILITY_PROVIDER=ClickHouse
COLLECTION_MODE=OpenTelemetryCollector
OpenTelemetry__Endpoint=http://otel-collector:4317
Telemetry__ObservabilityProvider__ClickHouse__ConnectionString=Host=clickhouse;Port=8123;Database=nextraceone_obs
```

### Schema do ClickHouse

O schema é inicializado automaticamente pelo ficheiro `build/clickhouse/init-schema.sql`, que cria as tabelas `otel_logs`, `otel_traces` e `otel_metrics` com TTL de 30 dias.

### Configuração do OTel Collector

A configuração do OpenTelemetry Collector está em `build/otel-collector/otel-collector.yaml` e define:

- Receivers: OTLP (gRPC e HTTP)
- Processors: batch, normalização
- Exporters: ClickHouse (padrão)

---

## Referências de código

| Componente | Localização |
|------------|-------------|
| Modelos de telemetria | `src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Observability/Models/TelemetryModels.cs` |
| Abstração de provider | `src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Observability/Abstractions/IObservabilityProvider.cs` |
| Provider ClickHouse | `src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Observability/Providers/ClickHouse/` |
| Provider Elastic | `src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Observability/Providers/Elastic/` |
| Estratégia OTel Collector | `src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Observability/Collection/OpenTelemetryCollector/` |
| Estratégia CLR Profiler | `src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Observability/Collection/ClrProfiler/` |
| Registo de dependências | `src/building-blocks/NexTraceOne.BuildingBlocks.Observability/DependencyInjection.cs` |
| Schema ClickHouse | `build/clickhouse/init-schema.sql` |
| Config OTel Collector | `build/otel-collector/otel-collector.yaml` |
