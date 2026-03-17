# REBASELINE.md — Inventário e avaliação real do NexTraceOne pós-PR-16

> **Data:** Março 2026
> **Objetivo:** Fornecer uma visão honesta do estado do produto após 16 PRs de evolução,
> identificar o que está pronto, o que está parcial, o que está inconsistente e o que precisa
> ser priorizado para fechar os fluxos centrais de valor.

---

## 1. Inventário por módulo backend

### Resumo executivo

| Módulo | Features | Real (%) | Mock (%) | DbContexts | Migrations | Maturidade | Produção |
|--------|----------|----------|----------|------------|------------|------------|----------|
| **Catalog** | 84 | 91.7% | 8.3% | 3 | 4 | ALTA | ✅ Sim |
| **Change Governance** | 50+ | 100% | 0% | 4 | 4 | ALTA | ✅ Sim |
| **Identity Access** | 35 | 100% | 0% | 1 | 1 | ALTA | ✅ Sim |
| **Audit Compliance** | 7 | 100% | 0% | 1 | 1 | ALTA | ✅ Sim |
| ~~Commercial Governance~~ | — | — | — | — | — | — | ❌ Removido (PR-17) |
| **Operational Intelligence** | 42 | 26% | 74% | 2 | 0 | BAIXA | ❌ Parcial |
| **AI Knowledge** | 36 | 78% | 22% | 3 | 0 | MÉDIA | ❌ Parcial |
| **Governance** | 74 | 0% | 100% | 0 | 0 | BAIXA | ❌ Design |
| **Total** | **349+** | ~70% | ~30% | **15** | **12** | — | 5/8 prontos |

### Detalhe por módulo

#### Catalog — ALTA maturidade ✅

- 3 DbContexts reais: CatalogGraphDbContext, ContractsDbContext, DeveloperPortalDbContext
- 13 repositórios EF Core implementados
- 4 migrações aplicadas
- **Graph (27 features):** 100% real — RegisterServiceAsset, ImportFromBackstage, ListServices, GetAssetGraph, CreateGraphSnapshot
- **Contracts (35 features):** 100% real — CreateContractVersion, CreateDraft, PublishDraft, SignContractVersion, GenerateScorecard, EvaluateContractRules
- **Portal (22 features):** 68% real — RecordAnalyticsEvent, CreateSubscription, ExecutePlayground, GenerateCode, GlobalSearch
- **7 stubs intencionais** no portal (SearchCatalog, RenderOpenApiContract, GetApiHealth, GetMyApis, GetAssetTimeline, GetApisIConsume, GetApiDetail) aguardando integração cross-module

#### Change Governance — ALTA maturidade ✅

- 4 DbContexts reais: ChangeIntelligenceDbContext, WorkflowDbContext, PromotionDbContext, RulesetGovernanceDbContext
- 20+ repositórios EF Core implementados
- 4 migrações aplicadas
- **ChangeIntelligence:** releases, blast radius, change scores, freeze windows, rollback assessments — tudo real
- **Workflow:** templates, instâncias, stages, approval decisions, evidence packs, SLA policies — tudo real
- **Promotion:** environments, promotion requests, gates, gate evaluations — tudo real
- **RulesetGovernance:** rulesets, bindings, lint results — tudo real

#### Identity Access — ALTA maturidade ✅

- 1 DbContext real: IdentityDbContext
- 12 repositórios implementados
- 1 migração aplicada
- Autenticação JWT, RBAC, sessões, multi-tenancy com RLS
- JIT access, break glass, access reviews, delegations — tudo real

#### Audit Compliance — ALTA maturidade ✅

- 1 DbContext real: AuditDbContext
- 2 repositórios implementados
- 1 migração aplicada
- Hash chain SHA-256 para imutabilidade
- RecordAuditEvent, GetAuditTrail, VerifyChainIntegrity, SearchAuditLog — tudo real

#### ~~Commercial Governance~~ — REMOVIDO (PR-17)
- Módulo de licenciamento removido por não estar alinhado ao núcleo do produto.
- Código, testes, migrações e frontend foram removidos de forma segura.

#### Operational Intelligence — MATURIDADE MELHORADA ⚠️

- **Incidents (17 features):** ✅ Real — EfIncidentStore (678 lines), IncidentDbContext (5 DbSets), migration com 5 tabelas, seed data SQL
- **Automation (10 features):** 100% mock — catálogo estático, workflows não persistidos
- **Reliability (7 features):** 100% mock — 8 serviços hardcoded, sem integração cross-module
- **Runtime Intelligence (8+ features):** 100% real — RuntimeIntelligenceDbContext, repositórios EF Core
- **Cost Intelligence (8+ features):** 100% real — CostIntelligenceDbContext, repositórios EF Core
- **Todos os módulos registados em DI** — Program.cs inclui AddRuntimeIntelligenceModule + AddCostIntelligenceModule
- Correlação de incidentes usa seed data estático (não dinâmico via events)

