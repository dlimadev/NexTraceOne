# Phase 7 — Transition Notes

## O que foi implementado na Phase 7

### Backend (NexTraceOne.AIKnowledge)

| Componente | Ficheiro |
|---|---|
| Feature `AnalyzeNonProdEnvironment` | `Application/Orchestration/Features/AnalyzeNonProdEnvironment/AnalyzeNonProdEnvironment.cs` |
| Feature `CompareEnvironments` | `Application/Orchestration/Features/CompareEnvironments/CompareEnvironments.cs` |
| Feature `AssessPromotionReadiness` | `Application/Orchestration/Features/AssessPromotionReadiness/AssessPromotionReadiness.cs` |
| DI registrations | `Application/Orchestration/DependencyInjection.cs` (+3 validators) |
| Endpoints | `API/Orchestration/Endpoints/Endpoints/AiOrchestrationEndpointModule.cs` (+`MapEnvironmentAnalysisEndpoints`) |

### Backend Tests

| Ficheiro | Testes |
|---|---|
| `AnalyzeNonProdEnvironmentTests.cs` | 7 testes |
| `CompareEnvironmentsTests.cs` | 4 testes |
| `AssessPromotionReadinessTests.cs` | 4 testes |

Total de testes no módulo após Phase 7: **220 testes** (todos passando).

### Frontend

| Componente | Ficheiro |
|---|---|
| API methods | `features/ai-hub/api/aiGovernance.ts` (+3 métodos) |
| Página principal | `features/ai-hub/pages/AiAnalysisPage.tsx` |
| Rota | `App.tsx` (`/ai/analysis`) |
| Sidebar | `components/shell/AppSidebar.tsx` (+1 item) |
| Locales | `locales/en.json`, `pt-BR.json`, `pt-PT.json`, `es.json` (+`aiAnalysis` section) |

### Frontend Tests

`src/__tests__/pages/AiAnalysisPage.test.tsx` — 6 testes (todos passando)

---

## Estado dos testes antes da Phase 7

Falhas pré-existentes (não alteradas):
- `releaseScope.test.ts`: 15 falhas
- `tokenStorage.test.ts`: 2 falhas
- `AssistantPanel.test.tsx`: 3 falhas
- `PromotionPage.test.tsx`: 1 falha

**Total pré-existente**: 21 falhas em 4 ficheiros de teste.

A Phase 7 não altera estes testes.

---

## Gaps conhecidos e trabalho futuro

### Backend

1. **Dados reais de telemetria**: Actualmente o grounding context não inclui dados
   reais de telemetria, contratos ou incidentes. O handler passa apenas metadados
   descritivos. Uma versão futura deve enriquecer o grounding com dados do
   `ServiceCatalog`, `ContractGovernance` e `Operations`.

2. **Auditoria granular**: As chamadas de análise não são registadas como eventos
   de auditoria específicos. A Phase 8 deve adicionar `IAuditPort.RecordAsync`
   para cada invocação de análise.

3. **Caching de resultados**: Análises repetidas para o mesmo ambiente/janela
   poderiam ser cached por `correlationId` para reduzir custos de tokens.

### Frontend

1. **EnvironmentContext com dados reais**: O `EnvironmentContext` usa dados mock
   (`loadEnvironmentsForTenant`). A Phase 7 não modifica este comportamento —
   a integração com a API real de ambientes está anotada como TODO no ficheiro.

2. **Observation window configurável**: Actualmente fixado a 7 dias na UI.
   Uma versão futura deve expor um selector de janela temporal.

3. **Service filter**: O campo `serviceFilter` está disponível na API mas
   não exposto na UI nesta fase.

---

## Pontos de integração com fases anteriores

| Fase | Integração |
|---|---|
| Phase 2 | `EnvironmentResolutionMiddleware` e `X-Environment-Id` header (usados indiretamente) |
| Phase 5 | `IExternalAIRoutingPort`, model registry, AI policies (reutilizados directamente) |
| Phase 6 | `AiOrchestrationEndpointModule` estendido com `MapEnvironmentAnalysisEndpoints` |

---

## Notas de segurança

- `TenantId` obrigatório em todos os commands (isolamento garantido)
- `ai:runtime:write` requerido para todos os endpoints
- Grounding não inclui dados sensíveis (apenas metadados)
- CorrelationId disponível para rastreabilidade em logs
