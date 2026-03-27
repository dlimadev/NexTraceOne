# P-02-01 — Completar CRUD do Knowledge e implementar Full-Text Search com PostgreSQL

## 1. Título

Completar operações CRUD do módulo Knowledge (update/delete para documentos, notas e relações) e implementar pesquisa Full-Text Search com PostgreSQL.

## 2. Modo de operação

**Implementation**

## 3. Objetivo

O módulo Knowledge atualmente só possui operações de criação (Create) e consulta por relação (GetByRelationTarget, GetRelationsBySource). Faltam as operações de atualização (Update) e remoção (Delete) para KnowledgeDocument, OperationalNote e KnowledgeRelation. Além disso, o KnowledgeSearchProvider precisa de implementação real com PostgreSQL FTS em vez de pesquisa simplificada em memória.

## 4. Problema atual

- O KnowledgeEndpointModule em `src/modules/knowledge/NexTraceOne.Knowledge.API/Endpoints/KnowledgeEndpointModule.cs` expõe apenas endpoints de criação e pesquisa básica.
- Os repositórios (`KnowledgeDocumentRepository.cs`, `OperationalNoteRepository.cs`, `KnowledgeRelationRepository.cs`) não possuem métodos UpdateAsync nem DeleteAsync.
- As interfaces de abstração (`IKnowledgeDocumentRepository.cs`, `IOperationalNoteRepository.cs`, `IKnowledgeRelationRepository.cs`) não declaram contratos de update/delete.
- O `KnowledgeSearchProvider.cs` em Infrastructure/Search faz pesquisa simplificada, sem usar `ts_vector` ou `to_tsquery` do PostgreSQL.
- Sem CRUD completo, o Knowledge Hub não pode ser usado como Source of Truth editável para documentação operacional.

## 5. Escopo permitido

- `src/modules/knowledge/NexTraceOne.Knowledge.Application/Features/` — criar pastas UpdateKnowledgeDocument, DeleteKnowledgeDocument, UpdateOperationalNote, DeleteOperationalNote, DeleteKnowledgeRelation
- `src/modules/knowledge/NexTraceOne.Knowledge.Application/Abstractions/` — adicionar métodos UpdateAsync, DeleteAsync nas interfaces
- `src/modules/knowledge/NexTraceOne.Knowledge.Infrastructure/Persistence/Repositories/` — implementar métodos nos 3 repositórios
- `src/modules/knowledge/NexTraceOne.Knowledge.Infrastructure/Search/KnowledgeSearchProvider.cs` — implementar FTS com PostgreSQL
- `src/modules/knowledge/NexTraceOne.Knowledge.API/Endpoints/KnowledgeEndpointModule.cs` — registar novos endpoints PUT e DELETE
- `src/modules/knowledge/NexTraceOne.Knowledge.Infrastructure/Persistence/KnowledgeDbContext.cs` — se necessário para índices FTS

## 6. Escopo proibido

- Não alterar entidades de domínio (`KnowledgeDocument.cs`, `OperationalNote.cs`, `KnowledgeRelation.cs`) sem necessidade comprovada.
- Não alterar módulos fora de `src/modules/knowledge/`.
- Não criar dependências de pesquisa externa (Elasticsearch, OpenSearch). Usar exclusivamente PostgreSQL FTS.
- Não remover endpoints existentes de criação ou pesquisa.
- Não alterar migrações já aplicadas.

## 7. Ficheiros principais candidatos a alteração

