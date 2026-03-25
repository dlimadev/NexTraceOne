# Notifications — Finalização do Escopo Funcional

> **Módulo:** 11 — Notifications  
> **Data:** 2026-03-25  
> **Fase:** N8-R — Reexecução completa  
> **Estado:** ✅ FINALIZADO

---

## 1. Escopo funcional do módulo

O módulo Notifications cobre o ciclo completo de notificação no NexTraceOne: desde a recepção de um evento de outro módulo até à entrega ao utilizador final, com rastreabilidade, preferências e inteligência.

---

## 2. Capacidades funcionais

### 2.1 Templates de notificação

| Capacidade | Estado | Implementação |
|-----------|--------|--------------|
| Templates internos por EventType | ✅ Real | `NotificationTemplateResolver.cs` — 13 templates definidos |
| Templates de e-mail (HTML + plain text) | ✅ Real | `ExternalChannelTemplateResolver.cs` — `ResolveEmailTemplate()` |
| Templates de Teams (Adaptive Card) | ✅ Real | `ExternalChannelTemplateResolver.cs` — `ResolveTeamsTemplate()` |
| Parametrização dinâmica | ✅ Real | Substituição de `{EntityName}`, `{EntityId}`, `{SourceModule}` |
| Override de template (título/mensagem directa) | ✅ Real | `NotificationOrchestrator` aceita Title+Message override |
| Templates persistidos como entidade | ❌ Ausente | Templates são in-code, não em base de dados |
| Editor visual de templates | ❌ Ausente | Não existe UI para editar templates |

### 2.2 Canais de entrega

| Canal | Estado | Implementação |
|-------|--------|--------------|
| In-app (central de notificações) | ✅ Real | `Notification` entity + `NotificationCenterPage` |
| Email | ✅ Real | `EmailNotificationDispatcher.cs` |
| Microsoft Teams | ✅ Real | `TeamsNotificationDispatcher.cs` |
| Webhook | ❌ Ausente | Não implementado |
| SMS | ❌ Ausente | Fora do escopo actual |

### 2.3 Envio síncrono/assíncrono

| Capacidade | Estado | Detalhes |
|-----------|--------|---------|
| Notificação interna (síncrona) | ✅ Real | Criada e persistida no mesmo fluxo |
| Delivery externa (assíncrona) | ⚠️ Parcial | Chamada no mesmo request, mas com tratamento de erro graceful |
| Background job para delivery | ❌ Ausente | Sem background worker; delivery é fire-and-forget no request |
| Queue/outbox para delivery | ❌ Ausente | Não usa outbox pattern para external delivery |

### 2.4 Retries

| Capacidade | Estado | Detalhes |
|-----------|--------|---------|
| RetryCount tracking | ✅ Real | Campo `RetryCount` na entidade `NotificationDelivery` |
| IncrementRetry() | ✅ Real | Método de domínio em `NotificationDelivery.cs` |
| Retry automático | ⚠️ Parcial | `DeliveryRetryOptions` configurado, mas sem background retry scheduler |
| Retry manual endpoint | ❌ Ausente | Sem endpoint para re-trigger delivery |
| Dead letter | ❌ Ausente | Sem conceito de dead letter queue |

### 2.5 Status de entrega

| Status | Implementado | Usado em |
|--------|-------------|---------|
| Pending | ✅ | `NotificationDelivery.Create()` — estado inicial |
| Delivered | ✅ | `MarkDelivered()` — entrega confirmada |
| Failed | ✅ | `MarkFailed(errorMessage)` — falha com razão |
| Skipped | ✅ | `MarkSkipped()` — canal desactivado ou quiet hours |

### 2.6 Histórico de envio

| Capacidade | Estado | Detalhes |
|-----------|--------|---------|
| Persistência de delivery records | ✅ Real | Tabela `ntf_deliveries` |
| Timestamps (created/delivered/failed) | ✅ Real | 3 timestamps na entidade |
| Error message tracking | ✅ Real | Campo `ErrorMessage` (max 4000 chars) |
| Consulta de histórico (API) | ❌ Ausente | Sem endpoint GET para delivery history |
| Dashboard de delivery (frontend) | ❌ Ausente | Sem visualização de status de entrega |

### 2.7 Notificações por tipo

