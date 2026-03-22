# Fase 2 — Fechamento Funcional do Núcleo de IA (AIKnowledge)

## Visão Geral

A Fase 2 transformou o módulo AIKnowledge de uma base com handlers vazios e contexto simulado em uma capability real de produto enterprise.

---

## Handlers Implementados

### AIKnowledge.ExternalAI (6 handlers)

| Handler | Tipo | Descrição |
|---------|------|-----------|
| `CaptureExternalAIResponse` | Command | Captura e persiste respostas de IA externa com status Pending para revisão |
| `ApproveKnowledgeCapture` | Command | Aprova formalmente um capture, registando revisor e timestamp |
| `ListKnowledgeCaptures` | Query | Lista captures com filtros por status, categoria, tags, período e texto |
| `GetExternalAIUsage` | Query | Métricas agregadas reais: consultas, tokens, aprovações, reutilizações |
| `ReuseKnowledgeCapture` | Command | Reutiliza capture aprovado, incrementa contador e regista contexto |
| `ConfigureExternalAIPolicy` | Command | Cria ou atualiza política de governança de IA externa (create-or-update por nome) |

### AIKnowledge.Orchestration (5 handlers)

| Handler | Tipo | Descrição |
|---------|------|-----------|
| `GetAiConversationHistory` | Query | Histórico real de conversas com filtros por release, serviço, tópico, status |
| `ValidateKnowledgeCapture` | Command | Valida se entry está apta para aprovação (completude, relevância, duplicidade) |
| `GenerateTestScenarios` | Command | Gera cenários de teste via IA, persiste como `GeneratedTestArtifact` se releaseId fornecido |
| `GenerateRobotFrameworkDraft` | Command | Gera draft Robot Framework via IA, persiste como `GeneratedTestArtifact` se releaseId fornecido |
| `SummarizeReleaseForApproval` | Command | Resume release para aprovação usando dados reais de conversas e artefatos gerados |

---

## EnrichContext — Remoção do Stub

O handler `EnrichContext` foi refatorado de stub para implementação real:

**Antes:** retornava string fixa `"Stub: retorna contexto simulado"` para cada fonte
**Depois:**
1. Lê fontes ativas do `IAiKnowledgeSourceRepository` (dados reais do `AiGovernanceDbContext`)
2. Classifica o caso de uso a partir do `UseCaseType` ou inferência por keywords da query
3. Seleciona fontes por prioridade ponderada por caso de uso
4. Usa `Description` real de cada fonte como snippet de contexto
5. Adiciona entity hints para ServiceId, ContractId, IncidentId quando fornecidos
6. Calcula `ConfidenceLevel` baseado no número de fontes resolvidas
7. Mede tempo de processamento real via `Stopwatch`

---

## Novas Interfaces de Repositório (Application Layer)

### ExternalAI
- `IKnowledgeCaptureRepository` — CRUD + listagem paginada + métricas agregadas
- `IExternalAiConsultationRepository` — persistência de consultas
- `IExternalAiPolicyRepository` — CRUD de políticas por nome
- `IExternalAiProviderRepository` — verificação de existência

### Orchestration
- `IAiOrchestrationConversationRepository` — listagem histórica + resumo por release
- `IKnowledgeCaptureEntryRepository` — GetById + verificação de duplicidade por conversa
- `IGeneratedTestArtifactRepository` — persistência + resumo por release

---

## Endpoints Adicionados

### ExternalAI (`/api/v1/externalai/knowledge`)
- `POST /capture` — captura resposta de IA externa
- `GET /captures` — lista captures com filtros
- `POST /captures/{captureId}/approve` — aprova capture
- `POST /captures/{captureId}/reuse` — reutiliza capture
- `GET /usage` — métricas agregadas de uso
- `POST /policy` — configura política de IA externa

### Orchestration (`/api/v1/aiorchestration`)
- `GET /conversations/history` — histórico de conversas
- `POST /knowledge/entries/{entryId}/validate` — valida capture de conhecimento
- `POST /generate/test-scenarios` — gera cenários de teste
- `POST /generate/robot-framework` — gera draft Robot Framework
- `POST /generate/releases/{releaseId}/approval-summary` — resume release para aprovação

---

## Testes Adicionados

- `Phase2ExternalAiHandlerTests.cs` — 10 testes cobrindo todos os 6 handlers ExternalAI
- `Phase2OrchestrationHandlerTests.cs` — 13 testes cobrindo todos os 5 handlers Orchestration
- Total: **399 testes** (eram 374 na Fase anterior) — todos passam

---

## Limitações Conhecidas e Próximos Passos

Ver [PHASE-2-AIKNOWLEDGE-REPORT.md](../audits/PHASE-2-AIKNOWLEDGE-REPORT.md) para detalhes completos.
