# Contract Conformance — Novos Endpoints de API

> Parte do plano: [01-OVERVIEW.md](01-OVERVIEW.md)

---

## 1. Endpoint principal — Validate Implementation

### `POST /contracts/validate-implementation`

**Propósito:** Gate de conformance CI/CD. Recebe a spec implementada e valida contra o contrato desenhado no NexTraceOne.

**Permissão:** `contracts:validate` (nova permissão, escopo: serviço + ambiente)

**Autenticação aceite:** JWT de utilizador com `contracts:read` | CI Token (`ctr_*` prefixed)

---

#### Request body

```json
{
  "resolution": {
    "serviceSlug": "payment-api",
    "environmentName": "pre-production",
    "contractVersionId": null,
    "apiAssetId": null
  },
  "implementedSpecContent": "<conteúdo OpenAPI/AsyncAPI/WSDL em string>",
  "implementedSpecFormat": "openapi-yaml",
  "sourceSystem": "github-actions",
  "pipelineRunId": "12345678",
  "commitSha": "abc123def456",
  "branchName": "feature/payment-v2",
  "linkedReleaseId": null
}
```

**Campos de `resolution` (pelo menos um obrigatório):**

| Campo | Tipo | Quando usar |
|-------|------|-------------|
| `serviceSlug` + `environmentName` | string | CI com `.nextraceone.yaml` |
| `apiAssetId` | guid | Quando o repo tem o asset ID no config |
| `contractVersionId` | guid | Acesso directo a versão específica |

**Nota:** Se o request vier com CI Token no header, `serviceSlug` é resolvido automaticamente a partir do binding do token — pode ser omitido.

---

#### Response — sucesso (200)

```json
{
  "conformanceCheckId": "uuid",
  "contractVersionId": "uuid",
  "contractVersion": "2.1.0",
  "serviceSlug": "payment-api",
  "environmentName": "pre-production",
  "status": "Compliant",
  "recommendation": "Approve",
  "conformanceScore": 97.5,
  "deviationCount": 0,
  "breakingDeviationCount": 0,
  "driftDeviationCount": 0,
  "policyEnforced": false,
  "policyDecisionReason": null,
  "deviations": [],
  "summary": "Implementation matches the designed contract. No deviations detected.",
  "checkedAt": "2026-04-10T14:30:00Z"
}
```

#### Response — drift detectado (200, recomendação Warn)

```json
{
  "conformanceCheckId": "uuid",
  "status": "Drifted",
  "recommendation": "Warn",
  "conformanceScore": 84.0,
  "deviationCount": 2,
  "breakingDeviationCount": 0,
  "driftDeviationCount": 2,
  "policyEnforced": false,
  "deviations": [
    {
      "type": "AdditionalEndpoint",
      "severity": "Drift",
      "path": "POST /payments/retry",
      "description": "Endpoint exists in implementation but not in designed contract.",
      "isBreaking": false,
      "suggestedAction": "Add endpoint to contract design or configure ignored_paths."
    },
    {
      "type": "AdditionalResponseField",
      "severity": "Drift",
      "path": "GET /payments/{id} → response.body.internalRef",
      "description": "Extra field in response body not present in designed contract.",
      "isBreaking": false,
      "suggestedAction": "Add field to contract schema or mark as extension."
    }
  ],
  "summary": "2 drift deviations found. Pipeline can proceed per current policy.",
  "checkedAt": "2026-04-10T14:30:00Z"
}
```

#### Response — breaking change bloqueante (422)

```json
{
  "conformanceCheckId": "uuid",
  "status": "Breaking",
  "recommendation": "Block",
  "conformanceScore": 52.0,
  "deviationCount": 3,
  "breakingDeviationCount": 2,
  "policyEnforced": true,
  "policyDecisionReason": "Policy 'breaking_only' blocks pipeline on breaking deviations.",
  "deviations": [
    {
      "type": "RemovedEndpoint",
      "severity": "Breaking",
      "path": "DELETE /payments/{id}",
      "description": "Endpoint present in designed contract is missing from implementation.",
      "isBreaking": true,
      "suggestedAction": "Implement the endpoint or create a new contract version removing it."
    },
    {
      "type": "RequiredParameterRemoved",
      "severity": "Breaking",
      "path": "POST /payments → body.currency",
      "description": "Required field 'currency' in designed contract is not present in implementation.",
      "isBreaking": true,
      "suggestedAction": "Implement the required field or submit a breaking change to the contract."
    }
  ],
  "summary": "2 breaking deviations found. Pipeline blocked per policy.",
  "checkedAt": "2026-04-10T14:30:00Z"
}
```

#### Response — contrato não encontrado (404)

```json
{
  "type": "https://nextraceone.io/errors/contract-not-found",
  "title": "Active contract not found",
  "status": 404,
  "messageKey": "contracts.conformance.no_active_contract",
  "detail": "No locked or approved contract version found for service 'payment-api' in environment 'pre-production'.",
  "correlationId": "uuid"
}
```

---

## 2. Conformance History por Contrato

### `GET /contracts/{contractVersionId}/conformance-history`

**Permissão:** `contracts:read`

**Query params:**
- `environmentId` (opcional) — filtrar por ambiente
- `status` (opcional) — Compliant | Drifted | Breaking | Error
- `sourceSystem` (opcional) — github-actions | jenkins | ...
- `pageNumber` / `pageSize`

