# PARTE 4 — Modelo de Domínio Final do Módulo Identity & Access

> Documento gerado em 2026-03-25 | Prompt N14 | Consolidação do módulo Identity & Access

---

## 1. Aggregate Roots

| Aggregate Root | Ficheiro | LOC | Subdomínio |
|---|---|---|---|
| User | `Domain/Entities/User.cs` | 135 | Core Identity |
| Tenant | `Domain/Entities/Tenant.cs` | 82 | Core Identity |
| Session | `Domain/Entities/Session.cs` | 78 | Core Identity |
| JitAccessRequest | `Domain/Entities/JitAccessRequest.cs` | 215 | Enterprise Access |
| BreakGlassRequest | `Domain/Entities/BreakGlassRequest.cs` | 175 | Enterprise Access |
| Delegation | `Domain/Entities/Delegation.cs` | 163 | Enterprise Access |
| AccessReviewCampaign | `Domain/Entities/AccessReviewCampaign.cs` | 149 | Compliance |

---

## 2. Entidades

| Entidade | Ficheiro | LOC | Aggregate |
|---|---|---|---|
| Role | `Domain/Entities/Role.cs` | 76 | User (ou standalone) |
| Permission | `Domain/Entities/Permission.cs` | 43 | Standalone (seed) |
| TenantMembership | `Domain/Entities/TenantMembership.cs` | 71 | Tenant / User |
| ExternalIdentity | `Domain/Entities/ExternalIdentity.cs` | 88 | User |
| SsoGroupMapping | `Domain/Entities/SsoGroupMapping.cs` | 93 | Tenant |
| SecurityEvent | `Domain/Entities/SecurityEvent.cs` | 132 | Standalone (append-only) |
| AccessReviewItem | `Domain/Entities/AccessReviewItem.cs` | 139 | AccessReviewCampaign |
| Environment ⚠️ | `Domain/Entities/Environment.cs` | 246 | A migrar para módulo 02 |
| EnvironmentAccess ⚠️ | `Domain/Entities/EnvironmentAccess.cs` | 173 | A migrar para módulo 02 |
| EnvironmentPolicy ⚠️ | `Domain/Entities/EnvironmentPolicy.cs` | 123 | A migrar (sem EF mapping) |
| EnvironmentTelemetryPolicy ⚠️ | `Domain/Entities/EnvironmentTelemetryPolicy.cs` | 121 | A migrar (sem EF mapping) |
| EnvironmentIntegrationBinding ⚠️ | `Domain/Entities/EnvironmentIntegrationBinding.cs` | 108 | A migrar (sem EF mapping) |

### Catálogos estáticos

| Catálogo | Ficheiro | LOC |
|---|---|---|
| RolePermissionCatalog | `Domain/Entities/RolePermissionCatalog.cs` | 261 |
| SecurityEventType | `Domain/Entities/SecurityEventType.cs` | 146 |

---

## 3. Value Objects

| Value Object | Ficheiro | Descrição |
|---|---|---|
| Email | `ValueObjects/Email.cs` | Endereço de email validado |
| FullName | `ValueObjects/FullName.cs` | Nome + apelido |
| HashedPassword | `ValueObjects/HashedPassword.cs` | Hash de password (PBKDF2) |
| RefreshTokenHash | `ValueObjects/RefreshTokenHash.cs` | Hash do refresh token |
| MfaPolicy | `ValueObjects/MfaPolicy.cs` | Política de MFA (SaaS/SelfHosted/OnPremise) |
| SessionPolicy | `ValueObjects/SessionPolicy.cs` | Política de sessão |
| AuthenticationPolicy | `ValueObjects/AuthenticationPolicy.cs` | Política de autenticação |
| AuthenticationMode | `ValueObjects/AuthenticationMode.cs` | Modo de autenticação (Local/OIDC/etc.) |
| DeploymentModel | `ValueObjects/DeploymentModel.cs` | Modelo de deployment (SaaS/OnPrem) |
| EnvironmentUiProfile ⚠️ | `ValueObjects/EnvironmentUiProfile.cs` | A migrar para módulo 02 |
| TenantEnvironmentContext | `ValueObjects/TenantEnvironmentContext.cs` | Contexto composto (TenantId + EnvironmentId) |

---

## 4. Enums persistidos

| Enum | Ficheiro | Valores | Migrar? |
|---|---|---|---|
| EnvironmentCriticality ⚠️ | `Enums/EnvironmentCriticality.cs` | Low, Medium, High, Critical | Sim → módulo 02 |
| EnvironmentProfile ⚠️ | `Enums/EnvironmentProfile.cs` | Development, QA, Staging, Production, DR, etc. | Sim → módulo 02 |

**Nota:** O módulo Identity não tem enums próprios explícitos além dos de Environment. Estados internos (como lockout, session status) são derivados de campos boolean/datetime.

---

## 5. Relações internas

```
User ──1:N──> TenantMembership ──N:1──> Tenant
User ──1:N──> Session
User ──1:N──> ExternalIdentity
User ──1:N──> Role (via assignment)
User ──1:N──> JitAccessRequest
User ──1:N──> BreakGlassRequest
User ──1:N──> Delegation (como delegant ou delegate)
Tenant ──1:N──> SsoGroupMapping
Tenant ──1:N──> Environment ⚠️ (a migrar)
AccessReviewCampaign ──1:N──> AccessReviewItem
Role ──N:N──> Permission (via RolePermissionCatalog, in-memory)
```

