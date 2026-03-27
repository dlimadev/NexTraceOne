# P-00-05 — Adicionar CancellationToken ao módulo OperationalIntelligence

## Modo de operação

Refactor

## Objetivo

Adicionar `CancellationToken` a todos os métodos assíncronos do módulo OperationalIntelligence (~36 métodos).
Este módulo abrange Reliability, Cost Intelligence, Automation, Incidents e Runtime — domínios
onde queries a séries temporais, snapshots e correlações podem ser custosas em tempo e recursos.

## Problema atual

O módulo OperationalIntelligence contém 36 métodos `async Task` sem `CancellationToken`. Os
ficheiros afetados incluem repositórios de Runtime (RuntimeBaselineRepository, ObservabilityProfileRepository,
RuntimeSnapshotRepository, DriftFindingRepository), repositórios de Cost (CostAttributionRepository,
CostSnapshotRepository, CostTrendRepository, ServiceCostProfileRepository, CostImportBatchRepository),
serviços de custo (CostIntelligenceModuleService com 6 métodos), repositórios de Automation
(AutomationRepositories com 3 métodos), surfaces de Incidents (IncidentContextSurface com 3 métodos),
surfaces de Reliability (ReliabilityIncidentSurface com 4 métodos, ReliabilitySnapshotRepository
com 2 métodos, ReliabilityRuntimeSurface com 4 métodos).

## Escopo permitido

- `src/modules/operationalintelligence/` — apenas este módulo
- Application/**/*.cs — handlers e serviços
- Infrastructure/**/*.cs — repositórios e surfaces

## Escopo proibido

- Outros módulos
- Ficheiros de migração existentes
- Configuração do host
- Lógica de cálculo de SLO/SLA, burn rate ou error budget

## Ficheiros principais candidatos a alteração

- `Infrastructure/Persistence/Runtime/RuntimeBaselineRepository.cs` (GetByServiceAndEnvironmentAsync, ListByServiceAsync)
- `Infrastructure/Persistence/Runtime/ObservabilityProfileRepository.cs` (GetByServiceAndEnvironmentAsync)
- `Infrastructure/Persistence/Runtime/RuntimeSnapshotRepository.cs` (ListByServiceAsync, GetLatestByServiceAsync, GetServicesWithRecentSnapshotsAsync)
- `Infrastructure/Persistence/Runtime/DriftFindingRepository.cs` (ListByServiceAsync, ListUnacknowledgedAsync)
- `Infrastructure/Persistence/Cost/CostAttributionRepository.cs` (ListByServiceAsync, ListByPeriodAsync)
- `Infrastructure/Persistence/Cost/CostSnapshotRepository.cs` (ListByServiceAsync)
- `Infrastructure/Persistence/Cost/CostTrendRepository.cs` (ListByServiceAsync)
- `Infrastructure/Persistence/Cost/ServiceCostProfileRepository.cs` (GetByServiceAndEnvironmentAsync)
- `Infrastructure/Persistence/Cost/CostImportBatchRepository.cs` (ListAsync)
- `Infrastructure/Services/CostIntelligenceModuleService.cs` (GetCurrentMonthlyCostAsync, GetCostTrendPercentageAsync, GetCostRecordsAsync, GetServiceCostAsync, GetCostsByTeamAsync, GetCostsByDomainAsync)
- `Infrastructure/Persistence/Automation/AutomationRepositories.cs` (ListAsync, GetByWorkflowIdAsync ×2)
- `Infrastructure/Surfaces/IncidentContextSurface.cs` (ListByContextAsync, GetSeverityCountByContextAsync, ListNonProductionSignalsAsync)
- `Infrastructure/Surfaces/ReliabilityIncidentSurface.cs` (GetActiveIncidentsAsync, GetAllServicesIncidentSignalsAsync, GetTeamIncidentsAsync, GetDomainIncidentsAsync)
- `Infrastructure/Persistence/Reliability/ReliabilitySnapshotRepository.cs` (GetHistoryAsync, GetLatestAsync)
- `Infrastructure/Surfaces/ReliabilityRuntimeSurface.cs` (GetLatestSignalAsync, GetLatestSignalsAllServicesAsync, GetObservabilityScoreAsync, GetObservabilityScoresAllServicesAsync)

## Responsabilidades permitidas

- Adicionar `CancellationToken cancellationToken = default` a cada método async
- Propagar token para queries EF Core e operações I/O
- Atualizar interfaces e abstrações correspondentes

## Responsabilidades proibidas

- Alterar lógica de cálculo de métricas, SLO, error budget ou burn rate
- Alterar lógica de correlação de incidentes
- Refatorar estrutura de surfaces ou repositórios

## Critérios de aceite

1. Todos os 36 métodos async têm `CancellationToken`
2. Token propagado para todas as queries EF Core
3. Módulo compila sem erros
4. Testes existentes compilam e passam
5. Solução completa compila

## Validações obrigatórias

- `dotnet build src/modules/operationalintelligence/` — sem erros
- `dotnet build NexTraceOne.sln` — sem erros
- `grep -r "async Task" src/modules/operationalintelligence/ | grep -v CancellationToken` retorna zero

## Riscos e cuidados

- CostIntelligenceModuleService é consumido cross-module (via ICostIntelligenceModule) — atualizar interface
- ReliabilityRuntimeSurface e ReliabilityIncidentSurface são consumidos por handlers de Reliability — coordenar
- IncidentContextSurface é consumida por OperationalIntelligence e Governance — interface partilhada

## Dependências

- Nenhuma dependência hard de outros prompts
- Pode ser executado em paralelo com P-00-01 a P-00-04

## Próximos prompts sugeridos

- P-00-06 (CancellationToken nos módulos restantes — fecha a série completa)
