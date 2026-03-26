# P3.1 — Configuration Hierarchy Expansion Report

**Data de execução:** 2026-03-26  
**Fase:** P3.1 — Expandir o ConfigurationDbContext para hierarquia tenant/environment/module  
**Módulo:** Configuration  
**Estado:** CONCLUÍDO

---

## 1. Objetivo da fase

Expandir o modelo de dados do módulo Configuration para que a hierarquia
**Instance → Tenant → Environment → Module** fique explicitamente representada
na persistência, tornando o `ConfigurationDbContext` pronto para parametrização
enterprise e preparando o terreno para feature flags e schema validation nas
fases seguintes.

---

## 2. Estado anterior do modelo

O módulo possuía **3 entidades** na persistência:

| Entidade | Tabela | Propósito |
|---|---|---|
| `ConfigurationDefinition` | `cfg_definitions` | Metadados e schema de uma configuração |
| `ConfigurationEntry` | `cfg_entries` | Valor concreto por âmbito |
| `ConfigurationAuditEntry` | `cfg_audit_entries` | Registo imutável de auditoria |

**Lacunas identificadas antes desta fase:**

- A dimensão **Module** da hierarquia estava apenas implícita no prefixo da chave (ex: `"notifications.*"`) — sem entidade persistida.
- Não existia entidade explícita para **feature flags** no modelo.
- O `ConfigurationDbContext` expunha apenas os 3 DbSets originais.
- A propriedade `ConfigurationDefinition.ModuleId` não existia — não havia FK formal para o módulo.

---

## 3. Ficheiros alterados

### 3.1 Novas entidades (Domain)

| Ficheiro | Descrição |
|---|---|
| `src/modules/configuration/NexTraceOne.Configuration.Domain/Entities/ConfigurationModule.cs` | Nova entidade `ConfigurationModule` + `ConfigurationModuleId` |
| `src/modules/configuration/NexTraceOne.Configuration.Domain/Entities/FeatureFlagDefinition.cs` | Nova entidade `FeatureFlagDefinition` + `FeatureFlagDefinitionId` |

### 3.2 Entidade modificada (Domain)

| Ficheiro | Alteração |
|---|---|
| `src/modules/configuration/NexTraceOne.Configuration.Domain/Entities/ConfigurationDefinition.cs` | Adicionada propriedade `ModuleId? ConfigurationModuleId`; parâmetro `moduleId` adicionado aos métodos `Create` e `Update` (opcional/nullable para retrocompatibilidade) |

### 3.3 Novos mappings EF Core (Infrastructure)

| Ficheiro | Descrição |
|---|---|
| `src/modules/configuration/NexTraceOne.Configuration.Infrastructure/Persistence/Configurations/ConfigurationModuleConfiguration.cs` | Mapping para `cfg_modules` com typed ID, constraints, índices e xmin |
| `src/modules/configuration/NexTraceOne.Configuration.Infrastructure/Persistence/Configurations/FeatureFlagDefinitionConfiguration.cs` | Mapping para `cfg_feature_flag_definitions` com typed ID, scopes array, FK para módulo, índices e xmin |

### 3.4 Mapping modificado (Infrastructure)

| Ficheiro | Alteração |
|---|---|
| `src/modules/configuration/NexTraceOne.Configuration.Infrastructure/Persistence/Configurations/ConfigurationDefinitionConfiguration.cs` | Adicionado mapeamento da propriedade `ModuleId`; adicionada FK para `ConfigurationModule` com `OnDelete.SetNull`; adicionado índice em `ModuleId` |

### 3.5 DbContext expandido (Infrastructure)

| Ficheiro | Alteração |
|---|---|
| `src/modules/configuration/NexTraceOne.Configuration.Infrastructure/Persistence/ConfigurationDbContext.cs` | Adicionados 2 novos DbSets: `Modules` (`DbSet<ConfigurationModule>`) e `FeatureFlagDefinitions` (`DbSet<FeatureFlagDefinition>`); comentário do XML-doc atualizado para refletir a hierarquia |

