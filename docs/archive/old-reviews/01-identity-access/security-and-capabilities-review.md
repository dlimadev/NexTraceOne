# PARTE 9 — Revisão de Segurança, Permissões e Capabilities

> Documento gerado em 2026-03-25 | Prompt N14 | Consolidação do módulo Identity & Access

---

## 1. Permissões por página

| Página | Rota | Permissão guard | Estado |
|---|---|---|---|
| UsersPage | `/users` | `identity:users:read` | ✅ |
| BreakGlassPage | `/break-glass` | `identity:sessions:read` | ✅ |
| JitAccessPage | `/jit-access` | `identity:users:read` | ✅ |
| DelegationPage | `/delegations` | `identity:users:read` | ✅ |
| AccessReviewPage | `/access-reviews` | `identity:users:read` | ✅ |
| MySessionsPage | `/my-sessions` | `identity:sessions:read` | ✅ |
| EnvironmentsPage | `/environments` | `identity:users:read` | ⚠️ Deveria ser `env:*` após migração |
| LoginPage | `/login` | Pública | ✅ |
| TenantSelectionPage | `/tenant-selection` | Pública (pós-auth) | ✅ |
| UnauthorizedPage | `/unauthorized` | Pública | ✅ |

---

## 2. Permissões por acção (backend)

| Acção | Endpoint | Permissão | Enforcement | Estado |
|---|---|---|---|---|
| Login | POST `/auth/login` | AllowAnonymous | ✅ Correcto | ✅ |
| OIDC start | POST `/auth/oidc/start` | AllowAnonymous | ✅ Correcto | ✅ |
| OIDC callback | GET `/auth/oidc/callback` | AllowAnonymous | ✅ Correcto | ✅ |
| Get current user | GET `/auth/me` | RequireAuthorization | ✅ | ✅ |
| Change password | PUT `/auth/password` | RequireAuthorization | ✅ | ✅ |
| Create user | POST `/users` | `identity:users:write` | ✅ | ✅ |
| Get user profile | GET `/users/{id}` | `identity:users:read` | ✅ | ✅ |
| List users | GET `/tenants/{id}/users` | `identity:users:read` | ✅ | ✅ |
| Assign role | POST `/users/{id}/roles` | `identity:roles:assign` | ✅ | ✅ |
| Deactivate user | PUT `/users/{id}/deactivate` | `identity:users:write` | ✅ | ✅ |
| List roles | GET `/roles` | `identity:roles:read` | ✅ | ✅ |
| List permissions | GET `/permissions` | `identity:permissions:read` | ✅ | ✅ |
| Request Break Glass | POST `/break-glass` | RequireAuthorization | ⚠️ Demasiado amplo | |
| Revoke Break Glass | POST `/break-glass/{id}/revoke` | `identity:sessions:revoke` | ✅ | ✅ |
| Request JIT Access | POST `/jit-access` | RequireAuthorization | ⚠️ Demasiado amplo | |
| Decide JIT Access | POST `/jit-access/{id}/decide` | `identity:sessions:revoke` | ⚠️ Permissão incorrecta | |
| Create Delegation | POST `/delegations` | RequireAuthorization | ⚠️ Demasiado amplo | |
| Revoke Delegation | POST `/delegations/{id}/revoke` | `identity:sessions:revoke` | ✅ | ✅ |
| Start Access Review | POST `/access-reviews` | `identity:users:write` | ✅ | ✅ |
| Decide Review Item | POST `/access-reviews/.../decide` | `identity:users:write` | ✅ | ✅ |
| List Environments | GET `/environments` | `identity:users:read` | ⚠️ Permissão incorrecta | |
| Create Environment | POST `/environments` | `identity:users:write` | ⚠️ Permissão incorrecta | |

---

## 3. Guards do frontend

| Guard | Ficheiro | LOC | Comportamento |
|---|---|---|---|
| ProtectedRoute | `components/ProtectedRoute.tsx` | 38 | Check permission → redirect se falhar |
| AuthContext | `contexts/AuthContext.tsx` | ~200 | Session bootstrap, token management |
| usePermissions | `hooks/usePermissions.ts` | 31 | `can(permission)` utility |
| Permission catalog | `auth/permissions.ts` | 161 | Type-safe permission strings |

### Avaliação dos guards

| Aspecto | Estado | Notas |
|---|---|---|
| Todas as rotas protegidas têm guard | ✅ | Via ProtectedRoute |
| Fallback para loading state | ✅ | Spinner durante isLoadingUser |
| Redirect para unauthorized | ✅ | Configurable redirectTo |
| Permission check no servidor | ✅ | Frontend é apenas UX; backend enforce |
| **CSRF protection** | ✅ | Cookie session mode com CSRF token |

---

## 4. Enforcement no backend

| Aspecto | Estado | Notas |
|---|---|---|
| Todos os endpoints protegidos | ✅ | Auth endpoints são AllowAnonymous; resto tem RequireAuthorization |
| RLS tenant isolation | ✅ | Via NexTraceDbContextBase |
| JWT signature validation | ✅ | ASP.NET middleware |
| Token expiration | ✅ | Validated no middleware |
| **MFA step-up enforcement** | ❌ | MfaPolicy.RequiredForPrivilegedOps não é verificado |
| **API Key validation middleware** | ❌ | Ausente |

---

## 5. Capabilities por provider/model/agent

O módulo Identity define as permissões que controlam AI capabilities:

| Permissão | Descrição | Roles com acesso |
|---|---|---|
| `ai:assistant:read` | Usar AI assistant (chat) | PlatformAdmin, TechLead, Developer |
| `ai:assistant:write` | Enviar prompts ao AI | PlatformAdmin, TechLead, Developer |
| `ai:governance:read` | Ver configuração de AI | PlatformAdmin |
| `ai:governance:write` | Alterar configuração de AI | PlatformAdmin |
| `ai:ide:read` | Usar IDE extensions | PlatformAdmin, TechLead, Developer |
| `ai:ide:write` | Configurar IDE extensions | PlatformAdmin |
| `ai:runtime:write` | Executar agents | PlatformAdmin |

### Lacunas de capabilities

| Lacuna | Acção |
|---|---|
| Sem permissão granular por agent | Considerar `ai:agents:{agentId}:execute` |
| Sem permissão para ver histórico de AI | Adicionar `ai:history:read` |
| Sem permissão para gestão de knowledge bases | Adicionar `ai:knowledge:read/write` |
| `ai:runtime:write` é genérica para execução | Granularizar por tipo de operação |

---

## 6. Acções sensíveis — revisão

| Acção sensível | Permissão | MFA step-up? | Audit event? | Estado |
|---|---|---|---|---|
| Alterar roles de utilizador | identity:roles:assign | ❌ Não | ❌ Não | 🔴 |
| Criar/desactivar utilizadores | identity:users:write | ❌ Não | ❌ Não | 🔴 |
| Revogar sessões | identity:sessions:revoke | ❌ Não | ⚠️ Parcial | 🟠 |
| Break Glass activation | Authenticated | ❌ Não | ✅ SecurityEvent | 🟡 |
| JIT Access decision | identity:sessions:revoke | ❌ Não | ✅ SecurityEvent | 🟡 |
| Delegation creation | Authenticated | ❌ Não | ✅ SecurityEvent | 🟡 |
| Access Review decision | identity:users:write | ❌ Não | ❌ Não | 🟠 |
| Change password | Authenticated | ❌ Não | ⚠️ Verificar | 🟡 |
| Create environment | identity:users:write | ❌ Não | ❌ Não | 🟠 |
| AI agent execution | ai:runtime:write | ❌ Não | ❌ Não | 🟠 |

---

## 7. Escopo por tenant

| Aspecto | Estado |
|---|---|
| Todas as queries filtram por TenantId | ✅ Via RLS |
| JWT contém TenantId claim | ✅ |
| Selecção de tenant gera novo token | ✅ |
| Cross-tenant access impossível (RLS) | ✅ |
| Admin pode aceder múltiplos tenants | ✅ Via TenantMembership |

---

## 8. Escopo por environment

| Aspecto | Estado | Notas |
|---|---|---|
| EnvironmentAccess entity existe | ✅ | Mapeia user → environment |
| IEnvironmentAccessValidator definida | ✅ | Abstracção presente |
| Enforcement sistemático em endpoints | ❌ | Não aplicado consistentemente |
| EnvironmentId em JWT claims | ⚠️ | Verificar se está incluído |

---

## 9. Auditoria de acções críticas

| Acção | SecurityEvent gerado? | Integration event? | Estado |
|---|---|---|---|
| Login success | ✅ | ✅ SecurityAuditBridge | ✅ |
| Login failure | ✅ | ✅ | ✅ |
| Account lockout | ✅ | ✅ | ✅ |
| OIDC callback | ✅ | ✅ | ✅ |
| Break Glass | ✅ | ✅ | ✅ |
| JIT Access | ✅ | ✅ | ✅ |
| Delegation | ✅ | ✅ | ✅ |
| **Role assignment** | ❌ | ❌ | 🔴 |
| **User creation** | ❌ | ❌ | 🔴 |
| **User deactivation** | ❌ | ❌ | 🔴 |
| **Access Review decision** | ❌ | ❌ | 🟠 |
| **Tenant selection** | ❌ | ❌ | 🟡 |
| **Password change** | ❌ | ❌ | 🟡 |

---

## 10. Backlog de correcções de segurança

| ID | Correcção | Prioridade | Esforço |
|---|---|---|---|
| S-01 | Implementar MFA verification e enforcement no login flow | 🔴 P0 | 2-3 semanas |
| S-02 | Adicionar audit events para role assignment | 🔴 P0 | 4h |
| S-03 | Adicionar audit events para user creation/deactivation | 🔴 P0 | 4h |
| S-04 | Adicionar audit events para Access Review decisions | 🟠 P1 | 2h |
| S-05 | Granularizar permissões de Break Glass/JIT/Delegation requests | 🟠 P1 | 4h |
| S-06 | Corrigir permissão de DecideJitAccess (identity:sessions:revoke → identity:jit:decide) | 🟠 P1 | 1h |
| S-07 | Implementar API Key authentication middleware | 🟠 P1 | 1 semana |
| S-08 | Implementar rate limiting em endpoints de autenticação | 🟡 P2 | 2 dias |
| S-09 | Adicionar password complexity policy | 🟡 P2 | 4h |
| S-10 | Validar IP/UserAgent consistency em token refresh | 🟡 P2 | 4h |
| S-11 | Adicionar audit para tenant selection | 🟡 P2 | 2h |
| S-12 | Adicionar audit para password change | 🟡 P2 | 2h |
| S-13 | Remover 17 licensing permissions do RolePermissionCatalog | 🟡 P2 | 2h |
| S-14 | Sistematizar environment-aware authorization | 🟡 P2 | 1 semana |
| S-15 | Adicionar granularidade a AI capabilities (per-agent, per-knowledge) | 🟡 P3 | 4h |
