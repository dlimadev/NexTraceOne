# P7.3 — Notification Audit Correlation: Origin → Notification → Delivery

**Data:** 2026-03-27
**Fase:** P7.3 — Fechar geração de eventos auditáveis e correlação origem → notificação → entrega

---

## 1. Objetivo

Fechar a rastreabilidade auditável do módulo Notifications para garantir que:

- toda notificação possa ser ligada ao evento que a originou (via `SourceEventId`);
- toda tentativa de entrega seja ligada à notificação correspondente (via `NotificationId` em `NotificationDelivery`);
- o fluxo gere **AuditEvent reais** persistidos com hash chain SHA-256 através do módulo Audit existente (`IAuditModule`);
- o backend exponha consultas funcionais dessa correlação (`/notifications/{id}/trail`);
- o frontend possa consumir a trilha de rastreabilidade.

---

## 2. Estado Anterior

| Aspecto | Estado antes do P7.3 |
|---|---|
| `Notification.SourceEventId` | Não existia — sem ligação explícita ao evento de origem |
| `NotificationAuditService` | Apenas logging estruturado (`ILogger`) — sem persistência real |
| `IAuditModule` em Notifications | Não referenciado — sem ponte para o Audit module |
| Endpoint de trilha | Não existia |
| Audit events no fluxo | Inexistentes — nenhum `AuditEvent` gerado por notificações |

---

## 3. Modelo de Correlação Adotado

```
Evento Externo (Incidente, Deploy, Aprovação...)
   │
   │  SourceEventId = "incident-123"  ← ligação explícita ao evento de origem
   ▼
Notification (ntf_notifications)
   │  NotificationId
   │
   ├──▶ NotificationDelivery (ntf_notification_deliveries)
   │       Channel, Status, RetryCount, LastAttemptAt, NextRetryAt
   │       ← ligação via NotificationId (já existia desde P7.2)
   │
   └──▶ AuditEvent (aud_audit_events)
            sourceModule = "notifications"
            actionType   = "notification.created" / "notification.critical.delivered" / ...
            resourceId   = NotificationId ou DeliveryId
            hash chain SHA-256 preservada
```

### Identificadores de correlação

| Campo | Onde fica | Propósito |
|---|---|---|
| `SourceEventId` | `Notification.SourceEventId` (novo P7.3) | Liga notificação ao evento de origem |
| `NotificationId` | `NotificationDelivery.NotificationId` | Liga entrega à notificação |
| `SourceModule` | `Notification.SourceModule` | Identifica o módulo que originou |
| `SourceEntityId` | `Notification.SourceEntityId` | Id da entidade de negócio de origem |
| `EventType` | `Notification.EventType` | Tipo do evento (e.g., `IncidentCreated`) |

---

## 4. Ficheiros Alterados

### Domínio

| Ficheiro | Alteração |
|---|---|
| `Notification.cs` | Adicionado `string? SourceEventId` (property + constructor + `Create()`) |

### Contratos

| Ficheiro | Alteração |
|---|---|
| `INotificationModule.cs` (Contracts) | Adicionado `string? SourceEventId` a `NotificationRequest` |

### Aplicação

| Ficheiro | Alteração |
|---|---|
| `INotificationAuditService.cs` | Adicionados 4 novos `NotificationAuditActions`: `NotificationGenerated`, `NotificationDelivered`, `NotificationDeliveryFailed`, `NotificationDeliveryRetryScheduled` |
| `NotificationOrchestrator.cs` | Injetado `INotificationAuditService`; passagem de `SourceEventId` ao `Notification.Create()`; chamada de auditoria após criação de cada notificação; helper `RecordAuditAsync()` best-effort |
| `GetNotificationTrail.cs` (novo) | Feature CQRS: `Query` + `Handler` + DTOs para trilha de correlação completa |

### Infraestrutura

| Ficheiro | Alteração |
|---|---|
| `NotificationAuditService.cs` | Substituído logging por chamada real a `IAuditModule.RecordEventAsync()` com payload combinado; best-effort (não bloqueia fluxo principal) |
| `ExternalDeliveryService.cs` | Injetado `INotificationAuditService`; chamada de auditoria após `MarkDelivered`, `MarkFailed` e `ScheduleRetry` |
| `NexTraceOne.Notifications.Infrastructure.csproj` | Adicionada referência a `NexTraceOne.AuditCompliance.Contracts` (P7.3) |
| `NotificationConfiguration.cs` (EF) | Adicionado mapeamento de `SourceEventId` (`HasMaxLength(500)`) |
| `DependencyInjection.cs` | Sem alteração estrutural — `IAuditModule` é resolvido automaticamente por DI (já registado por `AuditCompliance.Infrastructure`) |

### Migração

| Ficheiro | Alteração |
|---|---|
| `P7_3_NotificationSourceEventId` | Migração EF Core: adiciona coluna `source_event_id` (`varchar(500)`, nullable) à tabela `ntf_notifications` |

### API

| Ficheiro | Alteração |
|---|---|
| `NotificationCenterEndpointModule.cs` | Adicionado `GET /notifications/{id}/trail` com permissão `notifications:delivery:read` |

### Frontend

| Ficheiro | Alteração |
|---|---|
| `types.ts` | Adicionados `NotificationCorrelationDto`, `DeliveryTrailEntryDto`, `NotificationTrailResponse` |
| `notifications.ts` (api) | Adicionado `getNotificationTrail()` |
| `useNotificationConfiguration.ts` | Adicionado hook `useNotificationTrail()` |
| `index.ts` | Exportados novos tipos e hook |

### Testes

