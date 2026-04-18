# Knowledge Module — NexTraceOne

> **Bounded Context:** Knowledge
> **Responsabilidade:** Knowledge Hub centralizado — documentação técnica e operacional, notas operacionais, relações entre entidades de conhecimento e Knowledge Graph da plataforma.

---

## Propósito

O módulo Knowledge é o **repositório de conhecimento governado** do NexTraceOne. Resolve um problema real e recorrente em ambientes enterprise: o conhecimento operacional e técnico fica disperso em wikis, emails, Confluences, Notion e documentos avulsos, sem ligação ao contexto real dos serviços, contratos e mudanças.

O Knowledge Hub centraliza este conhecimento, liga-o ao contexto operacional (serviços, contratos, incidentes, mudanças) e torna-o consultável, versionável e auditável. O módulo serve também como base de conhecimento para o AI Assistant — documentos revistos e aprovados alimentam o contexto do assistente com informação governada.

## Bounded Context

| Aspecto | Detalhe |
|---------|---------|
| **Assemblies** | `NexTraceOne.Knowledge.Domain`, `.Application`, `.Infrastructure`, `.API`, `.Contracts` |
| **DbContext** | `KnowledgeDbContext` — banco `KnowledgeDatabase` |
| **Outbox table** | `knowledge_outbox_messages` |
| **Base URL de API** | `/api/v1/knowledge` |

---

## Entidades Principais

### KnowledgeDocument

A unidade central de conhecimento. Pode representar documentação técnica, guias operacionais, runbooks, post-mortems, procedimentos ou referências arquitecturais.

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `Id` | `KnowledgeDocumentId` | Identificador fortemente tipado |
| `TenantId` | `Guid` | Isolamento multi-tenant |
| `Title` | `string` | Título do documento |
| `Slug` | `string` | URL amigável gerado a partir do título |
| `Content` | `string` | Conteúdo em Markdown |
| `Summary` | `string?` | Resumo curto para listagens e previews |
| `Category` | `DocumentCategory` | Categoria: `Runbook`, `PostMortem`, `Architecture`, `Guide`, `Reference`, `Procedure` |
| `Status` | `DocumentStatus` | `Draft`, `InReview`, `Approved`, `Archived`, `Deprecated` |
| `AuthorId` | `Guid` | ID do autor |
| `OwnerServiceId` | `Guid?` | Serviço owner do documento (ligação cross-module via ID) |
| `Tags` | `IReadOnlyList<string>` | Tags para categorização e search |
| `Version` | `int` | Versão do documento (incremental) |
| `ReviewedAt` | `DateTimeOffset?` | Quando foi revisto pela última vez |
| `ExpiresAt` | `DateTimeOffset?` | Data de expiração (para conteúdo com validade) |

O ciclo de vida de um documento segue: `Draft → InReview → Approved → (Deprecated | Archived)`.

### OperationalNote

Nota operacional associada a um contexto específico (serviço, incidente, mudança, ambiente). É mais leve que um documento completo e captura observações, alertas e insights operacionais de curto prazo.

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `Id` | `OperationalNoteId` | Identificador fortemente tipado |
| `TenantId` | `Guid` | Isolamento multi-tenant |
| `Content` | `string` | Conteúdo da nota (Markdown suportado) |
| `Type` | `OperationalNoteType` | `Observation`, `Warning`, `Incident`, `Maintenance`, `General` |
| `Severity` | `NoteSeverity` | `Info`, `Warning`, `Critical` |
| `LinkedEntityType` | `KnowledgeSourceEntityType` | Tipo de entidade ligada |
| `LinkedEntityId` | `Guid` | ID da entidade ligada (serviço, incidente, etc.) |
| `EnvironmentId` | `Guid?` | Ambiente ao qual a nota se aplica |
| `AuthorId` | `Guid` | Autor da nota |
| `CreatedAt` | `DateTimeOffset` | Quando foi criada |
| `ExpiresAt` | `DateTimeOffset?` | Quando expira (notas temporárias) |

### KnowledgeRelation

Relação semântica entre dois artefactos de conhecimento ou entre um documento e uma entidade do domínio. Alimenta o Knowledge Graph.

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `Id` | `KnowledgeRelationId` | Identificador fortemente tipado |
| `SourceEntityType` | `KnowledgeSourceEntityType` | Tipo da entidade de origem |
| `SourceEntityId` | `Guid` | ID da entidade de origem |
| `TargetEntityType` | `KnowledgeSourceEntityType` | Tipo da entidade de destino |
| `TargetEntityId` | `Guid` | ID da entidade de destino |
| `RelationType` | `RelationType` | `Documents`, `References`, `Supersedes`, `RelatedTo`, `CausedBy`, `MitigatedBy` |

### KnowledgeGraphSnapshot

Snapshot periódico do Knowledge Graph — usado para visualização e queries de grafo sem computação em tempo real.

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `Id` | `KnowledgeGraphSnapshotId` | Identificador fortemente tipado |
| `Status` | `KnowledgeGraphSnapshotStatus` | `Building`, `Ready`, `Stale`, `Failed` |
| `NodesCount` | `int` | Número de nós no grafo |
| `EdgesCount` | `int` | Número de arestas |
| `GeneratedAt` | `DateTimeOffset` | Quando foi gerado |

