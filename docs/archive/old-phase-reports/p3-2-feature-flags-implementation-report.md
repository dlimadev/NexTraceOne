# P3.2 — Feature Flags Implementation Report

**Data de execução:** 2026-03-26  
**Fase:** P3.2 — Implementar feature flags reais no módulo Configuration  
**Módulo:** Configuration  
**Estado:** CONCLUÍDO

---

## 1. Objetivo da fase

Tornar o módulo Configuration capaz de sustentar feature flags reais com:

- modelo persistido completo (definição + substituição por âmbito);
- resolução hierárquica Instance → Tenant → Environment;
- backend CQRS funcional para leitura e escrita;
- endpoints REST expostos;
- tipos e API client actualizados no frontend.

---

## 2. Ficheiros alterados

### 2.1 Nova entidade (Domain)

| Ficheiro | Descrição |
|---|---|
| `src/modules/configuration/NexTraceOne.Configuration.Domain/Entities/FeatureFlagEntry.cs` | Nova entidade `FeatureFlagEntry` + `FeatureFlagEntryId` |

### 2.2 Novo mapping EF Core (Infrastructure)

| Ficheiro | Descrição |
|---|---|
| `src/modules/configuration/NexTraceOne.Configuration.Infrastructure/Persistence/Configurations/FeatureFlagEntryConfiguration.cs` | Mapping para `cfg_feature_flag_entries` com typed ID, FK, check constraint, unique index e xmin |

### 2.3 DbContext expandido (Infrastructure)

| Ficheiro | Alteração |
|---|---|
| `src/modules/configuration/NexTraceOne.Configuration.Infrastructure/Persistence/ConfigurationDbContext.cs` | Adicionado `FeatureFlagEntries` DbSet; atualizado comentário XML-doc |

### 2.4 Nova abstracção de repositório (Application)

| Ficheiro | Descrição |
|---|---|
| `src/modules/configuration/NexTraceOne.Configuration.Application/Abstractions/IFeatureFlagRepository.cs` | Interface com 7 operações: 4 para FeatureFlagDefinition e 5 para FeatureFlagEntry |

### 2.5 Nova implementação de repositório (Infrastructure)

| Ficheiro | Descrição |
|---|---|
| `src/modules/configuration/NexTraceOne.Configuration.Infrastructure/Persistence/Repositories/FeatureFlagRepository.cs` | Implementação EF Core do `IFeatureFlagRepository` |

### 2.6 DI atualizado (Infrastructure)

| Ficheiro | Alteração |
|---|---|
| `src/modules/configuration/NexTraceOne.Configuration.Infrastructure/DependencyInjection.cs` | `IFeatureFlagRepository` → `FeatureFlagRepository` registado como Scoped |

### 2.7 Novos DTOs (Contracts)

| Ficheiro | DTOs adicionados |
|---|---|
| `src/modules/configuration/NexTraceOne.Configuration.Contracts/DTOs/FeatureFlagDtos.cs` | `FeatureFlagDefinitionDto`, `FeatureFlagEntryDto`, `EvaluatedFeatureFlagDto` |

### 2.8 Novos handlers CQRS (Application)

| Handler | Ficheiro |
|---|---|
| `GetFeatureFlags.Query` | `Features/GetFeatureFlags/GetFeatureFlags.cs` |
| `GetEffectiveFeatureFlag.Query` | `Features/GetEffectiveFeatureFlag/GetEffectiveFeatureFlag.cs` |
| `SetFeatureFlagOverride.Command` | `Features/SetFeatureFlagOverride/SetFeatureFlagOverride.cs` |
| `RemoveFeatureFlagOverride.Command` | `Features/RemoveFeatureFlagOverride/RemoveFeatureFlagOverride.cs` |

### 2.9 Endpoints adicionados (API)

| Ficheiro | Alterações |
|---|---|
| `src/modules/configuration/NexTraceOne.Configuration.API/Endpoints/ConfigurationEndpointModule.cs` | 4 novos endpoints + `SetFeatureFlagOverrideRequest`; novos using aliases adicionados |

### 2.10 Frontend atualizado

| Ficheiro | Alterações |
|---|---|
| `src/frontend/src/features/configuration/types/index.ts` | 4 novos tipos: `FeatureFlagDefinitionDto`, `FeatureFlagEntryDto`, `EvaluatedFeatureFlagDto`, `SetFeatureFlagOverrideRequest` |
| `src/frontend/src/features/configuration/api/configurationApi.ts` | 5 novos métodos: `getFeatureFlags`, `getEffectiveFeatureFlags`, `getEffectiveFeatureFlag`, `setFeatureFlagOverride`, `removeFeatureFlagOverride` |

### 2.11 Novos testes

