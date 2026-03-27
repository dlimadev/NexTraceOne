# Alinhamento Domínio vs. Schema de Base de Dados — NexTraceOne

> **Data:** 2025-01-XX  
> **Escopo:** Cruzamento entre modelo de domínio do produto e schema de persistência  
> **Método:** Análise estática de entidades, DbSets, entity configurations vs. pilares e módulos oficiais  
> **Objetivo:** Verificar se o schema de base de dados suporta a visão do NexTraceOne como Source of Truth

---

## Resumo Executivo

| Classificação | Módulos | Quantidade |
|---|---|---|
| **ALIGNED** | Identity, Contracts, Change Intelligence, AI Governance | 4 |
| **PARTIALLY_ALIGNED** | Catalog/Graph, Audit/Compliance, Workflow, Incidents | 4 |
| **WEAK_ALIGNMENT** | FinOps/Cost, Notifications, Configuration, Reliability | 4 |
| **MISALIGNED** | Publication Center, Risk Center, IDE Extensions, Operational Notes | 4 |

### Cobertura por Pilar do Produto

| Pilar | Schema Existente | Classificação |
|---|---|---|
| Service Governance | ServiceDefinition, Ownership, Dependencies, Health | ✅ ALIGNED |
| Contract Governance | ApiContract, SoapContract, EventContract, BackgroundServiceContract, Versions | ✅ ALIGNED |
| Change Confidence | Release, BlastRadius, ChangeScore, Promotion, Validation | ✅ ALIGNED |
| Operational Reliability | Incident, Runbook, SLODefinition, AutomationRule | ⚠️ PARTIALLY_ALIGNED |
| AI-assisted Operations | AiAgent, Conversation, AiModel, AiProvider | ✅ ALIGNED |
| Source of Truth | GovernancePolicy, Standard, ComplianceReport | ⚠️ PARTIALLY_ALIGNED |
| AI Governance | AiAccessPolicy, TokenBudget, AuditEntry, Guardrail | ✅ ALIGNED |
| FinOps | CostAllocation, BudgetAlert, CostAnomaly | ⚠️ WEAK_ALIGNMENT |

---

## Análise Detalhada por Módulo

### 1. Identity & Access — `identityaccess`

**Classificação: ✅ ALIGNED**

| Conceito de Domínio | Entidade DB | Estado |
|---|---|---|
| Utilizador | `User` | ✅ Existe |
| Role/Papel | `Role` | ✅ Existe |
| Permissão | `Permission` | ✅ Existe |
| Role↔Permission | `RolePermission` | ✅ Existe |
| User↔Role | `UserRole` | ✅ Existe |
| Tenant/Organização | `Tenant` | ✅ Existe |
| Membro de tenant | `TenantMember` | ✅ Existe |
| Equipa | `Team` | ✅ Existe |
| Membro de equipa | `TeamMember` | ✅ Existe |
| Ambiente (dev/staging/prod) | `EnvironmentProfile` | ✅ Existe |
| Chave de API | `ApiKey` | ✅ Existe |
| Sessão | `Session` | ✅ Existe |
| Token de refresh | `RefreshToken` | ✅ Existe |
| Token de convite | `InviteToken` | ✅ Existe |
| Reset de password | `PasswordResetToken` | ✅ Existe |

**Value Objects:** Email, FullName (owned), HashedPassword  
**Strongly-typed IDs:** UserId, TenantId, TeamId, RoleId, PermissionId, EnvironmentId

**Lacunas:**
- Sem modelo de grupo/departamento (apenas Teams)
- Sem modelo de persona funcional (Engineer, Tech Lead, etc.) — persona é derivada, não persistida

**Evidência:** `src/modules/identityaccess/Infrastructure/Persistence/`

---

### 2. Audit & Compliance — `auditcompliance`

**Classificação: ⚠️ PARTIALLY_ALIGNED**

