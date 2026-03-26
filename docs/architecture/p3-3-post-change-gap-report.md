# P3.3 — Post-Change Gap Report

**Data de execução:** 2026-03-26  
**Fase:** P3.3 — Adaptar o ConfigurationDefinitionSeeder para execução em produção  
**Módulo:** Configuration  
**Estado:** FASE CONCLUÍDA COM GAPS CONTROLADOS

---

## 1. O que foi resolvido nesta fase

| Item | Resultado |
|---|---|
| `IConfigurationDefinitionSeeder` criada (interface injetável) | ✅ Resolvido |
| `SeedingResult` record criado com `Added`, `Skipped`, `Total`, `IsFirstRun`, `IsNoOp` | ✅ Resolvido |
| `ConfigurationDefinitionSeeder` convertido de `static` para `sealed class` | ✅ Resolvido |
| `ConfigurationDefinitionSeeder` implementa `IConfigurationDefinitionSeeder` | ✅ Resolvido |
| `SeedDefaultDefinitionsAsync` retorna `SeedingResult` com contagens | ✅ Resolvido |
| `SaveChangesAsync` otimizado (só chama se `added > 0`) | ✅ Resolvido |
| Seeder registado em DI como Scoped | ✅ Resolvido |
| `SeedConfigurationDefinitionsAsync` usa `IConfigurationDefinitionSeeder` via DI | ✅ Resolvido |
| Logging melhorado: added/skipped/total em cada execução | ✅ Resolvido |
| `SeedConfigurationDefinitionsAsync` já chamado para TODOS os ambientes | ✅ Já existia — confirmado |
| 13 novos testes para `SeedingResult` e invariantes do contrato | ✅ Criados |
| Compilação sem erros | ✅ Validada (319/319 testes) |
| Idempotência preservada | ✅ Confirmada |

---

## 2. O que ficou pendente (gaps controlados)

### 2.1 Migração de base de dados (P1 — bloqueador de P3.1+P3.2+P3.3)

**Prioridade:** P1  
**Contexto:** O seeder agora executa em todos os ambientes, mas só funciona corretamente se o schema existir. As migrações de P3.1/P3.2 ainda não foram geradas.

**Impacto:** Em produção, sem as tabelas `cfg_modules`, `cfg_feature_flag_definitions`, `cfg_feature_flag_entries` e a coluna `cfg_definitions.module_id`, o seeder falha silenciosamente (LogWarning).

**Ação necessária em P3.4:**

```bash
dotnet ef migrations add P3_HierarchyAndFeatureFlags \
  --project src/modules/configuration/NexTraceOne.Configuration.Infrastructure \
  --startup-project src/platform/NexTraceOne.ApiHost
```

---

### 2.2 `FeatureFlagDefinitionSeeder` (P2)

**Prioridade:** P2  
**Contexto:** P3.2 adicionou `FeatureFlagEntry` e `FeatureFlagDefinition`, mas não há seeder que popule definições de flags padrão. A tabela `cfg_feature_flag_definitions` ficará vazia após a migração.

**Ação necessária em P3.4:**
- Criar `IFeatureFlagDefinitionSeeder` + `FeatureFlagDefinitionSeeder`
- Registar em DI
- Chamar em `WebApplicationExtensions` para todos os ambientes
- Definir flags base da plataforma (ver sugestão no P3.2 gap report)

---

### 2.3 `ConfigurationModuleSeeder` (P3)

**Prioridade:** P3  
**Contexto:** P3.1 adicionou `ConfigurationModule` mas não há seeder para as 8+ áreas funcionais da plataforma.

**Ação necessária em P3.4:**
- Criar `ConfigurationModuleSeeder` com módulos padrão
- Ligar `ConfigurationDefinition.ModuleId` às definições existentes (por prefixo de chave)

---

### 2.4 Falha silenciosa no seed (limitação residual)

**Prioridade:** P4  
**Contexto:** O `SeedConfigurationDefinitionsAsync` captura todas as exceções e apenas loga um `LogWarning`. Se o seed falhar em produção por razão diferente de "schema inexistente" (ex: problema de rede, concorrência), a aplicação continua sem configurações.

**Mitigação atual:** O aviso no log é suficiente para diagnóstico. Uma aplicação sem definições falhará nos endpoints de configuração, tornando o problema visível.

**Ação opcional em P3.4+:**
- Expor um health check `/ready` que verifique se o número de definições está acima de um threshold mínimo

---

### 2.5 Testes de integração do seeder (P4)

**Prioridade:** P4  
**Contexto:** Os testes actuais são testes de unidade puros (sem DB). Não existe um teste de integração que valide o comportamento real do seeder contra uma base de dados PostgreSQL.

**Ação necessária em P3.4+:**
- Criar testes de integração com base de dados SQLite ou PostgreSQL em memória (via Testcontainers)
- Validar: primeira execução insere 345 definições; segunda execução é IsNoOp

---

## 3. O que fica explicitamente para P3.4

| Item | Prioridade |
|---|---|
| EF Core migration consolidada (P3.1+P3.2) | P1 — Bloqueador |
| `FeatureFlagDefinitionSeeder` | P2 |
| `ConfigurationModuleSeeder` | P3 |
| Health check de seed | P4 |
| Testes de integração com DB real | P4 |
| UI de gestão de feature flags (herdado de P3.2) | P3 |

---

## 4. Limitações residuais após a adaptação

1. **Seed falha silenciosamente se o schema não existir:** Intencional para suportar startup sem DB (ex: primeiro deploy antes das migrações). O `LogWarning` é o mecanismo de diagnóstico.

2. **`SeedingResult` não persiste:** Não há registo histórico de quando cada definição foi inserida pela primeira vez. Se necessário, pode ser adicionado em P3.4 via tabela de metadados do seeder.

3. **Sem versionamento do catálogo de definições:** Se uma definição existente precisar ser alterada (displayName, description, allowedScopes), o seeder não actualiza — apenas ignora chaves já existentes. Alterações a definições existentes requerem migração de dados.

---

## 5. Classificação da fase

```
P3_3_STATUS = COMPLETE_WITH_CONTROLLED_GAPS
```

- `IConfigurationDefinitionSeeder` injetável: ✅ Concluído
- `SeedingResult` com observabilidade: ✅ Concluído
- Execução em todos os ambientes: ✅ Já estava correto, confirmado
- Idempotência preservada: ✅ Confirmada
- Logging detalhado: ✅ Melhorado
- Migrações pendentes: ⏳ P3.4
- FeatureFlagDefinitionSeeder: ⏳ P3.4
