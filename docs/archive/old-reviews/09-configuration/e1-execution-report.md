# E1 — Configuration Module Execution Report

## Data de Execução
2026-03-25

## Resumo
Execução real de correções no módulo Configuration conforme a trilha N.
Todas as alterações aproximam o módulo do desenho final definido nos artefactos da trilha N.

---

## Ficheiros Alterados

### Domain
| Ficheiro | Alteração |
|----------|-----------|
| `src/modules/configuration/NexTraceOne.Configuration.Domain/Entities/ConfigurationDefinition.cs` | Adicionados campos `RowVersion` (uint, concorrência otimista), `IsDeprecated` (bool), `DeprecatedMessage` (string?). Factory `Create()` e método `Update()` atualizados com novos parâmetros e guards. |
| `src/modules/configuration/NexTraceOne.Configuration.Domain/Entities/ConfigurationEntry.cs` | Adicionado campo `RowVersion` (uint, concorrência otimista). |

### Persistence — EF Core Mappings
| Ficheiro | Alteração |
|----------|-----------|
| `src/modules/configuration/NexTraceOne.Configuration.Infrastructure/Persistence/Configurations/ConfigurationDefinitionConfiguration.cs` | Adicionados: `IsRowVersion()` para concorrência otimista xmin, check constraints para `category` e `value_type`, mapeamento de `IsDeprecated` e `DeprecatedMessage`, índice em `SortOrder`. |
| `src/modules/configuration/NexTraceOne.Configuration.Infrastructure/Persistence/Configurations/ConfigurationEntryConfiguration.cs` | Adicionados: `IsRowVersion()` para concorrência otimista xmin, check constraints para `scope` e `version >= 1`, FK explícita para `ConfigurationDefinition` com `DeleteBehavior.Restrict`, índices em `DefinitionId` e `IsActive` (filtrado). |
| `src/modules/configuration/NexTraceOne.Configuration.Infrastructure/Persistence/Configurations/ConfigurationAuditEntryConfiguration.cs` | Adicionados: FK explícita para `ConfigurationEntry` com `DeleteBehavior.Restrict`, índice em `ChangedBy`. XML doc atualizado. |

### Backend — Features/Handlers
| Ficheiro | Alteração |
|----------|-----------|
| `src/modules/configuration/NexTraceOne.Configuration.Application/Features/SetConfigurationValue/SetConfigurationValue.cs` | Adicionada validação de tipo de valor (`ValidateValueType`): Boolean→true/false, Integer→parseable, Decimal→parseable, Json→válido. Adicionada verificação de `IsDeprecated` que rejeita escritas em definições obsoletas. |
| `src/modules/configuration/NexTraceOne.Configuration.Application/Features/GetDefinitions/GetDefinitions.cs` | DTO de saída agora inclui `IsDeprecated` e `DeprecatedMessage`. |

### Contracts/DTOs
| Ficheiro | Alteração |
|----------|-----------|
| `src/modules/configuration/NexTraceOne.Configuration.Contracts/DTOs/ConfigurationDefinitionDto.cs` | Adicionados campos `IsDeprecated` e `DeprecatedMessage`. |

### Seeds e Startup
| Ficheiro | Alteração |
|----------|-----------|
| `src/platform/NexTraceOne.ApiHost/WebApplicationExtensions.cs` | `ConfigurationDbContext` adicionado à lista de migrações. Adicionado método `SeedConfigurationDefinitionsAsync()` que executa o seeder idempotente em **todos os ambientes** (não apenas Development). |
| `src/platform/NexTraceOne.ApiHost/Program.cs` | Chamada a `SeedConfigurationDefinitionsAsync()` adicionada entre migrações e seed de desenvolvimento. |

### Frontend
| Ficheiro | Alteração |
|----------|-----------|
| `src/frontend/src/features/configuration/types/index.ts` | `ConfigurationDefinitionDto` atualizado com campos `isDeprecated` e `deprecatedMessage`. |

### Documentação
| Ficheiro | Alteração |
|----------|-----------|
| `src/modules/configuration/README.md` | **CRIADO** — README completo do módulo com arquitetura, entidades, endpoints, segurança, seeds, DB schema e convenções. |

---

## Correções por Parte

### PART 1 — Domínio
- ✅ `RowVersion` (uint) adicionado a `ConfigurationDefinition` e `ConfigurationEntry`
- ✅ `IsDeprecated` e `DeprecatedMessage` adicionados a `ConfigurationDefinition`
- ✅ Guards de validação para `DeprecatedMessage` (máx. 500 caracteres)
- ✅ Factory `Create()` e método `Update()` atualizados