| Ficheiro | Alteração |
|---|---|
| `NotificationAuditServiceTests.cs` | Reescrito: mock de `IAuditModule`, verifica chamadas reais com `.Received()`, testa best-effort (módulo que falha não propaga exceção), cobre todos os action types (13) |
| `NotificationTests.cs` | Adicionados 2 testes: `Create_WithSourceEventId_SetsAuditCorrelationField`, `Create_WithoutSourceEventId_SourceEventIdIsNull` |
| `NotificationTrailHandlerTests.cs` (novo) | 8 testes: not found, trilha vazia, SourceEventId preservado, entregue, retry agendado, falha permanente, múltiplos canais, SourceEventId null |
| `OrchestratorExternalDeliveryIntegrationTests.cs` | Atualizado: `INotificationAuditService` adicionado como mock na construção do orchestrator |
| `ExternalDeliveryServiceTests.cs` | Atualizado: `INotificationAuditService` adicionado como mock na construção do service |
| `NotificationOrchestratorTests.cs` | Atualizado: `INotificationAuditService` adicionado como mock |
| `NexTraceOne.Notifications.Tests.csproj` | Referência a `AuditCompliance.Contracts` adicionada |

---

## 5. AuditEvent Gerado no Fluxo

### Pontos de auditoria implementados

| Ponto do fluxo | ActionType | ResourceType | Quem chama |
|---|---|---|---|
| Notificação criada (não-crítica) | `notification.generated` | `Notification` | `NotificationOrchestrator` |
| Notificação criada (crítica) | `notification.critical.generated` | `Notification` | `NotificationOrchestrator` |
| Entrega concluída (não-crítica) | `notification.delivered` | `NotificationDelivery` | `ExternalDeliveryService` |
| Entrega concluída (crítica) | `notification.critical.delivered` | `NotificationDelivery` | `ExternalDeliveryService` |
| Falha permanente (não-crítica) | `notification.delivery.failed` | `NotificationDelivery` | `ExternalDeliveryService` |
| Falha permanente (crítica) | `notification.critical.failed` | `NotificationDelivery` | `ExternalDeliveryService` |
| Retry agendado | `notification.delivery.retry_scheduled` | `NotificationDelivery` | `ExternalDeliveryService` |

Todos os eventos são persistidos via `IAuditModule.RecordEventAsync()` → `RecordAuditEvent.Command` → `AuditEvent` com hash chain SHA-256 no `aud_audit_events`.

### Estratégia de falha: best-effort

Falhas na auditoria não propagam exceção para o fluxo principal, seguindo o padrão `SecurityAuditBridge` existente.

---

## 6. Nova API: GET /notifications/{id}/trail

```http
GET /api/v1/notifications/{id}/trail
Authorization: Bearer {token}
Permission: notifications:delivery:read
```

**Resposta:**

```json
{
  "notificationId": "...",
  "notification": {
    "notificationId": "...",
    "eventType": "IncidentCreated",
    "sourceModule": "OperationalIntelligence",
    "sourceEntityType": "Incident",
    "sourceEntityId": "inc-999",
    "sourceEventId": "incident-evt-abc-123",
    "category": "Incident",
    "severity": "Critical",
    "status": "Unread",
    "recipientUserId": "...",
    "createdAt": "...",
    "readAt": null,
    "requiresAction": true
  },
  "deliveries": [
    {
      "deliveryId": "...",
      "channel": "Email",
      "status": "Delivered",
      "retryCount": 1,
      "createdAt": "...",
      "lastAttemptAt": "...",
      "deliveredAt": "...",
      "failedAt": null,
      "nextRetryAt": null,
      "errorMessage": null
    }
  ],
  "totalDeliveryAttempts": 1,
  "isDeliveredToAnyChannel": true,
  "hasPendingRetry": false,
  "hasPermanentFailure": false
}
```

---

## 7. Impacto no Frontend

### Novos tipos TypeScript

- `NotificationCorrelationDto` — notificação com SourceEventId e contexto de origem
- `DeliveryTrailEntryDto` — tentativa de entrega com timestamps e error
- `NotificationTrailResponse` — trilha completa

### Novo hook

```typescript
const { data: trail } = useNotificationTrail(notificationId);
// trail.notification.sourceEventId — evento de origem
// trail.deliveries — tentativas por canal
// trail.isDeliveredToAnyChannel, hasPendingRetry, hasPermanentFailure
```

---

## 8. Validação

- **Build:** `dotnet build NexTraceOne.sln` — Build succeeded, 0 errors
- **Testes:** 455 → 470 tests passing (+15 novos testes)
  - `NotificationAuditServiceTests`: reescrito com verificação real de `IAuditModule`
  - `NotificationTrailHandlerTests`: 8 novos testes de correlação
  - `NotificationTests`: 2 novos testes de `SourceEventId`
  - Outros testes atualizados: `NotificationOrchestratorTests`, `OrchestratorExternalDeliveryIntegrationTests`, `ExternalDeliveryServiceTests`

---

## 9. Cobertura da rastreabilidade pós-P7.3

O sistema consegue responder às seguintes perguntas de rastreabilidade:

| Pergunta | Resposta disponível? |
|---|---|
| Que evento originou a notificação? | ✅ `SourceEventId` + `EventType` + `SourceModule` |
| Quando foi criada a notificação? | ✅ `CreatedAt` |
| Em que canais foi tentada? | ✅ `Deliveries[].Channel` |
| Qual foi o resultado de cada tentativa? | ✅ `Deliveries[].Status` + timestamps |
| Que eventos de auditoria foram produzidos? | ✅ `AuditEvent` com hash chain SHA-256 |
| Existem retries pendentes? | ✅ `HasPendingRetry` / `NextRetryAt` |
| Houve falha permanente? | ✅ `HasPermanentFailure` / `FailedAt` |
