# P-00-08 — Criar migração EF Core para o módulo Integrations

## Modo de operação

Implementation

## Objetivo

Criar a migração EF Core inicial para o módulo Integrations, gerando as tabelas `int_connectors`,
`int_ingestion_sources`, `int_ingestion_executions` e `int_outbox_messages` em PostgreSQL.
O IntegrationsDbContext já está configurado com as entidades extraídas do GovernanceDbContext
nas fases P2.1 e P2.2, mas não existe migração — as tabelas ainda não existem na base de dados.

## Problema atual

O módulo Integrations foi extraído do módulo Governance como bounded context independente. O
`IntegrationsDbContext` em `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Persistence/IntegrationsDbContext.cs`
define três DbSets:

- **IntegrationConnectors** — conectores de integração com sistemas externos (GitLab, Jenkins, GitHub, Azure DevOps, etc.)
- **IngestionSources** — fontes de ingestão de dados configuradas (telemetria, pipelines, eventos)
- **IngestionExecutions** — registo de execuções de ingestão com estado, timestamps e métricas

Todos usam prefixo `int_` e a tabela de outbox é `int_outbox_messages`. O módulo implementa
`IUnitOfWork` e usa Integration Events. Sem migração, os handlers de integração não conseguem
persistir conectores nem registar execuções de ingestão.

## Escopo permitido

- `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Persistence/` — criação de migração
- `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Persistence/Migrations/` — novo diretório

## Escopo proibido

- Alterar o IntegrationsDbContext ou entity configurations existentes
- Alterar entidades de domínio do módulo Integrations
- Criar migrações para outros módulos
- Remover tabelas do GovernanceDbContext (a migração de dados é separada)

## Ficheiros principais candidatos a alteração

- `Persistence/Migrations/<timestamp>_InitialIntegrationsMigration.cs` (novo)
- `Persistence/Migrations/<timestamp>_InitialIntegrationsMigration.Designer.cs` (novo)
- `Persistence/Migrations/IntegrationsDbContextModelSnapshot.cs` (novo)

## Responsabilidades permitidas

- Criar migração EF Core usando `dotnet ef migrations add`
- Verificar que o ficheiro cria as 4 tabelas com prefixo `int_`
- Verificar índices, chaves primárias e constraints
- Confirmar que a migração é independente de outros schemas

## Responsabilidades proibidas

- Alterar schema ou mapeamento de entidades
- Adicionar seed data
- Executar contra produção
- Remover tabelas equivalentes do GovernanceDbContext

## Critérios de aceite

1. Ficheiro de migração criado em `Persistence/Migrations/`
2. A migração cria: `int_connectors`, `int_ingestion_sources`, `int_ingestion_executions`, `int_outbox_messages`
3. Migração compila sem erros
4. `dotnet ef database update` aplica com sucesso
5. Módulo e solução compilam

## Validações obrigatórias

- `dotnet build src/modules/integrations/` — sem erros
- `dotnet build NexTraceOne.sln` — sem erros
- Verificar que o ficheiro de migração contém `CreateTable` para as 4 tabelas
- Verificar que não há referências a tabelas de outros módulos (ex: `gov_` tables)

## Riscos e cuidados

- As entidades foram extraídas do GovernanceDbContext — garantir que não há duplicação de tabelas
- Se dados já existem nas tabelas `gov_` correspondentes, será necessário um script de migração de dados separado
- Verificar que o schema isolation está correto — `int_` prefix consistente
- A tabela de outbox deve seguir o padrão dos outros módulos

## Dependências

- Nenhuma dependência de outros prompts
- Pode ser executado em paralelo com P-00-07 e P-00-09

## Próximos prompts sugeridos

- P-00-09 (Migração do módulo ProductAnalytics)
- P-01-01 (Início da fase de correções críticas)
