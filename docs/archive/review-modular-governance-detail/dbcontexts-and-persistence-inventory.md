# Inventário de DbContexts e Persistência — NexTraceOne

> **Data:** 2025-01-XX  
> **Escopo:** Todos os DbContexts registados no sistema  
> **Método:** Análise estática de DbContext classes, DbSets, configurações e interceptors  
> **Base class comum:** `NexTraceDbContextBase`

---

## Resumo

| Métrica | Valor |
|---|---|
| Total de DbContexts | 20 |
| Bases de dados lógicas | 4 |
| Total estimado de DbSets | 129+ |
| Entity type configurations | 132 |
| Model snapshots | 19 |
| DbContexts sem migrations | 2 (Configuration, Notifications) |

---

## Legenda de Classificação

| Classificação | Significado |
|---|---|
| **COHESIVE** | DbContext bem delimitado, entidades coesas, responsabilidade clara |
| **PARTIAL** | Maioritariamente coeso mas com algumas entidades tangenciais |
| **OVERLOADED** | Demasiadas entidades ou responsabilidades misturadas |
| **FRAGMENTED** | Poucas entidades, potencialmente fragmentação excessiva |

---

## Base de Dados: `nextraceone_identity`

### 1. IdentityDbContext

| Propriedade | Valor |
|---|---|
| **Módulo** | `identityaccess` |
| **Base class** | `NexTraceDbContextBase` |
| **DbSets** | 16 |
| **ConfigurationsNamespace** | `NexTraceOne.Modules.IdentityAccess.Infrastructure.Persistence.Configurations` |
| **Migrations** | 2 (InitialCreate, AddIsPrimaryProductionToEnvironment) |
| **Tenant/Audit/Encryption** | ✅ / ✅ / ✅ |
| **Classificação** | **COHESIVE** |

**Entidades (16 DbSets):**

| # | Entidade | Responsabilidade |
|---|---|---|
| 1 | User | Utilizadores do sistema |
| 2 | Role | Papéis/roles |
| 3 | Permission | Permissões granulares |
| 4 | RolePermission | Associação role↔permission |
| 5 | UserRole | Associação user↔role |
| 6 | Tenant | Tenants/organizações |
| 7 | TenantMember | Membros de tenant |
| 8 | Team | Equipas |
| 9 | TeamMember | Membros de equipa |
| 10 | EnvironmentProfile | Perfis de ambiente (dev, staging, prod) |
| 11 | ApiKey | Chaves de API |
| 12 | RefreshToken | Tokens de refresh |
| 13 | Session | Sessões ativas |
| 14 | InviteToken | Tokens de convite |
| 15 | PasswordResetToken | Tokens de reset de password |
| 16 | AuditLog | Log de auditoria local do módulo |

**Observações:**
- Módulo mais maduro em termos de entidades
- Suporta modelo de identidade completo: users, roles, permissions, teams, tenants, environments
- `IsPrimaryProduction` adicionado em segunda migration — necessário para Change Confidence

---

### 2. AuditDbContext

| Propriedade | Valor |
|---|---|
| **Módulo** | `auditcompliance` |
| **Base class** | `NexTraceDbContextBase` |
| **DbSets** | 6 |
| **ConfigurationsNamespace** | `NexTraceOne.Modules.AuditCompliance.Infrastructure.Persistence.Configurations` |
| **Migrations** | 2 (InitialCreate, Phase3ComplianceDomain) |
| **Tenant/Audit/Encryption** | ✅ / ✅ / ✅ |
| **Classificação** | **COHESIVE** |

**Entidades (6 DbSets):**

| # | Entidade | Responsabilidade |
|---|---|---|
| 1 | AuditEvent | Eventos de auditoria |
| 2 | AuditChainLink | Cadeia de auditoria (blockchain-like) |
| 3 | ComplianceReport | Relatórios de compliance |
| 4 | ComplianceRule | Regras de compliance |
| 5 | ComplianceViolation | Violações detetadas |
| 6 | ComplianceSnapshot | Snapshots de estado de compliance |

