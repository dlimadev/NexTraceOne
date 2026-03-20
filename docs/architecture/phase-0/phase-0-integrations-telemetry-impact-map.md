# Phase 0 — Integrations & Telemetry Impact Map

**Data:** 2026-03-20  
**Scope:** Inventário de impacto em telemetria, integrações, mensageria, autenticação e observabilidade

---

## 1. Telemetria e Observabilidade

### 1.1 OpenTelemetry / Tracing

| Item | Localização | TenantId? | EnvironmentId? | Risco | Fase |
|------|-------------|-----------|----------------|-------|------|
| OTel configuration | `BuildingBlocks.Observability.DependencyInjection` | ❌ | ❌ | 🟠 Spans de diferentes tenants não são distinguíveis | 5 |
| Serilog enrichers | `BuildingBlocks.Observability.Logging.SerilogConfiguration` | ❌ | ❌ | 🟠 Logs sem tenant_id e environment_id nos structured fields | 5 |
| Metrics (`NexTraceMeters`) | `BuildingBlocks.Observability.Metrics.NexTraceMeters` | ❌ | ❌ | 🟠 Métricas sem dimensão tenant/ambiente | 5 |

**Problema central:** Nenhum enricher de TenantId ou EnvironmentId é configurado no OpenTelemetry ou no Serilog. Traces, logs e métricas não carregam `tenant_id` nem `environment_id` como atributos de span / structured fields, tornando impossível filtrar por tenant/ambiente em ferramentas de observabilidade.

**Ação futura (Fase 5):**
- Adicionar `tenant_id` e `environment_id` como activity tags via `Activity.Current?.SetTag()`
- Enriquecer Serilog com `tenant_id` e `environment_id` via `LogContext.PushProperty`
- Adicionar dimensões tenant/environment às métricas

### 1.2 Modelos de Telemetria no Product Store

Ver `phase-0-data-impact-map.md` — todos os modelos (`ObservedTopologyEntry`, `AnomalySnapshot`, `ServiceMetricsSnapshot`, `ReleaseRuntimeCorrelation`, `InvestigationContext`, `TelemetryReference`) têm `TenantId` como `Guid?` nullable e `environment` como `string`.

### 1.3 IProductStore / IMetricsStore

| Interface | Localização | Problema |
|-----------|-------------|----------|
| `IObservedTopologyReader.GetByServiceAsync` | BuildingBlocks.Observability | Parâmetro `string environment` sem TenantId |
| `IObservedTopologyReader.GetByEnvironmentAsync` | BuildingBlocks.Observability | Apenas `string environment` — query global |
| `IAnomalySnapshotReader.GetActiveByServiceAsync` | BuildingBlocks.Observability | `string environment` sem TenantId |
| `IAnomalySnapshotReader.GetByTimeRangeAsync` | BuildingBlocks.Observability | `string environment` sem TenantId |
| `IInvestigationContextReader.GetOpenByServiceAsync` | BuildingBlocks.Observability | `string environment` sem TenantId |
| `IInvestigationContextReader.GetByTimeRangeAsync` | BuildingBlocks.Observability | `string environment` sem TenantId |
| `IReleaseCorrelationReader.GetByServiceAsync` | BuildingBlocks.Observability | `string environment` sem TenantId |
| `IMetricsStore` (várias operações) | BuildingBlocks.Observability | `string environment` sem TenantId em todas as assinaturas |

**Risco:** Queries de telemetria hoje consultam dados por ambiente sem filtrar por tenant. Dois tenants com o mesmo nome de ambiente (ex: ambos com "Production") teriam dados misturados.

---

## 2. Autenticação e IAM

### 2.1 JWT e Claims

| Item | Localização | Estado | Risco |
|------|-------------|--------|-------|
| JWT claim `tenant_id` | `TenantResolutionMiddleware` | ✅ Resolvido via JWT | Baixo |
| JWT claim sem `environment_id` | — | ❌ Não existe | 🟠 Ambiente não está no token |
| API Key com `tenant_id` | `ApiKeyAuthenticationHandler` | ⚠️ Validado como Guid string | Médio |
| API Key sem `environment_id` | — | ❌ | 🟡 Integrações externas não indicam ambiente |

**Problema:** O JWT atual não contém `environment_id`. O ambiente ativo não é propagado de forma confiável do frontend para o backend, dependendo de headers opcionais ou parâmetros de query.

