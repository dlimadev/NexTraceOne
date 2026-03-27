# Notifications — Validação do Fluxo Ponta a Ponta de Delivery

> **Módulo:** 11 — Notifications  
> **Data:** 2026-03-25  
> **Fase:** N8-R — Reexecução completa  
> **Estado:** ✅ FINALIZADO

---

## 1. Fluxo completo de delivery

### Diagrama do pipeline

```
Módulo Emissor (ex: Change Gov, OI, Catalog)
  ↓ (Domain Event / Integration Event)
Event Handler (ex: IncidentNotificationHandler)
  ↓ (NotificationRequest)
INotificationModule.SubmitAsync()
  ↓
NotificationOrchestrator.ProcessAsync()
  ├── 1. Validação (EventType, SourceModule, TenantId)
  ├── 2. Resolução de recipientes (RecipientResolver)
  ├── 3. Extracção de parâmetros (PayloadJson → Dictionary)
  ├── 4. Resolução de template (NotificationTemplateResolver)
  ├── 5. POR CADA recipiente:
  │     ├── 5a. Deduplicação (5-min window)
  │     ├── 5b. Criação da entidade Notification
  │     ├── 5c. Persistência (NotificationStore.AddAsync)
  │     └── 5d. Delivery externa (ExternalDeliveryService)
  │           ├── Routing (NotificationRoutingEngine)
  │           ├── Template externo (ExternalChannelTemplateResolver)
  │           ├── Email (EmailNotificationDispatcher)
  │           └── Teams (TeamsNotificationDispatcher)
  │           └── Persistência do delivery (NotificationDeliveryStore)
  └── 6. SaveChangesAsync()
```

---

## 2. Análise etapa por etapa

### 2.1 Evento/Origem

| Aspecto | Estado | Detalhes |
|---------|--------|---------|
| Event handlers para 8 módulos | ✅ Real | `CatalogNotificationHandler`, `IncidentNotificationHandler`, `ApprovalNotificationHandler`, `ComplianceNotificationHandler`, `AiGovernanceNotificationHandler`, `IntegrationFailureNotificationHandler`, `BudgetNotificationHandler`, `SecurityNotificationHandler` |
| Localização | ✅ Real | `src/modules/notifications/NexTraceOne.Notifications.Infrastructure/EventHandlers/` |
| Conversão evento → NotificationRequest | ✅ Real | Cada handler mapeia o evento do domínio para `NotificationRequest` |
| 25+ tipos de evento suportados | ✅ Real | Enum `NotificationType.cs` (3970 linhas) |

**Ficheiros concretos:**
- `CatalogNotificationHandler.cs`
- `IncidentNotificationHandler.cs`
- `ApprovalNotificationHandler.cs`
- `ComplianceNotificationHandler.cs`
- `AiGovernanceNotificationHandler.cs`
- `IntegrationFailureNotificationHandler.cs`
- `BudgetNotificationHandler.cs`
- `SecurityNotificationHandler.cs`

### 2.2 Composição da mensagem

| Aspecto | Estado | Detalhes |
|---------|--------|---------|
| Extracção de parâmetros | ✅ Real | `NotificationOrchestrator` linhas 53, 180-213 — extrai de PayloadJson |
| Parâmetros base | ✅ Real | EntityName, EntityId, SourceModule — sempre incluídos |
| Parâmetros adicionais | ✅ Real | Deserializados de PayloadJson via `JsonSerializer` |

### 2.3 Resolução de template

| Aspecto | Estado | Detalhes |
|---------|--------|---------|
| Template resolver interno | ✅ Real | `NotificationTemplateResolver.cs` (183 linhas) |
| 13 templates definidos | ✅ Real | IncidentCreated, IncidentEscalated, ApprovalPending, ApprovalApproved, ApprovalRejected, BreakGlassActivated, JitAccessPending, ComplianceCheckFailed, BudgetExceeded, IntegrationFailed, AiProviderUnavailable + generic fallback |
| Override por request | ✅ Real | Se `NotificationRequest` inclui Title+Message, template é ignorado |
| Template externo (Email) | ✅ Real | `ExternalChannelTemplateResolver.ResolveEmailTemplate()` — HTML + plain text |
| Template externo (Teams) | ✅ Real | `ExternalChannelTemplateResolver.ResolveTeamsTemplate()` — Adaptive Card JSON |

### 2.4 Seleção de canal

| Aspecto | Estado | Detalhes |
|---------|--------|---------|
| Routing engine | ✅ Real | `NotificationRoutingEngine.cs` |
| Preferências do utilizador | ✅ Real | Consulta `NotificationPreference` por categoria/canal |
| Mandatory notification policy | ✅ Real | `MandatoryNotificationPolicy.cs` — notificações críticas ignoram preferências |
| Quiet hours check | ✅ Real | `QuietHoursService.cs` — pode marcar como Skipped |
| Suppression check | ✅ Real | `NotificationSuppressionService.cs` — pode suprimir |

