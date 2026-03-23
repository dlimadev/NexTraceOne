# Elastic — Provider de Observabilidade Alternativo

> **Módulo:** Observability · **Provider:** Elastic  
> **Papel no produto:** Integração com stacks Elastic existentes para centralizar
> telemetria do NexTraceOne.

---

## Índice

1. [Objetivo](#objetivo)
2. [Quando usar](#quando-usar)
3. [Quando não usar](#quando-não-usar)
4. [Pré-requisitos](#pré-requisitos)
5. [Arquitetura resumida](#arquitetura-resumida)
6. [Configuração](#configuração)
7. [Integração](#integração)
8. [Autenticação](#autenticação)
9. [Organização lógica](#organização-lógica)
10. [Validação](#validação)
11. [Troubleshooting](#troubleshooting)
12. [Segurança](#segurança)
13. [Limitações](#limitações)
14. [Próximos passos](#próximos-passos)

---

## Objetivo

O Elastic é o **provider alternativo de observabilidade** do NexTraceOne, desenhado
para integração com **stacks Elastic já existentes** na organização. Permite que
empresas que já investiram no ecossistema Elastic (Elasticsearch, Kibana, APM) possam
centralizar a telemetria do NexTraceOne na mesma plataforma que já utilizam para outros
serviços.

> **Princípio fundamental:** O NexTraceOne **não provisiona nem gere infraestrutura
> Elastic**. A responsabilidade de disponibilizar, escalar e manter o cluster
> Elasticsearch é da equipa de infraestrutura/plataforma da organização.

---

## Quando usar

| Cenário | Recomendação |
|---|---|
| Empresa já possui cluster Elasticsearch operacional | ✅ Recomendado |
| Equipa de plataforma gere Elastic Cloud | ✅ Recomendado |
| Requisito de observabilidade centralizada em Kibana | ✅ Recomendado |
| Políticas internas exigem um único backend de observabilidade | ✅ Recomendado |
| Equipa já tem dashboards e alertas configurados em Kibana | ✅ Ideal para evitar duplicação |

---

## Quando não usar

| Cenário | Alternativa |
|---|---|
| Instalação limpa sem Elastic existente | Usar o [provider ClickHouse](clickhouse.md) (padrão) |
| Ambientes pequenos / desenvolvimento local | Usar ClickHouse — mais leve e já incluído no docker-compose |
| Sem equipa dedicada a gerir o cluster Elastic | Usar ClickHouse — autónomo e auto-contido |
| Budget limitado para licenciamento Elastic | Usar ClickHouse — open source e sem custos de licença |

> **Nota:** Provisionar um cluster Elasticsearch apenas para o NexTraceOne é
> desaconselhado. O ClickHouse é uma solução mais leve e eficiente para este propósito.

---

## Pré-requisitos

- **Cluster Elasticsearch** operacional (versão ≥ 8.x recomendada)
- **API Key** ou credenciais de acesso com permissão de escrita
- **Acesso de rede** do ambiente NexTraceOne ao endpoint Elasticsearch (porta 9200
  ou a porta configurada)
- **TLS configurado** no cluster (recomendado para produção)
- **Capacidade de disco** adequada no cluster para o volume de telemetria esperado
- **ILM (Index Lifecycle Management)** configurado ou disponível para configurar

---

## Arquitetura resumida

```
┌─────────────────────────────────────────────────────────┐
│                  Ambiente NexTraceOne                    │
│                                                         │
│  ┌───────────────┐         ┌──────────────────────┐     │
│  │  Serviços do  │  OTLP   │  OpenTelemetry       │     │
│  │  NexTraceOne  │ ──────► │  Collector           │     │
│  └───────────────┘         └──────────┬───────────┘     │
│                                       │                 │
└───────────────────────────────────────┼─────────────────┘
                                        │
                              HTTPS :9200
                                        │
┌───────────────────────────────────────▼─────────────────┐
│              Infraestrutura Elastic (externa)            │
│                                                         │
│  ┌─────────────────┐    ┌──────────────────────────┐    │
│  │  Elasticsearch  │    │  Kibana (opcional)        │    │
│  │                 │    │                           │    │
│  │  nextraceone-   │    │  Dashboards, alertas,     │    │
│  │  logs-*         │    │  APM, visualizações       │    │
│  │  nextraceone-   │    │                           │    │
│  │  traces-*       │    └──────────────────────────┘    │
│  │  nextraceone-   │                                    │
│  │  metrics-*      │                                    │
│  └─────────────────┘                                    │
│                                                         │
│  ⚠️ Gerido pela equipa de infraestrutura/plataforma     │
└─────────────────────────────────────────────────────────┘
```

**Fluxo de dados:**

1. Os serviços do NexTraceOne exportam telemetria via OTLP para o Collector.
2. O OTel Collector processa os dados e envia para o Elasticsearch externo via HTTP/HTTPS.
3. Os dados são indexados no Elasticsearch com o prefixo `nextraceone-`.
4. A equipa pode utilizar o Kibana para visualizar e analisar os dados.

> **IMPORTANTE:** O NexTraceOne **não inclui** Elasticsearch nem Kibana no seu
> `docker-compose.yml`. A integração pressupõe infraestrutura Elastic já existente.

---

## Configuração

### appsettings.json

A configuração do provider Elastic está na secção
`Telemetry:ObservabilityProvider`:

```json
{
  "Telemetry": {
    "ObservabilityProvider": {
      "Provider": "Elastic",
      "Elastic": {
        "Enabled": true,
        "Endpoint": "https://elastic.example.com:9200",
        "ApiKey": "your-api-key",
        "IndexPrefix": "nextraceone",
        "LogsRetentionDays": 30,
        "TracesRetentionDays": 30,
        "MetricsRetentionDays": 90
      }
    }
  }
}
```

| Propriedade | Descrição | Valor padrão |
|---|---|---|
| `Provider` | Provider ativo de observabilidade | `"Elastic"` |
| `Enabled` | Ativa/desativa o provider | `false` |
| `Endpoint` | URL completa do cluster Elasticsearch | *(obrigatório)* |
| `ApiKey` | API Key para autenticação | *(obrigatório se usar API Key)* |
| `IndexPrefix` | Prefixo para os índices criados | `"nextraceone"` |
| `LogsRetentionDays` | Retenção desejada de logs em dias | `30` |
| `TracesRetentionDays` | Retenção desejada de traces em dias | `30` |
| `MetricsRetentionDays` | Retenção desejada de métricas em dias | `90` |

### Ativar o provider Elastic

Para trocar de ClickHouse para Elastic, alterar a propriedade `Provider`:

```json
{
  "Telemetry": {
    "ObservabilityProvider": {
      "Provider": "Elastic",
      "ClickHouse": {
        "Enabled": false
      },
      "Elastic": {
        "Enabled": true,
        "Endpoint": "https://elastic.example.com:9200",
        "ApiKey": "your-api-key",
        "IndexPrefix": "nextraceone"
      }
    }
  }
}
```

### Variáveis de ambiente

As seguintes variáveis de ambiente permitem sobrescrever a configuração em runtime:

| Variável | Descrição | Exemplo |
|---|---|---|
| `OBSERVABILITY_PROVIDER` | Provider ativo | `Elastic` |
| `ELASTIC_ENDPOINT` | URL do Elasticsearch | `https://elastic.example.com:9200` |
| `ELASTIC_API_KEY` | API Key | `VnVhQ2ZHY0JDZ...` |
| `ELASTIC_INDEX_PREFIX` | Prefixo dos índices | `nextraceone` |
| `ELASTIC_USERNAME` | Utilizador (auth básica) | `nextraceone_writer` |
| `ELASTIC_PASSWORD` | Password (auth básica) | *(sensível)* |
| `ELASTIC_LOGS_RETENTION_DAYS` | Retenção de logs | `30` |
| `ELASTIC_TRACES_RETENTION_DAYS` | Retenção de traces | `30` |
| `ELASTIC_METRICS_RETENTION_DAYS` | Retenção de métricas | `90` |

### Exemplo com Docker Compose (override)

```yaml
# docker-compose.override.yml
services:
  apihost:
    environment:
      OBSERVABILITY_PROVIDER: Elastic
      ELASTIC_ENDPOINT: https://elastic.example.com:9200
      ELASTIC_API_KEY: ${ELASTIC_API_KEY}
      ELASTIC_INDEX_PREFIX: nextraceone
```

---

## Integração

### Conexão ao Elasticsearch

O NexTraceOne liga-se ao Elasticsearch existente via **HTTP/HTTPS** na porta
configurada (tipicamente 9200). A comunicação é feita através do OTel Collector
configurado com um exporter Elasticsearch.

### Convenção de nomenclatura de índices

O NexTraceOne cria índices com a seguinte convenção:

| Sinal | Padrão do índice | Exemplo |
|---|---|---|
| Logs | `{prefix}-logs-*` | `nextraceone-logs-2025.01.15` |
| Traces | `{prefix}-traces-*` | `nextraceone-traces-2025.01.15` |
| Métricas | `{prefix}-metrics-*` | `nextraceone-metrics-2025.01.15` |

O `{prefix}` é configurável via `IndexPrefix` (padrão: `nextraceone`).

### Mapeamento de dados

Os dados de telemetria seguem o schema OpenTelemetry e são mapeados para campos
Elasticsearch conforme o [OTLP/Elasticsearch mapping](https://opentelemetry.io/docs/specs/otlp/):

| Campo OTel | Campo Elasticsearch | Tipo |
|---|---|---|
| `Timestamp` | `@timestamp` | `date_nanos` |
| `ServiceName` | `resource.service.name` | `keyword` |
| `SeverityText` | `severity_text` | `keyword` |
| `Body` | `body` | `text` |
| `TraceId` | `trace_id` | `keyword` |
| `SpanId` | `span_id` | `keyword` |

---

## Autenticação

### API Key (recomendado)

A autenticação por API Key é o método **recomendado** por ser mais segura e
granular.

#### Criar uma API Key no Elasticsearch

```bash
curl -X POST "https://elastic.example.com:9200/_security/api_key" \
  -H "Content-Type: application/json" \
  -u admin:password \
  -d '{
    "name": "nextraceone-writer",
    "role_descriptors": {
      "nextraceone_writer": {
        "cluster": ["monitor"],
        "index": [
          {
            "names": ["nextraceone-*"],
            "privileges": ["create_index", "write", "read", "manage"]
          }
        ]
      }
    },
    "expiration": "365d"
  }'
```

A resposta contém o campo `encoded` — esse é o valor a configurar em `ApiKey`:

```json
{
  "id": "VuaCfGcBCdzIhVnVhczQ",
  "name": "nextraceone-writer",
  "encoded": "VnVhQ2ZHY0JDZHpJaFZuVmhjejQ6...",
  "expiration": 1735689600000
}
```

#### Configurar no NexTraceOne

```json
{
  "Elastic": {
    "ApiKey": "VnVhQ2ZHY0JDZHpJaFZuVmhjejQ6..."
  }
}
```

### Autenticação básica (username/password)

Para ambientes onde API Keys não estão disponíveis:

```json
{
  "Elastic": {
    "Endpoint": "https://elastic.example.com:9200",
    "Username": "nextraceone_writer",
    "Password": "S3cur3P@ssw0rd!"
  }
}
```

> ⚠️ **Atenção:** Autenticação básica é menos segura. Preferir sempre API Keys.
> Se usar autenticação básica, garantir que as credenciais não ficam em ficheiros
> de configuração versionados — usar variáveis de ambiente ou secret managers.

### Prioridade de autenticação

1. **API Key** — se configurada, é utilizada prioritariamente.
2. **Username/Password** — utilizado como fallback.
3. **Sem autenticação** — apenas para clusters de desenvolvimento sem segurança.

---

## Organização lógica

### Um índice por tipo de sinal

O NexTraceOne cria **índices separados** para cada tipo de sinal de telemetria:

```
nextraceone-logs-2025.01.15
nextraceone-logs-2025.01.16
nextraceone-traces-2025.01.15
nextraceone-traces-2025.01.16
nextraceone-metrics-2025.01.15
nextraceone-metrics-2025.01.16
```

### Data Streams (recomendado para Elastic ≥ 8.x)

Em clusters Elastic 8.x+, os índices podem ser geridos como **Data Streams**:

```
nextraceone-logs    → data stream com backing indices
nextraceone-traces  → data stream com backing indices
nextraceone-metrics → data stream com backing indices
```

### ILM (Index Lifecycle Management)

A retenção de dados no Elasticsearch é gerida via **ILM policies**. O NexTraceOne
**não cria nem gere ILM policies automaticamente** — esta responsabilidade é do
administrador do cluster Elastic.

#### Exemplo de ILM policy recomendada

```json
{
  "policy": {
    "phases": {
      "hot": {
        "min_age": "0ms",
        "actions": {
          "rollover": {
            "max_primary_shard_size": "50gb",
            "max_age": "1d"
          }
        }
      },
      "warm": {
        "min_age": "7d",
        "actions": {
          "shrink": { "number_of_shards": 1 },
          "forcemerge": { "max_num_segments": 1 }
        }
      },
      "delete": {
        "min_age": "30d",
        "actions": {
          "delete": {}
        }
      }
    }
  }
}
```

#### Aplicar a ILM policy

```bash
# Criar a policy
curl -X PUT "https://elastic.example.com:9200/_ilm/policy/nextraceone-logs-policy" \
  -H "Content-Type: application/json" \
  -d @ilm-policy.json

# Associar ao index template
curl -X PUT "https://elastic.example.com:9200/_index_template/nextraceone-logs" \
  -H "Content-Type: application/json" \
  -d '{
    "index_patterns": ["nextraceone-logs-*"],
    "template": {
      "settings": {
        "index.lifecycle.name": "nextraceone-logs-policy"
      }
    }
  }'
```

> **Recomendação:** Criar policies separadas para logs (30 dias), traces (30 dias) e
> métricas (90 dias), alinhadas com os valores configurados no NexTraceOne.

---

## Validação

### Verificar conectividade

```bash
# Ping ao cluster
curl -s -o /dev/null -w "%{http_code}" \
  -H "Authorization: ApiKey VnVhQ2ZHY0JDZHpJaFZuVmhjejQ6..." \
  "https://elastic.example.com:9200"
# Resposta esperada: 200

# Verificar saúde do cluster
curl -s \
  -H "Authorization: ApiKey VnVhQ2ZHY0JDZHpJaFZuVmhjejQ6..." \
  "https://elastic.example.com:9200/_cluster/health" | python3 -m json.tool
```

### Verificar índices do NexTraceOne

```bash
curl -s \
  -H "Authorization: ApiKey VnVhQ2ZHY0JDZHpJaFZuVmhjejQ6..." \
  "https://elastic.example.com:9200/_cat/indices/nextraceone-*?v&s=index"
```

Saída esperada:

```
health status index                         uuid                   pri rep docs.count store.size
green  open   nextraceone-logs-2025.01.15   abc123...              1   1   15234      12.3mb
green  open   nextraceone-traces-2025.01.15 def456...              1   1   8921       8.1mb
green  open   nextraceone-metrics-2025.01.15 ghi789...             1   1   45123      3.2mb
```

### Verificar ingestão de logs

```bash
curl -s \
  -H "Authorization: ApiKey VnVhQ2ZHY0JDZHpJaFZuVmhjejQ6..." \
  "https://elastic.example.com:9200/nextraceone-logs-*/_count"
# Resposta: {"count":15234,"_shards":{"total":1,"successful":1,"skipped":0,"failed":0}}
```

### Verificar documentos recentes

```bash
curl -s \
  -H "Authorization: ApiKey VnVhQ2ZHY0JDZHpJaFZuVmhjejQ6..." \
  "https://elastic.example.com:9200/nextraceone-logs-*/_search?size=5&sort=@timestamp:desc" \
  | python3 -m json.tool
```

### Verificar via Kibana

Se o Kibana está disponível:

1. Aceder ao Kibana.
2. Ir a **Stack Management → Index Patterns**.
3. Criar um index pattern `nextraceone-logs-*`.
4. Ir a **Discover** e selecionar o index pattern criado.
5. Verificar que os documentos aparecem com os campos esperados.

---

## Troubleshooting

### Erro de conexão recusada

**Causa:** Endpoint inacessível, firewall, ou DNS incorreto.

```bash
# Testar conectividade TCP
nc -zv elastic.example.com 9200

# Testar resolução DNS
nslookup elastic.example.com

# Testar via curl
curl -v "https://elastic.example.com:9200"
```

### Erro 401 — Unauthorized

**Causa:** API Key inválida, expirada, ou credenciais incorretas.

```bash
# Verificar API Key
curl -s -w "\nHTTP Status: %{http_code}\n" \
  -H "Authorization: ApiKey VnVhQ2ZHY0JDZHpJaFZuVmhjejQ6..." \
  "https://elastic.example.com:9200/_security/_authenticate"
```

**Soluções:**
- Verificar se a API Key não expirou.
- Regenerar a API Key se necessário.
- Confirmar que a API Key tem as permissões corretas sobre `nextraceone-*`.

### Erro 403 — Forbidden

**Causa:** A API Key não tem permissões suficientes.

Verificar que a role associada à API Key inclui:
- `create_index` nos índices `nextraceone-*`
- `write` nos índices `nextraceone-*`
- `read` nos índices `nextraceone-*`

### Índices não são criados

**Causa:** O OTel Collector não está a enviar dados, ou o exporter não está configurado.

```bash
# Verificar logs do OTel Collector
docker compose logs otel-collector --tail 50

# Verificar configuração do exporter Elasticsearch
cat build/otel-collector/otel-collector.yaml
```

### Erro de certificado TLS

**Causa:** Certificado auto-assinado ou CA não reconhecida.

**Soluções:**
- Adicionar a CA ao trust store do container.
- Em desenvolvimento, configurar `tls.insecure_skip_verify: true` no OTel Collector
  (nunca em produção).

### Cluster em estado yellow ou red

**Causa:** Problema no cluster Elasticsearch (não relacionado com o NexTraceOne).

```bash
# Verificar saúde
curl -s "https://elastic.example.com:9200/_cluster/health?pretty"

# Verificar shards não alocados
curl -s "https://elastic.example.com:9200/_cat/shards?v&s=state&h=index,shard,prirep,state,unassigned.reason" \
  | grep -i unassigned
```

> **Nota:** Problemas de saúde do cluster devem ser resolvidos pela equipa de
> infraestrutura Elastic, não pela equipa NexTraceOne.

---

## Segurança

### API Key — boas práticas

| Prática | Descrição |
|---|---|
| **Rotação periódica** | Rotacionar API Keys a cada 90–365 dias |
| **Princípio do menor privilégio** | Conceder apenas permissões necessárias (`nextraceone-*`) |
| **Expiração** | Definir sempre uma data de expiração na API Key |
| **Não versionar** | Nunca guardar API Keys em ficheiros de configuração no Git |
| **Secret manager** | Usar HashiCorp Vault, AWS Secrets Manager, Azure Key Vault, etc. |

### Rotação de API Key

```bash
# 1. Criar nova API Key
curl -X POST "https://elastic.example.com:9200/_security/api_key" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "nextraceone-writer-v2",
    "role_descriptors": { ... },
    "expiration": "365d"
  }'

# 2. Atualizar a configuração do NexTraceOne com a nova key

# 3. Verificar que a ingestão funciona com a nova key

# 4. Invalidar a key antiga
curl -X DELETE "https://elastic.example.com:9200/_security/api_key" \
  -H "Content-Type: application/json" \
  -d '{"ids": ["id-da-key-antiga"]}'
```

### TLS

- **Obrigatório** em produção.
- Verificar que o endpoint usa `https://`.
- Garantir que o certificado do cluster é válido e reconhecido.
- Em ambientes com certificados internos, adicionar a CA ao trust store.

### Isolamento de rede

- O acesso ao Elasticsearch deve ser restrito por firewall/security groups.
- Apenas o OTel Collector e os serviços NexTraceOne devem ter acesso ao endpoint.
- Em Kubernetes, usar `NetworkPolicy` para restringir o tráfego de saída.

### Dados sensíveis na telemetria

- Garantir que dados PII não são enviados como atributos de telemetria.
- Configurar processadores no OTel Collector para filtrar/mascarar campos sensíveis.
- Rever periodicamente os dados indexados para detetar informação sensível.

---

## Limitações

| Limitação | Impacto | Mitigação |
|---|---|---|
| **NexTraceOne não gere infraestrutura Elastic** | A equipa de plataforma é responsável pela disponibilidade, escalabilidade e manutenção do cluster | Documentar responsabilidades claramente |
| **ILM deve ser configurado externamente** | Sem ILM, os índices crescem indefinidamente | Criar ILM policies alinhadas com a retenção configurada |
| **Não inclui Kibana** | Visualizações e dashboards devem ser criados pela equipa | Fornecer templates de dashboard no futuro |
| **Licenciamento Elastic** | Funcionalidades avançadas (ML, alerting) requerem licença | Avaliar custo-benefício vs ClickHouse |
| **Latência de rede** | O cluster é externo — latência pode impactar ingestão | Garantir proximidade de rede e avaliar buffering no Collector |
| **Sem provisão local** | Não há docker-compose para Elastic | Intencional — evita complexidade desnecessária para dev local |

---

## Próximos passos

- 📖 [Provider ClickHouse](clickhouse.md) — provider padrão para ambientes sem
  Elastic existente.
- 📖 [Configuração do módulo de observabilidade](../configuration/) — opções avançadas
  de configuração e modos de coleta.
- 📖 [Modos de coleta](../collection/) — diferenças entre OpenTelemetry Collector e
  Direct Push.
- 📖 [Arquitetura de observabilidade](../architecture-overview.md) — visão geral da
  arquitetura de observabilidade do NexTraceOne.
