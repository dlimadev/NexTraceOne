# Identity & Access — API de Integração Externa

## Visão Geral

O módulo Identity & Access do NexTraceOne expõe uma API REST completa para
autenticação, autorização, gestão de utilizadores, multi-tenancy e funcionalidades
enterprise (BreakGlass, JIT Access, Delegation, Access Review).

Todas as APIs seguem o padrão REST sob `/api/v1/identity/` e utilizam
JWT Bearer ou API Key para autenticação sistema-a-sistema.

## Autenticação e Autorização

### Mecanismos Suportados

| Mecanismo | Header | Descrição |
|-----------|--------|-----------|
| JWT Bearer | `Authorization: Bearer <token>` | Autenticação padrão via token JWT |
| API Key | `X-Api-Key: <key>` | Autenticação sistema-a-sistema (sem necessidade de login) |
| OIDC | Redirect flow via `/auth/oidc/start` | Autenticação federada com providers externos |

### Obter Token JWT

```bash
curl -X POST http://localhost:5000/api/v1/identity/auth/login \
  -H "Content-Type: application/json" \
  -d '{ "email": "admin@example.com", "password": "P@ssw0rd123" }'
```

Response:
```json
{
  "accessToken": "eyJhbGciOi...",
  "refreshToken": "rt-...",
  "expiresIn": 3600,
  "tenantId": "TENANT_UUID"
}
```

### API Key (Sistema-a-Sistema)

Para integrações automatizadas, configure API Keys em `Security:ApiKeys`:
```json
{
  "Security": {
    "ApiKeys": [
      {
        "ClientId": "external-system",
        "ClientName": "External Integration",
        "Key": "<api-key-value>",
        "TenantId": "TENANT_UUID",
        "Permissions": ["identity:users:read", "identity:sessions:read"]
      }
    ]
  }
}
```

Enviar no header: `X-Api-Key: <api-key-value>`

### Tenant Scoping

Todas as operações são automaticamente escopadas ao tenant do token/API key.
Dados de um tenant não vazam para outro.

## Endpoints

### Autenticação (`/auth`)

| Método | Rota | Autenticação | Descrição |
|--------|------|-------------|-----------|
| POST | `/auth/login` | Anónimo | Login local (email + password) |
| POST | `/auth/federated` | Anónimo | Login federado (provider externo) |
| POST | `/auth/refresh` | Anónimo | Renovar token JWT |
| POST | `/auth/logout` | JWT/ApiKey | Invalidar sessão corrente |
| POST | `/auth/revoke` | JWT/ApiKey | Revogar sessão específica |
| GET | `/auth/me` | JWT/ApiKey | Obter perfil do utilizador autenticado |
| PUT | `/auth/password` | JWT/ApiKey | Alterar password (self-service) |
| POST | `/auth/oidc/start` | Anónimo | Iniciar fluxo OIDC redirect |
| GET | `/auth/oidc/callback` | Anónimo | Callback do provider OIDC |

### Gestão de Utilizadores (`/users`)

| Método | Rota | Permissão | Descrição |
|--------|------|-----------|-----------|
| POST | `/users` | `identity:users:write` | Criar utilizador |
| GET | `/users/{id}` | `identity:users:read` | Obter perfil por ID |
| GET | `/tenants/{tenantId}/users` | `identity:users:read` | Listar utilizadores do tenant (paginado) |
| POST | `/users/{userId}/roles` | `identity:roles:assign` | Atribuir role a utilizador |
| PUT | `/users/{userId}/deactivate` | `identity:users:write` | Desactivar utilizador |
| PUT | `/users/{userId}/activate` | `identity:users:write` | Reactivar utilizador |
| GET | `/users/{userId}/sessions` | `identity:sessions:read` | Listar sessões activas |

### Roles e Permissões

| Método | Rota | Permissão | Descrição |
|--------|------|-----------|-----------|
| GET | `/roles` | `identity:roles:read` | Listar roles disponíveis |
| GET | `/permissions` | `identity:permissions:read` | Listar permissões |

### Tenants

| Método | Rota | Autenticação | Descrição |
|--------|------|-------------|-----------|
| GET | `/tenants/mine` | JWT/ApiKey | Listar tenants do utilizador |
| POST | `/auth/select-tenant` | JWT/ApiKey | Selecionar tenant activo |

### Environments

| Método | Rota | Permissão | Descrição |
|--------|------|-----------|-----------|
| GET | `/environments` | `identity:users:read` | Listar environments do tenant |
| POST | `/environments/access` | `identity:users:write` | Conceder acesso a environment |

### Break Glass (Acesso Emergencial)

| Método | Rota | Permissão | Descrição |
|--------|------|-----------|-----------|
| POST | `/break-glass` | JWT/ApiKey | Solicitar acesso emergencial |
| POST | `/break-glass/{requestId}/revoke` | `identity:sessions:revoke` | Revogar pedido |
| GET | `/break-glass` | `identity:sessions:read` | Listar pedidos (auditoria) |

### JIT Access (Acesso Just-In-Time)

| Método | Rota | Permissão | Descrição |
|--------|------|-----------|-----------|
| POST | `/jit-access` | JWT/ApiKey | Solicitar acesso temporário privilegiado |
| POST | `/jit-access/{requestId}/decide` | `identity:sessions:revoke` | Aprovar/rejeitar pedido |
| GET | `/jit-access/pending` | `identity:sessions:read` | Listar pedidos pendentes |

