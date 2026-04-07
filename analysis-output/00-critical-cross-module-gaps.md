> **⚠️ ARCHIVED — April 2026**: Este documento foi gerado como análise pontual de gaps. Muitos dos gaps aqui listados já foram resolvidos. Para o estado atual, consultar `docs/CONSOLIDATED-GAP-ANALYSIS-AND-ACTION-PLAN.md` e `docs/IMPLEMENTATION-STATUS.md`.

# NexTraceOne — Critical Cross-Module Gaps
**Forensic Analysis | June 2026**

---

## 1. Cross-Module Interfaces sem Implementação

### 1.1 `IIdentityModule` — NOT IMPLEMENTED
- **Severidade:** MEDIUM
- **Classificação:** INCOMPLETE
- **Ficheiro:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Contracts/ServiceInterfaces/IIdentityModule.cs`
- **Descrição:** Interface define 3 métodos (`GetUserByIdAsync`, `GetUserPermissionsAsync`, `ValidateTenantMembershipAsync`) mas nenhum `IdentityModuleService` existe no namespace `Infrastructure`.
- **Impacto:** Outros módulos não conseguem consultar dados de utilizador/permissão via contrato cross-module. Dependem de `ICurrentUser`/`ICurrentTenant` ou acesso direto ao `IdentityDbContext`.
- **Evidência:** Zero ficheiros que implementem `: IIdentityModule` no repositório.

### 1.2 `IPromotionModule` — NOT IMPLEMENTED
- **Severidade:** MEDIUM
- **Classificação:** INCOMPLETE
- **Ficheiro:** `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Contracts/Promotion/ServiceInterfaces/IPromotionModule.cs`
- **Descrição:** Interface define `IsPromotionApprovedAsync` e `GetPromotionStatusAsync`. Comentário no ficheiro: `IMPLEMENTATION STATUS: Planned — no implementation exists, no consumers.`
- **Impacto:** Nenhum módulo externo pode consultar estado de promoção de release.
- **Evidência:** Zero ficheiros que implementem `: IPromotionModule`.

### 1.3 `IRulesetGovernanceModule` — NOT IMPLEMENTED
- **Severidade:** MEDIUM
- **Classificação:** INCOMPLETE
- **Ficheiro:** `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Contracts/RulesetGovernance/ServiceInterfaces/IRulesetGovernanceModule.cs`
- **Descrição:** Interface define `GetRulesetScoreAsync` e `IsReleaseCompliantAsync`. Comentário: `IMPLEMENTATION STATUS: Planned — no implementation exists, no consumers.`
- **Impacto:** Scoring de conformidade de releases não disponível cross-module.
- **Evidência:** Zero ficheiros que implementem `: IRulesetGovernanceModule`.

---

## 2. Outbox Processing — Registado mas sem Consumers Reais

- **Severidade:** MEDIUM
- **Classificação:** PARTIAL
- **Descrição:** `ModuleOutboxProcessorJob<TContext>` está registado para **21 DbContexts** (verificado em `BackgroundWorkers/Jobs/`). O processamento genérico de outbox funciona (batch de 50, retry 5x, ciclo 5s). Porém, a maioria dos módulos não publica domain events para o outbox — o processor existe mas o inbox está vazio na maioria dos módulos.
- **Impacto:** A infraestrutura de outbox está pronta mas subutilizada. Eventos de domínio que deveriam propagar entre módulos (ex: `ChangeCreated → Incident correlation`, `ContractPublished → Notification`) provavelmente não estão a ser publicados.
- **Evidência:** `src/platform/NexTraceOne.BackgroundWorkers/Jobs/ModuleOutboxProcessorJob.cs` — 21 registos confirmados.

---

## 3. CI/CD Integration — Deploy Events são Stubs

- **Severidade:** HIGH
- **Classificação:** STUB
- **Descrição:** A ingestão de deploy/change events existe como infraestrutura (`ProcessIngestionPayload` handler é real, com parsing semântico), mas não existem conectores reais configurados para GitLab, Jenkins, GitHub Actions ou Azure DevOps. Os connectors no módulo `Integrations` são metadata-only.
- **Impacto:** Change Intelligence depende de dados de deploy reais. Sem ingestão real de CI/CD, blast radius, promotion gates e risk scoring operam com dados limitados.
- **Evidência:**
  - `src/modules/integrations/NexTraceOne.Integrations.Application/Features/ProcessIngestionPayload/ProcessIngestionPayload.cs` — handler real
  - `src/modules/integrations/` — connectors são stubs

---

## 4. Knowledge Module — Backend sem Frontend

- **Severidade:** HIGH
- **Classificação:** INCOMPLETE
- **Descrição:** O módulo Knowledge tem backend funcional com 5 endpoints (search, create document, create operational note, create relation, get by target, get by source), 3 entidades com EF configurations, migration confirmada e repositories reais. Porém **não existe feature module `knowledge` no frontend** — nenhuma página, nenhum componente, nenhuma rota.
- **Impacto:** Knowledge Hub é pilar #9 do produto. Documentação operacional, notas contextuais e relações de conhecimento são inacessíveis ao utilizador.
- **Evidência:**
  - Backend: `src/modules/knowledge/` — 27 .cs files, 5 endpoints, 3 entities
  - Frontend: `src/frontend/src/features/` — nenhuma pasta `knowledge`

---

## 5. Documentação Cross-Module Contraditória

- **Severidade:** HIGH
- **Classificação:** DOC_CONTRADICTION
- **Descrição:** Os seguintes documentos contêm informação **factualmente incorreta** comparada com o estado real do código:
  - `docs/IMPLEMENTATION-STATUS.md` — afirma `IContractsModule = PLAN` e `IChangeIntelligenceModule = PLAN` — ambos estão **IMPLEMENTED**
  - `docs/IMPLEMENTATION-STATUS.md` — afirma `ICostIntelligenceModule = PLAN` — está **IMPLEMENTED** por `CostIntelligenceModuleService.cs`
  - `docs/IMPLEMENTATION-STATUS.md` — afirma `IAiOrchestrationModule = PLAN` e `IExternalAiModule = PLAN` — ambos **IMPLEMENTED**
  - `docs/IMPLEMENTATION-STATUS.md` — afirma `IKnowledgeModule = PLAN` — mas `IKnowledgeModule` não existe como interface; Knowledge funciona via endpoints diretos
  - `docs/IMPLEMENTATION-STATUS.md` — afirma "Operations/Incidents: frontend mock" — frontend usa **real API calls** via `incidents.ts` (21 chamadas)
  - `docs/IMPLEMENTATION-STATUS.md` — afirma "AI Knowledge: sem LLM real E2E" — `SendAssistantMessage` usa **real IChatCompletionProvider**
  - `docs/CORE-FLOW-GAPS.md` — afirma "IncidentsPage.tsx uses mockIncidents hardcoded inline" — **false**, usa `useQuery` com API real
  - `docs/CORE-FLOW-GAPS.md` — afirma "SendAssistantMessage returns hardcoded responses" — **false**, invoca LLM real
  - `docs/CORE-FLOW-GAPS.md` — afirma "AiAssistantPage.tsx uses mockConversations" — **false**, usa `aiGovernanceApi.listConversations/sendMessage`
- **Impacto:** Qualquer novo contribuidor será enganado pela documentação. Decisões de roadmap baseadas nestes documentos serão incorretas.
- **Evidência:**
  - `docs/IMPLEMENTATION-STATUS.md` — §CrossModule, §OperationalIntelligence, §AIKnowledge
  - `docs/CORE-FLOW-GAPS.md` — §Flow 3, §Flow 4

---

## 6. Frontend Error Handling Coverage

- **Severidade:** MEDIUM
- **Classificação:** INCOMPLETE
- **Descrição:** De 106 páginas verificadas:
  - **76 (72%)** têm `isError`/`ErrorBoundary` handling
  - **30 (28%)** não têm tratamento de erro — erros de API são silenciosos
  - **35 (33%)** têm empty state pattern
  - **71 (67%)** não têm empty state — mostram conteúdo vazio sem feedback
  - **104 (98%)** têm loading state (isLoading/isPending/Skeleton)
- **Impacto:** 30 páginas falham silenciosamente em erro. 71 páginas mostram vazio sem explicação.
- **Evidência:** Scan automático de todas as páginas `*Page.tsx` em `src/frontend/src/features/`

---

## 7. Seed Strategy — 6 de 7 SQL Files Missing

- **Severidade:** CRITICAL
- **Classificação:** BROKEN
- **Descrição:** `DevelopmentSeedDataExtensions.cs` referencia 7 ficheiros SQL. Apenas `seed-incidents.sql` existe (4.9KB). Os outros 6 ficheiros não existem no disco.
- **Impacto:** Seed de desenvolvimento falha silenciosamente para identity, catalog, changegovernance, audit, aiknowledge e governance.
- **Evidência:** `src/platform/NexTraceOne.ApiHost/DevelopmentSeedDataExtensions.cs` (linhas 21-30), `src/platform/NexTraceOne.ApiHost/SeedData/` — apenas `seed-incidents.sql`
