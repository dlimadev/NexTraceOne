# Plano de Refatoração DDD — NexTraceOne
## De 27 DbContexts / 12 módulos → 10 DbContexts / 10 módulos

> Branch: `claude/backend-dbcontext-ddd-analysis-NcMGd`
> Iniciado: 2026-06-01
> Objetivo: Alinhar bounded contexts com linguagem ubíqua, eliminar fragmentação de DbContexts e consolidar módulos sem coesão real.

---

## Contexto e Motivação

A análise DDD revelou que o projeto tem **27 DbContexts distribuídos em 12 módulos**, criando uma inconsistência grave: módulos que declaram ser um único bounded context mas hospedam internamente múltiplos contextos com linguagens ubíquas distintas.

**Problemas identificados:**
- `catalog`: 7 DbContexts internos — 4 bounded contexts distintos colapsados
- `governance`: god-module com 60+ entidades heterogéneas, zero coesão
- `operationalintelligence`: 6 DbContexts — 3 domínios distintos
- `changegovernance`: 4 DbContexts que deveriam ser 1
- `aiknowledge`: 3 DbContexts que deveriam ser 1
- Duplicação de responsabilidade FinOps entre `governance` e `operationalintelligence`
- `knowledge` e `productanalytics` pertencem semanticamente ao `servicecatalog`

---

## Modelo Alvo

| BC | Módulo | DbContexts | Origem |
|----|--------|-----------|--------|
| **ServiceCatalog** | `servicecatalog` | 1 | catalog (7) + knowledge + productanalytics |
| **ChangeGovernance** | `changegovernance` | 1 | changegovernance (4→1) |
| **IncidentResponse** | `operationalintelligence` | 1 | operationalintelligence (6→1) |
| **FinOps** | `finops` | 1 | extrai de opi/Cost + governance |
| **PlatformGovernance** | `platformgovernance` | 1 | governance + auditcompliance |
| **IdentityAccess** | `identityaccess` | 1 | + Teams + SamlSso de governance |
| **AIHub** | `aiknowledge` | 1 | aiknowledge (3→1) |
| **Integrations** | `integrations` | 1 | inalterado |
| **Notifications** | `notifications` | 1 | inalterado |
| **Configuration** | `configuration` | 1 | enxuto |
| **TOTAL** | **10 módulos** | **10 DbContexts** | de 27 |

**Redução de connection strings**: 27 → 10  
**Redução de Outbox Processors**: 26 → 10  
**Redução de linhas BackgroundWorkers/Program.cs**: 439 → ~120

---

## Princípios de Execução

1. **Build verde obrigatório** ao fim de cada fase antes de avançar
2. **Consolidação additive** — migrations nunca fazem DROP de tabelas
3. **Sem movimentação de dados** — todas as connection strings apontam para o mesmo PostgreSQL
4. **Uma linguagem ubíqua por módulo** — critério de validação de cada fase
5. **Testes reescritos** em paralelo com o desenvolvimento de cada fase

---

## Fases de Execução

### Fase 0 — Limpeza Geral [ ]
- [ ] Eliminar `NexTraceOne.OperationalIntelligence.Infrastructure.Tests` (2 ficheiros — absorver em OPI.Tests)
- [ ] Eliminar `NexTraceOne.Governance.Application.Tests` (4 ficheiros — consolidar em Governance.Tests)
- [ ] Rever e eliminar pastas `Phase5Preview/`, `Phase7Acceleration/`, `SourceOfTruth/` em catalog tests
- [ ] Resolver TODOs reais nos 6 ficheiros identificados:
  - `aiknowledge/.../PromptRouter.cs` — implementar ML.NET routing
  - `aiknowledge/.../IntelligentRouter.cs` — implementar classificação por embeddings
  - `aiknowledge/.../OpenAILLMProvider.cs` — implementar SDK OpenAI real
- [ ] Mover `SamlSsoConfiguration.cs` de governance → identityaccess

