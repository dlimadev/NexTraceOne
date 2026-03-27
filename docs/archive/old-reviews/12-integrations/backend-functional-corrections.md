# Integrations — Backend Functional Corrections

> **Module:** Integrations (12)  
> **Date:** 2026-03-25  
> **Status:** Backlog de correcções gerado

---

## 1. Endpoints actuais do módulo

| # | Método | Rota | Permissão | Feature (CQRS) | Estado |
|---|--------|------|-----------|----------------|--------|
| 1 | GET | `/api/v1/integrations/connectors` | `integrations:read` | `ListIntegrationConnectors` | ✅ Funcional |
| 2 | GET | `/api/v1/integrations/connectors/{id}` | `integrations:read` | `GetIntegrationConnector` | ✅ Funcional |
| 3 | GET | `/api/v1/ingestion/sources` | `integrations:read` | `ListIngestionSources` | ✅ Funcional |
| 4 | GET | `/api/v1/ingestion/executions` | `integrations:read` | `ListIngestionExecutions` | ✅ Funcional |
| 5 | GET | `/api/v1/integrations/health` | `integrations:read` | `GetIngestionHealth` | ✅ Funcional |
| 6 | GET | `/api/v1/ingestion/freshness` | `integrations:read` | `GetIngestionFreshness` | ✅ Funcional |
| 7 | POST | `/api/v1/integrations/connectors/{id}/retry` | `integrations:write` | `RetryConnector` | ✅ Funcional |
| 8 | POST | `/api/v1/ingestion/executions/{id}/reprocess` | `integrations:write` | `ReprocessExecution` | ✅ Funcional |

**Ficheiro:** `src/modules/governance/NexTraceOne.Governance.API/Endpoints/IntegrationHubEndpointModule.cs` (142 LOC)

---

## 2. Endpoints → Casos de uso

| Endpoint | Caso de uso | Cobertura |
|----------|-------------|-----------|
| GET connectors | Listar conectores com filtros (status, health, type, search) | ✅ Completo |
| GET connector/{id} | Ver detalhe com execuções recentes | ✅ Completo |
| GET sources | Listar fontes com filtros (connector, status, freshness) | ✅ Completo |
| GET executions | Listar execuções com filtros (connector, source, result, date range) | ✅ Completo |
| GET health | Summary de saúde — total, healthy, degraded, critical | ✅ Completo |
| GET freshness | Status de freshness por domínio | ✅ Completo |
| POST retry | Queuing retry de conector falhado | ⚠️ Queue sem processamento real |
| POST reprocess | Queuing reprocessamento de execução | ⚠️ Queue sem processamento real |

---

## 3. Endpoints mortos ou sem efeito real

| # | Endpoint | Problema |
|---|----------|----------|
| B-01 | POST `/retry` | ⚠️ Marca retry como queued mas **não há worker/background job** que execute o retry real |
| B-02 | POST `/reprocess` | ⚠️ Marca reprocess como queued mas **não há worker** que reprocesse |

**Impacto:** Utilizador vê "Retry queued successfully" mas nada acontece.

---

## 4. Endpoints ausentes (❌)

| # | Endpoint necessário | Método | Permissão | Prioridade | Impacto |
|---|-------------------|--------|-----------|-----------|---------|
| B-03 | `/api/v1/integrations/connectors` | POST | `integrations:write` | 🔴 P1_CRITICAL | Sem CRUD, conectores só existem via seed |
| B-04 | `/api/v1/integrations/connectors/{id}` | PUT | `integrations:write` | 🔴 P1_CRITICAL | Sem update de configuração via API |
| B-05 | `/api/v1/integrations/connectors/{id}` | DELETE | `integrations:write` | 🟡 P2_HIGH | Sem cleanup de conectores |
| B-06 | `/api/v1/integrations/connectors/{id}/activate` | POST | `integrations:write` | 🟡 P2_HIGH | Activar conector |
| B-07 | `/api/v1/integrations/connectors/{id}/disable` | POST | `integrations:write` | 🟡 P2_HIGH | Desactivar conector |
| B-08 | `/api/v1/integrations/connectors/{id}/test` | POST | `integrations:write` | 🟡 P2_HIGH | Testar se conector funciona |
| B-09 | `/api/v1/ingestion/sources` | POST | `integrations:write` | 🔴 P1_CRITICAL | Criar fonte de ingestão |
| B-10 | `/api/v1/ingestion/sources/{id}` | PUT | `integrations:write` | 🟡 P2_HIGH | Actualizar fonte |
| B-11 | `/api/v1/ingestion/sources/{id}` | DELETE | `integrations:write` | 🟡 P2_HIGH | Eliminar fonte |
| B-12 | `/api/v1/integrations/webhooks/{connectorId}` | POST | (webhook secret) | 🔴 P1_CRITICAL | Recepção de webhooks |

