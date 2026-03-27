# P-01-03 — Substituir IsSimulated nos handlers de FinOps do módulo Governance

## Modo de operação

Implementation

## Objetivo

Substituir a propriedade `IsSimulated` e os dados parcialmente estáticos nos 10 handlers de FinOps
do módulo Governance por queries reais às tabelas `gov_` e dados do módulo OperationalIntelligence
(via interfaces cross-module). Os handlers devem consultar custos, tendências, benchmarks e
indicadores de eficiência a partir de dados persistidos, eliminando a flag `IsSimulated` dos DTOs.

## Problema atual

O módulo Governance contém 10 handlers de FinOps que incluem a propriedade `IsSimulated` nos seus
DTOs de resposta. A análise do código revela:

| Handler                  | Ficheiro                                    | Linhas com IsSimulated |
|--------------------------|---------------------------------------------|------------------------|
| GetDomainFinOps          | `GetDomainFinOps/GetDomainFinOps.cs`        | 76, 91, 107            |
| GetEfficiencyIndicators  | `GetEfficiencyIndicators/GetEfficiencyIndicators.cs` | 35, 94, 118    |
| GetExecutiveTrends       | `GetExecutiveTrends/GetExecutiveTrends.cs`  | 46, 124                |
| GetBenchmarking          | `GetBenchmarking/GetBenchmarking.cs`        | 86, 101, 106           |
| GetServiceFinOps         | `GetServiceFinOps/GetServiceFinOps.cs`      | 56, 71, 91             |
| GetFinOpsSummary         | `GetFinOpsSummary/GetFinOpsSummary.cs`      | 88, 103, 114           |
| GetFinOpsTrends          | `GetFinOpsTrends/GetFinOpsTrends.cs`        | 92, 99, 107            |
| GetTeamFinOps            | `GetTeamFinOps/GetTeamFinOps.cs`            | 68, 83, 100            |
| GetWasteSignals          | `GetWasteSignals/GetWasteSignals.cs`        | 37, 94, 121            |
| GetExecutiveDrillDown    | `GetExecutiveDrillDown/GetExecutiveDrillDown.cs` | 82, 115            |

Padrão encontrado: todos os DTOs declaram `bool IsSimulated = false` com comentário XML
"IsSimulated=true indica dados demonstrativos". Embora o valor seja `false`, a propriedade existe
como preparação para sinalizar dados fictícios — indicando que os dados retornados podem não
vir de queries reais aos repositórios de custo.

Os dados de custo reais estão disponíveis no módulo OperationalIntelligence via `ICostIntelligenceModule`
(com métodos GetCurrentMonthlyCostAsync, GetCostTrendPercentageAsync, GetCostRecordsAsync,
GetServiceCostAsync, GetCostsByTeamAsync, GetCostsByDomainAsync).

## Escopo permitido

- `src/modules/governance/NexTraceOne.Governance.Application/Features/` — apenas handlers de FinOps
- Interfaces cross-module com OperationalIntelligence (ICostIntelligenceModule)

## Escopo proibido

- Handlers de compliance, risk, teams, domains ou governance packs
- Módulo OperationalIntelligence (apenas consumir interfaces existentes)
- Ficheiros de migração
- Frontend

## Ficheiros principais candidatos a alteração

- `Features/GetServiceFinOps/GetServiceFinOps.cs`
- `Features/GetTeamFinOps/GetTeamFinOps.cs`
- `Features/GetDomainFinOps/GetDomainFinOps.cs`
- `Features/GetFinOpsTrends/GetFinOpsTrends.cs`
- `Features/GetFinOpsSummary/GetFinOpsSummary.cs`
- `Features/GetBenchmarking/GetBenchmarking.cs`
- `Features/GetExecutiveDrillDown/GetExecutiveDrillDown.cs`
- `Features/GetExecutiveTrends/GetExecutiveTrends.cs`
- `Features/GetWasteSignals/GetWasteSignals.cs`
- `Features/GetEfficiencyIndicators/GetEfficiencyIndicators.cs`

## Responsabilidades permitidas

- Remover propriedade `IsSimulated` de todos os DTOs de resposta
- Substituir dados estáticos por queries a `ICostIntelligenceModule`
- Adicionar injeção de dependência do `ICostIntelligenceModule` onde necessário
- Tratar cenário de "sem dados de custo disponíveis" de forma explícita nos DTOs

## Responsabilidades proibidas

- Alterar lógica do módulo OperationalIntelligence
- Criar novas tabelas ou migrações
- Alterar handlers fora do escopo FinOps

## Critérios de aceite

1. Propriedade `IsSimulated` removida de todos os 10 handlers de FinOps
2. Handlers consultam dados reais via `ICostIntelligenceModule` ou repositórios `gov_`
3. Cenário de "sem dados" tratado com DTOs claros (campos nullable ou indicadores explícitos)
4. Módulo compila sem erros
5. Testes existentes passam
6. `grep -rn "IsSimulated" src/modules/governance/` retorna zero resultados

## Validações obrigatórias

- `dotnet build src/modules/governance/` — sem erros
- `dotnet build NexTraceOne.sln` — sem erros
- `grep -rn "IsSimulated" src/modules/governance/` — zero resultados
- Verificar que cada handler tem injeção de pelo menos um serviço real de dados

## Riscos e cuidados

- Remoção de `IsSimulated` dos DTOs é breaking change na API — atualizar contratos documentados
- Frontend pode depender da propriedade `IsSimulated` para sinalizar dados demonstrativos — coordenar remoção
- `ICostIntelligenceModule` pode não estar registado em todos os ambientes — tratar gracefully
- Alguns handlers podem compor dados de múltiplas fontes — garantir fallback coerente

## Dependências

- P-00-05 e P-00-06 (CancellationToken nos módulos envolvidos) idealmente já aplicados
- P-01-02 (consolidação de Cost no OperationalIntelligence) preferencialmente concluído

## Próximos prompts sugeridos

- P-01-04 (Hardening dos handlers de ExternalAI)
- P-01-07 (Completar handlers de Governance com DeferredFields)