### PART 2 — Persistência
- ✅ `IsRowVersion()` configurado em Definition e Entry (mapeia para PostgreSQL xmin)
- ✅ FK: cfg_entries.definition_id → cfg_definitions.id (Restrict)
- ✅ FK: cfg_audit_entries.entry_id → cfg_entries.id (Restrict)
- ✅ Check constraint: `CK_cfg_definitions_category` (Bootstrap, SensitiveOperational, Functional)
- ✅ Check constraint: `CK_cfg_definitions_value_type` (String, Integer, Decimal, Boolean, Json, StringList)
- ✅ Check constraint: `CK_cfg_entries_scope` (System, Tenant, Environment, Role, Team, User)
- ✅ Check constraint: `CK_cfg_entries_version_positive` (version >= 1)
- ✅ Índice: cfg_definitions.sort_order
- ✅ Índice: cfg_entries.definition_id
- ✅ Índice filtrado: cfg_entries.is_active (WHERE is_active = true)
- ✅ Índice: cfg_audit_entries.changed_by
- ✅ Mapeamento de IsDeprecated (default false) e DeprecatedMessage (max 500)
- ✅ ConfigurationDbContext adicionado à lista de migrações do ApiHost
- ✅ Ausência confirmada de EnsureCreated em todo o codebase

### PART 3 — Seeds
- ✅ `SeedConfigurationDefinitionsAsync()` criado em WebApplicationExtensions
- ✅ Seeder executa em TODOS os ambientes (não apenas Development)
- ✅ Seeder é idempotente (verifica chaves existentes antes de inserir)
- ✅ Chamada integrada no Program.cs (após migrações, antes de seed de dev)
- ✅ Tratamento seguro de falhas (catch com LogWarning se schema ainda não existe)

### PART 4 — Backend
- ✅ Validação de tipo de valor em SetConfigurationValue:
  - Boolean: aceita apenas "true"/"false" (case-insensitive)
  - Integer: valida com `long.TryParse`
  - Decimal: valida com `decimal.TryParse`
  - Json: valida com `JsonDocument.Parse`
  - String e StringList: sem restrição adicional
- ✅ Rejeição de escritas em definições marcadas como `IsDeprecated`
- ✅ Código de erro específico: `CONFIG_DEPRECATED`, `CONFIG_VALUE_TYPE_INVALID`

### PART 5 — Frontend
- ✅ `ConfigurationDefinitionDto` TypeScript atualizado com `isDeprecated` e `deprecatedMessage`
- ✅ Alinhamento de tipos com backend

### PART 6 — Segurança
- ✅ Verificação: todos os endpoints têm `RequirePermission` (configuration:read ou configuration:write)
- ✅ Valores sensíveis continuam encriptados (AES-256-GCM) e mascarados
- ✅ Auditoria completa de todas as ações de escrita
- ✅ Verificação de IsEditable no handler antes de permitir escrita
- ✅ Nova verificação de IsDeprecated reforça governança

### PART 7 — Documentação
- ✅ `src/modules/configuration/README.md` criado com conteúdo completo
- ✅ XML docs existentes nos serviços já são adequados (ResolutionService, CacheService, SecurityService)

---

## Validação

- ✅ Build completo da solução: 0 erros
- ✅ 251 testes do módulo Configuration: todos passam
- ✅ Sem EnsureCreated em todo o codebase
- ✅ Sem migrations antigas removidas
- ✅ Sem nova baseline gerada

---

## Classes Alteradas

| Classe | Tipo de Alteração |
|--------|-------------------|
| `ConfigurationDefinition` | Novos campos (RowVersion, IsDeprecated, DeprecatedMessage) |
| `ConfigurationEntry` | Novo campo (RowVersion) |
| `ConfigurationDefinitionConfiguration` | xmin, check constraints, indexes, deprecated mapping |
| `ConfigurationEntryConfiguration` | xmin, FK, check constraints, indexes |
| `ConfigurationAuditEntryConfiguration` | FK, index |
| `SetConfigurationValue.Handler` | Value type validation, deprecated check |
| `GetDefinitions.Handler` | DTO mapping com novos campos |
| `ConfigurationDefinitionDto` | Novos campos |
| `WebApplicationExtensions` | ConfigurationDbContext em migrações, SeedConfigurationDefinitionsAsync |
