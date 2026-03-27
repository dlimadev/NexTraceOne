# P10.3 — Knowledge Relations + Operational Notes Report

> **Status:** COMPLETED  
> **Date:** 2026-03-27  
> **Phase:** P10.3 — Ligar Knowledge Hub a serviços, contratos, mudanças e incidentes

---

## Objetivo

Fechar a utilidade real do Knowledge Hub no backend, tornando `KnowledgeRelation` e `OperationalNote` mecanismos reais de ligação entre conhecimento e entidades centrais do produto:

- serviços (Catalog / Source of Truth)
- contratos (Contracts)
- mudanças (Change Governance)
- incidentes (Operational Intelligence)

Sem ampliar escopo para vector DB, embeddings, graph traversal avançado ou UI completa.

---

## Estado antigo do domínio Knowledge

Antes desta alteração, o módulo `src/modules/knowledge/` já existia (P10.1) e tinha search básica/cross-module (P10.2), porém com lacunas de uso real:

- `KnowledgeDocument`, `OperationalNote` e `KnowledgeRelation` existiam estruturalmente.
- Não havia handlers/queries/endpoints mínimos para criar relações e navegar por alvo/origem.
- `KnowledgeRelation.SourceEntityType` era string livre.
- `OperationalNote` não tinha metadados funcionais explícitos de tipo/origem.
- Search de Knowledge não refletia contexto de relações.

---

## Modelo de KnowledgeRelation adotado (mínimo e explícito)

### Decisões de modelo

1. `SourceEntityType` passou de string para enum forte:
   - `KnowledgeSourceEntityType` (`KnowledgeDocument`, `OperationalNote`)
2. Relação ganhou `Context` opcional (texto curto rastreável).
3. Repositório ganhou query por alvo:
   - `ListByTargetAsync(RelationType targetType, Guid targetEntityId)`
4. Mantido desacoplamento entre bounded contexts:
   - apenas `Guid` + `RelationType` para alvo.

### Tipos alvo suportados

`RelationType` já cobre explicitamente:

- `Service`
- `Contract`
- `Change`
- `Incident`

(
`KnowledgeDocument`, `Runbook`, `Other` permanecem válidos para evolução incremental.)

### Ajustes de persistência

- `KnowledgeRelationConfiguration`:
  - check constraint para `SourceEntityType`
  - mapeamento enum string de `SourceEntityType`
  - coluna `Context` (max 100)
  - índice composto `{TargetType, TargetEntityId}`

---

## OperationalNote com uso real contextual

### Novos metadados úteis

`OperationalNote` agora inclui:

- `NoteType` (`OperationalNoteType`): `Observation`, `Mitigation`, `Decision`, `Hypothesis`, `FollowUp`
- `Origin` (ex.: `IncidentTimeline`, `Manual`, `PostChangeValidation`)

### Capacidade de associação contextual

A nota continua com:

- `ContextEntityId`
- `ContextType`

E passa a ter métodos explícitos para manutenção contextual:

- `UpdateType(...)`
- `UpdateContext(...)`

### Ajustes de persistência

`OperationalNoteConfiguration` recebeu:

- check constraint para `NoteType`
- coluna `Origin` (required, max 100)
- índice por `NoteType`, `Origin` e `{ContextType, ContextEntityId}`

---

## Ligações implementadas com serviços, contratos, mudanças e incidentes

As ligações são suportadas por `CreateKnowledgeRelation` + `RelationType`:

- **Serviços:** `RelationType.Service`
- **Contratos:** `RelationType.Contract`
- **Mudanças/Releases:** `RelationType.Change`
- **Incidentes:** `RelationType.Incident`

### Endpoints mínimos adicionados

Em `KnowledgeEndpointModule`:

- `POST /api/v1/knowledge/documents` — cria `KnowledgeDocument`
- `POST /api/v1/knowledge/operational-notes` — cria `OperationalNote`
- `POST /api/v1/knowledge/relations` — cria `KnowledgeRelation`
- `GET /api/v1/knowledge/relations/by-target/{targetType}/{targetEntityId}` — consulta conhecimento ligado ao alvo
- `GET /api/v1/knowledge/relations/by-source/{sourceEntityId}` — consulta relações a partir da origem