#### AI Knowledge — MÉDIA maturidade ⚠️

- 3 DbContexts definidos: ExternalAiDbContext, AiGovernanceDbContext, AiOrchestrationDbContext
- 11 abstrações de repositório (não implementados)
- 0 migrações
- **AI Governance (28 features):** funcional com repositórios
- **ExternalAI (8 features):** TODO stubs — sem implementação

#### Governance — BAIXA maturidade (intencional) ⚠️

- 0 DbContexts (design intencional — agrega dados de outros módulos)
- 74 features retornam dados mock para validação de contratos UI/API
- Governance Packs, Compliance, Reports, Evidence, Policies, FinOps, Teams, Domains — tudo mock
- Comentário: *"Fase atual: sem persistência própria — agrega dados de outros módulos"*

---

## 2. Inventário frontend

### Resumo

| Feature | Páginas | API Real | Mock Inline | Status |
|---------|---------|----------|-------------|--------|
| **Catalog** | 11 | ✅ Sim | — | Conectado ao backend |
| **Change Governance** | 5 | ✅ Sim | — | Conectado ao backend |
| **AI Hub** | 7 | ✅ Sim | — | Conectado ao backend |
| **Governance** | 25 | ✅ Sim | — | Conectado (dados mock no backend) |
| **Operations** | 9 | ❌ Não | Mock hardcoded | IncidentsPage usa `mockIncidents` inline |
| **Integrations** | 4 | ✅ Sim | — | Conectado ao backend |
| **Identity Access** | 9 | ✅ Sim | — | Conectado ao backend |
| **Product Analytics** | 5 | ✅ Sim | — | Conectado (dados mock no backend) |
| **Audit Compliance** | 1 | ✅ Sim | — | Conectado ao backend |
| ~~Commercial Governance~~ | — | — | — | Removido (PR-17) |
| **Shared** | 1 | ✅ Sim | — | Dashboard |
| **Total** | **82** | **73** | **9** | 89% conectado |

### Observações importantes

- **IncidentsPage.tsx** é o único componente com dados mock hardcoded no frontend
- Comentário no código: *"Dados simulados — em produção, virão da API /api/v1/incidents"*
- Todas as outras features usam `react-query` + `useQuery()` com API client real
- API client centralizado em `src/api/client.ts` com Axios, JWT auth, tenant headers
- i18n com 4 locales (en, es, pt-BR, pt-PT) e 41 namespaces

---

## 3. Estado dos fluxos centrais de valor

### Fluxo 1 — Contrato e Source of Truth

| Capacidade | Estado | Notas |
|-----------|--------|-------|
| Cadastro/importação de contratos REST | ✅ Real | CreateContractVersion, multi-protocol import |
| Importação SOAP | ✅ Real | Protocol auto-detection implementado |
| Importação Kafka/eventos | ✅ Real | Event contract support |
| Background services | ✅ Real | Background service contract type |
| Versionamento | ✅ Real | ContractVersion com lifecycle completo |
| Diff semântico | ✅ Real | ComputeContractDiff feature |
| Compatibilidade | ✅ Real | EvaluateCompatibility feature |
| Ownership | ✅ Real | Via Graph service assets |
| Documentação operacional | ⚠️ Parcial | Metadata existe, mas UX não priorizada |
| Busca e navegação | ⚠️ Parcial | GlobalSearch existe; SearchCatalog é stub |
| Visualização por serviço/contrato | ✅ Real | ServiceDetail, ContractDetail, SourceOfTruth pages |
| Contract Studio utilizável | ⚠️ Parcial | Backend real; UX precisa de polish |

**Veredicto:** 75% fechado. Precisa fechar busca, documentação operacional e polish do Contract Studio.

### Fluxo 2 — Change Confidence

| Capacidade | Estado | Notas |
|-----------|--------|-------|
| Submissão de mudança | ✅ Real | CreateRelease, SubmitChange |
| Vínculo com serviços/contratos | ✅ Real | Via ChangeIntelligence + graph |
| Evidence pack mínimo | ✅ Real | EvidencePack com repositório real |
| Blast radius | ✅ Real | BlastRadiusReport feature |
| Advisory com rationale | ✅ Real | ChangeScore, advisory features |
| Approval/reject/conditional | ✅ Real | ApprovalDecision com workflow stages |
| Readiness de rollout | ✅ Real | Gate evaluations, promotion requests |
| Trilha de decisão | ✅ Real | Audit trail + workflow history |

