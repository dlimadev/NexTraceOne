# Relatório de Suporte a IA, Agentes e Workflows — Base de Dados NexTraceOne

> **Data:** 2025-01-XX  
> **Escopo:** Persistência de IA (governance, orchestration, external), agentes, workflows e change intelligence  
> **Método:** Análise estática de DbContexts, entidades e entity configurations  
> **Foco:** Verificar se o schema suporta os pilares AI-assisted Operations, AI Governance e Change Confidence

---

## 1. Resumo Executivo

| Área | DbContext(s) | DbSets | Avaliação |
|---|---|---|---|
| AI Governance | AiGovernanceDbContext | 19+ | ✅ Forte (maior contexto do sistema) |
| AI Orchestration | AiOrchestrationDbContext | 4 | ⚠️ Adequado (faltam logs de execução) |
| External AI | ExternalAiDbContext | 4 | ✅ Forte |
| Workflows | WorkflowDbContext | 6 | ✅ Forte |
| Promotions | PromotionDbContext | 4 | ⚠️ Parcial |
| Change Intelligence | ChangeIntelligenceDbContext | 10 | ✅ Forte |
| Rulesets | RulesetGovernanceDbContext | 3 | ⚠️ Parcial |

### Classificação Global

| Pilar | Suporte DB | Maturidade |
|---|---|---|
| AI Governance & Developer Acceleration | ✅ Forte | Alta — 27+ entidades dedicadas |
| Change Confidence | ✅ Forte | Alta — 10 entidades dedicadas |
| Operational Reliability (Workflows) | ✅ Adequado | Média — modelo genérico reutilizável |
| Production Change Confidence | ✅ Forte | Alta — blast radius + scores |

---

## 2. IA — Governance (AiGovernanceDbContext)

### 2.1 Visão Geral

| Propriedade | Valor |
|---|---|
| Base de dados | `nextraceone_ai` |
| DbSets | 19+ (maior contexto do sistema) |
| Migrations | 7 (inclui dívida técnica) |
| Classificação | OVERLOADED — candidato a split |

### 2.2 Sub-domínios de IA no Schema

#### A. Provider & Model Registry

| Entidade | Responsabilidade | Alinhamento |
|---|---|---|
| `AiProvider` | Providers de IA (OpenAI, Azure, Ollama, local) | ✅ Model Registry |
| `AiModel` | Modelos registados por provider | ✅ Model Registry |
| `AiModelVersion` | Versões de modelo (tracking de evolução) | ✅ Model Registry |
| `AiModelEvaluation` | Avaliações de qualidade de modelo | ✅ Model Registry |

**Avaliação:** Suporta completamente o requisito de "Model Registry" do produto. Permite registar, versionar e avaliar modelos de IA de múltiplos providers.

#### B. Agent Execution Model

| Entidade | Responsabilidade | Alinhamento |
|---|---|---|
| `AiAgent` | Agentes de IA configurados | ✅ AI-assisted Operations |
| `AiAgentCapability` | Capacidades declaradas do agente | ✅ Agent definition |
| `AiAgentTool` | Ferramentas disponíveis ao agente | ✅ Tool integration |

**Avaliação:** Modelo de agente é extensível — capabilities e tools são entidades separadas, permitindo composição dinâmica. Falta:
- `AiAgentExecution` — registo de execuções de agente
- `AiAgentExecutionStep` — passos individuais de execução
- `AiAgentExecutionResult` — resultados de execução

#### C. Access Control & Governance

| Entidade | Responsabilidade | Alinhamento |
|---|---|---|
| `AiAccessPolicy` | Políticas de acesso a IA por user/group/role | ✅ AI Governance |
| `AiTokenBudget` | Budgets de tokens por user/group | ✅ Token & Budget Governance |
| `AiTokenUsage` | Registo de consumo de tokens | ✅ Usage tracking |
| `AiAuditEntry` | Auditoria de IA (prompts, respostas, contexto) | ✅ AI Audit & Usage |

**Avaliação:** Suporte completo para AI Governance. Cobre acesso, budgets, uso e auditoria.

#### D. Knowledge & Prompts

| Entidade | Responsabilidade | Alinhamento |
|---|---|---|
| `AiKnowledgeSource` | Fontes de conhecimento (docs, código, APIs) | ✅ AI Knowledge Sources |
| `AiKnowledgeIndex` | Índices de conhecimento (embeddings, search) | ✅ Knowledge indexing |
| `AiPromptTemplate` | Templates de prompts reutilizáveis | ✅ Prompt management |
| `AiPromptVersion` | Versões de prompts | ✅ Prompt versioning |

