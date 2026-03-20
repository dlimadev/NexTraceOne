# Fase 2 — Fundação de Contexto de Execução

**Data:** 2026-03-20  
**Status:** Completo  
**ADR relacionada:** ADR-002

---

## 1. Objetivo

Implementar a infraestrutura de resolução de contexto de execução operacional (TenantId + EnvironmentId + User) de forma segura, auditável e não destrutiva, conforme as decisões arquiteturais do ADR-002.

---

## 2. Estratégia Adotada para Resolução de Contexto

### 2.1 Pipeline de resolução (ordem)

```
Request HTTP
   │
   ▼
TenantResolutionMiddleware (já existia)
   ├── 1) JWT claim "tenant_id"
   ├── 2) Header X-Tenant-Id (fallback)
   └── 3) Subdomínio (fallback)
   │
   ▼ (tenant resolvido em CurrentTenantAccessor)
UseAuthentication (JWT/API Key)
   │
   ▼
EnvironmentResolutionMiddleware (NOVO — Fase 2)
   ├── 1) Header X-Environment-Id (Guid preferido)
   ├── 2) Query string ?environmentId=... (fallback)
   └── 3) Sem ambiente → IsResolved=false (aceitável para endpoints globais)
         VALIDAÇÃO CRÍTICA: ambiente DEVE pertencer ao tenant ativo
   │
   ▼ (ambiente resolvido em EnvironmentContextAccessor)
UseAuthorization
   ├── PermissionAuthorizationHandler (existia)
   ├── EnvironmentAccessAuthorizationHandler (NOVO)
   └── OperationalContextAuthorizationHandler (NOVO)
   │
   ▼
Handler / Endpoint
   └── IOperationalExecutionContext disponível via DI
```

### 2.2 Segurança do modelo

**O cliente indica preferência. O backend valida.**

Um cliente malicioso que passe um `X-Environment-Id` arbitrário:
1. O middleware chama `ITenantEnvironmentContextResolver.ResolveAsync(tenantId, environmentId)`
2. O resolver valida que `environment.TenantId == currentTenant.Id`
3. Se não pertencer, `EnvironmentContextAccessor.IsResolved` permanece `false`
4. O endpoint operacional protegido por `OperationalContextRequirement` retorna 403

**Isolamento garantido no middleware, não apenas na autorização.**

### 2.3 Consistência da estratégia

| Aspecto | Decisão |
|---|---|
| Tenant resolution | JWT claim → X-Tenant-Id header → subdomínio |
| Environment resolution | X-Environment-Id header → ?environmentId query |
| Validação de ownership | Sempre no backend (middleware e resolver) |
| Contexto parcial | Permitido — APIs globais não requerem ambiente |
| Contexto completo | Exigido por `OperationalContextRequirement` |
| Fonte de verdade | Backend sempre — frontend apenas indica preferência |

---

## 3. Contratos e Interfaces Criados

### 3.1 `IEnvironmentContextAccessor` (Phase 1 criou a interface)

**Implementação:** `EnvironmentContextAccessor` (IdentityAccess.Infrastructure.Context)

Scoped — limpo por request. Populado pelo `EnvironmentResolutionMiddleware`.

| Propriedade | Descrição |
|---|---|
| `EnvironmentId` | ID do ambiente resolvido |
| `Profile` | EnvironmentProfile do ambiente |
| `IsProductionLike` | True para Production/DisasterRecovery |
| `IsResolved` | True se middleware resolveu com sucesso |

### 3.2 `IOperationalExecutionContext` (NOVO)

**Implementação:** `OperationalExecutionContext` (IdentityAccess.Infrastructure.Context)

Agrega ICurrentTenant + ICurrentUser + EnvironmentContextAccessor em um único ponto de acesso para handlers operacionais.

| Propriedade | Origem |
|---|---|
| `UserId`, `UserName`, `UserEmail` | ICurrentUser |
| `TenantId`, `IsProductionLikeEnvironment` | ICurrentTenant + EnvironmentContextAccessor |
| `EnvironmentId`, `EnvironmentProfile` | EnvironmentContextAccessor |
| `TenantEnvironmentContext` | Construído a partir dos três |
| `IsFullyResolved` | Todos os três resolvidos |
| `HasTenantContext` | Apenas tenant resolvido |

