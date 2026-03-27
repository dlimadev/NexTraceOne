# NexTraceOne — Fase 0 — Rebaseline Arquitetural (revalidação contra o código atual)

> **Data:** 2026-03-18  
> **Fonte de verdade (documentos):**
> - `docs/acceptance/NexTraceOne_Baseline_Estavel.md`
> - `docs/NexTraceOne_Plano_Operacional_Finalizacao.md`
> - `docs/planos/NexTraceOne_Fase10_Plano_Evolucao.md`
> - Diagnóstico histórico revalidado: `docs/REBASELINE.md` + `docs/SOLUTION-GAP-ANALYSIS.md`
> 
> **Regra aplicada:** *o código atual vale mais que documentação*.

---

## 1) Resumo executivo atualizado

O código atual confirma que o NexTraceOne continua alinhado com a baseline: **modular monolith**, DDD/CQRS, `Result<T>`, IDs strongly typed, multi-tenancy com interceptors e endpoint modules por bounded context.

A revalidação mostra uma mudança material desde o diagnóstico histórico (`docs/REBASELINE.md`, `docs/planos/NexTraceOne_Fase10_Plano_Evolucao.md`): **Governance deixou de ser “0% persistência”** e agora tem `GovernanceDbContext`, repositórios, configurações EF Core e migrações. Também foi implementada persistência real para **Product Analytics** (event store + agregações) e **Integration Hub** já está estruturado para persistência via repositórios.

Em contrapartida, ainda existem lacunas reais relevantes para produção:

- **Test suite não está verde no estado atual**: `dotnet test` reportou *1397 testes, 2 falhas* (ambas em `NexTraceOne.Governance.Tests`).
- **Migrations ausentes** para alguns DbContexts (principalmente `OperationalIntelligence.Runtime` e `OperationalIntelligence.Cost`; e `AIKnowledge.ExternalAI`/`AIKnowledge.Orchestration`).
- Algumas áreas seguem “placeholder/mock” por design/escopo (ex.: simulações e contagens cross-module em Governance, páginas de Platform Operations e partes avançadas de Contracts workspace).

---

## 2) Matriz de revalidação (achados do relatório crítico vs código atual)

Legenda de status:
- **Continua verdadeiro**
- **Parcialmente verdadeiro**
- **Já foi resolvido**
- **Ficou desatualizado**
- **Precisa de reclassificação**

> Observação: “achado original” é tratado como *hipótese histórica* (documento), e “evidência atual” referencia *código concreto*.

