# AI Knowledge — Gaps, Erros e Pendências

## 1. Estado resumido do módulo
287 .cs files (maior módulo), múltiplos DbContexts, LLM integration real via `IChatCompletionProvider`. Gaps concentrados em 3 stubs de Runtime e documentação dramaticamente desactualizada.

## 2. Gaps críticos
Nenhum gap crítico.

## 3. Gaps altos
Nenhum gap alto.

## 4. Gaps médios

### 4.1 `ListKnowledgeSourceWeights` — Stub in-memory
- **Severidade:** MEDIUM
- **Classificação:** STUB
- **Descrição:** Pesos de knowledge sources são hardcoded/in-memory em vez de persistidos no banco. Não reflecte configuração por tenant ou administrador.
- **Impacto:** Customização de pesos de knowledge sources não é possível nem persistida.
- **Evidência:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Runtime/Features/ListKnowledgeSourceWeights/ListKnowledgeSourceWeights.cs`

### 4.2 `PlanExecution` — Model Selection Simplificado
- **Severidade:** MEDIUM
- **Classificação:** STUB
- **Descrição:** A selecção de modelo no planeamento de execução é simplificada, não usa o model registry completo com routing por contexto.
- **Impacto:** Selecção de modelo de IA não é óptima por contexto, tenant ou policy.
- **Evidência:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Runtime/Features/PlanExecution/PlanExecution.cs`

### 4.3 `AiSourceRegistryService` — Health Check Stub
- **Severidade:** LOW
- **Classificação:** STUB
- **Descrição:** Health check de AI sources retorna valor fixo em vez de verificar conectividade real.
- **Impacto:** Monitorização de saúde de providers de IA não reflecte estado real.
- **Evidência:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Services/AiSourceRegistryService.cs`

## 5. Itens mock / stub / placeholder
- `ListKnowledgeSourceWeights` — stub in-memory
- `PlanExecution` — model selection simplificado
- `AiSourceRegistryService.CheckHealthAsync()` — stub

**NOTA:** `InMemoryToolRegistry` NÃO é stub — é um padrão de design válido (singleton cache de tools registados via DI no arranque). Está registado em DI e é funcional.

## 6. Erros de desenho / implementação incorreta
Nenhum. O módulo segue correctamente Clean Architecture + VSA.

**CORRECÇÃO DE REGISTO:** A auditoria de Março 2026 afirmava:
- "SendAssistantMessage returns hardcoded responses — no real LLM invoked" — **FALSO**
- `SendAssistantMessage.cs` invoca `IChatCompletionProvider.CompleteAsync()` com LLM real, routing, governance, audit trail e fallback degradado
- `IExternalAiModule` foi marcado como "PLAN" — está **IMPLEMENTED** por `ExternalAiModule.cs`
- `IAiOrchestrationModule` foi marcado como "PLAN" — está **IMPLEMENTED** por `AiOrchestrationModule.cs`

## 7. Gaps de frontend ligados a este módulo
- `AiAssistantPage.tsx` — usa API real (`aiGovernanceApi.sendMessage`, `listConversations`, etc.) mas sem `isError` handling
- `AgentDetailPage.tsx` — sem error handling
- `AiAgentsPage.tsx` — sem error handling e sem empty state
- `AiAnalysisPage.tsx` — sem error handling
- 4 de 8 páginas de AI Hub sem error handling explícito

**CORRECÇÃO DE REGISTO:** Março 2026 afirmava `AiAssistantPage.tsx` usa `mockConversations` — **FALSO**. Usa `aiGovernanceApi.listConversations/sendMessage` (7 chamadas API reais).

## 8. Gaps de backend ligados a este módulo
- 3 stubs em Runtime (listados acima)

## 9. Gaps de banco/migração ligados a este módulo
Nenhum — DbContexts com migrations confirmadas (AiGovernanceDbContext, AiOrchestrationDbContext, ExternalAiDbContext).

## 10. Gaps de configuração ligados a este módulo
Nenhum.

## 11. Gaps de documentação ligados a este módulo
- `docs/IMPLEMENTATION-STATUS.md` §AIKnowledge afirma "sem LLM real E2E" — **FALSO**
- `docs/IMPLEMENTATION-STATUS.md` §CrossModule afirma `IAiOrchestrationModule = PLAN` e `IExternalAiModule = PLAN` — ambos **IMPLEMENTED**
- `docs/CORE-FLOW-GAPS.md` §Flow 4 completamente desactualizado — afirma respostas hardcoded, mock conversations, 8 ExternalAI stubs — tudo **FALSO**

## 12. Gaps de seed/bootstrap ligados a este módulo
- `seed-aiknowledge.sql` referenciado mas **NÃO EXISTE** no disco.

## 13. Ações corretivas obrigatórias
1. Substituir `ListKnowledgeSourceWeights` por leitura de configuração persistida
2. Melhorar `PlanExecution` model selection com integração do model registry
3. Implementar health check real em `AiSourceRegistryService`
4. Adicionar error handling a 4 páginas frontend de AI Hub
5. Actualizar `docs/IMPLEMENTATION-STATUS.md` e `docs/CORE-FLOW-GAPS.md`
6. Criar `seed-aiknowledge.sql` ou remover referência
