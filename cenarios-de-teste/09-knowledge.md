# Cenários de Teste — Módulo: Knowledge

> **Versão:** 1.0  
> **Data:** 2026-05-18  
> **Responsável:** QA Engineering  
> **Módulo:** Knowledge  
> **Total de casos:** 28

---

## Sumário

| ID | Título | Prioridade |
|----|--------|-----------|
| TC-KNW-001 | Criar documento de conhecimento com sucesso | Crítica |
| TC-KNW-002 | Atualizar documento de conhecimento existente | Alta |
| TC-KNW-003 | Obter documento de conhecimento por ID | Alta |
| TC-KNW-004 | Listar documentos de conhecimento com filtro | Alta |
| TC-KNW-005 | Excluir prompt salvo (DeleteSavedPrompt) | Média |
| TC-KNW-006 | Criar relação de conhecimento entre documentos | Alta |
| TC-KNW-007 | Obter documentos por alvo de relação | Alta |
| TC-KNW-008 | Obter relações por fonte (GetKnowledgeRelationsBySource) | Alta |
| TC-KNW-009 | Construir snapshot do grafo de conhecimento | Crítica |
| TC-KNW-010 | Obter visão geral do grafo de conhecimento | Alta |
| TC-KNW-011 | Obter snapshot específico do grafo | Alta |
| TC-KNW-012 | Listar snapshots do grafo de conhecimento | Média |
| TC-KNW-013 | Criar runbook operacional | Alta |
| TC-KNW-014 | Atualizar runbook existente | Alta |
| TC-KNW-015 | Obter detalhe do runbook | Alta |
| TC-KNW-016 | Listar runbooks por categoria | Média |
| TC-KNW-017 | Executar passo de runbook (ExecuteRunbookStep) | Crítica |
| TC-KNW-018 | Criar notebook de análise | Alta |
| TC-KNW-019 | Atualizar notebook com nova célula | Alta |
| TC-KNW-020 | Exportar dados de analytics (ExportAnalyticsData) | Média |
| TC-KNW-021 | Obter relatório de frescor de documentação | Alta |
| TC-KNW-022 | Pontuar frescor de documento (ScoreDocumentFreshness) | Alta |
| TC-KNW-023 | Validar gate de revisão de documento | Alta |
| TC-KNW-024 | Adicionar observação a um ativo | Alta |
| TC-KNW-025 | Registrar métricas de observação | Média |
| TC-KNW-026 | Criar e gerenciar visualização salva do usuário | Alta |
| TC-KNW-027 | Adicionar e remover bookmark | Alta |
| TC-KNW-028 | Listar bookmarks do usuário | Média |

---

### TC-KNW-001 — Criar documento de conhecimento com sucesso

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | CreateKnowledgeDocument |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Usuário autenticado com role `knowledge:write`
- Tenant com capability `knowledge_base`

**Passos:**
1. Autenticar com JWT válido
2. Enviar `POST /api/knowledge/documents` com body:
   ```json
   {
     "title": "Guia de Resposta a Incidentes",
     "content": "## Passo 1\nAcione o on-call...",
     "type": "runbook",
     "tags": ["incidents", "on-call"],
     "serviceIds": ["payment-svc"]
   }
   ```
3. Verificar ID gerado

**Resultado Esperado:**
- HTTP 201 Created
- `id`, `title`, `type`, `createdAt`, `createdBy` na resposta
- Documento disponível via `GetKnowledgeDocumentById`

**Critério de Aceite:** `result.IsSuccess == true` / HTTP 201 com ID persistido

---

### TC-KNW-002 — Atualizar documento de conhecimento existente

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | UpdateKnowledgeDocument |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Documento criado com ID conhecido
- Usuário proprietário do documento

**Passos:**
1. Autenticar com JWT do criador
2. Enviar `PUT /api/knowledge/documents/{id}` com conteúdo atualizado e nova tag
3. Verificar `updatedAt` e versionamento

**Resultado Esperado:**
- HTTP 200 OK
- Conteúdo e tags atualizados
- `updatedAt` atualizado; `version` incrementado

