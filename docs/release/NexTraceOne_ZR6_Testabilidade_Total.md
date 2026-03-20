# NexTraceOne — ZR-6 Testabilidade total

**Data da execução:** 2026-03-20  
**Escopo executado:** `Fase ZR-6 — Testabilidade total`

## 1. Resumo executivo

A execução da `ZR-6` foi conduzida com foco exclusivo em evidência real e auditável.

O fechamento realizado nesta fase fortaleceu a testabilidade real dos fluxos finais de:

- `Identity & Access`
- `Catalog / Source of Truth`
- `Contracts`
- `Change Governance`
- `Incidents`
- `Audit`
- `AI Assistant core`

Principais resultados:

- o inventário das suites foi consolidado e reclassificado com critério rígido
- foram eliminadas suites `PlaceholderTests` que contaminavam a perceção de cobertura
- `CoreApiHostIntegrationTests` passou a cobrir também `current user`, `tenant users`, `source-of-truth search/detail` e `audit search/verify`
- `RealBusinessApiFlowTests` passou a cobrir `audit` com backend real
- foi criada uma suite curta de `smoke tests` reais para gate de release candidate
- a UI de `Audit` foi alinhada com o contrato backend real e o `release scope` voltou a expor `Incidents` e `AI Assistant core`, coerente com ZR-3/ZR-4
- o browser E2E real recebeu cobertura explícita de `Audit` e endurecimento das chamadas autenticadas de `Contracts`

## 2. Inventário completo das suites

### 2.1 Backend / plataforma

| Suite / arquivo | Tipo | Classificação ZR-6 | Evidência real | Gap / observação | Ação ZR-6 |
|---|---|---|---|---|---|
| `tests/platform/NexTraceOne.IntegrationTests/CriticalFlows/CoreApiHostIntegrationTests.cs` | integração host + PostgreSQL real | REAL E ÚTIL | Sim | faltavam `me/users`, `source-of-truth search/detail` e `audit` | ampliado |
| `tests/platform/NexTraceOne.IntegrationTests/CriticalFlows/CriticalFlowsPostgreSqlTests.cs` | integração DbContext | REAL MAS INSUFICIENTE | Sim | prova persistência, mas não fecha fluxo HTTP | mantido |
| `tests/platform/NexTraceOne.IntegrationTests/CriticalFlows/DeepCoveragePostgreSqlTests.cs` | integração DbContext | REAL MAS INSUFICIENTE | Sim | útil para mapeamento/EF, não para release flow | mantido |
| `tests/platform/NexTraceOne.IntegrationTests/CriticalFlows/ExtendedDbContextsPostgreSqlTests.cs` | integração DbContext | REAL MAS INSUFICIENTE | Sim | mistura áreas fora do release final e cobertura infra | mantido |
| `tests/platform/NexTraceOne.IntegrationTests/CriticalFlows/GovernanceWorkflowPostgreSqlTests.cs` | integração DbContext | DESATUALIZADO PARA RELEASE | Parcial | cobre áreas removidas do produto final | não contabilizado para release |
| `tests/platform/NexTraceOne.IntegrationTests/CriticalFlows/AiGovernancePostgreSqlTests.cs` | integração DbContext + Testcontainers | REAL MAS QUEBRADO NO AMBIENTE ATUAL | Potencialmente sim | reexecução bloqueada por indisponibilidade de Docker no ambiente atual | gap explícito |
| `tests/platform/NexTraceOne.E2E.Tests/Flows/RealBusinessApiFlowTests.cs` | E2E HTTP real | REAL E ÚTIL | Sim | faltava `audit` | ampliado |
| `tests/platform/NexTraceOne.E2E.Tests/Flows/ReleaseCandidateSmokeFlowTests.cs` | smoke real | REAL E ÚTIL | Sim | inexistente antes | criado |
| `tests/platform/NexTraceOne.E2E.Tests/Flows/SystemHealthFlowTests.cs` | smoke/infra real | REAL E ÚTIL | Sim | checks rápidos úteis, mas não suficientes sozinhos | mantido |
| `tests/platform/NexTraceOne.E2E.Tests/Flows/AuthApiFlowTests.cs` | E2E HTTP real | REAL MAS INSUFICIENTE | Sim | alguns testes toleram `return` precoce em caso de seed ausente | mantido com reclassificação |
| `tests/platform/NexTraceOne.E2E.Tests/Flows/CatalogAndIncidentApiFlowTests.cs` | E2E HTTP real | REAL MAS INSUFICIENTE | Sim | vários asserts genéricos `200 ou 403`, pouco valor de negócio | mantido com reclassificação |

