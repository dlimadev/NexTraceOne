# Módulo IdentityAccess — Identidade e Acesso

## Visão Geral

O **IdentityAccess** é o bounded context responsável por toda a gestão de identidade, autenticação, autorização e governança de acessos da plataforma NexTraceOne. Consolida os antigos módulos Identity, Roles, Sessions e FederatedLogin num contexto coeso, servindo como ponto central de controlo de acesso para todos os outros bounded contexts.

Este módulo suporta cenários enterprise complexos: multi-tenancy, autenticação federada (OIDC), autorização granular por ambiente, acesso privilegiado temporário (Break Glass, JIT, Delegação) e campanhas de recertificação de acessos.

---

## Responsabilidade

### O que este módulo gere

- Autenticação local (email + password) e federada (OIDC/SSO)
- Gestão de utilizadores, tenants e ambientes
- Modelo de autorização hierárquico: Role → Permission → Tenant → Environment
- Sessões com refresh token rotation e revogação
- Acesso privilegiado temporário: Break Glass, JIT Access, Delegação Formal
- Campanhas de Access Review (recertificação periódica)
- Registo completo de Security Events para auditoria
- Mapeamento de grupos SSO para roles internos
- Políticas de autenticação configuráveis por tenant (Local, Federated, Hybrid)

### O que este módulo **não** gere

- Auditoria central e evidence packs → **AuditCompliance**
- Licenciamento e capacidades comerciais → **CommercialGovernance**
- Catálogo de APIs e contratos → **Catalog**
- Workflows de aprovação e promoção → **ChangeGovernance**
- Dados de runtime e custos → **OperationalIntelligence**

A comunicação com outros bounded contexts faz-se exclusivamente via **Integration Events** (Outbox Pattern) ou interfaces definidas em **Contracts/ServiceInterfaces**.

---

## Arquitetura

O módulo segue a estrutura obrigatória de 3 camadas: **Domain → Application → Infrastructure**.

```
src/modules/identityaccess/
├── NexTraceOne.IdentityAccess.Domain/          ← Entidades, VOs, eventos, erros
├── NexTraceOne.IdentityAccess.Application/     ← Features CQRS, abstrações, behaviors
└── NexTraceOne.IdentityAccess.Infrastructure/  ← Persistência, endpoints, serviços
```

### Domain

A camada de domínio contém toda a lógica de negócio, invariantes e regras de identidade. Não depende de nenhuma outra camada.

#### Entidades (18 no total, 4 Aggregate Roots)

| Entidade | Tipo | Responsabilidade |
|----------|------|------------------|
| **User** | Aggregate Root | Utilizador da plataforma — suporta criação local e federada, lockout, gestão de perfil |
| **Tenant** | Aggregate Root | Organização/empresa — unidade de isolamento multi-tenant |
| **Session** | Aggregate Root | Sessão autenticada — refresh token rotation, revogação, expiração |
| **BreakGlassRequest** | Aggregate Root | Pedido de acesso de emergência — janela de 2h, limite trimestral de 3, post-mortem obrigatório |
| **JitAccessRequest** | Aggregate Root | Pedido de acesso temporário privilegiado — aprovação por outro utilizador, duração de 8h |
| **Delegation** | Aggregate Root | Delegação formal de permissões — validação contra auto-delegação e delegação de system admin |
| **AccessReviewCampaign** | Aggregate Root | Campanha de recertificação de acessos — prazo de 14 dias, itens individuais por utilizador |
| **AccessReviewItem** | Entity | Item individual de recertificação dentro de uma campanha |
| **Role** | Entity | Perfil funcional (7 roles de sistema: PlatformAdmin, TechLead, Developer, Viewer, Auditor, SecurityReview, ApprovalOnly) |
| **Permission** | Entity | Permissão granular no formato `{domínio}:{recurso}:{ação}` |
| **Environment** | Entity | Ambiente de deployment (Development, Pre-Production, Production) por tenant |
| **TenantMembership** | Entity | Associação utilizador ↔ tenant com role atribuído |
| **EnvironmentAccess** | Entity | Acesso granular por utilizador/ambiente com níveis (read/write/admin/none) |
| **SecurityEvent** | Entity | Evento de segurança com risk score (0-100), IP, user agent e metadata JSON |
| **ExternalIdentity** | Entity | Ligação entre identidade externa (OIDC/SAML) e utilizador interno |
| **SsoGroupMapping** | Entity | Mapeamento de grupo SSO externo para role interno |
| **RolePermissionCatalog** | Static Class | Catálogo imutável que mapeia cada role às suas permissões padrão |
| **SecurityEventType** | Static Class | Catálogo de 50+ tipos de eventos de segurança |

