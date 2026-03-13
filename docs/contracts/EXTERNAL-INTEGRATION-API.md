# Contracts & Interoperability — API de Integração Externa

## Visão Geral

Este documento descreve as APIs públicas do módulo Contracts & Interoperability para integração
com sistemas externos (CI/CD, ferramentas de governança, plataformas de API management).

## Autenticação

Todas as requisições requerem:
- Header `Authorization: Bearer <access_token>` (JWT) ou `X-Api-Key: <api_key>`
- Header `X-Tenant-Id: <tenant_id>` (obrigatório em ambiente multi-tenant)

## Endpoints

### 1. Importar Contrato

Importa uma nova versão de contrato para um API Asset existente.

```http
POST /api/v1/contracts
Content-Type: application/json

{
  "apiAssetId": "550e8400-e29b-41d4-a716-446655440000",
  "semVer": "1.0.0",
  "specContent": "{ \"openapi\": \"3.0.3\", ... }",
  "format": "json",
  "importedFrom": "ci-pipeline",
  "protocol": "OpenApi"
}
```

**Resposta (201 Created):**
```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "semVer": "1.0.0",
  "lifecycleState": "Draft"
}
```

**Protocolos suportados:** `OpenApi`, `Swagger`, `Wsdl`, `AsyncApi`, `Protobuf`, `GraphQl`

**Formatos suportados:** `json`, `yaml`, `xml`

### 2. Computar Diff Semântico

Compara duas versões de contrato e retorna as mudanças categorizadas.

```http
POST /api/v1/contracts/diff
Content-Type: application/json

{
  "baseVersionId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "targetVersionId": "8d0e7790-8536-51ef-a55c-f18gd2g01bf8"
}
```

**Resposta (200 OK):**
```json
{
  "diffId": "9e1f8891-9647-62f0-b66d-g29he3h12cg9",
  "baseVersionId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "targetVersionId": "8d0e7790-8536-51ef-a55c-f18gd2g01bf8",
  "changeLevel": "Breaking",
  "suggestedSemVer": "2.0.0",
  "breakingChanges": [
    {
      "changeType": "PathRemoved",
      "path": "/users/{id}",
      "method": null,
      "description": "Path '/users/{id}' was removed.",
      "isBreaking": true
    }
  ],
  "nonBreakingChanges": [],
  "additiveChanges": [
    {
      "changeType": "PathAdded",
      "path": "/v2/users",
      "method": null,
      "description": "Path '/v2/users' was added.",
      "isBreaking": false
    }
  ]
}
```

### 3. Histórico de Versões

Retorna todas as versões de contrato para um API Asset.

```http
GET /api/v1/contracts/history/{apiAssetId}
```

**Resposta (200 OK):**
```json
[
  {
    "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "semVer": "1.0.0",
    "protocol": "OpenApi",
    "lifecycleState": "Locked",
    "isLocked": true,
    "createdAt": "2026-03-01T10:00:00Z"
  }
]
```

### 4. Detalhes da Versão

```http
GET /api/v1/contracts/{contractVersionId}/detail
```

**Resposta (200 OK):**
```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "apiAssetId": "550e8400-e29b-41d4-a716-446655440000",
  "semVer": "1.0.0",
  "specContent": "...",
  "format": "json",
  "protocol": "OpenApi",
  "lifecycleState": "Locked",
  "isLocked": true,
  "lockedAt": "2026-03-10T15:30:00Z",
  "lockedBy": "admin@company.com",
  "fingerprint": "abc123...",
  "algorithm": "SHA-256",
  "signedBy": "admin@company.com",
  "signedAt": "2026-03-10T15:31:00Z",
  "importedFrom": "ci-pipeline",
  "createdAt": "2026-03-01T10:00:00Z"
}
```

### 5. Bloquear Versão

```http
POST /api/v1/contracts/{contractVersionId}/lock
Content-Type: application/json

{
  "lockedBy": "admin@company.com"
}
```

### 6. Assinar Versão

```http
POST /api/v1/contracts/{contractVersionId}/sign
Content-Type: application/json

{
  "contractVersionId": "7c9e6679-7425-40de-944b-e07fc1f90ae7"
}
```

### 7. Verificar Assinatura

