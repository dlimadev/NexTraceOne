# Change Governance — Consolidated Module Report

> Gerado a partir da consolidação de todos os relatórios de auditoria e revisão modular do NexTraceOne.
> Última atualização: 2026-03-24

---

## 1. Visão Geral do Módulo

O módulo **Change Governance** é o pilar central de **Change Confidence** do NexTraceOne. Responsável por garantir que todas as mudanças em produção são rastreadas, avaliadas, governadas e auditáveis, o módulo cobre quatro subdomínios distintos:

| Subdomínio | Responsabilidade | Aggregate Roots |
|---|---|---|
| **ChangeIntelligence** | Tracking de releases, risk scoring, blast radius, post-release review, rollback assessment, freeze windows, deployment state | `Release` (referido como `Change` em alguns relatórios — ver secção 10, I-6) |
| **Workflow** | Templates de aprovação, instâncias runtime, estágios sequenciais, decisões, evidence packs, SLA | `WorkflowTemplate`, `WorkflowInstance` |
| **Promotion** | Promoção cross-environment, gates de avaliação, aprovações com bloqueio | `PromotionRequest` |
| **RulesetGovernance** | Linting de artefactos, regras de validação, bindings a asset types, scores agregados | `Ruleset` |

**Localização no código:**

- Backend: `src/modules/changegovernance/`
- Frontend: `src/frontend/src/features/change-governance/`
- Persistência: 4 DbContexts independentes (`ChangeIntelligenceDbContext`, `WorkflowDbContext`, `PromotionDbContext`, `RulesetGovernanceDbContext`)

---

## 2. Estado Atual

| Dimensão | Maturidade | Observação |
|---|---|---|
| **Backend** | 🟢 90% | CQRS completo, domínio sólido, 232 ficheiros, 40+ features |
| **Frontend** | 🟢 85% | 6 páginas funcionais, 5 API clients, 13 ficheiros totais |
| **Documentação** | 🟡 70% | Melhor documentação entre módulos, mas README de módulo inexistente |
| **Testes** | 🟢 80% | 179+ testes existentes, boa cobertura |
| **Maturidade global** | **🟢 81%** | Terceiro módulo mais maturo do produto |

- **Prioridade:** P2 (Pilar Core — Change Confidence)
- **Status:** 🟢 Funcional — módulo estável com gaps pontuais identificados
- **Classificação frontend:** `COMPLETE_APPARENT` (inventário de módulos)
- **Classificação domínio:** `COERENTE` — 4 bounded contexts claros e bem separados
- **Tipo de correção necessária:** `LOCAL_FIX`
- **Ordem de execução recomendada:** Wave 2–3

---

## 3. Problemas Críticos e Bloqueadores

Não foram identificados bloqueadores críticos neste módulo. Os problemas existentes são de severidade alta ou média e não impedem a operação corrente. Os três gaps mais relevantes, pela sua relação com a narrativa central do produto, são:

| # | Problema | Severidade | Impacto |
|---|---|---|---|
| 1 | Blast radius calculation parcial — cálculo de consumidores transitivos incompleto | 🟠 Alto | Reduz fiabilidade da análise de impacto, pilar central do Change Confidence |
| 2 | Correlação incidente-mudança dependente de dados em tempo real ainda não integrados | 🟠 Alto | Impede correlação automática change→incident, funcionalidade diferenciadora |
| 3 | Revisão detalhada do módulo (`08-releases-change-intelligence/`) não iniciada | 🟡 Médio | Toda a documentação granular (agents, endpoints, validações, domínio) está em estado `NOT_STARTED` com templates vazios |

---

## 4. Problemas por Camada

### 4.1 Frontend

O frontend do Change Governance está funcional e classificado como `COMPLETE_APPARENT`. Todas as 6 páginas estão operacionais com permissões configuradas e integradas no menu de navegação.

