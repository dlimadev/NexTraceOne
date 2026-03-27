# P1.5 — X-Tenant-Id Header Hardening Report

**Data de execução:** 2026-03-26  
**Classificação:** MEDIUM / P2 — Multi-tenancy surface restriction  
**Estado:** CONCLUÍDO

---

## 1. Contexto

O sistema resolvia o tenant por JWT claim (prioritário) e aceitava o header `X-Tenant-Id`
como fallback genérico quando o JWT não estava presente. Isto permitia que pedidos
não autenticados injetassem um contexto de tenant arbitrário, abrindo superfície de
ataque por:

- log pollution com tenant IDs arbitrários
- injeção de tenant context em futuros middlewares que possam confiar nesse estado
- violação do princípio de defense-in-depth

---

## 2. Ficheiros alterados

| Ficheiro | Tipo de alteração |
|---|---|
| `src/building-blocks/NexTraceOne.BuildingBlocks.Security/MultiTenancy/TenantResolutionMiddleware.cs` | **Alterado** — header só aceito para utilizadores autenticados |
| `src/building-blocks/NexTraceOne.BuildingBlocks.Application/Context/ContextPropagationHeaders.cs` | **Alterado** — documentação atualizada com nova política de segurança |
| `src/frontend/src/api/client.ts` | **Alterado** — comentário do interceptor atualizado com política de segurança |
| `tests/building-blocks/NexTraceOne.BuildingBlocks.Security.Tests/MultiTenancy/TenantResolutionMiddlewareTests.cs` | **Alterado** — teste existente atualizado + 2 novos testes |

---

## 3. Pontos onde X-Tenant-Id era lido/aceito

| Localização | Comportamento anterior |
|---|---|
| `TenantResolutionMiddleware.cs:41–47` | `else if (TryResolveFromHeader(...))` — sem verificação de autenticação. Qualquer pedido (autenticado ou não) com um `X-Tenant-Id` válido tinha o tenant context definido. |
| `src/frontend/src/api/client.ts:42–45` | Interceptor envia `X-Tenant-Id` em todos os pedidos com tenantId em sessionStorage (comportamento mantido — frontend correto, backend que protegia de forma insuficiente). |

---

## 4. Nova política aplicada

### Backend — `TenantResolutionMiddleware.cs`

**Antes:**
```csharp
else if (TryResolveFromHeader(context, out var headerTenantId))
{
    // sem verificação de autenticação
    currentTenant.Set(headerTenantId, ...);
}
```

**Depois:**
```csharp
else if (context.User.Identity?.IsAuthenticated == true && TryResolveFromHeader(context, out var headerTenantId))
{
    // header apenas aceito para utilizadores autenticados
    currentTenant.Set(headerTenantId, ...);
}
```

**Motivo da elegância da solução:** o middleware corre APÓS `UseAuthentication`
(linha 248 do `Program.cs`), pelo que `context.User.Identity?.IsAuthenticated` já
reflete o estado correto quando o tenant é resolvido.

### Prioridade de resolução atual

| Prioridade | Fonte | Condição |
|---|---|---|
| 1 | JWT claim `tenant_id` | Sempre — fonte de verdade |
| 2 | Header `X-Tenant-Id` | **Apenas quando autenticado** e JWT não tem claim tenant_id |
| 3 | Subdomínio | Fallback determinístico via SHA-256 |

---

## 5. Casos em que o header continua permitido

O header `X-Tenant-Id` continua aceito apenas quando:

1. O utilizador está autenticado (`context.User.Identity?.IsAuthenticated == true`)
2. O JWT autenticado não contém o claim `tenant_id` (caso de transição controlada)

**Na prática**, este cenário é de uso extremamente reduzido porque:
- Todo JWT gerado pelo `JwtTokenGenerator` do NexTraceOne inclui `tenant_id`
- Após `LocalLogin`, `FederatedLogin`, ou `OidcCallback`, o JWT contém sempre o tenant
- O header como fallback ativo é um edge-case de compatibilidade

---

## 6. Endpoints pré-auth analisados

Os seguintes endpoints pré-auth foram verificados e confirmados como
**independentes de X-Tenant-Id**:

| Endpoint / Feature | Marcação | Depende de X-Tenant-Id? |
|---|---|---|
| `LocalLogin` | `IPublicRequest` | ❌ Não — bypass de TenantIsolationBehavior |
| `FederatedLogin` | `IPublicRequest` | ❌ Não |
| `OidcCallback` | `IPublicRequest` | ❌ Não |
| `RefreshToken` | `IPublicRequest` | ❌ Não |
| `StartOidcLogin` | `IPublicRequest` | ❌ Não |
| `SelectTenant` | Authenticated (não IPublicRequest) | ❌ Não — TenantId passado como parâmetro do command |

**Conclusão:** não existe nenhum fluxo pré-auth legítimo que necessite de
`X-Tenant-Id` header sem JWT autenticado.

---

## 7. Alterações no frontend

O comportamento do `client.ts` foi mantido (header enviado quando tenantId está em
sessionStorage). Isto é correto porque:

- Para pedidos autenticados (JWT ou cookie session): tenantId está em sessionStorage
  após login → header enviado → backend aceita (utilizador autenticado)
- Para pedidos pré-login (login, refresh): tenantId não está em sessionStorage ainda
  → header não enviado
- Para sessões expiradas: `clearAllTokens()` remove tenantId de sessionStorage no
  logout/session-expired → header não enviado

A documentação do interceptor foi atualizada para explicar a nova política do backend.

---

## 8. Testes atualizados e adicionados

| Teste | Mudança |
|---|---|
| `InvokeAsync_WithTenantIdHeader_ResolvesFromHeader` | **Removido** — substituído por dois testes com nomes mais claros |
| `InvokeAsync_WithTenantIdHeader_WithoutAuthentication_DoesNotResolve` | **Novo** — valida que header é ignorado sem autenticação |
| `InvokeAsync_WithTenantIdHeader_WhenAuthenticated_ResolvesFromHeader` | **Novo** — valida que header é aceito como fallback com autenticação |
| `InvokeAsync_WithEmptyGuidHeader_DoesNotResolve` | **Atualizado** — comentário corrigido (antes explicava Guid.Empty TryParse; agora reflete que o motivo é ausência de autenticação) |

---

## 9. Resultado dos testes

- **102 testes passam** (0 falhas) no projeto `NexTraceOne.BuildingBlocks.Security.Tests`
- 2 novos testes adicionados (100 → 102)
- 0 testes quebrados

---

## 10. Validação funcional

| Cenário | Comportamento após fix |
|---|---|
| Pedido sem autenticação + X-Tenant-Id qualquer | Tenant: `Guid.Empty` (header ignorado) |
| Pedido sem autenticação + sem header | Tenant: `Guid.Empty` |
| Pedido autenticado com JWT tendo `tenant_id` | Tenant: do JWT (prioridade máxima) |
| Pedido autenticado sem JWT `tenant_id` + com header | Tenant: do header (fallback aceito) |
| Pedido autenticado + subdomain | Tenant: do subdomain (fallback final) |
