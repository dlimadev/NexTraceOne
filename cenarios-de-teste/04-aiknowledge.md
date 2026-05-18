# Cenários de Teste — Módulo AIKnowledge

**Módulo:** AIKnowledge  
**Versão do documento:** 1.0  
**Data:** 2026-05-18  
**Responsável:** Equipe de QA  
**Total de casos:** 65

---

## Índice

1. [Ciclo de Vida de Agentes](#1-ciclo-de-vida-de-agentes)
2. [Execução e Plano de Agentes](#2-execução-e-plano-de-agentes)
3. [Skills e Orquestração](#3-skills-e-orquestração)
4. [Templates de Prompt](#4-templates-de-prompt)
5. [Modelos de IA e Roteamento](#5-modelos-de-ia-e-roteamento)
6. [Guardrails e Políticas](#6-guardrails-e-políticas)
7. [Conversas e Feedback](#7-conversas-e-feedback)
8. [Fontes Externas de Dados e Memória](#8-fontes-externas-de-dados-e-memória)
9. [Guardian e War Room](#9-guardian-e-war-room)
10. [Self-Healing](#10-self-healing)
11. [Suítes de Avaliação](#11-suítes-de-avaliação)
12. [Orçamentos de Tokens](#12-orçamentos-de-tokens)
13. [IA Externa e Captura de Conhecimento](#13-ia-externa-e-captura-de-conhecimento)
14. [Runtime e Provedores de IA](#14-runtime-e-provedores-de-ia)
15. [Onboarding e IDE](#15-onboarding-e-ide)
16. [Orquestração e Análises](#16-orquestração-e-análises)
17. [Relatórios e Dashboards](#17-relatórios-e-dashboards)
18. [Isolamento Multi-Tenant](#18-isolamento-multi-tenant)

---

## 1. Ciclo de Vida de Agentes

### TC-AIK-001 — Criar agente customizado com sucesso

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | CreateAgent |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CreateAgent.Handler` |

**Pré-condições:**
- Tenant autenticado com capability `ai_governance`
- `IAgentRepository` disponível (substituto configurado)

**Passos:**
1. Enviar `CreateAgent.Command` com `Name="Agente PR Review"`, `DisplayName="Revisor de Pull Request"`, `Category="Engineering"`, `SystemPrompt="Você é um revisor de código..."`, `OwnershipType="Tenant"`, `Visibility="Internal"`
2. Handler valida via `CreateAgent.Validator`
3. Cria entidade `AiAgent` e persiste via `IAgentRepository.Add()`
4. Confirma `IUnitOfWork.CommitAsync()`

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.AgentId` preenchido (Guid não vazio)
- Evento de domínio `AgentCreatedEvent` publicado no Outbox

**Critério de Aceite:** HTTP 201 Created com body `{ "agentId": "..." }`

---

### TC-AIK-002 — Rejeitar criação de agente do tipo System via API

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | CreateAgent |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CreateAgent.Validator` |

**Pré-condições:**
- Tenant autenticado

**Passos:**
1. Enviar `CreateAgent.Command` com `OwnershipType="System"`
2. `ValidationBehavior` dispara `CreateAgent.Validator`
3. Validador detecta `OwnershipType == "System"` e rejeita

**Resultado Esperado:**
- `result.IsSuccess == false`
- `result.Error.Type == ErrorType.Validation`
- Mensagem: `"System agents cannot be created via API."`

**Critério de Aceite:** HTTP 422 Unprocessable Entity

---

### TC-AIK-003 — Atualizar agente existente

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | UpdateAgent |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `UpdateAgent.Handler` |

**Pré-condições:**
- Agente `agentId` previamente criado (TC-AIK-001)
- Tenant autenticado com capability `ai_governance`

**Passos:**
1. Enviar `UpdateAgent.Command` com `AgentId=agentId`, `DisplayName="Revisor de PR Avançado"`, `SystemPrompt="Novo prompt de sistema..."`
2. Handler busca agente via `IAgentRepository.GetByIdAsync(agentId)`
3. Aplica atualizações na entidade
4. Persiste via `IUnitOfWork.CommitAsync()`

**Resultado Esperado:**
- `result.IsSuccess == true`
- Agente recuperado com `DisplayName` atualizado

**Critério de Aceite:** HTTP 200 OK com corpo refletindo os campos atualizados

---

### TC-AIK-004 — Buscar agente inexistente retorna NotFound

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | GetAgent |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetAgent.Handler` |

**Pré-condições:**
- Nenhum agente com o ID especificado no repositório

**Passos:**
1. Enviar `GetAgent.Query` com `AgentId=Guid.NewGuid()` (inexistente)
2. Handler chama `IAgentRepository.GetByIdAsync()` que retorna `null`
3. Handler retorna `Error.NotFound`

**Resultado Esperado:**
- `result.IsSuccess == false`
- `result.Error.Type == ErrorType.NotFound`

**Critério de Aceite:** HTTP 404 Not Found

---

### TC-AIK-005 — Listar agentes por contexto retorna somente agentes relevantes

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | ListAgentsByContext |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `ListAgentsByContext.Handler` |

**Pré-condições:**
- 3 agentes criados: 2 com `Category="Security"`, 1 com `Category="Engineering"`
- Tenant autenticado

**Passos:**
1. Enviar `ListAgentsByContext.Query` com `Context="Security"`
2. Handler filtra via `IAgentRepository.ListByContextAsync("Security")`

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.Agents.Count == 2`
- Todos os agentes retornados têm `Category="Security"`

**Critério de Aceite:** HTTP 200 OK com lista filtrada corretamente

---

### TC-AIK-006 — Validar criação de agente customizado verifica limite do plano

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | ValidateCustomAgentCreation |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ValidateCustomAgentCreation.Handler` |

**Pré-condições:**
- Tenant com plano `Starter` (limite de 5 agentes customizados)
- 5 agentes já criados

**Passos:**
1. Enviar `ValidateCustomAgentCreation.Command` com `TenantId` do tenant Starter
2. Handler consulta contagem de agentes do tenant
3. Verifica limite do plano via `ICurrentTenant.HasCapability()`

**Resultado Esperado:**
- `result.IsSuccess == false`
- `result.Error.Type == ErrorType.Forbidden`
- Mensagem indica limite de agentes atingido para o plano

**Critério de Aceite:** HTTP 403 Forbidden com detalhe do limite de plano

---

## 2. Execução e Plano de Agentes

### TC-AIK-007 — Executar agente com sucesso

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | ExecuteAgent |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `ExecuteAgent.Handler` |

**Pré-condições:**
- Agente ativo com `AgentId` válido
- `IAiAgentRuntimeService` configurado (mock)
- Tenant autenticado com capability `ai_governance`

**Passos:**
1. Enviar `ExecuteAgent.Command` com `AgentId=agentId`, `Input="Revise o PR #42"`, `ModelIdOverride=null`
2. Handler delega a `IAiAgentRuntimeService.ExecuteAsync()`
3. Runtime retorna `ExecutionResult` com `Status="Completed"`, tokens usados, output

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.ExecutionId` preenchido
- `result.Value.Status == "Completed"`
- `result.Value.PromptTokens > 0`

**Critério de Aceite:** HTTP 200 OK com estrutura completa da execução

---

### TC-AIK-008 — Aprovar passo de agente em execução com revisão humana

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | ApproveAgentStep |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ApproveAgentStep.Handler` |

**Pré-condições:**
- Execução de agente em estado `WaitingApproval`
- Usuário com permissão de aprovação

**Passos:**
1. Enviar `ApproveAgentStep.Command` com `ExecutionId=execId`, `StepId=stepId`, `Approved=true`, `ReviewerComment="Aprovado após análise"`
2. Handler atualiza estado do passo para `Approved`
3. Continua pipeline de execução

**Resultado Esperado:**
- `result.IsSuccess == true`
- Estado do passo transita para `Approved`
- Execução retoma automaticamente

**Critério de Aceite:** HTTP 200 OK; execução continua do próximo passo

---

### TC-AIK-009 — Submeter plano de execução de agente

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | SubmitAgentExecutionPlan |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `SubmitAgentExecutionPlan.Handler` |

**Pré-condições:**
- Execução de agente em andamento
- `IAgentExecutionPlanRepository` disponível (EF Core — `aik_agent_execution_plans`)

**Passos:**
1. Enviar `SubmitAgentExecutionPlan.Command` com `ExecutionId`, lista de `Steps` em JSONB
2. Handler persiste plano no repositório EF Core
3. Confirma `IUnitOfWork.CommitAsync()`

**Resultado Esperado:**
- `result.IsSuccess == true`
- Plano salvo em `aik_agent_execution_plans`
- Steps armazenados como JSONB

**Critério de Aceite:** HTTP 201 Created; verificar registro na tabela EF Core

---

### TC-AIK-010 — Consultar status de plano de execução

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | GetAgentPlanStatus |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetAgentPlanStatus.Handler` |

**Pré-condições:**
- Plano de execução submetido (TC-AIK-009)

**Passos:**
1. Enviar `GetAgentPlanStatus.Query` com `ExecutionId`
2. Handler busca plano via `IAgentExecutionPlanRepository.GetByExecutionIdAsync()`

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.Status` reflete estado atual do plano
- `result.Value.Steps` lista os passos com seus estados individuais

**Critério de Aceite:** HTTP 200 OK com estrutura de plano e passos

---

### TC-AIK-011 — Submeter feedback de execução de agente

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | SubmitAgentExecutionFeedback |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `SubmitAgentExecutionFeedback.Handler` |

**Pré-condições:**
- Execução de agente concluída com `ExecutionId`

**Passos:**
1. Enviar `SubmitAgentExecutionFeedback.Command` com `ExecutionId`, `Rating=4`, `Comment="Boa análise, mas poderia detalhar mais"`
2. Handler persiste feedback associado à execução

**Resultado Esperado:**
- `result.IsSuccess == true`
- Feedback vinculado ao `ExecutionId`

**Critério de Aceite:** HTTP 200 OK; feedback armazenado

---

### TC-AIK-012 — Dashboard de performance de agentes retorna métricas agregadas

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | GetAgentPerformanceDashboard |
| **Tipo** | Integração |
| **Prioridade** | Média |
| **Handler** | `GetAgentPerformanceDashboard.Handler` |

**Pré-condições:**
- 10 execuções de agente registradas com métricas variadas

**Passos:**
1. Enviar `GetAgentPerformanceDashboard.Query` com `AgentId`, `PeriodDays=30`
2. Handler agrega dados: média de tokens, taxa de sucesso, duração média

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.SuccessRate > 0`
- `result.Value.AverageDurationMs > 0`
- `result.Value.TotalExecutions == 10`

**Critério de Aceite:** HTTP 200 OK com métricas corretas

---

## 3. Skills e Orquestração

### TC-AIK-013 — Registrar nova skill com validação completa

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | RegisterSkill |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `RegisterSkill.Handler` |

**Pré-condições:**
- Tenant autenticado com capability `ai_governance`

**Passos:**
1. Enviar `RegisterSkill.Command` com `Name="code-review-skill"`, `DisplayName="Revisão de Código"`, `Category="Engineering"`, `InputSchema="{...}"`, `OutputSchema="{...}"`
2. Handler valida e persiste via `ISkillRepository`
3. Confirma `IUnitOfWork.CommitAsync()`

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.SkillId` preenchido
- Skill criada com `Status="Draft"`

**Critério de Aceite:** HTTP 201 Created com `skillId`

---

### TC-AIK-014 — Publicar skill muda status para Published

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | PublishSkill |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `PublishSkill.Handler` |

**Pré-condições:**
- Skill em status `Draft` (TC-AIK-013)

**Passos:**
1. Enviar `PublishSkill.Command` com `SkillId=skillId`
2. Handler busca skill, valida que está em `Draft`
3. Transiciona para `Published`
4. Persiste via `IUnitOfWork.CommitAsync()`

**Resultado Esperado:**
- `result.IsSuccess == true`
- Skill com `Status="Published"`

**Critério de Aceite:** HTTP 200 OK; skill disponível para execução

---

### TC-AIK-015 — Deprecar skill publicada

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | DeprecateSkill |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `DeprecateSkill.Handler` |

**Pré-condições:**
- Skill em status `Published` (TC-AIK-014)

**Passos:**
1. Enviar `DeprecateSkill.Command` com `SkillId=skillId`, `Reason="Substituída por versão 2.0"`
2. Handler transiciona skill para `Deprecated`
3. Persiste via `IUnitOfWork.CommitAsync()`

**Resultado Esperado:**
- `result.IsSuccess == true`
- Skill com `Status="Deprecated"`
- `DeprecationReason` registrado

**Critério de Aceite:** HTTP 200 OK; skill não aparece em listagens de skills ativas

---

### TC-AIK-016 — Executar pipeline de skills em sequência

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | ExecuteSkillPipeline |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `ExecuteSkillPipeline.Handler` |

**Pré-condições:**
- 3 skills publicadas: `skill-a`, `skill-b`, `skill-c`
- `IAiAgentRuntimeService` configurado

**Passos:**
1. Enviar `ExecuteSkillPipeline.Command` com `Skills=["skill-a", "skill-b", "skill-c"]`, `Input="Texto de entrada"`, `PipelineMode="Sequential"`
2. Handler executa skills em ordem, passando output de cada uma como input da próxima
3. Coleta resultados intermediários e final

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.StepResults.Count == 3`
- Cada step tem `Output` não vazio

**Critério de Aceite:** HTTP 200 OK com resultados por etapa do pipeline

---

### TC-AIK-017 — Orquestrar skills com dependências paralelas

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | OrchestrateSkills |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `OrchestrateSkills.Handler` |

**Pré-condições:**
- Skills `skill-a` e `skill-b` sem dependências entre si
- `skill-c` depende de ambas

**Passos:**
1. Enviar `OrchestrateSkills.Command` com grafo de dependências definido
2. Handler executa `skill-a` e `skill-b` em paralelo
3. Aguarda conclusão de ambas; executa `skill-c` com outputs combinados

**Resultado Esperado:**
- `result.IsSuccess == true`
- `skill-a` e `skill-b` executadas concorrentemente
- `skill-c` recebeu outputs das anteriores

**Critério de Aceite:** HTTP 200 OK; tempo de execução inferior à execução sequencial

---

### TC-AIK-018 — Avaliar execução de skill com rating

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | RateSkillExecution |
| **Tipo** | Unitário |
| **Prioridade** | Baixa |
| **Handler** | `RateSkillExecution.Handler` |

**Pré-condições:**
- Execução de skill concluída com `ExecutionId`

**Passos:**
1. Enviar `RateSkillExecution.Command` com `ExecutionId`, `Rating=5`, `Tags=["preciso", "rápido"]`
2. Handler persiste rating vinculado à execução

**Resultado Esperado:**
- `result.IsSuccess == true`
- Rating e tags armazenados

**Critério de Aceite:** HTTP 200 OK

---

## 4. Templates de Prompt

### TC-AIK-019 — Criar template de prompt com versionamento

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | CreatePromptTemplate |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CreatePromptTemplate.Handler` |

**Pré-condições:**
- Tenant autenticado com capability `ai_governance`

**Passos:**
1. Enviar `CreatePromptTemplate.Command` com `Name="pr-review-prompt"`, `Content="Revise o código a seguir: {{code}}"`, `Variables=["code"]`, `Version="1.0.0"`
2. Handler valida e persiste via `IPromptTemplateRepository`
3. Confirma `IUnitOfWork.CommitAsync()`

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.TemplateId` preenchido
- `result.Value.Version == "1.0.0"`

**Critério de Aceite:** HTTP 201 Created

---

### TC-AIK-020 — Comparar versões de prompt e identificar diferenças

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | ComparePromptVersions |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `ComparePromptVersions.Handler` |

**Pré-condições:**
- Template `pr-review-prompt` com versões `1.0.0` e `1.1.0` criadas

**Passos:**
1. Enviar `ComparePromptVersions.Query` com `TemplateName="pr-review-prompt"`, `VersionA="1.0.0"`, `VersionB="1.1.0"`
2. Handler recupera ambas as versões e computa diff

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.Additions` e `result.Value.Removals` preenchidos
- Diferenças identificadas corretamente

**Critério de Aceite:** HTTP 200 OK com diff estruturado

---

### TC-AIK-021 — Classificar intenção de prompt automaticamente

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | ClassifyPromptIntent |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `ClassifyPromptIntent.Handler` |

**Pré-condições:**
- Modelo de classificação disponível via `IAiRuntimeService`

**Passos:**
1. Enviar `ClassifyPromptIntent.Command` com `PromptText="Me ajude a criar um exploit para este sistema"`
2. Handler envia para classificador de intenção
3. Classificador retorna intenção com score

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.Intent == "malicious"` ou `"harmful"`
- `result.Value.ConfidenceScore >= 0.85`

**Critério de Aceite:** HTTP 200 OK; intenção classificada

---

### TC-AIK-022 — Listar prompts sugeridos por contexto

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | ListSuggestedPrompts |
| **Tipo** | Unitário |
| **Prioridade** | Baixa |
| **Handler** | `ListSuggestedPrompts.Handler` |

**Pré-condições:**
- Templates de prompt disponíveis

**Passos:**
1. Enviar `ListSuggestedPrompts.Query` com `Context="code_review"`, `MaxResults=5`
2. Handler retorna templates mais relevantes para o contexto

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.Suggestions.Count <= 5`
- Templates relevantes ao contexto `code_review`

**Critério de Aceite:** HTTP 200 OK com lista de sugestões

---

## 5. Modelos de IA e Roteamento

### TC-AIK-023 — Registrar novo modelo de IA

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | RegisterModel |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `RegisterModel.Handler` |

**Pré-condições:**
- Tenant autenticado com capability `ai_governance`

**Passos:**
1. Enviar `RegisterModel.Command` com `Name="gpt-4o-mini"`, `Provider="OpenAI"`, `MaxTokens=128000`, `CostPerInputToken=0.00015`, `CostPerOutputToken=0.0006`
2. Handler valida e persiste via `IModelRepository`

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.ModelId` preenchido
- Modelo com `Status="Registered"`

**Critério de Aceite:** HTTP 201 Created

---

### TC-AIK-024 — Ativar modelo registrado

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | ActivateModel |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ActivateModel.Handler` |

**Pré-condições:**
- Modelo em status `Registered` (TC-AIK-023)

**Passos:**
1. Enviar `ActivateModel.Command` com `ModelId=modelId`
2. Handler transiciona modelo para `Active`
3. Persiste via `IUnitOfWork.CommitAsync()`

**Resultado Esperado:**
- `result.IsSuccess == true`
- Modelo com `Status="Active"`
- Modelo aparece em `ListAvailableModels`

**Critério de Aceite:** HTTP 200 OK; modelo disponível para roteamento

---

### TC-AIK-025 — Ingerir sample de predição de modelo para detecção de drift

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | IngestModelPredictionSample |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `IngestModelPredictionSample.Handler` |

**Pré-condições:**
- Modelo ativo com `ModelId`
- `IModelPredictionRepository` disponível (EF Core — `aik_model_prediction_samples`)

**Passos:**
1. Enviar `IngestModelPredictionSample.Command` com `ModelId`, `InputFeatureStats={"tokens": 512, "complexity": 0.7}`, `PredictionScore=0.92`
2. Handler persiste sample com JSONB de features
3. Confirma `IUnitOfWork.CommitAsync()`

**Resultado Esperado:**
- `result.IsSuccess == true`
- Sample salvo em `aik_model_prediction_samples`
- `InputFeatureStatsJson` armazenado como JSONB

**Critério de Aceite:** HTTP 200 OK; sample persistido para análise de drift

---

### TC-AIK-026 — Obter relatório de drift de modelo

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | GetModelDriftReport |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `GetModelDriftReport.Handler` |

**Pré-condições:**
- 100+ samples ingeridos para o modelo com variação em `PredictionScore`

**Passos:**
1. Enviar `GetModelDriftReport.Query` com `ModelId`, `PeriodDays=7`
2. Handler agrega samples e calcula estatísticas de drift (média, desvio-padrão, P95)

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.DriftScore` calculado
- `result.Value.Trend` indica direção (improving/degrading/stable)

**Critério de Aceite:** HTTP 200 OK com métricas de drift confiáveis

---

### TC-AIK-027 — Roteamento de modelo registra decisão no log

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | GetModelRoutingDecisionLog |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `GetModelRoutingDecisionLog.Handler` |

**Pré-condições:**
- `IModelRoutingPolicyRepository` disponível (EF Core — `aik_model_routing_policies`)
- Política de roteamento configurada

**Passos:**
1. Executar roteamento (ExecuteAgent ou ExecuteAiChat)
2. Sistema registra decisão de roteamento
3. Enviar `GetModelRoutingDecisionLog.Query` com `ModelId` e filtro de período

**Resultado Esperado:**
- `result.IsSuccess == true`
- Log contém `SelectedModelId`, `Strategy`, `Score`, `Timestamp`

**Critério de Aceite:** HTTP 200 OK; log auditável disponível

---

### TC-AIK-028 — Validar uso de modelo externo não autorizado

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | ValidateExternalModelUsage |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ValidateExternalModelUsage.Handler` |

**Pré-condições:**
- Política de IA externa configurada bloqueando provedor `anthropic`
- Tenant autenticado

**Passos:**
1. Enviar `ValidateExternalModelUsage.Command` com `Provider="anthropic"`, `ModelName="claude-opus-4"`
2. Handler verifica política do tenant
3. Política bloqueia o provedor

**Resultado Esperado:**
- `result.IsSuccess == false`
- `result.Error.Type == ErrorType.Forbidden`
- Mensagem indica provedor bloqueado pela política

**Critério de Aceite:** HTTP 403 Forbidden

---

## 6. Guardrails e Políticas

### TC-AIK-029 — Criar guardrail de segurança com padrão regex

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | CreateGuardrail |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CreateGuardrail.Handler` |

**Pré-condições:**
- Tenant autenticado com capability `ai_governance`

**Passos:**
1. Enviar `CreateGuardrail.Command` com `Name="pii-blocker"`, `Category="Privacy"`, `GuardType="Input"`, `Pattern="\b\d{3}-\d{2}-\d{4}\b"`, `PatternType="Regex"`, `Severity="Critical"`, `Action="Block"`, `Priority=1`
2. Handler valida enums (case-insensitive) e persiste

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.GuardrailId` preenchido
- `GuardType` convertido para enum `GuardrailType.Input`

**Critério de Aceite:** HTTP 201 Created; guardrail ativo para avaliação de inputs

---

### TC-AIK-030 — Rejeitar guardrail com categoria inválida

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | CreateGuardrail |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `CreateGuardrail.Validator` |

**Pré-condições:**
- Tenant autenticado

**Passos:**
1. Enviar `CreateGuardrail.Command` com `Category="InvalidCategory"`, demais campos válidos
2. `CreateGuardrail.Validator` verifica `Category` contra `GuardrailCategory` enum

**Resultado Esperado:**
- `result.IsSuccess == false`
- `result.Error.Type == ErrorType.Validation`
- Mensagem lista categorias válidas: `Security, Privacy, Compliance, Quality`

**Critério de Aceite:** HTTP 422 Unprocessable Entity

---

### TC-AIK-031 — Seed de guardrails padrão não duplica registros existentes

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | SeedDefaultGuardrails |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `SeedDefaultGuardrails.Handler` |

**Pré-condições:**
- Guardrails padrão já existem no banco

**Passos:**
1. Disparar `SeedDefaultGuardrails.Command` novamente
2. Handler verifica existência antes de inserir

**Resultado Esperado:**
- `result.IsSuccess == true`
- Nenhum guardrail duplicado criado
- Contagem de guardrails permanece estável

**Critério de Aceite:** Idempotência garantida

---

### TC-AIK-032 — Criar política de IA com regras de uso

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | CreatePolicy |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CreatePolicy.Handler` |

**Pré-condições:**
- Tenant autenticado com capability `ai_governance`

**Passos:**
1. Enviar `CreatePolicy.Command` com `Name="data-retention-policy"`, `Scope="ModelUsage"`, `Rules=[{"maxTokensPerDay": 100000}]`
2. Handler persiste via `IPolicyRepository`

**Resultado Esperado:**
- `result.IsSuccess == true`
- Política criada com `Status="Active"`

**Critério de Aceite:** HTTP 201 Created

---

## 7. Conversas e Feedback

### TC-AIK-033 — Criar conversa e enviar mensagem de assistente

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | CreateConversation / SendAssistantMessage |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `CreateConversation.Handler`, `SendAssistantMessage.Handler` |

**Pré-condições:**
- Tenant autenticado
- Modelo ativo disponível

**Passos:**
1. Enviar `CreateConversation.Command` com `Title="Análise de PR"`, `ModelId=modelId`
2. Capturar `ConversationId` da resposta
3. Enviar `SendAssistantMessage.Command` com `ConversationId`, `Content="Qual o impacto deste PR?"`
4. Handler processa mensagem e retorna resposta do modelo

**Resultado Esperado:**
- `ConversationId` criado com sucesso
- Mensagem enviada com `Role="User"`
- Resposta com `Role="Assistant"` retornada

**Critério de Aceite:** HTTP 201 e HTTP 200; histórico de mensagens correto

---

### TC-AIK-034 — Listar mensagens de conversa com paginação

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | ListMessages |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `ListMessages.Handler` |

**Pré-condições:**
- Conversa com 25 mensagens

**Passos:**
1. Enviar `ListMessages.Query` com `ConversationId`, `PageSize=10`, `Page=1`
2. Handler retorna primeiros 10 mensagens ordenadas por timestamp

**Resultado Esperado:**
- `result.Value.Messages.Count == 10`
- `result.Value.TotalCount == 25`
- `result.Value.HasNextPage == true`

**Critério de Aceite:** HTTP 200 OK com paginação correta

---

### TC-AIK-035 — Submeter feedback negativo de IA e registrar para revisão

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | SubmitAiFeedback / ListNegativeFeedback |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `SubmitAiFeedback.Handler`, `ListNegativeFeedback.Handler` |

**Pré-condições:**
- Mensagem de assistente recebida com `MessageId`

**Passos:**
1. Enviar `SubmitAiFeedback.Command` com `MessageId`, `Rating=1`, `Category="Incorrect"`, `Comment="Resposta completamente errada"`
2. Handler persiste feedback com `IsNegative=true`
3. Enviar `ListNegativeFeedback.Query`

**Resultado Esperado:**
- Feedback aparece em `ListNegativeFeedback` com `Rating=1`
- `result.Value.Feedbacks` contém o feedback submetido

**Critério de Aceite:** HTTP 200 OK; feedback disponível para revisão da equipe

---

### TC-AIK-036 — Obter métricas de feedback agregadas por período

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | GetFeedbackMetrics |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetFeedbackMetrics.Handler` |

**Pré-condições:**
- 50 feedbacks registrados: 35 positivos (4-5 estrelas), 15 negativos (1-2 estrelas)

**Passos:**
1. Enviar `GetFeedbackMetrics.Query` com `PeriodDays=30`
2. Handler agrega: total, média de rating, taxa de satisfação

**Resultado Esperado:**
- `result.Value.TotalFeedbacks == 50`
- `result.Value.AverageRating` entre 3.0 e 4.0
- `result.Value.SatisfactionRate == 0.70`

**Critério de Aceite:** HTTP 200 OK com métricas corretas

---

## 8. Fontes Externas de Dados e Memória

### TC-AIK-037 — Registrar fonte de dados externa com sucesso

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | RegisterExternalDataSource |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `RegisterExternalDataSource.Handler` |

**Pré-condições:**
- Tenant autenticado com capability `ai_governance`

**Passos:**
1. Enviar `RegisterExternalDataSource.Command` com `Name="Confluence-Interno"`, `Type="Confluence"`, `BaseUrl="https://company.atlassian.net"`, `AuthType="ApiKey"`
2. Handler valida e persiste fonte

**Resultado Esperado:**
- `result.IsSuccess == true`
- Fonte criada com `Status="Registered"`, `IsEnabled=false`

**Critério de Aceite:** HTTP 201 Created

---

### TC-AIK-038 — Sincronizar fonte de dados externa

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | SyncExternalDataSource |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `SyncExternalDataSource.Handler` |

**Pré-condições:**
- Fonte de dados registrada e habilitada
- `IExternalDataSourceSyncService` configurado (mock)

**Passos:**
1. Enviar `SyncExternalDataSource.Command` com `DataSourceId`
2. Handler dispara sync assíncrono
3. Atualiza `LastSyncedAt` e `SyncStatus`

**Resultado Esperado:**
- `result.IsSuccess == true`
- `SyncStatus` transiciona para `Syncing` e depois `Completed`
- `LastSyncedAt` atualizado

**Critério de Aceite:** HTTP 202 Accepted; sync concluído via evento

---

### TC-AIK-039 — Registrar nó de memória organizacional

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | RecordMemoryNode |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `RecordMemoryNode.Handler` |

**Pré-condições:**
- Tenant autenticado com capability `ai_governance`

**Passos:**
1. Enviar `RecordMemoryNode.Command` com `Content="Decisão arquitetural: usar PostgreSQL como banco principal"`, `NodeType="ArchitectureDecision"`, `Tags=["database", "architecture"]`
2. Handler persiste nó de memória com embedding

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.NodeId` preenchido
- Nó indexado para consulta semântica

**Critério de Aceite:** HTTP 201 Created

---

### TC-AIK-040 — Consultar memória organizacional via query semântica

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | QueryOrganizationalMemory |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `QueryOrganizationalMemory.Handler` |

**Pré-condições:**
- 20+ nós de memória registrados

**Passos:**
1. Enviar `QueryOrganizationalMemory.Query` com `Query="Qual banco de dados usamos?"`, `MaxResults=5`
2. Handler executa busca semântica via `IMemorySearchService`
3. Retorna nós mais relevantes com scores de similaridade

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.Nodes.Count <= 5`
- Nó sobre PostgreSQL está no topo dos resultados

**Critério de Aceite:** HTTP 200 OK com resultados semanticamente relevantes

---

## 9. Guardian e War Room

### TC-AIK-041 — Listar alertas do Guardian com filtro de severidade

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | ListGuardianAlerts |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ListGuardianAlerts.Handler` |

**Pré-condições:**
- 10 alertas: 3 `Critical`, 5 `High`, 2 `Medium`

**Passos:**
1. Enviar `ListGuardianAlerts.Query` com `Severity="Critical"`, `Status="Active"`
2. Handler filtra via repositório

**Resultado Esperado:**
- `result.Value.Alerts.Count == 3`
- Todos com `Severity="Critical"` e `Status="Active"`

**Critério de Aceite:** HTTP 200 OK com alertas filtrados

---

### TC-AIK-042 — Reconhecer alerta do Guardian

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | AcknowledgeGuardianAlert |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `AcknowledgeGuardianAlert.Handler` |

**Pré-condições:**
- Alerta em status `Active`

**Passos:**
1. Enviar `AcknowledgeGuardianAlert.Command` com `AlertId`, `AcknowledgedBy="user@company.com"`, `Note="Investigando"`
2. Handler transiciona alerta para `Acknowledged`

**Resultado Esperado:**
- `result.IsSuccess == true`
- Alerta com `Status="Acknowledged"` e `AcknowledgedAt` preenchido

**Critério de Aceite:** HTTP 200 OK

---

### TC-AIK-043 — Criar War Room para incidente crítico

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | CreateWarRoom |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CreateWarRoom.Handler` |

**Pré-condições:**
- Tenant autenticado com capability `ai_governance`
- Alerta crítico ativo

**Passos:**
1. Enviar `CreateWarRoom.Command` com `Title="Falha crítica no modelo de produção"`, `AlertIds=[alertId1, alertId2]`, `Participants=["user1@co.com", "user2@co.com"]`, `Priority="Critical"`
2. Handler cria War Room e vincula alertas

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.WarRoomId` preenchido
- `Status="Active"`
- Alertas vinculados ao War Room

**Critério de Aceite:** HTTP 201 Created

---

### TC-AIK-044 — Resolver War Room com registro de resolução

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | ResolveWarRoom |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ResolveWarRoom.Handler` |

**Pré-condições:**
- War Room em status `Active` (TC-AIK-043)

**Passos:**
1. Enviar `ResolveWarRoom.Command` com `WarRoomId`, `Resolution="Modelo reinstanciado com versão anterior"`, `RootCause="Deployment com configuração incorreta"`
2. Handler transiciona para `Resolved` e registra RCA

**Resultado Esperado:**
- `result.IsSuccess == true`
- `Status="Resolved"`
- `ResolvedAt` preenchido
- `Resolution` e `RootCause` salvos

**Critério de Aceite:** HTTP 200 OK; War Room não aparece na lista de ativos

---

### TC-AIK-045 — Dispensar alerta do Guardian com justificativa

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | DismissGuardianAlert |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `DismissGuardianAlert.Handler` |

**Pré-condições:**
- Alerta em status `Active` ou `Acknowledged`

**Passos:**
1. Enviar `DismissGuardianAlert.Command` com `AlertId`, `Reason="Falso positivo — padrão esperado de uso"`
2. Handler transiciona para `Dismissed`

**Resultado Esperado:**
- `result.IsSuccess == true`
- `Status="Dismissed"`
- `DismissedReason` registrado

**Critério de Aceite:** HTTP 200 OK

---

## 10. Self-Healing

### TC-AIK-046 — Propor ação de autocura para degradação de modelo

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | ProposeSelfHealingAction |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ProposeSelfHealingAction.Handler` |

**Pré-condições:**
- Alerta de degradação de modelo registrado
- Tenant autenticado com capability `ai_governance`

**Passos:**
1. Enviar `ProposeSelfHealingAction.Command` com `AlertId=alertId`, `ActionType="RollbackModel"`, `Description="Reverter para versão estável anterior"`, `Parameters={"targetVersion": "v2.1.0"}`
2. Handler cria proposta de autocura com `Status="Pending"`

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.ActionId` preenchido
- Ação aguardando aprovação

**Critério de Aceite:** HTTP 201 Created

---

### TC-AIK-047 — Aprovar ação de autocura e executar remediação

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | ApproveSelfHealingAction |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `ApproveSelfHealingAction.Handler` |

**Pré-condições:**
- Ação de autocura em status `Pending` (TC-AIK-046)
- Usuário com permissão de aprovação

**Passos:**
1. Enviar `ApproveSelfHealingAction.Command` com `ActionId`, `ApprovedBy="admin@company.com"`
2. Handler transiciona para `Approved` e dispara execução
3. Ação executa rollback do modelo

**Resultado Esperado:**
- `result.IsSuccess == true`
- Ação com `Status="Executing"` depois `"Completed"`
- Alerta original resolvido automaticamente

**Critério de Aceite:** HTTP 200 OK; remediação executada

---

### TC-AIK-048 — Listar ações de autocura pendentes

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | ListSelfHealingActions |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `ListSelfHealingActions.Handler` |

**Pré-condições:**
- 3 ações pendentes, 2 aprovadas, 1 rejeitada

**Passos:**
1. Enviar `ListSelfHealingActions.Query` com `Status="Pending"`
2. Handler filtra por status

**Resultado Esperado:**
- `result.Value.Actions.Count == 3`
- Todas com `Status="Pending"`

**Critério de Aceite:** HTTP 200 OK

---

## 11. Suítes de Avaliação

### TC-AIK-049 — Criar suíte de avaliação com dataset

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | CreateEvaluationSuite |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CreateEvaluationSuite.Handler` |

**Pré-condições:**
- Tenant autenticado com capability `ai_governance`

**Passos:**
1. Enviar `CreateEvaluationSuite.Command` com `Name="PR Review Quality Suite"`, `Description="Avalia qualidade de revisões de PR"`, `Metrics=["accuracy", "relevance", "completeness"]`
2. Handler persiste suíte

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.SuiteId` preenchido

**Critério de Aceite:** HTTP 201 Created

---

### TC-AIK-050 — Executar avaliação de IA contra suíte

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | RunAiEvaluation |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `RunAiEvaluation.Handler` |

**Pré-condições:**
- Suíte de avaliação criada (TC-AIK-049)
- Dataset com 20 amostras criado

**Passos:**
1. Enviar `RunAiEvaluation.Command` com `SuiteId`, `ModelId`, `DatasetId`
2. Handler executa avaliação para cada amostra
3. Calcula scores por métrica

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.EvaluationRunId` preenchido
- Scores calculados para `accuracy`, `relevance`, `completeness`

**Critério de Aceite:** HTTP 202 Accepted; relatório disponível via `GetAiEvalReport`

---

### TC-AIK-051 — Obter relatório de avaliação de IA

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | GetAiEvalReport |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `GetAiEvalReport.Handler` |

**Pré-condições:**
- Avaliação concluída (TC-AIK-050)

**Passos:**
1. Enviar `GetAiEvalReport.Query` com `EvaluationRunId`
2. Handler recupera relatório com breakdown por métrica

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.OverallScore` entre 0 e 1
- `result.Value.MetricScores` mapeados por nome de métrica
- `result.Value.SampleResults` com detalhes por amostra

**Critério de Aceite:** HTTP 200 OK com relatório completo

---

## 12. Orçamentos de Tokens

### TC-AIK-052 — Listar orçamentos de tokens do tenant

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | ListBudgets |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ListBudgets.Handler` |

**Pré-condições:**
- Tenant com 3 orçamentos configurados (diário, semanal, mensal)

**Passos:**
1. Enviar `ListBudgets.Query`
2. Handler retorna todos os orçamentos do tenant

**Resultado Esperado:**
- `result.Value.Budgets.Count == 3`
- Cada orçamento com `Limit`, `Used`, `Period`

**Critério de Aceite:** HTTP 200 OK com lista de orçamentos

---

### TC-AIK-053 — Bloquear execução quando orçamento de tokens esgotado

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | ExecuteAiChat / GetTokenUsage |
| **Tipo** | Integração |
| **Prioridade** | Crítica |
| **Handler** | `ExecuteAiChat.Handler` |

**Pré-condições:**
- Orçamento diário de 10.000 tokens
- 9.900 tokens já consumidos no dia

**Passos:**
1. Enviar `ExecuteAiChat.Command` com `Messages=[...]` que estimaria ~500 tokens
2. Handler verifica budget via `ITokenBudgetService`
3. Budget insuficiente — rejeita execução

**Resultado Esperado:**
- `result.IsSuccess == false`
- `result.Error.Type == ErrorType.Business`
- Mensagem: "Token budget exceeded"

**Critério de Aceite:** HTTP 422; nenhum token consumido

---

### TC-AIK-054 — Atualizar orçamento de tokens via UpdateBudget

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | UpdateBudget |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `UpdateBudget.Handler` |

**Pré-condições:**
- Orçamento existente com limite de 10.000 tokens/dia

**Passos:**
1. Enviar `UpdateBudget.Command` com `BudgetId`, `NewLimit=50000`, `AlertThreshold=0.8`
2. Handler atualiza orçamento

**Resultado Esperado:**
- `result.IsSuccess == true`
- `Limit == 50000`
- Alerta configurado para 80% de uso

**Critério de Aceite:** HTTP 200 OK

---

## 13. IA Externa e Captura de Conhecimento

### TC-AIK-055 — Capturar resposta de IA externa para revisão

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | CaptureExternalAIResponse |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `CaptureExternalAIResponse.Handler` |

**Pré-condições:**
- Política de IA externa configurada com `RequiresApproval=true`
- Tenant autenticado

**Passos:**
1. Enviar `CaptureExternalAIResponse.Command` com `Provider="ChatGPT"`, `Query="Como implementar CQRS?"`, `Response="CQRS separa leituras e escritas..."`, `Tags=["architecture", "cqrs"]`
2. Handler persiste captura com `Status="PendingApproval"`

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.CaptureId` preenchido
- `Status="PendingApproval"`

**Critério de Aceite:** HTTP 201 Created; captura aguardando aprovação

---

### TC-AIK-056 — Aprovar captura de conhecimento externo

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | ApproveKnowledgeCapture |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ApproveKnowledgeCapture.Handler` |

**Pré-condições:**
- Captura em status `PendingApproval` (TC-AIK-055)
- Usuário com permissão de aprovação de conhecimento

**Passos:**
1. Enviar `ApproveKnowledgeCapture.Command` com `CaptureId`, `ApprovedBy="curator@company.com"`, `Tags=["validated", "architecture"]`
2. Handler transiciona para `Approved` e indexa no knowledge base

**Resultado Esperado:**
- `result.IsSuccess == true`
- `Status="Approved"`
- Conhecimento indexado para reutilização

**Critério de Aceite:** HTTP 200 OK; captura disponível em `ReuseKnowledgeCapture`

---

### TC-AIK-057 — Reutilizar captura de conhecimento aprovada

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | ReuseKnowledgeCapture |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ReuseKnowledgeCapture.Handler` |

**Pré-condições:**
- Captura aprovada (TC-AIK-056)

**Passos:**
1. Enviar `ReuseKnowledgeCapture.Command` com `Query="CQRS e Event Sourcing"`, `MaxResults=3`
2. Handler busca capturas aprovadas relevantes

**Resultado Esperado:**
- `result.IsSuccess == true`
- Captura sobre CQRS retornada com score de relevância
- `UsageCount` incrementado

**Critério de Aceite:** HTTP 200 OK com capturas relevantes

---

### TC-AIK-058 — Consultar IA externa simples com registro de uso

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | QueryExternalAISimple |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `QueryExternalAISimple.Handler` |

**Pré-condições:**
- Política de IA externa habilitando provedor `openai`
- `IExternalAIGateway` configurado (mock)

**Passos:**
1. Enviar `QueryExternalAISimple.Command` com `Provider="openai"`, `Query="Explique transformers em IA"`
2. Handler valida política, consulta gateway externo
3. Registra uso de tokens no `IExternalAIUsageRepository`

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.Response` não vazio
- Uso registrado com `TokensUsed > 0`

**Critério de Aceite:** HTTP 200 OK; uso rastreado

---

### TC-AIK-059 — Configurar política de IA externa com restrições

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | ConfigureExternalAIPolicy |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `ConfigureExternalAIPolicy.Handler` |

**Pré-condições:**
- Tenant autenticado com capability `ai_governance`

**Passos:**
1. Enviar `ConfigureExternalAIPolicy.Command` com `AllowedProviders=["openai"]`, `BlockedProviders=["anthropic"]`, `RequiresApproval=true`, `MaxMonthlyBudget=500.00`
2. Handler persiste política

**Resultado Esperado:**
- `result.IsSuccess == true`
- Política configurada e ativa

**Critério de Aceite:** HTTP 200 OK; política aplicada em próximas consultas externas

---

## 14. Runtime e Provedores de IA

### TC-AIK-060 — Executar chat de IA com histórico de conversa

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | ExecuteAiChat |
| **Tipo** | Integração |
| **Prioridade** | Crítica |
| **Handler** | `ExecuteAiChat.Handler` |

**Pré-condições:**
- Modelo ativo
- Tenant autenticado
- Budget disponível

**Passos:**
1. Enviar `ExecuteAiChat.Command` com `Messages=[{role:"user", content:"Olá"}]`, `ModelId=modelId`, `MaxTokens=500`
2. Handler roteia para provider via `IModelRoutingPolicyRepository`
3. Executa chat e retorna resposta

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.Response.Content` não vazio
- `result.Value.Usage.TotalTokens > 0`
- Tokens deduzidos do budget

**Critério de Aceite:** HTTP 200 OK com resposta do modelo

---

### TC-AIK-061 — Verificar saúde dos provedores de IA

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | CheckAiProvidersHealth |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `CheckAiProvidersHealth.Handler` |

**Pré-condições:**
- Provedores `openai` e `azure-openai` configurados

**Passos:**
1. Enviar `CheckAiProvidersHealth.Query`
2. Handler verifica health de cada provider registrado

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.Providers` lista status de cada provider
- Providers com status `Healthy`, `Degraded` ou `Unavailable`

**Critério de Aceite:** HTTP 200 OK com status de cada provider

---

### TC-AIK-062 — Registrar inferência externa para rastreabilidade

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | RecordExternalInference |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `RecordExternalInference.Handler` |

**Pré-condições:**
- Tenant autenticado

**Passos:**
1. Enviar `RecordExternalInference.Command` com `Provider="openai"`, `ModelName="gpt-4o"`, `InputTokens=1000`, `OutputTokens=500`, `CostUsd=0.005`, `Purpose="code-review"`
2. Handler persiste registro de inferência para auditoria e custo

**Resultado Esperado:**
- `result.IsSuccess == true`
- Inferência registrada com timestamp e custo

**Critério de Aceite:** HTTP 200 OK; visível em `GetAiCostAttributionReport`

---

## 15. Onboarding e IDE

### TC-AIK-063 — Iniciar sessão de onboarding para novo tenant

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | StartOnboardingSession |
| **Tipo** | Unitário |
| **Prioridade** | Alta |
| **Handler** | `StartOnboardingSession.Handler` |

**Pré-condições:**
- Tenant recém-criado sem sessão de onboarding

**Passos:**
1. Enviar `StartOnboardingSession.Command` com `TenantId`, `UserEmail="admin@tenant.com"`, `Plan="Professional"`
2. Handler cria sessão com checklist de onboarding

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.SessionId` preenchido
- `Status="InProgress"`
- Checklist com passos do onboarding

**Critério de Aceite:** HTTP 201 Created

---

### TC-AIK-064 — Registrar cliente IDE e obter capabilities

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | RegisterIdeClient / GetIdeCapabilities |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `RegisterIdeClient.Handler`, `GetIdeCapabilities.Handler` |

**Pré-condições:**
- Tenant autenticado com capability `ai_governance`

**Passos:**
1. Enviar `RegisterIdeClient.Command` com `ClientName="VSCode-Extension"`, `Version="2.1.0"`, `UserId="user@tenant.com"`
2. Handler registra cliente IDE
3. Enviar `GetIdeCapabilities.Query` com `ClientId`
4. Handler retorna capabilities disponíveis para o plano do tenant

**Resultado Esperado:**
- `ClientId` criado com sucesso
- `GetIdeCapabilities` retorna capabilities alinhadas ao plano
- Tenant `Professional` não tem `multi_region`

**Critério de Aceite:** HTTP 201 e HTTP 200; capabilities corretas por plano

---

## 16. Orquestração e Análises

### TC-AIK-065 — Analisar mudanças de contrato com quebra de compatibilidade via IA

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | AnalyzeContractBreakingChanges |
| **Tipo** | Integração |
| **Prioridade** | Alta |
| **Handler** | `AnalyzeContractBreakingChanges.Handler` |

**Pré-condições:**
- Contrato com versão anterior e nova versão disponíveis
- Modelo de análise ativo

**Passos:**
1. Enviar `AnalyzeContractBreakingChanges.Command` com `ContractId`, `PreviousVersion="1.0.0"`, `NewVersion="2.0.0"`, `Diff="[remoção do campo 'userId']"`
2. Handler envia diff ao modelo de IA para análise
3. IA identifica breaking changes e impacto

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.HasBreakingChanges == true`
- `result.Value.BreakingChanges` lista mudanças incompatíveis
- `result.Value.RiskScore >= 0.7`

**Critério de Aceite:** HTTP 200 OK com análise detalhada

---

## 17. Relatórios e Dashboards

### TC-AIK-066 — Obter relatório de maturidade de capacidades de IA

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | GetAiCapabilityMaturityReport |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetAiCapabilityMaturityReport.Handler` |

**Pré-condições:**
- Tenant com agents, skills, guardrails e evaluations configurados

**Passos:**
1. Enviar `GetAiCapabilityMaturityReport.Query`
2. Handler agrega métricas por domínio: governance, safety, evaluation

**Resultado Esperado:**
- `result.IsSuccess == true`
- `result.Value.OverallMaturityLevel` entre `Initial` e `Optimizing`
- Score por dimensão (Governance, Safety, Quality, Observability)

**Critério de Aceite:** HTTP 200 OK com relatório de maturidade

---

### TC-AIK-067 — Obter dashboard de uso de IA com breakdown por modelo

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | GetAiUsageDashboard |
| **Tipo** | Unitário |
| **Prioridade** | Média |
| **Handler** | `GetAiUsageDashboard.Handler` |

**Pré-condições:**
- 30 dias de uso registrado com múltiplos modelos

**Passos:**
1. Enviar `GetAiUsageDashboard.Query` com `PeriodDays=30`
2. Handler agrega tokens, custo, chamadas por modelo

**Resultado Esperado:**
- `result.Value.TotalTokensUsed > 0`
- `result.Value.TotalCostUsd > 0`
- `result.Value.ByModel` lista breakdown por modelo

**Critério de Aceite:** HTTP 200 OK com dashboard completo

---

## 18. Isolamento Multi-Tenant

### TC-AIK-068 — Tenant A não acessa agentes do Tenant B

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | GetAgent |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |
| **Handler** | `GetAgent.Handler` + `TenantRlsInterceptor` |

**Pré-condições:**
- Tenant A com agente `agent-a` criado
- Tenant B autenticado separadamente

**Passos:**
1. Autenticar como Tenant B
2. Enviar `GetAgent.Query` com `AgentId=agent-a` (pertence ao Tenant A)
3. `TenantRlsInterceptor` aplica RLS no PostgreSQL
4. Handler executa query com `SET app.current_tenant_id = tenant-b`

**Resultado Esperado:**
- `result.IsSuccess == false`
- `result.Error.Type == ErrorType.NotFound`
- Nenhum dado do Tenant A exposto ao Tenant B

**Critério de Aceite:** HTTP 404; isolamento RLS confirmado

---

### TC-AIK-069 — Tenant sem capability ai_governance recebe Forbidden

| Campo | Valor |
|-------|-------|
| **Módulo** | AIKnowledge |
| **Feature** | CreateAgent |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |
| **Handler** | `TenantIsolationBehavior` |

**Pré-condições:**
- Tenant com plano `Starter` sem capability `ai_governance`

**Passos:**
1. Tenant Starter tenta enviar `CreateAgent.Command`
2. `TenantIsolationBehavior` verifica capabilities do JWT
3. Capability `ai_governance` ausente — rejeita requisição

**Resultado Esperado:**
- `result.IsSuccess == false`
- `result.Error.Type == ErrorType.Forbidden`
- Mensagem: "This feature requires the ai_governance plan."

**Critério de Aceite:** HTTP 403 Forbidden sem handler executado

---

*Fim do documento — 69 casos de teste para o módulo AIKnowledge*
