# Relatório de Migrations de Base de Dados — NexTraceOne

> **Data:** 2025-01-XX  
> **Escopo:** Todas as migrations EF Core do sistema  
> **Método:** Análise estática de ficheiros de migration e model snapshots  
> **Total:** 29 migrations ativas, 19 model snapshots, 2 módulos sem migrations

---

## 1. Resumo Executivo

| Métrica | Valor |
|---|---|
| Migrations ativas | 29 |
| Model snapshots | 19 |
| DbContexts com migrations | 17 de 20 |
| DbContexts sem migrations | 2 (Configuration, Notifications) — usam `EnsureCreated` |
| Maior número de migrations | AiGovernanceDbContext (7) |
| Migrations com dívida técnica | AiGovernanceDbContext (TenantId type fixes) |
| DesignTimeDbContextFactory | Não existe |

### Classificação de Saúde

| Estado | DbContexts |
|---|---|
| ✅ Estável (1-2 migrations, schema maduro) | Identity, Audit, Contracts, CatalogGraph, DeveloperPortal, ChangeIntelligence, Workflow, Promotion, Ruleset, CostIntelligence, Incident, Automation, Reliability, Runtime, AiOrchestration, ExternalAi |
| ⚠️ Atenção (múltiplas migrations, evolução ativa) | Governance (3), AiGovernance (7) |
| ❌ Ausente (sem migrations formais) | Configuration, Notifications |

---

## 2. Inventário Detalhado de Migrations

### 2.1 `nextraceone_identity`

#### IdentityDbContext — 2 migrations

| # | Migration | Conteúdo | Observações |
|---|---|---|---|
| 1 | `InitialCreate` | Schema completo: Users, Roles, Permissions, Teams, Tenants, Environments, ApiKeys, Sessions, Tokens | Schema fundacional — 16 entidades |
| 2 | `AddIsPrimaryProductionToEnvironment` | Adiciona `IsPrimaryProduction` a EnvironmentProfile | Necessário para Change Confidence |

**Localização:** `src/modules/identityaccess/Infrastructure/Persistence/Migrations/`  
**Estado:** ✅ Estável — schema maduro com evolução mínima

#### AuditDbContext — 2 migrations

| # | Migration | Conteúdo | Observações |
|---|---|---|---|
| 1 | `InitialCreate` | Schema base: AuditEvent, AuditChainLink | Audit trail fundacional |
| 2 | `Phase3ComplianceDomain` | Adiciona: ComplianceReport, ComplianceRule, ComplianceViolation, ComplianceSnapshot | Evolução planeada por fases |

**Localização:** `src/modules/auditcompliance/Infrastructure/Persistence/Migrations/`  
**Estado:** ✅ Estável — nomenclatura "Phase3" indica roadmap planeado

---

### 2.2 `nextraceone_catalog`

#### ContractsDbContext — 1 migration

| # | Migration | Conteúdo |
|---|---|---|
| 1 | `InitialCreate` | ApiContract, SoapContract, EventContract, BackgroundServiceContract, ContractVersion (com owned entities), ContractSchema, ContractPolicy |

**Localização:** `src/modules/catalog/Infrastructure/Persistence/Migrations/Contracts/`  
**Estado:** ✅ Estável

#### CatalogGraphDbContext — 1 migration

| # | Migration | Conteúdo |
|---|---|---|
| 1 | `InitialCreate` | ServiceDefinition, ServiceDependency, ServiceEndpoint, ServiceOwnership, ServiceTag, ServiceHealth, DependencyEdge, TopologySnapshot |

**Localização:** `src/modules/catalog/Infrastructure/Persistence/Migrations/Graph/`  
**Estado:** ✅ Estável

#### DeveloperPortalDbContext — 1 migration

| # | Migration | Conteúdo |
|---|---|---|
| 1 | `InitialCreate` | PortalPage, PortalSection, PortalAsset, PortalNavigation, PortalSetting |

**Localização:** `src/modules/catalog/Infrastructure/Persistence/Migrations/Portal/`  
**Estado:** ✅ Estável

---

### 2.3 `nextraceone_operations`

#### ChangeIntelligenceDbContext — 1 migration