**Critério de Aceite:** Atualização persistida; versionamento funcional

---

### TC-KNW-003 — Obter documento de conhecimento por ID

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | GetKnowledgeDocumentById |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Documento criado com ID conhecido

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/knowledge/documents/{id}`

**Resultado Esperado:**
- HTTP 200 OK
- Campos: `id`, `title`, `content`, `type`, `tags`, `serviceIds`, `createdAt`, `updatedAt`, `version`

**Critério de Aceite:** Todos os campos retornados; isolamento por tenant via RLS

---

### TC-KNW-004 — Listar documentos de conhecimento com filtro

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | ListKnowledgeDocuments |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Documentos de tipos `runbook`, `guide`, `architecture` criados

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/knowledge/documents?type=runbook&tag=on-call`
3. Enviar `GET /api/knowledge/documents?serviceId=payment-svc`

**Resultado Esperado:**
- Primeira chamada: apenas documentos do tipo `runbook` com tag `on-call`
- Segunda chamada: apenas documentos vinculados a `payment-svc`

**Critério de Aceite:** Filtros funcionais; isolamento por tenant correto

---

### TC-KNW-005 — Excluir prompt salvo (DeleteSavedPrompt)

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | DeleteSavedPrompt |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Prompt salvo com ID conhecido pertencente ao usuário atual

**Passos:**
1. Autenticar com JWT do proprietário
2. Enviar `DELETE /api/knowledge/saved-prompts/{id}`
3. Tentar obter o prompt excluído

**Resultado Esperado:**
- HTTP 204 No Content
- `GET /api/knowledge/saved-prompts/{id}` retorna HTTP 404

**Critério de Aceite:** Soft-delete aplicado; filtro global oculta o registro

---

### TC-KNW-006 — Criar relação de conhecimento entre documentos

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | CreateKnowledgeRelation |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Dois documentos criados: `doc-A` (runbook) e `doc-B` (architecture guide)

**Passos:**
1. Autenticar com JWT com permissão de escrita
2. Enviar `POST /api/knowledge/relations` com body:
   ```json
   {
     "sourceId": "{doc-A-id}",
     "targetId": "{doc-B-id}",
     "relationType": "references"
   }
   ```

**Resultado Esperado:**
- HTTP 201 Created
- Relação criada com `sourceId`, `targetId`, `relationType`, `createdAt`

**Critério de Aceite:** Relação bidirecional acessível via `GetKnowledgeRelationsBySource`

---

### TC-KNW-007 — Obter documentos por alvo de relação

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | GetKnowledgeByRelationTarget |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- `doc-A` referencia `doc-B` e `doc-C` via relações `references`

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/knowledge/relations/by-target/{doc-B-id}?relationType=references`

**Resultado Esperado:**
- Lista contendo `doc-A` como fonte da relação com `doc-B`
- Metadados do documento fonte incluídos

**Critério de Aceite:** Documentos relacionados ao alvo retornados corretamente

---

### TC-KNW-008 — Obter relações por fonte (GetKnowledgeRelationsBySource)

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | GetKnowledgeRelationsBySource |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- `doc-A` possui relações com `doc-B` (references) e `doc-C` (supersedes)

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/knowledge/relations/by-source/{doc-A-id}`

**Resultado Esperado:**
- Lista de 2 relações: uma do tipo `references` e uma do tipo `supersedes`
- Cada relação contém `targetId`, `relationType`, `targetTitle`

**Critério de Aceite:** Todas as relações originadas de `doc-A` retornadas

---

### TC-KNW-009 — Construir snapshot do grafo de conhecimento

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | BuildKnowledgeGraphSnapshot |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Base de conhecimento com 20+ documentos e 15+ relações

**Passos:**
1. Autenticar com JWT de admin
2. Enviar `POST /api/knowledge/graph/snapshots/build`
3. Aguardar conclusão assíncrona
4. Verificar snapshot criado

