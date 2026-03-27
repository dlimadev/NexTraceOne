# P-01-01 — Substituir dados simulados nos handlers de Reliability do OperationalIntelligence

## Modo de operação

Implementation

## Objetivo

Verificar e consolidar os 7 handlers de Reliability do módulo OperationalIntelligence (ListServiceReliability,
GetServiceReliabilityDetail, GetServiceReliabilityCoverage, GetServiceReliabilityTrend,
GetDomainReliabilitySummary, GetTeamReliabilitySummary, GetTeamReliabilityTrend) para garantir
que todos utilizam exclusivamente dados reais do repositório, sem fallbacks hardcoded, scores
fictícios ou dados simulados residuais.

## Problema atual

A análise da codebase revela que os handlers de Reliability já utilizam dados reais via repositórios
(IReliabilityRuntimeSurface, IReliabilityIncidentSurface, IReliabilitySnapshotRepository,
ISloDefinitionRepository, IErrorBudgetSnapshotRepository, IBurnRateSnapshotRepository). No entanto,
há áreas que necessitam consolidação:

- **ListServiceReliability** — compõe runtime signals e incident data, calcula scores — verificar se há defaults hardcoded quando dados estão vazios
- **GetServiceReliabilityDetail** — consolida runtime, incidentes e snapshots — verificar completude de campos
- **GetServiceReliabilityCoverage** — verifica sinais operacionais (janela 24h), existência de runbooks e ownership de incidentes
- **GetServiceReliabilityTrend** — consulta snapshots históricos (janela 30 dias)
- **GetDomainReliabilitySummary** — filtra incidentes reais por domínio
- **GetTeamReliabilitySummary** — filtra incidentes reais por equipa
- **GetTeamReliabilityTrend** — nota: trending histórico requer snapshots agregados futuros

Os handlers não contêm `IsSimulated` (ao contrário do módulo Governance), mas podem ter valores
default (0m, "Unknown") quando não existem dados na base — necessário distinguir "sem dados"
de "dados reais com valor zero".

## Escopo permitido

- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/Reliability/Features/` — apenas Reliability
- Repositórios e surfaces correspondentes em Infrastructure/

## Escopo proibido

- Handlers de Automation ou Cost do OperationalIntelligence
- Outros módulos (Governance, AIKnowledge, etc.)
- Ficheiros de migração
- Lógica de cálculo de SLO/SLA core (RegisterSloDefinition, RegisterSlaDefinition)

## Ficheiros principais candidatos a alteração

- `Reliability/Features/ListServiceReliability/ListServiceReliability.cs`
- `Reliability/Features/GetServiceReliabilityDetail/GetServiceReliabilityDetail.cs`
- `Reliability/Features/GetServiceReliabilityCoverage/GetServiceReliabilityCoverage.cs`
- `Reliability/Features/GetServiceReliabilityTrend/GetServiceReliabilityTrend.cs`
- `Reliability/Features/GetDomainReliabilitySummary/GetDomainReliabilitySummary.cs`
- `Reliability/Features/GetTeamReliabilitySummary/GetTeamReliabilitySummary.cs`
- `Reliability/Features/GetTeamReliabilityTrend/GetTeamReliabilityTrend.cs`
- Surfaces: `ReliabilityRuntimeSurface.cs`, `ReliabilityIncidentSurface.cs`
- Repositórios: `ReliabilitySnapshotRepository.cs`

## Responsabilidades permitidas

- Auditar cada handler para identificar valores default ou fallbacks fictícios
- Substituir defaults por indicadores claros de "sem dados disponíveis" (ex: null em vez de 0m)
- Garantir que DTOs de resposta indicam claramente quando dados são insuficientes
- Melhorar queries ao repositório se necessário para obter dados mais completos

## Responsabilidades proibidas

- Alterar lógica de cálculo de SLO, error budget ou burn rate
- Introduzir `IsSimulated` ou flags de simulação
- Alterar handlers de Automation ou Cost
- Alterar migrações

## Critérios de aceite

1. Cada handler consulta exclusivamente dados reais do repositório
2. Nenhum valor hardcoded residual como score fictício
3. Respostas indicam claramente quando não há dados suficientes
4. Módulo compila sem erros
5. Testes existentes passam

## Validações obrigatórias

- `dotnet build src/modules/operationalintelligence/` — sem erros
- `dotnet build NexTraceOne.sln` — sem erros
- Pesquisa de strings suspeitas: `grep -rn "hardcoded\|simulated\|fake\|placeholder" src/modules/operationalintelligence/Application/Reliability/`

## Riscos e cuidados

- Handlers de Reliability são consumidos pelo frontend — alterações nos DTOs podem exigir ajustes na UI
- GetTeamReliabilityTrend depende de snapshots agregados que podem não existir ainda — documentar limitação
- Alterar semântica de "0" para "null" pode quebrar consumidores — verificar frontend

## Dependências

- Idealmente P-00-05 (CancellationToken neste módulo) já foi aplicado
- Não depende de outros prompts da fase 01

## Próximos prompts sugeridos

- P-01-02 (Consolidar handlers de Automation e Cost)
- P-01-03 (Substituir IsSimulated nos handlers de FinOps do Governance)