Todas as entidades usam **Strongly Typed IDs** (ex: `UserId(Guid)`, `TenantId(Guid)`).

#### Value Objects (7 no total)

| Value Object | Propriedades | Validação |
|-------------|-------------|-----------|
| **Email** | `Value` (normalizado para minúsculas) | Validação RFC de formato de email |
| **FullName** | `FirstName`, `LastName`, `Value` | Máx. 100 caracteres por campo |
| **HashedPassword** | `Value` (hash PBKDF2) | Formato `v1.{base64-salt}.{base64-hash}`, 100k iterações SHA-256 |
| **AuthenticationMode** | `IsFederated`, `IsLocal`, `IsHybrid` | Case-insensitive, valores: `Federated`, `Local`, `Hybrid` |
| **AuthenticationPolicy** | `Mode`, `AllowLocalFallback`, `RequireMfa`, `DefaultOidcProvider`, `SessionTimeoutMinutes`, `MaxConcurrentSessionsPerUser` | Modo federado rejeita fallback local e exige OIDC provider; timeout 5-1440 min; sessões 1-100 |
| **DeploymentModel** | `AllowsExternalConnectivity` | Valores: `SaaS`, `SelfHosted`, `OnPremise` |
| **RefreshTokenHash** | `Value` (SHA-256) | Hash seguro do refresh token para armazenamento |

#### Domain Events

| Evento | Payload | Quando é publicado |
|--------|---------|-------------------|
| `UserCreatedDomainEvent` | `UserId`, `Email` | Criação de utilizador (local ou federado) |
| `UserLockedDomainEvent` | `UserId`, `LockoutEnd` | Bloqueio por tentativas falhadas |

#### Catálogo de Erros (`IdentityErrors`)

50+ códigos de erro com chaves i18n no formato `Identity.{Módulo}.{Tipo}`:

- **Utilizadores:** `UserNotFound`, `EmailAlreadyExists`, `InvalidCredentials`, `AccountLocked`, `AccountDeactivated`
- **Sessões:** `SessionExpired`, `SessionRevoked`, `InvalidRefreshToken`
- **Roles/Permissões:** `RoleNotFound`, `InsufficientPermissions`
- **Tenants:** `TenantNotFound`, `TenantSlugAlreadyExists`, `TenantMembershipNotFound`
- **Break Glass:** `BreakGlassNotFound`, `BreakGlassQuotaExceeded`, `BreakGlassNotActive`
- **JIT Access:** `JitAccessNotFound`, `JitSelfApprovalNotAllowed`, `JitAccessNotPending`
- **Delegação:** `DelegationNotFound`, `DelegationScopeExceedsGrantor`, `DelegationSystemAdminNotAllowed`
- **Access Review:** `AccessReviewCampaignNotFound`, `AccessReviewItemNotFound`, `AccessReviewItemAlreadyDecided`
- **Ambientes:** `EnvironmentNotFound`, `EnvironmentSlugAlreadyExists`, `EnvironmentAccessDenied`, `EnvironmentNotActive`
- **OIDC:** `OidcProviderNotConfigured`, `OidcCallbackFailed`

Todos os erros seguem o **Result Pattern** — nunca se usam exceções para controlo de fluxo.

---

### Application

A camada de aplicação orquestra os casos de uso via CQRS (Commands e Queries) usando MediatR.

#### Features (37 handlers CQRS)

##### Autenticação e Sessões

| Feature | Tipo | Descrição |
|---------|------|-----------|
| `LocalLogin` | Command | Autenticação com email + password → `LoginResponse` |
| `FederatedLogin` | Command | Início do fluxo OIDC — redireciona para provider externo |
| `OidcCallback` | Command | Callback OIDC — cria/vincula utilizador, gera sessão |
| `RefreshToken` | Command | Rotação de refresh token → novo par access + refresh |
| `Logout` | Command | Invalidação da sessão corrente |
| `RevokeSession` | Command | Revogação administrativa de sessão específica |
| `SelectTenant` | Command | Seleção de tenant ativo (contexto multi-tenant) |

##### Gestão de Utilizadores

| Feature | Tipo | Descrição |
|---------|------|-----------|
| `CreateUser` | Command | Criação de utilizador local ou federado → `UserId` |
| `ActivateUser` | Command | Reactivação de utilizador desativado |
| `DeactivateUser` | Command | Desativação de utilizador |
| `GetCurrentUser` | Query | Perfil do utilizador autenticado |
| `GetUserProfile` | Query | Perfil de utilizador específico |
| `ListTenantUsers` | Query | Listagem paginada de utilizadores do tenant |
| `ChangePassword` | Command | Alteração de password com validação da password atual |