### 2.2 Frontend

| Suite / arquivo | Tipo | Classificação ZR-6 | Evidência real | Gap / observação | Ação ZR-6 |
|---|---|---|---|---|---|
| `src/frontend/e2e-real/real-core-flows.spec.ts` | browser E2E real | REAL E ÚTIL | Sim, por desenho | não foi reexecutado neste ambiente por indisponibilidade de Docker | ampliado + gap explícito |
| `src/frontend/src/__tests__/pages/AuditPage.test.tsx` | unit/component | MOCKADO DEMAIS, MAS ÚTIL | Não | prova wiring de UI, não backend real | alinhado ao contrato real |
| `src/frontend/src/__tests__/pages/IncidentsPage.test.tsx` | unit/component | MOCKADO DEMAIS, MAS ÚTIL | Não | cobre UX/paginação/permissão, não round-trip | mantido |
| `src/frontend/src/__tests__/pages/IncidentDetailPage.test.tsx` | unit/component | MOCKADO DEMAIS, MAS ÚTIL | Não | cobre estados honestos, não persistência real | mantido |
| `src/frontend/src/__tests__/pages/AiAssistantPage.test.tsx` | unit/component | MOCKADO DEMAIS, MAS ÚTIL | Não | cobre reopen/reload/wiring, não provider real | mantido |
| `src/frontend/src/__tests__/releaseScope.test.ts` | unit | ÚTIL, NÃO CONTA COMO EVIDÊNCIA REAL | Não | protege coerência do escopo final | ajustado |

## 3. Matriz de cobertura por fluxo

| Módulo | Fluxo | Integração host real | E2E HTTP real | Browser E2E real | Smoke real | Estado ZR-6 |
|---|---|---|---|---|---|---|
| Identity | login / tenant / cookie session | Sim | Sim | Sim (`auth web real`) | Sim | COBERTO |
| Identity | `/me` + listagem mínima de utilizadores | Sim | Sim | indireto via shell autenticada | Sim | COBERTO |
| Catalog | list services / detail | Sim | Sim | Sim | Sim | COBERTO |
| Source of Truth | search / service detail / contract detail | Sim | Parcial | Sim | Sim | COBERTO MÍNIMO |
| Contracts | create / edit / submit / approve / publish / reopen | Sim | Sim | Sim | summary smoke | COBERTO |
| Change Governance | list / detail / start review | Sim | Sim | Sim | Sim | COBERTO |
| Incidents | list / create / detail / reopen / permission | Sim | Sim | Sim | Sim | COBERTO |
| Audit | search / verify chain | Sim | Sim | Sim, por código | Sim | COBERTO COM BLOQUEIO DE REEXECUÇÃO BROWSER |
| AI Assistant | create / open / send / persist / reopen / access control | Sim | Sim | Sim | create conversation smoke | COBERTO COM BLOQUEIO DE REEXECUÇÃO BROWSER |

## 4. Cobertura real por módulo

### Identity & Access
- `login`, `tenant selection`, `cookie-session`, `auth/me`, `tenant users`, `403` de mutação administrativa
- evidência: `CoreApiHostIntegrationTests`, `AuthApiFlowTests`, `ReleaseCandidateSmokeFlowTests`

### Catalog / Source of Truth
- `list services`, `service detail`, `source-of-truth search`, `service source-of-truth`, `contract source-of-truth`, `global-search`
- evidência: `CoreApiHostIntegrationTests`, `RealBusinessApiFlowTests`, `real-core-flows.spec.ts`, `ReleaseCandidateSmokeFlowTests`

### Contracts
- `create draft`, `get draft`, `update content`, `update metadata`, `submit`, `approve`, `publish`, `detail`, `by-service`
- evidência: `CoreApiHostIntegrationTests`, `RealBusinessApiFlowTests`, `real-core-flows.spec.ts`