**Observações:**
- Separação clara entre audit trail e compliance
- Phase3ComplianceDomain indica evolução planeada por fases

---

## Base de Dados: `nextraceone_catalog`

### 3. ContractsDbContext

| Propriedade | Valor |
|---|---|
| **Módulo** | `catalog` |
| **Base class** | `NexTraceDbContextBase` |
| **DbSets** | 7 |
| **ConfigurationsNamespace** | `NexTraceOne.Modules.Catalog.Infrastructure.Persistence.Configurations.Contracts` |
| **Migrations** | 1 (InitialCreate) |
| **Tenant/Audit/Encryption** | ✅ / ✅ / ✅ |
| **Classificação** | **COHESIVE** |

**Entidades (7 DbSets):**

| # | Entidade | Responsabilidade |
|---|---|---|
| 1 | ApiContract | Contratos REST API |
| 2 | SoapContract | Contratos SOAP |
| 3 | EventContract | Contratos de eventos (Kafka, etc.) |
| 4 | BackgroundServiceContract | Contratos de serviços background |
| 5 | ContractVersion | Versões de contratos (com Signature, Provenance) |
| 6 | ContractSchema | Schemas associados |
| 7 | ContractPolicy | Políticas aplicadas a contratos |

**Observações:**
- Pilar central do produto (Contract Governance)
- Owned entities: `ContractVersion.Signature`, `ContractVersion.Provenance`
- Apenas 1 migration — schema relativamente estável

---

### 4. CatalogGraphDbContext

| Propriedade | Valor |
|---|---|
| **Módulo** | `catalog` |
| **Base class** | `NexTraceDbContextBase` |
| **DbSets** | 8 |
| **ConfigurationsNamespace** | `NexTraceOne.Modules.Catalog.Infrastructure.Persistence.Configurations.Graph` |
| **Migrations** | 1 (InitialCreate) |
| **Tenant/Audit/Encryption** | ✅ / ✅ / ✅ |
| **Classificação** | **COHESIVE** |

**Entidades (8 DbSets):**

| # | Entidade | Responsabilidade |
|---|---|---|
| 1 | ServiceDefinition | Definição de serviço |
| 2 | ServiceDependency | Dependências entre serviços |
| 3 | ServiceEndpoint | Endpoints expostos |
| 4 | ServiceOwnership | Ownership por equipa |
| 5 | ServiceTag | Tags/labels de serviço |
| 6 | ServiceHealth | Estado de saúde |
| 7 | DependencyEdge | Arestas do grafo de dependências |
| 8 | TopologySnapshot | Snapshots de topologia |

**Observações:**
- Suporta Service Catalog & Topology
- Grafo de dependências é essencial para Blast Radius (Change Intelligence)

---

### 5. DeveloperPortalDbContext

| Propriedade | Valor |
|---|---|
| **Módulo** | `catalog` |
| **Base class** | `NexTraceDbContextBase` |
| **DbSets** | 5 |
| **ConfigurationsNamespace** | `NexTraceOne.Modules.Catalog.Infrastructure.Persistence.Configurations.Portal` |
| **Migrations** | 1 (InitialCreate) |
| **Tenant/Audit/Encryption** | ✅ / ✅ / ✅ |
| **Classificação** | **COHESIVE** |

**Entidades (5 DbSets):**

| # | Entidade | Responsabilidade |
|---|---|---|
| 1 | PortalPage | Páginas do portal |
| 2 | PortalSection | Secções de página |
| 3 | PortalAsset | Assets (imagens, ficheiros) |
| 4 | PortalNavigation | Estrutura de navegação |
| 5 | PortalSetting | Configurações do portal |

**Observações:**
- Portal de developer — alinhado com Developer Acceleration
- Separação clara do catálogo técnico

---

## Base de Dados: `nextraceone_operations`

> ⚠️ **Atenção:** Esta base de dados contém 12 DbContexts — é a mais sobrecarregada do sistema.

### 6. ChangeIntelligenceDbContext