##### Roles e Permissões

| Feature | Tipo | Descrição |
|---------|------|-----------|
| `AssignRole` | Command | Atribuição de role a utilizador num tenant |
| `ListRoles` | Query | Listagem de roles disponíveis |
| `ListPermissions` | Query | Listagem de permissões por módulo |

##### Acesso Privilegiado — Break Glass

| Feature | Tipo | Descrição |
|---------|------|-----------|
| `RequestBreakGlass` | Command | Pedido de acesso de emergência → `BreakGlassRequestId` |
| `RevokeBreakGlass` | Command | Revogação de acesso de emergência |
| `ListBreakGlassRequests` | Query | Listagem de pedidos com trilha de auditoria |

##### Acesso Privilegiado — JIT

| Feature | Tipo | Descrição |
|---------|------|-----------|
| `RequestJitAccess` | Command | Pedido de acesso temporário → `JitAccessRequestId` |
| `DecideJitAccess` | Command | Aprovação/rejeição de pedido JIT |
| `ListJitAccessRequests` | Query | Listagem de pedidos pendentes e históricos |

##### Delegação

| Feature | Tipo | Descrição |
|---------|------|-----------|
| `CreateDelegation` | Command | Delegação formal de permissões → `DelegationId` |
| `RevokeDelegation` | Command | Revogação de delegação |
| `ListDelegations` | Query | Listagem de delegações ativas |

##### Access Review (Recertificação)

| Feature | Tipo | Descrição |
|---------|------|-----------|
| `StartAccessReviewCampaign` | Command | Início de campanha de recertificação → `AccessReviewCampaignId` |
| `GetAccessReviewCampaign` | Query | Detalhes da campanha com itens |
| `ListAccessReviewCampaigns` | Query | Listagem de campanhas |
| `DecideAccessReviewItem` | Command | Decisão sobre item (aprovar/revogar) |

##### Ambientes

| Feature | Tipo | Descrição |
|---------|------|-----------|
| `ListEnvironments` | Query | Listagem de ambientes do tenant |
| `GrantEnvironmentAccess` | Command | Atribuição de acesso granular a ambiente |

##### Sessões e Tenants

| Feature | Tipo | Descrição |
|---------|------|-----------|
| `ListActiveSessions` | Query | Sessões ativas do utilizador |
| `ListMyTenants` | Query | Tenants associados ao utilizador |

#### Abstrações (20 interfaces)

**Repositórios (12 interfaces):**
`IUserRepository`, `ISessionRepository`, `ITenantRepository`, `IRoleRepository`, `IPermissionRepository`, `ITenantMembershipRepository`, `IEnvironmentRepository`, `ISecurityEventRepository`, `IBreakGlassRepository`, `IJitAccessRepository`, `IDelegationRepository`, `IAccessReviewRepository`

**Serviços (8 interfaces):**

| Interface | Responsabilidade |
|-----------|------------------|
| `IJwtTokenGenerator` | Geração de access e refresh tokens JWT |
| `IPasswordHasher` | Hash e verificação de passwords (PBKDF2) |
| `IOidcProvider` | Início e callback do fluxo OIDC |
| `ILoginResponseBuilder` | Resolução de memberships/permissões e construção da resposta de login |
| `ILoginSessionCreator` | Orquestração de criação de sessão + geração de tokens |
| `ISecurityAuditRecorder` | Registo de eventos de segurança no módulo de auditoria |
| `ISecurityAuditBridge` | Ponte para reencaminhar eventos para o módulo AuditCompliance |
| `ISecurityEventTracker` | Acumulação de eventos de segurança durante o ciclo de vida do request |

#### Behaviors

- **SecurityEventAuditBehavior** — behavior de pipeline MediatR que regista automaticamente eventos de segurança para todos os commands processados.

#### Localização

- `IdentityMessages` — recursos centralizados de mensagens i18n para o módulo.

---

### Infrastructure

A camada de infraestrutura implementa todos os detalhes técnicos: persistência, endpoints HTTP e serviços externos.

#### Persistência (EF Core + PostgreSQL)

- **IdentityDbContext** — `sealed`, herda de `NexTraceDbContextBase`
  - Interceptors: Row-Level Security (RLS) + Audit tracking
  - 20 configurações Fluent API para todas as entidades
  - Migração inicial: `20260313210303_InitialIdentitySchema`

