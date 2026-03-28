# Relatório de Estado do Backend — NexTraceOne
**Auditoria Forense | 28 de Março de 2026**

---

## Objetivo da Área no Contexto do Produto

O backend é a autoridade final de todas as operações — governança de contratos, change intelligence, identidade, auditoria, IA governada e conhecimento operacional. Toda lógica de negócio, autorização e persistência deve residir aqui.

---

## Estado Atual Encontrado

### Estrutura Geral

| Componente | Valor |
|---|---|
| Total de ficheiros `.cs` | 1.866+ |
| Módulos de domínio | 12 |
| Camadas por módulo | 5 (Domain, Application, Infrastructure, API, Contracts) |
| Building blocks | 5 (Core, Application, Infrastructure, Security, Observability) |
| DbContexts totais | 24 |
| Migrações totais | ~100+ (confirmadas em 24 conjuntos) |
| Serviços de plataforma | 3 (ApiHost, Ingestion.Api, BackgroundWorkers) |
| TODOs/FIXMEs em módulos | ~14 (aiknowledge: 9, governance: 5) |

### Padrões Aplicados (confirmados por inspeção)

- Clean Architecture: Domain → Application → Infrastructure, sem inversão de dependência
- CQRS via MediatR: handlers separados para Commands e Queries
- `Result<T>`: erros controlados sem exceções para controlo de fluxo
- `StronglyTypedIds`: todos os IDs com `TypedIdBase<Guid>`
- `CancellationToken`: presente em handlers async
- `NexTraceGuards` estendendo Ardalis.GuardClauses
- Serilog: logging estruturado com contexto
- Minimal API pattern nos controllers

---

## Building Blocks — Estado Detalhado

### BuildingBlocks.Core — READY
`src/building-blocks/NexTraceOne.BuildingBlocks.Core/`

- `Result<T>` e `Error` com códigos estruturados
- `TypedIdBase<T>` para strongly typed IDs
- Primitivos DDD: `Entity`, `AggregateRoot`, `AuditableEntity`, `ValueObject`
- Domain events e integration events
- `NexTraceGuards` estendendo Ardalis
- Enums: `ChangeLevel`, `DiscoveryConfidence`

**Qualidade:** Zero TODOs, zero stubs.

### BuildingBlocks.Application — READY
`src/building-blocks/NexTraceOne.BuildingBlocks.Application/`

- `ICommand<T>`, `IQuery<T>`, handlers abstratos
- `ICurrentUser`, `ICurrentTenant` para contexto de segurança
- `IDateTimeProvider` para tempo testável (sem `DateTime.Now` direto)
- `IErrorLocalizer` para i18n de erros
- Extension methods para Result → HTTP response
- Paginação, correlação, CQRS behaviors (logging, validation)

### BuildingBlocks.Infrastructure — READY (com gap crítico de outbox)
`src/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure/`

- `NexTraceDbContextBase` com outbox, audit interceptor, RLS interceptor
- `TenantRlsInterceptor` — Row Level Security por tenant
- `AuditInterceptor` — auditoria automática de entidades
- `RepositoryBase<TEntity, TId>` genérico
- Outbox pattern implementado

**GAP CRÍTICO:** Apenas `IdentityDbContext` tem processamento ativo de outbox no `BackgroundWorkers`. Os outros 23 DbContexts têm tabelas de outbox sem processador. Eventos de domínio não propagam entre módulos.

### BuildingBlocks.Security — READY
`src/building-blocks/NexTraceOne.BuildingBlocks.Security/`

- JWT + API Key authentication
- AES-256-GCM encryption
- `AssemblyIntegrityChecker` (base para anti-tampering)
- CORS (5+ políticas), rate limiting (6 políticas)
- Permission-based authorization
- Environment access requirements

### BuildingBlocks.Observability — READY (config gap)
`src/building-blocks/NexTraceOne.BuildingBlocks.Observability/`

- OpenTelemetry (traces, metrics, logs)
- ClickHouse analytics writer + Null implementation
- `NexTraceHealthChecks`, `NexTraceMeters`, `NexTraceActivitySources`
- Serilog configuration

**Gap:** OpenTelemetry aponta para `localhost:4317` em config base — requer override por ambiente em produção.

---

## Módulos — Estado Detalhado

### Catalog — READY (91.7%)
`src/modules/catalog/` | 317 ficheiros | 3 DbContexts | 13 migrações