```http
GET /api/v1/contracts/{contractVersionId}/verify
```

**Resposta (200 OK):**
```json
{
  "isValid": true,
  "fingerprint": "abc123...",
  "verifiedAt": "2026-03-13T19:00:00Z"
}
```

### 8. Transicionar Lifecycle

```http
POST /api/v1/contracts/{contractVersionId}/lifecycle
Content-Type: application/json

{
  "contractVersionId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "newState": "InReview"
}
```

### 9. Depreciar Versão

```http
POST /api/v1/contracts/{contractVersionId}/deprecate
Content-Type: application/json

{
  "contractVersionId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "deprecationNotice": "Use v2.0 instead. This version will be sunset on 2026-06-01.",
  "sunsetDate": "2026-06-01T00:00:00Z"
}
```

### 10. Exportar Especificação

```http
GET /api/v1/contracts/{contractVersionId}/export
```

**Resposta (200 OK):**
```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "semVer": "1.0.0",
  "specContent": "{ \"openapi\": \"3.0.3\", ... }",
  "format": "json"
}
```

## Códigos de Erro

| Código | Chave i18n | Descrição |
|--------|-----------|-----------|
| 404 | `Contracts.ContractVersion.NotFound` | Versão não encontrada |
| 409 | `Contracts.ContractVersion.AlreadyExists` | SemVer já existe para o API Asset |
| 409 | `Contracts.ContractVersion.AlreadyLocked` | Versão já bloqueada |
| 400 | `Contracts.ContractVersion.InvalidSemVer` | Versão semântica inválida |
| 400 | `Contracts.ContractVersion.EmptySpecContent` | Conteúdo vazio |
| 400 | `Contracts.Lifecycle.InvalidTransition` | Transição de estado inválida |
| 400 | `Contracts.Signing.InvalidState` | Estado incompatível para assinatura |
| 400 | `Contracts.Signing.VerificationFailed` | Verificação de assinatura falhou |
| 400 | `Contracts.Protocol.Unsupported` | Protocolo não suportado para operação |

## Integração com CI/CD

### Exemplo: Pipeline de Import Automático

```bash
# 1. Importar spec do build
curl -X POST https://nextraceone.company.com/api/v1/contracts \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -H "Content-Type: application/json" \
  -d "{
    \"apiAssetId\": \"$API_ASSET_ID\",
    \"semVer\": \"$VERSION\",
    \"specContent\": $(cat openapi.json | jq -Rs .),
    \"format\": \"json\",
    \"importedFrom\": \"ci-pipeline\",
    \"protocol\": \"OpenApi\"
  }"

# 2. Computar diff com versão anterior
curl -X POST https://nextraceone.company.com/api/v1/contracts/diff \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Tenant-Id: $TENANT_ID" \
  -H "Content-Type: application/json" \
  -d "{
    \"baseVersionId\": \"$PREVIOUS_VERSION_ID\",
    \"targetVersionId\": \"$NEW_VERSION_ID\"
  }"
```

### Exemplo: Python

```python
import requests

base_url = "https://nextraceone.company.com/api/v1"
headers = {
    "Authorization": f"Bearer {token}",
    "X-Tenant-Id": tenant_id,
    "Content-Type": "application/json"
}

# Importar contrato
response = requests.post(f"{base_url}/contracts", json={
    "apiAssetId": api_asset_id,
    "semVer": "2.0.0",
    "specContent": open("openapi.json").read(),
    "format": "json",
    "importedFrom": "ci-pipeline",
    "protocol": "OpenApi"
}, headers=headers)

contract_id = response.json()["id"]

# Verificar integridade após assinatura
verify = requests.get(
    f"{base_url}/contracts/{contract_id}/verify",
    headers=headers
)
print(f"Signature valid: {verify.json()['isValid']}")
```

## Notas de Segurança

- Todas as APIs exigem autenticação
- Operações de escrita (lock, sign, deprecate) são auditadas
- Assinaturas usam SHA-256 determinístico via canonicalização
- Verificação de assinatura é timing-safe (previne ataques de timing)
- Conteúdo é limitado a 5 MB para prevenção de DoS
- Parsing é resiliente a specs malformadas (não lança exceções)
- Auto-detecção de protocolo disponível no import (Swagger, AsyncAPI, WSDL)