- **12 repositórios** implementam as interfaces definidas na camada Application

- **DesignTimeFactory** para geração de migrações EF Core

#### Endpoints (Minimal API)

Rota base: `/api/v1/identity`

| Grupo | Endpoints | Exemplos |
|-------|-----------|----------|
| **AuthEndpoints** | Login, logout, refresh, password, OIDC, tenant selection | `POST /auth/login`, `POST /auth/refresh` |
| **UserEndpoints** | CRUD de utilizadores | `POST /users`, `GET /users/{id}` |
| **RolePermissionEndpoints** | Listagem de roles e permissões | `GET /roles`, `GET /permissions` |
| **TenantEndpoints** | Tenants do utilizador | `GET /tenants/mine` |
| **EnvironmentEndpoints** | Ambientes e acessos | `GET /environments`, `POST /environments/access` |
| **BreakGlassEndpoints** | Acesso de emergência | `POST /break-glass`, `DELETE /break-glass/{id}` |
| **JitAccessEndpoints** | Acesso temporário | `POST /jit`, `PUT /jit/{id}/decide` |
| **DelegationEndpoints** | Delegação formal | `POST /delegations`, `DELETE /delegations/{id}` |
| **AccessReviewEndpoints** | Recertificação | `POST /access-reviews`, `PUT /access-reviews/items/{id}` |

Cada endpoint segue o padrão: **receber request → enviar para MediatR → retornar resultado via `ToHttpResult()`**. Nenhuma regra de negócio nos endpoints.

#### Serviços (7 implementações)

| Serviço | Interface | Detalhes |
|---------|-----------|----------|
| **JwtTokenGenerator** | `IJwtTokenGenerator` | HMAC-SHA256; claims: sub, email, name, tenant_id, role_id, permissions; configurável via `Jwt:*` |
| **Pbkdf2PasswordHasher** | `IPasswordHasher` | PBKDF2-HMAC-SHA256, 100k iterações, salt de 16 bytes, formato `v1.{salt}.{hash}` |
| **OidcProviderService** | `IOidcProvider` | Authorization Code flow via OpenID Connect discovery; suporta Azure AD, Okta, etc. |
| **SecurityAuditBridge** | `ISecurityAuditBridge` | Reencaminha eventos de segurança críticos para o módulo AuditCompliance via Integration Events |
| **SecurityEventTracker** | `ISecurityEventTracker` | Scoped por request — acumula eventos e faz flush no final |
| **IdTokenDecoder** | *(interno)* | Descodificação de ID tokens OIDC e extração de claims |
| **IdentityModuleService** | `IIdentityModule` | Contrato público exposto a outros módulos via interface |

---

## Fluxos Principais

### 1. Autenticação Local

```
Utilizador → POST /auth/login (email, password)
  → LocalLogin.Handler
    → IUserRepository.GetByEmailAsync()
    → HashedPassword.Verify(password)
    → Verifica IsActive, IsLocked
    → User.RegisterSuccessfulLogin() / RegisterFailedLogin()
    → ILoginSessionCreator.CreateSessionAsync()
      → Session.Create(userId, refreshTokenHash, ip, userAgent)
      → IJwtTokenGenerator.GenerateAccessToken(claims)
      → IJwtTokenGenerator.GenerateRefreshToken()
    → ILoginResponseBuilder.BuildResponseAsync()
      → Resolve TenantMembership, Role, Permissions
    → SecurityEvent (auth.succeeded / auth.failed)
  → LoginResponse { accessToken, refreshToken, expiresIn, tenants[], permissions[] }
```

**Proteção contra brute force:** Após N tentativas falhadas, a conta é bloqueada (`User.IsLocked()`) e gera-se um `UserLockedDomainEvent`.

### 2. Autenticação Federada (OIDC)

```
Utilizador → POST /auth/federated (provider, returnUrl)
  → FederatedLogin.Handler
    → IOidcProvider.StartAuthenticationAsync()
    → Preserva deep link (returnUrl) no state parameter
  → Redirect 302 → Provider OIDC (Azure AD, Okta, etc.)

Provider → GET /auth/oidc/callback (code, state)
  → OidcCallback.Handler
    → IOidcProvider.HandleCallbackAsync(code)
    → IdTokenDecoder — extrai claims (sub, email, name, groups)
    → IUserRepository.GetByFederatedIdentityAsync(provider, externalId)
      → Se não existe: cria User + ExternalIdentity (auto-provisioning)
      → Se existe: atualiza claims se necessário
    → SsoGroupMapping — mapeia grupos SSO para roles internos
    → ILoginSessionCreator.CreateSessionAsync()
    → SecurityEvent (oidc.callback_success / oidc.callback_failed)
  → Redirect → returnUrl original (deep link preservado)
```

