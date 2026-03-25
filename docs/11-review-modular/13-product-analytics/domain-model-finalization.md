# PARTE 4 — Modelo de Domínio Final do Módulo Product Analytics

> **Data**: 2026-03-25
> **Prompt**: N12 — Consolidação do módulo Product Analytics
> **Estado**: DEFINIÇÃO FINAL

---

## 1. Estado atual do domínio

### Aggregate Roots atuais

| Aggregate Root | Ficheiro | LOC | Status |
|---------------|---------|-----|--------|
| AnalyticsEvent | `Governance.Domain/Entities/AnalyticsEvent.cs` | ~80 | ✅ Existente (em Governance) |

### Entidades atuais

| Entidade | Tipo | Descrição |
|---------|------|-----------|
| AnalyticsEvent | Aggregate Root | Evento de uso do produto com ID tipado (AnalyticsEventId) |

### Enums persistidos atuais

| Enum | Ficheiro | Valores | Status |
|------|---------|---------|--------|
| AnalyticsEventType | `Governance.Domain/Enums/AnalyticsEventType.cs` | 25 valores | ✅ Bem definido |

### Valores do AnalyticsEventType

| Valor | Nome | Categoria |
|-------|------|-----------|
| 0 | ModuleViewed | Navigation |
| 1 | EntityViewed | Navigation |
| 2 | SearchExecuted | Search |
| 3 | SearchResultClicked | Search |
| 4 | ZeroResultSearch | Friction |
| 5 | QuickActionTriggered | Action |
| 6 | AssistantPromptSubmitted | AI |
| 7 | AssistantResponseUsed | AI |
| 8 | ContractDraftCreated | Contract |
| 9 | ContractPublished | Contract |
| 10 | ChangeViewed | Change |
| 11 | IncidentInvestigated | Operations |
| 12 | MitigationWorkflowStarted | Operations |
| 13 | MitigationWorkflowCompleted | Operations |
| 14 | EvidencePackageExported | Governance |
| 15 | PolicyViewed | Governance |
| 16 | ExecutiveOverviewViewed | Executive |
| 17 | RunbookViewed | Operations |
| 18 | SourceOfTruthQueried | Knowledge |
| 19 | ReportGenerated | Reporting |
| 20 | OnboardingStepCompleted | Onboarding |
| 21 | JourneyAbandoned | Friction |
| 22 | EmptyStateEncountered | Friction |
| 23 | ReliabilityDashboardViewed | Operations |
| 24 | AutomationWorkflowManaged | Operations |

### Value Objects atuais

Nenhum value object dedicado ao módulo.

---

## 2. Análise de lacunas no domínio

### Entidades anémicas

| Entidade | Problema | Recomendação |
|---------|----------|--------------|
| AnalyticsEvent | Apenas propriedades, sem métodos de domínio significativos | ⚠️ Aceitável — eventos analíticos são naturalmente data-centric; adicionar factory method e validação de criação |

### Regras de negócio fora do lugar

| Regra | Localização atual | Deveria estar em |
|-------|-------------------|------------------|
| Validação de evento | `RecordAnalyticsEvent.Validator` (Governance.Application) | ProductAnalytics.Application |
| Cálculo de friction score | `GetFrictionIndicators.Handler` | ProductAnalytics.Application |
| Cálculo de adoption score | `GetAnalyticsSummary.Handler` | ProductAnalytics.Application |
| Mock data de personas | `GetPersonaUsage.Handler` (hardcoded) | **Deve ser eliminado** |

### Campos ausentes na entidade AnalyticsEvent

| Campo | Tipo | Justificação |
|-------|------|-------------|
| EnvironmentId | Guid? | Correlação com ambiente (dev/staging/prod) |
| Duration | int? | Duração da ação em milliseconds |
| ParentEventId | AnalyticsEventId? | Correlação entre eventos (ex: search → click) |
| Source | string | Origem do evento (frontend/backend/api) |
| AppVersion | string? | Versão do frontend que gerou o evento |

### Campos indevidos

Nenhum campo indevido identificado. O domínio atual é limpo mas incompleto.

---

## 3. Relações com outros módulos

### Relações com Governance

| Tipo | Descrição | Estado | Alvo |
|------|-----------|--------|------|
| Acoplamento físico | Backend inteiro dentro de Governance | 🔴 Blocker | Extrair para módulo próprio |
| DbContext partilhado | Usa GovernanceDbContext | 🔴 Blocker | ProductAnalyticsDbContext próprio |
| Dependência conceitual | Nenhuma (não depende de policies/waivers) | ✅ OK | Manter independência |

### Relações com Identity & Access

| Tipo | Descrição | Estado |
|------|-----------|--------|
| Contextual | Usa TenantId e UserId do JWT | ✅ OK (via interceptors) |
| Persona | Captura persona do utilizador | ✅ OK |

### Relações com Notifications

| Tipo | Descrição | Estado |
|------|-----------|--------|
| Nenhuma | Product Analytics não emite nem consome notificações | ✅ OK |

### Relações com Operational Intelligence

| Tipo | Descrição | Estado |
|------|-----------|--------|
| Emissão de eventos | OI pode emitir eventos capturados por analytics (ex: IncidentInvestigated) | ⚠️ Depende de instrumentação |

### Relações com todos os módulos (como emissor de eventos)