| Propriedade | Valor |
|---|---|
| **Módulo** | `changegovernance` |
| **Base class** | `NexTraceDbContextBase` |
| **DbSets** | 10 |
| **ConfigurationsNamespace** | `NexTraceOne.Modules.ChangeGovernance.Infrastructure.Persistence.Configurations.ChangeIntelligence` |
| **Migrations** | 1 (InitialCreate) |
| **Tenant/Audit/Encryption** | ✅ / ✅ / ✅ |
| **Classificação** | **COHESIVE** |

**Entidades (10 DbSets):**

| # | Entidade | Responsabilidade |
|---|---|---|
| 1 | Release | Releases/deploys |
| 2 | ReleaseChange | Alterações incluídas numa release |
| 3 | BlastRadius | Análise de impacto |
| 4 | ChangeScore | Score de confiança da mudança |
| 5 | ChangeValidation | Validações pós-change |
| 6 | ChangeCorrelation | Correlação change↔incident |
| 7 | ChangeEvidence | Evidências associadas |
| 8 | ChangeMetric | Métricas de mudança |
| 9 | ChangeTimeline | Timeline de eventos |
| 10 | ChangeImpactAssessment | Avaliação de impacto |

**Observações:**
- Pilar central: Change Confidence / Production Change Confidence
- 10 entidades bem definidas para o domínio

---

### 7. PromotionDbContext

| Propriedade | Valor |
|---|---|
| **Módulo** | `changegovernance` |
| **Base class** | `NexTraceDbContextBase` |
| **DbSets** | 4 |
| **ConfigurationsNamespace** | `NexTraceOne.Modules.ChangeGovernance.Infrastructure.Persistence.Configurations.Promotion` |
| **Migrations** | 1 (InitialCreate) |
| **Tenant/Audit/Encryption** | ✅ / ✅ / ✅ |
| **Classificação** | **FRAGMENTED** |

**Entidades (4 DbSets):**

| # | Entidade | Responsabilidade |
|---|---|---|
| 1 | PromotionRequest | Pedidos de promoção entre ambientes |
| 2 | PromotionApproval | Aprovações de promoção |
| 3 | PromotionSLA | SLAs de promoção |
| 4 | PromotionHistory | Histórico de promoções |

**Observações:**
- Apenas 4 entidades — potencialmente poderia ser integrado com ChangeIntelligence
- Separação justificada se workflow de promoção for independente

---

### 8. WorkflowDbContext

| Propriedade | Valor |
|---|---|
| **Módulo** | `changegovernance` |
| **Base class** | `NexTraceDbContextBase` |
| **DbSets** | 6 |
| **ConfigurationsNamespace** | `NexTraceOne.Modules.ChangeGovernance.Infrastructure.Persistence.Configurations.Workflow` |
| **Migrations** | 1 (InitialCreate) |
| **Tenant/Audit/Encryption** | ✅ / ✅ / ✅ |
| **Classificação** | **COHESIVE** |

**Entidades (6 DbSets):**

| # | Entidade | Responsabilidade |
|---|---|---|
| 1 | WorkflowTemplate | Templates de workflow |
| 2 | WorkflowInstance | Instâncias em execução |
| 3 | WorkflowStage | Etapas do workflow |
| 4 | WorkflowEvidence | Evidências por etapa |
| 5 | WorkflowApproval | Aprovações por etapa |
| 6 | WorkflowTransition | Transições de estado |

**Observações:**
- Modelo de workflow genérico e reutilizável
- Suporta approval workflows para contratos e promoções

---

### 9. RulesetGovernanceDbContext

| Propriedade | Valor |
|---|---|
| **Módulo** | `changegovernance` |
| **Base class** | `NexTraceDbContextBase` |
| **DbSets** | 3 |
| **ConfigurationsNamespace** | `NexTraceOne.Modules.ChangeGovernance.Infrastructure.Persistence.Configurations.Ruleset` |
| **Migrations** | 1 (InitialCreate) |
| **Tenant/Audit/Encryption** | ✅ / ✅ / ✅ |
| **Classificação** | **FRAGMENTED** |