### Fase 1 — AIHub: 3 DbContexts → 1 [✅ Em Progresso]
- [x] Criar `AiHubDbContext.cs` (combina AiGovernance + ExternalAi + AiOrchestration)
- [x] Criar `AiHubDbContextDesignTimeFactory.cs`
- [x] Criar `DependencyInjection.cs` unificado com `AddAiHubModule()`
- [ ] Adicionar migration de consolidação
- [ ] Atualizar `BackgroundWorkers/Program.cs`
- [ ] Atualizar `ApiHost/Program.cs`
- [ ] Atualizar `appsettings.json`
- [ ] Reescrever testes de infraestrutura AIHub

### Fase 2 — ChangeGovernance: 4 DbContexts → 1 [✅ Em Progresso]
- [x] Criar `ChangeGovernanceDbContext.cs` (combina CI + Workflow + Promotion + Ruleset)
- [x] Criar `ChangeGovernanceDbContextDesignTimeFactory.cs`
- [x] Criar `DependencyInjection.cs` unificado com `AddChangeGovernanceModule()`
- [ ] Consolidar 4 IXxxModule → IChangeGovernanceModule
- [ ] Adicionar migration de consolidação
- [ ] Atualizar hosts
- [ ] Reescrever testes

### Fase 3 — IncidentResponse: 6 DbContexts → 1 [✅ Em Progresso]
- [x] Criar `IncidentResponseDbContext.cs` (combina Incidents + Reliability + Automation + Runtime)
- [x] Criar `IncidentResponseDbContextDesignTimeFactory.cs`
- [x] Criar `DependencyInjection.cs` unificado com `AddIncidentResponseModule()`
- [ ] Mover `TelemetryStore` para `Ingestion.Api`
- [ ] Separar entidades de Cost (→ Fase 4)
- [ ] Adicionar migration de consolidação
- [ ] Atualizar hosts
- [ ] Reescrever testes

### Fase 4 — FinOps: Novo Módulo [ ]
- [ ] Criar estrutura `src/modules/finops/`
- [ ] Criar `FinOpsDbContext.cs`
- [ ] Criar DI com `AddFinOpsModule()`
- [ ] Mover entidades Cost de OperationalIntelligence
- [ ] Mover entidades FinOps de Governance
- [ ] Criar integration events ChangeDeployed → FinOps
- [ ] Criar testes

### Fase 5 — PlatformGovernance: Governance + AuditCompliance [ ]
- [ ] Renomear `governance/` → `platformgovernance/`
- [ ] Criar `PlatformGovernanceDbContext.cs`
- [ ] Absorver `AuditDbContext` entidades
- [ ] Eliminar módulo `auditcompliance/`
- [ ] Mover Teams → IdentityAccess (Fase 6)
- [ ] Mover FinOps entities → FinOps (Fase 4)
- [ ] Eliminar entidades não-governance: SetupWizardState, DemoSeedState, PresenceSession
- [ ] Criar DI unificado
- [ ] Reescrever testes

### Fase 6 — IdentityAccess: Absorver Teams + SamlSso [ ]
- [ ] Adicionar Team, TeamDomainLink, TeamHealthSnapshot ao IdentityDbContext
- [ ] Absorver SamlSsoConfiguration (já movida na Fase 0)
- [ ] Criar migration `AddTeamsToIdentityContext`
- [ ] Criar integration event `TeamCreatedIntegrationEvent`
- [ ] Adicionar testes de Team

### Fase 7 — ServiceCatalog: Mega-Consolidação [ ]
- [ ] Renomear `catalog/` → `servicecatalog/`
- [ ] Absorver módulo `knowledge/`
- [ ] Absorver módulo `productanalytics/`
- [ ] Criar `ServiceCatalogDbContext.cs` (9 DbContexts → 1)
- [ ] Renomear `RunbookRecord` → `RunbookDocument` para distinguir de IncidentResponse
- [ ] Eliminar módulos `knowledge/` e `productanalytics/`
- [ ] Consolidar interfaces IXxxModule → IServiceCatalogModule
- [ ] Criar DI unificado
- [ ] Reescrever testes