**Response:**
```json
{
  "items": [
    {
      "id": "uuid",
      "status": "Compliant",
      "conformanceScore": 97.5,
      "sourceSystem": "github-actions",
      "pipelineRunId": "12345",
      "commitSha": "abc123",
      "branchName": "main",
      "environmentName": "pre-production",
      "checkedAt": "2026-04-10T14:30:00Z",
      "deviationCount": 0,
      "breakingDeviationCount": 0
    }
  ],
  "totalCount": 42,
  "pageNumber": 1,
  "pageSize": 20
}
```

---

## 3. Status de Conformance por Serviço

### `GET /services/{serviceId}/conformance-status`

**Propósito:** Visão rápida do estado de conformance de um serviço por ambiente. Útil para dashboards de equipa.

**Permissão:** `contracts:read`

**Response:**
```json
{
  "serviceId": "uuid",
  "serviceSlug": "payment-api",
  "environments": [
    {
      "environmentName": "development",
      "environmentId": "uuid",
      "lastCheckAt": "2026-04-10T10:00:00Z",
      "lastStatus": "Compliant",
      "conformanceScore": 98.0,
      "contractVersion": "2.1.0",
      "isStale": false
    },
    {
      "environmentName": "pre-production",
      "environmentId": "uuid",
      "lastCheckAt": "2026-04-09T16:00:00Z",
      "lastStatus": "Drifted",
      "conformanceScore": 84.0,
      "contractVersion": "2.1.0",
      "isStale": false
    },
    {
      "environmentName": "production",
      "environmentId": "uuid",
      "lastCheckAt": null,
      "lastStatus": null,
      "conformanceScore": null,
      "contractVersion": "2.0.1",
      "isStale": true
    }
  ]
}
```

---

## 4. CI Token Management

### `POST /contracts/ci-tokens`

**Permissão:** `contracts:write` (owner da equipa do serviço)

**Request:**
```json
{
  "serviceId": "uuid",
  "name": "payment-api-github-ci",
  "allowedEnvironments": ["development", "pre-production"],
  "expiresAt": "2027-04-10T00:00:00Z"
}
```

**Response (201) — token raw retornado uma única vez:**
```json
{
  "tokenId": "uuid",
  "name": "payment-api-github-ci",
  "keyPrefix": "ctr_ci_p",
  "rawToken": "ctr_ci_pXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
  "serviceId": "uuid",
  "serviceSlug": "payment-api",
  "allowedEnvironments": ["development", "pre-production"],
  "expiresAt": "2027-04-10T00:00:00Z",
  "warning": "Store this token securely. It will not be shown again."
}
```

---

### `GET /contracts/ci-tokens`

**Permissão:** `contracts:read`

**Query params:** `serviceId` (opcional), `isActive` (bool)

**Response:** Lista de tokens sem `rawToken` (apenas prefixo e metadados)

---

### `DELETE /contracts/ci-tokens/{id}`

**Permissão:** `contracts:write`

**Request body:**
```json
{ "reason": "Token comprometido — rotação preventiva" }
```

**Response:** 204 No Content

---

## 5. Changelog de Contrato

### `GET /contracts/{apiAssetId}/changelog`

**Permissão:** `contracts:read`

**Query params:**
- `eventTypes` (opcional, array) — filtrar por tipos de evento
- `isBreaking` (bool, opcional) — apenas breaking changes
- `from` / `to` (datetime) — janela temporal
- `pageNumber` / `pageSize`

**Response:**
```json
{
  "apiAssetId": "uuid",
  "contractName": "Payment API",
  "items": [
    {
      "id": "uuid",
      "eventType": "ConformanceCheckFailed",
      "changeLevel": "Breaking",
      "title": "CI conformance check failed — 2 breaking deviations",
      "description": "Pipeline 'github-actions #1234' detected breaking deviations on branch 'feature/v2'.",
      "isBreaking": true,
      "requiresConsumerAction": false,
      "triggeredBySystem": "github-actions",
      "linkedConformanceCheckId": "uuid",
      "happenedAt": "2026-04-10T14:30:00Z"
    },
    {
      "id": "uuid",
      "eventType": "ContractPublished",
      "changeLevel": "NonBreaking",
      "title": "Contract version 2.1.0 published",
      "description": "Added optional field 'metadata' to POST /payments response.",
      "isBreaking": false,
      "requiresConsumerAction": false,
      "triggeredBySystem": "nextraceone-ui",
      "happenedAt": "2026-04-08T09:15:00Z"
    }
  ],
  "totalCount": 28,
  "pageNumber": 1,
  "pageSize": 20
}
```

---

### `GET /contracts/changelog/feed`

**Propósito:** Feed global de eventos de contratos para dashboards de equipa e governança.

**Permissão:** `contracts:read`

**Query params:**
- `teamId` (opcional)
- `isBreaking` (bool)
- `eventTypes` (array)
- `from` / `to`
- `pageNumber` / `pageSize`

---

## 6. Resumo de permissões novas

| Permissão | Acção |
|-----------|-------|
| `contracts:validate` | Executar conformance check (CI Token ou utilizador) |
| `contracts:ci-tokens:write` | Criar e revogar CI tokens |
| `contracts:ci-tokens:read` | Listar CI tokens do serviço |
| `contracts:changelog:read` | Ler changelog de contratos |

As permissões existentes (`contracts:read`, `contracts:write`) mantêm-se inalteradas.