### Delegation (Delegação Formal)

| Método | Rota | Permissão | Descrição |
|--------|------|-----------|-----------|
| POST | `/delegations` | JWT/ApiKey | Criar delegação formal de permissões |
| POST | `/delegations/{delegationId}/revoke` | `identity:sessions:revoke` | Revogar delegação |
| GET | `/delegations` | `identity:users:read` | Listar delegações activas |

### Access Review (Recertificação)

| Método | Rota | Permissão | Descrição |
|--------|------|-----------|-----------|
| POST | `/access-reviews` | `identity:users:write` | Iniciar campanha de recertificação |
| GET | `/access-reviews` | `identity:users:read` | Listar campanhas |
| GET | `/access-reviews/{campaignId}` | `identity:users:read` | Detalhe da campanha com itens |
| POST | `/access-reviews/{campaignId}/items/{itemId}/decide` | `identity:users:write` | Registar decisão sobre item |

## Contratos Públicos (DTOs)

### TenantMembershipDto
```json
{
  "tenantId": "UUID",
  "roleId": "UUID",
  "roleName": "Admin",
  "isActive": true
}
```

### UserSummaryDto
```json
{
  "userId": "UUID",
  "email": "user@example.com",
  "fullName": "João Silva",
  "isActive": true
}
```

## Integration Events (Outbox Pattern)

| Evento | Campos | Descrição |
|--------|--------|-----------|
| `UserCreatedIntegrationEvent` | `UserId`, `Email`, `TenantId` | Publicado quando um utilizador é criado |
| `UserRoleChangedIntegrationEvent` | `UserId`, `TenantId`, `RoleName` | Publicado quando uma role é alterada |

Estes eventos são publicados via Outbox Pattern e podem ser consumidos por
outros módulos (ex: Audit, Licensing).

## Interface Cross-Module

```csharp
public interface IIdentityModule
{
    Task<UserSummaryDto?> GetUserSummaryAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<TenantMembershipDto>> GetUserMembershipsAsync(Guid userId, CancellationToken ct);
}
```

## Códigos de Erro (i18n)

| Código | Descrição |
|--------|-----------|
| `Identity.User.NotFound` | Utilizador não encontrado |
| `Identity.User.Inactive` | Utilizador desactivado |
| `Identity.User.InvalidCredentials` | Credenciais inválidas |
| `Identity.Session.NotFound` | Sessão não encontrada |
| `Identity.Session.Expired` | Sessão expirada |
| `Identity.Tenant.NotFound` | Tenant não encontrado |
| `Identity.Role.NotFound` | Role não encontrada |
| `Identity.BreakGlass.NotFound` | Pedido de break glass não encontrado |
| `Identity.JitAccess.NotFound` | Pedido de JIT access não encontrado |
| `Identity.Delegation.NotFound` | Delegação não encontrada |
| `Identity.AccessReview.NotFound` | Campanha não encontrada |

## Códigos HTTP

| Código | Descrição |
|--------|-----------|
| 200 | Operação bem sucedida |
| 201 | Recurso criado (com header Location) |
| 400 | Payload inválido (validação FluentValidation) |
| 401 | Não autenticado |
| 403 | Sem permissão |
| 404 | Recurso não encontrado |
| 409 | Conflito (ex: email duplicado) |
| 429 | Rate limit excedido (100 req/min por IP) |

## Exemplos de Integração

### Login e Obter Perfil (curl)

```bash
# 1. Autenticar
TOKEN=$(curl -s -X POST http://localhost:5000/api/v1/identity/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "admin@example.com", "password": "P@ssw0rd123"}' \
  | jq -r '.accessToken')

# 2. Obter perfil
curl -s http://localhost:5000/api/v1/identity/auth/me \
  -H "Authorization: Bearer $TOKEN" | jq .

# 3. Listar utilizadores do tenant
curl -s "http://localhost:5000/api/v1/identity/tenants/TENANT_UUID/users?page=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN" | jq .
```

### Integração com API Key (Python)

```python
import requests

BASE_URL = "http://localhost:5000/api/v1/identity"
HEADERS = {
    "Content-Type": "application/json",
    "X-Api-Key": "<your-api-key>",
}

# Listar utilizadores
response = requests.get(
    f"{BASE_URL}/tenants/{tenant_id}/users",
    headers=HEADERS,
    params={"page": 1, "pageSize": 50},
)
users = response.json()
print(f"Total users: {users['totalCount']}")
```

## Auditabilidade

Todas as operações do módulo Identity:
- Passam pelo pipeline de auditoria (AuditInterceptor: CreatedAt/By, UpdatedAt/By)
- Geram SecurityEvents para acções críticas (login, break glass, delegation, etc.)
- São rastreáveis pelo correlationId do request
- Respeitam isolamento de tenant via RLS (Row-Level Security)
- Integram com o módulo Audit via ISecurityAuditBridge

## Observabilidade

- Logs em inglês com structured logging (Serilog)
- Tracing OpenTelemetry para cada operação
- SecurityEvents com risk scoring (40+ tipos de evento)
- Métricas de logins bem/mal sucedidos por tenant