**Auto-provisioning:** Utilizadores OIDC são criados automaticamente no primeiro login. O mapeamento de grupos SSO garante que o role correcto é atribuído sem intervenção manual.

### 3. Seleção de Tenant

```
Utilizador autenticado → POST /auth/select-tenant (tenantId)
  → SelectTenant.Handler
    → Verifica TenantMembership (utilizador pertence ao tenant?)
    → Verifica Tenant.IsActive
    → Gera novo access token com tenant_id e role_id no contexto
  → LoginResponse atualizado com permissões do tenant selecionado
```

Um utilizador pode pertencer a múltiplos tenants com roles diferentes em cada um. A seleção de tenant define o contexto ativo para todas as operações subsequentes.

### 4. Autorização Granular

O modelo de autorização é hierárquico e combina quatro dimensões:

```
Role → define conjunto base de permissões (via RolePermissionCatalog)
  └── Permission → permissão granular no formato {domínio}:{recurso}:{ação}
        └── Tenant → contexto organizacional onde a permissão é válida
              └── Environment → ambiente específico com nível de acesso (read/write/admin)
```

**Roles de sistema (7):**

| Role | Perfil | Permissões-chave |
|------|--------|------------------|
| `PlatformAdmin` | Administrador total | Todas as permissões |
| `TechLead` | Líder técnico / aprovador | Aprovação de workflows, gestão de equipa |
| `Developer` | Desenvolvedor | Submissão de mudanças, leitura de catálogo/contratos |
| `Viewer` | Leitor | Acesso read-only ao catálogo e releases |
| `Auditor` | Auditor | Trilha de auditoria e exportação de evidências |
| `SecurityReview` | Revisor de segurança | Access reviews, gestão de sessões, análise de risco |
| `ApprovalOnly` | Aprovador | Apenas aprovação/rejeição de workflows |

**Permissões granulares (17+):** Organizadas por categoria usando o padrão `{domínio}:{recurso}:{ação}`:
- `identity:users:read`, `identity:users:write`, `identity:roles:assign`, `identity:sessions:revoke`
- `contracts:read`, `contracts:write`
- `catalog:read`, `catalog:write`
- `workflow:read`, `workflow:write`, `workflow:approve`
- `audit:read`, `audit:export`
- `releases:read`, `releases:write`

**Enforcement:** O backend é sempre a fonte de verdade — o frontend usa permissões recebidas via `/me` apenas para controlo visual (UX). Toda API verifica autenticação e autorização.

### 5. Sessões

#### Criação
Cada login (local ou OIDC) cria um registo `Session` com:
- `RefreshTokenHash` (SHA-256 do refresh token)
- `CreatedByIp` e `UserAgent` para auditoria
- `ExpiresAt` configurável via `AuthenticationPolicy.SessionTimeoutMinutes`

#### Renovação (Refresh Token Rotation)
```
POST /auth/refresh (refreshToken)
  → RefreshToken.Handler
    → ISessionRepository.GetByRefreshTokenHashAsync(hash)
    → Verifica sessão ativa e não expirada
    → Session.Rotate() — gera novo refresh token, invalida o anterior
    → Novo par access + refresh token
```

Cada refresh token é de **uso único** — após rotação, o token anterior é invalidado (proteção contra token replay).

#### Revogação
- **Self-service:** `POST /auth/logout` — invalida a sessão corrente
- **Administrativa:** `DELETE /sessions/{id}` — admin revoga sessão de outro utilizador
- **Automática:** Sessões expiradas são marcadas como inativas

#### Limite de Sessões Concorrentes
Configurável via `AuthenticationPolicy.MaxConcurrentSessionsPerUser` (1-100). Sessões mais antigas são revogadas quando o limite é atingido.

### 6. Acesso Privilegiado

#### Break Glass (Acesso de Emergência)

Mecanismo de elevação de privilégios para situações de emergência com controlos rigorosos:

```
POST /break-glass (justification)
  → RequestBreakGlass.Handler
    → Verifica quota trimestral (máx. 3 por utilizador)
    → BreakGlassRequest.Create(userId, tenantId, justification)
    → Janela de acesso: 2 horas (DefaultAccessWindow)
    → SecurityEvent (break_glass_activated) com risk score elevado
  → BreakGlassRequestId

DELETE /break-glass/{id} (postMortemNotes)
  → RevokeBreakGlass.Handler
    → BreakGlassRequest.Revoke()
    → Post-mortem obrigatório até 24h após ativação
```