1. `src/modules/knowledge/NexTraceOne.Knowledge.Application/Abstractions/IKnowledgeDocumentRepository.cs`
2. `src/modules/knowledge/NexTraceOne.Knowledge.Application/Abstractions/IOperationalNoteRepository.cs`
3. `src/modules/knowledge/NexTraceOne.Knowledge.Application/Abstractions/IKnowledgeRelationRepository.cs`
4. `src/modules/knowledge/NexTraceOne.Knowledge.Infrastructure/Persistence/Repositories/KnowledgeDocumentRepository.cs`
5. `src/modules/knowledge/NexTraceOne.Knowledge.Infrastructure/Persistence/Repositories/OperationalNoteRepository.cs`
6. `src/modules/knowledge/NexTraceOne.Knowledge.Infrastructure/Persistence/Repositories/KnowledgeRelationRepository.cs`
7. `src/modules/knowledge/NexTraceOne.Knowledge.Infrastructure/Search/KnowledgeSearchProvider.cs`
8. `src/modules/knowledge/NexTraceOne.Knowledge.API/Endpoints/KnowledgeEndpointModule.cs`
9. `src/modules/knowledge/NexTraceOne.Knowledge.Application/Features/UpdateKnowledgeDocument/UpdateKnowledgeDocument.cs` (novo)
10. `src/modules/knowledge/NexTraceOne.Knowledge.Application/Features/DeleteKnowledgeDocument/DeleteKnowledgeDocument.cs` (novo)

## 8. Responsabilidades permitidas

- Criar handlers MediatR para Update e Delete de cada entidade do Knowledge.
- Adicionar métodos de repositório com validação de existência.
- Implementar PostgreSQL FTS usando `to_tsvector('portuguese', ...)` e `to_tsquery`.
- Registar endpoints REST (PUT, DELETE) no KnowledgeEndpointModule.
- Adicionar FluentValidation para os novos commands.
- Usar CancellationToken em todas as operações async.

## 9. Responsabilidades proibidas

- Não implementar soft-delete sem decisão arquitetural explícita; preferir hard-delete com auditoria via evento.
- Não adicionar lógica de autorização granular neste prompt (será tratado em prompt de segurança futuro).
- Não criar nova migração EF Core — o schema deve já suportar as operações (se não suportar, documentar gap).

## 10. Critérios de aceite

- [ ] Endpoints PUT /api/v1/knowledge/documents/{id} e DELETE /api/v1/knowledge/documents/{id} funcionam.
- [ ] Endpoints PUT /api/v1/knowledge/notes/{id} e DELETE /api/v1/knowledge/notes/{id} funcionam.
- [ ] Endpoint DELETE /api/v1/knowledge/relations/{id} funciona.
- [ ] GET /api/v1/knowledge/search?q=termo retorna resultados usando PostgreSQL FTS real.
- [ ] Pesquisa funciona com termos parciais e acentuação portuguesa.
- [ ] Todos os handlers usam CancellationToken.
- [ ] Projeto compila sem erros.

## 11. Validações obrigatórias

- Compilação completa do módulo Knowledge (todos os 5 projetos).
- Compilação completa da solution NexTraceOne.sln.
- Verificar que endpoints existentes (create, search, status) continuam a funcionar.
- Verificar que os handlers devolvem Result<T> com erros claros quando entidade não existe.

## 12. Riscos e cuidados

- O KnowledgeDbContext pode não ter índice GIN para FTS — pode ser necessário nova migração.
- A pesquisa FTS com `to_tsvector('portuguese', ...)` requer extensão `unaccent` se se quiser normalizar acentos.
- Delete de KnowledgeRelation pode afetar integridade referencial se houver relações em cascata.
- Verificar se KnowledgeDocument tem campo de conteúdo suficientemente grande para FTS indexing.

## 13. Dependências

- **P-00-07** — Migração do módulo Knowledge deve estar aplicada (KnowledgeDbContext com tabelas criadas).
- O schema das entidades KnowledgeDocument, OperationalNote e KnowledgeRelation já deve existir em base de dados.

## 14. Próximos prompts sugeridos

- **P-02-04** — AI grounding cross-module (o Knowledge com FTS real será fonte de dados para o AI retrieval).
- **P-03-XX** — Frontend de edição de documentos e notas no Knowledge Hub (depende deste CRUD estar completo).
- **P-XX-XX** — Auditoria de alterações no Knowledge (registar quem editou/removeu, quando e porquê).
