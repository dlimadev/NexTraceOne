# P-00-07 — Criar migração EF Core para o módulo Knowledge

## Modo de operação

Implementation

## Objetivo

Criar a migração EF Core inicial para o módulo Knowledge, gerando as tabelas `knw_documents`,
`knw_operational_notes`, `knw_relations` e `knw_outbox_messages` em PostgreSQL. O KnowledgeDbContext
já está configurado com as entidades e mapeamentos, mas não existe nenhum ficheiro de migração
no diretório de Persistence — as tabelas ainda não foram criadas na base de dados.

## Problema atual

O módulo Knowledge tem o `KnowledgeDbContext` completamente configurado em
`src/modules/knowledge/NexTraceOne.Knowledge.Infrastructure/Persistence/KnowledgeDbContext.cs`
com três DbSets (KnowledgeDocuments, OperationalNotes, KnowledgeRelations) e tabela de outbox,
todos com prefixo `knw_`. No entanto, o diretório de Migrations não existe — o módulo não pode
persistir dados. As entidades mapeadas são:

- **KnowledgeDocument** — documentos de conhecimento operacional e técnico
- **OperationalNote** — notas operacionais associadas a serviços, incidentes ou mudanças
- **KnowledgeRelation** — relações entre objetos de conhecimento e outros contextos do produto

O módulo implementa `IUnitOfWork` e usa Integration Events via outbox pattern.

## Escopo permitido

- `src/modules/knowledge/NexTraceOne.Knowledge.Infrastructure/Persistence/` — criação de migração
- `src/modules/knowledge/NexTraceOne.Knowledge.Infrastructure/Persistence/Migrations/` — novo diretório

## Escopo proibido

- Alterar o KnowledgeDbContext ou entity configurations existentes
- Alterar entidades de domínio
- Criar migrações para outros módulos
- Alterar dados existentes noutros schemas

## Ficheiros principais candidatos a alteração

- `Persistence/Migrations/<timestamp>_InitialKnowledgeMigration.cs` (novo)
- `Persistence/Migrations/<timestamp>_InitialKnowledgeMigration.Designer.cs` (novo)
- `Persistence/Migrations/KnowledgeDbContextModelSnapshot.cs` (novo)

## Responsabilidades permitidas

- Criar migração EF Core usando `dotnet ef migrations add`
- Verificar que o ficheiro gerado cria as tabelas esperadas com prefixo `knw_`
- Verificar que índices, chaves primárias e foreign keys estão corretos
- Verificar que a tabela de outbox (`knw_outbox_messages`) é criada

## Responsabilidades proibidas

- Alterar o schema ou mapeamento das entidades
- Adicionar seed data
- Executar a migração contra base de dados de produção
- Alterar migrações de outros módulos

## Critérios de aceite

1. Ficheiro de migração criado em `Persistence/Migrations/`
2. A migração cria as tabelas: `knw_documents`, `knw_operational_notes`, `knw_relations`, `knw_outbox_messages`
3. Migração compila sem erros
4. `dotnet ef migrations list` mostra a migração
5. `dotnet ef database update` aplica a migração com sucesso em PostgreSQL local
6. Módulo e solução compilam

## Validações obrigatórias

- `dotnet build src/modules/knowledge/` — sem erros
- `dotnet build NexTraceOne.sln` — sem erros
- Verificar que o ficheiro de migração contém `CreateTable` para as 4 tabelas esperadas
- Verificar que colunas, tipos e constraints refletem as entity configurations

## Riscos e cuidados

- Verificar que o connection string de desenvolvimento está disponível para gerar a migração
- Se o DbContext usar `HasDefaultSchema`, confirmar que o schema está correto
- Verificar que o outbox pattern segue o mesmo modelo dos outros módulos (ex: notificações, catalog)
- A migração não deve conter dependências de tabelas de outros módulos (isolamento de bounded context)

## Dependências

- Nenhuma dependência de outros prompts
- Pode ser executado em paralelo com P-00-08 e P-00-09

## Próximos prompts sugeridos

- P-00-08 (Migração do módulo Integrations)
- P-00-09 (Migração do módulo ProductAnalytics)
