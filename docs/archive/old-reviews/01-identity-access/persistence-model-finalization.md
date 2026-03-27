# PARTE 5 — Modelo de Persistência Final (PostgreSQL) do Módulo Identity & Access

> Documento gerado em 2026-03-25 | Prompt N14 | Consolidação do módulo Identity & Access

---

## 1. Tabelas actuais do módulo

| # | Tabela actual | Entidade | EF Config |
|---|---|---|---|
| 1 | identity_tenants | Tenant | TenantConfiguration.cs |
| 2 | identity_users | User | UserConfiguration.cs |
| 3 | identity_roles | Role | RoleConfiguration.cs |
| 4 | identity_permissions | Permission | PermissionConfiguration.cs |
| 5 | identity_sessions | Session | SessionConfiguration.cs |
| 6 | identity_tenant_memberships | TenantMembership | TenantMembershipConfiguration.cs |
| 7 | identity_external_identities | ExternalIdentity | ExternalIdentityConfiguration.cs |
| 8 | identity_sso_group_mappings | SsoGroupMapping | SsoGroupMappingConfiguration.cs |
| 9 | identity_break_glass_requests | BreakGlassRequest | BreakGlassRequestConfiguration.cs |
| 10 | identity_jit_access_requests | JitAccessRequest | JitAccessRequestConfiguration.cs |
| 11 | identity_delegations | Delegation | DelegationConfiguration.cs |
| 12 | identity_access_review_campaigns | AccessReviewCampaign | AccessReviewCampaignConfiguration.cs |
| 13 | identity_access_review_items | AccessReviewItem | AccessReviewItemConfiguration.cs |
| 14 | identity_security_events | SecurityEvent | SecurityEventConfiguration.cs |
| 15 | identity_environments ⚠️ | Environment | EnvironmentConfiguration.cs |
| 16 | identity_environment_accesses ⚠️ | EnvironmentAccess | EnvironmentAccessConfiguration.cs |
| 17 | identity_outbox_messages | OutboxMessage | NexTraceDbContextBase |

---

## 2. Mapeamento entidade → tabela final (prefixo `iam_`)

| Entidade | Tabela actual | Tabela final | Acção |
|---|---|---|---|
| Tenant | identity_tenants | **iam_tenants** | Renomear |
| User | identity_users | **iam_users** | Renomear |
| Role | identity_roles | **iam_roles** | Renomear |
| Permission | identity_permissions | **iam_permissions** | Renomear |
| Session | identity_sessions | **iam_sessions** | Renomear |
| TenantMembership | identity_tenant_memberships | **iam_tenant_memberships** | Renomear |
| ExternalIdentity | identity_external_identities | **iam_external_identities** | Renomear |
| SsoGroupMapping | identity_sso_group_mappings | **iam_sso_group_mappings** | Renomear |
| BreakGlassRequest | identity_break_glass_requests | **iam_break_glass_requests** | Renomear |
| JitAccessRequest | identity_jit_access_requests | **iam_jit_access_requests** | Renomear |
| Delegation | identity_delegations | **iam_delegations** | Renomear |
| AccessReviewCampaign | identity_access_review_campaigns | **iam_access_review_campaigns** | Renomear |
| AccessReviewItem | identity_access_review_items | **iam_access_review_items** | Renomear |
| SecurityEvent | identity_security_events | **iam_security_events** | Renomear |
| **ApiKey (novo)** | — | **iam_api_keys** | Criar |
| OutboxMessage | identity_outbox_messages | **iam_outbox_messages** | Renomear |
| Environment ⚠️ | identity_environments | **env_environments** | Migrar para módulo 02 |
| EnvironmentAccess ⚠️ | identity_environment_accesses | **env_environment_accesses** | Migrar para módulo 02 |

---

## 3. PKs

| Tabela | PK | Tipo |
|---|---|---|
| iam_tenants | Id (Guid, strongly-typed TenantId) | UUID |
| iam_users | Id (Guid, strongly-typed UserId) | UUID |
| iam_roles | Id (Guid, strongly-typed RoleId) | UUID |
| iam_permissions | Id (Guid, strongly-typed PermissionId) | UUID |
| iam_sessions | Id (Guid, strongly-typed SessionId) | UUID |
| iam_tenant_memberships | Id (Guid) | UUID |
| iam_external_identities | Id (Guid) | UUID |
| iam_sso_group_mappings | Id (Guid) | UUID |
| iam_break_glass_requests | Id (Guid) | UUID |
| iam_jit_access_requests | Id (Guid) | UUID |
| iam_delegations | Id (Guid) | UUID |
| iam_access_review_campaigns | Id (Guid) | UUID |
| iam_access_review_items | Id (Guid) | UUID |
| iam_security_events | Id (Guid) | UUID |
| iam_api_keys | Id (Guid) | UUID |

---

## 4. FKs

| Tabela | FK | Referência |
|---|---|---|
| iam_users | TenantId | iam_tenants(Id) |
| iam_sessions | UserId | iam_users(Id) |
| iam_sessions | TenantId | iam_tenants(Id) |
| iam_tenant_memberships | UserId | iam_users(Id) |
| iam_tenant_memberships | TenantId | iam_tenants(Id) |
| iam_tenant_memberships | RoleId | iam_roles(Id) |
| iam_external_identities | UserId | iam_users(Id) |
| iam_sso_group_mappings | TenantId | iam_tenants(Id) |
| iam_break_glass_requests | RequestedByUserId | iam_users(Id) |
| iam_jit_access_requests | RequestedByUserId | iam_users(Id) |
| iam_delegations | DelegantUserId | iam_users(Id) |
| iam_delegations | DelegateUserId | iam_users(Id) |
| iam_access_review_campaigns | CreatedByUserId | iam_users(Id) |
| iam_access_review_campaigns | TenantId | iam_tenants(Id) |
| iam_access_review_items | CampaignId | iam_access_review_campaigns(Id) |
| iam_access_review_items | UserId | iam_users(Id) |
| iam_security_events | UserId | iam_users(Id) (nullable) |
| iam_security_events | TenantId | iam_tenants(Id) (nullable) |
| iam_api_keys (novo) | UserId | iam_users(Id) |
| iam_api_keys (novo) | TenantId | iam_tenants(Id) |