**Entidades (3 DbSets):**

| # | Entidade | Responsabilidade |
|---|---|---|
| 1 | Ruleset | Conjuntos de regras |
| 2 | Rule | Regras individuais |
| 3 | RuleCondition | Condições de regras |

**Observações:**
- Apenas 3 entidades — candidato a fusão com GovernanceDbContext ou WorkflowDbContext
- Separação pode ser justificada se rulesets tiverem ciclo de vida independente

---

### 10. ConfigurationDbContext

| Propriedade | Valor |
|---|---|
| **Módulo** | `configuration` |
| **Base class** | `NexTraceDbContextBase` |
| **DbSets** | 3 |
| **ConfigurationsNamespace** | `NexTraceOne.Modules.Configuration.Infrastructure.Persistence.Configurations` |
| **Migrations** | **0** ❌ (usa `EnsureCreated`) |
| **Tenant/Audit/Encryption** | ✅ / ✅ / ✅ |
| **Classificação** | **FRAGMENTED** |

**Entidades (3 DbSets):**

| # | Entidade | Responsabilidade |
|---|---|---|
| 1 | ConfigurationDefinition | Definições de configuração |
| 2 | ConfigurationValue | Valores de configuração |
| 3 | ConfigurationOverride | Overrides por tenant/ambiente |

**Observações:**
- ❌ **Sem migrations** — usa `EnsureCreated`, que não suporta evolução incremental
- C# seeder `ConfigurationDefinitionSeeder` com 345+ definitions
- Risco: alteração de schema requer drop/recreate da tabela

---

### 11. GovernanceDbContext

| Propriedade | Valor |
|---|---|
| **Módulo** | `governance` |
| **Base class** | `NexTraceDbContextBase` |
| **DbSets** | 12 |
| **ConfigurationsNamespace** | `NexTraceOne.Modules.Governance.Infrastructure.Persistence.Configurations` |
| **Migrations** | 3 (InitialCreate, Phase5Enrichment, AddLastProcessedAt) |
| **Tenant/Audit/Encryption** | ✅ / ✅ / ✅ |
| **Classificação** | **PARTIAL** |

**Entidades (12 DbSets):**

| # | Entidade | Responsabilidade |
|---|---|---|
| 1 | GovernancePolicy | Políticas de governança |
| 2 | GovernanceStandard | Standards/padrões |
| 3 | PolicyRule | Regras de política |
| 4 | PolicyViolation | Violações de política |
| 5 | PolicyException | Exceções aprovadas |
| 6 | ComplianceReport | Relatórios de conformidade |
| 7 | GovernanceMetric | Métricas de governança |
| 8 | GovernanceDashboard | Configurações de dashboard |
| 9 | GovernanceAlert | Alertas de governança |
| 10 | GovernanceReview | Reviews de governança |
| 11 | GovernanceTag | Tags de categorização |
| 12 | GovernanceSchedule | Agendamentos de revisão |

**Observações:**
- 12 entidades — é o segundo maior contexto em `nextraceone_operations`
- PARTIAL porque mistura políticas, compliance, dashboards e alertas
- Potencial sobreposição com AuditDbContext (ComplianceReport)

---

### 12. NotificationsDbContext

| Propriedade | Valor |
|---|---|
| **Módulo** | `notifications` |
| **Base class** | `NexTraceDbContextBase` |
| **DbSets** | 3 |
| **ConfigurationsNamespace** | `NexTraceOne.Modules.Notifications.Infrastructure.Persistence.Configurations` |
| **Migrations** | **0** ❌ (usa `EnsureCreated`) |
| **Tenant/Audit/Encryption** | ✅ / ✅ / ✅ |
| **Classificação** | **FRAGMENTED** |

**Entidades (3 DbSets):**

| # | Entidade | Responsabilidade |
|---|---|---|
| 1 | NotificationTemplate | Templates de notificação |
| 2 | NotificationChannel | Canais (email, Slack, webhook) |
| 3 | NotificationDelivery | Entregas/envios |