**Avaliação:** Knowledge sources e prompt management são essenciais para IA contextualizada. Modelo adequado.

#### E. Safety & Experimentation

| Entidade | Responsabilidade | Alinhamento |
|---|---|---|
| `AiGuardrail` | Guardrails de segurança da IA | ✅ AI safety |
| `AiGuardrailViolation` | Violações de guardrails registadas | ✅ Safety audit |
| `AiExperiment` | Experiências A/B de IA | ✅ Experimentation |
| `AiExperimentResult` | Resultados de experiências | ✅ Experiment tracking |

**Avaliação:** Guardrails e experimentation são features avançadas — indicam maturidade do modelo de IA.

### 2.3 Gaps do AiGovernanceDbContext

| Gap | Impacto | Prioridade |
|---|---|---|
| Sem `AiAgentExecution` (log de execuções) | Alto — não rastreia execuções de agentes | 🔴 Alta |
| Sem `AiAgentExecutionStep` | Médio — não rastreia passos individuais | 🟡 Média |
| Sem modelo de IDE Extensions | Médio — pilar AI Governance inclui IDE | 🟡 Média |
| Sem modelo de AI Feedback Loop | Baixo — improvement cycle não persistido | 🟢 Baixa |
| OVERLOADED (19+ entidades) | Médio — manutenibilidade degradada | 🟡 Média |

### 2.4 Proposta de Split

```
AiGovernanceDbContext (19+) → Split em:
├── AiRegistryDbContext (6)
│   ├── AiProvider
│   ├── AiModel
│   ├── AiModelVersion
│   ├── AiModelEvaluation
│   ├── AiAccessPolicy
│   └── AiTokenBudget
├── AiAgentDbContext (5+)
│   ├── AiAgent
│   ├── AiAgentCapability
│   ├── AiAgentTool
│   ├── AiAgentExecution (novo)
│   └── AiAgentExecutionStep (novo)
├── AiKnowledgeDbContext (4)
│   ├── AiKnowledgeSource
│   ├── AiKnowledgeIndex
│   ├── AiPromptTemplate
│   └── AiPromptVersion
└── AiSafetyDbContext (5+)
    ├── AiGuardrail
    ├── AiGuardrailViolation
    ├── AiExperiment
    ├── AiExperimentResult
    ├── AiTokenUsage
    └── AiAuditEntry
```

---

## 3. IA — Orchestration (AiOrchestrationDbContext)

### 3.1 Entidades

| Entidade | Responsabilidade | Avaliação |
|---|---|---|
| `Conversation` | Conversas de IA (sessões) | ✅ Core |
| `ConversationMessage` | Mensagens (user + assistant) | ✅ Core |
| `ConversationContext` | Contexto associado à conversa | ✅ Contextualização |
| `ConversationFeedback` | Feedback do utilizador | ✅ Quality tracking |

### 3.2 Gaps

| Gap | Impacto |
|---|---|
| Sem `ChainOfThought` | Não persiste raciocínio do agente |
| Sem `ToolExecutionLog` | Não persiste chamadas a tools |
| Sem `ConversationSummary` | Não persiste resumos de conversa longa |
| Sem `ConversationTag` | Não categoriza conversas |
| Sem referência a `AiAgent` | Conversa não sabe qual agente respondeu |

### 3.3 Recomendação

Adicionar ao AiOrchestrationDbContext:

| Entidade | Propósito |
|---|---|
| `ToolExecutionLog` | Rastrear chamadas a tools durante conversa |
| `ChainOfThought` | Persistir raciocínio/steps do agente |
| FK `AgentId` em `Conversation` | Associar conversa ao agente responsável |

---

## 4. IA — External (ExternalAiDbContext)

### 4.1 Entidades

| Entidade | Responsabilidade | Avaliação |
|---|---|---|
| `ExternalAiIntegration` | Integrações com IAs externas | ✅ Adequado |
| `ExternalAiRequest` | Requests enviados | ✅ Audit trail |
| `ExternalAiResponse` | Responses recebidos | ✅ Audit trail |
| `ExternalAiPolicy` | Políticas para IA externa | ✅ Governance |

### 4.2 Avaliação