---

## 5. Índices recomendados

| Tabela | Índice | Colunas | Tipo |
|---|---|---|---|
| iam_users | IX_users_email | Email | UNIQUE |
| iam_users | IX_users_tenant_active | TenantId, IsActive | — |
| iam_sessions | IX_sessions_user | UserId | — |
| iam_sessions | IX_sessions_expires | ExpiresAt | — (para cleanup) |
| iam_tenant_memberships | IX_tm_user_tenant | UserId, TenantId | UNIQUE |
| iam_external_identities | IX_ext_provider_externalid | Provider, ExternalId | UNIQUE |
| iam_sso_group_mappings | IX_sso_tenant_group | TenantId, ExternalGroupId | UNIQUE |
| iam_permissions | IX_permissions_code | Code | UNIQUE |
| iam_security_events | IX_se_timestamp | CreatedAt | — (para queries temporais) |
| iam_security_events | IX_se_user | UserId | — |
| iam_break_glass_requests | IX_bg_user_status | RequestedByUserId, Status | — |
| iam_jit_access_requests | IX_jit_user_status | RequestedByUserId, Status | — |
| iam_delegations | IX_del_delegant | DelegantUserId | — |
| iam_delegations | IX_del_delegate | DelegateUserId | — |
| iam_access_review_items | IX_ari_campaign | CampaignId | — |
| iam_api_keys (novo) | IX_ak_hash | KeyHash | UNIQUE |
| iam_api_keys (novo) | IX_ak_user | UserId | — |

---

## 6. Constraints de unicidade

| Tabela | Constraint | Colunas |
|---|---|---|
| iam_users | UQ_users_email | Email |
| iam_tenant_memberships | UQ_tm_user_tenant | UserId + TenantId |
| iam_external_identities | UQ_ext_provider_id | Provider + ExternalId |
| iam_sso_group_mappings | UQ_sso_tenant_group | TenantId + ExternalGroupId |
| iam_permissions | UQ_permissions_code | Code |
| iam_api_keys | UQ_ak_hash | KeyHash |

---

## 7. Colunas de auditoria

Todas as tabelas devem ter (via NexTraceDbContextBase):

| Coluna | Tipo | Descrição |
|---|---|---|
| CreatedAt | timestamptz | Data de criação |
| CreatedBy | text | UserId do criador |
| ModifiedAt | timestamptz? | Data de última modificação |
| ModifiedBy | text? | UserId do modificador |
| IsDeleted | bool | Soft delete flag |

---

## 8. TenantId

| Tabela | Tem TenantId? | RLS? |
|---|---|---|
| iam_tenants | ❌ (é o próprio tenant) | ❌ |
| iam_users | ✅ | ✅ |
| iam_sessions | ✅ | ✅ |
| iam_tenant_memberships | ✅ | ✅ |
| iam_external_identities | Via UserId→User.TenantId | Indirecto |
| iam_sso_group_mappings | ✅ | ✅ |
| iam_break_glass_requests | ✅ (via contexto) | ✅ |
| iam_jit_access_requests | ✅ (via contexto) | ✅ |
| iam_delegations | ✅ (via contexto) | ✅ |
| iam_access_review_campaigns | ✅ | ✅ |
| iam_security_events | ✅ (nullable) | ✅ |
| iam_api_keys | ✅ | ✅ |

---

## 9. RowVersion (concurrency)

| Entidade | RowVersion actual? | Necessário? |
|---|---|---|
| User | ❌ | 🔴 SIM — updates concorrentes |
| Tenant | ❌ | 🟠 SIM |
| Role | ❌ | 🟡 Opcional (seed-like) |
| Session | ❌ | 🟡 Opcional |
| Delegation | ❌ | 🟠 SIM — revogação concorrente |
| BreakGlassRequest | ❌ | 🟠 SIM |
| JitAccessRequest | ❌ | 🟠 SIM |
| AccessReviewCampaign | ❌ | 🟠 SIM |

**Recomendação:** Adicionar `xmin` como concurrency token via `UseXminAsConcurrencyToken()` em todas as entidades mutáveis.

---

## 10. Divergências entre estado actual e modelo final

| # | Divergência | Impacto | Acção |
|---|---|---|---|
| D-01 | Prefixo `identity_` → `iam_` | Todas as tabelas | Migration reset futuro |
| D-02 | 2 tabelas de Environment em IdentityDbContext | Bloqueio do módulo 02 | Extracção futura |
| D-03 | Sem tabela iam_api_keys | API Key management ausente | Criar entidade + config |
| D-04 | Sem RowVersion/xmin | Concurrency issues possíveis | Adicionar em todas as configs |
| D-05 | Licensing permissions no seed | Dados inválidos persistidos | Remover na próxima migration |
| D-06 | 3 entidades de Environment sem EF mapping | EnvironmentPolicy, TelemetryPolicy, IntegrationBinding | Mapear no módulo 02 |
| D-07 | Outbox table como `identity_outbox_messages` | Deve ser `iam_outbox_messages` | Migration reset futuro |
