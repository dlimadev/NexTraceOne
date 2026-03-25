# Integrations — Module Scope Finalization

> **Module:** Integrations (12)  
> **Date:** 2026-03-25  
> **Status:** Scope finalized

---

## 1. Avaliação de áreas funcionais

| # | Área funcional | Estado | Obrigatória | Notas |
|---|---------------|--------|-------------|-------|
| 1 | Cadastro/configuração de integrações | ⚠️ Parcial | SIM | Entities existem, sem endpoint POST/PUT/DELETE |
| 2 | Conectores externos | ✅ Modelado | SIM | `IntegrationConnector` com provider, type, endpoint |
| 3 | Webhooks | ❌ Ausente | SIM | Sem endpoint de recepção de webhooks |
| 4 | Ingestão e envio | ⚠️ Parcial | SIM | `IngestionExecution` rastreia, mas sem orquestração real |
| 5 | Retries | ⚠️ Parcial | SIM | `RetryConnector` endpoint existe, mas sem retry policy configurável |
| 6 | Status de execução | ✅ Implementado | SIM | `ExecutionResult` enum com 6 estados, tracking completo |
| 7 | Health de integração | ✅ Implementado | SIM | `ConnectorHealth` + `GetIngestionHealth` endpoint |
| 8 | Histórico operacional | ✅ Implementado | SIM | `IngestionExecution` com pagination e filtros |
| 9 | Políticas consumidas de Governance | ❌ Ausente | SIM | Sem consumo de eventos/policies de Governance |
| 10 | Rastreabilidade e auditoria | ⚠️ Parcial | SIM | `CorrelationId` existe, sem forward para Audit |
| 11 | Observabilidade operacional | ✅ Implementado | SIM | Health, freshness, métricas de execução |
| 12 | Integração com notificações | ❌ Ausente | SIM | Sem publicação de eventos para Notifications |

---

## 2. Funcionalidades já existentes (✅)

| Funcionalidade | Backend | Frontend | Endpoint |
|---------------|---------|----------|----------|
| Listar conectores com filtros | ✅ `ListIntegrationConnectors` | ✅ `IntegrationHubPage` | GET `/connectors` |
| Ver detalhe de conector | ✅ `GetIntegrationConnector` | ✅ `ConnectorDetailPage` | GET `/connectors/{id}` |
| Listar fontes de ingestão | ✅ `ListIngestionSources` | ⚠️ Parcial (dentro de detail) | GET `/ingestion/sources` |
| Listar execuções | ✅ `ListIngestionExecutions` | ✅ `IngestionExecutionsPage` | GET `/ingestion/executions` |
| Health summary | ✅ `GetIngestionHealth` | ✅ (badge/stats) | GET `/integrations/health` |
| Freshness monitoring | ✅ `GetIngestionFreshness` | ✅ `IngestionFreshnessPage` | GET `/ingestion/freshness` |
| Retry de conector | ✅ `RetryConnector` | ✅ (botão retry) | POST `/connectors/{id}/retry` |
| Reprocessar execução | ✅ `ReprocessExecution` | ✅ (botão reprocess) | POST `/executions/{id}/reprocess` |

---

## 3. Funcionalidades parcialmente implementadas (⚠️)

| Funcionalidade | O que existe | O que falta |
|---------------|-------------|-------------|
| **CRUD de conectores** | Entity com Create(), métodos de update | Endpoints POST, PUT, DELETE não expostos na API |
| **CRUD de fontes** | Entity com Create(), métodos de gestão | Endpoints POST, PUT, DELETE não expostos na API |
| **Retry policy** | `RetryConnector` command + `RetryAttempt` counter | Sem política configurável (max retries, backoff, circuit breaker) |
| **Rastreabilidade** | `CorrelationId` em execuções | Sem forward de eventos para Audit & Compliance |
| **Métricas de conector** | TotalExecutions, SuccessfulExecutions, FailedExecutions | Sem dashboard de métricas agregadas, sem ClickHouse |

---

## 4. Funcionalidades ausentes (❌)

