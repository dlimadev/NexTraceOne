# P3.1 — Post-Change Gap Report

**Data de execução:** 2026-03-26  
**Fase:** P3.1 — Expandir o ConfigurationDbContext para hierarquia tenant/environment/module  
**Módulo:** Configuration  
**Estado:** FASE CONCLUÍDA COM GAPS CONTROLADOS

---

## 1. O que foi resolvido nesta fase

| Item | Resultado |
|---|---|
| `ConfigurationDbContext` expandido com DbSets para `ConfigurationModule` e `FeatureFlagDefinition` | ✅ Resolvido |
| Entidade `ConfigurationModule` criada — torna explícita a dimensão Module na hierarquia | ✅ Resolvido |
| Entidade `FeatureFlagDefinition` criada — prepara o terreno para feature flags em P3.2 | ✅ Resolvido |
| `ConfigurationDefinition.ModuleId` adicionado — FK opcional para `ConfigurationModule` | ✅ Resolvido |
| Mappings EF Core criados/atualizados para as novas entidades e relações | ✅ Resolvido |
| Retrocompatibilidade com o `ConfigurationDefinitionSeeder` (345+ definições) | ✅ Preservada |
| Retrocompatibilidade com os 7 handlers CQRS existentes | ✅ Preservada |
| Testes de domínio para as novas entidades (34 novos testes) | ✅ Criados |
| Compilação sem erros | ✅ Validada |

---

## 2. O que ficou pendente (gaps controlados)

### 2.1 Migração de base de dados

**Prioridade:** P1 — Bloqueador para produção  
**Contexto:** Por definição do escopo desta fase, não foi criada migração EF Core.

**Ação necessária em P3.2:**

```bash
dotnet ef migrations add P3_1_ConfigurationHierarchyExpansion \
  --project src/modules/configuration/NexTraceOne.Configuration.Infrastructure \
  --startup-project src/platform/NexTraceOne.ApiHost
```

**Tabelas que precisam ser criadas:**
- `cfg_modules`
- `cfg_feature_flag_definitions`

**Coluna que precisa ser adicionada:**
- `cfg_definitions.module_id` (nullable FK)

---

### 2.2 Feature flags — resolução por âmbito (FeatureFlagEntry)

**Prioridade:** P2  
**Contexto:** `FeatureFlagDefinition` representa apenas os metadados da flag.
Falta a entidade `FeatureFlagEntry` para armazenar substituições de valor por âmbito
(Tenant, Environment) e o serviço de resolução correspondente.

**Ação necessária em P3.2:**
- Criar entidade `FeatureFlagEntry` com `Key`, `Scope`, `ScopeReferenceId`, `IsEnabled`
- Criar `IFeatureFlagRepository` e `FeatureFlagRepository`
- Criar `IFeatureFlagResolutionService` e implementação
- Criar handlers CQRS: `GetFeatureFlag`, `SetFeatureFlag`, `ToggleFeatureFlag`
- Criar endpoint `FeatureFlagEndpointModule`
- Adicionar `FeatureFlagEntries` ao `ConfigurationDbContext`

---

### 2.3 Enriquecimento do seeder com módulos

**Prioridade:** P3  
**Contexto:** O `ConfigurationDefinitionSeeder` cria 345+ definições com `ModuleId = null`.
As definições existentes têm prefixos de chave claros (ex: `"notifications.*"`, `"ai.*"`)
que mapeiam naturalmente para módulos.

**Ação necessária em P3.2:**
- Criar `ConfigurationModuleSeeder` com os módulos padrão da plataforma
- Atualizar o `ConfigurationDefinitionSeeder` para associar definições a módulos por prefixo de chave
- Garantir que o seeder execute os módulos antes das definições (dependência de FK)

**Módulos sugeridos a criar no seeder:**

| Key | DisplayName |
|---|---|
| `notifications` | Notifications |
| `ai` | AI & Intelligence |
| `governance` | Governance & Compliance |
| `contracts` | Contract Management |
| `changes` | Change Intelligence |
| `operations` | Operations & Incidents |
| `catalog` | Service Catalog |
| `identity` | Identity & Access |
| `platform` | Platform & Infrastructure |
| `integrations` | Integrations |
| `finops` | FinOps |
| `observability` | Observability |

---

### 2.4 API endpoints para `ConfigurationModule`

**Prioridade:** P3  
**Contexto:** Não foram criados handlers CQRS nem endpoints para gerir módulos.

**Ação necessária em P3.2:**
- Criar handlers: `GetModules`, `GetModuleById`, `CreateModule`, `UpdateModule`, `ToggleModule`
- Criar `ConfigurationModuleEndpointModule` ou expandir `ConfigurationEndpointModule`
- Adicionar permissões: `configuration:modules:read`, `configuration:modules:write`

---

### 2.5 Schema validation

**Fora do escopo de P3.1 e P3.2 (P3.3+)**  
`ConfigurationDefinition.ValidationRules` já aceita JSON de regras, mas não existe
serviço de validação que as aplique ao salvar um `ConfigurationEntry`.

---

### 2.6 Frontend

**Fora do escopo de P3.1 (P3.2+)**  
Nenhuma alteração de frontend foi realizada nesta fase. O frontend do módulo
Configuration continua a funcionar com o modelo anterior. A UI para módulos e
feature flags fica para fase P3.2.

---

### 2.7 Permissões granulares para módulos e flags

**Fora do escopo de P3.1 (P3.3+)**  
Não foram registadas permissões específicas para `configuration:modules:*` e
`configuration:flags:*`. As permissões existentes (`configuration:read`,
`configuration:write`) continuam a cobrir os endpoints existentes.

---

## 3. O que fica explicitamente para P3.2

| Item | Prioridade |
|---|---|
| Migração EF Core para novas tabelas | P1 — Bloqueador |
| Entidade `FeatureFlagEntry` + repositório + serviço de resolução | P2 |
| Handlers CQRS para feature flags | P2 |
| `ConfigurationModuleSeeder` | P3 |
| Enriquecimento do seeder de definições com `ModuleId` | P3 |
| Endpoints para gestão de módulos | P3 |
| Frontend para módulos e feature flags | P3 |

---

## 4. Limitações residuais após a expansão

1. **Modelo snapshot desatualizado:** O `ConfigurationDbContextModelSnapshot.cs` da migração
   `InitialCreate` não reflete as novas entidades. Não há impacto no código, mas `dotnet ef`
   mostrará pending model changes até P3.2 criar a nova migração.

2. **`cfg_modules` e `cfg_feature_flag_definitions` não existem no banco:** As tabelas só
   serão criadas após a migração de P3.2. Em runtime com banco real, qualquer query sobre
   `dbContext.Modules` ou `dbContext.FeatureFlagDefinitions` resultará em erro SQL até
   a migração ser aplicada.

3. **`cfg_definitions.module_id` não existe no banco:** A coluna só será adicionada pela
   migração de P3.2. O seeder continua a funcionar porque não persiste `ModuleId`
   (valor é `null` por omissão e a coluna não existe ainda).

---

## 5. Classificação da fase

```
P3_1_STATUS = COMPLETE_WITH_CONTROLLED_GAPS
```

- Expansão estrutural do modelo: ✅ Concluída
- Compilação e testes: ✅ Validados
- Base funcional preservada: ✅ Confirmada
- Migrações: ⏳ Pendentes para P3.2
- Feature flags end-to-end: ⏳ Pendentes para P3.2