### Change Governance
- `list releases`, `intelligence/detail`, `start review`
- evidência: `CoreApiHostIntegrationTests`, `RealBusinessApiFlowTests`, `real-core-flows.spec.ts`, smoke

### Incidents
- `list`, `detail`, `create`, `reopen`, `permission/read-only`
- evidência: `CoreApiHostIntegrationTests`, `RealBusinessApiFlowTests`, `real-core-flows.spec.ts`, smoke

### Audit
- `search`, `filtered search`, `verify chain`
- evidência: `CoreApiHostIntegrationTests`, `RealBusinessApiFlowTests`, `ReleaseCandidateSmokeFlowTests`, `real-core-flows.spec.ts`

### AI Assistant core
- `create conversation`, `open`, `list messages`, `send`, `persist`, `reopen`, `ownership`
- evidência: `CoreApiHostIntegrationTests`, `RealBusinessApiFlowTests`, `ReleaseCandidateSmokeFlowTests`, `real-core-flows.spec.ts`

## 5. Integration tests criados/ajustados

### Ajustados
- `tests/platform/NexTraceOne.IntegrationTests/CriticalFlows/CoreApiHostIntegrationTests.cs`
  - novo fluxo real `IdentityAccess_Should_Get_Current_User_And_List_Tenant_Users_With_Real_Backend`
  - expansão do fluxo `Catalog_And_SourceOfTruth...` com `search`, `service source-of-truth`, `contract source-of-truth` e `global-search`
  - novo fluxo real `Audit_Should_Record_Search_And_Verify_Real_Audit_Chain`

## 6. E2E criados/ajustados

### E2E HTTP reais
- `tests/platform/NexTraceOne.E2E.Tests/Flows/RealBusinessApiFlowTests.cs`
  - novo fluxo real `Audit_Should_Record_Search_And_Verify_Real_Audit_Log`

### Browser E2E reais
- `src/frontend/e2e-real/real-core-flows.spec.ts`
  - `Contracts` endurecido com publish autenticado real via API request
  - novo fluxo `audit real: list, search and verify integrity with real backend data`

## 7. Smoke tests criados/ajustados

### Criado
- `tests/platform/NexTraceOne.E2E.Tests/Flows/ReleaseCandidateSmokeFlowTests.cs`

### Fluxos smoke cobertos
- `health`
- `login + current user`
- `catalog + source-of-truth`
- `contracts summary`
- `change governance + incidents`
- `audit search + verify chain`
- `AI assistant create conversation`

## 8. Massa de teste definida/ajustada

A ZR-6 passou a depender explicitamente da massa seedada real já estabilizada pelo host:

- tenants: `NexTrace Corp`, `Acme Fintech`, `E2E Test Org`
- utilizadores: `admin@nextraceone.dev`, `developer@nextraceone.dev`, `auditor@nextraceone.dev`, `e2e.admin@nextraceone.test`, `e2e.viewer@nextraceone.test`
- serviços seedados: `Payments Service`, `Orders Service`
- change/release seedado: `Orders Service 1.3.0`
- incidente seedado: `INC-2026-0042`
- audit seedado: eventos reais do `DevelopmentSeedService`
- AI governance seedado: conversas/massa mínima de assistant

A massa usada em smoke e E2E foi simplificada para:

- reusar seed estável quando possível
- criar entidades transitórias apenas para drafts/incidents/conversations
- evitar pré-requisitos ocultos

## 9. Suites placeholder/mockadas removidas ou reclassificadas

### Removidas
- `tests/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure.Tests/PlaceholderTests.cs`
- `tests/building-blocks/NexTraceOne.BuildingBlocks.Security.Tests/PlaceholderTests.cs`
- `tests/modules/auditcompliance/NexTraceOne.AuditCompliance.Tests/PlaceholderTests.cs`
- `tests/platform/NexTraceOne.E2E.Tests/PlaceholderTests.cs`

### Reclassificadas como não contabilizáveis para evidência real
- testes frontend unitários/component com mocks intensivos
- suites HTTP genéricas com asserts permissivos `200 ou 403`
- suites de DbContext que não fecham fluxo funcional do produto final

## 10. Nível de confiança por módulo