**Ação futura (Fase 2):**
- Avaliar se `environment_id` deve ir no JWT como claim opcional
- Alternativa: propagar via header `X-Environment-Id` (mais flexível, permite troca sem relogin)
- Preferência arquitetural: **header `X-Environment-Id`** para flexibilidade, validado no pipeline behavior

### 2.2 TenantResolutionMiddleware

Resolução atual: JWT → Header → Subdomain. Funciona bem para TenantId.  
Não há resolução equivalente para EnvironmentId.

**Ação futura:** Criar `EnvironmentResolutionMiddleware` que resolve `EnvironmentId` de:
1. Header `X-Environment-Id` (prioridade 1 — frontend)
2. Query parameter `?environmentId=` (fallback para integrations)
3. Contexto padrão do tenant (primeiro ambiente ativo do tenant)

---

## 3. Ingestion API — Integrações CI/CD Externas

| Endpoint | TenantId? | EnvironmentId? | Problema | Risco |
|----------|-----------|----------------|----------|-------|
| POST `/ingest/deployments` | ⚠️ Via ApiKey claim | ⚠️ `environment: string` no body | Ambiente como string livre na notificação de deploy | 🔴 |
| POST `/ingest/promotions` | ⚠️ Via ApiKey claim | ⚠️ `sourceEnv: string`, `targetEnv: string` | Ambientes como strings, não IDs validados | 🔴 |
| POST `/ingest/consumers` | ⚠️ Via ApiKey claim | ⚠️ `environment?: string` | Ambiente opcional como string | 🟠 |
| POST `/ingest/runtime-signals` | ⚠️ Via ApiKey claim | ⚠️ `environment: string` | Sinal de runtime sem EnvironmentId tipado | 🟠 |
| POST `/ingest/contracts` | ⚠️ Via ApiKey claim | ❌ | Contratos sem ambiente | 🟡 |
| POST `/ingest/markers` | ⚠️ Via ApiKey claim | ⚠️ `environment: string` | Marcador operacional sem EnvironmentId | 🟡 |

**Problema principal:** A Ingestion API é o entry point de integrações externas (GitHub Actions, GitLab CI, Azure DevOps, Jenkins). Hoje, o ambiente nos payloads é uma string livre não validada contra o modelo de ambientes do tenant. Após a refatoração, a API deve:
1. Resolver TenantId via ApiKey (já funciona)
2. Resolver EnvironmentId via slug do tenant: `POST /ingest/deployments { ..., environmentSlug: "production" }` → lookup `SELECT id FROM environments WHERE tenant_id = ? AND slug = ?`
3. Rejeitar payloads com ambientes desconhecidos para o tenant

---

## 4. Event Bus e Outbox

| Item | Localização | TenantId? | EnvironmentId? | Risco |
|------|-------------|-----------|----------------|-------|
| `InProcessEventBus` | BuildingBlocks.Infrastructure.EventBus | ❌ No handler dispatch | ❌ | 🟡 Eventos internos sem contexto |
| `OutboxEventBus` | BuildingBlocks.Infrastructure.EventBus | ❌ | ❌ | 🟡 Outbox sem tenant tagging |
| `OutboxMessage` | BuildingBlocks.Infrastructure.Outbox | ❌ | ❌ | 🟡 Mensagens do outbox sem TenantId |
| Integration events (`UserCreatedIntegrationEvent`, `UserRoleChangedIntegrationEvent`) | IdentityAccess.Contracts.IntegrationEvents | ✅ `TenantId` em payload | ❌ | 🟡 Sem EnvironmentId |

**Problema:** O `OutboxMessage` não tem `TenantId`. Eventos no outbox de um tenant podem ser processados fora do contexto correto. Em ambientes de alta carga, isso pode causar processamento cross-tenant se o consumer não validar o tenant.

**Ação futura (Fase 5):**
- Adicionar `TenantId` ao `OutboxMessage`
- Garantir que handlers de integração events recebem TenantId no payload
- Considerar adicionar `EnvironmentId` em integration events operacionais (ex: `ReleaseDeployedIntegrationEvent`)

---

## 5. Integrações ITSM e Externas (Governance Module)

| Integração | Estado Atual | TenantId? | EnvironmentId? | Risco |
|------------|-------------|-----------|----------------|-------|
| `IntegrationConnector` entity | Infrastructure vazia — sem persistência real | ❌ | 🔴 hardcoded `"Production"` | 🔴 |
| Jira, ServiceNow, PagerDuty, Slack bindings | Stubs / mock responses | ❌ | ❌ | 🟠 Quando implementados, devem ser tenant+environment scoped |