| # | Migration | Conteúdo |
|---|---|---|
| 1 | `InitialCreate` | Release, ReleaseChange, BlastRadius, ChangeScore, ChangeValidation, ChangeCorrelation, ChangeEvidence, ChangeMetric, ChangeTimeline, ChangeImpactAssessment |

**Localização:** `src/modules/changegovernance/Infrastructure/Persistence/Migrations/ChangeIntelligence/`  
**Estado:** ✅ Estável — 10 entidades bem definidas

#### PromotionDbContext — 1 migration

| # | Migration | Conteúdo |
|---|---|---|
| 1 | `InitialCreate` | PromotionRequest, PromotionApproval, PromotionSLA, PromotionHistory |

**Estado:** ✅ Estável

#### WorkflowDbContext — 1 migration

| # | Migration | Conteúdo |
|---|---|---|
| 1 | `InitialCreate` | WorkflowTemplate, WorkflowInstance, WorkflowStage, WorkflowEvidence, WorkflowApproval, WorkflowTransition |

**Estado:** ✅ Estável

#### RulesetGovernanceDbContext — 1 migration

| # | Migration | Conteúdo |
|---|---|---|
| 1 | `InitialCreate` | Ruleset, Rule, RuleCondition |

**Estado:** ✅ Estável

#### GovernanceDbContext — 3 migrations

| # | Migration | Conteúdo | Observações |
|---|---|---|---|
| 1 | `InitialCreate` | Schema base: GovernancePolicy, Standard, PolicyRule, PolicyViolation, etc. | 12 entidades |
| 2 | `Phase5Enrichment` | Enriquecimento de entidades existentes — campos adicionais | Nomenclatura "Phase5" indica roadmap |
| 3 | `AddLastProcessedAt` | Adiciona campo `LastProcessedAt` | Campo operacional para processamento |

**Localização:** `src/modules/governance/Infrastructure/Persistence/Migrations/`  
**Estado:** ⚠️ Atenção — 3 migrations; evolução ativa

#### CostIntelligenceDbContext — 2 migrations

| # | Migration | Conteúdo | Observações |
|---|---|---|---|
| 1 | `InitialCreate` | CostAllocation, CostReport, BudgetAlert, CostTag, CostAnomaly | Schema base |
| 2 | `AddCostImportPipeline` | Adiciona CostImportPipeline | Nova entidade para importação |

**Estado:** ✅ Estável — evolução orgânica

#### IncidentDbContext — 1 migration

| # | Migration | Conteúdo |
|---|---|---|
| 1 | `InitialCreate` | Incident, IncidentTimeline, IncidentCorrelation, Runbook, RunbookExecution |

**Estado:** ✅ Estável

#### AutomationDbContext — 1 migration

| # | Migration | Conteúdo |
|---|---|---|
| 1 | `InitialCreate` | AutomationRule, AutomationExecution, AutomationSchedule |

**Estado:** ✅ Estável

#### ReliabilityDbContext — 1 migration

| # | Migration | Conteúdo |
|---|---|---|
| 1 | `InitialCreate` | SLODefinition |

**Estado:** ✅ Estável (mas apenas 1 entidade — fragmentação)

#### RuntimeIntelligenceDbContext — 1 migration

| # | Migration | Conteúdo |
|---|---|---|
| 1 | `InitialCreate` | RuntimeMetric, RuntimeAlert, RuntimeBaseline, RuntimeAnomaly |

**Estado:** ✅ Estável

#### ConfigurationDbContext — ❌ 0 migrations

| Estado | Detalhe |
|---|---|
| Criação de schema | `EnsureCreated` (sem migrations) |
| Seed | C# `ConfigurationDefinitionSeeder` com 345+ definitions |
| Risco | Schema não pode evoluir incrementalmente |

**Impacto:**
- Qualquer alteração de schema requer drop/recreate
- Dados existentes perdem-se em cada alteração
- Seeder recria tudo do zero cada vez

#### NotificationsDbContext — ❌ 0 migrations

| Estado | Detalhe |
|---|---|
| Criação de schema | `EnsureCreated` (sem migrations) |
| Risco | Mesmo problema do ConfigurationDbContext |

---

### 2.4 `nextraceone_ai`

#### AiGovernanceDbContext — 7 migrations ⚠️