| Tipo | Event Handler | Eventos |
|------|-------------|---------|
| Incident/Operations | `IncidentNotificationHandler.cs` | IncidentCreated, IncidentEscalated, IncidentResolved |
| Approval/Change | `ApprovalNotificationHandler.cs` | ApprovalPending, ApprovalApproved, ApprovalRejected, ApprovalExpiring |
| Contract/Catalog | `CatalogNotificationHandler.cs` | ContractPublished, BreakingChangeDetected, ContractValidationFailed |
| Security | `SecurityNotificationHandler.cs` | BreakGlassActivated, SecurityIncident, UnauthorizedAccess |
| Compliance | `ComplianceNotificationHandler.cs` | ComplianceViolation, PolicyViolation |
| AI Governance | `AiGovernanceNotificationHandler.cs` | AiGovernancePolicyViolation, AiCostAnomaly |
| Integration | `IntegrationFailureNotificationHandler.cs` | IntegrationFailure |
| Budget/FinOps | `BudgetNotificationHandler.cs` | BudgetAlert, AnomalyDetected |

### 2.8 Rastreabilidade por ambiente

| Capacidade | Estado | Detalhes |
|-----------|--------|---------|
| EnvironmentId na notificação | ✅ Real | Campo opcional em `Notification` entity |
| Filtro por environment (API) | ❌ Ausente | Sem filtro de environment nos endpoints |
| Contexto de environment na UI | ❌ Ausente | Sem indicação visual de environment |

### 2.9 Deduplicação

| Capacidade | Estado | Detalhes |
|-----------|--------|---------|
| Janela de deduplicação | ✅ Real | 5 minutos via `INotificationDeduplicationService` |
| Dedup por evento+recipiente | ✅ Real | Verifica combinação EventType+RecipientUserId |
| Configuração da janela | ❌ Ausente | Hardcoded a 5 minutos |

### 2.10 Agrupamento e Digest

| Capacidade | Estado | Detalhes |
|-----------|--------|---------|
| GroupId field | ✅ Real | Campo `GroupId` na entidade Notification |
| Grouping service | ✅ Real | `NotificationGroupingService.cs` |
| Digest service | ✅ Real | `NotificationDigestService.cs` |
| Digest scheduled | ❌ Ausente | Sem background scheduler para gerar digests |

### 2.11 Escalação

| Capacidade | Estado | Detalhes |
|-----------|--------|---------|
| MarkAsEscalated() | ✅ Real | Método de domínio na entidade |
| Escalation service | ✅ Real | `NotificationEscalationService.cs` |
| Auto-escalation rules | ⚠️ Parcial | Serviço existe, mas sem scheduler automático |

### 2.12 Quiet Hours e Supressão

| Capacidade | Estado | Detalhes |
|-----------|--------|---------|
| Quiet hours service | ✅ Real | `QuietHoursService.cs` |
| Suppression service | ✅ Real | `NotificationSuppressionService.cs` |
| Suppress() domain method | ✅ Real | `Notification.Suppress(reason)` |
| Quiet hours config | ⚠️ Parcial | Depende de Configuration module (`notifications.quiet_hours.*`) |

### 2.13 Preferências por utilizador

| Capacidade | Estado | Detalhes |
|-----------|--------|---------|
| Preferência por categoria/canal | ✅ Real | `NotificationPreference` entity |
| API GET/PUT preferences | ✅ Real | 2 endpoints dedicados |
| Frontend de preferências | ✅ Real | `NotificationPreferencesPage.tsx` |
| Mandatory notification policy | ✅ Real | `MandatoryNotificationPolicy.cs` — notificações críticas não desactiváveis |
| Unique constraint | ✅ Real | (TenantId, UserId, Category, Channel) unique index |

### 2.14 Impacto em permissões

| Capacidade | Estado | Detalhes |
|-----------|--------|---------|
| 4 permissões definidas nos endpoints | ✅ Real | inbox:read/write, preferences:read/write |
| Permissões no RolePermissionCatalog | 🔴 Ausente | **BLOCKER** — nenhuma role tem permissões de notificações |

### 2.15 Configuração por tenant

| Capacidade | Estado | Detalhes |
|-----------|--------|---------|
| TenantId em Notification | ✅ Real | Campo obrigatório |
| TenantId em Preferences | ✅ Real | Campo obrigatório |
| RLS via NexTraceDbContextBase | ✅ Real | Herdado do base context |
| Config keys por tenant | ⚠️ Parcial | Depende do módulo Configuration |

---

## 3. Sumário de cobertura funcional

| Área | Cobertura |
|------|----------|
| Templates (in-code) | 🟢 80% |
| Canais (Email/Teams) | 🟢 80% |
| Delivery tracking | 🟡 60% |
| Retries | 🟠 40% |
| Status de entrega | 🟢 85% |
| Histórico | 🟠 50% |
| Deduplicação | 🟢 80% |
| Preferências | 🟢 90% |
| Escalação | 🟡 60% |
| Quiet Hours/Supressão | 🟡 60% |
| Auditoria | 🟡 60% |
| Rastreabilidade | 🟡 65% |
| **Global** | **🟡 68%** |