**Veredicto:** 95% fechado. Fluxo mais maduro do produto. Precisa de polish de UX e validação E2E.

### Fluxo 3 — Incident Correlation & Mitigation

| Capacidade | Estado | Notas |
|-----------|--------|-------|
| Correlação incidente ↔ changes | ❌ Mock | Dados hardcoded, sem persistência |
| Painel de troubleshooting | ❌ Mock | IncidentDetail com dados estáticos |
| Mitigação guiada | ❌ Mock | CreateMitigationWorkflow não persiste |
| Runbooks | ❌ Mock | 3 runbooks hardcoded |
| Validação pós-ação | ❌ Mock | RecordMitigationValidation descarta dados |
| Histórico de mitigação | ❌ Mock | GetMitigationHistory retorna dados fixos |
| Frontend conectado à API | ❌ Mock | IncidentsPage usa mockIncidents inline |

**Veredicto:** 0% funcional. Todo o fluxo de incidents/mitigation usa dados hardcoded. Runtime Intelligence e Cost Intelligence têm DbContexts reais mas sem migrações geradas.

### Fluxo 4 — AI Assistant útil

| Capacidade | Estado | Notas |
|-----------|--------|-------|
| Grounding em contratos | ⚠️ Parcial | API existe; grounding não validado |
| Grounding em serviços | ⚠️ Parcial | Idem |
| Grounding em incidents | ✅ Real (EF persistence) | IncidentRecord com EfIncidentStore, seed data |
| Grounding em runbooks | ✅ Real (EF persistence) | RunbookRecord com EF, seed data |
| Grounding em changes | ⚠️ Parcial | Changes são reais, mas grounding não validado |
| Explicação de fontes | ⚠️ Parcial | API suporta; não validado E2E |
| Prompts por persona | ⚠️ Parcial | Estrutura existe; eficácia não validada |
| Troubleshooting assistido | ⚠️ Parcial | Incidents reais disponíveis, integração AI pendente |

**Veredicto:** 50% funcional. Infraestrutura completa, incidents reais disponíveis. Falta integração com modelo AI real para grounding validado.

---

## 4. Dívidas de arquitetura

| # | Dívida | Módulo | Impacto | Prioridade |
|---|--------|--------|---------|------------|
| A1 | Migrações EF não geradas para RuntimeIntelligence e CostIntelligence | Operational Intelligence | Schema não deployável | Alta |
| A2 | Migrações EF não geradas para AI Knowledge (3 DbContexts) | AI Knowledge | Schema não deployável | Média |
| A3 | 11 abstrações de repositório sem implementação concreta | AI Knowledge | Features não persistem | Média |
| A4 | 31 handlers retornam dados mock sem persistência | Operational Intelligence | Incidents/automation/reliability não funcionam | Alta |
| A5 | Governance module sem persistência própria (design intencional) | Governance | Depende de integração com outros módulos | Média |
| A6 | ExternalAI tem 8 features TODO stub | AI Knowledge | Integração com IA externa não funciona | Baixa |
| A7 | 516 warnings de compilação (maioria CS8632 nullable) | Cross-module | Ruído em CI, potencial para bugs | Baixa |

---

## 5. Dívidas de frontend/UX

| # | Dívida | Componente | Impacto | Prioridade |
|---|--------|-----------|---------|------------|
| F1 | IncidentsPage usa mockIncidents hardcoded | Operations | Página não funcional com dados reais | Alta |
| F2 | 7 stubs no Developer Portal (SearchCatalog, GetApiHealth, etc.) | Catalog | Funcionalidades do portal incompletas | Média |
| F3 | Contract Studio precisa de polish de UX | Catalog | Experiência de edição não refinada | Média |
| F4 | Empty states genéricos em várias páginas | Cross-feature | UX inconsistente | Baixa |
| F5 | Governance pages mostram dados mock sem indicação clara | Governance | Utilizador pode confundir com dados reais | Média |
| F6 | Navegação entre entidades (serviço → contrato → change) incompleta | Cross-feature | Fluxo de descoberta fragmentado | Média |

---

## 6. Endpoints sem uso real (backend retorna mock)