| Funcionalidade | Prioridade | Impacto |
|---------------|-----------|---------|
| **Criar conector via API** | 🔴 P1_CRITICAL | Sem isto, conectores só são criados via seed/migration |
| **Editar conector via API** | 🔴 P1_CRITICAL | Sem update de configuração via UI |
| **Eliminar conector** | 🟡 P2_HIGH | Sem cleanup de conectores deprecados |
| **Criar fonte de ingestão via API** | 🔴 P1_CRITICAL | Fontes não podem ser geridas operacionalmente |
| **Webhook endpoint** | 🔴 P1_CRITICAL | Sem recepção de dados push de sistemas externos |
| **Credential management** | 🔴 P1_CRITICAL | API keys/tokens não geridos de forma segura |
| **Testar conexão** | 🟡 P2_HIGH | Sem validação se conector funciona antes de activar |
| **Publicação de domain events** | 🔴 P1_CRITICAL | Sem eventos, Notifications e Audit não integram |
| **Consumo de policies de Governance** | 🟡 P2_HIGH | Sem validação de policies antes de activar conector |
| **Circuit breaker** | 🟡 P2_HIGH | Sem protecção contra falhas em cascata |
| **Configuração de retry policy** | 🟡 P2_HIGH | Retry sem limites configuráveis |
| **Scheduling/polling** | 🟡 P2_HIGH | Sem orquestração de polling periódico |
| **Bulk operations** | 🟢 P3_MEDIUM | Sem operações em lote sobre conectores |

---

## 5. Escopo funcional mínimo completo do módulo final

### Tier 1 — Obrigatório para o módulo ser utilizável

| # | Funcionalidade | Detalhes |
|---|---------------|----------|
| 1 | **CRUD completo de conectores** | POST, GET, PUT, DELETE com validação |
| 2 | **CRUD completo de fontes de ingestão** | POST, GET, PUT, DELETE por conector |
| 3 | **Tracking de execuções** | Start, Complete, Fail com métricas |
| 4 | **Health monitoring** | Status, health, freshness em tempo real |
| 5 | **Retry e reprocessamento** | Com policy configurável (max attempts, backoff) |
| 6 | **Publicação de domain events** | ConnectorCreated, ConnectorFailed, ExecutionCompleted, etc. |
| 7 | **Webhook endpoint** | Recepção de dados push com validação |
| 8 | **Credential management** | Gestão segura de API keys/tokens com encriptação |
| 9 | **Backend independente** | Extraído de Governance com `IntegrationsDbContext` |
| 10 | **Persistência com `int_`** | Tabelas com prefixo correcto |

### Tier 2 — Importante para qualidade

| # | Funcionalidade | Detalhes |
|---|---------------|----------|
| 11 | **Test connection** | Endpoint para testar se conector funciona |
| 12 | **Circuit breaker** | Protecção contra falhas repetidas |
| 13 | **Integração com Governance policies** | Consumir events de policy approval |
| 14 | **Forward para Audit** | Eventos sensíveis enviados ao Audit |
| 15 | **Forward para Notifications** | Falhas e degradações notificadas |
| 16 | **Scheduling/polling** | Execução periódica configurável |
| 17 | **Frontend CRUD** | Formulários de criação/edição no frontend |
| 18 | **Menu entries** | Execuções e freshness como items directos no menu |

### Tier 3 — Desejável

| # | Funcionalidade | Detalhes |
|---|---------------|----------|
| 19 | Bulk operations | Activar/desactivar múltiplos conectores |
| 20 | Import/export de configuração | Exportar config de conector como JSON |
| 21 | Connector templates | Templates pré-configurados para providers comuns |
| 22 | Analytics em ClickHouse | Métricas de execução para análise de longo prazo |

---

## 6. O que NÃO pertence ao módulo

| Item | Módulo correcto |
|------|----------------|
| Definição de políticas de integração aprovadas | Governance (08) |
| Compliance checks sobre integrações | Governance (08) |
| Registo de auditoria de acções | Audit & Compliance (10) |
| Envio de notificações de falha | Notifications (11) |
| Definição de serviços que integrações alimentam | Service Catalog (03) |
| Gestão de ambientes | Environment Management (02) |
| Métricas operacionais agregadas | Operational Intelligence (06) |
