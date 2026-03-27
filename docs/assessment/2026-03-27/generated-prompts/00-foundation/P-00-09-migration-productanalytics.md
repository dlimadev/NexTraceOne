# P-00-09 — Criar migração EF Core para o módulo ProductAnalytics

## Modo de operação

Implementation

## Objetivo

Criar a migração EF Core inicial para o módulo ProductAnalytics, gerando as tabelas `pan_analytics_events`
e `pan_outbox_messages` em PostgreSQL. O ProductAnalyticsDbContext já está configurado com a
entidade AnalyticsEvent extraída do GovernanceDbContext na fase P2.3, mas não existe migração.

## Problema atual

O módulo ProductAnalytics foi extraído do módulo Governance como bounded context independente. O
`ProductAnalyticsDbContext` em `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Infrastructure/Persistence/ProductAnalyticsDbContext.cs`
define um DbSet:

- **AnalyticsEvents** — eventos de product analytics para tracking de utilização, feature adoption e comportamento de utilizadores

Prefixo `pan_`, tabela de outbox `pan_outbox_messages`, implementa `IUnitOfWork`. Sem migração,
o módulo de analytics não pode persistir eventos de produto, impedindo tracking de utilização
e análise de adoption que são fundamentais para o pilar de Operational Intelligence do NexTraceOne.

## Escopo permitido

- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Infrastructure/Persistence/` — criação de migração
- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Infrastructure/Persistence/Migrations/` — novo diretório

## Escopo proibido

- Alterar o ProductAnalyticsDbContext ou entity configurations
- Alterar entidades de domínio
- Criar migrações para outros módulos
- Remover entidade AnalyticsEvent do GovernanceDbContext (migração de dados é separada)

## Ficheiros principais candidatos a alteração

- `Persistence/Migrations/<timestamp>_InitialProductAnalyticsMigration.cs` (novo)
- `Persistence/Migrations/<timestamp>_InitialProductAnalyticsMigration.Designer.cs` (novo)
- `Persistence/Migrations/ProductAnalyticsDbContextModelSnapshot.cs` (novo)

## Responsabilidades permitidas

- Criar migração EF Core usando `dotnet ef migrations add`
- Verificar que o ficheiro cria as tabelas `pan_analytics_events` e `pan_outbox_messages`
- Verificar índices, chaves primárias e tipos de coluna
- Confirmar independência de outros schemas

## Responsabilidades proibidas

- Alterar schema ou mapeamento de entidades
- Adicionar seed data
- Executar contra produção
- Remover entidade equivalente do GovernanceDbContext

## Critérios de aceite

1. Ficheiro de migração criado em `Persistence/Migrations/`
2. A migração cria: `pan_analytics_events` e `pan_outbox_messages`
3. Migração compila sem erros
4. `dotnet ef database update` aplica com sucesso
5. Módulo e solução compilam

## Validações obrigatórias

- `dotnet build src/modules/productanalytics/` — sem erros
- `dotnet build NexTraceOne.sln` — sem erros
- Verificar que o ficheiro de migração contém `CreateTable` para as 2 tabelas
- Verificar que colunas de AnalyticsEvent (EventType, Timestamp, UserId, TenantId, Payload, etc.) estão mapeadas

## Riscos e cuidados

- Se a tabela equivalente já existe no schema de Governance, pode haver conflito de nomes
- Verificar que o prefixo `pan_` é consistente em todas as tabelas e índices
- A migração deve ser idempotente e não falhar se aplicada num ambiente limpo
- Comparar com migrações de outros módulos novos (Knowledge, Integrations) para consistência de padrão

## Dependências

- Nenhuma dependência de outros prompts
- Pode ser executado em paralelo com P-00-07 e P-00-08

## Próximos prompts sugeridos

- P-01-01 (Início da fase de correções críticas)
- P-00-06 (Se CancellationToken nos módulos restantes ainda não foi executado)
