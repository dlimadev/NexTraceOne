# P-01-07 — Completar handlers de Governance com campos DeferredFields e stubs residuais

## Modo de operação

Implementation

## Objetivo

Completar os handlers do módulo Governance que possuem campos marcados como `DeferredFields`
(indicando dados ainda não integrados de outros módulos) e handlers parcialmente implementados
com stubs ou dados hardcoded. Isto inclui: GetTeamDetail, ListTeams, GetDomainDetail, ListDomains,
GetGovernancePack, ListGovernancePacks, ListGovernanceWaivers e GetTeamGovernanceSummary. O objetivo
é substituir campos diferidos por queries reais cross-module ou, quando a fonte de dados não
estiver disponível, tornar o estado explícito com indicadores claros no DTO.

## Problema atual

A análise da codebase revela que vários handlers de Governance possuem o padrão `DeferredFields`
que lista explicitamente campos não populados por dados reais:

### Handlers com DeferredFields

| Handler         | DeferredFields                                                             | Valores atuais       |
|-----------------|---------------------------------------------------------------------------|---------------------|
| GetTeamDetail   | `["ActiveIncidentCount", "RecentChangeCount", "MaturityLevel", "ReliabilityScore"]` | 0, 0, "Developing", 0 |
| ListTeams       | `["ContractCount", "MemberCount", "MaturityLevel"]`                       | 0, 0, "Developing"   |
| GetDomainDetail | `["ActiveIncidentCount", "RecentChangeCount", "MaturityLevel", "ReliabilityScore"]` | 0, 0, "Developing", 0 |
| ListDomains     | `["ContractCount", "MaturityLevel"]`                                      | 0, "Developing"      |

Estes campos estão hardcoded porque requerem dados cross-module:
- **ActiveIncidentCount** — requer `IIncidentContextSurface` do OperationalIntelligence
- **RecentChangeCount** — requer dados do ChangeGovernance
- **MaturityLevel** — requer cálculo baseado em múltiplos sinais (contratos, SLOs, runbooks, cobertura)
- **ReliabilityScore** — requer `IReliabilityRuntimeSurface` do OperationalIntelligence
- **ContractCount** — requer integração com Catalog (contratos por serviço/equipa)
- **MemberCount** — requer integração com IdentityAccess (membros por equipa)

### Handlers com stubs parciais

Adicionalmente, os handlers de FinOps e governance packs (GetGovernancePack, ListGovernancePacks,
ListGovernanceWaivers) podem ter campos que dependem de rollouts e compliance checks que
necessitam de consolidação.

**Nota:** Os handlers ListIntegrationConnectors, GetIntegrationConnector, ListIngestionSources
e ListIngestionExecutions foram extraídos para o módulo Integrations — não existem mais no Governance.

## Escopo permitido

- `src/modules/governance/NexTraceOne.Governance.Application/Features/` — handlers afetados
- Interfaces cross-module: `IIncidentContextSurface`, `IReliabilityRuntimeSurface`, `ICatalogGraphModule`
- Abstrações de integração com IdentityAccess e ChangeGovernance

## Escopo proibido

- Módulos fonte (OperationalIntelligence, ChangeGovernance, IdentityAccess, Catalog) — apenas consumir interfaces
- Handlers de FinOps (tratados em P-01-03)
- Ficheiros de migração
- Frontend

## Ficheiros principais candidatos a alteração

- `Features/GetTeamDetail/GetTeamDetail.cs`
- `Features/ListTeams/ListTeams.cs`
- `Features/GetDomainDetail/GetDomainDetail.cs`
- `Features/ListDomains/ListDomains.cs`
- `Features/GetGovernancePack/GetGovernancePack.cs`
- `Features/ListGovernancePacks/ListGovernancePacks.cs`
- `Features/ListGovernanceWaivers/ListGovernanceWaivers.cs`
- `Features/GetTeamGovernanceSummary/GetTeamGovernanceSummary.cs`

## Responsabilidades permitidas

- Injectar interfaces cross-module (IIncidentContextSurface, IReliabilityRuntimeSurface, ICatalogGraphModule)
- Substituir valores hardcoded por queries reais quando a interface estiver disponível
- Para campos cuja fonte ainda não está disponível, tornar nullable e remover da DeferredFields
- Remover o padrão DeferredFields quando todos os campos forem resolvidos ou nullable
- Adicionar logging quando uma fonte cross-module não está disponível

## Responsabilidades proibidas

- Alterar módulos fonte
- Criar novas interfaces cross-module sem justificação
- Alterar migrações
- Inventar dados fictícios para substituir os DeferredFields

## Critérios de aceite

1. ActiveIncidentCount populado via IIncidentContextSurface (ou nullable se indisponível)
2. ReliabilityScore populado via IReliabilityRuntimeSurface (ou nullable se indisponível)
3. ContractCount populado via ICatalogGraphModule (ou nullable se indisponível)
4. DeferredFields removido ou reduzido ao mínimo necessário
5. Campos sem fonte disponível são nullable com documentação explícita
6. Módulo compila e testes passam

## Validações obrigatórias

- `dotnet build src/modules/governance/` — sem erros
- `dotnet build NexTraceOne.sln` — sem erros
- `grep -rn "DeferredFields" src/modules/governance/` — reduzido ou zero resultados
- Verificar que campos anteriormente hardcoded agora vêm de queries reais ou são nullable

## Riscos e cuidados

- Interfaces cross-module podem não estar registadas em DI — tratar com optional injection ou null checks
- Queries cross-module podem ser lentas — considerar caching ou lazy loading para listagens grandes
- Alterar tipos de campos de int para int? pode quebrar consumidores — verificar frontend
- MemberCount e MaturityLevel podem requerer interfaces que ainda não existem — documentar gap

## Dependências

- P-00-06 (CancellationToken nos módulos restantes) idealmente já aplicado
- P-01-01 e P-01-02 (consolidação do OperationalIntelligence) preferencialmente concluídos
- P-01-03 (FinOps) pode ser executado em paralelo

## Próximos prompts sugeridos

- Fase 02 — Consolidação cross-module de dados operacionais
- Fase 02 — Implementação de MaturityLevel calculator baseado em sinais reais
