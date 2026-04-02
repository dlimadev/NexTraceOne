# NexTraceOne — Laboratory Environment

Ambiente de laboratório para testes reais de observabilidade, change intelligence e ingestão de telemetria do NexTraceOne.

## Objetivo

Este laboratório fornece:

- **3 fake APIs instrumentadas com OpenTelemetry** que simulam um ecossistema de microserviços
- **Elasticsearch** como provider de observabilidade
- **OpenTelemetry Collector** configurado com o pipeline real do NexTraceOne
- **Kibana** para visualização direta dos dados no Elasticsearch
- **PostgreSQL** para os serviços fake
- **Gerador de tráfego** para produzir telemetria realista
- **Postman Collection** com todos os endpoints, testes automáticos e fluxo E2E

## Arquitetura do Laboratório

```
┌──────────────┐    ┌──────────────┐    ┌──────────────────┐
│ order-service │───▶│payment-service│───▶│inventory-service │
│   :5010       │    │   :5020      │    │   :5030          │
└──────┬───────┘    └──────┬───────┘    └──────┬───────────┘
       │                   │                   │
       ▼                   ▼                   ▼
       ┌───────────────────────────────────────┐
       │     OpenTelemetry Collector :4317     │
       │         (gRPC) / :4318 (HTTP)        │
       └──────────────────┬────────────────────┘
                          │
                          ▼
       ┌───────────────────────────────────────┐
       │      Elasticsearch :9200              │
       │      Kibana :5601                     │
       └───────────────────────────────────────┘
```

## Serviços Fake

| Serviço            | Porta | Descrição                                 |
|--------------------|-------|-------------------------------------------|
| order-service      | 5010  | API de encomendas (REST) — cria, lista, consulta ordens |
| payment-service    | 5020  | API de pagamentos — processa e valida pagamentos        |
| inventory-service  | 5030  | API de inventário — consulta stock e reserva itens      |

### Fluxo de negócio simulado

1. `POST /api/orders` → cria encomenda no order-service
2. order-service chama `POST /api/payments` no payment-service
3. payment-service chama `POST /api/inventory/reserve` no inventory-service
4. Respostas propagam de volta com distributed tracing completo

## Como usar

### 1. Subir o laboratório

```bash
cd lab
docker compose -f docker-compose.lab.yml up -d
```

### 2. Verificar se tudo está a correr

```bash
docker compose -f docker-compose.lab.yml ps
```

### 3. Gerar tráfego de teste

```bash
# Criar uma encomenda (dispara chamadas em cadeia para payment e inventory)
curl -X POST http://localhost:5010/api/orders \
  -H "Content-Type: application/json" \
  -d '{"customerId": "cust-001", "items": [{"productId": "prod-100", "quantity": 2}]}'

# Listar encomendas
curl http://localhost:5010/api/orders

# Gerar tráfego contínuo (requer bash)
./scripts/generate-traffic.sh
```

### 4. Verificar telemetria no Elasticsearch

```bash
# Verificar traces
curl http://localhost:9200/nextraceone-obs-traces/_search?pretty&size=5

# Verificar logs
curl http://localhost:9200/nextraceone-obs-logs/_search?pretty&size=5

# Verificar métricas
curl http://localhost:9200/nextraceone-obs-metrics/_search?pretty&size=5
```

### 5. Usar a Postman Collection

A coleção Postman em `postman/NexTraceOne-Lab.postman_collection.json` cobre todos os endpoints com testes automáticos.

**Importar no Postman:**
1. Abrir o Postman → Import → selecionar `lab/postman/NexTraceOne-Lab.postman_collection.json`
2. Todas as variáveis (URLs, IDs) estão pré-configuradas com os defaults do lab

**Folders disponíveis:**

| Folder | Descrição |
|--------|-----------|
| 🏥 Health Checks | Saúde de todos os serviços (APIs + Elasticsearch + OTel Collector) |
| 📦 Order Service | CRUD completo de encomendas (list, create, get, cancel) |
| 💳 Payment Service | Processamento e consulta de pagamentos |
| 📦 Inventory Service | Consulta de stock, reservas e libertações |
| 🔄 E2E Flow | Fluxo completo em 6 passos (stock → order → verify → cancel) |
| 🔍 Observability Verification | Queries ao Elasticsearch para validar traces, logs e distributed trace |

**Variáveis automáticas:** `order_id`, `payment_id`, `reservation_id` e `trace_id` são preenchidas automaticamente pelos scripts de teste de cada request.

### 6. Aceder ao Kibana

Abrir no browser: http://localhost:5601

### 7. Parar o laboratório

```bash
docker compose -f docker-compose.lab.yml down -v
```

## Endpoints disponíveis

### order-service (:5010)

| Método | Rota                  | Descrição                       |
|--------|-----------------------|---------------------------------|
| GET    | /api/orders           | Lista todas as encomendas       |
| GET    | /api/orders/{id}      | Detalhe de uma encomenda        |
| POST   | /api/orders           | Criar nova encomenda            |
| DELETE | /api/orders/{id}      | Cancelar encomenda              |
| GET    | /health               | Health check                    |

### payment-service (:5020)

| Método | Rota                     | Descrição                       |
|--------|-------------------------|---------------------------------|
| POST   | /api/payments           | Processar pagamento             |
| GET    | /api/payments/{id}      | Consultar estado do pagamento   |
| GET    | /health                 | Health check                    |

### inventory-service (:5030)

| Método | Rota                        | Descrição                       |
|--------|----------------------------|---------------------------------|
| GET    | /api/inventory/{productId} | Consultar stock de um produto   |
| POST   | /api/inventory/reserve     | Reservar itens no inventário    |
| POST   | /api/inventory/release     | Libertar reserva                |
| GET    | /health                    | Health check                    |

## Variáveis de ambiente

Ver `.env.lab` para configuração completa. Valores padrão funcionam out-of-the-box.

## Notas

- Este ambiente é **apenas para desenvolvimento e testes**. Não usar em produção.
- O Elasticsearch não tem segurança activada (xpack.security.enabled=false).
- Os serviços fake geram erros aleatórios (~5%) para simular cenários reais.
- O OpenTelemetry Collector usa a mesma configuração de pipeline do NexTraceOne real.