**Observações:**
- ❌ **Sem migrations** — mesmo problema do ConfigurationDbContext
- Apenas 3 entidades — FRAGMENTED

---

### 13. CostIntelligenceDbContext

| Propriedade | Valor |
|---|---|
| **Módulo** | `operationalintelligence` |
| **Base class** | `NexTraceDbContextBase` |
| **DbSets** | 6 |
| **ConfigurationsNamespace** | `NexTraceOne.Modules.OperationalIntelligence.Infrastructure.Persistence.Configurations.Cost` |
| **Migrations** | 2 (InitialCreate, AddCostImportPipeline) |
| **Tenant/Audit/Encryption** | ✅ / ✅ / ✅ |
| **Classificação** | **COHESIVE** |

**Entidades (6 DbSets):**

| # | Entidade | Responsabilidade |
|---|---|---|
| 1 | CostAllocation | Alocação de custos por serviço |
| 2 | CostImportPipeline | Pipeline de importação de custos |
| 3 | CostReport | Relatórios de custo |
| 4 | BudgetAlert | Alertas de orçamento |
| 5 | CostTag | Tags de custo |
| 6 | CostAnomaly | Anomalias de custo detetadas |

**Observações:**
- Suporta pilar FinOps contextual
- 2 migrations — evolução ativa

---

### 14. RuntimeIntelligenceDbContext

| Propriedade | Valor |
|---|---|
| **Módulo** | `operationalintelligence` |
| **Base class** | `NexTraceDbContextBase` |
| **DbSets** | 4 |
| **ConfigurationsNamespace** | `NexTraceOne.Modules.OperationalIntelligence.Infrastructure.Persistence.Configurations.Runtime` |
| **Migrations** | 1 (InitialCreate) |
| **Tenant/Audit/Encryption** | ✅ / ✅ / ✅ |
| **Classificação** | **FRAGMENTED** |

**Entidades (4 DbSets):**

| # | Entidade | Responsabilidade |
|---|---|---|
| 1 | RuntimeMetric | Métricas de runtime |
| 2 | RuntimeAlert | Alertas de runtime |
| 3 | RuntimeBaseline | Baselines de performance |
| 4 | RuntimeAnomaly | Anomalias detetadas |

---

### 15. IncidentDbContext

| Propriedade | Valor |
|---|---|
| **Módulo** | `operationalintelligence` |
| **Base class** | `NexTraceDbContextBase` |
| **DbSets** | 5 |
| **ConfigurationsNamespace** | `NexTraceOne.Modules.OperationalIntelligence.Infrastructure.Persistence.Configurations.Incident` |
| **Migrations** | 1 (InitialCreate) |
| **Tenant/Audit/Encryption** | ✅ / ✅ / ✅ |
| **Classificação** | **COHESIVE** |

**Entidades (5 DbSets):**

| # | Entidade | Responsabilidade |
|---|---|---|
| 1 | Incident | Incidentes |
| 2 | IncidentTimeline | Timeline de incidente |
| 3 | IncidentCorrelation | Correlações de incidente |
| 4 | Runbook | Runbooks operacionais |
| 5 | RunbookExecution | Execuções de runbook |

---

### 16. AutomationDbContext

| Propriedade | Valor |
|---|---|
| **Módulo** | `operationalintelligence` |
| **Base class** | `NexTraceDbContextBase` |
| **DbSets** | 3 |
| **ConfigurationsNamespace** | `NexTraceOne.Modules.OperationalIntelligence.Infrastructure.Persistence.Configurations.Automation` |
| **Migrations** | 1 (InitialCreate) |
| **Tenant/Audit/Encryption** | ✅ / ✅ / ✅ |
| **Classificação** | **FRAGMENTED** |

**Entidades (3 DbSets):**

| # | Entidade | Responsabilidade |
|---|---|---|
| 1 | AutomationRule | Regras de automação |
| 2 | AutomationExecution | Execuções de automação |
| 3 | AutomationSchedule | Agendamentos |

---

### 17. ReliabilityDbContext