---

## 6. Relações com outros módulos

### Identity & Access → Environment Management (02)
- **TenantEnvironmentContext** usado como dimensão de autorização
- Environment entities actualmente em IdentityDbContext (a migrar)
- EnvironmentAccess controla acesso user→environment

### Identity & Access → Audit & Compliance (10)
- SecurityEvents publicados como integration events
- SecurityAuditBridge publica para sistema externo via outbox

### Identity & Access → AI & Knowledge (07)
- Permissões `ai:assistant:*`, `ai:governance:*`, `ai:ide:*` no RolePermissionCatalog
- Contexto de utilizador/tenant via JWT claims

---

## 7. Entidades anémicas

| Entidade | Diagnóstico | Acção |
|---|---|---|
| Permission | ⚠️ Apenas `Id`, `Code`, `Description`, `Module` — sem lógica | Aceitável: é seed data estática |
| Role | ⚠️ Apenas `Id`, `Name`, `Description` — sem lógica de negócio | Considerar adicionar `AssignPermission()` se roles ficarem dinâmicas |
| TenantMembership | ⚠️ Associação pura sem lógica | Aceitável: é tabela de junção |
| SsoGroupMapping | ⚠️ Mapeamento sem lógica complexa | Aceitável |

---

## 8. Regras de negócio fora do lugar

| Regra | Localização actual | Localização correcta |
|---|---|---|
| NonDelegablePermissions (licensing:write) | `CreateDelegation.cs` (handler) | Domain: `Delegation` ou `DelegationPolicy` VO |
| Lockout logic (5 attempts, 15 min) | `User.cs` (inline) | ✅ Correcto — está no aggregate |
| Session creation + token gen | `LoginSessionCreator.cs` (utility) | ✅ Aceitável — application service |
| RolePermissionCatalog (static) | `Domain/Entities/` | ✅ Aceitável — catálogo estático |

---

## 9. Campos ausentes

| Entidade | Campo ausente | Justificação |
|---|---|---|
| User | `MfaEnabled`, `MfaMethod`, `MfaSecret` | Necessário para MFA enforcement |
| User | `ApiKeyHash` ou entidade ApiKey separada | Necessário para API Key management |
| Session | `MfaVerifiedAt` | Rastrear quando MFA foi verificado na sessão |
| Todas as entidades críticas | `xmin` / `RowVersion` | Concurrency control |
| User | `LastPasswordChangedAt` | Controlo de rotação de password |
| Tenant | `Plan`, `MaxUsers`, `Features` | Licenciamento foi removido — confirmar se estes campos existem |

---

## 10. Campos indevidos (a remover/migrar)

| Entidade/Ficheiro | Campo/Referência | Acção |
|---|---|---|
| RolePermissionCatalog | 17 licensing permissions | Remover — módulo Licensing eliminado |
| PermissionConfiguration | licensing:read, licensing:write seed | Remover seed data |
| MfaPolicy | Referência a "licenciamento" | Reescrever para "operações administrativas" |
| CreateDelegation | `licensing:write` em NonDelegablePermissions | Remover da lista |
| Environment* entities | 5 entidades + 2 enums + 1 VO | Migrar para módulo 02 (OI-04) |

---

## 11. Modelo final do domínio (target)

### Entidades que ficam no módulo Identity & Access

| Entidade | Tipo | Prefixo tabela |
|---|---|---|
| User | AggregateRoot | iam_users |
| Tenant | AggregateRoot | iam_tenants |
| Session | AggregateRoot | iam_sessions |
| Role | Entity | iam_roles |
| Permission | Entity (seed) | iam_permissions |
| TenantMembership | Entity | iam_tenant_memberships |
| ExternalIdentity | Entity | iam_external_identities |
| SsoGroupMapping | Entity | iam_sso_group_mappings |
| BreakGlassRequest | AggregateRoot | iam_break_glass_requests |
| JitAccessRequest | AggregateRoot | iam_jit_access_requests |
| Delegation | AggregateRoot | iam_delegations |
| AccessReviewCampaign | AggregateRoot | iam_access_review_campaigns |
| AccessReviewItem | Entity | iam_access_review_items |
| SecurityEvent | Entity (append) | iam_security_events |
| **ApiKey** (novo) | Entity | iam_api_keys |

### Entidades a migrar para Environment Management (02)

| Entidade | Tabela target |
|---|---|
| Environment | env_environments |
| EnvironmentAccess | env_environment_accesses |
| EnvironmentPolicy | env_environment_policies |
| EnvironmentTelemetryPolicy | env_environment_telemetry_policies |
| EnvironmentIntegrationBinding | env_environment_integration_bindings |

### Value Objects finais

| VO | Mantém-se? |
|---|---|
| Email | ✅ |
| FullName | ✅ |
| HashedPassword | ✅ |
| RefreshTokenHash | ✅ |
| MfaPolicy | ✅ (limpar ref a licensing) |
| SessionPolicy | ✅ |
| AuthenticationPolicy | ✅ |
| AuthenticationMode | ✅ |
| DeploymentModel | ✅ |
| TenantEnvironmentContext | ✅ (referência a EnvironmentId mantém-se) |
| EnvironmentUiProfile | ❌ Migrar para módulo 02 |
