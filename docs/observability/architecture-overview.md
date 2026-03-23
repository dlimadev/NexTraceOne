# Arquitetura de Observabilidade — NexTraceOne

## Objetivo

Este documento descreve a arquitetura de observabilidade do NexTraceOne: como a telemetria é recolhida, transportada, armazenada e consumida pelo produto.

A observabilidade no NexTraceOne é uma **capacidade interna da plataforma** que alimenta funcionalidades de governança, confiança em mudanças, análise assistida por IA e confiabilidade de serviços. Não é uma ferramenta de monitorização exposta diretamente ao utilizador como painel genérico.

---

## Visão antes e depois

### Antes (stack original)

Na concepção inicial, a stack de observabilidade baseava-se em:

```
Aplicação → Tempo (traces) + Loki (logs) + Grafana (visualização)
```

**Limitações identificadas:**

- Grafana como dependência externa para visualização — desalinhada com o objetivo de ser Source of Truth
- Tempo e Loki como backends separados dificultavam correlação unificada
- Sem abstração de provider — acoplamento direto à stack
- Sem suporte para ambientes IIS/Windows (apenas Kubernetes)
- Consultas de telemetria dispersas e não orientadas ao produto

### Depois (stack actual)

A arquitectura foi redesenhada com foco em:

- **Provider-agnostic**: ClickHouse (padrão) ou Elastic (enterprise)
- **Collection-agnostic**: OpenTelemetry Collector (Kubernetes) ou CLR Profiler (IIS)
- **Consumo interno**: o NexTraceOne consulta diretamente os providers, sem dashboards externos
- **IA integrada**: a IA interna do produto consulta telemetria para investigação e correlação

```
Aplicação → OTLP → OTel Collector / CLR Profiler → ClickHouse / Elastic → NexTraceOne Engine + IA
```

---

## Arquitetura alvo

```
┌─────────────────────────────────────────────────────────────────────┐
│                       NexTraceOne Platform                          │
│                                                                     │
│  Aplicação (.NET)                                                   │
│       │ OTLP (gRPC/HTTP)                                            │
│       ▼                                                             │
│  ┌──────────────────────────────────┐                               │
│  │   OpenTelemetry Collector        │ ◄── Kubernetes (padrão)       │
│  │   ou CLR Profiler                │ ◄── IIS / Windows             │
│  └───────────────┬──────────────────┘                               │
│                  │                                                   │
│          ┌───────┴────────┐                                         │
│          ▼                ▼                                          │
│     ClickHouse        Elastic                                       │
│     (padrão)          (enterprise)                                  │
│                                                                     │
│  NexTraceOne ──── consulta ────▶ Provider configurado               │
│  IA interna  ──── consulta ────▶ Provider configurado               │
│                                                                     │
│  ┌────────────────────────────────────────────────────────────┐     │
│  │                  Funcionalidades do produto                 │     │
│  │                                                            │     │
│  │  • Change Intelligence    • Incident Correlation           │     │
│  │  • Drift Detection        • Environment Comparison         │     │
│  │  • Release Risk Scoring   • AI-assisted Investigation      │     │
│  │  • Service Reliability    • Operational Consistency        │     │
│  └────────────────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────────────┘
```

### Fluxo alternativo para Kafka

```
┌──────────────────────────────────┐
│  Kafka Brokers / Connectors      │
│       │                          │
│       │ Métricas JMX / Logs      │
│       ▼                          │
│  Pipeline OTLP compatível        │
│       │                          │
│       ▼                          │
│  ClickHouse / Elastic            │
└──────────────────────────────────┘
```

---

## Separação de responsabilidades

A arquitectura separa claramente as responsabilidades de cada componente:

### PostgreSQL — dados de domínio