| Rota | Página | Ficheiro | Permissão | Status |
|---|---|---|---|---|
| `/changes` | ChangeCatalogPage | `change-governance/pages/ChangeCatalogPage.tsx` | `change-intelligence:read` | ✅ Funcional |
| `/changes/:changeId` | ChangeDetailPage | `change-governance/pages/ChangeDetailPage.tsx` | `change-intelligence:read` | ✅ Funcional |
| `/releases` | ReleasesPage | `change-governance/pages/ReleasesPage.tsx` | `change-intelligence:releases:read` | ✅ Funcional |
| `/workflow` | WorkflowPage | `change-governance/pages/WorkflowPage.tsx` | `workflow:read` | ✅ Funcional |
| `/promotion` | PromotionPage | `change-governance/pages/PromotionPage.tsx` | `promotion:read` | ✅ Funcional |
| `/platform/configuration/workflows` | WorkflowConfigurationPage | `change-governance/pages/WorkflowConfigurationPage.tsx` | `platform:admin:read` | ✅ Funcional |

**Gaps identificados:**

| # | Gap | Severidade | Descrição |
|---|---|---|---|
| F-1 | Validações de input não documentadas | 🟡 Médio | Não existe mapeamento das validações frontend nem da correspondência com validações backend |
| F-2 | Consistência i18n não auditada | 🟡 Médio | Relatório de i18n do módulo em estado `NOT_STARTED`; impossível confirmar cobertura completa |
| F-3 | Segmentação por persona não verificada | 🟡 Médio | As 6 páginas servem todas as personas sem diferenciação documentada de conteúdo/ações por papel |
| F-4 | API clients sem documentação detalhada | 🟢 Baixo | 5 API clients referenciados (`changeIntelligence`, `workflow`, `promotion`, `changeConfidence` + 1) mas sem inventário de chamadas |

### 4.2 Backend

O backend é a camada mais matura do módulo (90%), com padrão CQRS aplicado consistentemente e 4 bounded contexts bem definidos.

**Entidades do domínio (27 entidades em 4 contextos):**

| DbContext | Entidades | Outbox |
|---|---|---|
| `ChangeIntelligenceDbContext` | Release, ChangeEvent, BlastRadiusReport, ChangeIntelligenceScore, FreezeWindow, ReleaseBaseline, ObservationWindow, PostReleaseReview, RollbackAssessment, ExternalMarker, DeploymentState (11) | `ci_outbox_messages` |
| `WorkflowDbContext` | WorkflowTemplate, WorkflowInstance, WorkflowStage, ApprovalDecision, EvidencePack, SlaPolicy (6) | `wf_outbox_messages` |
| `PromotionDbContext` | DeploymentEnvironment, PromotionRequest, PromotionGate, GateEvaluation (4) | `prm_outbox_messages` |
| `RulesetGovernanceDbContext` | Ruleset, RulesetBinding, LintResult, LintFinding, LintExecution, RulesetScore (6) | `rg_outbox_messages` |

**Endpoints (~46 endpoints distribuídos por 4 módulos):**

| Módulo API | Endpoints principais | Estimativa |
|---|---|---|
| ChangeIntelligenceEndpointModule | `/api/changes/intelligence`, `/api/changes/blast-radius/{id}`, `/api/changes/correlation`, `/api/changes/confidence`, releases, markers, baseline, review, rollback-assessment, advisory, decisions, deployments, freeze-windows, analysis (classify, score, blast-radius) | ~20 |
| WorkflowEndpointModule | `/api/workflows/templates`, `/api/workflows/instances`, `/api/workflows/instances/{id}/approve`, approvals, status, evidence-packs | ~10 |
| PromotionEndpointModule | `/api/promotion/requests`, `/api/promotion/environments`, `/api/promotion/gates/{id}/override`, evaluate-gates, approve, block, gate-evaluations | ~9 |
| RulesetGovernanceEndpointModule | `/api/rulesets`, `/api/rulesets/{id}/execute`, upload, archive, bind, findings, scores | ~7 |

**Gaps identificados:**

