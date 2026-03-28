# Relatório de Estado do Banco de Dados — NexTraceOne
**Auditoria Forense | 28 de Março de 2026**

---

## Objetivo da Área no Contexto do Produto

A persistência é o alicerce do NexTraceOne como source of truth. Cada módulo deve ter schema deployável, auditável e alinhado ao domínio. Dados de auditoria, mudanças, contratos e serviços devem ser rastreáveis e consultáveis.

---

## Estado Atual Encontrado

### Inventário de DbContexts e Migrações

| DbContext | Módulo | Migrações | Schema Deployável | Observação |
|---|---|---|---|---|
| `IdentityDbContext` | identityaccess | 3 | ✅ SIM | Outbox processado ativamente |
| `ContractsDbContext` | catalog | 5 | ✅ SIM | — |
| `CatalogGraphDbContext` | catalog | 3 | ✅ SIM | Topologia de serviços |
| `DeveloperPortalDbContext` | catalog | 5 | ✅ SIM | — |
| `ChangeIntelligenceDbContext` | changegovernance | 7 | ✅ SIM | — |
| `WorkflowDbContext` | changegovernance | 5 | ✅ SIM | — |
| `PromotionDbContext` | changegovernance | 3 | ✅ SIM | — |
| `RulesetGovernanceDbContext` | changegovernance | 3 | ✅ SIM | — |
| `IncidentDbContext` | operationalintelligence | 3 | ✅ SIM | Frontend não conectado |
| `AutomationDbContext` | operationalintelligence | 3 | ✅ SIM | Handlers retornam PreviewOnly |
| `ReliabilityDbContext` | operationalintelligence | 5 | ✅ SIM | Handlers não consultam DB |
| `RuntimeIntelligenceDbContext` | operationalintelligence | 3 | ✅ SIM | — |
| `CostIntelligenceDbContext` | operationalintelligence | 7 | ✅ SIM | Não consumido pelos handlers |
| `AuditDbContext` | auditcompliance | 4 | ✅ SIM | Hash chain SHA-256 |
| `GovernanceDbContext` | governance | 3 | ✅ SIM | Handlers não consultam DB (mock) |
| `ConfigurationDbContext` | configuration | 13 | ✅ SIM | Feature flags DB-driven |
| `NotificationsDbContext` | notifications | 9 | ✅ SIM | — |
| `IntegrationsDbContext` | integrations | 3 | ✅ SIM | Conectores são stubs |
| `AiGovernanceDbContext` | aiknowledge | 3 | ✅ SIM | — |
| `AiOrchestrationDbContext` | aiknowledge | 5 | ✅ SIM | Outbox não processado |
| `ExternalAiDbContext` | aiknowledge | 3 | ✅ SIM | 8 handlers TODO |
| `KnowledgeDbContext` | knowledge | 3 | ✅ SIM | Features mínimas implementadas |
| `ProductAnalyticsDbContext` | productanalytics | 3 | ✅ SIM | 100% mock |
| `NexTraceOne` (base) | platform | — | ✅ SIM | Connection string principal |

**Total: 24 DbContexts, ~100+ ficheiros de migração**

---

## Análise de Migrações por Módulo

| Módulo | DbContexts | Total Migrações | Qualidade dos Nomes |
|---|---|---|---|
| catalog | 3 | 13 | ✅ Descritivos (InitialCreate, AddServiceDependencies, etc.) |
| changegovernance | 4 | 18 | ✅ Descritivos (AddBlastRadius, AddFreezeWindows, etc.) |
| operationalintelligence | 5 | 21 | ✅ Mais migrações — evolução clara |
| configuration | 1 | 13 | ✅ Feature flags evolutivos |
| notifications | 1 | 9 | ✅ Canais de entrega adicionados progressivamente |
| auditcompliance | 1 | 4 | ✅ — |
| aiknowledge | 3 | 11 | ✅ — |
| identityaccess | 1 | 3 | ✅ — |
| governance | 1 | 3 | ✅ — |
| knowledge | 1 | 3 | ✅ — |
| integrations | 1 | 3 | ✅ — |
| productanalytics | 1 | 3 | ✅ — |

---

## Estratégia de Base de Dados

### Configuração Atual (appsettings.json)

Todos os 22 connection strings apontam para o **mesmo servidor PostgreSQL**, mesma base de dados `nextraceone`:

```
Host=localhost;Port=5432;Database=nextraceone;Username=nextraceone;Password=REPLACE_VIA_ENV
```