| Ficheiro | Testes |
|---|---|
| `tests/modules/configuration/NexTraceOne.Configuration.Tests/Domain/FeatureFlagEntryTests.cs` | 21 testes para `FeatureFlagEntry` |

---

## 3. Entidade `FeatureFlagEntry`

**Tabela:** `cfg_feature_flag_entries`

**Diferença de design entre `ConfigurationEntry` e `FeatureFlagEntry`:**

| Aspeto | ConfigurationEntry | FeatureFlagEntry |
|---|---|---|
| Tipo de valor | `string` (valor polimórfico) | `bool IsEnabled` (forte e typed) |
| Âmbito de uso | Configurações operacionais | Feature gates de produto/plataforma |
| Herança | Sim, com `IsInheritable` | Sim, via hierarquia de âmbitos |
| Sensibilidade | Pode ter `IsSensitive` + encriptação | Não — valor booleano não é sensível |
| Validação de tipo | Via `ConfigurationValueType` | Nativa — sempre booleano |
| Propósito semântico | Parâmetros e thresholds operacionais | Activação/desactivação de funcionalidades |

**Propriedades:**

| Propriedade | Tipo | Descrição |
|---|---|---|
| `Id` | `FeatureFlagEntryId` | Typed ID (Guid) |
| `DefinitionId` | `FeatureFlagDefinitionId` | FK para definição |
| `Key` | `string` (max 256) | Chave desnormalizada para queries sem join |
| `Scope` | `ConfigurationScope` | Âmbito de aplicação |
| `ScopeReferenceId` | `string?` (max 256) | Identificador da entidade do âmbito |
| `IsEnabled` | `bool` | Valor da substituição |
| `IsActive` | `bool` | Estado da entrada |
| `ChangeReason` | `string?` (max 500) | Motivo da última alteração |
| `CreatedAt/By` | `DateTimeOffset/string` | Auditoria de criação |
| `UpdatedAt/By` | `DateTimeOffset?/string?` | Auditoria de atualização |
| `RowVersion` | `uint` | Concorrência xmin |

**Constraints EF Core:**
- Check constraint `CK_cfg_feature_flag_entries_scope` nos valores de `ConfigurationScope`
- Índice único em `(Key, Scope, ScopeReferenceId)`
- FK para `cfg_feature_flag_definitions` com `ON DELETE RESTRICT`

---

## 4. Novos endpoints REST

| Método | URL | Permissão | Descrição |
|---|---|---|---|
| `GET` | `/api/v1/configuration/flags` | `configuration:read` | Lista todas as definições de feature flags |
| `GET` | `/api/v1/configuration/flags/effective?scope=...&key=...` | `configuration:read` | Resolve o valor efetivo de uma ou todas as flags para o âmbito dado |
| `PUT` | `/api/v1/configuration/flags/{key}/override` | `configuration:write` | Cria ou actualiza uma substituição por âmbito |
| `DELETE` | `/api/v1/configuration/flags/{key}/override?scope=...` | `configuration:write` | Remove uma substituição por âmbito |

---

## 5. Resolução hierárquica de feature flags

O handler `GetEffectiveFeatureFlag` aplica a mesma lógica da `ConfigurationResolutionService`:

```
User → Team → Role → Environment → Tenant → System → DefaultEnabled
```

- Percorre a hierarquia do âmbito pedido para o mais genérico.
- Usa apenas entradas com `IsActive = true`.
- Se nenhuma entrada for encontrada, devolve `DefaultEnabled` da definição.
- O campo `IsInherited` indica se o valor veio de um âmbito mais genérico.
- O campo `IsDefault` indica que nenhuma substituição existe.

---

## 6. Impacto no seeder e nas features existentes

### ConfigurationDefinitionSeeder
- **Sem alterações necessárias.** O seeder não gere feature flags.
- Oportunidade futura (P3.3): criar `FeatureFlagDefinitionSeeder` com flags padrão da plataforma.

### Features CQRS existentes (7 handlers)
- **Todos preservados sem alteração.**

---

## 7. Validação funcional e de compilação

| Critério | Resultado |
|---|---|
| `dotnet build` Infrastructure sem erros | ✅ 0 erros |
| `dotnet build` API sem erros | ✅ 0 erros |
| Testes Configuration antes (285) | ✅ 285/285 passed |
| Testes Configuration após (306) | ✅ 306/306 passed (+21 novos) |
| Seeder sem alterações | ✅ compatível |
| 7 handlers CQRS existentes sem alterações | ✅ preservados |

---

## 8. Notas sobre migrações

As tabelas `cfg_feature_flag_entries` (nova) e as tabelas de P3.1 (`cfg_modules`, `cfg_feature_flag_definitions`, coluna `cfg_definitions.module_id`) ficam pendentes de migração para P3.3.

Ver `p3-2-post-change-gap-report.md` para detalhe.