| Aspecto | Detalhe |
|---------|---------|
| **Papel** | Armazenamento transacional de dados de domínio |
| **Conteúdo** | Identidade, catálogo de serviços, operações, AI knowledge |
| **Bases lógicas** | `identity`, `catalog`, `operations`, `ai` |
| **Não contém** | Logs, traces, métricas de telemetria |
| **Justificação** | Dados de domínio requerem transacionalidade ACID e integridade referencial. Telemetria é append-only e de alto volume — misturar degradaria o desempenho. |

### ClickHouse / Elastic — dados de observabilidade

| Aspecto | Detalhe |
|---------|---------|
| **Papel** | Armazenamento analítico de telemetria |
| **Conteúdo** | Logs estruturados, traces distribuídos, métricas agregadas |
| **Tabelas (ClickHouse)** | `otel_logs`, `otel_traces`, `otel_metrics` |
| **TTL padrão** | 30 dias (configurável) |
| **Justificação** | Optimizado para queries analíticas sobre volumes massivos de dados append-only com compressão eficiente |

### OpenTelemetry Collector — pipeline de recolha

| Aspecto | Detalhe |
|---------|---------|
| **Papel** | Pipeline de processamento entre a aplicação e o armazenamento |
| **Responsabilidades** | Recepção (OTLP), normalização, filtragem, batching, sampling, exportação |
| **Receivers** | OTLP gRPC (porta 4317), OTLP HTTP (porta 4318) |
| **Exporters** | ClickHouse (padrão), configurável para Elastic |
| **Ambiente** | Kubernetes — DaemonSet ou Sidecar |

### CLR Profiler — recolha alternativa

| Aspecto | Detalhe |
|---------|---------|
| **Papel** | Instrumentação nativa do runtime .NET em ambientes Windows |
| **Responsabilidades** | Captura de traces e métricas ao nível do CLR sem necessidade de OTel Collector |
| **Ambiente** | IIS / Windows Server |
| **Justificação** | Em ambientes onde não é possível ou prático executar o OTel Collector como sidecar |

### NexTraceOne Engine — consumo e análise

| Aspecto | Detalhe |
|---------|---------|
| **Papel** | Consumo, correlação, agregação e análise de telemetria |
| **Responsabilidades** | Consultar o provider configurado, correlacionar com dados de domínio, alimentar IA, gerar insights |
| **Abstração** | `IObservabilityProvider` para consultas, `ITelemetryQueryService` para queries orientadas ao produto |

---

## Camada de abstração

A arquitectura define interfaces de abstração que permitem trocar providers e modos de recolha sem alterar código de produto.

### `IObservabilityProvider`

```
Localização: src/building-blocks/NexTraceOne.BuildingBlocks.Observability/
             Observability/Abstractions/IObservabilityProvider.cs
```

Interface principal para consultas de observabilidade. Implementações:

- `ClickHouseObservabilityProvider` — provider padrão, optimizado para queries analíticas
- `ElasticObservabilityProvider` — provider alternativo para ambientes enterprise com Elastic existente

Métodos principais:

- Consultar logs por serviço, ambiente, janela temporal
- Consultar traces por TraceId, serviço, duração
- Consultar métricas agregadas por serviço e período
- Pesquisa full-text em logs

### `ICollectionModeStrategy`

```
Localização: src/building-blocks/NexTraceOne.BuildingBlocks.Observability/
             Observability/Collection/
```

Interface para estratégias de recolha de telemetria. Implementações:

- `OpenTelemetryCollectorStrategy` — recolha via OTel Collector (Kubernetes)
- `ClrProfilerStrategy` — recolha via CLR Profiler (IIS/Windows)

### `ITelemetryQueryService`

Interface de consultas orientadas ao produto. Suporta:

- Frequência de erros por serviço
- Comparação de latência entre versões
- Análise de impacto de release
- Correlação mudança-incidente
- Investigação assistida por IA

---

## Modelagem de dados

Os modelos de telemetria estão definidos em:

```
src/building-blocks/NexTraceOne.BuildingBlocks.Observability/
    Observability/Models/TelemetryModels.cs
```