**Controlos:**
- Limite trimestral de 3 utilizações por utilizador
- Janela de acesso limitada a 2 horas
- Post-mortem obrigatório dentro de 24 horas
- Evento de segurança com risk score elevado para revisão

#### JIT Access (Acesso Temporário Just-in-Time)

Elevação de privilégios controlada com aprovação por outro utilizador:

```
POST /jit (permissionCode, scope, justification)
  → RequestJitAccess.Handler
    → JitAccessRequest.Create(...)
    → Prazo de aprovação: 4 horas (DefaultApprovalTimeout)
  → JitAccessRequestId

PUT /jit/{id}/decide (approved, reason)
  → DecideJitAccess.Handler
    → Verifica que o decisor ≠ solicitante (sem auto-aprovação)
    → Se aprovado: acesso válido por 8 horas (DefaultAccessDuration)
    → SecurityEvent (jit_requested / jit_approved / jit_rejected)
```

#### Delegação Formal

Delegação explícita e auditável de permissões entre utilizadores:

```
POST /delegations (delegateeId, permissions[], reason, validUntil)
  → CreateDelegation.Handler
    → Validação: sem auto-delegação
    → Validação: permissões de system admin não podem ser delegadas
    → Validação: scope não excede permissões do grantor
    → Delegation.Create(...)
    → SecurityEvent (delegation_created)
  → DelegationId
```

### 7. Access Review (Recertificação)

Campanhas periódicas de revisão de acessos para conformidade:

```
POST /access-reviews (name, deadline?)
  → StartAccessReviewCampaign.Handler
    → Cria campanha com prazo (default: 14 dias)
    → Gera AccessReviewItem para cada utilizador do tenant
    → Status: Open → InProgress (quando a primeira decisão é tomada)
  → AccessReviewCampaignId

PUT /access-reviews/items/{id} (decision: approve|revoke)
  → DecideAccessReviewItem.Handler
    → Se revoke: desativa TenantMembership do utilizador
    → SecurityEvent (access_review.item_approved / item_revoked)
    → Quando todos os itens são decididos: campanha → Completed
```

**Expiração automática:** Itens não decididos até ao deadline resultam em revogação automática (`access_review.expired_auto_revoked`).

---

## Suporte a SaaS e On-Prem / Self-Hosted

O módulo foi desenhado para funcionar em múltiplos modelos de deployment sem alterações de código.

### DeploymentModel (Value Object)

| Modelo | Conectividade externa | Cenário típico |
|--------|----------------------|----------------|
| `SaaS` | ✅ Sim | Plataforma gerida pelo fornecedor |
| `SelfHosted` | ✅ Sim | Instalação no cliente com acesso à internet |
| `OnPremise` | ❌ Não | Instalação isolada sem conectividade externa |

### AuthenticationMode (Value Object)

| Modo | Login local | Login OIDC | Caso de uso |
|------|-------------|------------|-------------|
| `Local` | ✅ | ❌ | On-premise sem IdP externo |
| `Federated` | ❌ | ✅ | Enterprise com SSO obrigatório |
| `Hybrid` | ✅ | ✅ | Transição ou cenários mistos |

### AuthenticationPolicy (Value Object Composto)

Configuração completa da política de autenticação por tenant:
- `Mode` — modo de autenticação ativo
- `AllowLocalFallback` — permite login local quando OIDC falha (apenas em modo Hybrid)
- `RequireMfa` — exige MFA (preparado mas não implementado no MVP1)
- `DefaultOidcProvider` — provider OIDC padrão (obrigatório em modo Federated)
- `SessionTimeoutMinutes` — timeout de sessão (5-1440 minutos)
- `MaxConcurrentSessionsPerUser` — limite de sessões simultâneas (1-100)

**Factory methods pré-configurados:**
- `AuthenticationPolicy.ForSaaS()` — configuração optimizada para SaaS
- `AuthenticationPolicy.ForSelfHosted()` — configuração para instalação self-hosted
- `AuthenticationPolicy.Default()` — configuração segura por defeito

---

## Tenant vs Environment

Estes dois conceitos são frequentemente confundidos, mas têm papéis distintos:

| Aspecto | Tenant | Environment |
|---------|--------|-------------|
| **O que representa** | Organização / empresa | Fase do pipeline de deployment |
| **Exemplos** | "ACME Corp", "Globex Inc" | "Development", "Pre-Production", "Production" |
| **Isolamento** | Dados completamente isolados (RLS) | Acesso granular dentro do tenant |
| **Multiplicity** | Um utilizador pode pertencer a vários tenants | Um tenant tem vários environments |
| **Role** | Atribuído por tenant (via TenantMembership) | Nível de acesso por environment (via EnvironmentAccess) |
| **Controlo** | PlatformAdmin gere | TechLead/PlatformAdmin configura acessos |

**Exemplo concreto:**
> O utilizador `multi@globex-inc.test` tem role **Developer** no tenant ACME Corp (acesso write a Dev, read a Pre-Prod) e role **TechLead** no tenant Globex Inc (acesso admin a todos os environments).

---

## Security Events

O módulo regista 50+ tipos de eventos de segurança agrupados em categorias. Cada evento inclui: `TenantId`, `UserId`, `SessionId`, `EventType`, `Description`, `RiskScore` (0-100), `IpAddress`, `UserAgent`, `MetadataJson`, `OccurredAt`.

### Categorias de Eventos

| Categoria | Tipos | Exemplos |
|-----------|-------|----------|
| **Autenticação** | 4 | `security.auth.succeeded`, `security.auth.failed`, `security.auth.logout`, `security.auth.account_locked` |
| **Anomalias** | 6 | `security.anomaly.unknown_location`, `security.anomaly.outside_hours`, `security.anomaly.concurrent_sessions`, `security.anomaly.rapid_approval`, `security.anomaly.high_approval_volume`, `security.anomaly.first_resource_access` |
| **Acesso Privilegiado** | 3 | `security.privileged.break_glass_activated`, `security.privileged.jit_requested`, `security.privileged.delegation_created` |
| **Resposta** | 2 | `security.response.session_suspended`, `security.response.stepup_mfa_required` |
| **Identidade** | 5 | `security.identity.role_assigned`, `security.identity.role_revoked`, `security.identity.password_changed`, `security.identity.password_reset_admin`, `security.identity.password_change_failed` |
| **Ambientes** | 4 | `security.environment.access_denied`, `security.environment.access_granted`, `security.environment.access_revoked`, `security.environment.access_expired` |
| **Access Review** | 4 | `security.access_review.started`, `security.access_review.item_approved`, `security.access_review.item_revoked`, `security.access_review.expired_auto_revoked` |
| **OIDC/Federação** | 3 | `security.oidc.flow_started`, `security.oidc.callback_success`, `security.oidc.callback_failed` |
| **Expiração** | 3 | `security.privileged.delegation_expired`, `security.privileged.break_glass_expired`, `security.privileged.jit_expired` |

Eventos críticos (Break Glass, anomalias, lockouts) são automaticamente reencaminhados para o módulo **AuditCompliance** via `ISecurityAuditBridge` e Integration Events.

---

## Prontidão para OIDC/SCIM

### OIDC — Implementado ✅

- Authorization Code flow completo
- Suporte a múltiplos providers (Azure AD, Okta, qualquer provider compatível com OpenID Connect)
- Auto-provisioning de utilizadores no primeiro login federado
- Deep link preservation (returnUrl no state parameter)
- Mapeamento de grupos SSO para roles internos (`SsoGroupMapping`)
- Token decoding e extração de claims

### SCIM — Preparado 🟡

- Entidades `ExternalIdentity` e `SsoGroupMapping` já existem no domínio
- Interfaces de repositório definidas
- Falta implementar os endpoints SCIM 2.0 (`/scim/v2/Users`, `/scim/v2/Groups`)
- Estrutura pronta para receber provisioning/deprovisioning automático de IdPs

### WebAuthn/Passkeys — Futuro 🔲

- `AuthenticationPolicy.RequireMfa` já existe como propriedade
- Implementação de WebAuthn/FIDO2 planeada para fase posterior ao MVP1
- Infra de MFA step-up preparada (`security.response.stepup_mfa_required`)

---

## Decisões Arquiteturais