## Novos Endpoints (v2)

### Pesquisar Contratos

```http
GET /api/v1/contracts/search?protocol=OpenApi&lifecycleState=Draft&page=1&pageSize=20
```

Parâmetros query opcionais:
- `protocol` — OpenApi, Swagger, Wsdl, AsyncApi
- `lifecycleState` — Draft, InReview, Approved, Locked, Deprecated, Sunset, Retired
- `apiAssetId` — UUID do ativo de API
- `searchTerm` — busca textual na versão semântica
- `page` — número da página (padrão: 1)
- `pageSize` — tamanho da página (padrão: 20, máximo: 100)

**Resposta (200 OK):**
```json
{
  "items": [...],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20
}
```

### Listar Violações de Regras

```http
GET /api/v1/contracts/{contractVersionId}/violations
```

**Resposta (200 OK):**
```json
[
  {
    "id": "...",
    "ruleName": "operation-operationId",
    "severity": "Warning",
    "message": "Operation must have an operationId.",
    "path": "/pets/{petId}/GET"
  }
]
```

### Validar Integridade Estrutural

Valida se a especificação do contrato pode ser parseada corretamente conforme seu protocolo.
Retorna contagens de paths/canais, endpoints/operações e a versão extraída do spec.

```http
GET /api/v1/contracts/{contractVersionId}/validate
```

**Resposta (200 OK) — Contrato válido:**
```json
{
  "isValid": true,
  "pathCount": 5,
  "endpointCount": 12,
  "schemaVersion": "3.0.3",
  "validationError": null
}
```

**Resposta (200 OK) — Contrato com erro estrutural:**
```json
{
  "isValid": false,
  "pathCount": 0,
  "endpointCount": 0,
  "schemaVersion": null,
  "validationError": "The specification content could not be parsed."
}
```

### Sincronização em Lote (CI/CD — External Inbound)

Endpoint de sincronização em lote para integração sistema-a-sistema com CI/CD, API Gateways e outras plataformas.
Permite importar múltiplos contratos em uma única requisição. Idempotente por `apiAssetId + semVer`.
Itens com falha não bloqueiam os demais (fault isolation por item). Máximo de 50 itens por lote.

```http
POST /api/v1/contracts/sync
Authorization: X-Api-Key: <api_key>
X-Tenant-Id: <tenant_id>
Content-Type: application/json

{
  "items": [
    {
      "apiAssetId": "550e8400-e29b-41d4-a716-446655440000",
      "semVer": "2.1.0",
      "specContent": "{ \"openapi\": \"3.0.3\", ... }",
      "format": "json",
      "importedFrom": "github-actions/build-123",
      "protocol": "OpenApi"
    },
    {
      "apiAssetId": "661f9511-f30c-52e5-b827-557766551111",
      "semVer": "1.0.0",
      "specContent": "<definitions ...>...</definitions>",
      "format": "xml",
      "importedFrom": "jenkins/pipeline-456",
      "protocol": "Wsdl"
    }
  ],
  "sourceSystem": "github-actions",
  "correlationId": "build-run-20260313-001"
}
```

**Resposta (200 OK):**
```json
{
  "totalProcessed": 2,
  "created": 1,
  "skipped": 1,
  "failed": 0,
  "correlationId": "build-run-20260313-001",
  "processedAt": "2026-03-13T22:00:00Z",
  "items": [
    {
      "apiAssetId": "550e8400-e29b-41d4-a716-446655440000",
      "semVer": "2.1.0",
      "status": "Created",
      "contractVersionId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
      "errorMessage": null
    },
    {
      "apiAssetId": "661f9511-f30c-52e5-b827-557766551111",
      "semVer": "1.0.0",
      "status": "Skipped",
      "contractVersionId": "8d0f8801-g431-62fg-c66d-g29he3g12cg9",
      "errorMessage": null
    }
  ]
}
```

**Status por item:**
- `Created` — versão nova criada com sucesso
- `Skipped` — versão já existia para o ativo/semVer (idempotência)
- `Failed` — falha ao importar — detalhes em `errorMessage`

**Limites:**
- Máximo de 50 itens por lote
- Cada `specContent` limitado a 5 MB
- Formatos aceitos: `json`, `yaml`, `xml`