---

## 5. Revisão de requests/responses

| Endpoint | Problema | Correcção |
|----------|----------|-----------|
| GET connectors | ✅ Paginação, filtros, search | — |
| GET connector/{id} | ✅ Inclui execuções recentes | — |
| GET sources | ✅ Filtros por connector, status, freshness | — |
| GET executions | ✅ Filtros por connector, source, result, date range | — |
| GET health | ✅ Retorna contadores por status | — |
| GET freshness | ✅ Retorna freshness por domínio | — |
| POST retry | ⚠️ Retorna 200 com mensagem genérica | Deveria retornar 202 Accepted com job ID |
| POST reprocess | ⚠️ Retorna 200 com mensagem genérica | Deveria retornar 202 Accepted com job ID |

---

## 6. Revisão de validações

| # | Endpoint/Feature | Validação esperada | Estado |
|---|-----------------|-------------------|--------|
| B-13 | POST connector (futuro) | Name unique, Provider required, Endpoint URL format | ❌ Inexistente |
| B-14 | PUT connector (futuro) | Connector exists, not deleted | ❌ Inexistente |
| B-15 | POST source (futuro) | Connector exists, Name unique within connector | ❌ Inexistente |
| B-16 | POST retry | Connector exists, Status == Failed | ⚠️ Parcial — verifica existência mas não status |
| B-17 | POST reprocess | Execution exists, Result in (Failed, TimedOut) | ⚠️ Parcial |

---

## 7. Revisão de tratamento de erro

| Problema | Detalhe |
|----------|---------|
| B-18 | `RetryConnector` e `ReprocessExecution` não usam `Result<T>` consistentemente |
| B-19 | Sem tratamento de concurrency (sem RowVersion/xmin) |
| B-20 | Sem guard clause para tenant isolation em queries manuais (depende de RLS) |

---

## 8. Revisão de auditoria

| # | Problema | Impacto | Prioridade |
|---|----------|---------|-----------|
| B-21 | **Nenhum domain event é publicado** | Audit & Compliance não recebe eventos de integração | 🔴 P0_BLOCKER |
| B-22 | `IntegrationEvents.cs` contém apenas eventos de Governance, não de Integrations | Confuso — ficheiro mal nomeado | 🟡 P2_HIGH |
| B-23 | Sem `AuditLog` explícito em acções sensíveis (retry, reprocess, create, delete) | Rastreabilidade zero | 🔴 P1_CRITICAL |

---

## 9. Revisão de permissões por acção

| Acção | Permissão actual | Permissão correcta | Gap |
|-------|-----------------|-------------------|-----|
| Listar conectores | `integrations:read` | `integrations:read` | ✅ |
| Ver detalhe | `integrations:read` | `integrations:read` | ✅ |
| Retry conector | `integrations:write` | `integrations:write` | ✅ |
| Reprocess execução | `integrations:write` | `integrations:write` | ✅ |
| Criar conector | — | `integrations:write` | ❌ Endpoint não existe |
| Editar conector | — | `integrations:write` | ❌ Endpoint não existe |
| Eliminar conector | — | `integrations:admin` | ❌ Endpoint não existe, permissão nova |
| Testar conexão | — | `integrations:write` | ❌ Endpoint não existe |
| Gerir credenciais | — | `integrations:admin` | ❌ Permissão nova necessária |