| Propriedade | Valor |
|---|---|
| **Módulo** | `operationalintelligence` |
| **Base class** | `NexTraceDbContextBase` |
| **DbSets** | 1 |
| **ConfigurationsNamespace** | `NexTraceOne.Modules.OperationalIntelligence.Infrastructure.Persistence.Configurations.Reliability` |
| **Migrations** | 1 (InitialCreate) |
| **Tenant/Audit/Encryption** | ✅ / ✅ / ✅ |
| **Classificação** | **FRAGMENTED** |

**Entidades (1 DbSet):**

| # | Entidade | Responsabilidade |
|---|---|---|
| 1 | SLODefinition | Definições de SLO |

**Observações:**
- ⚠️ Apenas 1 entidade — extremamente fragmentado
- Candidato forte a fusão com RuntimeIntelligenceDbContext ou IncidentDbContext

---

## Base de Dados: `nextraceone_ai`

### 18. AiGovernanceDbContext

| Propriedade | Valor |
|---|---|
| **Módulo** | `aiknowledge` |
| **Base class** | `NexTraceDbContextBase` |
| **DbSets** | 19+ |
| **ConfigurationsNamespace** | `NexTraceOne.Modules.AiKnowledge.Infrastructure.Persistence.Configurations.Governance` |
| **Migrations** | 7 (inclui TenantId fixes — dívida técnica) |
| **Tenant/Audit/Encryption** | ✅ / ✅ / ✅ |
| **Classificação** | **OVERLOADED** |

**Entidades (19+ DbSets):**

| # | Entidade | Responsabilidade |
|---|---|---|
| 1 | AiProvider | Providers de IA (OpenAI, Azure, local) |
| 2 | AiModel | Modelos registados |
| 3 | AiAgent | Agentes de IA |
| 4 | AiAgentCapability | Capacidades de agentes |
| 5 | AiAgentTool | Ferramentas de agentes |
| 6 | AiAccessPolicy | Políticas de acesso a IA |
| 7 | AiTokenBudget | Budgets de tokens |
| 8 | AiTokenUsage | Uso de tokens |
| 9 | AiAuditEntry | Auditoria de IA |
| 10 | AiModelVersion | Versões de modelos |
| 11 | AiModelEvaluation | Avaliações de modelos |
| 12 | AiKnowledgeSource | Fontes de conhecimento |
| 13 | AiKnowledgeIndex | Índices de conhecimento |
| 14 | AiPromptTemplate | Templates de prompts |
| 15 | AiPromptVersion | Versões de prompts |
| 16 | AiGuardrail | Guardrails de IA |
| 17 | AiGuardrailViolation | Violações de guardrails |
| 18 | AiExperiment | Experiências/A-B testing |
| 19 | AiExperimentResult | Resultados de experiências |

**Observações:**
- ⚠️ **OVERLOADED** — 19+ entidades é excessivo para um único DbContext
- 7 migrations incluem fixes de TenantId (Guid→UUID) — dívida técnica acumulada
- Candidato a split: separar Governance, Knowledge e Experimentation

---

### 19. AiOrchestrationDbContext

| Propriedade | Valor |
|---|---|
| **Módulo** | `aiknowledge` |
| **Base class** | `NexTraceDbContextBase` |
| **DbSets** | 4 |
| **ConfigurationsNamespace** | `NexTraceOne.Modules.AiKnowledge.Infrastructure.Persistence.Configurations.Orchestration` |
| **Migrations** | 1 (InitialCreate) |
| **Tenant/Audit/Encryption** | ✅ / ✅ / ✅ |
| **Classificação** | **COHESIVE** |

**Entidades (4 DbSets):**

| # | Entidade | Responsabilidade |
|---|---|---|
| 1 | Conversation | Conversas de IA |
| 2 | ConversationMessage | Mensagens em conversas |
| 3 | ConversationContext | Contexto associado |
| 4 | ConversationFeedback | Feedback do utilizador |

---

### 20. ExternalAiDbContext

