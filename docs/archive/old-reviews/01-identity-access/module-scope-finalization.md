# PARTE 2 — Escopo Funcional Final do Módulo Identity & Access

> Documento gerado em 2026-03-25 | Prompt N14 | Consolidação do módulo Identity & Access

---

## 1. Funcionalidades já existentes ✅

| # | Funcionalidade | Ficheiros backend | Ficheiros frontend | Estado |
|---|---|---|---|---|
| F-01 | Login local (email/password) | `LocalLogin.cs` (155 LOC) | `LoginPage.tsx` (154 LOC) | ✅ Completo |
| F-02 | Login federado (OIDC) | `StartOidcLogin.cs` (149), `OidcCallback.cs` (275) | `LoginPage.tsx` | ✅ Completo |
| F-03 | Login federado (SAML/SCIM) | `FederatedLogin.cs` (134) | `LoginPage.tsx` | ✅ Completo |
| F-04 | Refresh token | `RefreshToken.cs` (103) | `AuthContext.tsx` | ✅ Completo |
| F-05 | Logout | `Logout.cs` (68) | `AuthContext.tsx` | ✅ Completo |
| F-06 | Cookie session (feature flag) | `CookieSessionEndpoints.cs` | `identity.ts` (API) | ✅ Completo |
| F-07 | Gestão de utilizadores | `CreateUser.cs`, `GetUserProfile.cs`, `ListTenantUsers.cs` | `UsersPage.tsx` (199 LOC) | ✅ Completo |
| F-08 | Activar/Desactivar utilizador | `ActivateUser.cs`, `DeactivateUser.cs` | `UsersPage.tsx` | ✅ Completo |
| F-09 | Atribuição de roles | `AssignRole.cs` (136) | `UsersPage.tsx` | ✅ Completo |
| F-10 | Listar roles e permissões | `ListRoles.cs`, `ListPermissions.cs` | `identity.ts` (API) | ✅ Completo |
| F-11 | Multi-tenancy | `Tenant.cs`, `TenantMembership.cs`, `SelectTenant.cs`, `ListMyTenants.cs` | `TenantSelectionPage.tsx` | ✅ Completo |
| F-12 | Break Glass (emergência) | `RequestBreakGlass.cs`, `RevokeBreakGlass.cs`, `ListBreakGlassRequests.cs` | `BreakGlassPage.tsx` (179 LOC) | ✅ Completo |
| F-13 | JIT Access (temporário) | `RequestJitAccess.cs`, `DecideJitAccess.cs`, `ListJitAccessRequests.cs` | `JitAccessPage.tsx` (218 LOC) | ✅ Completo |
| F-14 | Delegação de permissões | `CreateDelegation.cs`, `RevokeDelegation.cs`, `ListDelegations.cs` | `DelegationPage.tsx` (250 LOC) | ✅ Completo |
| F-15 | Access Review (campanhas) | `StartAccessReviewCampaign.cs`, `DecideAccessReviewItem.cs`, etc. | `AccessReviewPage.tsx` (308 LOC) | ✅ Completo |
| F-16 | Sessões activas | `RevokeSession.cs`, endpoint ListActiveSessions | `MySessionsPage.tsx` (180 LOC) | ✅ Completo |
| F-17 | Security Events (auditoria) | `SecurityEvent.cs`, `SecurityAuditRecorder.cs`, `SecurityEventType.cs` | — | ✅ Completo (backend) |
| F-18 | SSO Group Mapping | `SsoGroupMapping.cs` entity | — | ✅ Entity presente |
| F-19 | External Identity (OIDC linkage) | `ExternalIdentity.cs` entity | — | ✅ Entity presente |
| F-20 | Contexto de execução | `IOperationalExecutionContext`, `ITenantEnvironmentContextResolver` | `AuthContext.tsx`, `EnvironmentContext.tsx` | ✅ Completo |
| F-21 | Password change | `ChangePassword.cs` (144) | — | ✅ Backend completo |
| F-22 | Lockout por tentativas falhadas | `User.cs` (brute-force protection, 5 attempts → 15min) | — | ✅ Domain logic |

---

## 2. Funcionalidades parciais ⚠️

