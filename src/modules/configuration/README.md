# NexTraceOne — Configuration Module

## Propósito

O módulo Configuration é o sistema centralizado de configuração hierárquica da plataforma NexTraceOne.
Gere definições de chaves, valores por âmbito, feature flags, herança hierárquica, encriptação, auditoria e concorrência.

## Bounded Context

O módulo é a **fonte de verdade** para:
- o schema e metadados de cada chave de configuração (`ConfigurationDefinition`)
- os valores concretos por âmbito (`ConfigurationEntry`)
- as definições de feature flags (`FeatureFlagDefinition`) e as suas sobreposições (`FeatureFlagEntry`)
- o agrupamento por módulo funcional (`ConfigurationModule`)
- o histórico imutável de alterações (`ConfigurationAuditEntry`)

## Estrutura de projetos

```
NexTraceOne.Configuration.Domain         → Entidades, enums, invariantes de domínio
NexTraceOne.Configuration.Application    → Features CQRS (Commands + Queries)
NexTraceOne.Configuration.Contracts      → DTOs e Integration Events
NexTraceOne.Configuration.Infrastructure → DbContext, Repositórios, Serviços, Seed
NexTraceOne.Configuration.API            → Endpoints Minimal API
```

## Modelo de domínio (estado após P3.1–P3.4)

| Entidade | Tabela | Papel |
|---|---|---|
| `ConfigurationDefinition` | `cfg_definitions` | Schema/metadados de uma chave de configuração |
| `ConfigurationEntry` | `cfg_entries` | Valor concreto por âmbito |
| `ConfigurationAuditEntry` | `cfg_audit_entries` | Histórico imutável de alterações |
| `ConfigurationModule` | `cfg_modules` | Agrupamento funcional das definições (P3.1) |
| `FeatureFlagDefinition` | `cfg_feature_flag_definitions` | Schema de uma feature flag (P3.1) |
| `FeatureFlagEntry` | `cfg_feature_flag_entries` | Sobreposição de valor de flag por âmbito (P3.2) |

Todas as tabelas usam prefixo `cfg_`.

## Hierarquia de âmbitos (Scope Inheritance)

A resolução de configurações percorre do âmbito mais específico para o mais genérico:

```
User → Team → Role → Environment → Tenant → System → DefaultValue
```

Se não existir valor no âmbito pedido, o sistema herda do âmbito superior.
Se nenhum valor for encontrado, usa o `DefaultValue` da definição.

**Dimensão Módulo (P3.1):** cada `ConfigurationDefinition` pode ser associada a um `ConfigurationModule`
(ex: `notifications`, `governance`, `ai`) via FK opcional `ModuleId`, permitindo navegação e filtragem por área funcional.

## Enums

| Enum | Valores |
|---|---|
| `ConfigurationScope` | System, Tenant, Environment, Role, Team, User |
| `ConfigurationCategory` | Bootstrap, SensitiveOperational, Functional |
| `ConfigurationValueType` | String, Integer, Decimal, Boolean, Json, StringList |

## Endpoints REST

### Configurações

| Método | Rota | Permissão | Descrição |
|---|---|---|---|
| GET | `/api/v1/configuration/definitions` | `configuration:read` | Lista definições |
| GET | `/api/v1/configuration/entries` | `configuration:read` | Valores por âmbito |
| GET | `/api/v1/configuration/effective` | `configuration:read` | Valores resolvidos com herança |
| PUT | `/api/v1/configuration/{key}` | `configuration:write` | Define ou atualiza valor |
| DELETE | `/api/v1/configuration/{key}/override` | `configuration:write` | Remove override de âmbito |
| POST | `/api/v1/configuration/{key}/toggle` | `configuration:write` | Ativa/desativa configuração |
| GET | `/api/v1/configuration/{key}/audit` | `configuration:read` | Histórico de auditoria |

### Feature Flags (P3.2)

| Método | Rota | Permissão | Descrição |
|---|---|---|---|
| GET | `/api/v1/configuration/flags` | `configuration:read` | Lista definições de feature flags |
| GET | `/api/v1/configuration/flags/effective` | `configuration:read` | Flag efetiva por âmbito |
| PUT | `/api/v1/configuration/flags/{key}/override` | `configuration:write` | Define sobreposição de flag |
| DELETE | `/api/v1/configuration/flags/{key}/override` | `configuration:write` | Remove sobreposição de flag |

## Features CQRS principais

| Feature | Tipo | Descrição |
|---|---|---|
| `GetDefinitions` | Query | Lista definições com filtros |
| `GetEntries` | Query | Valores por âmbito |
| `GetEffectiveSettings` | Query | Resolução hierárquica de valores |
| `SetConfigurationValue` | Command | Cria ou atualiza valor com auditoria |
| `ToggleConfiguration` | Command | Ativa/desativa entrada |
| `RemoveOverride` | Command | Remove override de âmbito |
| `GetAuditHistory` | Query | Histórico de alterações |
| `GetFeatureFlags` | Query | Lista feature flags (P3.2) |
| `GetEffectiveFeatureFlag` | Query | Resolução hierárquica de flags (P3.2) |
| `SetFeatureFlagOverride` | Command | Sobreposição de flag (P3.2) |
| `RemoveFeatureFlagOverride` | Command | Remove sobreposição de flag (P3.2) |

## ConfigurationDefinitionSeeder (P3.3)