### 3.6 Novos testes (Tests)

| Ficheiro | Descrição |
|---|---|
| `tests/modules/configuration/NexTraceOne.Configuration.Tests/Domain/ConfigurationModuleTests.cs` | 13 testes para `ConfigurationModule` |
| `tests/modules/configuration/NexTraceOne.Configuration.Tests/Domain/FeatureFlagDefinitionTests.cs` | 16 testes para `FeatureFlagDefinition` |
| `tests/modules/configuration/NexTraceOne.Configuration.Tests/Domain/ConfigurationDefinitionTests.cs` | 5 testes adicionados para validar `ModuleId` em `ConfigurationDefinition` |

---

## 4. Novas entidades introduzidas

### 4.1 `ConfigurationModule`

Representa um módulo ou domínio funcional da plataforma, tornando explícita a dimensão
"módulo" na hierarquia de configuração.

**Tabela:** `cfg_modules`

**Propriedades:**

| Propriedade | Tipo | Descrição |
|---|---|---|
| `Id` | `ConfigurationModuleId` | Typed ID (Guid) |
| `Key` | `string` (max 100, unique) | Chave imutável normalizada lowercase (ex: `"notifications"`) |
| `DisplayName` | `string` (max 200) | Nome de exibição |
| `Description` | `string?` (max 500) | Descrição opcional |
| `SortOrder` | `int` | Ordem de apresentação |
| `IsActive` | `bool` | Estado ativo/inativo |
| `CreatedAt` | `DateTimeOffset` | Data de criação |
| `UpdatedAt` | `DateTimeOffset?` | Data da última atualização |
| `RowVersion` | `uint` | Concorrência otimista (xmin) |

**Índices:** `Key` (unique), `IsActive` (partial), `SortOrder`

### 4.2 `FeatureFlagDefinition`

Representa a definição persistida de uma feature flag — metadados, âmbitos suportados,
valor padrão e módulo de pertença. Prepara o terreno para a resolução de flags por
âmbito em P3.2 (sem implementar ainda a resolução completa).

**Tabela:** `cfg_feature_flag_definitions`

**Propriedades:**

| Propriedade | Tipo | Descrição |
|---|---|---|
| `Id` | `FeatureFlagDefinitionId` | Typed ID (Guid) |
| `Key` | `string` (max 256, unique) | Chave imutável da flag |
| `DisplayName` | `string` (max 200) | Nome de exibição |
| `Description` | `string?` (max 1000) | Descrição opcional |
| `DefaultEnabled` | `bool` | Valor padrão da flag |
| `AllowedScopes` | `ConfigurationScope[]` | Âmbitos onde pode ser substituída |
| `ModuleId` | `ConfigurationModuleId?` | FK opcional para módulo |
| `IsActive` | `bool` | Estado ativo/inativo |
| `IsEditable` | `bool` | Editável via interface |
| `CreatedAt` | `DateTimeOffset` | Data de criação |
| `UpdatedAt` | `DateTimeOffset?` | Data da última atualização |
| `RowVersion` | `uint` | Concorrência otimista (xmin) |

**Índices:** `Key` (unique), `ModuleId`, `IsActive` (partial)

**FK:** `ModuleId → cfg_modules.id` com `ON DELETE SET NULL`

### 4.3 `ConfigurationDefinition.ModuleId` (adição)

Propriedade nullable `ConfigurationModuleId? ModuleId` adicionada à entidade existente.
Cria FK `ModuleId → cfg_modules.id` com `ON DELETE SET NULL`.

- **Retrocompatível:** valor `null` para todas as 345+ definições existentes no seeder.
- O seeder **não precisa ser alterado** — o parâmetro `moduleId` é opcional com default `null`.

---

## 5. Hierarquia resultante no modelo