**Domain:** ApiAsset, ServiceAsset, contratos multi-protocolo (REST/SOAP/gRPC/AsyncAPI/Background), ContractVersion com SemVer, NodeHealthRecord, graph de dependências.

**Application (84 features):**
- 77 reais (100% business logic real)
- 7 stubs intencionais aguardando `IContractsModule`: `SearchCatalog`, `RenderOpenApiContract`, `GetApiHealth`, `GetMyApis`, `GetApisIConsume`, `GetApiDetail`, `GetAssetTimeline`
- Features notáveis: `ImportContract` (145 linhas), `ImportWsdlContract` (166), `ImportAsyncApiContract` (151), `ComputeSemanticDiff` (113), `EvaluateCompatibility` (138), `GenerateEvidencePack` (171)

**Gap:** `IContractsModule` definida em `NexTraceOne.Catalog.Contracts/Contracts/ServiceInterfaces/IContractsModule.cs`; `ContractsModuleService.cs` implementa a interface mas sem consumidores cross-module registados.

**Status: READY** — 91.7% real

---

### Change Governance — READY (100%)
`src/modules/changegovernance/` | 245 ficheiros | 4 DbContexts | 18 migrações

**Features confirmadas como reais:**
- BlastRadius, ChangeScores, FreezeWindows, RollbackAssessments
- Workflow: Templates, instâncias, stages, approval decisions, evidence packs, SLA policies
- Promotion: Environments, requests, gates, gate evaluations
- Ruleset Governance (Spectral lint)
- Audit trail: Decision trail, change timeline, correlation events

**Gap menor:** `RecordMitigationValidation` — validação pós-change incompleta.

**Status: READY** — módulo mais maduro

---

### Identity Access — READY (100%)
`src/modules/identityaccess/` | 185 ficheiros | 1 DbContext | 3 migrações

**Features:** Auth JWT/RBAC, multi-tenancy RLS, Sessions/Cookies, JIT privileged access, Break Glass (com expiração e auditoria), Access Reviews, Delegações, OIDC (Azure, Keycloak), TOTP/MFA.

**Value Objects validados:** `Email` (MailAddress validation), `HashedPassword`, `FullName`, `AuthenticationMode`, `MfaPolicy`, `SessionPolicy`, `DeploymentModel`.

**Status: READY**

---

### Audit Compliance — READY (100%)
`src/modules/auditcompliance/` | 56 ficheiros | 1 DbContext | 4 migrações

**Features:** Audit entry creation com hash chain SHA-256, campaign management, retention policies, compliance reporting.

**Status: READY**

---

### Configuration — READY (functional)
`src/modules/configuration/` | 67 ficheiros | 1 DbContext | 13 migrações

**Features:** Feature flags database-driven, override por tenant, parametrização persistida, quotas.

**Status: READY**

---

### Notifications — READY (coverage E2E não validada)
`src/modules/notifications/` | 124 ficheiros | 1 DbContext | 9 migrações

**Features:** Templates, delivery multi-canal (Email, Teams, Slack, SMS, Push), `MandatoryNotificationPolicy`, `TeamsNotificationDispatcher`.

**Status: READY** estruturalmente; PARTIAL em cobertura E2E validada.

---

### Operational Intelligence — PARTIAL
`src/modules/operationalintelligence/` | 275 ficheiros | 5 DbContexts | 21 migrações

**O que existe e é real:**
- `EfIncidentStore` (678 linhas) — persistência real com `IncidentDbContext`
- SLO/SLA modeling no domain com `ReliabilityDbContext`
- Lifecycle de incidentes
- `CostIntelligenceDbContext` com 7 migrações

**O que está mock/quebrado:**
- Frontend: `IncidentsPage.tsx` usa `mockIncidents` hardcoded inline — confirmado
- Runbooks: 3 hardcoded no handler; `RunbookRecord` no schema não é consultado
- `CreateMitigationWorkflow`: descarta dados sem persistir `MitigationRecord`
- Automation: handlers retornam `PreviewOnly` — sem automação real
- Reliability: 8 serviços hardcoded; `ReliabilityDbContext` não consultado
- Correlação incident↔change: seed data JSON estático, engine dinâmica ausente

**Status: PARTIAL** — backend tem estrutura; correlação, runbooks, mitigação e reliability são mock

---

### AI Knowledge — PARTIAL
`src/modules/aiknowledge/` | 287 ficheiros | 3 DbContexts | 11 migrações

