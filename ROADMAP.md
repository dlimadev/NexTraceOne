# NexTraceOne — Roadmap do Módulo Identity & Access

> Documento de acompanhamento da evolução do módulo de autenticação e autorização.
> Atualizado com base na análise arquitetural completa do repositório.

---

## Status Geral: 🟡 Parcialmente Completo

O módulo possui estrutura sólida no backend (domain, application, infrastructure, API) e integração básica com o frontend, mas **não está pronto para produção**. Existem gaps críticos em multi-tenancy, UX de autenticação, autorização granular no middleware, e ausência total de i18n no frontend.

---

## Legenda de Status

| Ícone | Significado |
|-------|-------------|
| ✅ | Completo |
| 🟡 | Parcial — funcional mas com gaps |
| 🔲 | Ausente — não implementado |
| ❌ | Incorreto — precisa correção conceitual |

---

## v1.0 — Fundação (Fase 1)

### Autenticação Backend

| Item | Status | Observação |
|------|--------|------------|
| Login local (email + senha) | ✅ | `LocalLogin` feature com PBKDF2, lockout, sessão |
| Login federado (OIDC) | 🟡 | `FederatedLogin` feature existe, mas recebe dados já resolvidos — falta integração real com OIDC provider (redirect, callback, token exchange) |
| Refresh token rotacionável | ✅ | `RefreshToken` feature com SHA-256 hash |
| Logout com revogação de sessão | ✅ | `Logout` feature revoga sessão ativa |
| Gestão de sessão (listar, revogar) | ✅ | `ListActiveSessions`, `RevokeSession` |
| Expiração/renovação segura | ✅ | TTL configurável, refresh token com rotação |
| Tratamento de lockout | ✅ | 5 tentativas → 15 min lockout |
| Endpoint `/me` | ✅ | `GetCurrentUser` com perfil + permissions |

### Autenticação Frontend

| Item | Status | Observação |
|------|--------|------------|
| Tela de login | 🟡 | Existe mas pede TenantId como GUID manual — UX incorreta |
| Callback OIDC | 🔲 | Não existe — sem redirect flow para provedores externos |
| Persistência de sessão | ✅ | localStorage com access_token, refresh_token, tenant_id |
| Carregamento de perfil ao iniciar | ✅ | `useEffect` no `AuthProvider` chama `/me` |
| Logout | ✅ | `logout()` revoga backend + limpa localStorage |
| Auto-redirect em 401 | ✅ | Interceptor Axios limpa sessão e redireciona |
| Preservação de deep link | 🔲 | Após login sempre redireciona para `/` |

### Autorização Backend

| Item | Status | Observação |
|------|--------|------------|
| 7 system roles | ✅ | PlatformAdmin, TechLead, Developer, Viewer, Auditor, SecurityReview, ApprovalOnly |
| 30 permissões granulares por módulo | ✅ | Catálogo em `Role.GetPermissionsForRole` + `PermissionConfiguration` seed |
| Permissões no JWT | ✅ | Array `permissions` incluído no token |
| `ICurrentUser.HasPermission()` | ✅ | Lê claims do JWT |
| Middleware de autorização por endpoint | ❌ | Apenas `RequireAuthorization()` genérico — não valida permissão específica. Qualquer usuário autenticado acessa qualquer endpoint protegido |
| Policy-based authorization | 🔲 | Não existe — sem `[Authorize(Policy="...")]` ou `RequireAuthorization("policy")` |
| Default deny | 🟡 | Endpoints sem `AllowAnonymous` exigem autenticação, mas não verificam permissão |

### Autorização Frontend

| Item | Status | Observação |
|------|--------|------------|
| `usePermissions` hook | ✅ | Lê permissões do servidor via `user.permissions` |
| `ProtectedRoute` component | ✅ | Guard de rota por permissão |
| Sidebar filtrada por permissão | ❌ | Usa códigos errados: `'users:read'` em vez de `'identity:users:read'`, `'releases:read'` em vez de `'change-intelligence:releases:read'` |
| Exibição de access denied | ✅ | `UnauthorizedPage` com UI clara |
| Textos hardcoded (sem i18n) | ❌ | Toda a UI está em inglês hardcoded — sem biblioteca i18n |

### Multi-Tenancy