| # | Migration | Conteúdo | Observações |
|---|---|---|---|
| 1 | `InitialCreate` | Schema base de IA: Providers, Models, Policies | Foundation |
| 2 | `ExpandProviderAndModelEntities` | Expande Provider e Model com campos adicionais | Evolução orgânica |
| 3 | `AddAiAgentEntity` | Adiciona AiAgent e entidades relacionadas | Major feature |
| 4 | `AddAgentRuntimeFoundation` | Runtime de agentes: Capabilities, Tools, Execution | Major feature |
| 5 | `StandardizeTenantIdToGuid` | Padroniza TenantId para Guid | ⚠️ **Dívida técnica** |
| 6 | `FixTenantIdToUuid` | Corrige TenantId para UUID PostgreSQL | ⚠️ **Dívida técnica** |
| 7 | `SeparateSharedEntityOwnership` | Separa ownership de entidades partilhadas | Refactoring |

**Localização:** `src/modules/aiknowledge/Infrastructure/Persistence/Migrations/Governance/`  
**Estado:** ⚠️ **Dívida técnica** — Migrations 5 e 6 são correções de tipo de TenantId

**Análise da dívida:**
- `StandardizeTenantIdToGuid` e `FixTenantIdToUuid` indicam que TenantId não foi definido corretamente no InitialCreate
- Isto pode ter causado problemas em fresh installs intermédios
- Candidato forte a consolidação de migrations

#### AiOrchestrationDbContext — 1 migration

| # | Migration | Conteúdo |
|---|---|---|
| 1 | `InitialCreate` | Conversation, ConversationMessage, ConversationContext, ConversationFeedback |

**Estado:** ✅ Estável

#### ExternalAiDbContext — 1 migration

| # | Migration | Conteúdo |
|---|---|---|
| 1 | `InitialCreate` | ExternalAiIntegration, ExternalAiRequest, ExternalAiResponse, ExternalAiPolicy |

**Estado:** ✅ Estável

---

## 3. Análise de Qualidade de Migrations

### 3.1 Critérios de Avaliação

| Critério | Estado Global |
|---|---|
| Nomenclatura consistente | ⚠️ Parcial — maioria é `InitialCreate`, AI usa nomes descritivos |
| Up/Down completo | ✅ EF Core gera ambas as direções |
| Idempotência | ⚠️ Não verificada — EF Core não garante por default |
| Tamanho das migrations | ✅ Aceitável — InitialCreate são grandes mas compreensíveis |
| Separação de concerns | ✅ Cada DbContext tem as suas migrations |

### 3.2 Padrões de Nomenclatura

| Padrão | Exemplos | Frequência |
|---|---|---|
| `InitialCreate` | Todos os módulos | 17/29 |
| `Add[Feature]` | `AddIsPrimaryProduction`, `AddCostImportPipeline`, `AddAiAgentEntity` | 5/29 |
| `Phase[N][Description]` | `Phase3ComplianceDomain`, `Phase5Enrichment` | 2/29 |
| `[ActionDescription]` | `ExpandProviderAndModelEntities`, `StandardizeTenantIdToGuid` | 5/29 |

**Observação:** Nomenclatura "Phase" em Audit e Governance sugere roadmap planeado — boa prática de comunicação.

---

## 4. Riscos Identificados

### 4.1 Fresh Install

| Risco | Detalhe | Severidade |
|---|---|---|
| AiGovernance TenantId migrations | Fresh install executa 7 migrations sequencialmente, incluindo 2 fixes de tipo | Médio |
| Configuration sem migrations | `EnsureCreated` pode conflitar com schema existente | Alto |
| Notifications sem migrations | Mesmo problema | Médio |
| Ordem de execução | 29 migrations em 4 bases de dados — dependências cross-DB? | Baixo |

### 4.2 Evolução Futura

| Risco | Detalhe | Severidade |
|---|---|---|
| Sem DesignTimeDbContextFactory | Migrations CLI requer aplicação em execução | Médio |
| Consolidação adiada | AiGovernance com 7 migrations cresce a cada iteração | Médio |
| EnsureCreated para 2 módulos | Limita capacidade de evolução incremental | Alto |

### 4.3 Produção

| Risco | Detalhe | Severidade |
|---|---|---|
| 12 DbContexts em nextraceone_operations | Migrations concorrentes na mesma BD | Médio |
| Sem rollback testado | Down migrations podem não funcionar corretamente | Médio |
| Sem migration lock | 2+ instâncias podem tentar migrar simultaneamente | Médio |