---

## 10. Fluxos críticos

### Criar/Configurar Integração
- **Estado:** ❌ Não implementado via API — entidades podem ser criadas via factory method mas sem endpoint
- **Fix:** Criar POST `/connectors` + `CreateConnector` command handler

### Testar Integração
- **Estado:** ❌ Não implementado
- **Fix:** Criar POST `/connectors/{id}/test` + `TestConnector` command handler

### Receber Webhook
- **Estado:** ❌ Não implementado
- **Fix:** Criar POST `/webhooks/{connectorId}` com validação de webhook secret

### Disparar Sincronização
- **Estado:** ❌ Não implementado (sem scheduling/polling engine)
- **Fix:** Background job com Quartz.NET ou similar (fase posterior)

### Retry/Reprocessar
- **Estado:** ⚠️ Endpoint existe mas sem worker
- **Fix:** Implementar background worker que processe retry queue

### Consultar Status/Histórico/Health
- **Estado:** ✅ Implementado e funcional
- **Fix:** Nenhuma correcção imediata necessária

---

## 11. Backlog de correcções backend

| # | Item | Prioridade | Tipo | Esforço |
|---|------|-----------|------|---------|
| B-01 | Implementar retry worker (background job) | 🔴 P1_CRITICAL | FUNCTIONAL_FIX | 8h |
| B-02 | Implementar reprocess worker (background job) | 🔴 P1_CRITICAL | FUNCTIONAL_FIX | 8h |
| B-03 | Criar POST `/connectors` (CreateConnector) | 🔴 P1_CRITICAL | FUNCTIONAL_FIX | 4h |
| B-04 | Criar PUT `/connectors/{id}` (UpdateConnector) | 🔴 P1_CRITICAL | FUNCTIONAL_FIX | 4h |
| B-05 | Criar DELETE `/connectors/{id}` (soft delete) | 🟡 P2_HIGH | FUNCTIONAL_FIX | 3h |
| B-06 | Criar POST/PUT/DELETE para ingestion sources | 🔴 P1_CRITICAL | FUNCTIONAL_FIX | 6h |
| B-07 | Criar POST `/connectors/{id}/activate` e `/disable` | 🟡 P2_HIGH | FUNCTIONAL_FIX | 2h |
| B-08 | Criar POST `/connectors/{id}/test` (TestConnector) | 🟡 P2_HIGH | FUNCTIONAL_FIX | 6h |
| B-09 | Criar POST `/webhooks/{connectorId}` (WebhookEndpoint) | 🔴 P1_CRITICAL | FUNCTIONAL_FIX | 8h |
| B-10 | Publicar domain events em todos os métodos de transição | 🔴 P0_BLOCKER | FUNCTIONAL_FIX | 4h |
| B-11 | Criar `IntegrationDomainEvents.cs` com eventos próprios | 🔴 P1_CRITICAL | FUNCTIONAL_FIX | 2h |
| B-12 | Adicionar audit logging para acções sensíveis | 🔴 P1_CRITICAL | FUNCTIONAL_FIX | 3h |
| B-13 | Adicionar validação FluentValidation em commands | 🟡 P2_HIGH | FUNCTIONAL_FIX | 3h |
| B-14 | Retornar 202 Accepted em retry/reprocess (em vez de 200) | 🟢 P3_MEDIUM | QUICK_WIN | 1h |
| B-15 | Adicionar RowVersion (xmin) a Connector e Source | 🟡 P2_HIGH | STRUCTURAL_FIX | 2h |
| B-16 | Criar permissão `integrations:admin` para acções destrutivas | 🟡 P2_HIGH | FUNCTIONAL_FIX | 2h |
| B-17 | Extrair backend de Governance para módulo independente | 🔴 P0_BLOCKER | STRUCTURAL_FIX | 16h |

**Total estimado: ~82h**
