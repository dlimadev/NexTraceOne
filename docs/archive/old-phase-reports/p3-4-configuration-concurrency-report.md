# P3.4 — Configuration Module Concurrency Report

**Data de execução:** 2026-03-26  
**Fase:** P3.4 — Adicionar RowVersion / controlo de concorrência no módulo Configuration  
**Módulo:** Configuration  
**Estado:** CONCLUÍDO

---

## 1. Estado anterior de concorrência

| Entidade | Estado antes de P3.4 |
|---|---|
| `ConfigurationDefinition.RowVersion` | ✅ Propriedade existia (`public uint RowVersion { get; set; }`) |
| `ConfigurationEntry.RowVersion` | ✅ Propriedade existia (`public uint RowVersion { get; set; }`) |
| `ConfigurationDefinitionConfiguration.IsRowVersion()` | ✅ Mapeamento existia |
| `ConfigurationEntryConfiguration.IsRowVersion()` | ✅ Mapeamento existia |
| Handlers capturavam `DbUpdateConcurrencyException` | ❌ Não — exceção propagava como 500 não tratada |
| DTOs expunham `RowVersion` | ❌ Não — `ConfigurationEntryDto` e `FeatureFlagEntryDto` sem campo |
| `NexTraceDbContextBase` traduzia exceção de concorrência | ❌ Não — `SaveChangesAsync` não capturava |
| `ConcurrencyException` em BuildingBlocks | ❌ Não existia |

**Consequência do estado anterior:** conflito de concorrência real → `DbUpdateConcurrencyException` não capturada → propagava como 500 Internal Server Error → perda de atualização silenciosa para o cliente.

---

## 2. Estratégia adotada

### Problema arquitetural de base

Os handlers estão na camada **Application**, que não pode referenciar `Microsoft.EntityFrameworkCore`. Portanto a abordagem de capturar `DbUpdateConcurrencyException` diretamente nos handlers viola Clean Architecture.

### Solução adotada (2 camadas)

**Camada Infrastructure (`NexTraceDbContextBase`):**
- `SaveChangesAsync` captura `DbUpdateConcurrencyException`
- Relança como `ConcurrencyException` (excepção definida em BuildingBlocks.Application)
- Isola a dependência EF Core da camada de aplicação

**Camada Application (handlers):**
- Captura `ConcurrencyException` (sem dependência EF Core)
- Retorna `Error.Conflict(code, message)` com HTTP 409

---

## 3. Ficheiros alterados

### 3.1 Novo ficheiro — BuildingBlocks

| Ficheiro | Descrição |
|---|---|
| `src/building-blocks/NexTraceOne.BuildingBlocks.Application/Abstractions/ConcurrencyException.cs` | Nova excepção `ConcurrencyException` com `EntityType` property; substitui dependência de `DbUpdateConcurrencyException` na Application layer |

### 3.2 Ficheiro modificado — Infrastructure base

| Ficheiro | Alteração |
|---|---|
| `src/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure/Persistence/NexTraceDbContextBase.cs` | `SaveChangesAsync` captura `DbUpdateConcurrencyException` e relança como `ConcurrencyException(entityType)` |

### 3.3 DTOs modificados — Contracts

| Ficheiro | Alteração |
|---|---|
| `src/modules/configuration/NexTraceOne.Configuration.Contracts/DTOs/ConfigurationEntryDto.cs` | Adicionado `uint RowVersion` como último parâmetro do record |
| `src/modules/configuration/NexTraceOne.Configuration.Contracts/DTOs/FeatureFlagDtos.cs` | Adicionado `uint RowVersion` a `FeatureFlagEntryDto` |

### 3.4 Handlers modificados — Application

| Ficheiro | Alteração |
|---|---|
| `SetConfigurationValue.cs` | Wrap de `CommitAsync` com `catch (ConcurrencyException)` → `Error.Conflict("CONFIG_CONCURRENCY_CONFLICT")`; `RowVersion` incluído no DTO |
| `ToggleConfiguration.cs` | Wrap de `CommitAsync` com `catch (ConcurrencyException)` → `Error.Conflict("CONFIG_CONCURRENCY_CONFLICT")` |
| `RemoveOverride.cs` | Wrap de `CommitAsync` com `catch (ConcurrencyException)` → `Error.Conflict("CONFIG_CONCURRENCY_CONFLICT")` |
| `SetFeatureFlagOverride.cs` | Wrap de `CommitAsync` com `catch (ConcurrencyException)` → `Error.Conflict("FEATURE_FLAG_CONCURRENCY_CONFLICT")`; `RowVersion` incluído no DTO |
| `RemoveFeatureFlagOverride.cs` | Wrap de `CommitAsync` com `catch (ConcurrencyException)` → `Error.Conflict("FEATURE_FLAG_CONCURRENCY_CONFLICT")` |
| `GetEntries.cs` | `RowVersion: e.RowVersion` adicionado ao DTO de leitura |