---

## 5. Análise de Consolidação

### 5.1 Candidatos a Consolidação

| DbContext | Migrations Atuais | Ação Recomendada |
|---|---|---|
| AiGovernanceDbContext | 7 (com 2 fixes) | **Consolidar em 1-2 migrations** |
| GovernanceDbContext | 3 | Manter (evolução orgânica) |
| IdentityDbContext | 2 | Manter |
| AuditDbContext | 2 | Manter |
| CostIntelligenceDbContext | 2 | Manter |

### 5.2 Processo de Consolidação para AiGovernanceDbContext

1. Gerar novo InitialCreate a partir do model snapshot atual
2. Incluir todas as entidades no estado final (19+)
3. Remover as 7 migrations antigas
4. Testar fresh install
5. Documentar breaking change para ambientes existentes

**Pré-requisito:** Garantir que nenhum ambiente de produção depende do estado intermédio das migrations.

---

## 6. Módulos sem Migrations — Análise Detalhada

### 6.1 ConfigurationDbContext

| Aspeto | Estado |
|---|---|
| Criação de schema | `context.Database.EnsureCreated()` |
| Seeding | `ConfigurationDefinitionSeeder` com 345+ definitions |
| Evolução | Impossível sem drop/recreate |
| Dados | Recriados pelo seeder (sem perda funcional) |

**Impacto prático:** Para Configuration, o uso de `EnsureCreated` pode ser aceitável se:
- As entidades são simples (3 DbSets)
- O seeder recria todos os dados
- Não há dados user-generated que precisem ser preservados

**Mas:** Se `ConfigurationValue` e `ConfigurationOverride` contêm dados de utilizador, a perda é inaceitável.

### 6.2 NotificationsDbContext

| Aspeto | Estado |
|---|---|
| Criação de schema | `context.Database.EnsureCreated()` |
| Seeding | Não existe seeder |
| Evolução | Impossível sem drop/recreate |
| Dados | Templates e channels de notificação seriam perdidos |

**Impacto prático:** Mais grave que Configuration — templates de notificação são provavelmente configurados manualmente.

---

## 7. Recomendações

### 🔴 Prioridade Alta

| # | Ação | Justificação |
|---|---|---|
| 1 | Migrar ConfigurationDbContext para migrations formais | EnsureCreated não suporta evolução |
| 2 | Migrar NotificationsDbContext para migrations formais | Templates não devem ser perdidos |
| 3 | Consolidar AiGovernanceDbContext migrations | 7 migrations com fixes = dívida técnica |

### 🟡 Prioridade Média

| # | Ação | Justificação |
|---|---|---|
| 4 | Implementar DesignTimeDbContextFactory | Facilita tooling de migrations |
| 5 | Testar fresh install com todas as 29 migrations | Verificar consistência end-to-end |
| 6 | Documentar ordem de execução de migrations entre BDs | 4 BDs com potenciais dependências |

### 🟢 Prioridade Baixa

| # | Ação | Justificação |
|---|---|---|
| 7 | Implementar migration lock para deploys multi-instância | Prevenir migrations concorrentes |
| 8 | Testar Down migrations | Verificar rollback funcional |
| 9 | Padronizar nomenclatura de migrations | Mistura de estilos atual |

---

## Referências

| Artefacto | Localização |
|---|---|
| Migrations Identity | `src/modules/identityaccess/Infrastructure/Persistence/Migrations/` |
| Migrations Audit | `src/modules/auditcompliance/Infrastructure/Persistence/Migrations/` |
| Migrations Catalog | `src/modules/catalog/Infrastructure/Persistence/Migrations/` |
| Migrations Change | `src/modules/changegovernance/Infrastructure/Persistence/Migrations/` |
| Migrations Governance | `src/modules/governance/Infrastructure/Persistence/Migrations/` |
| Migrations AI | `src/modules/aiknowledge/Infrastructure/Persistence/Migrations/` |
| Migrations OpIntel | `src/modules/operationalintelligence/Infrastructure/Persistence/Migrations/` |

---

*Relatório gerado como parte da auditoria modular de governança do NexTraceOne.*