| Propriedade | Valor |
|---|---|
| **Módulo** | `aiknowledge` |
| **Base class** | `NexTraceDbContextBase` |
| **DbSets** | 4 |
| **ConfigurationsNamespace** | `NexTraceOne.Modules.AiKnowledge.Infrastructure.Persistence.Configurations.External` |
| **Migrations** | 1 (InitialCreate) |
| **Tenant/Audit/Encryption** | ✅ / ✅ / ✅ |
| **Classificação** | **COHESIVE** |

**Entidades (4 DbSets):**

| # | Entidade | Responsabilidade |
|---|---|---|
| 1 | ExternalAiIntegration | Integrações com IA externa |
| 2 | ExternalAiRequest | Requests a IAs externas |
| 3 | ExternalAiResponse | Responses de IAs externas |
| 4 | ExternalAiPolicy | Políticas para IA externa |

---

## Resumo de Classificação

| Classificação | DbContexts | Quantidade |
|---|---|---|
| **COHESIVE** | Identity, Audit, Contracts, CatalogGraph, DeveloperPortal, ChangeIntelligence, Workflow, CostIntelligence, Incident, AiOrchestration, ExternalAi | 11 |
| **PARTIAL** | Governance | 1 |
| **OVERLOADED** | AiGovernance | 1 |
| **FRAGMENTED** | Promotion, RulesetGovernance, Configuration, Notifications, RuntimeIntelligence, Automation, Reliability | 7 |

### Recomendações de Consolidação

| Ação | Detalhe |
|---|---|
| Fundir ReliabilityDb → RuntimeIntelligenceDb | 1 entidade não justifica DbContext separado |
| Fundir RulesetGovernanceDb → GovernanceDb ou WorkflowDb | 3 entidades de rulesets são tangenciais |
| Avaliar split AiGovernanceDb | 19+ entidades — separar em Governance, Knowledge, Experimentation |
| Migrar Configuration + Notifications para migrations | Eliminar `EnsureCreated` |

---

## Distribuição por Base de Dados

```
nextraceone_identity (2 DbContexts, 22 DbSets)
├── IdentityDbContext .......... 16 DbSets  [COHESIVE]
└── AuditDbContext .............. 6 DbSets  [COHESIVE]

nextraceone_catalog (3 DbContexts, 20 DbSets)
├── ContractsDbContext .......... 7 DbSets  [COHESIVE]
├── CatalogGraphDbContext ....... 8 DbSets  [COHESIVE]
└── DeveloperPortalDbContext .... 5 DbSets  [COHESIVE]

nextraceone_operations (12 DbContexts, ~60 DbSets)
├── ChangeIntelligenceDbContext . 10 DbSets [COHESIVE]
├── GovernanceDbContext ........ 12 DbSets  [PARTIAL]
├── WorkflowDbContext ........... 6 DbSets  [COHESIVE]
├── CostIntelligenceDbContext ... 6 DbSets  [COHESIVE]
├── IncidentDbContext ........... 5 DbSets  [COHESIVE]
├── PromotionDbContext .......... 4 DbSets  [FRAGMENTED]
├── RuntimeIntelligenceDbContext  4 DbSets  [FRAGMENTED]
├── ConfigurationDbContext ...... 3 DbSets  [FRAGMENTED] ❌ sem migrations
├── NotificationsDbContext ...... 3 DbSets  [FRAGMENTED] ❌ sem migrations
├── RulesetGovernanceDbContext .. 3 DbSets  [FRAGMENTED]
├── AutomationDbContext ......... 3 DbSets  [FRAGMENTED]
└── ReliabilityDbContext ........ 1 DbSet   [FRAGMENTED]

nextraceone_ai (3 DbContexts, 27+ DbSets)
├── AiGovernanceDbContext ...... 19+ DbSets [OVERLOADED] ⚠️
├── AiOrchestrationDbContext .... 4 DbSets  [COHESIVE]
└── ExternalAiDbContext ......... 4 DbSets  [COHESIVE]
```

---

*Relatório gerado como parte da auditoria modular de governança do NexTraceOne.*
