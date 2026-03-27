# PARTE 6 — Revisão Backend Funcional do Módulo Identity & Access

> Documento gerado em 2026-03-25 | Prompt N14 | Consolidação do módulo Identity & Access

---

## 1. Inventário de endpoints

### Auth Endpoints (11)
| # | Método | Rota | Handler | Permissão | Estado |
|---|---|---|---|---|---|
| E-01 | POST | `/auth/login` | LocalLogin | AllowAnonymous | ✅ |
| E-02 | POST | `/auth/federated` | FederatedLogin | AllowAnonymous | ✅ |
| E-03 | POST | `/auth/refresh` | RefreshToken | AllowAnonymous | ✅ |
| E-04 | POST | `/auth/logout` | Logout | Authenticated | ✅ |
| E-05 | POST | `/auth/revoke` | RevokeSession | Authenticated | ✅ |
| E-06 | GET | `/auth/me` | GetCurrentUser | Authenticated | ✅ |
| E-07 | PUT | `/auth/password` | ChangePassword | Authenticated | ✅ |
| E-08 | POST | `/auth/oidc/start` | StartOidcLogin | AllowAnonymous | ✅ |
| E-09 | GET | `/auth/oidc/callback` | OidcCallback | AllowAnonymous | ✅ |
| E-10 | POST | `/auth/cookie-session` | LocalLogin (cookie) | Feature flag | ✅ |
| E-11 | GET | `/auth/cookie-session/csrf-token` | CSRF token | Feature flag | ✅ |

### User Endpoints (7)
| # | Método | Rota | Handler | Permissão | Estado |
|---|---|---|---|---|---|
| E-12 | POST | `/users` | CreateUser | identity:users:write | ✅ |
| E-13 | GET | `/users/{id}` | GetUserProfile | identity:users:read | ✅ |
| E-14 | GET | `/tenants/{tenantId}/users` | ListTenantUsers | identity:users:read | ✅ |
| E-15 | POST | `/users/{userId}/roles` | AssignRole | identity:roles:assign | ✅ |
| E-16 | PUT | `/users/{userId}/deactivate` | DeactivateUser | identity:users:write | ✅ |
| E-17 | PUT | `/users/{userId}/activate` | ActivateUser | identity:users:write | ✅ |
| E-18 | GET | `/users/{userId}/sessions` | ListActiveSessions | identity:users:read | ✅ |

### Roles/Permissions Endpoints (2)
| # | Método | Rota | Handler | Permissão | Estado |
|---|---|---|---|---|---|
| E-19 | GET | `/roles` | ListRoles | identity:roles:read | ✅ |
| E-20 | GET | `/permissions` | ListPermissions | identity:permissions:read | ✅ |

### Break Glass Endpoints (3)
| # | Método | Rota | Handler | Permissão | Estado |
|---|---|---|---|---|---|
| E-21 | POST | `/break-glass` | RequestBreakGlass | Authenticated | ✅ |
| E-22 | POST | `/break-glass/{id}/revoke` | RevokeBreakGlass | identity:sessions:revoke | ✅ |
| E-23 | GET | `/break-glass` | ListBreakGlassRequests | identity:sessions:read | ✅ |

### JIT Access Endpoints (3)
| # | Método | Rota | Handler | Permissão | Estado |
|---|---|---|---|---|---|
| E-24 | POST | `/jit-access` | RequestJitAccess | Authenticated | ✅ |
| E-25 | POST | `/jit-access/{id}/decide` | DecideJitAccess | identity:sessions:revoke | ✅ |
| E-26 | GET | `/jit-access/pending` | ListJitAccessRequests | identity:sessions:read | ✅ |

### Delegation Endpoints (3)
| # | Método | Rota | Handler | Permissão | Estado |
|---|---|---|---|---|---|
| E-27 | POST | `/delegations` | CreateDelegation | Authenticated | ✅ |
| E-28 | POST | `/delegations/{id}/revoke` | RevokeDelegation | identity:sessions:revoke | ✅ |
| E-29 | GET | `/delegations` | ListDelegations | identity:users:read | ✅ |

### Access Review Endpoints (4)
| # | Método | Rota | Handler | Permissão | Estado |
|---|---|---|---|---|---|
| E-30 | POST | `/access-reviews` | StartAccessReviewCampaign | identity:users:write | ✅ |
| E-31 | GET | `/access-reviews` | ListAccessReviewCampaigns | identity:users:read | ✅ |
| E-32 | GET | `/access-reviews/{id}` | GetAccessReviewCampaign | identity:users:read | ✅ |
| E-33 | POST | `/access-reviews/{id}/items/{itemId}/decide` | DecideAccessReviewItem | identity:users:write | ✅ |

### Environment Endpoints (6) — ⚠️ a migrar para módulo 02
| # | Método | Rota | Handler | Permissão | Estado |
|---|---|---|---|---|---|
| E-34 | GET | `/environments` | ListEnvironments | identity:users:read | ⚠️ |
| E-35 | GET | `/environments/primary-production` | GetPrimaryProductionEnvironment | identity:users:read | ⚠️ |
| E-36 | POST | `/environments` | CreateEnvironment | identity:users:write | ⚠️ |
| E-37 | PUT | `/environments/{id}` | UpdateEnvironment | identity:users:write | ⚠️ |
| E-38 | PATCH | `/environments/{id}/primary-production` | SetPrimaryProductionEnvironment | identity:users:write | ⚠️ |
| E-39 | POST | `/environments/{id}/access` | GrantEnvironmentAccess | identity:users:write | ⚠️ |

