# P-02-04 — Expandir DatabaseRetrievalService para pesquisa cross-module (serviços, contratos, mudanças, incidentes)

## 1. Título

Expandir o AI grounding DatabaseRetrievalService para pesquisar entidades cross-module do NexTraceOne.

## 2. Modo de operação

**Implementation**

## 3. Objetivo

O DatabaseRetrievalService é a peça central de grounding da IA do NexTraceOne — alimenta o assistente com contexto real da plataforma. Atualmente pesquisa apenas modelos de IA (tabela AIModels). Este prompt expande a pesquisa para incluir serviços (Catalog), contratos (Contracts), mudanças (ChangeIntelligence) e incidentes (OperationalIntelligence), transformando o assistente num verdadeiro copiloto operacional.

## 4. Problema atual

- O `DatabaseRetrievalService` em `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Services/DatabaseRetrievalService.cs` usa apenas `IAiModelRepository` como fonte de dados.
- O comentário no código confirma: "Proof of concept: pesquisa modelos de IA por keyword usando repositório governado. Futuramente será expandido para contratos, serviços, incidentes, etc."
- A interface `IDatabaseRetrievalService` em `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Runtime/Abstractions/IDatabaseRetrievalService.cs` define `DatabaseSearchRequest` com campo `EntityType` que não é usado na implementação atual.
- O handler `SearchData.cs` em `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Runtime/Features/SearchData/SearchData.cs` e o `SendAssistantMessage.cs` em Governance/Features invocam este serviço mas só recebem resultados de AIModels.
- As tools existentes (`GetServiceHealthTool.cs`, `ListRecentChangesTool.cs`, `ListServicesInfoTool.cs`) demonstram que a infra para cross-module já existe parcialmente.

## 5. Escopo permitido

- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Services/DatabaseRetrievalService.cs` — expansão principal
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Runtime/Abstractions/` — criar interfaces de cross-module query
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/DependencyInjection.cs` — registar novos serviços
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Contracts/` — definir interfaces de consulta cross-module (se seguir Contracts pattern)
- Interfaces em módulos de origem (Catalog, Contracts, ChangeIntelligence, OperationalIntelligence) — apenas interfaces de leitura

## 6. Escopo proibido

- Não alterar a lógica de chat, streaming ou providers de IA.
- Não aceder diretamente a DbContexts de outros módulos — usar contratos/interfaces publicadas.
- Não criar dependências circulares entre módulos.
- Não alterar o comportamento das tools existentes (GetServiceHealthTool, ListRecentChangesTool, ListServicesInfoTool).
- Não alterar migrações de nenhum módulo.

## 7. Ficheiros principais candidatos a alteração

1. `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Services/DatabaseRetrievalService.cs`
2. `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Runtime/Abstractions/IDatabaseRetrievalService.cs`
3. `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Runtime/Abstractions/ICrossModuleSearchPort.cs` (novo)
4. `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/DependencyInjection.cs`
5. `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Contracts/Orchestration/ServiceInterfaces/IAiOrchestrationModule.cs` (expandir se necessário)

## 8. Responsabilidades permitidas

- Criar interface ICrossModuleSearchPort com métodos: SearchServicesAsync, SearchContractsAsync, SearchChangesAsync, SearchIncidentsAsync.
- Implementar adapter que use interfaces publicadas pelos módulos Catalog, Contracts, ChangeIntelligence, OperationalIntelligence.
- Expandir DatabaseRetrievalService para despachar pesquisa por EntityType (Service, Contract, Change, Incident, AIModel).
- Combinar e rankear resultados de múltiplas fontes quando EntityType não é especificado.
- Preservar o padrão existente de DatabaseSearchHit como modelo canónico de resultado.
- Usar CancellationToken em todas as operações.
- Adicionar logging com métricas de performance por fonte de dados.

## 9. Responsabilidades proibidas

- Não criar cache de resultados cross-module neste prompt.
- Não implementar RAG (Retrieval Augmented Generation) completo — apenas expandir a pesquisa de grounding.
- Não expor dados sensíveis (credenciais, tokens) nos resultados de pesquisa.
- Não filtrar resultados sem respeitar tenant e ambiente do contexto.

## 10. Critérios de aceite

- [ ] DatabaseRetrievalService pesquisa em pelo menos 4 fontes: AIModels, Services, Contracts, Changes.
- [ ] Pesquisa por EntityType específico retorna resultados apenas daquela fonte.
- [ ] Pesquisa sem EntityType combina resultados de todas as fontes com ranking.
- [ ] Cada DatabaseSearchHit tem EntityType, EntityId, DisplayName e Summary preenchidos.
- [ ] CancellationToken propagado em todas as operações.
- [ ] Compilação completa da solution sem erros.
- [ ] Sem dependências circulares entre módulos.

## 11. Validações obrigatórias

- Compilação de todos os projetos do módulo aiknowledge.
- Compilação da solution NexTraceOne.sln completa.
- Verificar que SearchData handler e SendAssistantMessage continuam a funcionar.
- Verificar que não foram introduzidas referências circulares no grafo de dependências.
- Verificar que as tools existentes (GetServiceHealthTool, ListRecentChangesTool) não foram quebradas.

## 12. Riscos e cuidados

- Cross-module queries podem introduzir acoplamento forte — usar ports/adapters e interfaces em Contracts.
- A pesquisa em múltiplas fontes pode ser lenta — considerar execução paralela com Task.WhenAll.
- Se um módulo estiver indisponível, o serviço deve retornar resultados parciais (não falhar).
- O EntityType "Service" vs "Catalog" precisa de normalização clara.
- Resultados devem respeitar filtros de tenant e ambiente — não retornar dados de outros tenants.

## 13. Dependências

- **P-01-04** — O módulo aiknowledge deve ter a estrutura base estável.
- Interfaces de leitura dos módulos Catalog, Contracts, ChangeIntelligence e OperationalIntelligence devem existir em seus projetos Contracts.
- A entidade IntegrationConnector e handlers de busca já publicados nos respetivos módulos.

## 14. Próximos prompts sugeridos

- **P-02-01** — Knowledge CRUD com FTS (depois de pronto, o Knowledge será mais uma fonte para o AI grounding).
- **P-XX-XX** — RAG completo com embeddings e vector search.
- **P-XX-XX** — AI Agent que usa cross-module search para investigação operacional automatizada.
