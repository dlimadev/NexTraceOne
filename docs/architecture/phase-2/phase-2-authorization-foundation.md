# Fase 2 — Fundação de Autorização Contextual

**Data:** 2026-03-20  
**Status:** Completo  
**Relacionado com:** ADR-002, phase-2-execution-context-foundation.md

---

## 1. Modelo de Autorização Contextual

O NexTraceOne usa autorização em duas camadas:

### Camada 1 — Autorização baseada em permissão (existente)

`PermissionRequirement` + `PermissionAuthorizationHandler` + `PermissionPolicyProvider`

```
.RequirePermission("identity:users:write")
→ PermissionPolicyProvider cria policy dinâmica
→ PermissionAuthorizationHandler verifica claim "permissions" no JWT
```

Responsabilidade: **o que o usuário pode fazer no sistema** (RBAC granular).

### Camada 2 — Autorização baseada em contexto (NOVA — Fase 2)

`EnvironmentAccessRequirement` + `OperationalContextRequirement`

```
.RequireAuthorization("env:access") ou .RequireAuthorization("ctx:operational")
→ Handler verifica se tenant + ambiente + usuário estão todos resolvidos e válidos
```

Responsabilidade: **em qual contexto o usuário pode operar** (scoping contextual).

---

## 2. Responsabilidades por Componente

| Componente | Responsabilidade |
|---|---|
| `TenantIsolationBehavior` | Rejeita requests sem tenant ativo no pipeline MediatR |
| `EnvironmentResolutionMiddleware` | Resolve e valida ambiente; isola cross-tenant |
| `EnvironmentAccessRequirement` | Exige que usuário tenha acesso ao ambiente resolvido |
| `OperationalContextRequirement` | Exige contexto completo: tenant + ambiente + usuário |
| `EnvironmentAccessAuthorizationHandler` | Avalia EnvironmentAccessRequirement via `IEnvironmentAccessValidator` |
| `OperationalContextAuthorizationHandler` | Avalia OperationalContextRequirement verificando `IsResolved` de cada componente |

---

## 3. Policies/Requirements/Handlers Criados

### `EnvironmentAccessRequirement`

**Localização:** `BuildingBlocks.Security.Authorization`  
**Uso:** Endpoints que operam em dados de um ambiente específico.

```csharp
// Uso em endpoint
group.MapGet("/traces", ...)
    .RequireAuthorization(policy => policy.AddRequirements(EnvironmentAccessRequirement.Instance));
```

**Avaliação pelo handler:**
1. Usuário autenticado?
2. Ambiente resolvido? (IsResolved=true)
3. Tenant ativo?
4. Usuário tem EnvironmentAccess ativo no banco?

Qualquer falha → deny-by-default.

### `OperationalContextRequirement`

**Localização:** `BuildingBlocks.Security.Authorization`  
**Uso:** Endpoints operacionais que processam dados de observabilidade, incidentes, IA.

```csharp
// Uso em endpoint
group.MapPost("/ai/analyze", ...)
    .RequireAuthorization(policy => policy.AddRequirements(OperationalContextRequirement.Instance));
```

**Avaliação pelo handler:**
1. Usuário autenticado?
2. Tenant ativo e não-vazio?
3. Ambiente resolvido?

Mais leve que `EnvironmentAccessRequirement` — não faz query no banco.  
Ideal para pre-checks rápidos de contexto.

### `EnvironmentAccessAuthorizationHandler`

**Localização:** `IdentityAccess.Infrastructure.Authorization`  
Registrado como `IAuthorizationHandler` scoped via DI.  
Requer `IEnvironmentAccessValidator` — faz query no banco.

### `OperationalContextAuthorizationHandler`

**Localização:** `IdentityAccess.Infrastructure.Authorization`  
Registrado como `IAuthorizationHandler` scoped via DI.  
Sem acesso ao banco — apenas verifica estado dos accessors.

---

## 4. Separação entre Autorização Global e Operacional

### Capacidades globais (não requerem EnvironmentId)

Exemplos: listar tenants do usuário, gerenciar usuários, acessar catálogo de serviços.

Proteção atual: `[Authorize]` + permissão via `RequirePermission(...)` + `TenantIsolationBehavior`.

### Capacidades operacionais (requerem TenantId + EnvironmentId)

Exemplos: observabilidade, incidentes, análise de IA, comparison de ambientes.

Proteção futura: `RequirePermission("...") + OperationalContextRequirement`.

---

## 5. O que ficou para as próximas fases

### Fase 3
- [ ] Aplicar `OperationalContextRequirement` nos endpoints de observabilidade e incidentes
- [ ] Aplicar `EnvironmentAccessRequirement` nos endpoints de IA
- [ ] Migrar handlers existentes para usar `IOperationalExecutionContext`
- [ ] Criar policies nomeadas helpers (ex.: `.RequireOperationalContext()` extension)

### Fase 4+
- [ ] Autorização baseada em `EnvironmentProfile` (ex.: bloquear ações destrutivas em produção)
- [ ] Autorização para IA com escopo de dados (quais fontes pode consultar)
- [ ] Rate limiting por ambiente (produção → limits mais baixos)
- [ ] Auditoria de acessos negados (`EnvironmentAccessAuthorizationHandler` já loga)