### Modelos principais

| Modelo | Descrição | Campos principais |
|--------|-----------|-------------------|
| **LogEntry** | Entrada de log estruturada | Timestamp, Level, Message, TraceId, SpanId, CorrelationId, TenantId, ServiceName, Environment, Attributes |
| **TraceSummary** | Resumo de trace para listagens | TraceId, RootServiceName, Duration, SpanCount, HasError, StartTime |
| **TraceDetail** | Trace completo com todos os spans | TraceId, Spans[], ServicesInvolved[], TotalDuration, RootSpan |
| **SpanDetail** | Span individual dentro de um trace | SpanId, TraceId, ParentSpanId, OperationName, ServiceName, StartTime, EndTime, Duration, Status, Attributes, Events[], Links[] |
| **TelemetryMetricPoint** | Ponto de métrica agregado | MetricName, Value, Timestamp, Dimensions (serviço, ambiente, equipa), Unit |

### Modelos de produto

Além dos modelos base de telemetria, existem modelos orientados ao produto:

```
src/building-blocks/NexTraceOne.BuildingBlocks.Observability/
    Telemetry/Models/
```

| Modelo | Descrição |
|--------|-----------|
| **InvestigationContext** | Contexto de investigação de incidente para IA |
| **ReleaseRuntimeCorrelation** | Correlação entre release e comportamento em runtime |

---

## Fluxo de dados

O fluxo completo desde a aplicação até à análise pelo produto:

### 1. Instrumentação

A aplicação .NET é instrumentada com:

- **Serilog** para logs estruturados (com enriquecimento de TraceId, CorrelationId, TenantId)
- **OpenTelemetry SDK** para traces distribuídos (activity sources NexTrace)
- **Métricas .NET** para métricas de runtime e de negócio

### 2. Exportação

Os sinais são exportados em formato **OTLP** (OpenTelemetry Protocol):

- gRPC (porta 4317) — protocolo padrão, mais eficiente
- HTTP (porta 4318) — alternativa para ambientes com restrições de gRPC

### 3. Recolha e processamento

**Em Kubernetes (modo padrão):**

```
Aplicação → OTLP → OpenTelemetry Collector → Processamento → Exportação
```

O OTel Collector aplica:

- Batching — agrupa sinais para exportação eficiente
- Normalização — garante formato consistente
- Filtragem — remove sinais desnecessários
- Sampling — amostragem para controlo de volume

**Em IIS/Windows (modo alternativo):**

```
Aplicação → CLR Profiler → Captura nativa → Exportação directa
```

### 4. Armazenamento

Os sinais processados são armazenados no provider configurado:

**ClickHouse (padrão):**

| Tabela | Conteúdo |
|--------|----------|
| `otel_logs` | Logs estruturados |
| `otel_traces` | Spans de traces distribuídos |
| `otel_metrics` | Pontos de métricas agregados |

Todas as tabelas têm TTL de 30 dias (configurável).

**Elastic (alternativa):**

Índices equivalentes com mapeamentos compatíveis com o schema OpenTelemetry.

### 5. Consumo pelo produto

O NexTraceOne Engine consulta o provider configurado via `IObservabilityProvider`:

```
NexTraceOne Engine → IObservabilityProvider → ClickHouse / Elastic → Resultados
```

Os resultados são utilizados por:

- **Change Intelligence** — comparar telemetria antes e depois de uma mudança
- **Incident Correlation** — identificar padrões em logs e traces relacionados com incidentes
- **Drift Detection** — detetar e comparar comportamento entre ambientes
- **AI-assisted Investigation** — fornecer contexto de telemetria à IA interna para investigação
- **Service Reliability** — calcular métricas de confiabilidade por serviço

### 6. Análise e apresentação

Os dados analisados são apresentados no contexto do produto:

- Dentro de páginas de serviço (Service Catalog)
- Dentro de análise de mudanças (Change Intelligence)
- Dentro de investigação de incidentes (Operations)
- Dentro de consultas da IA (AI Assistant)

Nunca como dashboards genéricos isolados.

---

## Fontes de sinais

### IIS / Windows

| Sinal | Descrição | Recolha |
|-------|-----------|---------|
| **Logs de aplicação** | Logs estruturados da aplicação .NET via Serilog | CLR Profiler / OTLP |
| **Traces de aplicação** | Traces distribuídos da aplicação .NET via OpenTelemetry SDK | CLR Profiler / OTLP |
| **Métricas de runtime** | CPU, memória, GC, thread pool do processo IIS | CLR Profiler |
| **Logs de IIS** | Access logs e error logs do web server | Pipeline de ingestão |

### Kubernetes

| Sinal | Descrição | Recolha |
|-------|-----------|---------|
| **Logs de pods** | stdout/stderr dos containers da aplicação | OTel Collector (filelog receiver) |
| **Logs de aplicação** | Logs estruturados via Serilog exportados em OTLP | OTel Collector (OTLP receiver) |
| **Traces de aplicação** | Traces distribuídos via OpenTelemetry SDK | OTel Collector (OTLP receiver) |
| **Métricas de container** | CPU, memória, rede por container | OTel Collector (kubeletstats receiver) |
| **Métricas de aplicação** | Métricas customizadas da aplicação .NET | OTel Collector (OTLP receiver) |

### Kafka

| Sinal | Descrição | Recolha |
|-------|-----------|---------|
| **Logs de broker** | Logs operacionais dos brokers Kafka | Pipeline compatível OTLP |
| **Métricas de consumer** | Lag, throughput, erros por consumer group | JMX / pipeline OTLP |
| **Métricas de producer** | Taxa de envio, erros, latência de produção | JMX / pipeline OTLP |
| **Métricas de tópico** | Partições, replicação, tamanho | JMX / pipeline OTLP |

A telemetria Kafka é correlacionada com:

- Contratos de eventos (Event Contracts) definidos no NexTraceOne
- Ownership de tópicos e consumer groups
- Mudanças em producers/consumers

---

## Retenção

### Política de retenção por camada

| Camada | Período | Descrição |
|--------|---------|-----------|
| **Hot** | 0–7 dias | Dados recentes, acesso frequente, queries rápidas |
| **Warm** | 7–30 dias | Dados recentes mas com acesso menos frequente |
| **Cold** | 30+ dias | Dados históricos, acesso raro, compressão máxima |

### TTL padrão

O schema do ClickHouse define TTL de **30 dias** por defeito nas tabelas de observabilidade:

```sql
-- Excerto simplificado de build/clickhouse/init-schema.sql
-- Consultar o ficheiro completo para a definição integral das tabelas
TTL timestamp + INTERVAL 30 DAY
```

Este valor é configurável por instalação. Ambientes com requisitos de auditoria podem estender para 90 ou 365 dias.

### Considerações

- A retenção no Elastic segue política equivalente via Index Lifecycle Management (ILM)
- Dados de domínio (PostgreSQL) não têm TTL — são permanentes
- A política de retenção deve estar alinhada com requisitos de compliance da organização
- Dados agregados (resumos, tendências) podem ser mantidos por períodos mais longos do que dados brutos

---

## Decisões técnicas

### Porquê ClickHouse como padrão?

| Factor | Justificação |
|--------|-------------|
| **Desempenho analítico** | ClickHouse é optimizado para queries analíticas sobre dados colunares de alto volume — exactamente o perfil de dados de telemetria |
| **Compressão** | Compressão colunar eficiente reduz significativamente o armazenamento necessário para logs e traces |
| **TTL nativo** | Suporte nativo a TTL por tabela simplifica políticas de retenção |
| **Compatibilidade OTLP** | Exporters nativos do OpenTelemetry Collector para ClickHouse |
| **Custo** | Open-source, sem licenciamento — reduz custo operacional |
| **Simplicidade** | Stack mais simples de operar comparado com Elastic (sem gestão de shards, réplicas, JVM tuning) |