| # | Gap | Severidade | Descrição |
|---|---|---|---|
| B-1 | Blast radius — cálculo de consumidores transitivos incompleto | 🟠 Alto | O `BlastRadiusReport` calcula consumidores directos mas a resolução transitiva via `Catalog Graph` não está completa |
| B-2 | Correlação incidente-mudança sem dados real-time | 🟠 Alto | Domain service de correlação existe mas depende de integração com telemetria em tempo real ainda não operacional |
| B-3 | API reference dos 46 endpoints inexistente | 🟡 Médio | Nenhum endpoint tem documentação detalhada de request/response, validações e erros |
| B-4 | Revisão granular de application services não realizada | 🟡 Médio | Command/query handlers, regras de validação e autorização não foram auditados individualmente |
| B-5 | Permissão `workflow:templates:write` usada em GET de templates | 🟢 Baixo | No `backend-endpoints-report.md`, o GET `/api/workflows/templates` requer `workflow:templates:write` em vez de `:read` |

### 4.3 Database

A camada de persistência é sólida, com 4 DbContexts isolados e outbox pattern implementado em todos eles.

| DbContext | DbSets | Foco |
|---|---|---|
| `ChangeIntelligenceDbContext` | 10 | Releases, blast radius, change scores |
| `WorkflowDbContext` | 6 | Templates, instances, stages, evidence, approvals |
| `PromotionDbContext` | 4 | Promotion requests, approvals, SLAs |
| `RulesetGovernanceDbContext` | 3 | Rulesets, rules, conditions |

**Gaps identificados:**

| # | Gap | Severidade | Descrição |
|---|---|---|---|
| D-1 | Revisão de schema não realizada | 🟡 Médio | O ficheiro `database/schema-review.md` está em estado `NOT_STARTED`; não há validação de índices, integridade referencial ou alinhamento schema↔domínio |
| D-2 | Revisão de migrations não realizada | 🟡 Médio | Sem auditoria de migrations — possíveis inconsistências não detectadas |
| D-3 | Seed data não verificada | 🟢 Baixo | Não existe confirmação de que seed data está alinhada com o estado actual do domínio |
| D-4 | Discrepância no número de DbSets do RulesetGovernanceDbContext | 🟢 Baixo | O `database-structural-audit.md` refere 3 DbSets, mas o `module-review.md` documenta 6 entidades (Ruleset, RulesetBinding, LintResult, LintFinding, LintExecution, RulesetScore) |

### 4.4 Segurança

| # | Gap | Severidade | Descrição |
|---|---|---|---|
| S-1 | Autorizações não auditadas granularmente | 🟡 Médio | O ficheiro `authorization-rules.md` está `NOT_STARTED`; não há mapeamento completo permissão↔endpoint↔página |
| S-2 | Restrições por ambiente não documentadas | 🟡 Médio | Freeze windows existem no domínio mas não há documentação de como aplicam restrições por tipo de ambiente (produção vs staging) |
| S-3 | Isolamento de tenant não verificado | 🟡 Médio | Sem auditoria específica de isolamento tenant nos 4 DbContexts do módulo |
| S-4 | Rate limiting inconsistente | 🟢 Baixo | Endpoint `/api/promotion/gates/{id}/override` classificado como `auth-sensitive` mas sem documentação de limites específicos |

### 4.5 IA e Agentes

O módulo integra o `AssistantPanel` do AI Hub para recomendações na `ChangeDetailPage`, mas a revisão completa de capacidades de IA está em estado `NOT_STARTED`.

| # | Gap | Severidade | Descrição |
|---|---|---|---|
| AI-1 | Agents de IA não definidos | 🟡 Médio | O ficheiro `agents-review.md` lista agents genéricos (Identity Security Agent, Access Review Agent) copiados do template de Identity — nenhum agent específico para Change Governance foi identificado |
| AI-2 | Capacidades de IA esperadas não mapeadas | 🟡 Médio | Faltam capacidades como: análise automática de blast radius, recomendação de rollback, classificação de risco por IA, sugestão de aprovadores |
| AI-3 | Governança de IA não verificada | 🟡 Médio | Sem documentação de quotas, auditoria de prompts/respostas, ou políticas de modelo para as interacções de IA no módulo |