| Categoria | Achado original (histórico) | Fonte histórica | Status (revalidado) | Evidência no código atual | Nota técnica atualizada |
|---|---|---|---|---|---|
| Arquitetura base | Modular monolith com bounded contexts, DDD/CQRS, Result pattern | `docs/SOLUTION-GAP-ANALYSIS.md` | Continua verdadeiro | `NexTraceOne.sln` (múltiplos módulos + building blocks); `src/platform/NexTraceOne.ApiHost/Program.cs` mapeia módulos e endpoints | Sem divergência relevante detectada |
| Arquitetura base | Host com health endpoints e auto-migrations | baseline docs | Continua verdadeiro | `src/platform/NexTraceOne.ApiHost/Program.cs` (`/health`, `/ready`, `/live`, `ApplyDatabaseMigrationsAsync()`) | Produção readiness depende de migrations completas por módulo |
| Building Blocks | `Result<T>`, guard clauses, interceptors de auditoria/RLS | `docs/SOLUTION-GAP-ANALYSIS.md` | Continua verdadeiro | Ex.: `src/building-blocks/.../Results/Result.cs`; `src/modules/governance/.../DependencyInjection.cs` registra `AuditInterceptor`, `TenantRlsInterceptor` | Padrão consistente |
| Módulos core | Catalog/Contracts: catálogo de contratos e studio real; enriquecimento do catálogo deve vir do backend (sem mock) | `docs/planos/NexTraceOne_Fase10_Plano_Evolucao.md` (Trilha 1) | Já foi resolvido (1A confirmado) | Backend: `src/modules/catalog/.../ListContracts/ListContracts.cs` faz join `ContractVersion`→`ApiAsset`→`ServiceAsset`; Frontend: `src/frontend/.../ContractCatalogPage.tsx` declara “Dados enriquecidos vêm do backend real” | O ficheiro `mockEnrichment.ts` ainda existe no repo, mas a página já não depende dele (requer limpeza opcional) |
| Governance | “Governance não tem DbContext, não tem repositories, 100% mock” | `docs/REBASELINE.md`; `docs/planos/NexTraceOne_Fase10_Plano_Evolucao.md` (Trilha 2) | Ficou desatualizado | `src/modules/governance/NexTraceOne.Governance.Infrastructure/Persistence/GovernanceDbContext.cs`; migrations em `.../Persistence/Migrations/*`; DI registra repositórios em `.../DependencyInjection.cs` | O diagnóstico histórico precisa ser revisto: Governance agora tem persistência real (3A) |
| Governance | Handlers core (Teams/Domains/Packs/Waivers) devem usar repositórios e não retornar in-memory | `docs/planos/NexTraceOne_Fase10_Plano_Evolucao.md` (3B) | Parcialmente verdadeiro | Ex.: `src/modules/governance/.../Features/ListTeams/ListTeams.cs` usa `ITeamRepository` (real), mas ainda tem campos calculados como TODO (ex.: contagens cross-module) | Core CRUD está conectado; enriquecimento cross-module ainda pendente |
| Governance / Integrations | Integration Hub era mock e sem persistência | `docs/planos/NexTraceOne_Fase10_Plano_Evolucao.md` (Trilha 3) | Parcialmente verdadeiro | Persistência existe via `GovernanceDbContext` (`DbSet<IntegrationConnector>`, etc) e DI registra `IIntegrationConnectorRepository` etc. Testes unitários já mockam repositórios: `tests/modules/governance/.../IntegrationHubFeatureTests.cs` | Pipeline end-to-end (Ingestion API real) ainda pode estar incompleto; mas o módulo já não é “sem infraestrutura” |
| Governance / Product Analytics | Product Analytics era 100% mock e sem pipeline de eventos | `docs/planos/NexTraceOne_Fase10_Plano_Evolucao.md` (Trilha 4) | Precisa de reclassificação | Há persistência real: `AnalyticsEvent` entity + `AddAnalyticsEvents` migration + `IAnalyticsEventRepository` + handlers (`RecordAnalyticsEvent`, `GetAnalyticsSummary`, `GetModuleAdoption`) | “Pipeline” existe para captura via API; ainda falta fechar tracking completo no frontend (consistência de eventos) |
| AIKnowledge | “AiGovernanceDbContext existe mas 0 migrations” | `docs/REBASELINE.md` | Ficou desatualizado | Migration presente em `src/modules/aiknowledge/.../Governance/Persistence/Migrations/20260318084918_InitialAiGovernanceSchema.cs` | AI Governance está mais próximo de produção |
| AIKnowledge | ExternalAI e Orchestration com stubs / incompletos | `docs/REBASELINE.md`; `docs/SOLUTION-GAP-ANALYSIS.md` | Continua verdadeiro (parcial) | `src/modules/aiknowledge/.../ExternalAI/Persistence/ExternalAiDbContext.cs` e `.../Orchestration/Persistence/AiOrchestrationDbContext.cs` sem pasta `Migrations` | Persistência ainda não deployável nessas subáreas |
| OperationalIntelligence | “IncidentsPage no frontend usa mock inline” | `docs/REBASELINE.md` | Ficou desatualizado | `src/frontend/src/features/operations/pages/IncidentsPage.tsx` usa `react-query` + `incidentsApi.*` | Frontend agora está conectado |
| OperationalIntelligence | Runtime/Cost DbContexts sem migrations geradas (schema não deployável) | `docs/REBASELINE.md` | Continua verdadeiro | `src/modules/operationalintelligence/.../Runtime/Persistence` e `.../Cost/Persistence` não têm `Migrations/` | Bloqueador real para produção dessas áreas |
| Testes | “1.4k+ testes a passar” | `docs/SOLUTION-GAP-ANALYSIS.md` / baseline | Parcialmente verdadeiro | Execução atual: `dotnet test` total 1397, **2 falhas** (Governance tests) | Há regressão no CI quality gate: suite não está verde |
| Frontend | i18n e persona-aware UX maduros | baseline / `docs/SOLUTION-GAP-ANALYSIS.md` | Continua verdadeiro (amostragem) | Ex.: `ContractCatalogPage.tsx` e `AiAssistantPage.tsx` usam `useTranslation()`; PersonaContext em uso no AI Assistant | Não foi feita auditoria completa aqui (fora do escopo da fase), mas não há evidência contrária |
| Segurança | RequirePermission/segurança transversal ativa | `docs/SOLUTION-GAP-ANALYSIS.md` | Continua verdadeiro (amostragem) | `src/platform/NexTraceOne.ApiHost/Program.cs` registra `AddBuildingBlocksSecurity`; endpoints mapeados via modules; rate limiter e security headers habilitados | Revalidação profunda de policies fica para fase posterior |
| Documentação | Baseline estável e fase 10 refletem realidade | baseline / planos | Parcialmente verdadeiro | `docs/planos/NexTraceOne_Fase10_Plano_Evolucao.md` ainda afirma Governance “sem persistência”, o que o código já contradiz | A documentação de Fase 10 precisa atualização incremental |
| Produção readiness | “Migrations verificadas e seed idempotente” | `docs/acceptance/NexTraceOne_Baseline_Estavel.md` | Parcialmente verdadeiro | Há auto-migrations e múltiplas migrations. Porém existem DbContexts sem migrations (Runtime/Cost; ExternalAI/Orchestration) | Produção readiness é “por módulo”, não uniforme |