### Fase 8 — Configuration: Limpeza de Escopo [ ]
- [ ] Mover `AutomationRule.cs` → IncidentResponse
- [ ] Mover `WebhookTemplate.cs` → Integrations
- [ ] Mover `TaxonomyCategory.cs` → ServiceCatalog
- [ ] Mover `ServiceCustomField.cs` → ServiceCatalog
- [ ] Avaliar/remover `UserSavedView.cs`
- [ ] Mover `ScheduledReport.cs` → PlatformGovernance
- [ ] Mover `UserAlertRule.cs` → IncidentResponse

### Fase 9 — Actualizar Hosts [ ]
- [ ] `BackgroundWorkers/Program.cs`: 439 → ~120 linhas (26 → 10 módulos)
- [ ] `ApiHost/Program.cs`: simplificar registos de módulo
- [ ] `appsettings.json`: 27 → 10 connection strings
- [ ] `ConnectionStringsPreflightCheck.cs`: validar novas 10 strings
- [ ] Renomear seed data SQL files

### Fase 10 — Testes Unitários [ ]
- [ ] Criar `tests/modules/finops/NexTraceOne.FinOps.Tests/`
- [ ] Criar `tests/modules/platformgovernance/NexTraceOne.PlatformGovernance.Tests/`
- [ ] Criar `tests/modules/servicecatalog/NexTraceOne.ServiceCatalog.Tests/`
- [ ] Consolidar testes aiknowledge (3 sub-contextos → 1)
- [ ] Consolidar testes changegovernance (4 → 1)
- [ ] Consolidar testes operationalintelligence (6 → 1)
- [ ] Garantir cobertura mínima: 1 teste por handler em cada módulo

### Fase 11 — Verificação Final [ ]
- [ ] `dotnet build NexTraceOne.sln` — zero warnings
- [ ] `dotnet test --filter "FullyQualifiedName!~E2E&FullyQualifiedName!~Integration"` — todos passam
- [ ] DbContexts = 10
- [ ] Connection strings = 10
- [ ] Outbox processors = 10
- [ ] Zero referências cruzadas directas entre módulos

---

## Métricas de Progresso

| Métrica | Antes | Alvo | Actual |
|---------|-------|------|--------|
| Módulos | 12 | 10 | 12 |
| DbContexts | 27 | 10 | 27 |
| Connection strings | 27 | 10 | 27 |
| Outbox processors | 26 | 10 | 26 |
| Linhas BackgroundWorkers | 439 | ~120 | 439 |
| Módulos extintos | 0 | 3 | 0 |

---

## Notas de Migração de Base de Dados

**Descoberta crítica**: Todas as 27 connection strings apontam para o mesmo PostgreSQL (`Database=nextraceone`). Consolidar DbContexts é uma operação puramente de código — sem movimentação de dados.

**Estratégia para migrations de consolidação**:
```bash
# 1. Criar migration de consolidação (tabelas já existem)
dotnet ef migrations add ConsolidateContexts --context <NewDbContext>

# 2. Editar a migration gerada — substituir CREATE TABLE por condicional
# As tabelas já existem na base de dados — a migration apenas regista o histórico

# 3. Inserir na __EFMigrationsHistory se necessário
INSERT INTO "__EFMigrationsHistory" VALUES ('<timestamp>_ConsolidateContexts', '10.0.0');
```

---

## Regras DDD Reforçadas

- **1 módulo = 1 DbContext = 1 linguagem ubíqua = 1 team conceptual**
- Módulos comunicam APENAS via Contracts (integration events ou IXxxModule sync)
- Nenhum módulo acede ao DbContext de outro módulo
- `IXxxRepository` (CRUD) → sempre implementação EF Core real
- `IXxxReader` (analytics) → `NullXxxReader` válido como placeholder phase-gated
