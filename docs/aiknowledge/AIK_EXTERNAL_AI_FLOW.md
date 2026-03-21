# AIKnowledge ExternalAI — Fluxo de Conhecimento Externo (Fase 2)

## Visão Geral

O módulo `AIKnowledge.ExternalAI` governa a captura, validação, aprovação, listagem e reutilização de respostas produzidas por provedores externos de IA. É o ponto de entrada para o ciclo de vida de conhecimento externo no NexTraceOne.

---

## Entidades Principais

### `ExternalAiConsultation`
Registo de uma consulta enviada a um provedor externo de IA.
- Campos: `ProviderId`, `Context`, `Query`, `RequestedBy`, `RequestedAt`, `Status`, `Response`, `TokensUsed`, `Confidence`
- Status: `Pending → Completed | Failed`
- Criada via `ExternalAiConsultation.Create(providerId, context, query, requestedBy, now)`
- Resposta registada via `RecordResponse(response, tokensUsed, confidence, now)`

### `KnowledgeCapture`
Capture de conhecimento extraído de uma consulta externa, pronto para revisão humana.
- Campos: `ConsultationId`, `Title`, `Content`, `Category`, `Tags`, `Status`, `ReviewedBy`, `ReviewedAt`, `RejectionReason`, `ReuseCount`, `CapturedAt`
- Status: `Pending → Approved | Rejected`
- Criado via `KnowledgeCapture.Capture(consultationId, title, content, category, tags, now)`
- Aprovado via `Approve(reviewer, now)` — falha se já processado
- Reutilizado via `IncrementReuse()` — falha se não aprovado

### `ExternalAiPolicy`
Política de governança de uso de IA externa.
- Campos: `Name`, `Description`, `MaxDailyQueries`, `MaxTokensPerDay`, `RequiresApproval`, `AllowedContexts`, `IsActive`
- Criada via `ExternalAiPolicy.Create(...)` ou atualizada via `Update(...)`

### `ExternalAiProvider`
Provedor externo de IA registado (OpenAI, Anthropic, etc.).

---

## Fluxo de Captura de Conhecimento

```
1. POST /api/v1/externalai/knowledge/capture
   → Verifica que ProviderId existe
   → Cria ExternalAiConsultation (status: Completed)
   → Cria KnowledgeCapture (status: Pending)
   → Persiste ambos em ExternalAiDbContext

2. GET /api/v1/externalai/knowledge/captures
   → Lista captures com filtros (status, categoria, tags, período, texto livre)
   → Paginação com ordenação por CapturedAt desc

3. POST /api/v1/externalai/knowledge/captures/{captureId}/approve
   → Valida que capture existe e está Pending
   → Chama Approve(reviewer, now) — falha se já processado
   → Persiste status = Approved + reviewer + reviewedAt

4. POST /api/v1/externalai/knowledge/captures/{captureId}/reuse
   → Valida que capture existe e está Approved
   → Chama IncrementReuse() — falha se não aprovado
   → Persiste ReuseCount++ e regista novo contexto

5. GET /api/v1/externalai/knowledge/usage
   → Agrega métricas reais de ExternalAiDbContext
   → Retorna: consultas, tokens, providers, captures, aprovações, reutilizações
```

---

## Fluxo de Configuração de Políticas

```
POST /api/v1/externalai/knowledge/policy
  → Procura policy por nome
  → Se não existe: cria nova (Action = "Created")
  → Se existe: atualiza campos (Action = "Updated")
  → Persiste em ExternalAiDbContext
```

---

## Repositórios (Abstrações Application / Implementações Infrastructure)

```
IKnowledgeCaptureRepository
  - GetByIdAsync(id, ct)
  - AddAsync(capture, ct)       — persiste imediatamente (SaveChangesAsync)
  - UpdateAsync(capture, ct)    — persiste imediatamente (SaveChangesAsync)
  - ListAsync(status?, cat?, tags?, textFilter?, from?, to?, page, pageSize, ct)
  - GetUsageMetricsAsync(from?, to?, ct)

IExternalAiConsultationRepository
  - AddAsync(consultation, ct)  — persiste imediatamente

IExternalAiPolicyRepository
  - GetByNameAsync(name, ct)
  - AddAsync(policy, ct)        — persiste imediatamente
  - UpdateAsync(policy, ct)     — persiste imediatamente

IExternalAiProviderRepository
  - ExistsAsync(id, ct)
```

Implementações: `ExternalAiRepositories.cs` em `NexTraceOne.AIKnowledge.Infrastructure/ExternalAI/Persistence/Repositories/`

---

## Decisões de Design

1. **Persistência directa nos repos:** cada repositório de escrita chama `SaveChangesAsync()` directamente para garantir atomicidade por operação, sem depender da resolução de `IUnitOfWork` num container com múltiplos DbContexts.

2. **Create-or-update para políticas:** `ConfigureExternalAIPolicy` usa upsert por nome — facilita configuração idempotente via CI/CD ou scripts de setup.

3. **Validação de provedor em `CaptureExternalAIResponse`:** garante integridade referencial sem constraint de DB adicional.

---

## Próximos Passos (Fase 3+)

1. Endpoint para rejeitar capture (`POST /captures/{captureId}/reject`)
2. Listar políticas activas (`GET /knowledge/policies`)
3. Aplicar política automaticamente na captura (verificar `RequiresApproval`)
4. Notificação quando capture muda de estado
5. Índices full-text em `KnowledgeCapture.Content` para busca textual eficiente