| Conceito de Domínio | Entidade DB | Estado |
|---|---|---|
| Evento de auditoria | `AuditEvent` | ✅ Existe |
| Cadeia de auditoria | `AuditChainLink` | ✅ Existe |
| Relatório de compliance | `ComplianceReport` | ✅ Existe |
| Regra de compliance | `ComplianceRule` | ✅ Existe |
| Violação | `ComplianceViolation` | ✅ Existe |
| Snapshot de compliance | `ComplianceSnapshot` | ✅ Existe |
| Política de retenção | — | ❌ Não existe |
| Export/archive de auditoria | — | ❌ Não existe |
| Compliance pack | — | ❌ Não existe |

**Notas:**
- Schema sólido para audit trail e compliance
- Falta modelo de retenção de dados (GDPR, legal)
- Falta conceito de compliance packs pré-definidos (SOC2, ISO27001)
- Sobreposição potencial: `ComplianceReport` também existe em GovernanceDbContext

---

### 3. Catalog — Contratos — `catalog`

**Classificação: ✅ ALIGNED**

| Conceito de Domínio | Entidade DB | Estado |
|---|---|---|
| Contrato REST API | `ApiContract` | ✅ Existe |
| Contrato SOAP | `SoapContract` | ✅ Existe |
| Contrato de evento | `EventContract` | ✅ Existe |
| Contrato background service | `BackgroundServiceContract` | ✅ Existe |
| Versão de contrato | `ContractVersion` | ✅ Existe (com Signature, Provenance) |
| Schema | `ContractSchema` | ✅ Existe |
| Política de contrato | `ContractPolicy` | ✅ Existe |
| Diff de contrato | — | ❌ Não persistido (computado) |
| Compatibilidade de contrato | — | ❌ Não persistida (computada) |
| Publicação de contrato | — | ❌ Não existe (MISALIGNED com Publication Center) |
| Approval workflow de contrato | — | ⚠️ Via WorkflowDbContext (indireto) |

**Notas:**
- 4 tipos de contrato como first-class citizens — alinhado com visão do produto
- Owned entities (Signature, Provenance) demonstram maturidade do modelo
- Falta persistência para diff results, compatibility checks e publication state

---

### 4. Catalog — Grafo de Serviços — `catalog`

**Classificação: ⚠️ PARTIALLY_ALIGNED**

| Conceito de Domínio | Entidade DB | Estado |
|---|---|---|
| Definição de serviço | `ServiceDefinition` | ✅ Existe |
| Dependência de serviço | `ServiceDependency` | ✅ Existe |
| Endpoint de serviço | `ServiceEndpoint` | ✅ Existe |
| Ownership por equipa | `ServiceOwnership` | ✅ Existe |
| Tags de serviço | `ServiceTag` | ✅ Existe |
| Estado de saúde | `ServiceHealth` | ✅ Existe |
| Aresta de dependência | `DependencyEdge` | ✅ Existe |
| Snapshot de topologia | `TopologySnapshot` | ✅ Existe |
| Criticidade de serviço | — | ❌ Não existe como campo/entidade dedicada |
| SLA de serviço | — | ❌ Referência parcial via ReliabilityDb (SLODefinition) |
| Lifecycle de serviço | — | ❌ Não existe (Service Lifecycle) |

**Notas:**
- Grafo de dependências é forte (8 entidades)
- Falta modelo de criticidade e lifecycle de serviço
- SLODefinition está isolada em ReliabilityDbContext (1 entidade) — fraca integração

---

### 5. Change Governance — Change Intelligence — `changegovernance`

**Classificação: ✅ ALIGNED**

| Conceito de Domínio | Entidade DB | Estado |
|---|---|---|
| Release/Deploy | `Release` | ✅ Existe |
| Alteração em release | `ReleaseChange` | ✅ Existe |
| Blast radius | `BlastRadius` | ✅ Existe |
| Score de confiança | `ChangeScore` | ✅ Existe |
| Validação pós-change | `ChangeValidation` | ✅ Existe |
| Correlação change↔incident | `ChangeCorrelation` | ✅ Existe |
| Evidência | `ChangeEvidence` | ✅ Existe |
| Métrica de mudança | `ChangeMetric` | ✅ Existe |
| Timeline | `ChangeTimeline` | ✅ Existe |
| Avaliação de impacto | `ChangeImpactAssessment` | ✅ Existe |

