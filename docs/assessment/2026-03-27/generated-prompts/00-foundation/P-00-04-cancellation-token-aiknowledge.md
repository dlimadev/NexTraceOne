# P-00-04 — Adicionar CancellationToken ao módulo AIKnowledge

## Modo de operação

Refactor

## Objetivo

Adicionar `CancellationToken` a todos os métodos assíncronos do módulo AIKnowledge (~43 métodos).
Este módulo é particularmente crítico porque gere chamadas a providers de IA (Ollama, OpenAI),
serviços de retrieval (telemetria, base de dados, documentos) e orquestração de agentes — operações
potencialmente lentas e dispendiosas que beneficiam fortemente do cancelamento cooperativo.

## Problema atual

O módulo AIKnowledge contém 43 métodos `async Task` sem `CancellationToken`. Os ficheiros
afetados incluem providers de IA (OllamaProvider, OpenAiProvider e respetivos HttpClients),
builders de contexto (AIContextBuilder, PromotionRiskContextBuilder), serviços de runtime
(AiModelCatalogService, AiTokenQuotaService, AiProviderHealthService, AiAgentRuntimeService),
serviços de retrieval (TelemetryRetrievalService, DatabaseRetrievalService, DocumentRetrievalService),
adaptadores de routing (ExternalAiRoutingPortAdapter), executores de ferramentas (AgentToolExecutor),
repositórios de persistência (ExternalAiRepositories, AiOrchestrationRepositories, AiRuntimeRepositories,
AiGovernanceRepositories) e o handler core SendAssistantMessage.

## Escopo permitido

- `src/modules/aiknowledge/` — apenas este módulo
- Application/**/*.cs — handlers, serviços e builders
- Infrastructure/**/*.cs — providers, repositórios e adaptadores

## Escopo proibido

- Outros módulos
- Ficheiros de migração existentes
- Configuração de modelos de IA no appsettings
- Alteração de prompts ou templates de IA

## Ficheiros principais candidatos a alteração

- `Application/Orchestration/Services/AIContextBuilder.cs` (BuildForAsync)
- `Application/Orchestration/Services/PromotionRiskContextBuilder.cs` (BuildAsync, BuildComparisonAsync)
- `Application/Runtime/Features/SendAssistantMessage/SendAssistantMessage.cs` (AugmentWithRetrievalAsync)
- `Infrastructure/Providers/OllamaProvider.cs` (ListAvailableModelsAsync, CompleteAsync)
- `Infrastructure/Providers/OllamaHttpClient.cs` (ChatAsync, ListModelsAsync)
- `Infrastructure/Providers/OpenAiProvider.cs` (CompleteAsync)
- `Infrastructure/Providers/OpenAiHttpClient.cs` (ChatAsync)
- `Infrastructure/Services/AiModelCatalogService.cs` (ResolveDefaultModelAsync, ResolveModelByIdAsync)
- `Infrastructure/Services/TelemetryRetrievalService.cs` (SearchAsync)
- `Infrastructure/Services/DatabaseRetrievalService.cs` (SearchAsync)
- `Infrastructure/Services/DocumentRetrievalService.cs` (SearchAsync)
- `Infrastructure/Services/AiTokenQuotaService.cs` (ValidateQuotaAsync, RecordUsageAsync)
- `Infrastructure/Services/AiProviderHealthService.cs` (CheckAllProvidersAsync, CheckProviderAsync)
- `Infrastructure/Services/AiAgentRuntimeService.cs` (ExecuteAsync, GenerateArtifactsAsync)
- `Infrastructure/Adapters/ExternalAiRoutingPortAdapter.cs` (RouteQueryAsync)
- `Infrastructure/Adapters/AgentToolExecutor.cs` (ExecuteAsync)
- `Infrastructure/Persistence/ExternalAiRepositories.cs` (ListAsync, GetUsageMetricsAsync)
- `Infrastructure/Persistence/AiOrchestrationRepositories.cs` (GetRecentByServiceAsync, ListHistoryAsync, 2× GetRecentByReleaseAsync)
- `Infrastructure/Persistence/AiRuntimeRepositories.cs` (GetTotalTokensForPeriodAsync)
- `Infrastructure/Persistence/AiGovernanceRepositories.cs` (~16 métodos: ListAsync em múltiplos repositórios, ListByConversationAsync, ListByCategoriesAsync, ListByAgentAsync, ListByUserAsync, ListByExecutionAsync, GetByClientTypeAndPersonaAsync)

## Responsabilidades permitidas

- Adicionar `CancellationToken cancellationToken = default` a cada método async
- Propagar token para HttpClient.SendAsync (Ollama, OpenAI), EF Core e operações I/O
- Atualizar interfaces e port abstractions correspondentes

## Responsabilidades proibidas

- Alterar lógica de orquestração de IA ou prompts
- Alterar lógica de routing ou seleção de modelos
- Refatorar estrutura de agentes ou retrieval

## Critérios de aceite

1. Todos os 43 métodos async têm `CancellationToken`
2. Token propagado para chamadas HTTP a Ollama e OpenAI
3. Token propagado para todas as queries EF Core
4. Módulo compila sem erros
5. Solução completa compila

## Validações obrigatórias

- `dotnet build src/modules/aiknowledge/` — sem erros
- `dotnet build NexTraceOne.sln` — sem erros
- `grep -r "async Task" src/modules/aiknowledge/ | grep -v CancellationToken` retorna zero

## Riscos e cuidados

- Chamadas a Ollama e OpenAI são as que mais beneficiam de cancelamento — priorizar
- AiAgentRuntimeService.ExecuteAsync pode orquestrar múltiplas chamadas — propagar token em toda a cadeia
- AiTokenQuotaService gere quotas — verificar que cancelamento não deixa quotas inconsistentes
- AiGovernanceRepositories tem ~16 métodos — maior volume de alterações num único ficheiro

## Dependências

- Nenhuma dependência hard de outros prompts
- Pode ser executado em paralelo com P-00-01, P-00-02, P-00-03

## Próximos prompts sugeridos

- P-00-05 (CancellationToken no módulo OperationalIntelligence)
- P-00-06 (CancellationToken nos módulos restantes)