| Item | Status | Observação |
|------|--------|------------|
| `TenantId` strongly typed | ✅ | `TenantId(Guid)` record |
| `TenantMembership` entity | ✅ | Vincula User → Tenant → Role |
| `TenantResolutionMiddleware` | ✅ | JWT claim → Header → Subdomínio |
| `CurrentTenantAccessor` | ✅ | Scoped, com capabilities |
| `ICurrentTenant` abstração | ✅ | Id, Slug, Name, IsActive, HasCapability |
| Entidade `Tenant` (aggregate) | ❌ | **Não existe**. TenantId é um GUID sem entidade associada — sem Name, Slug, Settings, IsActive persistidos |
| Tenant selection no login | ❌ | Usuário digita GUID manualmente — UX inaceitável |
| Endpoint `ListTenantsByUser` | 🔲 | Não existe — frontend não pode listar tenants do usuário |
| Auto-select quando tenant único | 🔲 | Não implementado |

### Ambiente (Environment)

| Item | Status | Observação |
|------|--------|------------|
| `DeploymentEnvironment` entity | ✅ | Existe no módulo Promotion — mas não conectado ao Identity |
| Autorização por ambiente | 🔲 | Não modelado — permissões não consideram ambiente |
| Seleção de ambiente no frontend | 🔲 | Não existe |
| Environment como dimensão de autorização | 🔲 | Modelo conceitual ausente |

### Sessão

| Item | Status | Observação |
|------|--------|------------|
| Criação com IP + User Agent | ✅ | |
| Revogação | ✅ | |
| Listagem de sessões ativas | ✅ | |
| Rotação de refresh token | ✅ | |
| Expiração controlada | ✅ | |

### Auditoria

| Item | Status | Observação |
|------|--------|------------|
| `SecurityEvent` entity | ✅ | 13 tipos de evento, risk score 0-100 |
| Repository de eventos | ✅ | `ISecurityEventRepository` |
| Geração de eventos nos handlers | 🟡 | Apenas Break Glass e JIT geram eventos — login, logout, role change não geram |
| Integração com módulo Audit | 🔲 | Eventos de Identity não publicados para o módulo de Audit central |

### UX de Acesso

| Item | Status | Observação |
|------|--------|------------|
| Experiência de primeiro acesso | 🔲 | Sem onboarding wizard |
| Seleção de tenant user-friendly | ❌ | Pede GUID — deveria mostrar lista com nomes |
| Feedback de erro estruturado | 🟡 | Backend retorna i18n codes, mas frontend não os interpreta |
| i18n no frontend | ❌ | Zero infraestrutura de i18n — todo texto hardcoded |

---

## v1.1 — Enterprise IAM

| Item | Status | Observação |
|------|--------|------------|
| Break Glass (emergencial) | ✅ | Domain + Feature + Endpoint + Frontend API |
| JIT Access (acesso temporário) | ✅ | Com anti-self-approval |
| Delegation (delegação formal) | ✅ | Com non-delegable permissions |
| Access Review Campaign | ✅ | Domain + EF config — falta feature/endpoint |
| Session Intelligence (SecurityEvent) | ✅ | Domain completo — falta background job |
| ExternalIdentity + SsoGroupMapping | ✅ | Domain + EF config — falta feature/endpoint |

---

## v2.0 — Preparação

| Item | Status |
|------|--------|
| SCIM Provisioning contracts | 🔲 |
| SAML binding real | 🔲 |
| MFA / Step-up Authentication | 🔲 |
| Delegated admin per tenant | 🔲 |

---

## Gaps Críticos para Produção

### 1. 🚨 Entidade `Tenant` Inexistente
Não existe aggregate `Tenant` com Name, Slug, Settings. `TenantId` é um GUID fantasma sem persistência. Impacto: impossível listar tenants, exibir nomes, gerenciar configurações.

### 2. 🚨 Login Pede GUID de Tenant
O campo `Tenant ID` na tela de login exige formato GUID (`00000000-0000-0000-0000-000000000001`). Usuário final **nunca** deveria ver ou digitar isso.

### 3. 🚨 Autorização no Backend é Apenas Autenticação
`RequireAuthorization()` sem policy = qualquer usuário autenticado acessa qualquer endpoint protegido. Um `Viewer` pode chamar `/users/create`, `/break-glass`, etc.