Modelo simples mas adequado. Suporta:
- ✅ Integração controlada com IA externa
- ✅ Auditoria de requests/responses
- ✅ Políticas por integração
- ❌ Falta: rate limiting persistence, cost tracking por integração

---

## 5. Workflows (WorkflowDbContext)

### 5.1 Entidades

| Entidade | Responsabilidade | Uso Identificado |
|---|---|---|
| `WorkflowTemplate` | Templates reutilizáveis | Aprovação de contratos, promoções |
| `WorkflowInstance` | Instâncias em execução | Uma por processo de aprovação |
| `WorkflowStage` | Etapas do workflow | Review, Approve, Deploy, Validate |
| `WorkflowEvidence` | Evidências por etapa | Screenshots, logs, test results |
| `WorkflowApproval` | Aprovações por etapa | Quem aprovou, quando, comentário |
| `WorkflowTransition` | Transições de estado | Stage A → Stage B |

### 5.2 Casos de Uso Suportados

| Caso de Uso | Suporte | Detalhe |
|---|---|---|
| Aprovação de contrato | ✅ | Workflow com stages de review e approve |
| Promoção entre ambientes | ✅ | Via PromotionDb + WorkflowDb |
| Aprovação de política | ✅ | Workflow genérico aplicável |
| Aprovação de acesso a IA | ✅ | Workflow genérico aplicável |
| Rollback de deploy | ❌ | Não modelado |
| Escalation | ❌ | Não modelado |

### 5.3 Gaps

| Gap | Impacto |
|---|---|
| Sem SLA nativo no workflow (usa PromotionSLA) | Médio — SLA deveria ser do workflow |
| Sem escalation model | Médio — aprovações podem ficar pendentes |
| Sem notification hooks | Médio — não integra com NotificationsDb |
| Sem parallelism support | Baixo — stages são sequenciais |

---

## 6. Change Intelligence (ChangeIntelligenceDbContext)

### 6.1 Entidades

| Entidade | Responsabilidade | Pilar |
|---|---|---|
| `Release` | Releases/deploys registados | Change Confidence |
| `ReleaseChange` | Alterações individuais numa release | Change Intelligence |
| `BlastRadius` | Análise de impacto | Production Change Confidence |
| `ChangeScore` | Score de confiança (0-100) | Change Confidence |
| `ChangeValidation` | Validações pós-deploy | Change Validation |
| `ChangeCorrelation` | Correlação change↔incident | Change-to-Incident |
| `ChangeEvidence` | Evidências associadas | Auditoria |
| `ChangeMetric` | Métricas de mudança | Operational Intelligence |
| `ChangeTimeline` | Timeline de eventos da mudança | Visualization |
| `ChangeImpactAssessment` | Avaliação detalhada de impacto | Blast Radius |

### 6.2 Fluxo de Change Intelligence

```
Release criada
  → ReleaseChanges registados
    → BlastRadius calculado (serviços impactados)
      → ChangeScore atribuído (confiança)
        → ChangeValidation (pós-deploy)
          → ChangeCorrelation (se incidente ocorrer)
            → ChangeTimeline (visualização)
```

### 6.3 Avaliação

| Aspeto | Estado |
|---|---|
| Modelo de release | ✅ Completo |
| Blast radius | ✅ Integração com grafo de serviços |
| Score de confiança | ✅ Suporta decisão de go/no-go |
| Correlação com incidentes | ✅ Change-to-Incident |
| Validação pós-deploy | ✅ Feedback loop |
| Timeline | ✅ Visualização temporal |
| Rollback model | ❌ Não persistido |
| Canary/Blue-Green tracking | ❌ Não modelado |

### 6.4 Integração com Outros Módulos

| Módulo | Integração | Estado |
|---|---|---|
| Catalog (ServiceDefinition) | BlastRadius referencia serviços | ✅ Via ServiceId |
| Identity (EnvironmentProfile) | Release associada a ambiente | ✅ Via EnvironmentId |
| Incidents (Incident) | ChangeCorrelation | ✅ Via IncidentId |
| Workflows (WorkflowInstance) | Aprovação de release | ⚠️ Indireto |
| Promotion | Promoção de release | ✅ Via PromotionRequest |

---

## 7. Promotions (PromotionDbContext)

### 7.1 Entidades

| Entidade | Responsabilidade |
|---|---|
| `PromotionRequest` | Pedido de promoção entre ambientes |
| `PromotionApproval` | Aprovação do pedido |
| `PromotionSLA` | SLA para a promoção |
| `PromotionHistory` | Histórico de promoções |