---

## 3) Achados que continuam válidos (estado atual)

- Arquitetura modular monolítica e composição por módulos (ApiHost + EndpointModules) continuam coerentes (`src/platform/NexTraceOne.ApiHost/Program.cs`).
- Building blocks continuam sendo a base transversal (Result pattern, interceptors, security/observability).
- Contracts/Catalog seguem como pilar forte do produto e reforçam “Source of Truth”, agora com enriquecimento real do catálogo via backend (`ListContracts`).
- Falta de migrations em partes de `OperationalIntelligence` (Runtime/Cost) e de `AIKnowledge` (ExternalAI/Orchestration) continua como **dívida de arquitetura real**.

---

## 4) Achados que ficaram desatualizados (corrigidos pelo código)

- Governance “sem DbContext e sem persistência” (agora existe `GovernanceDbContext`, EF configurations, migrations e repositórios).
- Product Analytics “100% mock/sem persistência” (agora existe `AnalyticsEvent` + repository + handlers + migration `AddAnalyticsEvents`).
- Frontend Operations/Incidents “mock inline” (agora usa API client + `react-query`).
- AI Knowledge “0 migrations” (ao menos o *submódulo Governance* tem migration `InitialAiGovernanceSchema`).

---

## 5) Achados parcialmente resolvidos

- Governance handlers core usam repositórios, mas ainda há campos placeholder/TODO (ex.: contagens cross-module e maturity). O módulo deixou de ser “design only”, mas ainda não entrega todo o valor enterprise nas partes avançadas.
- Integrations: infraestrutura local (entidades/repositórios) existe, porém a trilha end-to-end (ingestão real via `NexTraceOne.Ingestion.Api`) ainda precisa ser revalidada em profundidade.
- Produção readiness é heterogênea: alguns DbContexts estão prontos (com migrations) e outros ainda não.

---

## 6) Blockers reais atuais (observáveis no estado do repo)

1. **Test suite não verde**: `dotnet test` (Release) reporta 2 falhas em `NexTraceOne.Governance.Tests`:
   - `IntegrationHubFeatureTests.RetryConnector_ValidId_ShouldReturnQueued`
   - `IntegrationHubFeatureTests.ReprocessExecution_ValidId_ShouldReturnQueued`

   Causa imediata (evidência no TRX): mismatch de retorno ao configurar `IUnitOfWork.CommitAsync` via NSubstitute (`CouldNotSetReturnDueToTypeMismatchException`).

