# P3.4 — Post-Change Gap Report

**Data de execução:** 2026-03-26  
**Fase:** P3.4 — Adicionar RowVersion / controlo de concorrência no módulo Configuration  
**Módulo:** Configuration  
**Estado:** FASE CONCLUÍDA COM GAPS CONTROLADOS

---

## 1. O que foi resolvido

| Item | Resultado |
|---|---|
| `ConcurrencyException` criada em `BuildingBlocks.Application` | ✅ |
| `NexTraceDbContextBase.SaveChangesAsync` traduz `DbUpdateConcurrencyException` | ✅ |
| `SetConfigurationValue` captura concorrência → 409 Conflict | ✅ |
| `ToggleConfiguration` captura concorrência → 409 Conflict | ✅ |
| `RemoveOverride` captura concorrência → 409 Conflict | ✅ |
| `SetFeatureFlagOverride` captura concorrência → 409 Conflict | ✅ |
| `RemoveFeatureFlagOverride` captura concorrência → 409 Conflict | ✅ |
| `RowVersion` em `ConfigurationEntryDto` | ✅ |
| `RowVersion` em `FeatureFlagEntryDto` | ✅ |
| `GetEntries` inclui `RowVersion` no DTO de leitura | ✅ |
| 7 novos testes de concorrência (326/326) | ✅ |
| Clean Architecture preservada (sem EF Core na Application) | ✅ |

---

## 2. O que ficou pendente (gaps controlados)

### 2.1 EF Core migration consolidada — P1 bloqueador

**Prioridade:** P1  
**Contexto:** As migrações de P3.1 (`cfg_modules`, `cfg_feature_flag_definitions`), P3.2 (`cfg_feature_flag_entries`, `module_id`) ainda não foram geradas. Sem migração, o schema da base de dados não reflete o estado atual do código.

**Nota sobre RowVersion:** xmin é uma coluna do sistema PostgreSQL (não precisa de migration para existir), mas as colunas `module_id` e as tabelas de P3.1/P3.2 precisam de migration.

**Ação necessária em P3.5+:**
```bash
dotnet ef migrations add P3_HierarchyFeatureFlagsConcurrency \
  --project src/modules/configuration/NexTraceOne.Configuration.Infrastructure \
  --startup-project src/platform/NexTraceOne.ApiHost
```

---

### 2.2 `ConcurrencyException` não tratada em outros módulos

**Prioridade:** P3  
**Contexto:** A mudança em `NexTraceDbContextBase` aplica-se a todos os módulos. Módulos como AIKnowledge, Governance e Identity já tinham `RowVersion` mas não capturavam a exceção. Agora receberão `ConcurrencyException` em vez de `DbUpdateConcurrencyException`.

**Impacto imediato:** Zero — nenhum módulo capturava a exceção explicitamente, então a mudança é transparente. A exceção continuará a propagar como 500 não tratada nos outros módulos (comportamento idêntico ao anterior).

**Ação recomendada:** Aplicar o mesmo padrão de `catch (ConcurrencyException)` em handlers críticos de outros módulos numa fase posterior.

---

### 2.3 Frontend não trata 409 explicitamente

**Prioridade:** P3  
**Contexto:** O frontend `configurationApi.ts` não tem lógica para tratar HTTP 409 explicitamente. Os clientes receberão o código de erro no payload mas a UX pode não mostrar uma mensagem clara de "conflito de concorrência".

**Ação recomendada em P3.5+:**
- Detetar 409 na camada de fetch do frontend
- Mostrar mensagem: "Esta configuração foi alterada por outro utilizador. Por favor recarregue e tente novamente."

---

### 2.4 Testes de integração com DB real

**Prioridade:** P4  
**Contexto:** Os 7 novos testes são testes de unidade que simulam a exceção. Não existe um teste de integração que valide o comportamento real do xmin com PostgreSQL.

---

## 3. O que fica para P3.5+

| Item | Prioridade |
|---|---|
| EF Core migration consolidada (P3.1+P3.2+P3.4) | P1 |
| Frontend trata 409 explicitamente | P3 |
| Aplicar padrão `ConcurrencyException` a outros módulos | P3 |
| Testes de integração com DB real | P4 |

---

## 4. Classificação

```
P3_4_STATUS = COMPLETE_WITH_CONTROLLED_GAPS
```

- Entidades com `RowVersion`: ✅ Confirmado (existia + preservado)
- EF Core configs com `IsRowVersion()`: ✅ Confirmado (existia + preservado)  
- `ConcurrencyException` em BuildingBlocks: ✅ Novo
- Handlers capturando concorrência → 409: ✅ Novo
- DTOs expondo `RowVersion`: ✅ Novo
- Migration pendente: ⏳ P3.5+
