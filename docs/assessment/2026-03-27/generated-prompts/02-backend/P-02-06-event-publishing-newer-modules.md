# P-02-06 — Implementar publicação de eventos via outbox nos módulos Integrations e ProductAnalytics

## 1. Título

Implementar publicação de integration events via outbox pattern para os módulos Integrations e ProductAnalytics.

## 2. Modo de operação

**Implementation**

## 3. Objetivo

Os módulos mais recentes (Integrations e ProductAnalytics) possuem definições de integration events nos seus projetos Contracts, mas não publicam eventos via outbox quando operações relevantes ocorrem. Este prompt implementa a publicação de eventos, alinhando estes módulos com o padrão já estabelecido nos módulos mais maduros da plataforma.

## 4. Problema atual

- `src/modules/integrations/NexTraceOne.Integrations.Contracts/IntegrationEvents.cs` define eventos mas nenhum handler os publica.
- O `IntegrationsDbContext` em `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Persistence/IntegrationsDbContext.cs` não implementa outbox processing.
- O `ProductAnalyticsDbContext` em `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Infrastructure/Persistence/ProductAnalyticsDbContext.cs` igualmente não publica eventos.
- Handlers como `RetryConnector`, `ReprocessExecution` no módulo Integrations executam operações sem notificar outros módulos.
- Handler `RecordAnalyticsEvent` no ProductAnalytics regista eventos analíticos sem disparar integration events.
- Sem publicação de eventos, os módulos operam em silos — violando o princípio de consistência eventual da arquitetura.

## 5. Escopo permitido

- `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/` — implementar outbox no DbContext e nos repositórios
- `src/modules/integrations/NexTraceOne.Integrations.Application/Features/` — adicionar publicação nos handlers existentes
- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Infrastructure/` — implementar outbox
- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Features/` — adicionar publicação
- Building blocks de outbox (se existir padrão em `src/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure/`)

## 6. Escopo proibido

- Não alterar módulos fora de Integrations e ProductAnalytics.
- Não implementar consumidores de eventos (será prompt separado).
- Não alterar as definições de integration events existentes no Contracts.
- Não criar sistema de mensageria externo (RabbitMQ, Kafka) — usar outbox pattern com PostgreSQL.
- Não alterar migrações já aplicadas (nova migração é aceitável se necessária para tabela de outbox).

## 7. Ficheiros principais candidatos a alteração

1. `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Persistence/IntegrationsDbContext.cs`
2. `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/DependencyInjection.cs`
3. `src/modules/integrations/NexTraceOne.Integrations.Application/Features/RetryConnector/RetryConnector.cs`
4. `src/modules/integrations/NexTraceOne.Integrations.Application/Features/ReprocessExecution/ReprocessExecution.cs`
5. `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Infrastructure/Persistence/ProductAnalyticsDbContext.cs`
6. `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Infrastructure/DependencyInjection.cs`

## 8. Responsabilidades permitidas

- Implementar IOutboxWriter ou equivalente nos DbContexts dos dois módulos, seguindo padrão dos módulos existentes.
- Adicionar chamada de publicação de evento em handlers que executam operações de escrita relevantes.
- Criar tabela de outbox_messages se não existir (via nova migração).
- Registar serviços de outbox no DI dos dois módulos.
- Usar transação atómica: operação de negócio + inserção na outbox na mesma transação.
- Usar CancellationToken em todas as operações.
- Adicionar logging estruturado para publicação e processamento de eventos.

## 9. Responsabilidades proibidas

- Não implementar background worker para processar outbox neste prompt (já deve existir no ApiHost ou Workers).
- Não criar consumidores de eventos — apenas publicação.
- Não expor endpoint REST para outbox — é mecanismo interno.
- Não criar dependências em sistemas de mensageria externos.

## 10. Critérios de aceite

- [ ] IntegrationsDbContext implementa outbox pattern (inserção atómica com operação de negócio).
- [ ] ProductAnalyticsDbContext implementa outbox pattern.
- [ ] RetryConnector publica ConnectorRetryRequestedIntegrationEvent (ou equivalente).
- [ ] ReprocessExecution publica ExecutionReprocessRequestedIntegrationEvent (ou equivalente).
- [ ] RecordAnalyticsEvent publica AnalyticsEventRecordedIntegrationEvent (ou equivalente).
- [ ] Compilação completa da solution sem erros.
- [ ] Padrão de outbox é consistente com módulos existentes.

## 11. Validações obrigatórias

- Compilação dos módulos Integrations e ProductAnalytics (todos os projetos).
- Compilação da solution NexTraceOne.sln.
- Verificar que o padrão de outbox seguido é o mesmo dos módulos maduros (comparar com Governance ou Catalog).
- Verificar que não foram introduzidas referências circulares.
- Verificar que handlers existentes continuam funcionais.

## 12. Riscos e cuidados

- A tabela de outbox pode já existir como schema partilhado — verificar antes de criar nova migração.
- Transações atómicas (negócio + outbox) podem afetar performance se não forem geridas corretamente.
- O processamento do outbox (background worker) deve já existir — caso contrário documentar o gap.
- Eventos publicados sem consumidor ficam pendentes — não é problema funcional mas deve ser monitorizado.
- Serialização dos eventos deve ser compatível com o formato esperado pelo outbox processor.

## 13. Dependências

- **P-00-08** — Migração do módulo Integrations aplicada.
- **P-00-09** — Migração do módulo ProductAnalytics aplicada.
- **P-02-05** — Projeto Contracts do ProductAnalytics criado (para ter os tipos de eventos disponíveis).
- Padrão de outbox existente em building blocks ou em módulos maduros como referência.

## 14. Próximos prompts sugeridos

- **P-XX-XX** — Consumidores de eventos: módulo Governance reage a eventos do Integrations.
- **P-XX-XX** — Dashboard de outbox health: visualizar eventos pendentes, falhados e processados.
- **P-XX-XX** — Retry policy para eventos falhados no outbox.
