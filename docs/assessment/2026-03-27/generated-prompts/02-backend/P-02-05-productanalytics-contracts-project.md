# P-02-05 — Criar projeto NexTraceOne.ProductAnalytics.Contracts

## 1. Título

Criar o projeto Contracts para o módulo ProductAnalytics, alinhando a estrutura com os restantes módulos da plataforma.

## 2. Modo de operação

**Implementation**

## 3. Objetivo

O módulo ProductAnalytics é o único módulo com apenas 4 camadas (Domain, Application, Infrastructure, API). Todos os outros módulos possuem 5 camadas, incluindo um projeto Contracts que define integration events e interfaces de consulta cross-module. Este prompt cria o projeto NexTraceOne.ProductAnalytics.Contracts para alinhar a arquitetura e permitir que outros módulos consumam eventos de analítica.

## 4. Problema atual

- O módulo ProductAnalytics possui 4 projetos:
  - `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Domain/`
  - `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/`
  - `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Infrastructure/`
  - `src/modules/productanalytics/NexTraceOne.ProductAnalytics.API/`
- Módulos como Integrations, Knowledge, AIKnowledge, Governance, Catalog, etc. possuem todos um projeto Contracts:
  - `src/modules/integrations/NexTraceOne.Integrations.Contracts/IntegrationEvents.cs`
  - `src/modules/knowledge/NexTraceOne.Knowledge.Contracts/KnowledgeContracts.cs`
  - `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Contracts/`
- Sem Contracts, o ProductAnalytics não pode publicar eventos de integração para outros módulos (ex: "milestone atingido", "friction detectada", "adoption threshold cruzado").
- Sem Contracts, outros módulos não podem consultar dados de analítica de forma governada.

## 5. Escopo permitido

- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Contracts/` — pasta e projeto novos
- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Contracts/NexTraceOne.ProductAnalytics.Contracts.csproj` — novo
- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Contracts/IntegrationEvents.cs` — novo
- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Contracts/ServiceInterfaces/` — interfaces de consulta cross-module (novo)
- `NexTraceOne.sln` — adicionar o novo projeto à solution
- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/NexTraceOne.ProductAnalytics.Application.csproj` — referenciar Contracts

## 6. Escopo proibido

- Não alterar módulos fora de `src/modules/productanalytics/`.
- Não alterar a solution beyond adicionar o novo projeto.
- Não implementar publishers de eventos neste prompt (isso é P-02-06).
- Não alterar entidades de domínio.
- Não alterar migrações.

## 7. Ficheiros principais candidatos a alteração

1. `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Contracts/NexTraceOne.ProductAnalytics.Contracts.csproj` (novo)
2. `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Contracts/IntegrationEvents.cs` (novo)
3. `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Contracts/ServiceInterfaces/IProductAnalyticsModule.cs` (novo)
4. `NexTraceOne.sln` — registo do projeto
5. `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/NexTraceOne.ProductAnalytics.Application.csproj`

## 8. Responsabilidades permitidas

- Criar .csproj seguindo exatamente o padrão dos outros módulos Contracts (ex: Knowledge.Contracts, Integrations.Contracts).
- Definir integration events relevantes: MilestoneAchievedIntegrationEvent, FrictionDetectedIntegrationEvent, ModuleAdoptionChangedIntegrationEvent.
- Definir interface IProductAnalyticsModule com métodos de consulta: GetModuleAdoptionAsync, GetPersonaUsageSummaryAsync.
- Referenciar apenas BuildingBlocks.Application (para base classes de integration events).
- Registar projeto na solution com folder structure correta.

## 9. Responsabilidades proibidas

- Não colocar lógica de negócio no projeto Contracts — apenas definições de eventos e interfaces.
- Não referenciar projetos de Infrastructure ou API a partir de Contracts.
- Não criar implementações concretas no Contracts — apenas tipos abstratos e records.
- Não publicar eventos neste prompt — apenas definir os tipos.

## 10. Critérios de aceite

- [ ] Projeto NexTraceOne.ProductAnalytics.Contracts criado e adicionado à solution.
- [ ] Pelo menos 3 integration events definidos como sealed records.
- [ ] Interface IProductAnalyticsModule definida com métodos de consulta.
- [ ] Projeto Application referencia Contracts.
- [ ] O .csproj segue o mesmo padrão de TargetFramework e referências dos outros Contracts.
- [ ] Compilação completa da solution sem erros.
- [ ] Sem dependências circulares.

## 11. Validações obrigatórias

- `dotnet build` da solution completa.
- Verificar que o projeto aparece no `NexTraceOne.sln` com folder structure correta.
- Verificar que o Application.csproj referencia o Contracts.csproj.
- Comparar estrutura do .csproj com `src/modules/integrations/NexTraceOne.Integrations.Contracts/NexTraceOne.Integrations.Contracts.csproj` para garantir consistência.

## 12. Riscos e cuidados

- Risco baixo — é criação de projeto novo sem alteração de código existente.
- Garantir que o namespace segue a convenção: `NexTraceOne.ProductAnalytics.Contracts`.
- Garantir que integration events herdam da base class correta (verificar padrão em IntegrationEvents.cs de outros módulos).
- Garantir que não se adicionam referências desnecessárias ao .csproj.

## 13. Dependências

- **P-00-09** — Módulo ProductAnalytics deve existir com estrutura base (Domain, Application, Infrastructure, API).
- Padrão de Contracts definido pelos módulos existentes (Knowledge.Contracts, Integrations.Contracts, AIKnowledge.Contracts).

## 14. Próximos prompts sugeridos

- **P-02-06** — Wire outbox event publishing usando os eventos definidos neste Contracts.
- **P-02-03** — Handlers com dados reais (complementar, os eventos serão disparados após operações reais).
- **P-XX-XX** — Consumidores cross-module que reagem a MilestoneAchievedIntegrationEvent.
