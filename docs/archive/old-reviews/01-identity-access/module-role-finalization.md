# PARTE 1 — Papel Final do Módulo Identity & Access

> Documento gerado em 2026-03-25 | Prompt N14 | Consolidação do módulo Identity & Access

---

## 1. Papel do módulo no NexTraceOne

O módulo **Identity & Access** é o módulo **fundacional** do NexTraceOne. Ele é o único módulo sem dependências externas e do qual todos os outros 12 módulos dependem. O seu papel é:

- Ser a **fonte única de verdade** para autenticação, autorização, tenant e sessão
- Fornecer o **contexto de segurança** (utilizador, tenant, permissões, ambiente) para todos os módulos do produto
- Implementar o modelo de **RBAC com 73+ permissões granulares** e deny-by-default
- Sustentar **multi-tenancy com isolamento RLS** na camada de persistência
- Registar **eventos de segurança** para auditoria e rastreabilidade

---

## 2. Confirmação de ownership

| Responsabilidade | Dono? | Estado | Notas |
|---|---|---|---|
| Autenticação (local, OIDC, SAML) | ✅ SIM | Implementado | LocalLogin, FederatedLogin, StartOidcLogin, OidcCallback |
| Sessão (JWT, cookie, refresh token) | ✅ SIM | Implementado | Session entity, JwtTokenGenerator, CookieSession endpoints |
| Autorização (RBAC, permissões) | ✅ SIM | Implementado | RolePermissionCatalog (73+ perms), 7 roles |
| Roles | ✅ SIM | Implementado | Role entity, ListRoles, AssignRole |
| Permissions | ✅ SIM | Implementado | Permission entity, PermissionConfiguration seed |
| Policies (MFA, sessão, autenticação) | ✅ SIM | Implementado | MfaPolicy, SessionPolicy, AuthenticationPolicy VOs |
| Tenant | ✅ SIM | Implementado | Tenant entity, TenantMembership, SelectTenant, ListMyTenants |
| Enforcement por ambiente | ⚠️ PARCIAL | Presente mas acoplado | Environment + EnvironmentAccess em IdentityDbContext — deve migrar para módulo Environment Management |
| Capabilities de IA | ⚠️ PARCIAL | Definido em catálogo | Permissões `ai:assistant:*`, `ai:governance:*`, `ai:ide:*` existem no RolePermissionCatalog |
| Histórico de interações de segurança | ✅ SIM | Implementado | SecurityEvent entity, SecurityAuditRecorder, SecurityEventType catalog |
| Break Glass (acesso de emergência) | ✅ SIM | Implementado | BreakGlassRequest, 3 endpoints |
| JIT Access (acesso temporário) | ✅ SIM | Implementado | JitAccessRequest, 3 endpoints |
| Delegation (delegação de permissões) | ✅ SIM | Implementado | Delegation entity, 3 endpoints, NonDelegablePermissions |
| Access Review (campanhas de revisão) | ✅ SIM | Implementado | AccessReviewCampaign + AccessReviewItem, 4 endpoints |
| SSO Group Mapping | ✅ SIM | Implementado | SsoGroupMapping entity |
| External Identity (OIDC/SCIM) | ✅ SIM | Implementado | ExternalIdentity entity |

---

## 3. O que o módulo NÃO deve ser dono

| Responsabilidade | Módulo correto | Justificação |
|---|---|---|
| Ciclo de vida de ambientes (criação, políticas, perfis) | Environment Management (02) | Bounded context próprio; actualmente acoplado (OI-04) |
| Políticas de telemetria por ambiente | Environment Management (02) | EnvironmentTelemetryPolicy deve migrar |
| Bindings de integração por ambiente | Environment Management (02) | EnvironmentIntegrationBinding deve migrar |
| Licenciamento do produto | ❌ Removido do escopo | Licensing foi eliminado; resíduos devem ser limpos |
| Trilha de auditoria global | Audit & Compliance (10) | Identity regista SecurityEvents locais; auditoria global é do módulo 10 |
| Gestão de configurações | Configuration (09) | Configurações de produto vivem no módulo 09 |
| Notificações de segurança | Notifications (11) | Identity publica eventos; Notifications entrega |

