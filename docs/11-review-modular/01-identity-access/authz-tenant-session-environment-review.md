# PARTE 8 — Revisão de Autenticação, Autorização, Tenant, Sessão e Ambiente

> Documento gerado em 2026-03-25 | Prompt N14 | Consolidação do módulo Identity & Access

---

## 1. Autenticação actual

### 1.1 Mecanismos suportados

| Mecanismo | Handler | Estado | Notas |
|---|---|---|---|
| Login local (email/password) | `LocalLogin.cs` (155 LOC) | ✅ Real | Password hash via PBKDF2, lockout, session creation |
| OIDC (redirect flow) | `StartOidcLogin.cs` + `OidcCallback.cs` (424 LOC) | ✅ Real | Full OIDC flow com state/nonce validation |
| Federated login (SAML/SCIM) | `FederatedLogin.cs` (134 LOC) | ✅ Real | Handler presente |
| Cookie session | `CookieSessionEndpoints.cs` | ✅ Real | Feature flag: `Auth:CookieSession:Enabled` |
| Refresh token | `RefreshToken.cs` (103 LOC) | ✅ Real | JWT refresh flow |
| **MFA (TOTP/WebAuthn/SMS)** | `MfaPolicy.cs` VO, `MfaPage.tsx` UI | ❌ **NÃO ENFORCED** | Modelado mas sem handler de verificação |
| **API Key authentication** | Mencionado no boundary matrix | ❌ **AUSENTE** | Sem entidade, endpoint ou middleware |

### 1.2 Lacunas críticas

| Lacuna | Impacto | Prioridade |
|---|---|---|
| MFA não é verificado durante login | Bypass de 2FA mesmo com política activa | 🔴 P0 |
| API Key auth não existe | Sem autenticação M2M (machine-to-machine) | 🟠 P1 |
| Password complexity policy não verificada | Passwords fracas possíveis | 🟡 P2 |
| Forgot/reset password handlers incertos | Self-service recovery pode não funcionar | 🟡 P2 |

---

## 2. Autorização actual

### 2.1 Modelo

| Aspecto | Implementação | Estado |
|---|---|---|
| Modelo | RBAC (Role-Based Access Control) | ✅ |
| Roles | 7: PlatformAdmin, TechLead, Developer, Viewer, Auditor, SecurityReview, ApprovalOnly | ✅ |
| Permissões | 73+ (sem licensing) | ✅ |
| Default | Deny-by-default | ✅ |
| Enforcement | `RequireAuthorization(permission)` em cada endpoint | ✅ |
| Frontend | `ProtectedRoute` component + `usePermissions` hook | ✅ |
| Claims | JWT com `permissions[]` array | ✅ |

### 2.2 Permissões por módulo

| Módulo | Prefixo | Count | Notas |
|---|---|---|---|
| Identity | `identity:*` | 7 | users, roles, sessions, permissions |
| Catalog | `catalog:*` | 2 | assets read/write |
| Contracts | `contracts:*` | 3 | read, write, import |
| Change Intelligence | `change-intelligence:*` | 2 | read, write |
| Operations | `operations:*` | ~14 | incidents, mitigation, runbooks, etc. |
| Governance | `governance:*` | ~20 | domains, teams, policies, etc. |
| Audit | `audit:*` | 4 | trail, reports, compliance, events |
| AI | `ai:*` | 4 | assistant, governance, IDE, runtime |
| Platform | `platform:*` | 2 | admin, settings |
| Promotion | `promotion:*` | 3 | requests, environments, gates |
| Rulesets | `rulesets:*` | 3 | read, write, execute |
| Integrations | `integrations:*` | 2 | read, write |
| **Licensing** ❌ | `licensing:*` | 17 | **RESÍDUO — remover** |

### 2.3 Problemas de autorização

| Problema | Impacto | Acção |
|---|---|---|
| Break Glass/JIT/Delegation usam `Authenticated` sem permissão granular | Qualquer utilizador autenticado pode criar | Adicionar permissões específicas |
| Environment endpoints usam `identity:users:read/write` | Permissão incorrecta para operações de ambiente | Migrar para `env:*` |
| Licensing permissions activas no catálogo | 17 permissões de módulo removido | Limpar |

---

## 3. Tenant actual

### 3.1 Implementação

| Aspecto | Implementação | Estado |
|---|---|---|
| Entity | `Tenant.cs` (82 LOC) | ✅ |
| Membership | `TenantMembership.cs` (71 LOC) — user pertence a N tenants | ✅ |
| Selecção | `SelectTenant.cs` — define tenant activo pós-login | ✅ |
| Claim JWT | `tenantId` claim no token | ✅ |
| RLS | `SET app.tenant_id` via NexTraceDbContextBase | ✅ |
| Frontend | `AuthContext.tsx` com `tenantId` state | ✅ |
| Multi-tenant | Um user pode pertencer a múltiplos tenants | ✅ |