**Resultado Esperado:**
- HTTP 202 Accepted
- `snapshotId` retornado
- Após conclusão: grafo com todos os nós (documentos) e arestas (relações) calculado

**Critério de Aceite:** Snapshot gerado; disponível via `GetKnowledgeGraphSnapshot`

---

### TC-KNW-010 — Obter visão geral do grafo de conhecimento

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | GetKnowledgeGraphOverview |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Snapshot gerado com dados disponíveis

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/knowledge/graph/overview`

**Resultado Esperado:**
- `totalNodes`, `totalEdges`, `documentTypes`, `mostConnectedNodes`, `isolatedNodes`
- Estatísticas de conectividade do grafo

**Critério de Aceite:** Visão geral com métricas de grafo precisas

---

### TC-KNW-011 — Obter snapshot específico do grafo

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | GetKnowledgeGraphSnapshot |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Múltiplos snapshots gerados; o mais recente com ID conhecido

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/knowledge/graph/snapshots/{snapshotId}`

**Resultado Esperado:**
- Dados completos do snapshot: `nodes`, `edges`, `generatedAt`, `documentCount`
- Estrutura de grafo serializável (JSON com nodes e edges)

**Critério de Aceite:** Snapshot retornado com estrutura de grafo completa

---

### TC-KNW-012 — Listar snapshots do grafo de conhecimento

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | ListKnowledgeGraphSnapshots |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Pelo menos 3 snapshots gerados em datas distintas

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/knowledge/graph/snapshots`

**Resultado Esperado:**
- Lista ordenada por `generatedAt` decrescente
- Cada item: `snapshotId`, `generatedAt`, `nodeCount`, `edgeCount`

**Critério de Aceite:** Listagem com metadados de cada snapshot; isolado por tenant

---

### TC-KNW-013 — Criar runbook operacional

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | CreateRunbook |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Usuário com role `knowledge:runbooks`

**Passos:**
1. Autenticar com JWT adequado
2. Enviar `POST /api/knowledge/runbooks` com body:
   ```json
   {
     "title": "Rollback de Deploy",
     "description": "Procedimento para reverter deploy problemático",
     "category": "deployment",
     "steps": [
       { "order": 1, "title": "Identificar versão estável", "instructions": "...", "automatable": false },
       { "order": 2, "title": "Executar rollback", "instructions": "kubectl rollout undo ...", "automatable": true }
     ]
   }
   ```

**Resultado Esperado:**
- HTTP 201 Created
- Runbook com ID, título, passos ordenados e status `Draft`

**Critério de Aceite:** Runbook criado com passos na ordem correta

---

### TC-KNW-014 — Atualizar runbook existente

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | UpdateRunbook |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Runbook `Rollback de Deploy` com 2 passos

**Passos:**
1. Autenticar com JWT do proprietário
2. Enviar `PUT /api/knowledge/runbooks/{id}` adicionando passo 3: "Notificar equipe"
3. Verificar ordenação dos passos

**Resultado Esperado:**
- HTTP 200 OK
- 3 passos na ordem correta
- `updatedAt` atualizado

**Critério de Aceite:** Passos atualizados na ordem correta; histórico de versão mantido

---

### TC-KNW-015 — Obter detalhe do runbook

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | GetRunbookDetail |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Runbook criado com passos e metadados

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/knowledge/runbooks/{id}`

**Resultado Esperado:**
- Runbook completo com todos os passos, `lastExecutedAt`, `executionCount`

**Critério de Aceite:** Detalhe completo retornado; isolamento por tenant correto

---