### 4.6 Documentação

| # | Gap | Severidade | Descrição |
|---|---|---|---|
| DOC-1 | README do módulo backend inexistente | 🟠 Alto | Referenciado como critério de fecho em múltiplos relatórios; bloqueia onboarding de novos developers |
| DOC-2 | Documentação detalhada (`08-releases-change-intelligence/`) inteiramente vazia | 🟠 Alto | Todos os 18 ficheiros de revisão granular estão em estado `NOT_STARTED` com templates por preencher |
| DOC-3 | Fluxo completo de Change Confidence sem diagrama | 🟡 Médio | Falta diagrama end-to-end: Release → Score → BlastRadius → Review → Decision |
| DOC-4 | API reference dos 46 endpoints por criar | 🟡 Médio | Endpoints documentados apenas a nível de inventário, sem request/response detalhado |
| DOC-5 | Comentários de código não auditados | 🟢 Baixo | O `code-comments-review.md` está `NOT_STARTED` |

---

## 5. Dependências

### 5.1 Dependências de entrada (módulos de que Change Governance depende)

| Módulo | Tipo | Propósito | Criticidade |
|---|---|---|---|
| **Catalog Graph** | Query | Resolução de APIs e serviços afectados para cálculo de blast radius | Alta — sem Catalog, blast radius é inoperável |
| **Identity & Access** | Context | Contexto de utilizador, tenant e permissões para todas as operações | Alta — fundacional |
| **Configuration** | Settings | Configuração de workflow templates, promoção e freeze windows | Média |
| **Event Bus** | Outbox | Publicação de eventos de integração (4 outbox tables) | Média |

### 5.2 Dependências de saída (módulos que dependem de Change Governance)

| Módulo | Tipo | Descrição |
|---|---|---|
| **Operational Intelligence** | Parcial | Consome dados de mudanças para correlação com incidentes e dashboards operacionais |

### 5.3 Integrações externas

| Sistema | Tipo | Propósito | Estado |
|---|---|---|---|
| **Jira** | Sync | `AttachWorkItemContext`, `SyncJiraWorkItems` — ligação de releases a work items | ✅ Implementado |
| **CI/CD** | Ingestão | `ExternalMarker` — markers de pipelines externas para tracking de deployments | ✅ Implementado |
| **AI Hub** | UI | `AssistantPanel` na `ChangeDetailPage` — recomendações contextualizadas | ✅ Integrado |

---

## 6. Quick Wins

Acções de baixo esforço e alto impacto que podem ser implementadas imediatamente:

| # | Acção | Esforço | Impacto | Severidade do gap |
|---|---|---|---|---|
| QW-1 | Criar README do módulo (`src/modules/changegovernance/README.md`) | 2h | Alto — critério de fecho e onboarding | 🟠 Alto |
| QW-2 | Corrigir permissão GET `/api/workflows/templates` de `write` para `read` | 30min | Médio — princípio do menor privilégio | 🟡 Médio |
| QW-3 | Validar fluxo completo Release → Score → BlastRadius → Review → Decision | 3h | Alto — confirma integridade do fluxo core | 🟠 Alto |
| QW-4 | Validar fluxo Workflow: Template → Instance → Stages → Approval | 2h | Alto — confirma fluxo de aprovação | 🟠 Alto |
| QW-5 | Validar fluxo Promotion: Request → Gates → Approve/Block | 2h | Alto — confirma promoção cross-environment | 🟠 Alto |
| QW-6 | Validar Freeze Windows e detecção de conflitos | 1h | Médio — segurança operacional | 🟡 Médio |
| QW-7 | Verificar integração Jira (`AttachWorkItemContext`) | 1h | Médio — integração externa | 🟡 Médio |
| QW-8 | Documentar fluxo Change Confidence com diagrama end-to-end | 3h | Médio — compreensão do módulo | 🟡 Médio |

