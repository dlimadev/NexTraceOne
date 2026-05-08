# Runbook: EF Core Database Migrations

> **Criado:** Maio 2026  
> **Contexto:** NexTraceOne usa múltiplos DbContexts por módulo, alguns em sub-directórios próprios. O tooling EF Core requer flags `--context` e `--output-dir` explícitos para cada sub-contexto. Este runbook documenta os comandos correctos para cada módulo.

---

## Pré-requisitos

```bash
# Instalar ferramenta EF Core (se não estiver instalada)
dotnet tool install --global dotnet-ef

# Verificar versão
dotnet ef --version  # deve ser >= 9.0
```

Todos os comandos assumem que `src/platform/NexTraceOne.ApiHost` é o `--startup-project`.

---

## Módulos com DbContext único

Para estes módulos basta omitir `--context` (só existe um DbContext):

| Módulo | DbContext | Migrations dir |
|--------|-----------|----------------|
| IdentityAccess | `IdentityAccessDbContext` | `Migrations/` |
| AuditCompliance | `AuditComplianceDbContext` | `Migrations/` |
| Knowledge | `KnowledgeDbContext` | `Migrations/` |
| Notifications | `NotificationsDbContext` | `Migrations/` |
| Integrations | `IntegrationsDbContext` | `Migrations/` |
| ProductAnalytics | `ProductAnalyticsDbContext` | `Migrations/` |
| Configuration | `ConfigurationDbContext` | `Migrations/` |

### Exemplo — IdentityAccess

```bash
# Adicionar migration
dotnet ef migrations add <MigrationName> \
  --project src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure \
  --startup-project src/platform/NexTraceOne.ApiHost \
  --output-dir Persistence/Migrations

# Aplicar migrations pendentes
dotnet ef database update \
  --project src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure \
  --startup-project src/platform/NexTraceOne.ApiHost

# Remover última migration (se ainda não aplicada)
dotnet ef migrations remove \
  --project src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure \
  --startup-project src/platform/NexTraceOne.ApiHost
```

---

## ChangeGovernance — 4 sub-contextos

O módulo ChangeGovernance usa 4 DbContexts independentes, cada um com o seu directório de migrations:

```bash
MODULE=src/modules/changegovernance/NexTraceOne.ChangeGovernance.Infrastructure
STARTUP=src/platform/NexTraceOne.ApiHost

# WorkflowDbContext
dotnet ef migrations add <MigrationName> \
  --project $MODULE --startup-project $STARTUP \
  --context WorkflowDbContext \
  --output-dir Workflow/Persistence/Migrations

# PromotionDbContext
dotnet ef migrations add <MigrationName> \
  --project $MODULE --startup-project $STARTUP \
  --context PromotionDbContext \
  --output-dir Promotion/Persistence/Migrations

# ChangeIntelligenceDbContext
dotnet ef migrations add <MigrationName> \
  --project $MODULE --startup-project $STARTUP \
  --context ChangeIntelligenceDbContext \
  --output-dir ChangeIntelligence/Persistence/Migrations

# RulesetGovernanceDbContext
dotnet ef migrations add <MigrationName> \
  --project $MODULE --startup-project $STARTUP \
  --context RulesetGovernanceDbContext \
  --output-dir RulesetGovernance/Persistence/Migrations
```

---

## Catalog — 7 sub-contextos

```bash
MODULE=src/modules/catalog/NexTraceOne.Catalog.Infrastructure
STARTUP=src/platform/NexTraceOne.ApiHost

# CatalogGraphDbContext (Graph)
dotnet ef migrations add <MigrationName> \
  --project $MODULE --startup-project $STARTUP \
  --context CatalogGraphDbContext \
  --output-dir Graph/Persistence/Migrations

# ContractsDbContext
dotnet ef migrations add <MigrationName> \
  --project $MODULE --startup-project $STARTUP \
  --context ContractsDbContext \
  --output-dir Contracts/Persistence/Migrations

# DeveloperPortalDbContext
dotnet ef migrations add <MigrationName> \
  --project $MODULE --startup-project $STARTUP \
  --context DeveloperPortalDbContext \
  --output-dir Portal/Persistence/Migrations

# TemplatesDbContext
dotnet ef migrations add <MigrationName> \
  --project $MODULE --startup-project $STARTUP \
  --context TemplatesDbContext \
  --output-dir Templates/Persistence/Migrations

# DependencyGovernanceDbContext
dotnet ef migrations add <MigrationName> \
  --project $MODULE --startup-project $STARTUP \
  --context DependencyGovernanceDbContext \
  --output-dir DependencyGovernance/Persistence/Migrations

# LegacyAssetsDbContext
dotnet ef migrations add <MigrationName> \
  --project $MODULE --startup-project $STARTUP \
  --context LegacyAssetsDbContext \
  --output-dir LegacyAssets/Persistence/Migrations
```

