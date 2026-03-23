# Configuração de Observabilidade — NexTraceOne

> **Referência central para todas as opções de configuração de observabilidade do NexTraceOne.**
>
> Este documento descreve a estrutura completa de configuração da secção `Telemetry` no `appsettings.json`,
> incluindo providers de armazenamento, modos de coleta, fontes de dados, retenção e variáveis de ambiente.

---

## Índice

1. [Objetivo](#objetivo)
2. [Estrutura de configuração](#estrutura-de-configuração)
3. [Escolher provider de armazenamento](#escolher-provider-de-armazenamento)
4. [Escolher modo de coleta](#escolher-modo-de-coleta)
5. [Habilitar e desabilitar fontes](#habilitar-e-desabilitar-fontes)
6. [Configuração ClickHouse](#configuração-clickhouse)
7. [Configuração Elastic](#configuração-elastic)
8. [Configuração OTel Collector](#configuração-otel-collector)
9. [Configuração CLR Profiler](#configuração-clr-profiler)
10. [Variáveis de ambiente](#variáveis-de-ambiente)
11. [Configuração completa de exemplo](#configuração-completa-de-exemplo)
12. [Validação](#validação)
13. [Retenção](#retenção)
14. [Segurança](#segurança)

---

## Objetivo

A configuração de observabilidade do NexTraceOne é gerida através da secção `Telemetry` do ficheiro
`appsettings.json` e das variáveis de ambiente correspondentes. Esta secção controla:

- **Provider de armazenamento** — onde traces, logs e métricas são persistidos (ClickHouse ou Elastic).
- **Modo de coleta** — como os dados de telemetria são capturados (OpenTelemetry Collector ou CLR Profiler).
- **Fontes de dados** — quais origens estão ativas (IIS, Kubernetes, Kafka).
- **Políticas de retenção** — quanto tempo os dados são mantidos em cada camada (hot, warm, cold).

A classe de configuração correspondente é `TelemetryStoreOptions`, localizada em:

```
src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Telemetry/Configuration/TelemetryStoreOptions.cs
```

---

## Estrutura de configuração

A secção `Telemetry` no `appsettings.json` segue esta estrutura hierárquica:

```
Telemetry
├── ObservabilityProvider
│   ├── Provider               → "ClickHouse" | "Elastic"
│   ├── ClickHouse
│   │   ├── Enabled
│   │   ├── ConnectionString
│   │   ├── LogsRetentionDays
│   │   ├── TracesRetentionDays
│   │   └── MetricsRetentionDays
│   └── Elastic
│       ├── Enabled
│       ├── Endpoint
│       ├── ApiKey
│       └── IndexPrefix
├── CollectionMode
│   ├── ActiveMode             → "OpenTelemetryCollector" | "ClrProfiler"
│   ├── OpenTelemetryCollector
│   │   ├── Enabled
│   │   ├── OtlpGrpcEndpoint
│   │   └── OtlpHttpEndpoint
│   └── ClrProfiler
│       ├── Enabled
│       ├── Mode               → "IIS" | "SelfHosted"
│       ├── InstrumentationMode → "AutoInstrumentation" | "Manual"
│       ├── ExportTarget       → "Collector" | "Direct"
│       └── OtlpEndpoint
├── Sources
│   ├── IIS
│   │   ├── Enabled
│   │   └── CollectLogs
│   ├── Kubernetes
│   │   ├── Enabled
│   │   └── CollectContainerLogs
│   └── Kafka
│       ├── Enabled
│       ├── CollectBrokerLogs
│       └── CollectApplicationLogs
└── Retention
    ├── HotRetentionDays
    ├── WarmRetentionDays
    └── ColdRetentionDays
```

---

## Escolher provider de armazenamento

O NexTraceOne suporta dois providers de armazenamento de telemetria. A escolha deve ser feita com base
no cenário operacional, volume de dados e infraestrutura existente.

### Matriz de decisão

| Critério                        | ClickHouse                          | Elastic                              |
|---------------------------------|-------------------------------------|--------------------------------------|
| **Caso de uso principal**       | Análise de alto volume e agregação  | Pesquisa full-text e exploração      |
| **Volume de dados**             | Excelente para volumes massivos     | Bom para volumes moderados           |
| **Custo por TB armazenado**     | Baixo                               | Moderado a alto                      |
| **Pesquisa full-text**          | Limitada                            | Excelente                            |
| **Agregações analíticas**       | Excelente                           | Bom                                  |
| **Complexidade operacional**    | Moderada                            | Moderada a alta                      |
| **Infraestrutura existente**    | Cluster ClickHouse necessário       | Cluster Elastic necessário           |
| **Padrão do NexTraceOne**       | **Sim (default)**                   | Não                                  |
| **Recomendado para produção**   | Sim, para a maioria dos cenários    | Sim, se já existe cluster Elastic    |
| **Recomendado para dev/local**  | Sim (via Docker Compose)            | Possível, mas mais pesado            |

### Recomendação

- **Escolher ClickHouse** quando não existe infraestrutura Elastic prévia, quando o volume de dados é
  elevado, ou quando o foco é em agregações analíticas e custo-eficiência.
- **Escolher Elastic** quando já existe um cluster Elastic gerido na organização, quando a pesquisa
  full-text em logs é prioritária, ou quando a equipa tem experiência operacional com Elastic.

### Como configurar

No campo `Telemetry.ObservabilityProvider.Provider`, definir o valor:

```json
"Provider": "ClickHouse"
```

ou

```json
"Provider": "Elastic"
```

> **Nota:** Apenas um provider deve estar ativo de cada vez. Ativar ambos pode causar comportamento
> inesperado e duplicação de dados.

---

## Escolher modo de coleta

O modo de coleta determina como os dados de telemetria são capturados das aplicações e enviados
para o provider de armazenamento.

### Matriz de decisão

| Critério                        | OpenTelemetry Collector              | CLR Profiler                         |
|---------------------------------|--------------------------------------|--------------------------------------|
| **Caso de uso principal**       | Coleta padronizada via OTLP          | Instrumentação profunda de .NET      |
| **Protocolos suportados**       | OTLP gRPC, OTLP HTTP, Prometheus    | Apenas .NET CLR                      |
| **Impacto em performance**      | Mínimo                               | Baixo a moderado                     |
| **Profundidade de instrumentação** | Depende da instrumentação da app  | Captura automática de chamadas .NET  |
| **Flexibilidade**               | Alta (qualquer linguagem/framework)  | Apenas aplicações .NET               |
| **Complexidade de setup**       | Baixa a moderada                     | Moderada                             |
| **Padrão do NexTraceOne**       | **Sim (default)**                    | Não                                  |
| **Suporte a IIS**               | Via instrumentação manual            | Nativo com modo IIS                  |
| **Suporte a Kubernetes**        | Nativo via DaemonSet/Sidecar        | Requer configuração adicional        |

### Recomendação

- **Escolher OpenTelemetry Collector** como padrão para novos deployments, ambientes multi-linguagem
  e Kubernetes. É o modo recomendado e mais flexível.
- **Escolher CLR Profiler** quando se pretende instrumentação automática profunda de aplicações .NET,
  especialmente em ambientes IIS legados onde a instrumentação via SDK não é viável.

### Como configurar

```json
"ActiveMode": "OpenTelemetryCollector"
```

ou

```json
"ActiveMode": "ClrProfiler"
```

---

## Habilitar e desabilitar fontes

As fontes determinam quais origens de dados de telemetria estão ativas. Cada fonte pode ser
habilitada ou desabilitada independentemente.

### IIS

Habilitar quando existem aplicações .NET hospedadas em IIS que devem ser monitorizadas.

```json
"IIS": {
  "Enabled": true,
  "CollectLogs": true
}
```

- `Enabled` — Ativa a coleta de telemetria de aplicações IIS.
- `CollectLogs` — Quando `true`, captura logs do IIS além de traces e métricas.

### Kubernetes

Habilitar quando o NexTraceOne está a monitorizar workloads em Kubernetes.

```json
"Kubernetes": {
  "Enabled": true,
  "CollectContainerLogs": true
}
```

- `Enabled` — Ativa a coleta de telemetria de pods e containers Kubernetes.
- `CollectContainerLogs` — Quando `true`, captura stdout/stderr dos containers.

### Kafka

Habilitar quando existem brokers e aplicações Kafka que devem ser monitorizados.

```json
"Kafka": {
  "Enabled": true,
  "CollectBrokerLogs": true,
  "CollectApplicationLogs": true
}
```

- `Enabled` — Ativa a coleta de telemetria de Kafka.
- `CollectBrokerLogs` — Quando `true`, captura logs dos brokers Kafka (JMX metrics, broker events).
- `CollectApplicationLogs` — Quando `true`, captura logs das aplicações que produzem/consomem Kafka.

---

## Configuração ClickHouse

O ClickHouse é o provider de armazenamento padrão do NexTraceOne. É um motor de base de dados
columnar optimizado para consultas analíticas de alto volume.

### Opções de configuração

| Opção                  | Tipo     | Default                                                                 | Descrição                                         |
|------------------------|----------|-------------------------------------------------------------------------|----------------------------------------------------|
| `Enabled`              | `bool`   | `true`                                                                  | Ativa o provider ClickHouse                        |
| `ConnectionString`     | `string` | `Host=clickhouse;Port=8123;Database=nextraceone_obs;Username=default;Password=` | String de conexão ao ClickHouse           |
| `LogsRetentionDays`    | `int`    | `30`                                                                    | Dias de retenção para logs raw                     |
| `TracesRetentionDays`  | `int`    | `30`                                                                    | Dias de retenção para traces raw                   |
| `MetricsRetentionDays` | `int`    | `90`                                                                    | Dias de retenção para métricas raw                 |

### Exemplo de configuração

```json
"ClickHouse": {
  "Enabled": true,
  "ConnectionString": "Host=clickhouse;Port=8123;Database=nextraceone_obs;Username=default;Password=",
  "LogsRetentionDays": 30,
  "TracesRetentionDays": 30,
  "MetricsRetentionDays": 90
}
```

### Notas sobre a connection string

- **Host** — Hostname ou IP do servidor ClickHouse. Em Docker Compose, usar o nome do serviço (`clickhouse`).
- **Port** — Porta HTTP do ClickHouse. O padrão é `8123`. Para conexão nativa, usar `9000`.
- **Database** — Nome da base de dados. Será criada automaticamente no primeiro arranque se não existir.
- **Username** — Utilizador ClickHouse. O padrão é `default`.
- **Password** — Palavra-passe do utilizador. Deve ser configurada via variável de ambiente `CLICKHOUSE_PASSWORD` em produção.

### Dimensionamento de retenção

Para estimar o espaço em disco necessário, considerar:

- **Logs** — ~1 KB por entrada de log comprimida. A 100.000 entradas/dia durante 30 dias: ~3 GB.
- **Traces** — ~2-5 KB por span comprimido. A 500.000 spans/dia durante 30 dias: ~30-75 GB.
- **Métricas** — ~0,1 KB por ponto de dados comprimido. A 1.000.000 pontos/dia durante 90 dias: ~9 GB.

> **Atenção:** Estes valores são estimativas. O volume real depende da cardinalidade dos dados,
> nível de compressão e configuração dos codecs do ClickHouse.

---

## Configuração Elastic

O Elastic é um provider alternativo para cenários onde a pesquisa full-text é prioritária ou onde
já existe infraestrutura Elastic na organização.

### Opções de configuração

| Opção          | Tipo     | Default                              | Descrição                                    |
|----------------|----------|--------------------------------------|----------------------------------------------|
| `Enabled`      | `bool`   | `false`                              | Ativa o provider Elastic                     |
| `Endpoint`     | `string` | `https://elastic.example.com:9200`   | URL do cluster Elastic                       |
| `ApiKey`       | `string` | `""`                                 | API key para autenticação                    |
| `IndexPrefix`  | `string` | `nextraceone`                        | Prefixo para os índices criados              |

### Exemplo de configuração

```json
"Elastic": {
  "Enabled": true,
  "Endpoint": "https://elastic.example.com:9200",
  "ApiKey": "base64-encoded-api-key",
  "IndexPrefix": "nextraceone"
}
```

### Índices criados automaticamente

O NexTraceOne cria os seguintes índices no Elastic, usando o prefixo configurado:

| Índice                           | Conteúdo                  |
|----------------------------------|---------------------------|
| `{prefix}-logs-*`               | Logs de aplicação         |
| `{prefix}-traces-*`             | Spans e traces            |
| `{prefix}-metrics-*`            | Métricas e séries         |

### Notas de segurança

- A `ApiKey` deve ser configurada via variável de ambiente `ELASTIC_API_KEY`, nunca diretamente
  no `appsettings.json`.
- O endpoint deve usar HTTPS em produção.
- Verificar que a API key tem permissões de leitura e escrita nos índices com o prefixo configurado.

---

## Configuração OTel Collector

O OpenTelemetry Collector é o modo de coleta padrão. Recebe dados via protocolo OTLP (gRPC e HTTP)
e encaminha para o provider de armazenamento configurado.

### Opções de configuração

| Opção               | Tipo     | Default                           | Descrição                                    |
|----------------------|----------|-----------------------------------|----------------------------------------------|
| `Enabled`            | `bool`   | `true`                            | Ativa o modo OpenTelemetry Collector         |
| `OtlpGrpcEndpoint`  | `string` | `http://otel-collector:4317`      | Endpoint OTLP gRPC do collector              |
| `OtlpHttpEndpoint`  | `string` | `http://otel-collector:4318`      | Endpoint OTLP HTTP do collector              |

### Exemplo de configuração

```json
"OpenTelemetryCollector": {
  "Enabled": true,
  "OtlpGrpcEndpoint": "http://otel-collector:4317",
  "OtlpHttpEndpoint": "http://otel-collector:4318"
}
```

### Portas padrão do OTel Collector

| Porta  | Protocolo  | Descrição                                                    |
|--------|------------|--------------------------------------------------------------|
| `4317` | gRPC       | Receptor OTLP gRPC — preferido para comunicação interna      |
| `4318` | HTTP       | Receptor OTLP HTTP — para clientes que não suportam gRPC     |

### Quando usar cada protocolo

- **gRPC (4317)** — Preferido para comunicação entre serviços internos. Mais eficiente, suporta
  streaming e compressão nativa.
- **HTTP (4318)** — Usar quando gRPC não é viável (proxies HTTP, firewalls restritivas, clientes
  JavaScript/browser).

### Configuração no Docker Compose

No `docker-compose.yml` do NexTraceOne, o OTel Collector está disponível como serviço `otel-collector`.
As aplicações do stack (apihost, workers, ingestion) enviam dados automaticamente para este serviço.

---

## Configuração CLR Profiler

O CLR Profiler permite instrumentação automática de aplicações .NET sem alteração de código.
É particularmente útil para aplicações legadas em IIS.

### Opções de configuração

| Opção                  | Tipo     | Default                      | Descrição                                          |
|------------------------|----------|------------------------------|-----------------------------------------------------|
| `Enabled`              | `bool`   | `false`                      | Ativa o modo CLR Profiler                           |
| `Mode`                 | `string` | `IIS`                        | Modo de operação: `IIS` ou `SelfHosted`             |
| `InstrumentationMode`  | `string` | `AutoInstrumentation`        | Modo de instrumentação: `AutoInstrumentation` ou `Manual` |
| `ExportTarget`         | `string` | `Collector`                  | Destino de exportação: `Collector` ou `Direct`      |
| `OtlpEndpoint`         | `string` | `http://otel-collector:4317` | Endpoint OTLP para envio de dados                   |

### Exemplo de configuração

```json
"ClrProfiler": {
  "Enabled": true,
  "Mode": "IIS",
  "InstrumentationMode": "AutoInstrumentation",
  "ExportTarget": "Collector",
  "OtlpEndpoint": "http://otel-collector:4317"
}
```

### Modos de operação

| Modo           | Descrição                                                                   |
|----------------|-----------------------------------------------------------------------------|
| `IIS`          | Para aplicações hospedadas em IIS. O profiler é carregado via módulo HTTP.  |
| `SelfHosted`   | Para aplicações .NET self-hosted (Kestrel, console apps, Windows Services). |

### Modos de instrumentação

| Modo                   | Descrição                                                                 |
|------------------------|---------------------------------------------------------------------------|
| `AutoInstrumentation`  | Captura automática de chamadas HTTP, SQL, gRPC e outras dependências.     |
| `Manual`               | Apenas captura spans criados explicitamente via API do OpenTelemetry.     |

### Destinos de exportação

| Destino      | Descrição                                                                      |
|--------------|--------------------------------------------------------------------------------|
| `Collector`  | Envia dados para o OTel Collector, que depois encaminha para o provider.       |
| `Direct`     | Envia dados diretamente para o provider de armazenamento (sem collector).      |

> **Recomendação:** Usar `Collector` como destino de exportação, mesmo com CLR Profiler. Isto permite
> que o OTel Collector aplique processamento, batching e retry antes de enviar para o provider.

---

## Variáveis de ambiente

As seguintes variáveis de ambiente podem ser usadas para sobrepor configurações do `appsettings.json`.
Em produção, **todas as credenciais devem ser configuradas exclusivamente via variáveis de ambiente**.

### Tabela completa

| Variável                  | Descrição                                          | Valor padrão                    | Obrigatória em produção |
|---------------------------|----------------------------------------------------|---------------------------------|-------------------------|
| `OBSERVABILITY_PROVIDER`  | Provider de armazenamento (`ClickHouse` ou `Elastic`) | `ClickHouse`                 | Não                     |
| `CLICKHOUSE_PASSWORD`     | Palavra-passe do utilizador ClickHouse             | (vazio)                         | **Sim** (se ClickHouse) |
| `COLLECTION_MODE`         | Modo de coleta (`OpenTelemetryCollector` ou `ClrProfiler`) | `OpenTelemetryCollector` | Não                     |
| `ELASTIC_ENDPOINT`        | URL do cluster Elastic                             | (não definido)                  | **Sim** (se Elastic)    |
| `ELASTIC_API_KEY`         | API key para autenticação no Elastic               | (não definido)                  | **Sim** (se Elastic)    |

### Exemplo de uso em Docker Compose

```yaml
services:
  apihost:
    environment:
      - OBSERVABILITY_PROVIDER=ClickHouse
      - CLICKHOUSE_PASSWORD=${CLICKHOUSE_PASSWORD}
      - COLLECTION_MODE=OpenTelemetryCollector
```

### Exemplo de uso em Kubernetes

```yaml
env:
  - name: OBSERVABILITY_PROVIDER
    value: "ClickHouse"
  - name: CLICKHOUSE_PASSWORD
    valueFrom:
      secretKeyRef:
        name: nextraceone-secrets
        key: clickhouse-password
  - name: COLLECTION_MODE
    value: "OpenTelemetryCollector"
```

---

## Configuração completa de exemplo

### Exemplo com ClickHouse (recomendado)

```json
{
  "Telemetry": {
    "ObservabilityProvider": {
      "Provider": "ClickHouse",
      "ClickHouse": {
        "Enabled": true,
        "ConnectionString": "Host=clickhouse;Port=8123;Database=nextraceone_obs;Username=default;Password=",
        "LogsRetentionDays": 30,
        "TracesRetentionDays": 30,
        "MetricsRetentionDays": 90
      },
      "Elastic": {
        "Enabled": false,
        "Endpoint": "https://elastic.example.com:9200",
        "ApiKey": "",
        "IndexPrefix": "nextraceone"
      }
    },
    "CollectionMode": {
      "ActiveMode": "OpenTelemetryCollector",
      "OpenTelemetryCollector": {
        "Enabled": true,
        "OtlpGrpcEndpoint": "http://otel-collector:4317",
        "OtlpHttpEndpoint": "http://otel-collector:4318"
      },
      "ClrProfiler": {
        "Enabled": false,
        "Mode": "IIS",
        "InstrumentationMode": "AutoInstrumentation",
        "ExportTarget": "Collector",
        "OtlpEndpoint": "http://otel-collector:4317"
      }
    },
    "Sources": {
      "IIS": {
        "Enabled": false,
        "CollectLogs": true
      },
      "Kubernetes": {
        "Enabled": true,
        "CollectContainerLogs": true
      },
      "Kafka": {
        "Enabled": false,
        "CollectBrokerLogs": true,
        "CollectApplicationLogs": true
      }
    },
    "Retention": {
      "HotRetentionDays": 30,
      "WarmRetentionDays": 90,
      "ColdRetentionDays": 365
    }
  }
}
```

### Exemplo com Elastic

```json
{
  "Telemetry": {
    "ObservabilityProvider": {
      "Provider": "Elastic",
      "ClickHouse": {
        "Enabled": false,
        "ConnectionString": "",
        "LogsRetentionDays": 30,
        "TracesRetentionDays": 30,
        "MetricsRetentionDays": 90
      },
      "Elastic": {
        "Enabled": true,
        "Endpoint": "https://elastic.production.internal:9200",
        "ApiKey": "",
        "IndexPrefix": "nextraceone-prod"
      }
    },
    "CollectionMode": {
      "ActiveMode": "OpenTelemetryCollector",
      "OpenTelemetryCollector": {
        "Enabled": true,
        "OtlpGrpcEndpoint": "http://otel-collector:4317",
        "OtlpHttpEndpoint": "http://otel-collector:4318"
      },
      "ClrProfiler": {
        "Enabled": false,
        "Mode": "SelfHosted",
        "InstrumentationMode": "AutoInstrumentation",
        "ExportTarget": "Collector",
        "OtlpEndpoint": "http://otel-collector:4317"
      }
    },
    "Sources": {
      "IIS": {
        "Enabled": false,
        "CollectLogs": true
      },
      "Kubernetes": {
        "Enabled": true,
        "CollectContainerLogs": true
      },
      "Kafka": {
        "Enabled": true,
        "CollectBrokerLogs": true,
        "CollectApplicationLogs": true
      }
    },
    "Retention": {
      "HotRetentionDays": 30,
      "WarmRetentionDays": 90,
      "ColdRetentionDays": 365
    }
  }
}
```

> **Nota:** A `ApiKey` do Elastic deve ser definida via variável de ambiente `ELASTIC_API_KEY`,
> nunca diretamente no ficheiro de configuração.

---

## Validação

Após alterar a configuração de observabilidade, é importante validar que tudo está correto
antes de colocar em produção.

### Lista de verificação

1. **Provider ativo corresponde ao `Enabled`** — Se `Provider` é `ClickHouse`, então
   `ClickHouse.Enabled` deve ser `true` e `Elastic.Enabled` deve ser `false` (e vice-versa).

2. **Modo de coleta ativo corresponde ao `Enabled`** — Se `ActiveMode` é `OpenTelemetryCollector`,
   então `OpenTelemetryCollector.Enabled` deve ser `true`.

3. **Endpoints acessíveis** — Verificar que os endpoints configurados (ClickHouse, Elastic,
   OTel Collector) são acessíveis a partir dos containers/pods da aplicação.

4. **Credenciais configuradas** — Verificar que palavras-passe e API keys estão definidas via
   variáveis de ambiente.

5. **Retenção adequada** — Verificar que os valores de retenção são adequados ao volume de dados
   e capacidade de armazenamento disponível.

### Validação via logs de arranque

Ao iniciar a aplicação, o NexTraceOne regista nos logs a configuração de observabilidade ativa:

```
[INF] Observability provider: ClickHouse
[INF] Collection mode: OpenTelemetryCollector
[INF] Sources enabled: Kubernetes
[INF] Retention policy: hot=30d, warm=90d, cold=365d
```

Se houver problemas de configuração, serão registados warnings ou errors:

```
[WRN] Observability provider mismatch: Provider=ClickHouse but ClickHouse.Enabled=false
[ERR] Failed to connect to ClickHouse: Connection refused (Host=clickhouse, Port=8123)
```

### Validação via endpoint de saúde

O endpoint `/health` inclui verificações dos componentes de observabilidade:

```bash
curl -s http://localhost:5000/health | jq '.checks[] | select(.name | startswith("observability"))'
```

---

## Retenção

O NexTraceOne implementa uma estratégia de retenção em três camadas para optimizar o equilíbrio
entre custo de armazenamento e disponibilidade de dados históricos.

### Camadas de retenção

| Camada   | Conteúdo                                    | Retenção padrão | Configuração           |
|----------|---------------------------------------------|-----------------|------------------------|
| **Hot**  | Traces e logs raw (detalhe completo)        | 30 dias         | `HotRetentionDays`     |
| **Warm** | Agregados e resumos (métricas, percentis)   | 90 dias         | `WarmRetentionDays`    |
| **Cold** | Snapshots e dados de compliance             | 365 dias        | `ColdRetentionDays`    |

### Como funciona

1. **Hot (30 dias)** — Dados raw completos, incluindo todos os spans, logs e métricas individuais.
   Permite drill-down completo em qualquer trace ou log. Consultas rápidas com baixa latência.

2. **Warm (90 dias)** — Dados são agregados automaticamente. Traces individuais são removidos,
   mas estatísticas (p50, p95, p99, error rates, throughput) são mantidas por serviço, endpoint
   e operação. Permite análise de tendências e comparações históricas.

3. **Cold (365 dias)** — Apenas snapshots periódicos e dados necessários para compliance e auditoria.
   Inclui resumos diários, alertas disparados e mudanças de configuração.

### Ajustar retenção

Para aumentar ou diminuir a retenção, alterar os valores no `appsettings.json`:

```json
"Retention": {
  "HotRetentionDays": 14,
  "WarmRetentionDays": 60,
  "ColdRetentionDays": 180
}
```

> **Atenção:** Reduzir a retenção hot abaixo de 7 dias pode limitar a capacidade de investigação
> de incidentes. Aumentar acima de 60 dias pode ter impacto significativo no armazenamento.

### Retenção no ClickHouse vs Elastic

- **ClickHouse** — A retenção é aplicada adicionalmente via `LogsRetentionDays`, `TracesRetentionDays`
  e `MetricsRetentionDays` no bloco `ClickHouse`. Estes valores controlam a retenção dos dados raw
  no ClickHouse e devem estar alinhados com o `HotRetentionDays`.

- **Elastic** — A retenção é gerida via Index Lifecycle Management (ILM). O NexTraceOne cria
  políticas ILM automaticamente com base nos valores de retenção configurados.

---

## Segurança

### Regras obrigatórias

1. **Nunca colocar palavras-passe ou API keys no `appsettings.json`** — Usar sempre variáveis de
   ambiente ou secret managers (Azure Key Vault, AWS Secrets Manager, Kubernetes Secrets).

2. **Usar HTTPS para endpoints Elastic em produção** — Comunicação não cifrada expõe dados de
   telemetria e credenciais.

3. **Restringir permissões de API keys** — No Elastic, criar API keys com permissões mínimas
   (leitura e escrita apenas nos índices com o prefixo configurado).

4. **Rotação de credenciais** — Implementar rotação periódica de palavras-passe ClickHouse e
   API keys Elastic.

5. **Segmentação de rede** — Os serviços de armazenamento (ClickHouse, Elastic) e o OTel Collector
   devem estar numa rede interna, não expostos à internet.

### Exemplo seguro com Docker Compose

```yaml
services:
  apihost:
    environment:
      - CLICKHOUSE_PASSWORD=${CLICKHOUSE_PASSWORD}
      - ELASTIC_API_KEY=${ELASTIC_API_KEY}
    # NÃO usar valores literais:
    # - CLICKHOUSE_PASSWORD=minha-password  ← ERRADO
```

### Exemplo seguro com Kubernetes

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: nextraceone-secrets
type: Opaque
data:
  clickhouse-password: <base64-encoded-value>
  elastic-api-key: <base64-encoded-value>
---
env:
  - name: CLICKHOUSE_PASSWORD
    valueFrom:
      secretKeyRef:
        name: nextraceone-secrets
        key: clickhouse-password
```

---

> **Documento mantido pela equipa NexTraceOne.**
> Para questões ou sugestões, abrir uma issue no repositório com a label `docs/observability`.