### 7.2 Gaps

| Gap | Impacto |
|---|---|
| Sem rollback model | Alto — promoção não tem undo |
| Sem evidência específica (usa WorkflowEvidence) | Médio |
| Sem métricas de promoção | Baixo — tempos e sucesso rate |
| Sem integração explícita com ChangeScore | Médio — promoção deveria considerar score |

---

## 8. Rulesets (RulesetGovernanceDbContext)

### 8.1 Entidades

| Entidade | Responsabilidade |
|---|---|
| `Ruleset` | Conjunto de regras |
| `Rule` | Regra individual |
| `RuleCondition` | Condição de avaliação |

### 8.2 Avaliação

- Modelo mínimo mas funcional
- Rulesets são avaliados para determinar se uma mudança pode prosseguir
- ❌ Sem modelo de ação (o que fazer quando regra é violada)
- ❌ Sem histórico de avaliação (quando foi avaliado, resultado)
- ⚠️ Candidato a fusão com GovernanceDbContext

---

## 9. Visão Integrada — Fluxo Completo de IA + Change

```
Utilizador solicita mudança
  │
  ├─→ AI Agent analisa impacto (AiGovernanceDb)
  │     ├─→ Consulta Knowledge Sources
  │     ├─→ Consulta Topology (CatalogGraphDb)
  │     └─→ Gera recomendação
  │
  ├─→ Change Intelligence regista (ChangeIntelDb)
  │     ├─→ Blast Radius calculado
  │     └─→ Change Score atribuído
  │
  ├─→ Workflow de aprovação (WorkflowDb)
  │     ├─→ Stages: Review → Approve → Deploy
  │     └─→ Evidências recolhidas
  │
  ├─→ Promotion (se cross-environment) (PromotionDb)
  │     ├─→ SLA verificado
  │     └─→ Aprovação obtida
  │
  ├─→ Validation pós-deploy (ChangeIntelDb)
  │     └─→ Correlação com incidentes
  │
  └─→ Audit trail completo (AuditDb + AiAuditEntry)
```

---

## 10. Recomendações

### 🔴 Prioridade Alta

| # | Ação | Justificação |
|---|---|---|
| 1 | Adicionar `AiAgentExecution` + steps | Rastrear execuções de agentes |
| 2 | Adicionar FK `AgentId` em `Conversation` | Associar conversa ao agente |
| 3 | Adicionar `ToolExecutionLog` ao Orchestration | Auditoria de tool calls |

### 🟡 Prioridade Média

| # | Ação | Justificação |
|---|---|---|
| 4 | Avaliar split do AiGovernanceDbContext | 19+ entidades é excessivo |
| 5 | Adicionar rollback model a Promotions | Promoção sem undo é risco |
| 6 | Adicionar SLA nativo ao WorkflowDbContext | SLA não deveria depender de Promotion |
| 7 | Adicionar escalation model ao Workflow | Aprovações pendentes indefinidamente |

### 🟢 Prioridade Baixa

| # | Ação | Justificação |
|---|---|---|
| 8 | Adicionar canary/blue-green tracking | Change Intelligence avançado |
| 9 | Adicionar rate limiting persistence ao ExternalAi | Governança avançada |
| 10 | Adicionar histórico de avaliação a Rulesets | Auditoria de regras |

---

## Referências

| Artefacto | Localização |
|---|---|
| AiGovernanceDbContext | `src/modules/aiknowledge/Infrastructure/Persistence/AiGovernanceDbContext.cs` |
| AiOrchestrationDbContext | `src/modules/aiknowledge/Infrastructure/Persistence/AiOrchestrationDbContext.cs` |
| ExternalAiDbContext | `src/modules/aiknowledge/Infrastructure/Persistence/ExternalAiDbContext.cs` |
| WorkflowDbContext | `src/modules/changegovernance/Infrastructure/Persistence/WorkflowDbContext.cs` |
| ChangeIntelligenceDbContext | `src/modules/changegovernance/Infrastructure/Persistence/ChangeIntelligenceDbContext.cs` |
| PromotionDbContext | `src/modules/changegovernance/Infrastructure/Persistence/PromotionDbContext.cs` |
| RulesetGovernanceDbContext | `src/modules/changegovernance/Infrastructure/Persistence/RulesetGovernanceDbContext.cs` |

---

*Relatório gerado como parte da auditoria modular de governança do NexTraceOne.*