---

## Features Disponíveis

| Feature (Application) | Tipo | Descrição |
|-----------------------|------|-----------|
| `CreateKnowledgeDocument` | Command | Cria novo documento (Draft) |
| `UpdateKnowledgeDocument` | Command | Atualiza conteúdo ou metadados |
| `ValidateDocumentReviewGate` | Command | Promove documento para Approved após review |
| `CreateOperationalNote` | Command | Cria nota operacional associada a um contexto |
| `UpdateOperationalNote` | Command | Atualiza nota existente |
| `CreateKnowledgeRelation` | Command | Adiciona relação semântica entre artefactos |
| `BuildKnowledgeGraphSnapshot` | Command | Constrói novo snapshot do Knowledge Graph |
| `ListKnowledgeDocuments` | Query | Lista documentos com filtros |
| `GetKnowledgeDocumentById` | Query | Detalhe de um documento |
| `ListOperationalNotes` | Query | Lista notas operacionais |
| `GetKnowledgeRelationsBySource` | Query | Relações a partir de uma entidade |
| `GetKnowledgeByRelationTarget` | Query | Documentos relacionados com uma entidade alvo |
| `GetKnowledgeGraphSnapshot` | Query | Último snapshot do Knowledge Graph |
| `GetKnowledgeGraphOverview` | Query | Visão geral do grafo (contagens, estatísticas) |
| `ListKnowledgeGraphSnapshots` | Query | Histórico de snapshots |

---

## Serviços de Infraestrutura

### KnowledgeSearchProvider

Implementa busca full-text cross-module (usa PostgreSQL FTS no MVP1). Expõe `IKnowledgeSearchProvider` para ser consumido por outros módulos (ex.: AI Knowledge Source).

### RunbookKnowledgeLinkingService

Liga runbooks de incidentes a documentos de conhecimento relevantes via análise de tags e relações. Consumido pelo módulo OperationalIntelligence.

### IKnowledgeModule (Cross-Module Interface)

Interface pública consumida pelo módulo Governance e AI para métricas de conhecimento (ex.: cobertura de documentação por serviço):

```csharp
public interface IKnowledgeModule
{
    Task<int> GetDocumentCountForServiceAsync(Guid serviceId, CancellationToken ct);
    Task<bool> HasApprovedRunbookAsync(Guid serviceId, CancellationToken ct);
}
```

---

## Frontend

O módulo Knowledge tem as seguintes páginas em `src/frontend/src/features/knowledge/`:

| Página | Rota | Descrição |
|--------|------|-----------|
| `KnowledgeHubPage` | `/knowledge` | Hub principal com search e navegação |
| `KnowledgeDocumentPage` | `/knowledge/documents/:id` | Visualização de documento com Markdown |
| `OperationalNotesPage` | `/knowledge/notes` | Lista e gestão de notas operacionais |
| `AutoDocumentationPage` | `/knowledge/auto-docs` | Documentação gerada automaticamente |
| `KnowledgeGraphPage` | `/knowledge/graph` | Visualização interativa do Knowledge Graph |

---

## Ligação com AI

Os documentos com status `Approved` são indexados como **Knowledge Sources** pelo módulo AIKnowledge. O AI Assistant pode consultar estes documentos via RAG (Retrieval-Augmented Generation) para responder a questões operacionais com base em conhecimento governado do tenant.

A governança garante que apenas conteúdo aprovado e não expirado alimenta o contexto do assistente.

---

## Endpoints

| Método | Rota | Descrição |
|--------|------|-----------|
| `GET` | `/knowledge/documents` | Lista documentos com filtros (categoria, status, tags) |
| `POST` | `/knowledge/documents` | Cria novo documento |
| `GET` | `/knowledge/documents/{id}` | Detalhe de um documento |
| `PUT` | `/knowledge/documents/{id}` | Atualiza documento |
| `POST` | `/knowledge/documents/{id}/approve` | Promove para Approved |
| `GET` | `/knowledge/notes` | Lista notas operacionais |
| `POST` | `/knowledge/notes` | Cria nota operacional |
| `PUT` | `/knowledge/notes/{id}` | Atualiza nota |
| `GET` | `/knowledge/relations` | Lista relações por entidade de origem |
| `POST` | `/knowledge/relations` | Cria relação semântica |
| `GET` | `/knowledge/graph/snapshot` | Último snapshot do Knowledge Graph |
| `POST` | `/knowledge/graph/build` | Dispara construção de novo snapshot |

---

## Registro no DI

```csharp
// Program.cs do ApiHost
builder.Services.AddKnowledgeInfrastructure(builder.Configuration);

// appsettings.json
"ConnectionStrings": {
  "KnowledgeDatabase": "Host=localhost;Database=nextraceone_knowledge;..."
}
```

---

## Dependências Cross-Module

| Módulo | Direção | Natureza |
|--------|---------|---------|
| AIKnowledge | Knowledge → AI | Documentos aprovados como knowledge sources |
| OperationalIntelligence | Knowledge ← Ops | Runbooks linkados a incidentes |
| Governance | Governance ← Knowledge | Métricas de cobertura de documentação via `IKnowledgeModule` |
| AuditCompliance | Knowledge → Audit | Eventos de criação, aprovação e expiração de documentos |

---

*Última atualização: Março 2026.*
