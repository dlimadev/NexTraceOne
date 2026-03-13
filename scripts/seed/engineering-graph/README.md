# Engineering Graph — Massa de Teste e Scripts de Seed

## Objetivo

Este diretório contém scripts para popular o módulo Engineering Graph com dados
de teste realistas, permitindo validação funcional do frontend, testes manuais,
e demonstrações de produto.

## Pré-requisitos

- API do NexTraceOne rodando localmente (`dotnet run --project src/platform/NexTraceOne.ApiHost`)
- PostgreSQL acessível e com migrations aplicadas
- `curl` e `jq` instalados no ambiente

## Scripts Disponíveis

| Script | Objetivo |
|--------|----------|
| `00-reset.sql` | Remove todos os dados do módulo (usar com cuidado) |
| `01-seed-via-api.sh` | Popula serviços, APIs, relações e snapshots via HTTP API |
| `02-test-sync-consumers.sh` | Testa o endpoint de integração inbound externa |

## Como Executar

```bash
# 1. Iniciar a API (em outro terminal)
dotnet run --project src/platform/NexTraceOne.ApiHost

# 2. Popular os dados de teste
chmod +x scripts/seed/engineering-graph/01-seed-via-api.sh
./scripts/seed/engineering-graph/01-seed-via-api.sh

# 3. Testar a integração inbound externa
chmod +x scripts/seed/engineering-graph/02-test-sync-consumers.sh
./scripts/seed/engineering-graph/02-test-sync-consumers.sh

# Para usar uma URL diferente:
./scripts/seed/engineering-graph/01-seed-via-api.sh http://localhost:8080/api/v1/engineeringgraph
```

## Dados Inseridos

### Serviços (10 serviços em 6 domínios)

| Serviço | Domínio | Time |
|---------|---------|------|
| payments-service | Payments | Payments Team |
| identity-service | Identity | Identity Squad |
| orders-service | Commerce | Core Commerce |
| catalog-service | Commerce | Core Commerce |
| checkout-service | Commerce | Checkout Squad |
| billing-service | Billing | Billing Team |
| invoicing-service | Billing | Billing Team |
| notifications-service | Notifications | Platform Team |
| analytics-service | Analytics | Data Team |
| api-gateway | Platform | Platform Team |

### APIs (11 APIs)

| API | Rota | Versão | Visibilidade | Proprietário |
|-----|------|--------|-------------|-------------|
| Payments API | /api/v1/payments | 2.1.0 | Internal | payments-service |
| Payments Webhook | /webhooks/payments | 1.0.0 | Public | payments-service |
| Auth API | /api/v1/auth | 3.0.0 | Internal | identity-service |
| Orders API | /api/v1/orders | 1.5.0 | Internal | orders-service |
| Catalog API | /api/v1/catalog | 2.0.0 | Public | catalog-service |
| Checkout API | /api/v1/checkout | 1.2.0 | Internal | checkout-service |
| Billing API | /api/v1/billing | 1.0.0 | Internal | billing-service |
| Invoicing API | /api/v1/invoicing | 1.1.0 | Internal | invoicing-service |
| Notifications API | /api/v1/notifications | 1.0.0 | Internal | notifications-service |
| Analytics API | /api/v1/analytics | 0.9.0 | Internal | analytics-service |
| API Gateway | /api/v1/gateway | 4.0.0 | Public | api-gateway |

### Relações de Consumo (~25 relações)

**Cenário: Serviço Crítico (API Gateway)**
- 9 consumidores diretos (todos os serviços consomem o gateway)
- Ideal para testar blast radius de alto impacto

**Cenário: Dependência Transversal (Auth API)**
- 8 consumidores (todos exceto api-gateway)
- Testa propagação de impacto transversal

**Cenário: Cadeia de Dependência (checkout → orders → billing)**
- Relações transitivas para testar propagação em profundidade

**Cenário: Serviço Isolado (analytics-service)**
- Apenas 1 relação de consumo com baixa confiança (0.55)
- Testa visualização de nós isolados

**Cenário: Confiança Variável**
- Scores de 0.55 a 0.99
- Testa badges de confiança e filtros

### Snapshots Temporais

- 1 snapshot baseline criado automaticamente
- Para testar diff temporal, execute o seed novamente após modificar dados

## Cenários de Frontend Testáveis

| Cenário | Como Testar |
|---------|-------------|
| **Grafo completo** | Aba "Graph" — visualiza 10 serviços, 11 APIs e ~25 relações |
| **Filtro por domínio** | Buscar "Payments" ou "Commerce" na aba Graph |
| **Serviço crítico** | Selecionar "api-gateway" na aba Impact — mostra 9 consumidores diretos |
| **Serviço isolado** | Selecionar "analytics-service" — mostra apenas 1 relação |
| **Blast radius** | Aba Impact → selecionar Auth API → profundidade 2+ |
| **Confiança variável** | Aba APIs → observar badges de confiança nas relações |
| **Cadeia transitiva** | Impact → checkout-service → profundidade 3 |
| **Temporal diff** | Aba Temporal → criar snapshot → modificar dados → criar novo snapshot → comparar |
| **Busca** | Digitar "billing" ou "payments" no campo de busca |
| **Empty state** | Executar 00-reset.sql → verificar mensagens de estado vazio |

## Cenários de Integração Inbound

| Cenário | Script |
|---------|--------|
| Criar novo consumidor | `02-test-sync-consumers.sh` Teste 1 |
| Atualizar existente (idempotência) | `02-test-sync-consumers.sh` Teste 2 |
| API não encontrada | `02-test-sync-consumers.sh` Teste 3 |
| Lote misto (sucesso + falha) | `02-test-sync-consumers.sh` Teste 4 |

## Notas

- Os scripts são idempotentes: executar múltiplas vezes não gera duplicatas (o backend retorna 409 Conflict)
- Para reset completo, use `00-reset.sql` antes de re-executar o seed
- Os IDs são gerados dinamicamente pelo backend — não são fixos
- Os scripts extraem IDs das respostas HTTP para manter referências corretas