**Implicação:** O produto usa uma única base de dados PostgreSQL com múltiplos DbContexts (multi-schema strategy). Cada DbContext tem prefixo de tabela por módulo (ex.: `cat_`, `chg_`, `id_`, `aud_`).

**Alinhamento com CLAUDE.md:** ✅ PostgreSQL 16 como base central no MVP — conforme definido.

### Inicialização
`infra/postgres/init-databases.sql` — script de inicialização confirmado.

### Seeds
- `db/seed/seed_development.sql` — dados de desenvolvimento
- `db/seed/seed_production.sql` — baseline de produção
- `src/platform/NexTraceOne.ApiHost/SeedData/` — seeds via EF Core

---

## Avaliação do Schema por Área de Produto

### Source of Truth (Catalog)
✅ Schema suporta adequadamente:
- Serviços com metadata, ownership, topologia
- Contratos com versões semânticas, assinaturas, evidências
- Developer Portal com subscriptions e analytics

### Change Intelligence
✅ Schema suporta:
- Releases, blast radius, change scores, freeze windows
- Workflows com aprovações, evidence packs
- Promotion requests e gate evaluations
- Rollback assessments

### Incidents & Operations
⚠️ Schema existe mas não é consumido:
- `IncidentDbContext` tem 5 DbSets (Incidents, RunbookRecords, MitigationRecords, ResolutionRecords, CorrelationEvents)
- `EfIncidentStore` real com 678 linhas mas frontend não conectado
- `RunbookRecord` existe mas handlers usam 3 runbooks hardcoded
- `MitigationRecord` existe mas `CreateMitigationWorkflow` não persiste

### FinOps
⚠️ Schema existe mas não consumido por governance:
- `CostIntelligenceDbContext` com 7 migrações
- Handlers de FinOps em `governance` módulo retornam `IsSimulated: true` sem consultar `CostIntelligenceDbContext`
- `ICostIntelligenceModule` interface não implementada

### Identity & Security
✅ Schema robusto:
- Multi-tenancy com RLS a nível de base de dados
- Auditoria de entidades via `AuditInterceptor`
- Delegações com expiração e revogação

### AI Governance
✅ Schema funcional:
- `AiGovernanceDbContext`: modelos, políticas, budgets, token usage ledger
- `AiOrchestrationDbContext`: conversações, agentes, execuções (sem outbox processado)

---

## Problemas Identificados

### 1. Outbox sem processadores (CRÍTICO)
**Impacto:** Eventos de domínio não propagam entre módulos.
- 23 DbContexts têm tabelas `OutboxMessages` criadas pelo `NexTraceDbContextBase`
- Apenas `IdentityDbContext` tem processador registado em `BackgroundWorkers`
- Sem processamento de outbox, eventos de Catalog, ChangeGovernance e OperationalIntelligence não chegam a consumidores

### 2. Schema existe mas handlers não consultam (ALTA)
- `ReliabilityDbContext`: handlers em reliability retornam 8 serviços hardcoded
- `CostIntelligenceDbContext`: não consultado pelos handlers de FinOps
- `GovernanceDbContext`: ~74 handlers retornam dados mock sem consultar o schema
- `RunbookRecord` e `MitigationRecord`: existem no schema mas não são usados

### 3. Prefixos de tabela
Prefixo de tabelas por módulo (`cat_`, `chg_`, `id_`, `aud_`) confirma estratégia de isolamento lógico dentro de um único DB PostgreSQL. Documentado em `docs/architecture/database-table-prefixes.md`.

---

## Restrições Técnicas Respeitadas

✅ PostgreSQL 16 como base central (alinhado ao MVP)
✅ EF Core 10 com Npgsql confirmado
✅ Migrations coerentes e auditáveis
✅ Sem Redis no MVP (confirmado — não está em nenhum appsettings)
✅ Sem OpenSearch no MVP (PostgreSQL FTS usado)
✅ Sem Temporal — Quartz.NET via BackgroundWorkers

---

## Recomendações

1. **Crítico:** Ativar outbox processors para Catalog e ChangeGovernance no BackgroundWorkers
2. **Alta:** Conectar handlers de Reliability ao `ReliabilityDbContext` (substituir hardcoded)
3. **Alta:** Conectar handlers de FinOps ao `CostIntelligenceDbContext` via `ICostIntelligenceModule`
4. **Alta:** Conectar handlers de Governance ao `GovernanceDbContext` (substituir `IsSimulated`)
5. **Média:** Usar `RunbookRecord` e `MitigationRecord` nos handlers de OperationalIntelligence
6. **Baixa:** Documentar estratégia de sharding/particionamento para volumes enterprise futuros

---

*Data: 28 de Março de 2026*
