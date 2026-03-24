# Relatório de Auditoria — Segurança do Frontend e Alinhamento com Backend

> **Módulo:** Frontend React + Backend ASP.NET Core  
> **Data da análise:** 2025-07  
> **Classificação:** GOOD_PRACTICE_APPARENT  
> **Autor:** Equipa de Governança de Segurança — NexTraceOne

---

## 1. Resumo Executivo

O frontend do NexTraceOne implementa práticas sólidas de segurança: armazenamento de refresh token em closure in-memory (seguro contra XSS), access token em sessionStorage (escopo por tab), auto-injecção de headers de segurança (Bearer, X-Tenant-Id, X-Environment-Id, X-Csrf-Token), refresh silencioso com gestão de pedidos concorrentes, e limpeza automática de tokens legacy. A navegação é protegida por permissões via `ProtectedRoute` com segurança de hydration. O código contém um comentário explícito declarando que "o frontend NUNCA deve fazer enforcement de autorização".

A classificação GOOD_PRACTICE_APPARENT reflecte uma abordagem madura de segurança no frontend, com decisões de design documentadas e trade-offs conhecidos.

---

## 2. Armazenamento de Tokens

### 2.1 Estratégia de Armazenamento

| Token | Localização | Justificação | Avaliação |
|---|---|---|---|
| **Refresh token** | Closure in-memory (variável JS) | Inacessível a XSS (não em storage) | ✅ SEGURO |
| **Access token** | sessionStorage | Escopo por tab, isolado de outras tabs | ⚠️ ACEITÁVEL |
| **Legacy tokens** | Limpeza automática no carregamento | Migração segura de localStorage | ✅ |

### 2.2 Análise de Ameaças

| Ameaça | Refresh Token (in-memory) | Access Token (sessionStorage) |
|---|---|---|
| XSS directo | ✅ PROTEGIDO (não acessível) | ⚠️ Acessível via document.cookie ou sessionStorage |
| XSS via extensão de browser | ✅ PROTEGIDO | ⚠️ Potencialmente acessível |
| Tab hijacking | ✅ PROTEGIDO | ⚠️ Acessível na mesma tab |
| Cross-tab attack | ✅ PROTEGIDO | ✅ PROTEGIDO (sessionStorage é per-tab) |
| Page refresh | ❌ PERDIDO (trade-off) | ✅ SOBREVIVE |

### 2.3 Trade-off Documentado

O refresh token in-memory é perdido no refresh da página, forçando re-autenticação. Esta é uma **decisão consciente de segurança**: a alternativa seria armazenar em localStorage (persistente mas vulnerável a XSS) ou em cookie HttpOnly (requer configuração server-side mais complexa).

**Mitigação:** O access token em sessionStorage (60 min) permite manter a sessão em refreshes de página. Quando o access token expira sem refresh token disponível, o utilizador é redireccionado para login.

### 2.4 Limpeza de Legacy

```javascript
// No carregamento da aplicação:
// Remove tokens legacy de localStorage
// Previne que tokens antigos permaneçam em storage menos seguro
```

**Avaliação:** ✅ Boa prática de migração — não deixa tokens em localizações inseguras.

---

## 3. Cliente API (Axios)

### 3.1 Interceptor de Request

O interceptor Axios injector automaticamente os seguintes headers:

| Header | Quando | Fonte |
|---|---|---|
| `Authorization: Bearer {token}` | Sempre (se autenticado) | Access token de sessionStorage |
| `X-Tenant-Id` | Sempre (se tenant seleccionado) | TenantContext |
| `X-Environment-Id` | Sempre (se ambiente seleccionado) | EnvironmentContext |
| `X-Csrf-Token` | POST, PUT, PATCH, DELETE | Token CSRF |

### 3.2 Injecção Selectiva de CSRF

```javascript
// X-Csrf-Token injectado APENAS em operações mutantes:
// POST, PUT, PATCH, DELETE
// GET e OPTIONS não incluem o header
```

**Avaliação:** ✅ Correcta — CSRF é relevante apenas para operações que alteram estado.

### 3.3 Refresh Silencioso em 401

| Aspecto | Implementação | Avaliação |
|---|---|---|
| Detecção de 401 | Interceptor de resposta | ✅ |
| Refresh automático | Tenta refresh antes de rejeitar | ✅ |
| Pedidos concorrentes | Fila de espera durante refresh | ✅ |
| Retry após refresh | Repete pedidos pendentes com novo token | ✅ |
| Falha de refresh | Redireciona para login | ✅ |
| Race condition | Apenas um refresh simultâneo | ✅ |

