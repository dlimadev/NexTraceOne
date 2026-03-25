# PARTE 3 — Validação dos Fluxos Ponta a Ponta de Autenticação e Acesso

> Documento gerado em 2026-03-25 | Prompt N14 | Consolidação do módulo Identity & Access

---

## 1. Fluxo real de autenticação (Login Local)

```
[Utilizador] → POST /api/v1/identity/auth/login
    ↓
[AuthEndpoints.cs] → AllowAnonymous
    ↓
[LocalLogin.Handler] (155 LOC)
    ├── Resolve user by email → IUserRepository.GetByEmailAsync()
    ├── Verify password → IPasswordHasher.Verify()
    ├── Check lockout → User.LockoutEnd > now?
    ├── Check IsActive
    ├── Record failed attempt or reset counter
    ├── Create session → LoginSessionCreator.CreateAsync()
    │       ├── Session.Create(userId, tenantId, ip, userAgent, expiresAt)
    │       ├── ISessionRepository.AddAsync()
    │       └── UnitOfWork.SaveChanges()
    ├── Generate JWT → IJwtTokenGenerator.GenerateAccessToken()
    ├── Generate RefreshToken → IJwtTokenGenerator.GenerateRefreshToken()
    ├── Record security event → SecurityAuditRecorder.RecordAuthenticationSuccess()
    └── Return { accessToken, refreshToken, expiresIn, user }
```

| Etapa | Backend real? | Persistência? | Auditoria? |
|---|---|---|---|
| Recepção do pedido | ✅ | — | — |
| Validação de credenciais | ✅ | ✅ User lookup | — |
| Verificação de lockout | ✅ | ✅ User.LockoutEnd | — |
| Criação de sessão | ✅ | ✅ Session entity | — |
| Geração de token JWT | ✅ | — | — |
| Registo de evento de segurança | ✅ | ✅ SecurityEvent | ✅ |
| **MFA enforcement** | ❌ **NÃO** | — | — |

### ⚠️ Lacuna crítica: MFA não é enforced

O `MfaPolicy` value object está modelado com `RequiredOnLogin = true` para SaaS, mas o fluxo de `LocalLogin.Handler` **não verifica nem bloqueia** caso MFA seja obrigatório. O `MfaPage.tsx` existe no frontend mas não há backend handler que valide o código MFA.

---

## 2. Fluxo real de autenticação (OIDC)

```
[Utilizador] → POST /api/v1/identity/auth/oidc/start
    ↓
[StartOidcLogin.Handler] (149 LOC)
    ├── Resolve OIDC provider configuration
    ├── Generate state + nonce
    ├── Build authorization URL
    └── Return { redirectUrl, state }

[Browser redirect] → External IdP → callback

[Utilizador] → GET /api/v1/identity/auth/oidc/callback?code=...&state=...
    ↓
[OidcCallback.Handler] (275 LOC)
    ├── Validate state parameter
    ├── Exchange code for tokens → IOidcProvider.ExchangeCodeAsync()
    ├── Decode ID token → IdTokenDecoder.DecodeAsync()
    ├── Find or create user → IUserRepository
    ├── Link federated identity → User.LinkFederatedIdentity()
    ├── Create session → LoginSessionCreator
    ├── Generate JWT + refresh token
    ├── Record security event
    └── Return { accessToken, refreshToken, user }
```

| Etapa | Backend real? | Persistência? | Auditoria? |
|---|---|---|---|
| Geração de URL OIDC | ✅ | — | — |
| Troca de código por token | ✅ | — | — |
| Validação de ID token | ✅ | — | — |
| Criação/linkage de utilizador | ✅ | ✅ User + ExternalIdentity | — |
| Criação de sessão | ✅ | ✅ Session | — |
| Registo de evento | ✅ | ✅ SecurityEvent | ✅ |

---

## 3. Fluxo real de autorização (por request)

```
[Request HTTP com JWT]
    ↓
[ASP.NET Authentication Middleware]
    ├── Validate JWT signature
    ├── Extract claims (userId, tenantId, permissions[])
    └── Set HttpContext.User
    ↓
[Authorization Middleware / RequireAuthorization]
    ├── Check endpoint policy (permission string)
    ├── Match against user claims.permissions[]
    └── 403 Forbidden se falhar
    ↓
[NexTraceDbContextBase — RLS]
    ├── SET app.tenant_id = {tenantId} (PostgreSQL session variable)
    └── Row-Level Security policies filtram dados por tenant
```

| Etapa | Backend real? | Enforcement? | Notas |
|---|---|---|---|
| JWT validation | ✅ | ✅ | Assinatura + expiração validadas |
| Permission check | ✅ | ✅ | Via `RequireAuthorization(permission)` |
| Tenant isolation (RLS) | ✅ | ✅ | PostgreSQL RLS via NexTraceDbContextBase |
| **Environment-aware check** | ⚠️ **PARCIAL** | ⚠️ | `IEnvironmentAccessValidator` existe mas uso no middleware não confirmado |

---

## 4. Fluxo de uso de tenant