| Decisão | Justificação |
|---------|-------------|
| **PBKDF2-HMAC-SHA256 (100k iterações)** | Padrão OWASP recomendado; equilibra segurança e performance em deployment on-premise sem hardware dedicado |
| **JWT com HMAC-SHA256** | Simples e eficaz para monolith; sem necessidade de chaves assimétricas no MVP1 |
| **Refresh Token Rotation** | Cada refresh token é de uso único — mitiga token replay attacks |
| **Refresh token em memória (frontend)** | Nunca em localStorage; access token em sessionStorage (escopo de aba) |
| **Sem Redis no MVP1** | Sessions em PostgreSQL; latência aceitável para escala MVP1 |
| **Row-Level Security (RLS)** | Isolamento multi-tenant garantido a nível de base de dados, não apenas aplicacional |
| **Strongly Typed IDs** | `UserId(Guid)`, `TenantId(Guid)` — previne mistura acidental de identificadores |
| **Result Pattern (sem exceções)** | Todas as operações que podem falhar retornam `Result<T>` — exceções reservadas para erros inesperados |
| **SecurityEvent como entidade (não log)** | Eventos de segurança são dados de primeira classe, consultáveis e auditáveis |
| **Integration Events via Outbox** | Comunicação assíncrona com AuditCompliance — garante consistência eventual sem acoplamento directo |
| **Feature como Vertical Slice** | Cada feature contém Command/Query + Validator + Handler + Response num único ficheiro |

---

## Riscos Residuais

| Risco | Impacto | Mitigação planeada |
|-------|---------|-------------------|
| **MFA não implementado no MVP1** | Autenticação depende apenas de password ou OIDC | `AuthenticationPolicy.RequireMfa` preparado; WebAuthn planeado pós-MVP1 |
| **JWT secret simétrico (HMAC)** | Comprometimento do secret expõe todos os tokens | Migração para RS256 (assimétrico) planeada; secret externalizado via variável de ambiente |
| **Sem rate limiting explícito** | Endpoints de login vulneráveis a brute force | Lockout por tentativas falhadas existe; rate limiting a nível de reverse proxy recomendado |
| **Sessions em PostgreSQL** | Latência superior a Redis para validação de sessão | Aceitável no MVP1; migração para Redis quando escala justificar |
| **SCIM não implementado** | Provisionamento automático de IdPs não disponível | Entidades e interfaces preparadas; endpoints SCIM 2.0 planeados |
| **Sem audit log immutability** | Eventos de segurança são mutáveis (flag `IsReviewed`) | Integridade criptográfica planeada no módulo AuditCompliance |

---

## Prontidão para Testes Funcionais

### Testes Unitários Existentes (34 ficheiros)

**Testes de Domínio (19):**
- `UserTests`, `TenantTests`, `SessionTests`, `RolePermissionCatalogTests`
- `BreakGlassRequestTests`, `JitAccessRequestTests`, `DelegationTests`
- `AccessReviewCampaignTests`, `SecurityEventTests`, `ExternalIdentityTests`
- `EmailTests`, `HashedPasswordTests`, `FullNameTests` (implícito nos VOs)
- `AuthenticationModeTests`, `AuthenticationPolicyTests`, `DeploymentModelTests`

**Testes de Aplicação (15):**
- `LocalLoginTests`, `RefreshTokenTests`, `LogoutTests`, `SelectTenantTests`
- `CreateUserTests`, `GetCurrentUserTests`, `ChangePasswordTests`
- `AssignRoleTests`, `RevokeSessionTests`, `GrantEnvironmentAccessTests`
- `ListEnvironmentsTests`, `ListMyTenantsTests`
- `LoginResponseBuilderTests`, `LoginSessionCreatorTests`, `SecurityAuditRecorderTests`

**Test Doubles:**
- `TestCurrentTenant` — simulação de contexto de tenant
- `TestDateTimeProvider` — controlo determinístico do tempo

### Massa de Teste (Seed Data)

10 scripts SQL que criam um ambiente completo para testes funcionais e de integração:
- 3 tenants (2 activos + 1 inactivo)
- 6 ambientes (Dev/Pre-Prod/Prod × 2 tenants)
- 10 utilizadores cobrindo todos os perfis
- 7 roles com 17 permissões
- Memberships, acessos a ambientes, sessões, eventos de segurança
- Cenários de acesso privilegiado (Break Glass, JIT, Delegação)

Ver [`database/seeds/identity-access/README.md`](../../database/seeds/identity-access/README.md) para detalhes completos da massa de teste.

### Como Executar Testes

```bash
# Testes unitários do módulo
dotnet test tests/modules/identityaccess/NexTraceOne.IdentityAccess.Tests/

# Todos os testes da solução
dotnet test NexTraceOne.sln
```

---

## Referências

- [API de Integração Externa](EXTERNAL-INTEGRATION-API.md) — endpoints, DTOs, exemplos cURL
- [Massa de Teste](../../database/seeds/identity-access/README.md) — scripts SQL e credenciais
- [Arquitectura](../ARCHITECTURE.md) — regras de dependência entre bounded contexts
- [Convenções](../CONVENTIONS.md) — padrões de código, idioma e documentação
- [Segurança](../SECURITY.md) — pilares de segurança, RLS, encryption