**Notas:**
- Modelo mais completo do sistema (10 entidades dedicadas)
- Blast radius + score + correlação = Production Change Confidence real
- Alinhamento forte com pilar central do produto

---

### 6. Change Governance — Workflows — `changegovernance`

**Classificação: ⚠️ PARTIALLY_ALIGNED**

| Conceito de Domínio | Entidade DB | Estado |
|---|---|---|
| Template de workflow | `WorkflowTemplate` | ✅ Existe |
| Instância de workflow | `WorkflowInstance` | ✅ Existe |
| Etapa | `WorkflowStage` | ✅ Existe |
| Evidência por etapa | `WorkflowEvidence` | ✅ Existe |
| Aprovação | `WorkflowApproval` | ✅ Existe |
| Transição | `WorkflowTransition` | ✅ Existe |
| SLA de workflow | — | ⚠️ Via PromotionSLA (indireto) |
| Notificação de workflow | — | ❌ Não integrado com NotificationsDb |

**Notas:**
- Modelo genérico de workflow — reutilizável para contratos e promoções
- Falta integração explícita com notificações

---

### 7. Change Governance — Promoções — `changegovernance`

**Classificação: ⚠️ PARTIALLY_ALIGNED**

| Conceito de Domínio | Entidade DB | Estado |
|---|---|---|
| Pedido de promoção | `PromotionRequest` | ✅ Existe |
| Aprovação | `PromotionApproval` | ✅ Existe |
| SLA | `PromotionSLA` | ✅ Existe |
| Histórico | `PromotionHistory` | ✅ Existe |
| Evidência de promoção | — | ❌ Falta (usa WorkflowEvidence?) |
| Rollback | — | ❌ Não modelado |

---

### 8. Governance — `governance`

**Classificação: ⚠️ PARTIALLY_ALIGNED**

| Conceito de Domínio | Entidade DB | Estado |
|---|---|---|
| Política de governança | `GovernancePolicy` | ✅ Existe |
| Standard/Padrão | `GovernanceStandard` | ✅ Existe |
| Regra de política | `PolicyRule` | ✅ Existe |
| Violação | `PolicyViolation` | ✅ Existe |
| Exceção aprovada | `PolicyException` | ✅ Existe |
| Compliance report | `ComplianceReport` | ✅ Existe (duplicado com AuditDb) |
| Métrica | `GovernanceMetric` | ✅ Existe |
| Dashboard | `GovernanceDashboard` | ✅ Existe |
| Alerta | `GovernanceAlert` | ✅ Existe |
| Review | `GovernanceReview` | ✅ Existe |
| Risk score | — | ❌ Não existe (Risk Center) |
| Risk mitigation | — | ❌ Não existe |

**Notas:**
- 12 entidades cobrem governança geral, mas falta modelo de risco dedicado
- `ComplianceReport` duplicado com AuditDbContext — possível sobreposição

---

### 9. AI Knowledge — IA Governança — `aiknowledge`

**Classificação: ✅ ALIGNED**

| Conceito de Domínio | Entidade DB | Estado |
|---|---|---|
| Provider de IA | `AiProvider` | ✅ Existe |
| Modelo de IA | `AiModel` + `AiModelVersion` | ✅ Existe |
| Agente | `AiAgent` + Capabilities + Tools | ✅ Existe |
| Política de acesso | `AiAccessPolicy` | ✅ Existe |
| Budget de tokens | `AiTokenBudget` + `AiTokenUsage` | ✅ Existe |
| Auditoria de IA | `AiAuditEntry` | ✅ Existe |
| Fonte de conhecimento | `AiKnowledgeSource` + Index | ✅ Existe |
| Prompt template | `AiPromptTemplate` + Version | ✅ Existe |
| Guardrails | `AiGuardrail` + Violation | ✅ Existe |
| Experiências A/B | `AiExperiment` + Result | ✅ Existe |
| Model registry | Via AiModel/Version/Provider | ✅ Implícito |

**Notas:**
- Módulo mais completo em relação à visão do produto
- 19+ entidades cobrem todos os requisitos de AI Governance
- Falta: integração IDE (extensões VS Code/Visual Studio)