### 3.2 Lacunas de tenant

| Lacuna | Impacto |
|---|---|
| Tenant creation não tem endpoint (admin only?) | Novos tenants criados como? |
| Tenant settings/configuration endpoint ausente | Sem gestão de tenant |
| Audit de tenant selection não existe | Mudança de contexto não rastreada |

---

## 4. Sessão actual

### 4.1 Implementação

| Aspecto | Implementação | Estado |
|---|---|---|
| Entity | `Session.cs` (78 LOC) | ✅ |
| Criação | `LoginSessionCreator.cs` (47 LOC) | ✅ |
| Dados | UserId, TenantId, IP, UserAgent, CreatedAt, ExpiresAt | ✅ |
| Revogação | `RevokeSession.cs` (51 LOC) | ✅ |
| Listagem | Endpoint ListActiveSessions | ✅ |
| Frontend | `MySessionsPage.tsx` (180 LOC) | ✅ |
| JWT refresh | `RefreshToken.cs` (103 LOC) | ✅ |
| RefreshTokenHash | `RefreshTokenHash.cs` VO no Session | ✅ |

### 4.2 Lacunas de sessão

| Lacuna | Impacto | Prioridade |
|---|---|---|
| IP/UserAgent collected mas não validated on refresh | Session hijacking possível | 🟡 P2 |
| Expiração automática não implementada (background job) | Sessions expiradas ficam na DB | 🟡 P2 |
| Sem limite de sessões simultâneas por user | Sem controlo | 🟡 P3 |

---

## 5. Ambiente na autorização

### 5.1 Implementação

| Aspecto | Implementação | Estado |
|---|---|---|
| Entidades | Environment, EnvironmentAccess (em Identity) | ✅ Presente |
| Abstrações | `IEnvironmentAccessValidator`, `IEnvironmentContextAccessor` | ✅ Definidas |
| Context | `TenantEnvironmentContext` VO | ✅ |
| Frontend | `EnvironmentContext.tsx` (9.4 KB) | ✅ |
| Selecção | Frontend permite seleccionar ambiente activo | ✅ |

### 5.2 Lacunas de ambiente

| Lacuna | Impacto | Prioridade |
|---|---|---|
| Environment-aware authorization não é sistemática | Endpoints não filtram por ambiente | 🟠 P1 |
| EnvironmentAccess enforcement parcial | User pode aceder dados de ambientes sem permissão | 🟠 P1 |
| EnvironmentsPage não está no sidebar | Feature undiscoverable | 🟡 P2 |
| 5 entidades de Environment acopladas a Identity | Bloqueio do módulo 02 | 🟠 P1 |

---

## 6. Capacidades sensíveis

### 6.1 Mapeamento

| Capacidade | Permissão | Enforcement | Estado |
|---|---|---|---|
| Gestão de utilizadores | identity:users:write | RequireAuthorization | ✅ |
| Atribuição de roles | identity:roles:assign | RequireAuthorization | ✅ |
| Revogação de sessões | identity:sessions:revoke | RequireAuthorization | ✅ |
| AI assistant | ai:assistant:read/write | RequireAuthorization (AI module) | ✅ |
| AI governance | ai:governance:read/write | RequireAuthorization (AI module) | ✅ |
| Platform admin | platform:admin:read/write | RequireAuthorization | ✅ |
| **MFA step-up** | MfaPolicy.RequiredForPrivilegedOps | ❌ **NÃO ENFORCED** | 🔴 |
| **Licensing operations** | licensing:vendor:* | ❌ **MÓDULO REMOVIDO** | Limpar |

---

## 7. Mínimo funcional real do módulo

### O que é real e funcional ✅

1. Login local com lockout e session creation
2. Login OIDC com full flow
3. Cookie session com feature flag
4. Token refresh
5. Logout e session revocation
6. RBAC com 73+ permissões e deny-by-default
7. Multi-tenancy com RLS
8. Break Glass, JIT, Delegation, Access Review
9. Security event logging
10. Frontend completo para 10 funcionalidades

### O que é parcial ⚠️

1. MFA — modelado mas não enforced
2. Environment-aware auth — abstrações existem mas enforcement parcial
3. Forgot/Reset password — UI sem backend confirmado
4. Activation/Invitation — UI sem backend confirmado

### O que é ausente ❌

1. API Key management
2. MFA verification handler
3. Background jobs de expiração
4. Audit events para role/user management actions
5. Password complexity enforcement