| # | Funcionalidade | O que existe | O que falta | Prioridade |
|---|---|---|---|---|
| P-01 | MFA (autenticação multifactor) | `MfaPolicy.cs` VO com ForSaaS/ForSelfHosted/ForOnPremise, `MfaPage.tsx` UI | Enforcement no fluxo de login — MFA modelado mas não bloqueante | 🔴 CRÍTICO |
| P-02 | Ambiente na autorização | `EnvironmentAccess.cs`, `IEnvironmentAccessValidator`, `IEnvironmentContextAccessor` | Enforcement real no middleware — Environment-aware access parcial | 🟠 ALTO |
| P-03 | Forgotten password | `ForgotPasswordPage.tsx` (113 LOC), `ResetPasswordPage.tsx` (148 LOC) | Backend handler para reset de password não confirmado | 🟡 MÉDIO |
| P-04 | Account activation | `ActivationPage.tsx` (129 LOC) | Backend handler para activação por token/email não confirmado | 🟡 MÉDIO |
| P-05 | Invitation flow | `InvitationPage.tsx` (187 LOC) | Backend handler para convites não confirmado | 🟡 MÉDIO |
| P-06 | Expiração automática de JIT/Break Glass/Delegation | Entities com `ExpiresAt` | Background job para expirar automaticamente | 🟠 ALTO |
| P-07 | Environments no sidebar | `EnvironmentsPage.tsx` (433 LOC), rota `/environments` | Entrada no `AppSidebar.tsx` ausente | 🟡 MÉDIO |

---

## 3. Funcionalidades ausentes ❌

| # | Funcionalidade | Obrigatório? | Justificação |
|---|---|---|---|
| A-01 | API Key management (criação, rotação, revogação) | 🔴 SIM | API keys mencionadas no boundary matrix mas sem CRUD endpoints |
| A-02 | RowVersion/ConcurrencyToken em entidades críticas | 🟠 SIM | Protecção contra updates concorrentes em User, Role, Delegation |
| A-03 | Auditoria de todas as acções sensíveis | 🟠 SIM | SecurityAuditRecorder cobre auth; falta cobertura para gestão de roles/permissões |
| A-04 | Rate limiting por endpoint de autenticação | 🟠 SIM | Protecção adicional contra brute-force além do lockout |
| A-05 | Token blacklist/revocation list | 🟡 RECOMENDADO | JWT é stateless; sem blacklist, tokens revogados podem ser usados até expirar |

---

## 4. Classificação: obrigatório vs futuro

### Obrigatório no produto final

| Funcionalidade | Estado | Acção |
|---|---|---|
| Autenticação completa (local + OIDC + cookie) | ✅ Existe | Manter |
| RBAC com 73+ permissões | ✅ Existe | Manter |
| Multi-tenancy com RLS | ✅ Existe | Manter |
| Sessões (JWT + cookie) | ✅ Existe | Manter |
| Break Glass / JIT / Delegation | ✅ Existe | Adicionar expiração automática |
| Access Review | ✅ Existe | Manter |
| Security Events | ✅ Existe | Expandir cobertura |
| MFA enforcement | ⚠️ Parcial | **Completar — blocker para produção** |
| API Key management | ❌ Ausente | **Implementar** |
| Prefixo iam_ nas tabelas | ❌ Pendente | **Preparar para migration reset** |

### Eliminado da visão (não pertence ao módulo)

| Item | Razão |
|---|---|
| Licensing permissions/entities | Módulo eliminado do produto — limpar resíduos |
| Ciclo de vida de ambientes completo | Pertence a Environment Management (02) |
| Vendor licensing operations | Módulo eliminado |

---

## 5. Conjunto mínimo completo do módulo final

### Core Authentication
- Login local + OIDC + SAML + cookie session
- MFA enforcement (quando política activa)
- Refresh token + logout
- Password change + lockout

### Core Authorization
- RBAC: 7 roles × 73+ permissions
- Deny-by-default enforcement
- Permission catalog (sem Licensing)
- Tenant-scoped authorization

### Enterprise Access
- Break Glass (com expiração automática)
- JIT Access (com expiração automática)
- Delegation (com NonDelegablePermissions)
- Access Review Campaigns

### Session & Security
- Session management (listar, revogar)
- Security events logging
- API Key management (CRUD)

### Infrastructure
- JWT token generation
- RLS tenant isolation
- Environment-aware context (até extracção)
- Integration events via outbox

---

## 6. Total de endpoints do módulo

| Grupo | Endpoints | Estado |
|---|---|---|
| Auth | 9 + 2 cookie | ✅ 11 endpoints |
| Users | 7 | ✅ |
| Roles/Permissions | 2 | ✅ |
| Break Glass | 3 | ✅ |
| JIT Access | 3 | ✅ |
| Delegations | 3 | ✅ |
| Access Review | 4 | ✅ |
| Environments | 6 | ⚠️ A migrar para módulo 02 |
| Runtime Context | 1 | ✅ |
| **Total** | **40** | **34 próprios + 6 de ambiente** |