### TC-KNW-016 — Listar runbooks por categoria

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | ListRunbooks |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Runbooks das categorias `deployment`, `incident`, `maintenance` criados

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/knowledge/runbooks?category=incident`

**Resultado Esperado:**
- Apenas runbooks da categoria `incident` retornados

**Critério de Aceite:** Filtro por categoria funcional

---

### TC-KNW-017 — Executar passo de runbook (ExecuteRunbookStep)

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | ExecuteRunbookStep |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Runbook com passo `automatable: true` usando comando `kubectl rollout undo deployment/payment-svc`
- Contexto de execução com permissão de kubectl disponível

**Passos:**
1. Autenticar com JWT de operações
2. Enviar `POST /api/knowledge/runbooks/{runbookId}/steps/{stepId}/execute` com body `{ "context": { "namespace": "production", "cluster": "eks-main" } }`
3. Verificar resultado

**Resultado Esperado:**
- HTTP 202 Accepted
- Execução registrada com `executionId`
- Status: `Executing` → `Completed` (ou `Failed` com log de erro)
- `executionCount` do runbook incrementado

**Critério de Aceite:** Passo executado e resultado registrado; `executionCount` atualizado

---

### TC-KNW-018 — Criar notebook de análise

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | CreateNotebook |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Usuário com role `knowledge:notebooks`

**Passos:**
1. Autenticar com JWT adequado
2. Enviar `POST /api/knowledge/notebooks` com body:
   ```json
   {
     "title": "Análise de Desempenho Q2/2026",
     "cells": [
       { "type": "markdown", "content": "# Introdução" },
       { "type": "query", "content": "SELECT * FROM metrics WHERE date > '2026-04-01'" }
     ]
   }
   ```

**Resultado Esperado:**
- HTTP 201 Created
- Notebook com ID e células na ordem correta

**Critério de Aceite:** Notebook criado com células ordenadas; disponível para edição

---

### TC-KNW-019 — Atualizar notebook com nova célula

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | UpdateNotebook |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Notebook existente com 2 células

**Passos:**
1. Autenticar com JWT do proprietário
2. Enviar `PUT /api/knowledge/notebooks/{id}` adicionando célula do tipo `chart`
3. Verificar ordem e conteúdo

**Resultado Esperado:**
- HTTP 200 OK
- 3 células na ordem correta; `updatedAt` atualizado

**Critério de Aceite:** Atualização idempotente; células na ordem definida

---

### TC-KNW-020 — Exportar dados de analytics (ExportAnalyticsData)

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | ExportAnalyticsData |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Notebook com dados de análise disponíveis

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/knowledge/notebooks/{id}/export?format=csv`

**Resultado Esperado:**
- HTTP 200 OK
- Arquivo CSV com dados das células de query do notebook
- `Content-Disposition: attachment; filename="analise-q2-2026.csv"`

**Critério de Aceite:** Export em CSV funcional com dados corretos das células

---

### TC-KNW-021 — Obter relatório de frescor de documentação

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | GetFreshnessReport |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Documentos com datas de criação e última revisão variadas; alguns desatualizados (> 90 dias sem revisão)

**Passos:**
1. Autenticar com JWT válido
2. Enviar `GET /api/knowledge/freshness/report`

**Resultado Esperado:**
- `totalDocuments`, `fresh`, `stale`, `critical` (>180 dias)
- Lista de documentos por categoria de frescor
- Score geral de saúde da base de conhecimento

**Critério de Aceite:** Classificação por frescor correta; documentos críticos identificados

---

### TC-KNW-022 — Pontuar frescor de documento (ScoreDocumentFreshness)

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | ScoreDocumentFreshness |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Documento criado há 120 dias com última revisão há 100 dias

**Passos:**
1. Autenticar com JWT válido
2. Enviar `POST /api/knowledge/freshness/score/{documentId}`

**Resultado Esperado:**
- `freshnessScore: 45` (escala 0-100, menor = mais desatualizado)
- `status: "Stale"`
- `recommendation: "Revisar documento em até 30 dias"`

**Critério de Aceite:** Score calculado com base em fórmula de frescor; status e recomendação corretos

---

### TC-KNW-023 — Validar gate de revisão de documento

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | ValidateDocumentReviewGate |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Documento com política de revisão obrigatória a cada 60 dias
- Última revisão há 65 dias

**Passos:**
1. Autenticar com JWT válido
2. Enviar `POST /api/knowledge/freshness/review-gate/{documentId}`