---

## 7. Refactors Estruturais

Acções de maior esforço que requerem planeamento:

| # | Refactor | Esforço | Prioridade | Descrição |
|---|---|---|---|---|
| RF-1 | Completar blast radius — resolução transitiva via Catalog Graph | 1–2 semanas | 🟠 Alto | Actualmente calcula consumidores directos; necessita traversal do grafo de dependências para consumidores transitivos. Depende de query service do Catalog. |
| RF-2 | Integrar correlação incidente-mudança com dados real-time | 2–3 semanas | 🟠 Alto | O domain service de correlação existe mas precisa de ligação a stream de telemetria/eventos de incidentes em tempo real. Depende de Operational Intelligence. |
| RF-3 | Criar API reference completa dos 46 endpoints | 1 semana | 🟡 Médio | Documentar request/response, validações, erros, autorizações e exemplos para cada endpoint. |
| RF-4 | Preencher revisão granular (`08-releases-change-intelligence/`) | 2–3 semanas | 🟡 Médio | 18 ficheiros de auditoria por camada estão vazios; preenchê-los completaria a documentação de qualidade do módulo. |
| RF-5 | Definir e implementar agents de IA específicos para Change Governance | 2–3 semanas | 🟡 Médio | Agents para: análise automática de blast radius, classificação de risco, recomendação de rollback, sugestão de aprovadores. |
| RF-6 | Auditar e testar isolamento de tenant nos 4 DbContexts | 3–5 dias | 🟡 Médio | Verificar que RLS ou filtros equivalentes estão aplicados em `ChangeIntelligenceDbContext`, `WorkflowDbContext`, `PromotionDbContext`, `RulesetGovernanceDbContext`. |

---

## 8. Critérios de Fecho

O módulo Change Governance será considerado `DONE` quando todos os critérios abaixo estiverem satisfeitos:

### Critérios obrigatórios (extraídos de `module-closure-plan.md` e `module-consolidation-report.md`)

- [ ] Blast radius funcional — incluindo resolução de consumidores transitivos
- [ ] README do módulo criado e publicado
- [ ] Testes de correlação incidente-mudança implementados e a passar
- [ ] Cobertura de testes mantida ≥ 80%

### Critérios complementares (extraídos de `module-overview.md` e boas práticas do produto)

- [ ] Todas as 6 páginas frontend revistas e aprovadas
- [ ] Todos os 46 endpoints documentados com request/response
- [ ] Regras de domínio auditadas e testadas em cada bounded context
- [ ] Autorizações completas e consistentes (permissão↔endpoint↔página)
- [ ] Schema de base de dados validado contra o modelo de domínio
- [ ] Capacidades de IA definidas e governadas
- [ ] Agents de IA configurados e auditáveis
- [ ] i18n verificado em todo o frontend do módulo
- [ ] Segurança e auditoria validadas (isolamento tenant, rate limiting)
- [ ] Documentação de onboarding pronta

---

## 9. Plano de Acção Priorizado

### Fase 1 — Validação (1 semana)

| # | Acção | Referência | Esforço |
|---|---|---|---|
| 1.1 | Validar fluxo Release → Score → BlastRadius → Review → Decision | QW-3 | 3h |
| 1.2 | Validar fluxo Workflow: Template → Instance → Stages → Approval | QW-4 | 2h |
| 1.3 | Validar fluxo Promotion: Request → Gates → Approve/Block | QW-5 | 2h |
| 1.4 | Validar Freeze Windows e conflitos | QW-6 | 1h |
| 1.5 | Verificar integração Jira | QW-7 | 1h |

### Fase 2 — Quick Wins (1 semana)