### 2.5 Envio

| Aspecto | Estado | Detalhes |
|---------|--------|---------|
| Dispatch multi-canal | ✅ Real | `INotificationChannelDispatcher` implementado por Email e Teams |
| Email dispatcher | ✅ Real | `EmailNotificationDispatcher.cs` |
| Teams dispatcher | ✅ Real | `TeamsNotificationDispatcher.cs` |
| Delivery record creation | ✅ Real | `NotificationDelivery.Create()` com status Pending |
| Graceful failure | ✅ Real | Falha de delivery não bloqueia notificação interna |

### 2.6 Retry

| Aspecto | Estado | Detalhes |
|---------|--------|---------|
| RetryCount field | ✅ Real | `NotificationDelivery.RetryCount` |
| IncrementRetry() | ✅ Real | Método de domínio implementado |
| DeliveryRetryOptions | ✅ Real | Configuração de retry disponível |
| Retry automático via scheduler | ❌ Ausente | **Sem background job para retries** |
| Retry manual via API | ❌ Ausente | **Sem endpoint de retry** |

### 2.7 Actualização de status

| Aspecto | Estado | Detalhes |
|---------|--------|---------|
| MarkDelivered() | ✅ Real | Sets DeliveredAt + Status = Delivered |
| MarkFailed(errorMessage) | ✅ Real | Sets FailedAt + ErrorMessage + Status = Failed |
| MarkSkipped() | ✅ Real | Sets Status = Skipped |
| Persistência do status | ✅ Real | Via `NotificationDeliveryStoreRepository` |

### 2.8 Auditoria

| Aspecto | Estado | Detalhes |
|---------|--------|---------|
| Audit service | ✅ Real | `NotificationAuditService.cs` |
| Domain events | ✅ Real | NotificationCreatedEvent, NotificationDeliveryCompletedEvent, NotificationReadEvent |
| Integration events | ✅ Real | 3 integration events publicados via outbox |
| AuditInterceptor | ✅ Real | CreatedAt/By/UpdatedAt/By via NexTraceDbContextBase |
| Audit logging | ✅ Real | ILogger com contexto completo no orchestrator |

### 2.9 Visualização/Consulta

| Aspecto | Estado | Detalhes |
|---------|--------|---------|
| Lista de notificações (API) | ✅ Real | `GET /api/v1/notifications` com filtros |
| Contagem não lidas (API) | ✅ Real | `GET /api/v1/notifications/unread-count` |
| NotificationCenterPage (UI) | ✅ Real | Polling 30s, filtros por status/categoria/severidade |
| NotificationBell (header) | ✅ Real | Badge com contagem + dropdown recentes |
| Delivery history (API) | ❌ Ausente | Sem endpoint para consultar entregas |
| Delivery dashboard (UI) | ❌ Ausente | Sem visualização de status de entrega |

---

## 3. Resumo: O que funciona vs. o que está ausente

### ✅ O que já funciona (de ponta a ponta)

1. Evento de outro módulo → Event handler → NotificationRequest → Orchestrator
2. Validação de request + resolução de recipientes
3. Resolução de template (13 tipos internos + Email HTML + Teams Adaptive Card)
4. Deduplicação com janela de 5 minutos
5. Criação e persistência da notificação interna
6. Dispatch para Email e Teams com tratamento graceful de falhas
7. Tracking de delivery (Pending → Delivered/Failed/Skipped)
8. Visualização in-app (NotificationCenterPage + NotificationBell)
9. Preferências por utilizador/categoria/canal
10. Mandatory notification policy (notificações críticas não desactiváveis)

### ⚠️ O que é parcial

1. Retries — IncrementRetry() existe, mas sem scheduler automático
2. Escalação — Serviço existe, mas sem scheduler
3. Digest — Serviço existe, mas sem scheduler
4. Quiet Hours — Serviço existe, depende de Configuration module

### ❌ O que está ausente

1. **Background workers** para retry, escalação e digest (sem Hangfire/Quartz/HostedService)
2. **Endpoint de retry manual** para re-trigger delivery falhada
3. **Endpoint de delivery history** para consultar status de entregas
4. **Dashboard de delivery** no frontend
5. **Permissões no RolePermissionCatalog** — nenhuma role tem `notifications:*`
6. **Migrations** — zero EF migrations, tabelas não criadas oficialmente
7. **Template editor** — templates são in-code, sem UI de gestão
8. **Configuração dinâmica da janela de dedup** — hardcoded a 5 minutos
9. **Filtro por EnvironmentId** nos endpoints de consulta

### 🔧 O que é cosmético (mas existe no código)

1. `NotificationConfigurationPage.tsx` — página de configuração admin com 6 secções, mas sem backend real de configuração de templates/canais — depende do módulo Configuration para settings