### 3.3 `IEnvironmentAccessValidator` (NOVO)

Centraliza verificação de acesso de usuário ao ambiente.

```
ValidateAsync(userId, tenantId, environmentId, now) → Result<Unit>
  → NotFound se ambiente não existir
  → Forbidden se ambiente não pertencer ao tenant
  → Forbidden se ambiente inativo
  → Forbidden se sem EnvironmentAccess
  → Forbidden se acesso expirado
  → Ok(Unit.Value) se tudo válido

HasAccessAsync(userId, tenantId, environmentId, now) → bool
  → Verificação rápida para guards de autorização
```

### 3.4 `ITenantEnvironmentContextResolver` (Phase 1 criou a interface)

**Implementação:** `TenantEnvironmentContextResolver` (IdentityAccess.Infrastructure.Context)

Valida ownership e status antes de criar o context.

```
ResolveAsync(tenantId, environmentId) → TenantEnvironmentContext?
  → null se não encontrado
  → null se environment.TenantId != tenantId (isolamento)
  → null se environment.IsActive == false
  → TenantEnvironmentContext.From(environment) se válido

ListActiveContextsForTenantAsync(tenantId) → IReadOnlyList<TenantEnvironmentContext>
  → Todos os ambientes ativos do tenant
```

### 3.5 `IEnvironmentProfileResolver` (Phase 1 criou a interface)

**Implementação:** `EnvironmentProfileResolver` (IdentityAccess.Infrastructure.Context)

Resolve profile/IsProductionLike com validação de tenant.

---

## 4. Middleware

### `EnvironmentResolutionMiddleware`

**Localização:** `IdentityAccess.Infrastructure.Context`  
**Registrado em:** `Program.cs`, após `TenantResolutionMiddleware`

**Detalhes de resolução:**

```
Header preferido: X-Environment-Id: <Guid>
Query fallback:   ?environmentId=<Guid>
```

**Logs gerados:**
- `DEBUG` quando resolvido com sucesso (EnvironmentId, Profile, ProductionLike)
- `DEBUG` quando sem tenant ativo (skipped)
- `WARNING` quando ambiente não pertence ao tenant (tentativa potencialmente suspeita)

---

## 5. Runtime Context Endpoint

**`GET /api/v1/identity/context/runtime`**

Permite que o frontend consulte o contexto validado pelo backend.

**Response (`RuntimeContextDto`):**
```json
{
  "user": {
    "userId": "...",
    "userName": "...",
    "email": "...",
    "isAuthenticated": true
  },
  "tenant": {
    "tenantId": "...",
    "tenantSlug": "banco-xyz",
    "tenantName": "Banco XYZ",
    "isActive": true
  },
  "environment": {
    "environmentId": "...",
    "profile": "Production",
    "profileDisplayName": "Production",
    "isProductionLike": true,
    "badgeColor": "red",
    "showProtectionWarning": true,
    "allowDestructiveActions": false
  },
  "isFullyResolved": true,
  "resolvedAt": "2026-03-20T20:00:00Z"
}
```

O campo `environment` é `null` quando nenhum ambiente está resolvido (endpoint global).  
`badgeColor` e `showProtectionWarning` vêm do backend — o frontend não decide comportamento.

---

## 6. Limitações e Próximos Passos

- A implementação do `EnvironmentAccessValidator.ValidateAsync` verifica `EnvironmentAccess` no banco. Endpoints globais que não passam X-Environment-Id terão `IsResolved=false` — correto por design.
- `IOperationalExecutionContext.TenantEnvironmentContext` constrói a criticidade com uma inferência simplificada (Production/DR → Critical, demais → Low). A Fase 3 deve derivar isso do banco após a migration.
- Os handlers operacionais existentes ainda não usam `IOperationalExecutionContext`. A migração gradual ocorre na Fase 3.
- Nenhum middleware de ambiente foi adicionado ao AIKnowledge — os builders de AI usam `IEnvironmentContextAccessor` diretamente.