**Problema:** A Governance module tem 9 entidades definidas mas infrastructure completamente vazia. Quando implementada, cada connector deve pertencer a um tenant e, em alguns casos, a um ambiente específico (ex: alertas de produção → PagerDuty; alertas de staging → Slack).

---

## 6. Caching

| Item | Localização | TenantId? | EnvironmentId? | Risco |
|------|-------------|-----------|----------------|-------|
| IMemoryCache / IDistributedCache | Vários módulos | ❌ Cache keys sem tenant/ambiente prefix | ❌ | 🟠 Cache contaminado entre tenants |

**Problema:** Onde cache é usado, as chaves de cache não incluem `TenantId` ou `EnvironmentId`. Isso pode causar:
- Tenant A receber dados cacheados do Tenant B
- Ambiente "staging" receber resposta cacheada de "production"

**Ação futura (Fase 4):**
- Implementar convenção de cache keys: `{tenantId}:{environmentId}:{resource}:{id}`
- Encapsular em helper `ICacheKeyBuilder.Build(tenantId, environmentId, resource, id)`

---

## 7. Correlação Distribuída

| Item | Estado Atual | Risco |
|------|-------------|-------|
| Correlation ID em headers HTTP | ⚠️ Não há middleware de correlation ID padronizado encontrado | 🟡 |
| `tenant_id` em span attributes | ❌ | 🟠 |
| `environment_id` em span attributes | ❌ | 🟠 |
| Trace ID propagação | ✅ Via OTel (assumido) | 🟢 |

**Ação futura (Fase 5):**
- Adicionar middleware de correlação que propaga `X-Correlation-Id`, `X-Tenant-Id`, `X-Environment-Id`
- Enriquecer Activity com `tenant_id` e `environment_id` como tags padrão

---

## 8. Riscos de Segurança e Isolamento

| Risco | Tipo | Severidade | Mitigação Atual | Mitigação Futura |
|-------|------|-----------|-----------------|-----------------|
| Queries de telemetria sem TenantId retornam dados cross-tenant | tenant-isolation-gap | 🔴 Crítico | Nenhuma | Tornar TenantId required em IProductStore/IMetricsStore |
| Ingestion API aceita ambiente como string livre sem validação | integration-binding-gap | 🔴 Crítico | Nenhuma | Resolver EnvironmentId via slug lookup |
| Cache sem tenant prefix pode contaminar dados | hidden-coupling | 🟠 Alto | Nenhuma | Cache key builder com tenant+env prefix |
| OutboxMessage sem TenantId pode processar fora de contexto | tenant-isolation-gap | 🟡 Médio | Nenhuma | Adicionar TenantId ao OutboxMessage |
| IA recebe TenantId como campo opcional no body | ai-context-gap | 🔴 Crítico | Nenhuma | Extrair sempre de ICurrentTenant |
| Spans e logs sem tenant_id | telemetry-context-gap | 🟠 Alto | Nenhuma | Enrichers e activity tags |
| Dois tenants com mesmo slug de ambiente conflitam na telemetria | naming-assumption | 🟠 Alto | Nenhuma | Usar EnvironmentId (Guid) em vez de slug em telemetria |

---

## 9. Dependências Externas Identificadas

| Dependência | Tipo | Tenant-aware? | Environment-aware? | Notas |
|-------------|------|--------------|-------------------|-------|
| PostgreSQL (via EF Core) | Banco de dados | ✅ Via RLS | ❌ Sem RLS por ambiente | RLS configurado para TenantId; adicionar EnvironmentId seria custo adicional |
| Ollama (AI local) | LLM provider | ❌ | ❌ | Globalmente compartilhado; contexto injetado via prompt |
| OpenAI (AI externa) | LLM provider | ❌ | ❌ | Globalmente compartilhado; contexto injetado via prompt |
| Quartz.NET | Job scheduler | ⚠️ | ❌ | Jobs hoje não têm TenantId no contexto; expiração handlers extrai via entidade |
| Serilog | Logging | ❌ enricher | ❌ | Precisa de enricher para tenant_id e environment_id |
| OpenTelemetry | Tracing | ❌ | ❌ | Precisa de activity tags |