---

## OperationalIntelligence — 5 sub-contextos

```bash
MODULE=src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure
STARTUP=src/platform/NexTraceOne.ApiHost

# IncidentDbContext
dotnet ef migrations add <MigrationName> \
  --project $MODULE --startup-project $STARTUP \
  --context IncidentDbContext \
  --output-dir Incidents/Persistence/Migrations

# AutomationDbContext
dotnet ef migrations add <MigrationName> \
  --project $MODULE --startup-project $STARTUP \
  --context AutomationDbContext \
  --output-dir Automation/Persistence/Migrations

# ReliabilityDbContext
dotnet ef migrations add <MigrationName> \
  --project $MODULE --startup-project $STARTUP \
  --context ReliabilityDbContext \
  --output-dir Reliability/Persistence/Migrations

# RuntimeIntelligenceDbContext
dotnet ef migrations add <MigrationName> \
  --project $MODULE --startup-project $STARTUP \
  --context RuntimeIntelligenceDbContext \
  --output-dir Runtime/Persistence/Migrations

# CostIntelligenceDbContext
dotnet ef migrations add <MigrationName> \
  --project $MODULE --startup-project $STARTUP \
  --context CostIntelligenceDbContext \
  --output-dir Cost/Persistence/Migrations
```

---

## AIKnowledge — 3 sub-contextos

```bash
MODULE=src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure
STARTUP=src/platform/NexTraceOne.ApiHost

# AiGovernanceDbContext
dotnet ef migrations add <MigrationName> \
  --project $MODULE --startup-project $STARTUP \
  --context AiGovernanceDbContext \
  --output-dir Governance/Persistence/Migrations

# AiOrchestrationDbContext
dotnet ef migrations add <MigrationName> \
  --project $MODULE --startup-project $STARTUP \
  --context AiOrchestrationDbContext \
  --output-dir Orchestration/Persistence/Migrations

# ExternalAiDbContext
dotnet ef migrations add <MigrationName> \
  --project $MODULE --startup-project $STARTUP \
  --context ExternalAiDbContext \
  --output-dir ExternalAI/Persistence/Migrations
```

---

## Governance

```bash
MODULE=src/modules/governance/NexTraceOne.Governance.Infrastructure
STARTUP=src/platform/NexTraceOne.ApiHost

dotnet ef migrations add <MigrationName> \
  --project $MODULE --startup-project $STARTUP \
  --output-dir Persistence/Migrations
```

---

## Aplicar todas as migrations em produção

O startup da aplicação aplica migrations automaticamente via `DbContext.Database.MigrateAsync()` registado em `Program.cs`. Para aplicação manual (CI/CD ou recovery):

```bash
# Script para aplicar todas — usar com cuidado em produção
for ctx in IdentityAccessDbContext AuditComplianceDbContext KnowledgeDbContext \
           NotificationsDbContext IntegrationsDbContext ProductAnalyticsDbContext \
           ConfigurationDbContext GovernanceDbContext; do
  dotnet ef database update \
    --project <module-infra-project> \
    --startup-project src/platform/NexTraceOne.ApiHost \
    --context $ctx
done
```

---

## SyncModelSnapshot — Migrações Vazias (GAP-M04)

Existem migrações `SyncModelSnapshot` com `Up()` e `Down()` vazios em Catalog e OperationalIntelligence. São harmless no-ops mas devem ser removidas quando possível:

```bash
# Para remover — apenas se a migration NÃO foi aplicada em produção
dotnet ef migrations remove \
  --project <module-infra-project> \
  --startup-project src/platform/NexTraceOne.ApiHost \
  --context <DbContext>
```

**Atenção:** Se a migration já foi aplicada em produção, criar uma nova migration de "cleanup" em vez de remover.

---

## Troubleshooting

### Erro: "More than one DbContext was found"

```
Error: More than one DbContext was found. Specify which one to use.
```

**Solução:** Adicionar `--context <ContextName>` ao comando.

### Erro: "Unable to create an object of type '...'"

O startup project precisa de referenciar o projeto de infraestrutura. Verificar `NexTraceOne.ApiHost.csproj`.

### Erro: "No project was found"

Verificar que os caminhos `--project` e `--startup-project` são relativos à raiz do repositório.