**O que é real:**
- AI Governance: modelos, políticas, budgets, access policies (`AiGovernanceDbContext`)
- Model registry, `AiTokenUsageLedger`
- 3 AI tools: `list_services`, `get_service_health`, `list_recent_changes`
- State machines de agentes (Draft → Active → Published)
- Grounding context builders (estrutura real)

**O que está mock/quebrado:**
- `SendAssistantMessage`: retorna respostas hardcoded sem chamada real a LLM
- ExternalAI: 8 handlers com `TODO: Phase 03.x` — `IExternalAIRoutingPort` é abstração sem implementação real
- `AiAssistantPage`: `mockConversations` hardcoded no frontend

**TODOs confirmados:** 9 (6 em ExternalAI Phase 03.x, 3 em Orchestration metadata Phase 02.6)

**Status: PARTIAL** — AI Governance sólido; AI Assistant e ExternalAI quebrados ponta a ponta

---

### Governance — MOCK (intencional)
`src/modules/governance/` | 143 ficheiros | 1 DbContext | 3 migrações

**Estado confirmado por inspeção:** 22+ ficheiros `.cs` no módulo contêm `IsSimulated: true`. Aproximadamente 74 handlers retornam dados fabricados. `GovernanceDbContext` existe com 3 migrações mas não é consultado pelos handlers para dados reais.

**Áreas afetadas:** Teams, Domains, FinOps (GetDomainFinOps, GetServiceFinOps, GetFinOpsTrends), Benchmarking, Waivers, Evidence Packages, Risk, Compliance, Policy Catalog, Executive Views, DelegatedAdmin.

**TODOs:** 5 (P03.5 — platform readiness probes, queue stats, job tracking, event stats, pack coverage)

**Status: MOCK** — módulo inteiro retorna dados simulados

---

### Knowledge — INCOMPLETE
`src/modules/knowledge/` | 34 ficheiros | 1 DbContext | 3 migrações

**Features básicas:** `CreateKnowledgeDocument`, `GetKnowledgeByRelationTarget`, CRUD básico.

**Gap:** Módulo pequeno. Knowledge Hub sem conectividade cross-module para servir contexto ao AI Assistant. Source of Truth incompleto.

**Status: INCOMPLETE**

---

### Integrations — STUB
`src/modules/integrations/` | 35 ficheiros | 1 DbContext | 3 migrações

**Gap:** Conectores externos (GitLab, Jenkins, GitHub, Azure DevOps) são stubs sem lógica real de ingestão de eventos CI/CD.

**Status: STUB**

---

### Product Analytics — MOCK
`src/modules/productanalytics/` | 26 ficheiros | 1 DbContext | 3 migrações

**Gap:** 100% mock. Sem event tracking pipeline real.

**Status: MOCK**

---

## Gaps Transversais Críticos

| Gap | Impacto | Ficheiro |
|---|---|---|
| Outbox processado só em IdentityDbContext | Eventos não propagam em 23 outros DbContexts | `src/platform/NexTraceOne.BackgroundWorkers/` |
| `IContractsModule` sem consumidores cross-module | Developer Portal e Search bloqueados | `NexTraceOne.Catalog.Contracts/Contracts/ServiceInterfaces/IContractsModule.cs` |
| `SendAssistantMessage` hardcoded | AI Assistant inoperante ponta a ponta | `NexTraceOne.AIKnowledge.Application/Features/SendAssistantMessage/` |
| Engine correlação incident↔change ausente | Fluxo 3 inoperante | `src/modules/operationalintelligence/` |
| Governance `IsSimulated: true` em 22+ ficheiros | Pilar Governance vazio | `src/modules/governance/NexTraceOne.Governance.Application/Features/` |
| 516 warnings CS8632 nullable | Risco NullReferenceException em runtime | Transversal |

---

## Recomendações Prioritárias

1. **Crítico:** Implementar engine correlação dinâmica incident↔change
2. **Crítico:** Conectar `SendAssistantMessage` ao `IExternalAIRoutingPort` → Ollama
3. **Alta:** Ativar outbox processing para Catalog e ChangeGovernance no BackgroundWorkers
4. **Alta:** Substituir Governance handlers mock por queries reais
5. **Alta:** Completar 8 stubs ExternalAI
6. **Média:** Resolver 516 warnings CS8632 nullable

---

*Data: 28 de Março de 2026*
