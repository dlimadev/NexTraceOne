# Notifications — Finalização do Modelo de Domínio

> **Módulo:** 11 — Notifications  
> **Data:** 2026-03-25  
> **Fase:** N8-R — Reexecução completa  
> **Estado:** ✅ FINALIZADO

---

## 1. Modelo de domínio actual

### 1.1 Aggregate roots

O módulo possui **3 aggregate roots**, cada um com o seu próprio strongly-typed ID:

#### Notification (aggregate root principal)
- **Ficheiro:** `src/modules/notifications/NexTraceOne.Notifications.Domain/Entities/Notification.cs` (327 linhas)
- **ID:** `NotificationId` (strongly-typed GUID)

| Propriedade | Tipo | Obrigatória | Descrição |
|------------|------|-------------|-----------|
| `Id` | `NotificationId` | ✅ | PK |
| `TenantId` | `Guid` | ✅ | Multi-tenancy |
| `RecipientUserId` | `Guid` | ✅ | Destinatário |
| `EventType` | `string` | ✅ | Tipo de evento (max 300) |
| `Category` | `NotificationCategory` | ✅ | Categoria (enum → string) |
| `Severity` | `NotificationSeverity` | ✅ | Severidade (enum → string) |
| `Title` | `string` | ✅ | Título (max 500) |
| `Message` | `string` | ✅ | Mensagem (max 4000) |
| `SourceModule` | `string` | ✅ | Módulo de origem (max 200) |
| `SourceEntityType` | `string` | ❌ | Tipo da entidade de origem (max 200) |
| `SourceEntityId` | `string` | ❌ | ID da entidade de origem (max 500) |
| `EnvironmentId` | `Guid?` | ❌ | Ambiente associado |
| `ActionUrl` | `string` | ❌ | URL de acção (max 2000) |
| `RequiresAction` | `bool` | ✅ | Requer acção do utilizador |
| `Status` | `NotificationStatus` | ✅ | Estado actual (enum → string) |
| `PayloadJson` | `string` | ❌ | Payload JSON (tipo text) |
| `CreatedAt` | `DateTime` | ✅ | Timestamp de criação |
| `ReadAt` | `DateTime?` | ❌ | Timestamp de leitura |
| `AcknowledgedAt` | `DateTime?` | ❌ | Timestamp de reconhecimento |
| `ArchivedAt` | `DateTime?` | ❌ | Timestamp de arquivo |
| `ExpiresAt` | `DateTime?` | ❌ | Timestamp de expiração |
| `CorrelationKey` | `string` | ❌ | Chave de correlação (Phase 6) |
| `GroupId` | `string` | ❌ | ID de grupo (Phase 6) |
| `OccurrenceCount` | `int` | ✅ | Contagem de ocorrências (Phase 6) |
| `LastOccurrenceAt` | `DateTime?` | ❌ | Última ocorrência (Phase 6) |

**Métodos de domínio:**

| Método | Acção |
|--------|-------|
| `Create(...)` | Factory method estático |
| `MarkAsRead()` | Status → Read, sets ReadAt |
| `MarkAsUnread()` | Status → Unread, clears ReadAt |
| `Acknowledge()` | Status → Acknowledged, sets AcknowledgedAt |
| `Archive()` | Status → Archived, sets ArchivedAt |
| `Dismiss()` | Status → Dismissed |
| `Snooze(until)` | Activates snooze até timestamp |
| `Unsnooze()` | Desactiva snooze |
| `IsSnoozed()` | Verifica se está snoozed |
| `MarkAsEscalated()` | Marca como escalada |
| `CorrelateWithIncident(incidentId)` | Associa a incidente |
| `Suppress(reason)` | Suprime com razão |
| `IsExpired()` | Verifica se expirou |

#### NotificationDelivery
- **Ficheiro:** `src/modules/notifications/NexTraceOne.Notifications.Domain/Entities/NotificationDelivery.cs` (100 linhas)
- **ID:** `NotificationDeliveryId` (strongly-typed GUID)

| Propriedade | Tipo | Obrigatória | Descrição |
|------------|------|-------------|-----------|
| `Id` | `NotificationDeliveryId` | ✅ | PK |
| `NotificationId` | `NotificationId` | ✅ | FK → Notification |
| `Channel` | `DeliveryChannel` | ✅ | Canal (Email/Teams) |
| `RecipientAddress` | `string` | ❌ | Endereço do destinatário (max 500) |
| `Status` | `DeliveryStatus` | ✅ | Estado (Pending/Delivered/Failed/Skipped) |
| `CreatedAt` | `DateTime` | ✅ | Timestamp de criação |
| `DeliveredAt` | `DateTime?` | ❌ | Timestamp de entrega |
| `FailedAt` | `DateTime?` | ❌ | Timestamp de falha |
| `ErrorMessage` | `string` | ❌ | Mensagem de erro (max 4000) |
| `RetryCount` | `int` | ✅ | Contagem de retries |

**Métodos de domínio:**

| Método | Acção |
|--------|-------|
| `Create(...)` | Factory method estático |
| `MarkDelivered()` | Status → Delivered, sets DeliveredAt |
| `MarkFailed(errorMessage)` | Status → Failed, sets FailedAt + ErrorMessage |
| `MarkSkipped()` | Status → Skipped |
| `IncrementRetry()` | RetryCount++ |

