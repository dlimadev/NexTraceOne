# Engineering Graph — API de Integração Inbound Externa

## Visão Geral

O NexTraceOne expõe uma API de integração inbound segura para que plataformas
externas (API gateways, service meshes, CI/CD, catálogos de serviços, etc.)
possam enviar dados de consumidores e relações de dependência para o Engineering Graph.

Esta API foi projetada como padrão reutilizável para futuras integrações inbound
em outros módulos do produto.

## Endpoint Principal

### POST `/api/v1/engineeringgraph/integration/v1/consumers/sync`

Sincroniza consumidores vindos de sistemas externos. Suporta criação e atualização
(upsert) de relações de consumo em lote.

#### Request

```json
{
  "items": [
    {
      "apiAssetId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "consumerName": "billing-service",
      "consumerKind": "Service",
      "consumerEnvironment": "Production",
      "externalReference": "kong-gateway/billing-integration",
      "confidenceScore": 0.92
    }
  ],
  "sourceSystem": "KongGateway",
  "correlationId": "req-12345"
}
```

#### Campos do Request

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `items` | array | Sim | Lista de consumidores a sincronizar (máx. 100) |
| `items[].apiAssetId` | UUID | Sim | ID do ativo de API consumido |
| `items[].consumerName` | string | Sim | Nome do serviço consumidor (máx. 200 chars) |
| `items[].consumerKind` | string | Sim | Tipo do consumidor: Service, Job, Lambda (máx. 100 chars) |
| `items[].consumerEnvironment` | string | Sim | Ambiente: Production, Staging, Development (máx. 100 chars) |
| `items[].externalReference` | string | Sim | Referência externa para rastreabilidade (máx. 500 chars) |
| `items[].confidenceScore` | decimal | Sim | Score de confiança da relação (0.01 a 1.0) |
| `sourceSystem` | string | Sim | Nome do sistema de origem (máx. 200 chars) |
| `correlationId` | string | Não | ID de correlação para rastreamento |

#### Response (200 OK)

```json
{
  "results": [
    {
      "apiAssetId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "consumerName": "billing-service",
      "outcome": "Created",
      "errorCode": null
    }
  ],
  "created": 1,
  "updated": 0,
  "failed": 0,
  "total": 1,
  "correlationId": "req-12345"
}
```

#### Campos do Response

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `results` | array | Resultado individual de cada item |
| `results[].outcome` | string | `Created`, `Updated` ou `Failed` |
| `results[].errorCode` | string? | Código de erro i18n quando `outcome == Failed` |
| `created` | int | Quantidade de relações criadas |
| `updated` | int | Quantidade de relações atualizadas |
| `failed` | int | Quantidade de falhas |
| `total` | int | Total de itens processados |
| `correlationId` | string? | ID de correlação ecoado do request |

#### Erros Possíveis por Item

| Código | Descrição |
|--------|-----------|
| `EngineeringGraph.ApiAsset.NotFound` | API asset não encontrado |
| `EngineeringGraph.ApiAsset.Decommissioned` | API asset descomissionado |

#### Códigos HTTP

| Código | Descrição |
|--------|-----------|
| 200 | Processamento concluído (pode conter falhas individuais) |
| 400 | Payload inválido (validação FluentValidation) |
| 401 | Não autenticado |
| 403 | Sem permissão |

## Comportamento de Idempotência

A chave de idempotência é a combinação `ApiAssetId + ConsumerName`.

- Se a relação não existir → **Created** (cria nova relação)
- Se a relação já existir → **Updated** (atualiza confidenceScore, sourceType e lastObservedAt)
- Se o ApiAssetId não existir → **Failed** com código de erro

Isto garante que chamadas repetidas com os mesmos dados não criam duplicatas.

## Autenticação e Autorização

### Sistema-a-Sistema

A API utiliza o mesmo mecanismo de autenticação da plataforma:
- **JWT Bearer Token** para autenticação
- **Tenant scoping** automático via header ou token claims
- **Permissões** verificadas pelo pipeline de autorização

Para integração sistema-a-sistema, recomenda-se:
1. Criar um usuário de serviço (service account) no módulo Identity
2. Atribuir a role `IntegrationWriter` ou permissão equivalente
3. Obter token via endpoint `/api/v1/identity/login`
4. Enviar token no header `Authorization: Bearer <token>`

### Tenant Scoping

Todas as operações são automaticamente escopadas ao tenant do token.
Dados de um tenant não vazam para outro.

## Versionamento

A API de integração usa versionamento explícito na rota:
- `/integration/v1/consumers/sync` — versão atual

Futuras versões serão adicionadas sem quebrar a versão anterior:
- `/integration/v2/consumers/sync` — quando houver breaking changes

## Validação de Payload

O payload é validado por FluentValidation antes de chegar ao handler:

- `items` não pode ser vazio
- Máximo de 100 itens por lote
- `sourceSystem` obrigatório
- Cada item: `apiAssetId`, `consumerName`, `consumerKind`, `consumerEnvironment`, `externalReference` obrigatórios
- `confidenceScore` entre 0.01 e 1.0

Erros de validação retornam 400 com detalhes estruturados.

## Exemplos de Integração

### curl

```bash
curl -X POST http://localhost:5000/api/v1/engineeringgraph/integration/v1/consumers/sync \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{
    "items": [
      {
        "apiAssetId": "API_ASSET_UUID",
        "consumerName": "external-service",
        "consumerKind": "Service",
        "consumerEnvironment": "Production",
        "externalReference": "kong/route-123",
        "confidenceScore": 0.90
      }
    ],
    "sourceSystem": "KongGateway",
    "correlationId": "sync-20260313-001"
  }'
```

### Python

```python
import requests

response = requests.post(
    "http://localhost:5000/api/v1/engineeringgraph/integration/v1/consumers/sync",
    headers={
        "Content-Type": "application/json",
        "Authorization": f"Bearer {token}",
    },
    json={
        "items": [
            {
                "apiAssetId": "API_ASSET_UUID",
                "consumerName": "external-service",
                "consumerKind": "Service",
                "consumerEnvironment": "Production",
                "externalReference": "istio/sidecar-123",
                "confidenceScore": 0.85,
            }
        ],
        "sourceSystem": "IstioServiceMesh",
        "correlationId": "sync-batch-001",
    },
)
result = response.json()
print(f"Created: {result['created']}, Updated: {result['updated']}, Failed: {result['failed']}")
```

## Evolução Futura

Esta API de integração foi desenhada como padrão reutilizável:

1. **Outros tipos de dados**: O mesmo padrão (batch sync + idempotência + correlação)
   pode ser aplicado para ingestão de contratos, métricas de runtime, snapshots de custo, etc.
2. **Batch assíncrono**: Para lotes muito grandes (>100 itens), uma futura versão pode
   aceitar upload assíncrono com callback ou polling de status.
3. **Eventos**: Os dados ingeridos podem gerar Integration Events para notificar outros
   módulos (ex: ChangeIntelligence pode recalcular blast radius quando novos consumidores são descobertos).
4. **Webhooks inversos**: Futuras versões podem suportar webhooks de notificação quando
   o estado de um consumidor mudar.

## Auditabilidade

Todas as operações de sincronização:
- Passam pelo pipeline de auditoria (AuditInterceptor)
- São rastreáveis pelo `correlationId`
- Registam o `sourceSystem` para proveniência
- Respeitam o tenant do token autenticado

## Observabilidade

- Logs em inglês com structured logging (Serilog)
- Tracing OpenTelemetry para cada operação de sync
- Métricas de contadores (created/updated/failed) por sourceSystem