2. **DbContexts sem migrations** (schema não deployável / risco de drift):
   - `OperationalIntelligence.Runtime` (`RuntimeIntelligenceDbContext`) sem `Migrations/`
   - `OperationalIntelligence.Cost` (`CostIntelligenceDbContext`) sem `Migrations/`
   - `AIKnowledge.ExternalAI` (`ExternalAiDbContext`) sem `Migrations/`
   - `AIKnowledge.Orchestration` (`AiOrchestrationDbContext`) sem `Migrations/`

3. **Documentação de Fase 10 desatualizada em pontos críticos** (ex.: Governance “0 persistência”), o que pode induzir decisões erradas de priorização.

---

## 7) Nova prioridade recomendada (baseada no estado atual do código)

Prioridade é derivada de: (1) produção readiness real, (2) desbloqueio cross-module, (3) alinhamento com Source of Truth, (4) regressão/qualidade.

### P0 — Qualidade e integridade (imediato)
1. Voltar a deixar `dotnet test` **100% verde** (corrigir os 2 testes falhando em Governance).

### P1 — Produção readiness por módulo (alto impacto, baixo acoplamento)
2. Gerar migrations para `OperationalIntelligence.Runtime` e `OperationalIntelligence.Cost` (fecha dívida A1 do diagnóstico histórico, ainda válida).
3. Gerar migrations mínimas para `AIKnowledge.ExternalAI` e `AIKnowledge.Orchestration` (ou formalizar que esses submódulos são “design-only” e retirar do deploy path).

### P2 — Consolidar Governance como “real” e reduzir placeholders
4. Completar enriquecimento cross-module em Governance (contagens reais e maturidade com queries cross-module ou projections/eventual consistency).
5. Revalidar trilhas Integrations e Product Analytics end-to-end (event tracking consistente no frontend + ingestão real onde aplicável).

### P3 — Hygiene / consistência
6. Remover/arquivar artefatos legacy não usados (ex.: `mockEnrichment.ts` se comprovadamente órfão) — **apenas após validação de imports**.

---

## 8) Observações de revalidação das etapas “já confirmadas”

- **1A — Contracts MVP (remover mock enrichment):** confirmado via `ListContracts` (backend) + `ContractCatalogPage` (frontend).
- **2A — AI Hub MVP (conversations reais):** confirmado via `AiAssistantPage` consumindo `aiGovernanceApi.listConversations()` e `listMessages()`.
- **3A — Governance persistência:** confirmado via `GovernanceDbContext`, `DependencyInjection`, EF configurations e migrations.
- **3B — Governance handlers core:** parcialmente confirmado (handlers usam repositories; ainda existem TODOs de enriquecimento).

---

## 9) Evidências objetivas usadas nesta fase

- Build: `run_build` → **sucesso**.
- Tests: `dotnet test NexTraceOne.sln -c Release` → **1397 total; 2 failed**.
- Files de evidência principais:
  - `src/modules/governance/NexTraceOne.Governance.Infrastructure/Persistence/GovernanceDbContext.cs`
  - `src/modules/governance/NexTraceOne.Governance.Infrastructure/Persistence/Migrations/*`
  - `src/modules/governance/NexTraceOne.Governance.Application/Features/RecordAnalyticsEvent/RecordAnalyticsEvent.cs`
  - `src/modules/catalog/NexTraceOne.Catalog.Application/Contracts/Features/ListContracts/ListContracts.cs`
  - `src/frontend/src/features/contracts/catalog/ContractCatalogPage.tsx`
  - `src/frontend/src/features/ai-hub/pages/AiAssistantPage.tsx`
  - TRX: `TestResults/dlima_DESKTOP-TOLLROB_2026-03-18_15_15_36[1].trx`

---

## 10) Nota final

Este documento substitui a leitura “estática” do diagnóstico histórico como base decisória. A partir de 2026-03-18, a baseline técnica real deve assumir:

- Governance e Product Analytics **já têm persistência** (não são “design-only”).
- O principal risco imediato é **qualidade (tests verdes)** + **migrations faltantes** em subáreas.