### Porquê Elastic como alternativa?

| Factor | Justificação |
|--------|-------------|
| **Enterprise adoption** | Muitas organizações já têm Elastic em produção — permite reutilizar infraestrutura existente |
| **Full-text search** | Elastic tem capacidades superiores de pesquisa full-text em logs |
| **Ecossistema** | Integração com Kibana e outras ferramentas do ecossistema pode ser requisito organizacional |
| **Conformidade** | Algumas organizações exigem Elastic por requisitos de segurança ou compliance |

### Porquê OpenTelemetry Collector?

| Factor | Justificação |
|--------|-------------|
| **Standard aberto** | OpenTelemetry é o standard da indústria para telemetria — garante interoperabilidade |
| **Pipeline flexível** | Receivers, processors e exporters configuráveis permitem adaptar o pipeline sem código |
| **Provider-agnostic** | O mesmo collector exporta para ClickHouse ou Elastic — a mudança de provider não afecta a recolha |
| **Kubernetes-native** | Deployment como DaemonSet ou Sidecar é o padrão para ambientes Kubernetes |
| **Comunidade** | Ecossistema vasto de componentes mantidos pela comunidade CNCF |

### Porquê CLR Profiler para IIS?

| Factor | Justificação |
|--------|-------------|
| **Ambientes Windows** | Em IIS/Windows Server, executar o OTel Collector como sidecar não é prático nem idiomático |
| **Instrumentação nativa** | O CLR Profiler captura dados ao nível do runtime .NET sem overhead adicional de um collector externo |
| **Sem dependências** | Não requer containers adicionais ou processos auxiliares no servidor Windows |
| **Cobertura completa** | Captura traces, métricas de runtime e profiling data de forma integrada |

### Porquê não Tempo + Loki + Grafana?

| Factor | Justificação |
|--------|-------------|
| **Source of Truth** | O NexTraceOne é a Source of Truth — depender de Grafana para visualização contradiz este princípio |
| **Acoplamento** | Três backends separados (Tempo, Loki, Grafana) aumentam complexidade operacional |
| **Correlação** | Correlacionar dados entre Tempo e Loki é menos eficiente do que numa base unificada |
| **Controlo** | Com ClickHouse/Elastic, o produto controla totalmente as queries e a apresentação dos dados |
| **Flexibilidade** | A abstração por provider permite que a organização escolha o backend mais adequado |

---

## Referências

| Recurso | Localização |
|---------|-------------|
| README de observabilidade | [docs/observability/README.md](./README.md) |
| Estratégia de observabilidade | [docs/OBSERVABILITY-STRATEGY.md](../OBSERVABILITY-STRATEGY.md) |
| Arquitectura de telemetria | [docs/telemetry/TELEMETRY-ARCHITECTURE.md](../telemetry/TELEMETRY-ARCHITECTURE.md) |
| Arquitectura de dados | [docs/DATA-ARCHITECTURE.md](../DATA-ARCHITECTURE.md) |
| Modelos de telemetria | `src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Observability/Models/TelemetryModels.cs` |
| Abstrações de provider | `src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Observability/Abstractions/IObservabilityProvider.cs` |
| Provider ClickHouse | `src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Observability/Providers/ClickHouse/` |
| Provider Elastic | `src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Observability/Providers/Elastic/` |
| Schema ClickHouse | `build/clickhouse/init-schema.sql` |
| Config OTel Collector | `build/otel-collector/otel-collector.yaml` |
| Docker Compose | `docker-compose.yml` |
| Variáveis de ambiente | [docs/ENVIRONMENT-VARIABLES.md](../ENVIRONMENT-VARIABLES.md) |
