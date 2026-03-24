# Revisão Modular — Identity & Access

> **Data:** 2026-03-24  
> **Prioridade:** P1 (Fundação — todos os módulos dependem deste)  
> **Módulo Backend:** `src/modules/identityaccess/`  
> **Módulo Frontend:** `src/frontend/src/features/identity-access/`  
> **Fonte de verdade:** Código do repositório

---

## 1. Propósito do Módulo

O módulo **Identity & Access** é o módulo fundacional do NexTraceOne. Todos os outros módulos dependem dele para:

- Autenticação (local, federada via OIDC/SAML, MFA)
- Gestão de sessões com refresh token rotation
- Autorização baseada em permissões granulares
- Multi-tenancy com seleção de tenant
- Gestão de ambientes (environments) com criticidade e perfil
- Row-Level Security (RLS) — contexto tenant propagado para todos os DbContexts
- Funcionalidades enterprise: Break Glass, JIT Access, Delegações, Access Review Campaigns
- Tracking de eventos de segurança

---

## 2. Aderência ao Produto

| Aspecto | Avaliação | Observação |
|---------|-----------|------------|
| Alinhamento com a visão | ✅ Forte | Identity é fundação de todo produto enterprise |
| Completude funcional | ✅ Alta | 15 páginas frontend, 11 endpoint modules, 186+ testes |
| Maturidade do domínio | ✅ Alta | 20+ entidades, value objects ricos, eventos de domínio |
| Funcionalidades enterprise | ✅ Implementadas | Break Glass, JIT Access, Delegations, Access Reviews |
| Multi-tenancy | ✅ Completa | Tenant selection, membership, RLS propagation |

---

## 3. Páginas e Ações do Frontend

### 3.1 Páginas de Autenticação (Públicas)

| Página | Rota | Estado | Funcionalidades |
|--------|------|--------|----------------|
| LoginPage | `/login` | ✅ Funcional | Login local + SSO, react-hook-form + zod, remember me |
| ForgotPasswordPage | `/forgot-password` | ✅ Funcional | Recuperação de password |
| ResetPasswordPage | `/reset-password` | ✅ Funcional | Reset com token |
| ActivationPage | `/activate` | ✅ Funcional | Ativação de conta |
| MfaPage | `/mfa` | ✅ Funcional | Verificação MFA com código |
| InvitationPage | `/invitation` | ✅ Funcional | Aceitação de convite |
| TenantSelectionPage | `/select-tenant` | ✅ Funcional | Seleção multi-tenant |

### 3.2 Páginas de Gestão (Protegidas)

| Página | Rota | Permissão | Estado | Funcionalidades |
|--------|------|-----------|--------|----------------|
| UsersPage | `/users` | identity:users:read | ✅ Funcional | CRUD utilizadores, atribuição de roles, ativação/desativação |
| EnvironmentsPage | `/environments` | identity:users:read | ✅ Funcional | CRUD ambientes, designação de produção primária |
| BreakGlassPage | `/break-glass` | identity:sessions:read | ✅ Funcional | Pedidos de acesso de emergência + revogação |
| JitAccessPage | `/jit-access` | identity:users:read | ✅ Funcional | JIT access requests + aprovação/rejeição |
| DelegationPage | `/delegations` | identity:users:read | ✅ Funcional | Delegação de permissões com validade |
| AccessReviewPage | `/access-reviews` | identity:users:read | ✅ Funcional | Campanhas de recertificação de acesso |
| MySessionsPage | `/my-sessions` | identity:sessions:read | ✅ Funcional | Gestão de sessões ativas + revogação |
| UnauthorizedPage | `/unauthorized` | — | ✅ Funcional | Página 403 |

---

## 4. Rotas e Navegação

### 4.1 Estado Atual

Todas as rotas estão corretamente definidas no App.tsx. Não existem rotas quebradas neste módulo.

### 4.2 Itens de Menu

| Item | Secção | Rota | Estado |
|------|--------|------|--------|
| Users | admin | /users | ✅ Funcional |
| Break Glass | admin | /break-glass | ✅ Funcional |
| JIT Access | admin | /jit-access | ✅ Funcional |
| Delegations | admin | /delegations | ✅ Funcional |
| Access Review | admin | /access-reviews | ✅ Funcional |
| My Sessions | admin | /my-sessions | ✅ Funcional |

### 4.3 Rota Escondida (Sem Menu)

| Rota | Página | Motivo |
|------|--------|--------|
| `/environments` | EnvironmentsPage | Sub-rota de admin, acessível internamente |

---