---

### 10. AI Knowledge — Orquestração — `aiknowledge`

**Classificação: ⚠️ PARTIALLY_ALIGNED**

| Conceito de Domínio | Entidade DB | Estado |
|---|---|---|
| Conversa | `Conversation` | ✅ Existe |
| Mensagem | `ConversationMessage` | ✅ Existe |
| Contexto | `ConversationContext` | ✅ Existe |
| Feedback | `ConversationFeedback` | ✅ Existe |
| Chain-of-thought | — | ❌ Não persistido |
| Tool execution log | — | ❌ Não persistido |

---

### 11. Operational Intelligence — Custos (FinOps) — `operationalintelligence`

**Classificação: ⚠️ WEAK_ALIGNMENT**

| Conceito de Domínio | Entidade DB | Estado |
|---|---|---|
| Alocação de custo | `CostAllocation` | ✅ Existe |
| Pipeline de importação | `CostImportPipeline` | ✅ Existe |
| Relatório de custo | `CostReport` | ✅ Existe |
| Alerta de budget | `BudgetAlert` | ✅ Existe |
| Tag de custo | `CostTag` | ✅ Existe |
| Anomalia | `CostAnomaly` | ✅ Existe |
| Custo por serviço contextualizado | — | ⚠️ Via CostAllocation (parcial) |
| Custo por equipa | — | ❌ Não modelado diretamente |
| Custo por operação | — | ❌ Não modelado |
| Custo por mudança | — | ❌ Não modelado |
| Desperdício operacional | — | ❌ Não modelado |

**Notas:**
- Modelo base existe mas falta contextualização por equipa, operação e mudança
- NexTraceOne define FinOps como contextualizado — o schema atual é genérico
- Classificação WEAK porque a estrutura não suporta totalmente a narrativa de FinOps contextual

---

### 12. Operational Intelligence — Incidentes — `operationalintelligence`

**Classificação: ⚠️ PARTIALLY_ALIGNED**

| Conceito de Domínio | Entidade DB | Estado |
|---|---|---|
| Incidente | `Incident` | ✅ Existe |
| Timeline de incidente | `IncidentTimeline` | ✅ Existe |
| Correlação | `IncidentCorrelation` | ✅ Existe |
| Runbook | `Runbook` | ✅ Existe |
| Execução de runbook | `RunbookExecution` | ✅ Existe |
| Post-mortem | — | ❌ Não modelado |
| Ação de mitigação | — | ❌ Não modelado |
| Impacto de incidente | — | ❌ Não modelado |

---

### 13. Operational Intelligence — Runtime — `operationalintelligence`

**Classificação: ⚠️ WEAK_ALIGNMENT**

| Conceito de Domínio | Entidade DB | Estado |
|---|---|---|
| Métrica de runtime | `RuntimeMetric` | ✅ Existe |
| Alerta | `RuntimeAlert` | ✅ Existe |
| Baseline | `RuntimeBaseline` | ✅ Existe |
| Anomalia | `RuntimeAnomaly` | ✅ Existe |
| Correlação com serviço | — | ❌ Fraca integração |
| Correlação com mudança | — | ❌ Não modelada |

---

### 14. Operational Intelligence — Reliability — `operationalintelligence`

**Classificação: ⚠️ WEAK_ALIGNMENT**

| Conceito de Domínio | Entidade DB | Estado |
|---|---|---|
| Definição de SLO | `SLODefinition` | ✅ Existe |
| Error budget | — | ❌ Não modelado |
| SLI (indicators) | — | ❌ Não modelado |
| Burn rate | — | ❌ Não modelado |
| SLO breach | — | ❌ Não modelado |

**Notas:**
- Apenas 1 entidade para todo o domínio de Reliability — muito insuficiente
- Faltam SLI, error budget, burn rate, breach alerts

---

## Módulos Sem Representação no Schema (MISALIGNED)

