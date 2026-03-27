# P-00-02 — Adicionar CancellationToken ao módulo Catalog

## Modo de operação

Refactor

## Objetivo

Adicionar `CancellationToken` a todos os métodos assíncronos do módulo Catalog (~26 métodos).
Este módulo é responsável pelo catálogo de serviços, contratos, assets e topologia — operações
que podem envolver queries complexas a PostgreSQL e chamadas a serviços de IA para geração de drafts.

## Problema atual

O módulo Catalog contém 26 métodos `async Task` sem `CancellationToken`. Os ficheiros afetados
incluem repositórios de persistência (NodeHealthRepository, ServiceAssetRepository, LinkedReferenceRepository,
ApiAssetRepository, ContractVersionRepository, ContractDraftRepository, ContractPublicationEntryRepository,
e vários repositórios de metadata SOAP/Event/BackgroundService), serviços de domínio (AiDraftGeneratorService,
DeveloperPortalModuleService) e o handler SyncContracts. Sem cancelamento cooperativo, queries
pesadas ao catálogo e chamadas ao provider de IA continuam mesmo quando o cliente HTTP desconecta.

## Escopo permitido

- `src/modules/catalog/` — apenas este módulo
- Application/Features/**/*.cs — handlers MediatR
- Infrastructure/Persistence/**/*.cs — todos os repositórios
- Infrastructure/Services/**/*.cs — serviços de infraestrutura
- Interfaces e abstrações correspondentes

## Escopo proibido

- Outros módulos
- Ficheiros de migração existentes
- Configuração do host
- Testes de outros módulos

## Ficheiros principais candidatos a alteração

- `Application/Features/SyncContracts/SyncContracts.cs` (ProcessItemAsync)
- `Infrastructure/Persistence/NodeHealthRepository.cs` (GetLatestByOverlayAsync, GetByNodeAsync)
- `Infrastructure/Persistence/ServiceAssetRepository.cs` (ListFilteredAsync)
- `Infrastructure/Persistence/LinkedReferenceRepository.cs` (ListByAssetAsync, ListByAssetAndTypeAsync, SearchAsync)
- `Infrastructure/Persistence/ApiAssetRepository.cs` (ListByApiAssetIdsAsync)
- `Infrastructure/Persistence/SoapDraftMetadataRepository.cs` (GetByDraftIdAsync)
- `Infrastructure/Persistence/SoapContractDetailRepository.cs` (GetByContractVersionIdAsync)
- `Infrastructure/Persistence/BackgroundServiceDraftMetadataRepository.cs` (GetByDraftIdAsync)
- `Infrastructure/Persistence/ContractVersionRepository.cs` (SearchAsync, ListLatestPerApiAssetAsync, ListByApiAssetIdsAsync)
- `Infrastructure/Persistence/EventContractDetailRepository.cs` (GetByContractVersionIdAsync)
- `Infrastructure/Persistence/ContractDraftRepository.cs` (ListAsync, CountAsync)
- `Infrastructure/Persistence/EventDraftMetadataRepository.cs` (GetByDraftIdAsync)
- `Infrastructure/Persistence/BackgroundServiceContractDetailRepository.cs` (GetByContractVersionIdAsync)
- `Infrastructure/Persistence/ContractPublicationEntryRepository.cs` (GetByIdAsync, GetByContractVersionIdAsync, ListAsync)
- `Infrastructure/Services/AiDraftGeneratorService.cs` (GenerateAsync)
- `Infrastructure/Services/DeveloperPortalModuleService.cs` (HasActiveSubscriptionsAsync, GetActiveSubscriptionCountAsync, GetSubscriberIdsAsync)

## Responsabilidades permitidas

- Adicionar `CancellationToken cancellationToken = default` a cada método async
- Propagar token para EF Core queries, HttpClient e chamadas a providers de IA
- Atualizar interfaces e abstrações correspondentes

## Responsabilidades proibidas

- Alterar lógica de negócio ou queries existentes
- Refatorar estrutura ou nomes
- Adicionar funcionalidades novas

## Critérios de aceite

1. Todos os 26 métodos async do módulo têm `CancellationToken`
2. Token propagado para todas as operações EF Core e chamadas HTTP
3. Módulo compila sem erros
4. Testes do módulo compilam e passam
5. Solução completa compila (`dotnet build NexTraceOne.sln`)

## Validações obrigatórias

- `dotnet build src/modules/catalog/` — sem erros
- `dotnet build NexTraceOne.sln` — sem erros
- Pesquisa `grep -r "async Task" src/modules/catalog/ | grep -v CancellationToken` retorna zero

## Riscos e cuidados

- `AiDraftGeneratorService.GenerateAsync` chama providers externos de IA — o token deve ser propagado para evitar chamadas penduradas
- `ContractVersionRepository.SearchAsync` pode envolver queries complexas com múltiplos joins — beneficia de cancelamento
- Interfaces partilhadas via módulos de surface (`ICatalogGraphModule`) podem precisar de atualização coordenada

## Dependências

- Nenhuma dependência hard de outros prompts
- Pode ser executado em paralelo com P-00-01

## Próximos prompts sugeridos

- P-00-03 (CancellationToken no módulo Notifications)
- P-00-06 (CancellationToken nos módulos restantes)