## 5. Integração com Backend

### 5.1 Endpoints (11 Módulos)

| Módulo | Endpoints | Funcionalidade |
|--------|----------|---------------|
| AuthEndpoints | POST login, federated, refresh, logout, revoke, oidc/start, oidc/callback; GET me; PUT password | Autenticação completa |
| UserEndpoints | POST users, roles; GET users/:id, tenants/:id/users; PUT deactivate, activate | Gestão de utilizadores |
| RolePermissionEndpoints | GET roles, permissions | Catálogo de roles e permissões |
| EnvironmentEndpoints | GET, POST, PUT environments; set-primary-production; grant-access | Gestão de ambientes |
| BreakGlassEndpoints | POST break-glass, revoke; GET break-glass | Acesso de emergência |
| JitAccessEndpoints | POST jit-access, decide; GET pending | Acesso just-in-time |
| DelegationEndpoints | POST delegations, revoke; GET delegations | Delegação de permissões |
| TenantEndpoints | GET tenants/mine; POST select-tenant | Multi-tenancy |
| AccessReviewEndpoints | POST/GET access-reviews, decide | Recertificação |
| CookieSessionEndpoints | POST cookie-session, csrf-token; DELETE cookie-session | Sessão com cookies httpOnly |
| RuntimeContextEndpoints | GET context/runtime | Contexto operacional (user + tenant + environment) |

### 5.2 API Client Frontend

| Método | Backend Endpoint |
|--------|-----------------|
| login() | POST /auth/login |
| refresh() | POST /auth/refresh |
| logout() | POST /auth/logout |
| getCurrentUser() | GET /auth/me |
| getCsrfToken() | GET /auth/cookie-session/csrf-token |
| changePassword() | PUT /auth/password |
| forgotPassword() | POST /auth/forgot-password |
| resetPassword() | POST /auth/reset-password |
| activateAccount() | POST /auth/activate |
| verifyMfa() | POST /auth/mfa/verify |

---

## 6. Regras de Negócio

| Regra | Estado | Evidência |
|-------|--------|-----------|
| Password hashing com PBKDF2 | ✅ | Pbkdf2PasswordHasher.cs |
| Refresh token rotation (SHA-256) | ✅ | RefreshTokenHash.cs, Session.cs |
| JWT com claims granulares (sub, email, tenant, role, permissions) | ✅ | JwtTokenGenerator.cs |
| Session policy (duração, rotação) | ✅ | SessionPolicy value object |
| MFA policy | ✅ | MfaPolicy value object |
| Authentication policy (lockout) | ✅ | AuthenticationPolicy value object |
| Environment access validation | ✅ | EnvironmentAccessValidator.cs |
| Environment resolution via header/query | ✅ | EnvironmentResolutionMiddleware.cs |
| Break Glass com tempo limitado | ✅ | BreakGlassRequest entity |
| JIT Access com aprovação | ✅ | JitAccessRequest entity |
| Delegação com validade temporal | ✅ | Delegation entity |
| Access Review campaigns | ✅ | AccessReviewCampaign, AccessReviewItem |
| Security event tracking | ✅ | SecurityEvent entity, SecurityAuditRecorder |
| OIDC federated login | ✅ | OidcProviderService (Google, Azure, Okta) |
| SSO group mapping | ✅ | SsoGroupMapping entity |
| User creation domain event | ✅ | UserCreatedDomainEvent |
| User locked domain event | ✅ | UserLockedDomainEvent |

---

## 7. Banco de Dados

| Aspecto | Detalhe |
|---------|---------|
| DbContext | IdentityDbContext |
| DbSets | 16: Tenants, Users, Roles, Permissions, Sessions, TenantMemberships, ExternalIdentities, SsoGroupMappings, BreakGlassRequests, JitAccessRequests, Delegations, AccessReviewCampaigns, AccessReviewItems, SecurityEvents, Environments, EnvironmentAccesses |
| Migrations | 2: InitialCreate, AddIsPrimaryProductionToEnvironment |
| Multi-tenancy | ✅ RLS via TenantRlsInterceptor |
| Auditoria | ✅ AuditInterceptor (CreatedAt, CreatedBy, UpdatedAt, UpdatedBy) |
| Encriptação | ✅ EncryptedStringConverter (AES-GCM) |
| Soft Delete | ✅ |
| Outbox | ✅ Pattern para integration events |

---

## 8. i18n / Traduções

| Aspecto | Estado |
|---------|--------|
| Login/Auth pages | ✅ Completo — useTranslation() em todas |
| Admin pages | ✅ Completo |
| Error messages | ⚠️ A verificar — backend pode retornar mensagens não localizadas |
| 4 locales | ✅ en, es, pt-BR, pt-PT |