O módulo inclui um seeder idempotente com **345 definições** organizadas em **8 fases**.

### Execução

Executa em **todos os ambientes** (Development, Staging, Production) durante o arranque da aplicação via `SeedConfigurationDefinitionsAsync()` em `WebApplicationExtensions`.

**Comportamento:**
- Primeira execução: insere todas as definições ausentes
- Re-execuções: ignora definições já existentes (não sobrescreve)
- Falha no seed é não-fatal: aplicação arranca com `LogWarning`

### Injeção via DI (P3.3)

```csharp
IConfigurationDefinitionSeeder seeder = ...;
SeedingResult result = await seeder.SeedAsync();
// result.Added, result.Skipped, result.Total, result.IsFirstRun, result.IsNoOp
```

### Fases do seeder

| Fase | Domínio | Prefixo das chaves |
|---|---|---|
| 1 | Foundation / Instance / Tenant / Environment | `instance.*`, `tenant.*`, `environment.*`, `platform.*`, `security.*`, `branding.*`, `feature.*`, `policy.*`, `integration.*` |
| 2 | Notifications | `notifications.*` |
| 3 | Workflow / Promoção | `workflow.*`, `promotion.*` |
| 4 | Governance | `governance.*` |
| 5 | Catalog / Contratos / Change | `catalog.*`, `change.*` |
| 6 | Operations / FinOps / Benchmarking | `incidents.*`, `operations.*`, `finops.*` |
| 7 | AI / Integrations | `ai.*`, `integrations.*` |
| 8 | Admin / UX | `admin.*` |

## Concorrência otimista (P3.4)

Todas as entidades mutáveis usam **PostgreSQL xmin** via `IsRowVersion()`:

```csharp
builder.Property(x => x.RowVersion).IsRowVersion();
```

O `NexTraceDbContextBase.SaveChangesAsync` traduz `DbUpdateConcurrencyException` → `ConcurrencyException` (BuildingBlocks.Application), isolando a Application layer de dependências EF Core.

Os handlers de escrita capturam `ConcurrencyException` e retornam:

| Handler | Código de conflito | HTTP |
|---|---|---|
| `SetConfigurationValue` | `CONFIG_CONCURRENCY_CONFLICT` | 409 |
| `ToggleConfiguration` | `CONFIG_CONCURRENCY_CONFLICT` | 409 |
| `RemoveOverride` | `CONFIG_CONCURRENCY_CONFLICT` | 409 |
| `SetFeatureFlagOverride` | `FEATURE_FLAG_CONCURRENCY_CONFLICT` | 409 |
| `RemoveFeatureFlagOverride` | `FEATURE_FLAG_CONCURRENCY_CONFLICT` | 409 |

O `RowVersion` é exposto nos DTOs `ConfigurationEntryDto` e `FeatureFlagEntryDto`.

## Segurança

- Valores sensíveis encriptados com AES-256-GCM em repouso (`IsEncrypted`, `IsSensitive`)
- Mascaramento automático de valores sensíveis em respostas API
- Permissões: `configuration:read`, `configuration:write`
- Auditoria completa e imutável de todas as alterações
- PostgreSQL RLS para multi-tenancy

## Base de dados

- **DbContext:** `ConfigurationDbContext`
- **Prefixo:** `cfg_`
- **Tabelas:** `cfg_definitions`, `cfg_entries`, `cfg_audit_entries`, `cfg_modules`, `cfg_feature_flag_definitions`, `cfg_feature_flag_entries`, `cfg_outbox_messages`
- **Concorrência:** xmin via `IsRowVersion()` em Definition, Entry, FeatureFlagEntry, FeatureFlagDefinition
- **Migration pendente:** P3.1+P3.2 (cfg_modules, cfg_feature_flag_definitions, cfg_feature_flag_entries, module_id)

## Testes

326 testes unitários cobrindo:
- Entidades de domínio (criação, atualização, invariantes)
- Seed data (completude, idempotência, `SeedingResult`)
- Concorrência (todos os 5 handlers → `ConcurrencyException` → 409)

## Limitações conhecidas

1. **Migration pendente (P1):** as tabelas de P3.1/P3.2 (`cfg_modules`, `cfg_feature_flag_definitions`, `cfg_feature_flag_entries`, coluna `module_id`) ainda aguardam `dotnet ef migrations add`
2. **FeatureFlagDefinitionSeeder:** não existe ainda — tabela `cfg_feature_flag_definitions` ficará vazia após migration
3. **Frontend 409:** o frontend não trata explicitamente HTTP 409 de concorrência
4. **Catálogo navegável das 345 definições:** documentação detalhada de cada definição fica para fase posterior

## Documentação relacionada

| Documento | Conteúdo |
|---|---|
| `docs/architecture/p3-1-*.md` | Expansão da hierarquia (ConfigurationModule, FeatureFlagDefinition) |
| `docs/architecture/p3-2-*.md` | Implementação de feature flags |
| `docs/architecture/p3-3-*.md` | Adaptação do seeder para produção |
| `docs/architecture/p3-4-*.md` | Controlo de concorrência |
| `docs/11-review-modular/09-configuration/` | Revisão modular detalhada |
| `docs/audits/CONFIGURATION-PHASE-*.md` | Auditorias das 8 fases do seeder |
| `docs/execution/CONFIGURATION-*.md` | Documentação de execução (fragmentada — ver README como fonte canónica) |