### 4. 🚨 Sidebar com Códigos de Permissão Errados
`Sidebar.tsx` usa `'users:read'`, `'releases:read'`, `'graph:read'` — os códigos reais são `'identity:users:read'`, `'change-intelligence:releases:read'`, `'engineering-graph:assets:read'`. Resultado: sidebar nunca filtra nada para ninguém.

### 5. 🚨 Zero i18n no Frontend
Nenhuma biblioteca de i18n instalada. Todos os textos são strings literais em inglês direto nos componentes. Viola diretriz obrigatória do projeto.

### 6. ⚠️ Fluxo OIDC Incompleto
`FederatedLogin` aceita dados já resolvidos (provider, externalId, email, name). Falta: redirect para provider → callback → token exchange → create-or-link. Sem isso, OIDC não funciona para o usuário final.

### 7. ⚠️ `Tenant` vs `Environment` Confusos
TenantId é usado como único contexto de escopo. Não há separação Environment. Permissões não variam por ambiente. Risco: um Developer com acesso a Dev pode ter as mesmas permissões em Production.

---

## Ações Recomendadas (Prioridade)

### Prioridade 1 — Blockers de Produção

1. **Criar aggregate `Tenant`** (Domain) com Name, Slug, IsActive, Settings. Criar `ITenantRepository`, endpoint `GET /tenants/mine`, EF config, seed.
2. **Refatorar tela de login**: após autenticar com email+senha, se o usuário tiver múltiplos tenants, exibir tela de seleção com nomes amigáveis. Se tiver um só, resolver automaticamente.
3. **Implementar autorização por permissão nos endpoints**: criar authorization policies ou middleware que valide `ICurrentUser.HasPermission("...")` por endpoint. Não basta `RequireAuthorization()`.
4. **Corrigir códigos de permissão na `Sidebar.tsx`**: alinhar com catálogo do backend (`identity:users:read`, etc.).
5. **Instalar e configurar i18n no frontend**: `react-i18next`, catálogos `pt-BR.json` e `en.json`, substituir todos os textos hardcoded.

### Prioridade 2 — Integridade do Módulo

6. **Modelar `Environment` como dimensão de autorização**: `UserEnvironmentPermission` ou `TenantMembership` expandido com EnvironmentId.
7. **Implementar OIDC redirect flow real**: `/auth/oidc/start?provider=azure` → redirect → `/auth/oidc/callback` → `FederatedLogin`.
8. **Gerar SecurityEvent em todos os fluxos críticos**: login, logout, role change, password change, session revoke.
9. **Integrar eventos de Identity com módulo Audit central**.
10. **Preservar deep link**: salvar URL original antes do redirect para login, restaurar após autenticação.

### Prioridade 3 — Qualidade e Completude

11. **Access Review features + endpoints**: `StartCampaign`, `DecideItem`, `ListPendingItems`.
12. **Handler-level tests** com NSubstitute para features enterprise.
13. **Background job** para expiração de Break Glass, JIT, Delegation e Access Review deadline.
14. **Sidebar user info**: `user?.roles?.[0]` referencia propriedade inexistente em `CurrentUserProfile` — deveria ser `user?.roleName`.

---

## Fluxo Ideal de Autenticação (Target State)

```
1. Usuário acessa a aplicação
2. Redireciona para /login
3. Autentica com email+senha OU redirect para OIDC provider
4. Backend resolve: a quais tenants o usuário pertence?
   - Se 1 tenant → resolve automaticamente, emite JWT com tenant_id
   - Se N tenants → retorna lista com {id, name, slug}
5. Frontend exibe tela de seleção de tenant (se necessário)
6. Usuário seleciona tenant → backend emite JWT final com tenant_id
7. Se aplicável, seleção de ambiente (Dev/Pre/Prod)
8. Backend aplica autorização: tenant + ambiente + role + permission
9. Frontend reflete contexto: sidebar filtrada, rotas protegidas, ações condicionais
```

---

## Modelo de Dados Recomendado

```
Tenant (aggregate)
├── TenantId, Name, Slug, IsActive, Settings, CreatedAt
├── TenantMembership (User → Tenant → Role)
└── TenantEnvironment (Tenant → Environment → Config)

Environment (aggregate — já existe no Promotion)
├── EnvironmentId, Name, Order, RequiresApproval
└── UserEnvironmentAccess (User → Tenant → Environment → Permissions)

Autorização efetiva = Tenant + Environment + Role + Permissions
```

---

*Última atualização: análise completa do repositório realizada sobre o branch `main`.*