```
[Após login] → POST /api/v1/identity/auth/select-tenant
    ↓
[SelectTenant.Handler] (96 LOC)
    ├── Validate TenantMembership (user pertence ao tenant?)
    ├── Set active tenant context
    ├── Generate new JWT com tenantId claim
    └── Return updated tokens
```

| Etapa | Backend real? | Persistência? |
|---|---|---|
| Validação de membership | ✅ | ✅ TenantMembership lookup |
| Geração de novo token | ✅ | — |
| Contexto activo | ✅ | Via JWT claims |

---

## 5. Fluxo de uso de ambiente na autorização

```
[Frontend] → EnvironmentContext.tsx selecciona ambiente
    ↓
[API calls incluem environmentId como header ou parâmetro]
    ↓
[ITenantEnvironmentContextResolver] → resolve TenantEnvironmentContext
    ↓
[IEnvironmentAccessValidator] → valida se user tem acesso ao ambiente
```

| Etapa | Backend real? | Enforcement? | Notas |
|---|---|---|---|
| Selecção de ambiente no frontend | ✅ | — | `EnvironmentContext.tsx` (9.4 KB) |
| Resolução de contexto | ✅ | — | Abstrações existem |
| Validação de acesso | ⚠️ **PARCIAL** | ⚠️ | `IEnvironmentAccessValidator` existe mas enforcement em endpoints não é sistemático |
| Filtragem de dados por ambiente | ⚠️ **PARCIAL** | ⚠️ | Nem todos os módulos consomem EnvironmentId |

---

## 6. Etapas sem backend real

| Etapa | Impacto | Prioridade |
|---|---|---|
| MFA verification handler | 🔴 CRÍTICO — utilizadores bypass MFA | P0 |
| Forgot password handler | 🟡 MÉDIO — UI existe sem backend | P2 |
| Account activation handler | 🟡 MÉDIO — UI existe sem backend | P2 |
| Invitation acceptance handler | 🟡 MÉDIO — UI existe sem backend | P2 |
| API Key CRUD endpoints | 🟠 ALTO — mencionado na arquitetura mas ausente | P1 |

---

## 7. Etapas cosméticas

| Item | Descrição |
|---|---|
| `ForgotPasswordPage.tsx` | Formulário renderiza mas submit não tem backend |
| `ResetPasswordPage.tsx` | Formulário renderiza mas token validation pode não estar completo |
| `ActivationPage.tsx` | UI presente, backend handler não confirmado |
| `InvitationPage.tsx` | UI presente, backend handler não confirmado |
| `MfaPage.tsx` | UI presente, MFA não é enforced no login flow |

---

## 8. Ausência de persistência

| Dado | Persistido? | Acção |
|---|---|---|
| Sessões | ✅ Session entity em identity_sessions | — |
| Security events | ✅ SecurityEvent em identity_security_events | — |
| Break Glass requests | ✅ BreakGlassRequest entity | — |
| JIT Access requests | ✅ JitAccessRequest entity | — |
| Delegations | ✅ Delegation entity | — |
| Access Review items | ✅ AccessReviewItem entity | — |
| **API Keys** | ❌ Sem entidade | Criar entidade + persistência |
| **MFA state/tokens** | ❌ Sem persistência | Necessário para MFA enforcement |
| **Refresh token hash** | ✅ RefreshTokenHash VO | Persistido em Session |

---

## 9. Ausência de rastreabilidade

| Acção | Auditada? | Acção necessária |
|---|---|---|
| Login sucesso/falha | ✅ SecurityAuditRecorder | — |
| OIDC callback | ✅ SecurityAuditRecorder | — |
| Account lockout | ✅ SecurityAuditRecorder | — |
| Break Glass activation | ✅ SecurityEventType | — |
| JIT Access request | ✅ SecurityEventType | — |
| Delegation creation | ✅ SecurityEventType | — |
| **Role assignment/removal** | ❌ Não auditado | Adicionar audit event |
| **Permission change** | ❌ Não auditado | Adicionar audit event |
| **User activation/deactivation** | ❌ Não auditado | Adicionar audit event |
| **Tenant selection** | ❌ Não auditado | Adicionar audit event |

---

## 10. Correções necessárias para fluxos reais

| ID | Correcção | Prioridade | Esforço |
|---|---|---|---|
| FL-01 | Implementar MFA verification handler e enforcement no login flow | 🔴 P0 | 2-3 semanas |
| FL-02 | Implementar API Key CRUD (create, list, revoke, rotate) | 🟠 P1 | 1 semana |
| FL-03 | Implementar expiração automática de JIT/Break Glass/Delegation (background job) | 🟠 P1 | 3 dias |
| FL-04 | Auditar role assignment, permission change, user activation | 🟠 P1 | 2 dias |
| FL-05 | Validar forgot password + reset password handlers existem e funcionam | 🟡 P2 | 3 dias |
| FL-06 | Validar activation + invitation handlers existem e funcionam | 🟡 P2 | 3 dias |
| FL-07 | Sistematizar environment-aware authorization em todos os endpoints | 🟡 P2 | 1 semana |
| FL-08 | Adicionar token blacklist ou short-lived tokens com session validation | 🟡 P2 | 1 semana |