```
Instance (ConfigurationScope.System)
  └── Tenant (ConfigurationScope.Tenant)
        └── Environment (ConfigurationScope.Environment)
              └── Module (ConfigurationModule.Key)
                    └── ConfigurationDefinition / FeatureFlagDefinition
                          └── ConfigurationEntry (valores efetivos por âmbito)
```

A hierarquia **Instance → Tenant → Environment** já estava coberta via `ConfigurationScope`
e `ScopeReferenceId` na lógica de resolução. A dimensão **Module** passa agora a ter
representação persistida formal via `ConfigurationModule`.

---

## 6. Impacto no seeder e nas features existentes

### ConfigurationDefinitionSeeder

- **Sem alterações necessárias.** O parâmetro `moduleId` em `ConfigurationDefinition.Create()`
  é opcional com default `null`. As 345+ definições existentes continuam a compilar e a
  funcionar sem modificação.
- Oportunidade futura (P3.2): enriquecer o seeder com associações de módulo por prefixo de chave.

### Features CQRS (7 handlers existentes)

Nenhum dos 7 handlers foi alterado:

| Feature | Estado |
|---|---|
| `GetEffectiveSettings` | Inalterado |
| `GetEntries` | Inalterado |
| `GetDefinitions` | Inalterado |
| `GetAuditHistory` | Inalterado |
| `SetConfigurationValue` | Inalterado |
| `ToggleConfiguration` | Inalterado |
| `RemoveOverride` | Inalterado |

A lógica de resolução hierárquica (`ConfigurationResolutionService`) e os repositórios
existentes não foram alterados — continuam a funcionar com o modelo expandido.

---

## 7. Estado do ConfigurationDbContext após expansão

```csharp
public sealed class ConfigurationDbContext : NexTraceDbContextBase, IUnitOfWork
{
    // Entidades originais (preservadas)
    public DbSet<ConfigurationDefinition> Definitions => Set<ConfigurationDefinition>();
    public DbSet<ConfigurationEntry> Entries => Set<ConfigurationEntry>();
    public DbSet<ConfigurationAuditEntry> AuditEntries => Set<ConfigurationAuditEntry>();

    // Novas entidades (P3.1)
    public DbSet<ConfigurationModule> Modules => Set<ConfigurationModule>();
    public DbSet<FeatureFlagDefinition> FeatureFlagDefinitions => Set<FeatureFlagDefinition>();
}
```

Tabelas geridas pelo contexto:

| Tabela | Entidade | Estado |
|---|---|---|
| `cfg_definitions` | `ConfigurationDefinition` | Existente + `module_id` FK adicionada |
| `cfg_entries` | `ConfigurationEntry` | Inalterada |
| `cfg_audit_entries` | `ConfigurationAuditEntry` | Inalterada |
| `cfg_modules` | `ConfigurationModule` | **Nova** |
| `cfg_feature_flag_definitions` | `FeatureFlagDefinition` | **Nova** |
| `cfg_outbox_messages` | Outbox | Inalterada |

---

## 8. Validação funcional e de compilação

| Critério | Resultado |
|---|---|
| `dotnet build` sem erros | ✅ 0 erros, 52 warnings pré-existentes |
| Testes Configuration antes (251) | ✅ 251/251 passed |
| Testes Configuration após (285) | ✅ 285/285 passed (+34 novos) |
| Seeder sem alterações | ✅ compatível |
| Features CQRS sem alterações | ✅ preservadas |
| DbContext compilável com novos DbSets | ✅ |

---

## 9. Notas sobre migrações

Por definição do escopo desta fase (P3.1), **não foi criada migração** para as novas tabelas.
As tabelas `cfg_modules` e `cfg_feature_flag_definitions`, bem como a coluna `module_id` em
`cfg_definitions`, ficam pendentes de migração para P3.2.

Ver `p3-1-post-change-gap-report.md` para detalhe.