---

## 9. Layout e UX

| Aspecto | Avaliação |
|---------|-----------|
| Auth Shell | ✅ Split layout (55% hero + 45% card) — design enterprise |
| Auth Card | ✅ Componente reutilizável |
| Auth Feedback | ✅ Mensagens de erro/sucesso |
| Design tokens | ✅ Usa --nto-* |
| Loading states | ✅ Em todas as páginas |
| Error states | ✅ Em todas as páginas |
| Form validation | ✅ react-hook-form + zod |

---

## 10. Segurança / Autorização

| Aspecto | Estado | Evidência |
|---------|--------|-----------|
| ProtectedRoute | ✅ | Todas as rotas admin protegidas |
| Permissões granulares | ✅ | identity:users:read, identity:users:write, identity:sessions:read |
| CSRF protection | ✅ | CookieSessionEndpoints com CSRF token |
| Cookie httpOnly | ✅ | Sessão via cookie httpOnly |
| Rate limiting | ✅ | Aplicado via middleware |
| Password hashing | ✅ | PBKDF2 |
| JWT security | ✅ | HS256, claims, expiry |
| Refresh token rotation | ✅ | SHA-256 hash, rotação a cada refresh |
| Break Glass auditing | ✅ | Eventos de integração publicados |
| Security event tracking | ✅ | SecurityEvent entity |

---

## 11. Auditoria / Observabilidade

| Aspecto | Estado |
|---------|--------|
| Audit trail automático | ✅ AuditInterceptor |
| Security events | ✅ SecurityEvent entity + SecurityAuditRecorder |
| Integration events | ✅ UserCreated, UserRoleChanged, BreakGlassActivated |
| Bridge para Audit module | ✅ SecurityAuditBridge |
| Pipeline behavior | ✅ SecurityEventAuditBehavior |
| Logging estruturado | ✅ Serilog |

---

## 12. IA

| Aspecto | Estado |
|---------|--------|
| Integração com AI | ❌ Não aplicável a este módulo diretamente |

---

## 13. Agents

| Aspecto | Estado |
|---------|--------|
| Integração com Agents | ❌ Não aplicável a este módulo diretamente |

---

## 14. Documentação Funcional

| Documento | Existe | Estado |
|-----------|--------|--------|
| docs/SECURITY.md | ✅ | Cobre RLS, encriptação, hash chain |
| docs/SECURITY-ARCHITECTURE.md | ✅ | Zero Trust, threat model |
| docs/security/BACKEND-ENDPOINT-AUTH-AUDIT.md | ✅ | Auditoria de endpoints |
| docs/assessment/08-SECURITY-AUDIT.md | ✅ | Auditoria de segurança |
| User guide de identity | ❌ | Não existe — precisaria docs/user-guide/identity-access.md |

---

## 15. Documentação Técnica

| Aspecto | Estado |
|---------|--------|
| README do módulo | ❌ Não existe |
| Documentação de entidades | ❌ Inline apenas |
| Documentação de endpoints | ❌ Não existe API reference |
| Documentação de fluxo de auth | ❌ Não existe diagrama |
| Inline comments | ✅ Comentários em PT nos componentes React |

---

## 16. Resumo de Ações

### Ações de Validação (P1)

| # | Ação | Esforço |
|---|------|---------|
| 1 | Validar auth flow end-to-end (login → session → refresh → logout) | 2h |
| 2 | Validar permissões em ProtectedRoute (todos os módulos) | 2h |
| 3 | Validar multi-tenancy selection e RLS propagation | 2h |
| 4 | Validar OIDC federated login (Google, Azure, Okta) | 2h |
| 5 | Validar Break Glass + JIT Access workflows | 1h |

### Ações de Documentação (P2)

| # | Ação | Esforço |
|---|------|---------|
| 6 | Criar docs/user-guide/identity-access.md | 3h |
| 7 | Criar diagrama de fluxo de autenticação | 2h |
| 8 | Documentar API endpoints de identity | 2h |
| 9 | Consolidar SECURITY.md + SECURITY-ARCHITECTURE.md | 2h |

### Ações de Melhoria (P3)

| # | Ação | Esforço |
|---|------|---------|
| 10 | Verificar mensagens de erro i18n no backend | 2h |
| 11 | Adicionar /environments ao menu admin (atualmente escondida) | 15 min |
| 12 | Verificar cobertura de testes (186+ existentes) | 2h |