| Módulo/Funcionalidade | Estado | Impacto |
|---|---|---|
| **Publication Center** | Sem entidade de publicação de contrato | Alto — pilar Contract Governance |
| **Risk Center** | Sem modelo de risco dedicado | Médio — parcialmente coberto por Governance |
| **IDE Extensions Management** | Sem persistência | Médio — pilar AI Governance |
| **Operational Notes** | Sem entidade específica | Baixo — pode usar Knowledge Hub |
| **Service Lifecycle** | Sem modelo de lifecycle | Médio — pilar Service Governance |
| **Compliance Packs** | Sem modelo pré-definido | Médio — necessário para onboarding |
| **Knowledge Hub** | Sem entidades dedicadas | Médio — pilar Source of Truth |

---

## Matriz de Alinhamento Geral

| Módulo | DbContext(s) | DbSets | Alinhamento | Lacunas Principais |
|---|---|---|---|---|
| Identity | IdentityDb | 16 | ✅ ALIGNED | Persona funcional |
| Audit | AuditDb | 6 | ⚠️ PARTIAL | Retenção, compliance packs |
| Contracts | ContractsDb | 7 | ✅ ALIGNED | Publication, diff persistence |
| Catalog Graph | CatalogGraphDb | 8 | ⚠️ PARTIAL | Criticidade, lifecycle |
| Developer Portal | DeveloperPortalDb | 5 | ⚠️ PARTIAL | Conteúdo dinâmico |
| Change Intelligence | ChangeIntelDb | 10 | ✅ ALIGNED | — |
| Workflow | WorkflowDb | 6 | ⚠️ PARTIAL | Notificação integrada |
| Promotion | PromotionDb | 4 | ⚠️ PARTIAL | Rollback, evidência |
| Ruleset | RulesetGovDb | 3 | ⚠️ PARTIAL | Integração fraca |
| Configuration | ConfigurationDb | 3 | ⚠️ WEAK | Sem migrations |
| Governance | GovernanceDb | 12 | ⚠️ PARTIAL | Risk, duplicação |
| Notifications | NotificationsDb | 3 | ⚠️ WEAK | Sem migrations |
| AI Governance | AiGovernanceDb | 19+ | ✅ ALIGNED | IDE extensions |
| AI Orchestration | AiOrchDb | 4 | ⚠️ PARTIAL | Chain-of-thought, tool log |
| External AI | ExternalAiDb | 4 | ✅ ALIGNED | — |
| Cost Intelligence | CostIntelDb | 6 | ⚠️ WEAK | Contextualização |
| Runtime Intelligence | RuntimeIntelDb | 4 | ⚠️ WEAK | Correlação |
| Incidents | IncidentDb | 5 | ⚠️ PARTIAL | Post-mortem, mitigação |
| Automation | AutomationDb | 3 | ⚠️ WEAK | Integração fraca |
| Reliability | ReliabilityDb | 1 | ⚠️ WEAK | SLI, error budget, burn rate |

---

## Recomendações de Alinhamento

### Prioridade Alta — Pilares Centrais

| # | Ação | Pilar Afetado |
|---|---|---|
| 1 | Criar modelo de Publication Center (publicação de contratos) | Contract Governance |
| 2 | Expandir Reliability: SLI, error budget, burn rate, breach | Operational Reliability |
| 3 | Contextualizar FinOps por equipa, operação e mudança | FinOps |
| 4 | Criar modelo de Service Lifecycle | Service Governance |

### Prioridade Média — Completude

| # | Ação | Pilar Afetado |
|---|---|---|
| 5 | Adicionar persistência de diff de contrato | Contract Governance |
| 6 | Adicionar post-mortem e mitigação a Incidents | Operational Reliability |
| 7 | Criar modelo de Risk Score e Risk Mitigation | Governance |
| 8 | Adicionar Knowledge Hub entities | Source of Truth |

### Prioridade Baixa — Melhoria Contínua

| # | Ação | Pilar Afetado |
|---|---|---|
| 9 | Adicionar IDE Extensions management | AI Governance |
| 10 | Eliminar duplicação ComplianceReport entre Audit e Governance | Manutenibilidade |
| 11 | Adicionar chain-of-thought e tool execution log a AI Orchestration | AI Operations |

---

*Relatório gerado como parte da auditoria modular de governança do NexTraceOne.*