**Fluxo:**

```
Request → 401 → Refresh em curso?
  ├─ NÃO → Inicia refresh → Novo token → Retry pedidos
  └─ SIM → Enfileira pedido → Aguarda refresh → Retry
```

### 3.4 Análise de Segurança do Cliente API

| Aspecto | Estado | Avaliação |
|---|---|---|
| Tokens injectados automaticamente | ✅ | Reduz erro humano |
| CSRF em mutações | ✅ | Protecção adequada |
| Tenant/Environment em contexto | ✅ | Consistência |
| Refresh silencioso | ✅ | UX + segurança |
| Gestão de concorrência | ✅ | Previne race conditions |

---

## 4. Contextos de Segurança

### 4.1 AuthContext / AuthProvider

| Aspecto | Implementação |
|---|---|
| Bootstrap | GET `/identity/auth/me` no carregamento |
| Estado | User, permissions, tenant, isAuthenticated |
| Selecção de tenant | Fluxo de selecção se múltiplos tenants |
| Logout | Limpeza de estado + redirect |

### 4.2 EnvironmentProvider

| Aspecto | Implementação |
|---|---|
| Carregamento | GET `/identity/environments` |
| Auto-selecção | Ambiente default do tenant |
| Persistência | sessionStorage (sobrevive refresh) |
| Mudança de tenant | Recarrega ambientes |

### 4.3 Hierarquia de Providers

```jsx
<AuthProvider>        ← Autenticação + Permissões
  <TenantProvider>    ← Tenant seleccionado
    <EnvironmentProvider>  ← Ambiente seleccionado
      <App />
    </EnvironmentProvider>
  </TenantProvider>
</AuthProvider>
```

---

## 5. Permissões no Frontend

### 5.1 Catálogo de Permissões

| Aspecto | Detalhe | Avaliação |
|---|---|---|
| Ficheiro | `auth/permissions.ts` | ✅ Centralizado |
| Nº de chaves | 84+ strings distintas | ✅ |
| Formato | Idêntico ao backend | ✅ Alinhado |

### 5.2 Hook usePermissions

```typescript
const { hasPermission } = usePermissions();

// Internamente:
const permissions = new Set(userPermissions);  // O(1) lookup
const hasPermission = (code: string) => permissions.has(code);
```

**Avaliação:** ✅ Performance optimizada com Set para lookup O(1).

### 5.3 Carregamento Server-Driven

- Permissões carregadas do endpoint `/identity/auth/me`
- Nunca calculadas no frontend
- Actualizam com refresh da sessão

### 5.4 Comentário de Segurança Explícito

```typescript
// "O frontend NUNCA deve fazer enforcement de autorização.
//  O backend é a única fonte de verdade."
```

**Avaliação:** ✅ Excelente documentação de política de segurança. O frontend utiliza permissões apenas para UX (mostrar/esconder elementos), nunca para enforcement.

---

## 6. ProtectedRoute

### 6.1 Comportamento

| Cenário | Comportamento |
|---|---|
| Utilizador não autenticado | Redirect para login |
| Utilizador sem permissão | Redirect para "sem permissão" |
| Perfil em carregamento | Aguarda (não avalia prematuramente) |
| Permissão válida | Renderiza conteúdo |

### 6.2 Hydration Safety

```typescript
// ProtectedRoute aguarda carregamento do perfil antes de avaliar permissões
// Previne false negatives durante hydration/bootstrap
if (loading) return <LoadingSpinner />;
if (!hasPermission(requiredPermission)) return <Navigate to="/unauthorized" />;
return <Outlet />;
```

**Avaliação:** ✅ Previne redirect prematuro durante carregamento.

### 6.3 Limitações (por design)

| Limitação | Detalhe | Mitigação |
|---|---|---|
| Client-side redirect only | Não previne acesso directo via API | Backend enforcement |
| Baseado em permissões carregadas | Stale se permissões mudam durante sessão | Refresh periódico |
| Não valida ambiente | Apenas permissões globais | EnvironmentAccess no backend |

---

## 7. Alinhamento Frontend ↔ Backend

### 7.1 Matriz de Alinhamento