#### NotificationPreference
- **Ficheiro:** `src/modules/notifications/NexTraceOne.Notifications.Domain/Entities/NotificationPreference.cs` (77 linhas)
- **ID:** `NotificationPreferenceId` (strongly-typed GUID)

| Propriedade | Tipo | Obrigatória | Descrição |
|------------|------|-------------|-----------|
| `Id` | `NotificationPreferenceId` | ✅ | PK |
| `TenantId` | `Guid` | ✅ | Multi-tenancy |
| `UserId` | `Guid` | ✅ | Utilizador |
| `Category` | `NotificationCategory` | ✅ | Categoria |
| `Channel` | `DeliveryChannel` | ✅ | Canal |
| `Enabled` | `bool` | ✅ | Activada |
| `UpdatedAt` | `DateTime` | ✅ | Última actualização |

**Métodos:** `Create()`, `Update()`

---

### 1.2 Enums

| Enum | Ficheiro | Valores |
|------|---------|---------|
| `NotificationCategory` | `Enums/NotificationCategory.cs` (1360 linhas) | Incident, Approval, Change, Contract, Security, Compliance, FinOps, AI, Integration (9) |
| `NotificationSeverity` | `Enums/NotificationSeverity.cs` (1392 linhas) | Critical, High, Medium, Low, Info (5) |
| `NotificationStatus` | `Enums/NotificationStatus.cs` (920 linhas) | Unread, Read, Acknowledged, Archived, Dismissed (5) |
| `NotificationType` | `Enums/NotificationType.cs` (3970 linhas) | 25+ event types |
| `DeliveryChannel` | `Enums/DeliveryChannel.cs` | Email, Teams (2) |
| `DeliveryStatus` | `Enums/DeliveryStatus.cs` | Pending, Delivered, Failed, Skipped (4) |

### 1.3 Domain Events

| Evento | Ficheiro | Quando |
|--------|---------|--------|
| `NotificationCreatedEvent` | `Events/NotificationCreatedEvent.cs` | Notificação persistida |
| `NotificationDeliveryCompletedEvent` | `Events/NotificationDeliveryCompletedEvent.cs` | Delivery completada |
| `NotificationReadEvent` | `Events/NotificationReadEvent.cs` | Notificação lida |

### 1.4 Strongly-Typed IDs

| ID | Ficheiro |
|----|---------|
| `NotificationId` | `Ids/NotificationId.cs` |
| `NotificationDeliveryId` | `Ids/NotificationDeliveryId.cs` |
| `NotificationPreferenceId` | `Ids/NotificationPreferenceId.cs` |

---

## 2. Relações internas

```
Notification (aggregate root)
  ├── 1:N → NotificationDelivery (delivery tracking)
  └── N:1 → NotificationPreference (via Category+Channel+UserId)
  
NotificationPreference (standalone aggregate)
  └── Unique: (TenantId, UserId, Category, Channel)
```

---

## 3. Lacunas de modelagem

| # | Lacuna | Impacto | Prioridade |
|---|--------|---------|-----------|
| L-01 | **Sem entidade NotificationTemplate** — templates são in-code | Sem gestão dinâmica de templates | 🟠 Médio |
| L-02 | **Sem entidade NotificationChannel** — canais são enum | Sem configuração dinâmica de canais | 🟡 Baixo |
| L-03 | **Sem RowVersion/ConcurrencyToken** | Conflitos de escrita não detectados | 🟠 Médio |
| L-04 | **Sem soft delete** — entidades são permanentes | Sem possibilidade de "apagar" notificações | 🟡 Baixo |
| L-05 | **Sem DeliveryAttempt** como entidade separada | Não rastreia tentativas individuais de retry | 🟡 Baixo |
| L-06 | **Sem NotificationRule/SuppressionRule** como entidade | Regras de supressão são in-memory | 🟠 Médio |
| L-07 | **Sem DigestConfiguration** como entidade | Config de digest não persistida | 🟡 Baixo |

---

## 4. Modelo final esperado (evolução)

### Entidades adicionais recomendadas (Phase 2)

| Entidade | Propósito | Prioridade |
|----------|----------|-----------|
| `NotificationTemplate` | Templates persistidos e editáveis por tenant | P2 |
| `NotificationChannelConfig` | Configuração de canais por tenant | P2 |
| `DeliveryAttempt` | Log de cada tentativa de delivery | P3 |
| `SuppressionRule` | Regras de supressão persistidas | P3 |
| `DigestSchedule` | Configuração de digest por utilizador | P3 |

---

## 5. O que torna Notifications uma dimensão funcional real

O módulo Notifications não é um simples envio de mensagens. Ele implementa:

1. **Ciclo de vida completo** — 5 estados (Unread → Read → Acknowledged → Archived → Dismissed) + escalação + supressão + snooze
2. **Multi-canal com inteligência** — Routing baseado em preferências, mandatory policies, quiet hours
3. **Rastreabilidade end-to-end** — EventType + SourceModule + SourceEntityType + SourceEntityId + EnvironmentId + PayloadJson
4. **Deduplicação** — Prevenção de spam com janela temporal
5. **Correlação** — GroupId + CorrelationKey + OccurrenceCount para agrupamento
6. **Delivery tracking** — Status por canal com retry count e error tracking
7. **Auditoria** — 3 domain events + integration events + audit interceptor
8. **Governança** — CatalogGovernance + MandatoryNotificationPolicy + HealthProvider + Metrics