### 3.5 Novos testes

| Ficheiro | Descrição |
|---|---|
| `tests/.../Features/ConcurrencyConflictTests.cs` | 7 testes: 5 handlers × `ConcurrencyException` → `Error.Conflict`, 2 × `RowVersion` em DTOs |

---

## 4. Mecanismo de concorrência — como funciona

```
1. Handler carrega entidade via repositório → EF Core rastreia RowVersion (xmin)
2. Handler muta entidade (UpdateValue, Activate, etc.)
3. Handler chama unitOfWork.CommitAsync()
4. ConfigurationDbContext.SaveChangesAsync() (herdado de NexTraceDbContextBase)
5. EF Core emite UPDATE ... WHERE xmin = @RowVersion
   ├── Se a linha não foi modificada → UPDATE afeta 1 linha → OK
   └── Se a linha foi modificada por outro processo → UPDATE afeta 0 linhas
       → EF Core lança DbUpdateConcurrencyException
       → NexTraceDbContextBase captura e relança como ConcurrencyException
       → Handler captura ConcurrencyException
       → Retorna Error.Conflict("CONFIG_CONCURRENCY_CONFLICT", ...)
       → HTTP 409 Conflict para o cliente
```

---

## 5. Entidades ajustadas

### Já tinham RowVersion (sem alteração)

```csharp
// ConfigurationDefinition
public uint RowVersion { get; set; }

// ConfigurationEntry
public uint RowVersion { get; set; }

// ConfigurationDefinitionConfiguration
builder.Property(x => x.RowVersion).IsRowVersion();

// ConfigurationEntryConfiguration
builder.Property(x => x.RowVersion).IsRowVersion();
```

### FeatureFlagEntry e FeatureFlagDefinition (P3.2)

Também já tinham `RowVersion` com `IsRowVersion()` — sem alteração necessária.

---

## 6. Conflitos por handler — comportamento pós-P3.4

| Handler | Código de conflito | Tipo HTTP |
|---|---|---|
| `SetConfigurationValue` | `CONFIG_CONCURRENCY_CONFLICT` | 409 Conflict |
| `ToggleConfiguration` | `CONFIG_CONCURRENCY_CONFLICT` | 409 Conflict |
| `RemoveOverride` | `CONFIG_CONCURRENCY_CONFLICT` | 409 Conflict |
| `SetFeatureFlagOverride` | `FEATURE_FLAG_CONCURRENCY_CONFLICT` | 409 Conflict |
| `RemoveFeatureFlagOverride` | `FEATURE_FLAG_CONCURRENCY_CONFLICT` | 409 Conflict |

---

## 7. Impacto cross-module de `NexTraceDbContextBase`

A tradução `DbUpdateConcurrencyException` → `ConcurrencyException` em `NexTraceDbContextBase` aplica-se a **todos os módulos** que herdam `NexTraceDbContextBase`. Módulos que já tinham `RowVersion` (AIKnowledge, Governance, Identity) passam agora a lançar `ConcurrencyException` em vez de `DbUpdateConcurrencyException` — sem quebrar o comportamento desses módulos, porque anteriormente nenhum deles capturava a exceção explicitamente.

---

## 8. Validação funcional e de compilação

| Critério | Resultado |
|---|---|
| `dotnet build` Application sem erros | ✅ 0 erros |
| Testes Configuration antes (319) | ✅ 319/319 passed |
| Testes Configuration após (326) | ✅ 326/326 passed (+7 novos) |
| `RowVersion` em `ConfigurationEntryDto` | ✅ Confirmado |
| `RowVersion` em `FeatureFlagEntryDto` | ✅ Confirmado |
| Conflito de concorrência → 409 Conflict | ✅ Testado |
| Clean Architecture preservada | ✅ Application sem dependência EF Core |
