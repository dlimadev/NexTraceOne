# E3 — Environment Management Module Execution Report

## Data de Execução
2026-03-25

## Resumo
Execução real de correções no módulo Environment Management conforme a trilha N.
Todas as alterações consolidam o bounded context, preparam a persistência para a
futura baseline com prefixo `env_`, e alinham segurança com namespace `env:*`.

---

## Ficheiros de Código Alterados

### Domain — Entidades
| Ficheiro | Alteração |
|----------|-----------|
| `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/Environment.cs` | Adicionados: RowVersion (uint xmin), IsDeleted, UpdatedAt, UpdatedBy, CreatedBy. Guard no Deactivate() para primary production. Novos métodos SoftDelete() e SetUpdated(). |
| `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/EnvironmentAccess.cs` | Adicionado RowVersion (uint xmin) para concorrência otimista. |
| `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/RolePermissionCatalog.cs` | Adicionadas permissões env:* a todos os 7 roles (PlatformAdmin: full, TechLead: read+write, Developer: read, Viewer: read, Auditor: read, SecurityReview: read, ApprovalOnly: none). |

### Persistence — EF Core Configurations
| Ficheiro | Alteração |
|----------|-----------|
| `EnvironmentConfiguration.cs` | Prefixo `identity_environments` → `env_environments`. IsRowVersion() para xmin. Novos mapeamentos IsDeleted, UpdatedAt, UpdatedBy, CreatedBy. Check constraints (Profile 1-9, Criticality 1-4, SortOrder ≥ 0). Filtered indexes (tenant_active, tenant_profile, tenant_criticality, not_deleted). Query filter soft-delete. Primary production unique filter atualizado com IsDeleted=false. |
| `EnvironmentAccessConfiguration.cs` | Prefixo `identity_environment_accesses` → `env_environment_accesses`. IsRowVersion() para xmin. Check constraint AccessLevel. FK EnvironmentAccess → Environment (Cascade). Unique filtered index (unique_active). Novos indexes (env_id, user_active). Tipos de coluna explícitos para timestamps. |

### Backend — Endpoints
| Ficheiro | Alteração |
|----------|-----------|
| `EnvironmentEndpoints.cs` | Migradas TODAS as permissões de `identity:users:*` para `env:*`. Escalação: list/get → `env:environments:read`, create/update → `env:environments:write`, set-primary-production → `env:environments:admin`, grant-access → `env:access:admin`. XML docs atualizados. |

### Frontend
| Ficheiro | Alteração |
|----------|-----------|
| `AppSidebar.tsx` | Adicionada entrada de navegação `/environments` com permissão `env:environments:read` na secção admin. |
| `en.json` | Adicionada chave `sidebar.environments`: "Environments". |
| `pt-PT.json` | Adicionada chave `sidebar.environments`: "Ambientes". |
| `pt-BR.json` | Adicionada chave `sidebar.environments`: "Ambientes". |
| `es.json` | Adicionada chave `sidebar.environments`: "Ambientes". |

### Documentação
| Ficheiro | Alteração |
|----------|-----------|
| `src/modules/identityaccess/README-environment-management.md` | **CRIADO** — README completo do módulo (arquitetura, entidades, endpoints, DB, permissões, frontend, consumidores). |

---

## Correções por Parte

### PART 1 — Bounded Context
- ✅ **Verificação**: Environment já tem código organizado em namespaces separados (Entities/Environment*, Enums/Environment*, ValueObjects/Environment*, Features/*/Environment*)
- ✅ **Verificação**: Environment referencia Tenant via TenantId (FK sem navigation property cross-context)
- ✅ Fronteira documentada no README
- ⏳ Extração física (OI-04) mantida como pendente

### PART 2 — Domínio
- ✅ RowVersion (uint) adicionado a Environment e EnvironmentAccess
- ✅ IsDeleted, UpdatedAt, UpdatedBy, CreatedBy adicionados a Environment
- ✅ Guard: Deactivate() impede desativação de primary production
- ✅ SoftDelete() com guard contra primary production
- ✅ SetUpdated() para registo de auditoria

### PART 3 — Persistência
- ✅ Prefixo identity_ → env_ em ambas as tabelas
- ✅ IsRowVersion() xmin em Environment e EnvironmentAccess
- ✅ Check constraints: Profile (1-9), Criticality (1-4), SortOrder ≥ 0, AccessLevel IN(...)
- ✅ FK: EnvironmentAccess → Environment (Cascade)
- ✅ Unique filtered index para active access por user+env+tenant
- ✅ Filtered indexes para active, profile, criticality, soft-delete
- ✅ Query filter para soft-delete global
- ✅ Tipos de coluna explícitos (timestamp with time zone)

### PART 4 — Backend
- ✅ Permissões migradas de identity:users:* para env:*
- ✅ Escalação: set-primary-production → env:environments:admin
- ✅ Escalação: grant-access → env:access:admin

### PART 5 — Frontend
- ✅ Entrada de sidebar adicionada para /environments
- ✅ i18n: "environments" adicionado a 4 locales (en, pt-PT, pt-BR, es)

### PART 6 — Segurança
- ✅ Namespace env:* criado e implementado
- ✅ 5 permissões distribuídas por 7 roles no RolePermissionCatalog
- ✅ PlatformAdmin: acesso total (read, write, admin, access:read, access:admin)
- ✅ TechLead: read + write + access:read
- ✅ Developer: read + access:read
- ✅ Viewer: read only
- ✅ Auditor: read + access:read
- ✅ SecurityReview: read + access:read
- ✅ ApprovalOnly: sem acesso a ambiente

### PART 7 — Dependências
- ✅ Documentado no README quais módulos consomem Environment
- ⏳ Integrações detalhadas com consumidores → fases futuras

### PART 8 — Documentação
- ✅ README-environment-management.md criado com conteúdo completo

---

## Validação

- ✅ Build completo da solução: 0 erros
- ✅ 290 testes Identity: todos passam
- ✅ Sem migrations antigas removidas
- ✅ Sem nova baseline gerada

---

## Classes Alteradas

| Classe | Tipo de Alteração |
|--------|-------------------|
| `Environment` | Novos campos RowVersion, IsDeleted, UpdatedAt, UpdatedBy, CreatedBy. Guard Deactivate. SoftDelete. SetUpdated. |
| `EnvironmentAccess` | Novo campo RowVersion |
| `RolePermissionCatalog` | 5 novas permissões env:* em 6 roles |
| `EnvironmentConfiguration` | env_ prefix, xmin, check constraints, filtered indexes, query filter, novos mapeamentos |
| `EnvironmentAccessConfiguration` | env_ prefix, xmin, check constraint, FK, filtered indexes, explicit types |
| `EnvironmentEndpoints` | Permissões migradas identity:users:* → env:*, escalação admin |