| Aspecto | Frontend | Backend | Alinhamento |
|---|---|---|---|
| Permissões | 84+ chaves | 73 códigos | ✅ Frontend superset |
| Formato | `módulo:recurso:acção` | `módulo:recurso:acção` | ✅ Idêntico |
| Enforcement | Apenas UX | Deny-by-default | ✅ Correcto |
| Tenant context | X-Tenant-Id header | TenantResolutionMiddleware | ✅ |
| Environment context | X-Environment-Id header | EnvironmentResolutionMiddleware | ✅ |
| CSRF | X-Csrf-Token em mutações | UseCookieSessionCsrfProtection | ✅ |
| Token refresh | Silent refresh em 401 | JWT 60-min + refresh token | ✅ |

### 7.2 Discrepância de Contagem (84+ vs 73)

| Causa Provável | Detalhe |
|---|---|
| Chaves de agrupamento UI | Frontend agrupa permissões para simplificar UI |
| Chaves futuras | Preparação para funcionalidades ainda não implementadas no backend |
| Chaves compostas | Combinações de permissões backend para UX |

**Recomendação:** Implementar teste automatizado que verifica alinhamento entre `auth/permissions.ts` e `RolePermissionCatalog`.

---

## 8. Análise de Vulnerabilidades

### 8.1 Ameaças e Mitigações

| Ameaça | Mitigação Frontend | Mitigação Backend | Risco Residual |
|---|---|---|---|
| **XSS roubo de refresh token** | ✅ In-memory closure | N/A | MÍNIMO |
| **XSS roubo de access token** | ⚠️ sessionStorage acessível | ✅ 60-min expiry | BAIXO |
| **CSRF** | ✅ X-Csrf-Token | ✅ Middleware | MÍNIMO |
| **Clickjacking** | N/A | ✅ X-Frame-Options | MÍNIMO |
| **Token replay** | ✅ Rotação no refresh | ✅ Hash SHA-256 | BAIXO |
| **Permission bypass** | ✅ (UX only, não enforcement) | ✅ Deny-by-default | MÍNIMO |
| **Tenant impersonation** | ✅ JWT claim prevalece | ✅ RLS | MÍNIMO |

### 8.2 Risco Residual: Access Token em sessionStorage

**Cenário:** Um ataque XSS consegue aceder sessionStorage e roubar o access token.

**Mitigações:**
1. Access token expira em 60 minutos (janela limitada)
2. Token contém tenant_id fixo (não permite cross-tenant)
3. Rate limiting limita uso abusivo
4. SecurityEvent tracking detecta anomalias

**Recomendação a longo prazo:** Considerar migração para cookie HttpOnly para o access token, eliminando completamente a exposição a XSS no armazenamento de tokens.

---

## 9. Recomendações

### Prioridade ALTA

1. **Teste automatizado de alinhamento** entre catálogo de permissões frontend e backend
2. **Content Security Policy** — verificar que CSP está configurado para prevenir XSS

### Prioridade MÉDIA

3. **Refresh periódico de permissões** — recarregar permissões do `/me` endpoint periodicamente (não apenas no login)
4. **Considerar cookie HttpOnly** para access token (elimina XSS no storage)
5. **Audit de dependências frontend** — verificar vulnerabilidades em pacotes npm

### Prioridade BAIXA

6. **Subresource Integrity** (SRI) para scripts de terceiros
7. **Documentar decisões de segurança** do frontend em architecture decision records (ADRs)
8. **Implementar Feature Flags** para controlo granular de funcionalidades por permissão

---

## 10. Conformidade

| Requisito | Estado | Evidência |
|---|---|---|
| Token storage seguro | ✅ | Refresh in-memory, access sessionStorage |
| CSRF protection | ✅ | X-Csrf-Token em mutações |
| No client-side enforcement | ✅ | Comentário explícito + implementação |
| Permissões server-driven | ✅ | Endpoint /me |
| Hydration-safe routing | ✅ | ProtectedRoute aguarda carregamento |
| Header injection automática | ✅ | Axios interceptor |
| Silent token refresh | ✅ | 401 handling com concurrency |
| Legacy token cleanup | ✅ | Limpeza no bootstrap |

---

> **Classificação final:** GOOD_PRACTICE_APPARENT — Frontend com práticas sólidas de segurança, decisões de trade-off documentadas, e alinhamento coerente com backend.