| Módulo | Eventos relevantes | Status de instrumentação |
|--------|-------------------|------------------------|
| Catalog | EntityViewed, SearchExecuted | ❌ Não instrumentado |
| Contracts | ContractDraftCreated, ContractPublished | ❌ Não instrumentado |
| Change Governance | ChangeViewed | ❌ Não instrumentado |
| Operational Intelligence | IncidentInvestigated, RunbookViewed | ❌ Não instrumentado |
| AI & Knowledge | AssistantPromptSubmitted, AssistantResponseUsed | ❌ Não instrumentado |
| Governance | PolicyViewed, EvidencePackageExported | ❌ Não instrumentado |
| Frontend (geral) | ModuleViewed | ✅ Via AnalyticsEventTracker |

---

## 4. Modelo final do domínio

### Aggregate Roots (alvo)

| Aggregate Root | Descrição | Persistência |
|---------------|-----------|-------------|
| **AnalyticsEvent** | Evento atómico de uso do produto | PostgreSQL (buffer) + ClickHouse (permanente) |
| **AnalyticsDefinition** (NOVO) | Configuração de métricas, journeys, milestones | PostgreSQL |

### Entidades (alvo)

| Entidade | Tipo | Descrição |
|---------|------|-----------|
| AnalyticsEvent | Aggregate Root | Evento de uso com campos enriquecidos |
| AnalyticsDefinition | Aggregate Root | Definição de métrica/journey/milestone |
| JourneyStep | Entity (child of AnalyticsDefinition) | Passo de um funnel de jornada |
| ValueMilestone | Entity (child of AnalyticsDefinition) | Definição de milestone de valor |

### Enums (alvo)

| Enum | Valores | Persistência |
|------|---------|-------------|
| AnalyticsEventType | 25+ valores (manter e expandir) | int (PostgreSQL + ClickHouse) |
| AnalyticsDefinitionType (NOVO) | Metric, Journey, Milestone, KPI | int |
| EventSource (NOVO) | Frontend, Backend, API, System | string |
| AnalyticsScope (NOVO) | Product, Module, Feature, Persona | string |

### Value Objects (alvo)

| Value Object | Descrição |
|-------------|-----------|
| EventMetadata | JSON structured metadata do evento |
| TimeRange | Período temporal para queries |
| AdoptionScore | Score composto de adoção (0-100) |

### Campos finais da entidade AnalyticsEvent

| Campo | Tipo | Obrigatório | Notas |
|-------|------|-------------|-------|
| Id | AnalyticsEventId (Guid) | ✅ | PK |
| TenantId | Guid | ✅ | RLS |
| UserId | Guid | ✅ | Quem executou |
| Persona | string(50) | ✅ | Persona do utilizador |
| Module | string(100) | ✅ | Módulo onde ocorreu |
| EventType | AnalyticsEventType (int) | ✅ | Tipo de evento |
| Feature | string(200) | ❌ | Feature específica |
| EntityType | string(100) | ❌ | Tipo de entidade |
| Outcome | string(100) | ❌ | Resultado da ação |
| Route | string(500) | ❌ | URL/rota |
| TeamId | Guid? | ❌ | Equipa contexto |
| DomainId | Guid? | ❌ | Domínio contexto |
| SessionId | string(50) | ❌ | ID de sessão |
| ClientType | string(50) | ❌ | Tipo de cliente |
| MetadataJson | string(2000) | ❌ | Metadata flexível |
| OccurredAt | DateTimeOffset | ✅ | Timestamp UTC |
| EnvironmentId | Guid? | ❌ | **NOVO** - Ambiente |
| Duration | int? | ❌ | **NOVO** - Duração ms |
| ParentEventId | Guid? | ❌ | **NOVO** - Evento pai |
| Source | string(20) | ✅ | **NOVO** - Origem (Frontend/Backend) |
| CreatedAt | DateTimeOffset | ✅ | Auditoria (via interceptor) |
| CreatedBy | string | ✅ | Auditoria (via interceptor) |

### Campos da entidade AnalyticsDefinition (NOVA)

| Campo | Tipo | Obrigatório | Notas |
|-------|------|-------------|-------|
| Id | Guid | ✅ | PK |
| TenantId | Guid | ✅ | RLS |
| Name | string(200) | ✅ | Nome da definição |
| Type | AnalyticsDefinitionType | ✅ | Metric/Journey/Milestone/KPI |
| Scope | AnalyticsScope | ✅ | Product/Module/Feature/Persona |
| Description | string(1000) | ❌ | Descrição |
| ConfigurationJson | string(4000) | ❌ | Configuração flexível |
| IsActive | bool | ✅ | Ativo ou não |
| CreatedAt/UpdatedAt | DateTimeOffset | ✅ | Auditoria |
| CreatedBy/UpdatedBy | string | ✅ | Auditoria |
| RowVersion | uint | ✅ | Concorrência |

---

## 5. Domain Events (alvo)

| Evento | Quando | Consumidores |
|--------|--------|-------------|
| AnalyticsEventRecorded | Após gravar evento | ClickHouse writer, Audit |
| AnalyticsDefinitionCreated | Após criar definição | Audit |
| AnalyticsDefinitionUpdated | Após atualizar definição | Audit |
| FrictionThresholdExceeded | Quando friction > threshold | Notifications |
| AdoptionDropDetected | Quando adoption desce | Notifications |