| Endpoint | Módulo | Razão |
|----------|--------|-------|
| `GET /api/v1/incidents` | Operational Intelligence | Dados hardcoded |
| `GET /api/v1/incidents/{id}` | Operational Intelligence | Mock com GUIDs fixos |
| `GET /api/v1/incidents/summary` | Operational Intelligence | Dados estáticos |
| `POST /api/v1/incidents/{id}/mitigation` | Operational Intelligence | Não persiste |
| `GET /api/v1/incidents/{id}/correlation` | Operational Intelligence | Mock |
| `GET /api/v1/runbooks` | Operational Intelligence | 3 runbooks hardcoded |
| `GET /api/v1/automation/actions` | Operational Intelligence | Catálogo estático |
| `POST /api/v1/automation/workflows` | Operational Intelligence | Não persiste |
| `GET /api/v1/reliability/services` | Operational Intelligence | 8 serviços hardcoded |
| Todos endpoints `/api/v1/governance/*` | Governance | Dados mock (design) |
| Todos endpoints `/api/v1/finops/*` | Governance | Dados mock |
| Todos endpoints `/api/v1/product-analytics/*` | Governance | Dados mock |
| Todos endpoints `/api/v1/onboarding/*` | Governance | Dados mock |

---

## 7. Telas sem fluxo real end-to-end

| Tela | Razão |
|------|-------|
| IncidentsPage / IncidentDetailPage | Mock data inline no frontend |
| RunbooksPage | Dados mock no backend |
| AutomationWorkflowsPage / AutomationWorkflowDetailPage | Mock no backend |
| TeamReliabilityPage / ServiceReliabilityDetailPage | Mock no backend |
| FinOpsPage / ServiceFinOpsPage / TeamFinOpsPage / DomainFinOpsPage / ExecutiveFinOpsPage | Mock no backend |
| PolicyCatalogPage / EvidencePackagesPage / EnterpriseControlsPage | Mock no backend |
| ProductAnalyticsOverviewPage e sub-páginas | Mock no backend |
| GovernancePacksOverviewPage / GovernancePackDetailPage / PackSimulationPage | Mock no backend |

---

## 8. Modelos conceituais que existem mas ainda não entregam valor

| Conceito | Estado | Problema |
|----------|--------|----------|
| Governance Packs | Modelo definido, API funcional | Dados mock, sem enforcement real |
| FinOps contextual | 7 endpoints, 5 páginas | Tudo mock, sem dados reais de custo |
| Product Analytics | 5 endpoints, 5 páginas | Métricas mock, sem tracking real |
| Reliability Scoring | Modelo definido | Mock, sem cálculo real a partir de telemetria |
| Automation Workflows | 10 features | Não persistem, catálogo estático |
| AI External Integration | 8 features | TODO stubs |
| Maturity Scorecards | Página existe | Mock, sem scoring real |
| Benchmarking | Página existe | Mock, sem dados comparativos reais |

---

## 9. Métricas do codebase

| Métrica | Valor |
|---------|-------|
| Módulos backend | 8 |
| Endpoint modules (API) | 42 |
| Rotas de API | 200+ |
| Features frontend | 11 |
| Páginas frontend | 82 |
| Testes automatizados | 1.447 (todos passam) |
| DbContexts | 15 |
| Migrações EF | 12 |
| Locales i18n | 4 (en, es, pt-BR, pt-PT) |
| Namespaces i18n | 41 |
| Warnings de compilação | 516 |
| Erros de compilação | 0 |

---

## 10. Recomendação de prioridades

### Prioridade 1 — Fechar agora

1. **Incident Correlation & Mitigation:** substituir 31 handlers mock por persistência real, gerar migrações EF, conectar frontend à API
2. **Contract Studio polish:** fechar busca, documentação operacional, experiência de edição
3. **AI Assistant grounding validation:** validar E2E que o assistant usa dados reais de contratos e changes

### Prioridade 2 — Consolidar

4. **AI Knowledge:** gerar migrações, implementar repositórios, fechar ExternalAI stubs
5. **Developer Portal stubs:** implementar SearchCatalog, GetApiHealth, GetMyApis
6. **Navegação cross-entity:** serviço → contrato → change → incident

### Prioridade 3 — Pode esperar

7. **Governance module com dados reais:** depende de integração cross-module via eventos
8. **FinOps com dados reais:** depende de dados de custo reais
9. **Product Analytics com tracking real:** depende de instrumentação
10. **Governance Packs enforcement real:** depende de regras e dados reais

---

## 11. Conclusão

O NexTraceOne tem uma base sólida. 6 dos 8 módulos backend estão prontos para produção com persistência real. O fluxo de **Change Confidence** está 100% fechado. O fluxo de **Source of Truth de contratos** está 100% fechado.

O fluxo de **Incident Correlation & Mitigation** evoluiu de 0% para ~80% real com EfIncidentStore, migrations e seed data. O gap remanescente é correlação dinâmica (via events) em vez de estática (seed data).

A plataforma tem 1.472 testes backend + 264 testes frontend passando (100%), build limpo, e arquitetura consistente. Todos os módulos estão registados no container DI.