**Resultado Esperado:**
- HTTP 422
- `{ "gateStatus": "Failed", "reason": "ReviewOverdue", "daysSinceLastReview": 65 }`
- Documento bloqueado para uso em novos runbooks até revisão

**Critério de Aceite:** Gate de revisão funcional; bloqueio aplicado conforme política

---

### TC-KNW-024 — Adicionar observação a um ativo

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | AddObservation |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Ativo `payment-svc` registrado no sistema
- Usuário com role `knowledge:observations`

**Passos:**
1. Autenticar com JWT adequado
2. Enviar `POST /api/knowledge/observations` com body:
   ```json
   {
     "assetId": "payment-svc",
     "type": "performance",
     "content": "Latência p99 elevada entre 14h-16h em dias úteis",
     "severity": "Medium"
   }
   ```

**Resultado Esperado:**
- HTTP 201 Created
- Observação criada com `id`, `assetId`, `createdBy`, `createdAt`

**Critério de Aceite:** Observação persistida e vinculada ao ativo correto

---

### TC-KNW-025 — Registrar métricas de observação

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | RecordObservationMetrics |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Observação criada para `payment-svc`

**Passos:**
1. Autenticar com JWT de telemetria
2. Enviar `POST /api/knowledge/observations/metrics` com body:
   ```json
   {
     "observationId": "{id}",
     "metrics": { "p50": 120, "p99": 850, "errorRate": 0.02 }
   }
   ```

**Resultado Esperado:**
- HTTP 202 Accepted
- Métricas associadas à observação e disponíveis para análise

**Critério de Aceite:** Métricas persistidas e vinculadas à observação

---

### TC-KNW-026 — Criar e gerenciar visualização salva do usuário

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | CreateUserSavedView / UpdateUserSavedView / DeleteUserSavedView / ListUserSavedViews |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Usuário autenticado sem visualizações salvas

**Passos:**
1. Criar visualização: `POST /api/knowledge/saved-views` com `{ "name": "Runbooks de Incidente", "filters": { "type": "runbook", "tag": "incident" } }`
2. Listar visualizações: `GET /api/knowledge/saved-views`
3. Atualizar nome: `PUT /api/knowledge/saved-views/{id}`
4. Excluir: `DELETE /api/knowledge/saved-views/{id}`
5. Verificar que não aparece na listagem

**Resultado Esperado:**
- CRUD completo funcional
- Visualizações isoladas por usuário (não por tenant)

**Critério de Aceite:** Ciclo de vida completo da visualização salva funcionando

---

### TC-KNW-027 — Adicionar e remover bookmark

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | AddBookmark / RemoveBookmark |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:**
- Documento `Guia de Resposta a Incidentes` disponível

**Passos:**
1. Autenticar com JWT de usuário
2. Adicionar bookmark: `POST /api/knowledge/bookmarks` com `{ "documentId": "{id}" }`
3. Listar bookmarks: `GET /api/knowledge/bookmarks`
4. Remover bookmark: `DELETE /api/knowledge/bookmarks/{id}`
5. Verificar remoção

**Resultado Esperado:**
- Após passo 2: documento marcado; aparece na listagem
- Após passo 4: removido da listagem sem afetar o documento original

**Critério de Aceite:** Bookmark isolado por usuário; remoção não afeta documento

---

### TC-KNW-028 — Listar bookmarks do usuário

| Campo | Valor |
|-------|-------|
| **Módulo** | Knowledge |
| **Feature** | ListBookmarks |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:**
- Usuário A com 4 bookmarks; Usuário B com 2 bookmarks no mesmo tenant

**Passos:**
1. Autenticar como Usuário A
2. Enviar `GET /api/knowledge/bookmarks`
3. Autenticar como Usuário B
4. Enviar `GET /api/knowledge/bookmarks`

**Resultado Esperado:**
- Usuário A vê exatamente 4 bookmarks
- Usuário B vê exatamente 2 bookmarks
- Nenhuma sobreposição

**Critério de Aceite:** Bookmarks isolados por `UserId`; sem vazamento de dados entre usuários