### Handlers/queries mínimos criados

- `CreateKnowledgeDocument`
- `CreateOperationalNote`
- `CreateKnowledgeRelation`
- `GetKnowledgeByRelationTarget`
- `GetKnowledgeRelationsBySource`

---

## Impacto mínimo em search (P10.2)

Sem reimplementar search:

- `KnowledgeSearchProvider` passou a enriquecer `subtitle` com contexto de relação (`linked:...`) quando houver relações para a origem.
- Isso permite resultados de Knowledge/Notes mais contextuais para Command Palette e search de Knowledge.

---

## Ficheiros alterados/criados

### Alterados

- `src/modules/knowledge/NexTraceOne.Knowledge.Domain/Entities/KnowledgeRelation.cs`
- `src/modules/knowledge/NexTraceOne.Knowledge.Domain/Entities/OperationalNote.cs`
- `src/modules/knowledge/NexTraceOne.Knowledge.Application/Abstractions/IKnowledgeRelationRepository.cs`
- `src/modules/knowledge/NexTraceOne.Knowledge.Infrastructure/Persistence/Repositories/KnowledgeRelationRepository.cs`
- `src/modules/knowledge/NexTraceOne.Knowledge.Infrastructure/Persistence/Configurations/KnowledgeRelationConfiguration.cs`
- `src/modules/knowledge/NexTraceOne.Knowledge.Infrastructure/Persistence/Configurations/OperationalNoteConfiguration.cs`
- `src/modules/knowledge/NexTraceOne.Knowledge.Infrastructure/Search/KnowledgeSearchProvider.cs`
- `src/modules/knowledge/NexTraceOne.Knowledge.API/Endpoints/KnowledgeEndpointModule.cs`
- `tests/modules/knowledge/NexTraceOne.Knowledge.Tests/Domain/KnowledgeRelationTests.cs`
- `tests/modules/knowledge/NexTraceOne.Knowledge.Tests/Domain/OperationalNoteTests.cs`

### Criados

- `src/modules/knowledge/NexTraceOne.Knowledge.Domain/Enums/KnowledgeSourceEntityType.cs`
- `src/modules/knowledge/NexTraceOne.Knowledge.Domain/Enums/OperationalNoteType.cs`
- `src/modules/knowledge/NexTraceOne.Knowledge.Application/Features/CreateKnowledgeDocument/CreateKnowledgeDocument.cs`
- `src/modules/knowledge/NexTraceOne.Knowledge.Application/Features/CreateOperationalNote/CreateOperationalNote.cs`
- `src/modules/knowledge/NexTraceOne.Knowledge.Application/Features/CreateKnowledgeRelation/CreateKnowledgeRelation.cs`
- `src/modules/knowledge/NexTraceOne.Knowledge.Application/Features/GetKnowledgeByRelationTarget/GetKnowledgeByRelationTarget.cs`
- `src/modules/knowledge/NexTraceOne.Knowledge.Application/Features/GetKnowledgeRelationsBySource/GetKnowledgeRelationsBySource.cs`

---

## Validação funcional e compilação

Executado:

- `dotnet build src/modules/knowledge/NexTraceOne.Knowledge.API/NexTraceOne.Knowledge.API.csproj --configuration Release --no-restore` ✅
- `dotnet test tests/modules/knowledge/NexTraceOne.Knowledge.Tests/NexTraceOne.Knowledge.Tests.csproj --configuration Release --no-restore` ✅ (22 passed)
- `dotnet build src/platform/NexTraceOne.ApiHost/NexTraceOne.ApiHost.csproj --configuration Release --no-restore` ✅

Observação: warnings pré-existentes do repositório permanecem; sem erros introduzidos pela alteração.

---

## Conclusão

P10.3 foi implementado com mudança mínima e rastreável no backend:

- `KnowledgeDocument` e `OperationalNote` deixaram de estar isolados.
- `KnowledgeRelation` passou a operar como ligação real cross-module.
- Há fluxo mínimo completo de criação de documento/nota, criação de relação e consulta contextual por alvo/origem.
- Search existente foi enriquecida com contexto de relação, sem desvio de escopo.
