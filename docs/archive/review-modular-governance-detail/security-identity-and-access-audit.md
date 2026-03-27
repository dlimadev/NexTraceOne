# Auditoria de Segurança — Identidade e Controlo de Acesso

> **Módulo:** `src/modules/identityaccess/`  
> **Data da análise:** 2025-07  
> **Classificação global:** ENTERPRISE_READY_APPARENT (~85 % de maturidade)  
> **Autor:** Equipa de Governança de Segurança — NexTraceOne

---

## Índice

1. [Resumo Executivo](#1-resumo-executivo)
2. [Arquitectura de Autenticação](#2-arquitectura-de-autenticação)
3. [Modelo de Autorização](#3-modelo-de-autorização)
4. [Isolamento de Tenant](#4-isolamento-de-tenant)
5. [Controlo de Ambientes](#5-controlo-de-ambientes)
6. [Acesso Avançado (JIT, Break Glass, Delegação, Access Review)](#6-acesso-avançado)
7. [Eventos de Segurança e Auditoria](#7-eventos-de-segurança-e-auditoria)
8. [Segurança do Frontend](#8-segurança-do-frontend)
9. [Análise de Lacunas](#9-análise-de-lacunas)
10. [Recomendações e Plano de Acção](#10-recomendações-e-plano-de-acção)

---

## 1. Resumo Executivo

O módulo de Identidade e Acesso do NexTraceOne apresenta uma arquitectura de segurança multi-camada de nível empresarial. A análise abrangeu 5 projectos C# (`IdentityAccess.Domain`, `IdentityAccess.Application`, `IdentityAccess.Infrastructure`, `IdentityAccess.API`, `IdentityAccess.IntegrationEvents`), o `IdentityDbContext` com 15 `DbSet`s, 73 permissões únicas distribuídas por 13 módulos, 7 papéis de sistema, 11 módulos de endpoints e toda a camada frontend correspondente.

### Métricas-chave

| Dimensão | Valor | Classificação |
|---|---|---|
| Autenticação | JWT + API Key + OIDC + Cookie | ENTERPRISE_READY_APPARENT |
| Autorização | 73 permissões, 7 papéis, política dinâmica | GRANULAR_AND_COHERENT |
| Isolamento de tenant | RLS PostgreSQL + middleware + JWT claim | STRONG_ISOLATION_APPARENT |
| Controlo de ambientes | Entidade first-class com AccessLevel temporal | FIRST_CLASS_CONCERN_APPARENT |
| Acesso avançado | JIT + Break Glass + Delegação + Access Review | IMPLEMENTED_APPARENT |
| Eventos de segurança | 15+ tipos, risk scoring 0-100, bridge MediatR | TRACEABLE |
| Frontend | In-memory refresh, CSRF, permissões server-driven | GOOD_PRACTICE_APPARENT |

### Conclusão principal

A camada de segurança atinge aproximadamente **85 % de maturidade empresarial**. A arquitectura multi-camada (JWT + API Key + OIDC, modelo granular de 73 permissões com políticas dinâmicas, RLS PostgreSQL para isolamento de tenant, ambiente como dimensão first-class, JIT + Break Glass + Delegação + Access Review implementados ao nível de entidade + endpoint, rastreamento abrangente de SecurityEvent com risk scoring, bridge MediatR para módulo de auditoria central) é sólida e coerente. As lacunas residuais são conhecidas e documentadas.

---

## 2. Arquitectura de Autenticação

### 2.1 JWT Bearer (Primário)

| Aspecto | Detalhe | Evidência |
|---|---|---|
| Algoritmo | HMAC-SHA256 | `JwtTokenService` |
| Expiração | 60 minutos | Configuração JWT |
| Claims | sub, email, name, tenant_id, role_id, permissions | `JwtTokenService` |
| Validação | Issuer + Audience + Lifetime + SigningKey | `Program.cs` (AddAuthentication) |
| Chave dev fallback | Existe, validação de arranque obriga chave externa em produção | `Program.cs` |

### 2.2 API Key (Sistema-a-Sistema)

| Aspecto | Detalhe |
|---|---|
| Header | `X-Api-Key` |
| Comparação | `CryptographicOperations.FixedTimeEquals()` (resistente a timing attacks) |
| Armazenamento actual | In-memory via `appsettings` (MVP1) |
| **Lacuna** | Necessita migração para BD encriptada |

### 2.3 OIDC Federation

| Aspecto | Detalhe |
|---|---|
| Interface | `IOidcProvider` → `OidcProviderService` |
| Fluxo | Authorization Code + PKCE |
| Protecção CSRF | Validação de state |
| Tokens do provider | Nunca armazenados |
| Configuração | Per-tenant |
| Endpoints | `POST /auth/federated`, `GET/POST /auth/oidc/start`, `GET /auth/oidc/callback` (AllowAnonymous + rate-limited) |

### 2.4 Autenticação Local

- Hashing de palavras-passe: **BCrypt** (`Pbkdf2PasswordHasher`)
- Bloqueio após **5 tentativas falhadas** (15 minutos)
- Factory methods separados para utilizadores locais vs. federados

### 2.5 Gestão de Sessões

- Entidade `Session` com `RefreshTokenHash` (SHA-256), `ExpiresAt`, `CreatedByIp`, `UserAgent`, `RevokedAt`
- Rotação de token no refresh
- Cookie session opcional com protecção CSRF (`UseCookieSessionCsrfProtection`)

### 2.6 Política de MFA

- Value object com `RequiredOnLogin`, `RequiredForPrivilegedOps`, `AllowedMethods` (TOTP/WebAuthn/SMS)
- Factory methods: `ForSaaS`, `ForSelfHosted`, `ForOnPremise`, `Disabled`
- **Enforcement ADIADO** — política modelada, step-up não implementado

### 2.7 Hybrid PolicyScheme

Esquema "smart" que roteia para API Key se `X-Api-Key` presente, caso contrário JWT Bearer.

### 2.8 Rate Limiting em Endpoints de Autenticação

| Política | Limite |
|---|---|
| `auth` | 20/min |
| `auth-sensitive` | 10/min |

---

## 3. Modelo de Autorização

### 3.1 Permissões

- **73 permissões únicas** distribuídas por 13 módulos
- Formato: `módulo:recurso:acção` (ex: `identity:users:write`, `promotion:promote`)

### 3.2 Papéis de Sistema

| Papel | Nº aprox. permissões |
|---|---|
| PlatformAdmin | 57+ |
| TechLead | 30+ |
| Developer | 20+ |
| Viewer | 15+ |
| Auditor | 10+ |
| SecurityReview | Subconjunto de segurança |
| ApprovalOnly | Subconjunto de aprovação |

### 3.3 Mecanismos

| Componente | Função | Evidência |
|---|---|---|
| `PermissionPolicyProvider` | Cria políticas com prefixo `Permission:` dinamicamente | `IdentityAccess.Infrastructure` |
| `PermissionAuthorizationHandler` | Extrai permissões de JWT claims, deny-by-default, logging WARNING em negação | `BuildingBlocks.Security` |
| `RolePermissionCatalog` | Mapeamento papel → permissões | `IdentityAccess.Domain` |
| `HttpContextCurrentUser` | Extrai Id/Name/Email/IsAuthenticated/HasPermission de ClaimsPrincipal | `BuildingBlocks.Security` |

### 3.4 Alinhamento Frontend

- 84+ chaves de permissão em `auth/permissions.ts`
- Hook `usePermissions` com `Set` para lookup O(1)
- Permissões server-driven via endpoint `/me`
- Comentário explícito: "O frontend NUNCA deve fazer enforcement de autorização"
- `ProtectedRoute` com redirect client-side, aguarda hydration do perfil

---

## 4. Isolamento de Tenant

### 4.1 Defesa em três camadas

```
JWT claim (tenant_id) → TenantResolutionMiddleware → PostgreSQL RLS (set_config)
```

### 4.2 Resolução de Tenant

Prioridade: JWT claim `tenant_id` → Header `X-Tenant-Id` → Query string → Subdomínio

### 4.3 RLS Enforcement

- `TenantRlsInterceptor` define `set_config('app.current_tenant_id', @param, false)` em TODOS os comandos EF Core (Reader, NonQuery, Scalar)
- SQL parametrizado (prevenção de injection)

### 4.4 Modelo de Dados

- `TenantMembership`: User-Tenant-Role com `JoinedAt`, `IsActive`
- `Tenant`: Id/Name/Slug(unique)/IsActive, soft-delete
- Seed: 2 tenants (NexTrace Corp, Acme Fintech)
- Configuração OIDC per-tenant

---

## 5. Controlo de Ambientes

### 5.1 Entidade Environment

| Campo | Tipo / Detalhe |
|---|---|
| TenantId | FK para Tenant |
| Name / Slug | Identificação |
| SortOrder | Ordenação |
| Profile | Development / Validation / Staging / Production / DisasterRecovery |
| Criticality | Low / Medium / High / Critical |
| IsProductionLike | Boolean |
| IsPrimaryProduction | Unique partial index por tenant activo |

### 5.2 EnvironmentAccess

- UserId + TenantId + EnvironmentId
- AccessLevel: ReadOnly / ReadWrite / Admin / None
- Campos temporais: GrantedAt / ExpiresAt / GrantedBy / RevokedAt

### 5.3 Middleware

- `EnvironmentResolutionMiddleware` (após `TenantResolutionMiddleware`)
- Resolve via header `X-Environment-Id` ou query string
- Valida que o ambiente pertence ao tenant activo

### 5.4 Seed

- 5 ambientes (3 para NexTrace Corp: dev/staging/prod, 2 para Acme: dev/prod)
- 8 registos de acesso com diferentes níveis

---

## 6. Acesso Avançado

### 6.1 Visão Geral

| Mecanismo | Entidade | Endpoints | Estado |
|---|---|---|---|
| JIT Access | `JitAccessRequest` | `JitAccessEndpoints` | IMPLEMENTED_APPARENT |
| Break Glass | `BreakGlassRequest` | `BreakGlassEndpoints` | IMPLEMENTED_APPARENT |
| Delegação | `Delegation` | `DelegationEndpoints` | IMPLEMENTED_APPARENT |
| Access Review | `AccessReviewCampaign` + `AccessReviewItem` | `AccessReviewEndpoints` | IMPLEMENTED_APPARENT |

### 6.2 JIT Access

- Status lifecycle: Pending → Approved/Rejected → GrantedFrom/GrantedUntil → Revoked
- Prazo de aprovação: 4 horas
- Janela de concessão: 8 horas
- Campos: RequestedBy, TenantId, PermissionCode, Scope, Justification

### 6.3 Break Glass

- Activação imediata (sem aprovação)
- Janela: 2 horas
- Post-mortem obrigatório em 24h
- Limite trimestral: 3 por utilizador
- Tracking: IpAddress, UserAgent
- Status: Active → Expired/Revoked → PostMortemCompleted

### 6.4 Delegação

- DelegatedBy / DelegatedTo / RoleId / TenantId
- Time-bounded: StartsAt / EndsAt
- Campo Reason obrigatório

### 6.5 Access Review

- Campaign: TenantId/Title/StartedAt/DeadlineAt/Status(Active/Completed/Cancelled)
- Items: UserId/RoleId/DecidedBy/Decision(Approved/Revoked/Pending)/Reason

---

## 7. Eventos de Segurança e Auditoria

### 7.1 SecurityEvent

| Campo | Detalhe |
|---|---|
| TenantId, UserId, SessionId | Contexto |
| EventType | 15+ tipos (ver abaixo) |
| Description | Texto descritivo |
| RiskScore | 0-100 |
| IpAddress, UserAgent | Origem |
| MetadataJson | Dados adicionais |
| OccurredAt | Timestamp |
| IsReviewed, ReviewedAt, ReviewedBy | Workflow de revisão |

### 7.2 Tipos de Evento

LoginFailed, LoginSuccess, PasswordChanged, AccessDenied, UnauthorizedAccess, BreakGlassActivated, BreakGlassRevoked, BreakGlassPostMortem, DelegationCreated, DelegationRevoked, DelegationUsed, SessionCreated, SessionRevoked, SessionRotated, AccountLocked, AccountDeactivated, AccountReactivated

### 7.3 Risk Scoring

| Cenário | Score |
|---|---|
| Localização incomum | 60 |
| Força bruta | 80 |
| Aprovação rápida | 45 |
| Fora de horário | 25 |

### 7.4 Bridge de Auditoria

```
ISecurityEventTracker → SecurityEventTracker (scoped) → SecurityEventAuditBehavior (MediatR) → ISecurityAuditBridge → Módulo AuditCompliance
```

### 7.5 Seed

8 eventos de segurança exemplificativos com diferentes perfis de risco.

---

## 8. Segurança do Frontend

### 8.1 Armazenamento de Tokens

| Token | Localização | Justificação |
|---|---|---|
| Refresh token | Closure in-memory | Seguro contra XSS |
| Access token | sessionStorage | Escopo por tab |
| Legacy tokens | Limpeza automática no carregamento | Migração segura |

### 8.2 Cliente API (Axios)

- Injecção automática: Bearer token, `X-Tenant-Id`, `X-Environment-Id`, `X-Csrf-Token` (apenas POST/PUT/PATCH/DELETE)
- Refresh silencioso em 401 com gestão de pedidos concorrentes

### 8.3 Contextos

- `AuthContext` / `AuthProvider`: Bootstrap via `/identity/auth/me`, gestão de tenant
- `EnvironmentProvider`: Carrega ambientes, auto-selecção, persistência em sessionStorage

### 8.4 Catálogo de Permissões

- 84+ strings de permissão distintas em `auth/permissions.ts`
- Comentário de segurança explícito sobre frontend não fazer enforcement

---

## 9. Análise de Lacunas

| # | Lacuna | Severidade | Estado |
|---|---|---|---|
| 1 | API Key armazenada em memória (appsettings) | MÉDIA | MVP1, migração planeada |
| 2 | MFA enforcement adiado (política modelada, step-up não implementado) | ALTA | Parcialmente implementado |
| 3 | Validação IP/UserAgent de sessão não implementada (dados recolhidos) | MÉDIA | Dados disponíveis |
| 4 | Enforcement de post-mortem Break Glass não automatizado | MÉDIA | Manual |
| 5 | CORS necessita verificação (não wildcard confirmado) | BAIXA | A verificar |
| 6 | JWT secret DEVE ser configurado externamente em produção | CRÍTICA (se falhar) | Validação de arranque existe |
| 7 | SAML NÃO implementado (apenas OIDC) | MÉDIA | Sem plano imediato |
| 8 | Restrições de permissão por ambiente parcialmente implementadas | MÉDIA | Interface EnvironmentAccessValidator existe |

---

## 10. Recomendações e Plano de Acção

### Prioridade ALTA (próximo sprint)

1. **Implementar MFA step-up** para operações privilegiadas — a política já existe, falta o enforcement
2. **Migrar API Keys para BD encriptada** — substituir armazenamento in-memory
3. **Validar configuração CORS** em todos os ambientes de deployment

### Prioridade MÉDIA (próximo trimestre)

4. **Automatizar enforcement de post-mortem Break Glass** — background job com notificações
5. **Implementar validação de anomalia de sessão** — usar IP/UserAgent já recolhidos
6. **Completar enforcement de EnvironmentAccessValidator** nos handlers de comando
7. **Adicionar job de limpeza de JIT expirados**

### Prioridade BAIXA (roadmap)

8. **Implementar SAML** para federação enterprise completa
9. **Surfacar workflow de revisão de SecurityEvent** na UI
10. **Implementar resposta automatizada** a regras de detecção de anomalia

### Pipeline de Middleware Verificado

```
1. UseResponseCompression
2. UseHttpsRedirection
3. UseCors
4. UseRateLimiter
5. UseSecurityHeaders
6. UseGlobalExceptionHandler
7. UseCookieSessionCsrfProtection
8. UseAuthentication
9. TenantResolutionMiddleware
10. EnvironmentResolutionMiddleware
11. UseAuthorization
```

A ordem está correcta: compressão → segurança de transporte → CORS → rate limiting → headers de segurança → tratamento de excepções → CSRF → autenticação → resolução de tenant → resolução de ambiente → autorização.

---

## Anexo: Endpoints Identificados

| Módulo de Endpoint | Ficheiro |
|---|---|
| AuthEndpoints | `IdentityAccess.API` |
| TenantEndpoints | `IdentityAccess.API` |
| UserEndpoints | `IdentityAccess.API` |
| EnvironmentEndpoints | `IdentityAccess.API` |
| RolePermissionEndpoints | `IdentityAccess.API` |
| JitAccessEndpoints | `IdentityAccess.API` |
| BreakGlassEndpoints | `IdentityAccess.API` |
| AccessReviewEndpoints | `IdentityAccess.API` |
| DelegationEndpoints | `IdentityAccess.API` |
| CookieSessionEndpoints | `IdentityAccess.API` |
| RuntimeContextEndpoints | `IdentityAccess.API` |

---

> **Próxima revisão agendada:** Após implementação das recomendações de prioridade ALTA.