| Módulo | Nível | Base objetiva |
|---|---|---|
| Identity & Access | ALTA CONFIANÇA | host integration + E2E HTTP + smoke + browser real preparado |
| Catalog / Source of Truth | ALTA CONFIANÇA | host integration + E2E HTTP + smoke + browser real preparado |
| Contracts | ALTA CONFIANÇA | host integration + E2E HTTP + browser real preparado |
| Change Governance | MÉDIA/ALTA CONFIANÇA | host integration + E2E HTTP + smoke + browser real preparado |
| Incidents | ALTA CONFIANÇA | host integration + E2E HTTP + browser real preparado + smoke |
| Audit | MÉDIA CONFIANÇA | host integration + E2E HTTP + smoke; browser real não reexecutado neste ambiente |
| AI Assistant core | MÉDIA/ALTA CONFIANÇA | host integration + E2E HTTP + smoke; browser real não reexecutado neste ambiente |

## 11. Ficheiros alterados

### Backend / testes
- `tests/platform/NexTraceOne.IntegrationTests/CriticalFlows/CoreApiHostIntegrationTests.cs`
- `tests/platform/NexTraceOne.E2E.Tests/Flows/RealBusinessApiFlowTests.cs`
- `tests/platform/NexTraceOne.E2E.Tests/Flows/ReleaseCandidateSmokeFlowTests.cs`

### Frontend
- `src/frontend/src/App.tsx`
- `src/frontend/src/releaseScope.ts`
- `src/frontend/src/__tests__/releaseScope.test.ts`
- `src/frontend/src/features/audit-compliance/api/audit.ts`
- `src/frontend/src/features/audit-compliance/pages/AuditPage.tsx`
- `src/frontend/src/__tests__/pages/AuditPage.test.tsx`
- `src/frontend/e2e-real/helpers/auth.ts`
- `src/frontend/e2e-real/real-core-flows.spec.ts`

### Removidos
- `tests/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure.Tests/PlaceholderTests.cs`
- `tests/building-blocks/NexTraceOne.BuildingBlocks.Security.Tests/PlaceholderTests.cs`
- `tests/modules/auditcompliance/NexTraceOne.AuditCompliance.Tests/PlaceholderTests.cs`
- `tests/platform/NexTraceOne.E2E.Tests/PlaceholderTests.cs`

## 12. Gaps remanescentes

1. **Reexecução browser E2E real bloqueada no ambiente atual**
   - tentativa de execução de `Playwright real` falhou por indisponibilidade de Docker:
   - `dockerDesktopLinuxEngine` não disponível no ambiente da execução
   - impacto: os fluxos browser foram preparados/ajustados, mas não puderam ser revalidados aqui

2. **Suites Testcontainers DbContext-level não puderam ser reexecutadas aqui**
   - `AiGovernancePostgreSqlTests` depende de Docker
   - impacto: a suite continua `real`, mas permanece `ambiente-bloqueada` nesta execução

3. **Há suites reais mas fracas que não devem ser usadas como principal métrica de release**
   - `AuthApiFlowTests`
   - `CatalogAndIncidentApiFlowTests`
   - motivo: asserts genéricos e tolerância excessiva a estados não-ideais

## 13. Veredicto final da testabilidade

**PARCIAL**

### Motivo do veredicto

A cobertura real do produto final subiu materialmente e ficou mais honesta:

- há integration tests reais fortes para os fluxos core
- há E2E HTTP reais para os fluxos principais
- há smoke tests reais de gate
- placeholders foram removidos
- `Audit` deixou de ficar desalinhado entre frontend e backend

No entanto, não é intelectualmente honesto classificar a testabilidade como `TOTAL` nesta execução porque:

- os browser E2E reais não puderam ser reexecutados no ambiente atual por bloqueio externo de Docker
- parte das suites Testcontainers de integração também ficou ambiente-bloqueada

### Conclusão prática

Do ponto de vista de código e estrutura, o NexTraceOne saiu **muito mais testável** e com evidência real significativamente melhor.

Do ponto de vista de prova executada nesta sessão, o estado correto é:

- **forte avanço para zero-ressalvas**
- **confiança alta nos fluxos core de backend e E2E HTTP**
- **pendência objetiva apenas na reexecução browser real / Testcontainers dependentes de Docker**