### Runtime Context (1)
| # | Método | Rota | Handler | Permissão | Estado |
|---|---|---|---|---|---|
| E-40 | GET | `/context/runtime` | GetRuntimeContext | Authenticated | ✅ |

### Tenant Endpoints (2)
| # | Método | Rota | Handler | Permissão | Estado |
|---|---|---|---|---|---|
| E-41 | GET | `/tenants/mine` | ListMyTenants | Authenticated | ✅ |
| E-42 | POST | `/auth/select-tenant` | SelectTenant | Authenticated | ✅ |

---

## 2. Endpoints mortos

Nenhum endpoint morto identificado. Todos os 42 endpoints têm handler correspondente.

---

## 3. Endpoints incompletos

| Endpoint | Problema | Acção |
|---|---|---|
| E-21 RequestBreakGlass | Permissão é apenas `Authenticated` — deveria ser mais granular | Avaliar `identity:break-glass:request` |
| E-24 RequestJitAccess | Permissão é apenas `Authenticated` — deveria ser mais granular | Avaliar `identity:jit-access:request` |
| E-27 CreateDelegation | Permissão é apenas `Authenticated` — deveria ser mais granular | Avaliar `identity:delegations:create` |
| E-34 a E-39 | Usam `identity:users:read/write` genérico para operações de ambiente | Devem usar `env:*` após migração |

---

## 4. Revisão de validações

| Handler | Validação | Estado |
|---|---|---|
| LocalLogin | Email + password obrigatórios, lockout check | ✅ |
| CreateUser | Email format, FullName, duplicate check | ✅ |
| AssignRole | Role exists, user exists, membership check | ✅ |
| CreateDelegation | Self-delegation blocked, NonDelegablePermissions, permission ownership check | ✅ |
| StartAccessReviewCampaign | Campaign name, scope validation | ✅ |
| RequestBreakGlass | Justification required | ✅ |
| ChangePassword | Old password validation, complexity? | ⚠️ Verificar se há policy de complexidade |

---

## 5. Revisão de tratamento de erro

| Aspecto | Estado | Notas |
|---|---|---|
| Result<T> pattern | ✅ Usado | Errors propagados via Result |
| Exception handling | ✅ | Via middleware global |
| Mensagens de erro genéricas (login) | ✅ | "Invalid credentials" sem detalhes |
| Logging de erros | ✅ | Via ILogger |

---

## 6. Revisão de auditoria por acção

| Acção | Auditada? | Via |
|---|---|---|
| Login success | ✅ | SecurityAuditRecorder |
| Login failure | ✅ | SecurityAuditRecorder |
| Account lockout | ✅ | SecurityAuditRecorder |
| OIDC callback | ✅ | SecurityAuditRecorder |
| Session revoke | ⚠️ Parcial | Verificar se gera SecurityEvent |
| Role assignment | ❌ | Adicionar |
| User creation | ❌ | Adicionar |
| User activation/deactivation | ❌ | Adicionar |
| Break Glass request | ✅ | SecurityEventType |
| JIT Access request | ✅ | SecurityEventType |
| Delegation creation | ✅ | SecurityEventType |
| Access Review decision | ❌ | Adicionar |
| Tenant selection | ❌ | Adicionar |

---

## 7. Backlog de correcções backend

| ID | Correcção | Prioridade | Esforço | Ficheiro(s) |
|---|---|---|---|---|
| B-01 | Implementar MFA verification handler (ValidateMfa command) | 🔴 P0 | 2-3 sem | Novo handler + User.cs |
| B-02 | Criar entidade ApiKey + CRUD endpoints | 🟠 P1 | 1 sem | Novo entity + 4 endpoints |
| B-03 | Implementar background job para expiração de JIT/BreakGlass/Delegation | 🟠 P1 | 3 dias | Novo HostedService |
| B-04 | Adicionar audit events para role assignment, user creation, activation | 🟠 P1 | 2 dias | AssignRole.cs, CreateUser.cs, etc. |
| B-05 | Adicionar audit event para Access Review decisions | 🟠 P1 | 1 dia | DecideAccessReviewItem.cs |
| B-06 | Adicionar RowVersion/xmin a entidades mutáveis | 🟠 P1 | 2 dias | Todas as configs |
| B-07 | Verificar/implementar forgot password + reset password handlers | 🟡 P2 | 3 dias | Novos handlers |
| B-08 | Verificar/implementar activation + invitation handlers | 🟡 P2 | 3 dias | Novos handlers |
| B-09 | Granularizar permissões de Break Glass/JIT/Delegation endpoints | 🟡 P2 | 1 dia | Endpoint files |
| B-10 | Limpar licensing permissions do RolePermissionCatalog | 🟡 P2 | 2h | RolePermissionCatalog.cs |
| B-11 | Limpar licensing seed data de PermissionConfiguration | 🟡 P2 | 1h | PermissionConfiguration.cs |
| B-12 | Preparar renomeação de prefixo identity_ → iam_ | 🟡 P2 | 1 dia | Todas as configs |
| B-13 | Avaliar password complexity policy no ChangePassword | 🟡 P2 | 2h | ChangePassword.cs |
| B-14 | Adicionar rate limiting nos endpoints de autenticação | 🟡 P2 | 2 dias | AuthEndpoints.cs |
| B-15 | Implementar token blacklist ou session-bound token validation | 🟡 P3 | 1 sem | Novo serviço |