| # | Acção | Referência | Esforço |
|---|---|---|---|
| 2.1 | Criar README do módulo | QW-1, DOC-1 | 2h |
| 2.2 | Corrigir permissão GET templates workflow | QW-2, B-5 | 30min |
| 2.3 | Documentar fluxo Change Confidence com diagrama | QW-8, DOC-3 | 3h |
| 2.4 | Verificar cobertura de testes actual (confirmar ≥80%) | — | 2h |
| 2.5 | Verificar contagem real de DbSets em `RulesetGovernanceDbContext` e normalizar documentação | D-4, I-2 | 1h |

### Fase 3 — Completude funcional (2–3 semanas)

| # | Acção | Referência | Esforço |
|---|---|---|---|
| 3.1 | Completar blast radius com resolução transitiva | RF-1, B-1 | 1–2 semanas |
| 3.2 | Implementar testes de correlação incidente-mudança | RF-2, B-2 | 1 semana |
| 3.3 | Auditar isolamento de tenant nos 4 DbContexts | RF-6, S-3 | 3–5 dias |

### Fase 4 — Documentação e IA (2–3 semanas)

| # | Acção | Referência | Esforço |
|---|---|---|---|
| 4.1 | Criar API reference dos 46 endpoints | RF-3, B-3 | 1 semana |
| 4.2 | Preencher revisão granular do módulo | RF-4, DOC-2 | 2–3 semanas |
| 4.3 | Definir agents de IA para Change Governance | RF-5, AI-1/AI-2 | 2–3 semanas |

---

## 10. Inconsistências entre Relatórios

As seguintes discrepâncias foram identificadas ao cruzar os diversos relatórios de auditoria:

| # | Inconsistência | Fontes | Severidade | Detalhe |
|---|---|---|---|---|
| I-1 | **Número de entidades do domínio diverge** | `module-review.md` indica 27 entidades; `backend-domain-report.md` refere 47 entidades | 🟡 Médio | O relatório de domínio contabiliza provavelmente value objects e tipos auxiliares; o module-review conta apenas entidades mapeadas. Necessita clarificação. |
| I-2 | **DbSets do RulesetGovernanceDbContext divergem** | `database-structural-audit.md` refere 3 DbSets; `module-review.md` documenta 6 entidades (Ruleset, RulesetBinding, LintResult, LintFinding, LintExecution, RulesetScore) | 🟢 Baixo | Provável que 3 das 6 entidades sejam owned types ou não tenham DbSet dedicado. Requer verificação no código. |
| I-3 | **Rota do WorkflowConfigurationPage diverge** | `module-review.md` indica `/platform/configuration/workflows`; `module-closure-plan.md` indica `/workflow/configuration` | 🟢 Baixo | Necessita verificação no router do frontend para determinar a rota correcta. |
| I-4 | **Permissão de GET em templates de workflow** | `backend-endpoints-report.md` atribui `workflow:templates:write` ao GET `/api/workflows/templates` | 🟡 Médio | Provavelmente um erro no relatório ou no código — GET deveria requerer `:read`. |
| I-5 | **Documentação detalhada do módulo refere o módulo errado** | Ficheiros em `08-releases-change-intelligence/` (README.md, module-overview.md) contêm descrições copiadas do módulo Identity & Access (mencionam "autenticação", "MFA", "OIDC", "SAML") | 🟠 Alto | Templates não foram adaptados ao módulo correcto. Todo o conteúdo dos 18 ficheiros de revisão granular é placeholder sem dados reais de Change Governance. |
| I-6 | **Número de Aggregate Roots diverge** | `module-review.md` identifica 4 aggregate roots (Release, WorkflowTemplate, WorkflowInstance, PromotionRequest, Ruleset = 5); `backend-domain-report.md` identifica 4 (Change, PromotionRequest, Ruleset, WorkflowInstance) usando nomes ligeiramente diferentes | 🟢 Baixo | `Release` vs `Change` como nome do aggregate root de ChangeIntelligence; `WorkflowTemplate` listado como aggregate root separado num relatório mas não no outro. Nomenclatura necessita normalização. |