---

## 4. Dependências principais do módulo

### 4.1 Dependências de entrada (o que o módulo consome)

| Módulo | O que consome | Estado |
|---|---|---|
| Nenhum | Identity é fundacional | ✅ Correto |

### 4.2 Dependências de saída (o que o módulo expõe)

| Módulo consumidor | O que consome | Mecanismo |
|---|---|---|
| Todos (12 módulos) | Contexto de utilizador, tenant, permissões | JWT claims + middleware |
| Audit & Compliance | SecurityEvents, ações de acesso | Integration events via outbox |
| Environment Management | Entidades Environment (acoplamento atual) | Directo em IdentityDbContext |
| AI & Knowledge | Permissões ai:*, contexto de utilizador | Claims + RolePermissionCatalog |
| Notifications | Eventos de segurança (break glass, JIT) | Integration events |
| Governance | Permissões governance:*, tenant context | Claims |

### 4.3 Contratos publicados

- `NexTraceOne.IdentityAccess.Contracts` — DTOs e integration events
- JWT token structure (claims: userId, tenantId, permissions[], role)
- `IOperationalExecutionContext` — contexto de execução disponível para todos os módulos

---

## 5. Relação com Environment Management

### Estado actual

| Entidade | Localização actual | Localização alvo |
|---|---|---|
| Environment | IdentityDbContext (identity_environments) | EnvironmentDbContext (env_environments) |
| EnvironmentAccess | IdentityDbContext (identity_environment_accesses) | EnvironmentDbContext (env_environment_accesses) |
| EnvironmentPolicy | Domain sem EF mapping | EnvironmentDbContext |
| EnvironmentTelemetryPolicy | Domain sem EF mapping | EnvironmentDbContext |
| EnvironmentIntegrationBinding | Domain sem EF mapping | EnvironmentDbContext |
| EnvironmentCriticality enum | Identity Domain | Environment Management Domain |
| EnvironmentProfile enum | Identity Domain | Environment Management Domain |

### Decisão arquitectural (OI-04)

O Environment Management é um bounded context próprio e deve ser extraído para `src/modules/environmentmanagement/`. **Até à extracção**, Identity mantém as entidades de ambiente mas deve:
- Não expandir funcionalidades de ambiente
- Preparar a interface de dependência (Identity consome EnvironmentId como dimensão de autorização)
- Manter os 6 endpoints de ambiente funcionais durante a transição

---

## 6. Maturidade do módulo

| Dimensão | Maturidade | Notas |
|---|---|---|
| Backend (CQRS/DDD) | 🟢 95% | 42 features, 12 repositórios, 7 serviços infra |
| Frontend (páginas/UX) | 🟢 90% | 15 páginas, 2746 LOC, 39 API calls |
| Persistência | 🟡 80% | 16 DbSets, 16 configs, prefixo `identity_` (target: `iam_`) |
| Segurança/Enforcement | 🟡 75% | 73+ perms, deny-by-default, mas MFA não enforced |
| Documentação | 🟠 60% | Docs multicamada existem mas incompletos |
| **Global** | **🟢 82%** | Módulo mais maduro do produto |

---

## 7. Conclusão

O módulo Identity & Access confirma o seu papel como **base fundacional de segurança** do NexTraceOne. As lacunas principais são:
1. **Extracção de Environment Management** — acoplamento estrutural que bloqueia independência do módulo 02
2. **MFA enforcement** — modelado mas não enforced no runtime
3. **Resíduos de Licensing** — 17+ referências de um módulo removido
4. **Prefixo de tabelas** — migrar de `identity_` para `iam_`
