# Estratégia de Migrations — NexTraceOne

## Visão geral

O NexTraceOne possui **16 DbContexts** distribuídos em **4 bancos lógicos**.
A estratégia de migrations é diferenciada por ambiente para garantir segurança em produção.

## Comportamento por ambiente

| Ambiente | Auto-migrate no startup | Mecanismo recomendado |
|---|---|---|
| Development | ✅ Automático sempre | `ApplyDatabaseMigrationsAsync()` |
| CI (testes) | ✅ Via Testcontainers | Automático nos testes de integração |
| Staging | ⚠️ Opt-in (`NEXTRACE_AUTO_MIGRATE=true`) | Script ou pipeline job |
| Production | ❌ **Bloqueado** (lança exceção) | Script versionado ou pipeline job |

## Implementação no startup (`WebApplicationExtensions.cs`)

O método `ApplyDatabaseMigrationsAsync()` implementa as seguintes regras:

```csharp
// Production com NEXTRACE_AUTO_MIGRATE=true → lança InvalidOperationException
// Development → sempre migra
// Staging com NEXTRACE_AUTO_MIGRATE=true → migra com aviso
// Demais → não migra
```

**Esta lógica já está implementada e testada.**
Production nunca executa auto-migrations, independente de configuração.

## DbContexts e seus bancos

### `nextraceone_identity`
| DbContext | Módulo |
|---|---|
| `IdentityDbContext` | IdentityAccess |
| `AuditDbContext` | AuditCompliance |

### `nextraceone_catalog`
| DbContext | Módulo |
|---|---|
| `CatalogGraphDbContext` | Catalog |
| `ContractsDbContext` | Catalog |
| `DeveloperPortalDbContext` | Catalog |

### `nextraceone_operations`
| DbContext | Módulo |
|---|---|
| `ChangeIntelligenceDbContext` | ChangeGovernance |
| `RulesetGovernanceDbContext` | ChangeGovernance |
| `WorkflowDbContext` | ChangeGovernance |
| `PromotionDbContext` | ChangeGovernance |
| `GovernanceDbContext` | Governance |
| `IncidentDbContext` | OperationalIntelligence |
| `RuntimeIntelligenceDbContext` | OperationalIntelligence |
| `CostIntelligenceDbContext` | OperationalIntelligence |

### `nextraceone_ai`
| DbContext | Módulo |
|---|---|
| `AiGovernanceDbContext` | AIKnowledge |
| `ExternalAiDbContext` | AIKnowledge |
| `AiOrchestrationDbContext` | AIKnowledge |

## Scripts de migrations

### Bash (Linux/macOS/CI)
```bash
# Staging
export CONN_IDENTITY="Host=pg;Database=nextraceone_identity;Username=app;Password=secret"
export CONN_CATALOG="..."
export CONN_OPERATIONS="..."
export CONN_AI="..."
bash scripts/db/apply-migrations.sh --env Staging

# Dry-run (ver pendências sem aplicar)
bash scripts/db/apply-migrations.sh --dry-run

# Production (requer confirmação interativa)
bash scripts/db/apply-migrations.sh --env Production
```

### PowerShell (Windows)
```powershell
$env:CONN_IDENTITY = "Host=pg;Database=nextraceone_identity;Username=app;Password=secret"
$env:CONN_CATALOG = "..."
$env:CONN_OPERATIONS = "..."
$env:CONN_AI = "..."
.\scripts\db\apply-migrations.ps1 -Env Staging

# Dry-run
.\scripts\db\apply-migrations.ps1 -DryRun

# Production (requer confirmação interativa)
.\scripts\db\apply-migrations.ps1 -Env Production
```

### Manualmente via `dotnet ef` (para um context específico)
```bash
dotnet ef database update \
  --project src/platform/NexTraceOne.ApiHost/NexTraceOne.ApiHost.csproj \
  --context NexTraceOne.IdentityAccess.Infrastructure.Persistence.IdentityDbContext \
  --connection "Host=...;Database=nextraceone_identity;..."
```

## Pipeline CI/CD — Job de migrations

No `staging.yml`, o job `run-migrations`:
1. Faz checkout e build da solução
2. Instala `dotnet-ef` global
3. Executa `scripts/db/apply-migrations.sh --env Staging`
4. Lê connection strings de GitHub Secrets (environment `staging`)

**Este job corre antes do deploy dos serviços.**

## Ordem recomendada de aplicação

Na primeira instalação ou upgrade com breaking changes:
1. `nextraceone_identity` (base: autenticação e auditoria)
2. `nextraceone_catalog` (serviços e contratos)
3. `nextraceone_operations` (mudanças e operações)
4. `nextraceone_ai` (AI e governança)

Os scripts já aplicam nesta ordem.

## Rollback de migrations

### Rollback para uma migration específica
```bash
dotnet ef database update <MigrationName> \
  --project src/platform/NexTraceOne.ApiHost/NexTraceOne.ApiHost.csproj \
  --context <FullContextName> \
  --connection "<connection-string>"
```

### Rollback completo (remover todas as migrations)
```bash
dotnet ef database update 0 \
  --project src/platform/NexTraceOne.ApiHost/NexTraceOne.ApiHost.csproj \
  --context <FullContextName> \
  --connection "<connection-string>"
```

⚠️ **Rollback de migration implica perda de dados.** Sempre ter backup antes.

## Seed data

O seed de dados de desenvolvimento (`SeedDevelopmentDataAsync()`) é executado **apenas em `IsDevelopment()`**.
Nunca contamina Staging ou Production.

Para resetar dados de desenvolvimento:
```bash
docker compose down -v && docker compose up -d
```

## Criação de nova migration (desenvolvimento)

```bash
dotnet ef migrations add <MigrationName> \
  --project src/platform/NexTraceOne.ApiHost/NexTraceOne.ApiHost.csproj \
  --context <FullContextName> \
  --output-dir <path/to/Migrations>
```

Exemplo para IdentityDbContext:
```bash
dotnet ef migrations add AddNewFeatureToUser \
  --project src/platform/NexTraceOne.ApiHost/NexTraceOne.ApiHost.csproj \
  --context NexTraceOne.IdentityAccess.Infrastructure.Persistence.IdentityDbContext \
  --output-dir ../NexTraceOne.IdentityAccess.Infrastructure/Persistence/Migrations
```
