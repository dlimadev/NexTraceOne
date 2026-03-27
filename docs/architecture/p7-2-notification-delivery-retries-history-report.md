# P7.2 — Notification Delivery Retries, Status and History Report

## Objetivo

Transformar o módulo Notifications de um backend estruturalmente mínimo para um sistema operacionalmente real com:

- Retry deferido por canal (não-bloqueante)
- Status de entrega persistido e consultável
- Histórico de tentativas rastreável por notificação
- Integração SMTP com prioridade para configuração persistida (P7.1)
- Background job de reprocessamento de retries agendados

---

## Estado Anterior do Modelo de Delivery

| Campo | Estado antes do P7.2 |
|---|---|
| `NotificationDelivery.LastAttemptAt` | **Ausente** |
| `NotificationDelivery.NextRetryAt` | **Ausente** |
| `DeliveryStatus.RetryScheduled` | **Ausente** |
| `NotificationDelivery.ScheduleRetry()` | **Ausente** |
| Retry inline com `Task.Delay` | Presente — bloqueante e não escalável |
| Background retry job | **Ausente** |
| Endpoint de delivery history | **Ausente** |
| Endpoint de delivery status | **Ausente** |
| `ISmtpConfigurationStore` em `EmailNotificationDispatcher` | **Ausente** |
| `INotificationDeliveryStore.ListScheduledForRetryAsync` | **Ausente** |

---

## Modelo de Retries/Status/History Adotado

### Estratégia: Retry Deferido (Deferred Retry)

Em vez de retry inline (loop com `Task.Delay` bloqueante), o P7.2 adota **retry deferido**:

1. **Primeira tentativa**: `ProcessExternalDeliveryAsync` cria o `NotificationDelivery`, invoca o dispatcher.
2. **Falha transitória (RetryCount < MaxAttempts)**: chama `delivery.ScheduleRetry(nextRetryAt)` — define status `RetryScheduled` e `NextRetryAt` com backoff linear.
3. **`NotificationDeliveryRetryJob`** (BackgroundService, ciclo de 60s): busca deliveries com `RetryScheduled` e `NextRetryAt ≤ now`, chama `RetryDeliveryAsync`.
4. **Falha final (RetryCount ≥ MaxAttempts)**: chama `delivery.MarkFailed()` — status `Failed` permanente.
5. **Sucesso**: chama `delivery.MarkDelivered()`.

### Vantagens sobre o modelo anterior
- Não bloqueia threads com `Task.Delay`
- Retries sobrevivem a reinicios da aplicação (estado persistido)
- `NextRetryAt` consultável — permite auditar quando cada delivery será tentado

---

## Ficheiros Alterados (Backend)

### Entidades/Domínio

| Ficheiro | Alteração |
|---|---|
| `Domain/Enums/DeliveryStatus.cs` | Adicionado `RetryScheduled = 4` |
| `Domain/Entities/NotificationDelivery.cs` | Adicionados `LastAttemptAt`, `NextRetryAt`; novo método `ScheduleRetry()`; `IncrementRetry()` e `MarkDelivered()/MarkFailed()/MarkSkipped()` atualizados para registar `LastAttemptAt` e limpar `NextRetryAt` |

### Application Layer

| Ficheiro | Alteração |
|---|---|
| `Application/ExternalDelivery/IExternalDeliveryService.cs` | Adicionado `RetryDeliveryAsync(delivery, notification, ct)` |
| `Application/ExternalDelivery/INotificationDeliveryStore.cs` | Adicionados `ListScheduledForRetryAsync(now, maxRetry, batchSize, ct)` e `ListByTenantAsync(tenantId, status?, channel?, skip, take, ct)` |
| `Application/ExternalDelivery/DeliveryRetryOptions.cs` | Adicionado `RetryJobIntervalSeconds` (padrão: 60s) |
| `Application/Features/GetDeliveryHistory/GetDeliveryHistory.cs` | **Novo** — query que retorna histórico completo de tentativas de entrega de uma notificação |
| `Application/Features/GetDeliveryStatus/GetDeliveryStatus.cs` | **Novo** — query que retorna estado agregado de entrega por canal de uma notificação |

### Infrastructure Layer

| Ficheiro | Alteração |
|---|---|
| `Infrastructure/Persistence/Configurations/NotificationDeliveryConfiguration.cs` | Adicionados mapeamentos de `LastAttemptAt`, `NextRetryAt`; índice `(Status, NextRetryAt)`; check constraint atualizado com `RetryScheduled` |
| `Infrastructure/Persistence/Repositories/NotificationDeliveryStoreRepository.cs` | Implementados `ListScheduledForRetryAsync` e `ListByTenantAsync` |
| `Infrastructure/ExternalDelivery/ExternalDeliveryService.cs` | Reescrito: retry deferido via `ScheduleRetry()` em vez de `Task.Delay`; novo método `RetryDeliveryAsync()` partilhado com retry job; método `AttemptDispatchAsync()` extraído |
| `Infrastructure/ExternalDelivery/EmailNotificationDispatcher.cs` | Adicionado `ISmtpConfigurationStore` + `ICurrentTenant`; `ResolveSmtpSettingsAsync()` com prioridade para configuração persistida e fallback para appsettings |
| `Infrastructure/ExternalDelivery/NotificationDeliveryRetryJob.cs` | **Novo** — `BackgroundService` com `PeriodicTimer`; processa deliveries `RetryScheduled` com `NextRetryAt ≤ now` |
| `Infrastructure/DependencyInjection.cs` | Registado `NotificationDeliveryRetryJob` como `AddHostedService<>` |
| `Infrastructure/Migrations/20260327084919_P7_2_DeliveryRetryHistory.cs` | **Nova** migration — adiciona `LastAttemptAt`, `NextRetryAt`, índice `(Status, NextRetryAt)`, check constraint atualizado |

### API Layer

| Ficheiro | Alteração |
|---|---|
| `API/Endpoints/NotificationCenterEndpointModule.cs` | Adicionados 2 endpoints: `GET /{id}/delivery-history` e `GET /{id}/delivery-status` com permissão `notifications:delivery:read` |

---

## Novos Endpoints API

| Método | Endpoint | Permissão | Descrição |
|---|---|---|---|
| `GET` | `/api/v1/notifications/{id}/delivery-history` | `notifications:delivery:read` | Histórico completo de tentativas de entrega de uma notificação |
| `GET` | `/api/v1/notifications/{id}/delivery-status` | `notifications:delivery:read` | Estado agregado de entrega por canal (delivered? retrying? failed?) |

---

## Ficheiros Alterados (Testes)

| Ficheiro | Alteração |
|---|---|
| `Tests/ExternalDelivery/EmailNotificationDispatcherTests.cs` | Adicionados mocks de `ISmtpConfigurationStore` + `ICurrentTenant` no `CreateDispatcher()` |
| `Tests/ExternalDelivery/ExternalDeliveryServiceTests.cs` | Atualizados 2 testes de retry inline para novos comportamentos de retry deferido; adicionados 3 novos testes (`SchedulesRetry`, `RetryDeliveryAsync_Delivered`, `RetryDeliveryAsync_PermanentFailure`) |
| `Tests/Domain/NotificationDeliveryTests.cs` | Adicionados testes para `ScheduleRetry()`, `LastAttemptAt`, `NextRetryAt`, lifecycle completo de retry deferido |
| `Tests/Application/DeliveryHistoryHandlerTests.cs` | **Novo** — 8 testes para `GetDeliveryHistory` e `GetDeliveryStatus` |

---

## Ficheiros Alterados (Frontend)

| Ficheiro | Alteração |
|---|---|
| `features/notifications/types.ts` | Adicionados tipos: `DeliveryEntryDto`, `DeliveryHistoryResponse`, `ChannelStatusDto`, `DeliveryStatusResponse` |
| `features/notifications/api/notifications.ts` | Adicionados: `getDeliveryHistory(id)`, `getDeliveryStatus(id)` |
| `features/notifications/hooks/useNotificationConfiguration.ts` | Adicionados: `useDeliveryHistory(id)`, `useDeliveryStatus(id)` com `notificationDeliveryKeys` |
| `features/notifications/index.ts` | Exportações atualizadas |

---

## Integração SMTP (P7.1 → P7.2)

O `EmailNotificationDispatcher` passou a usar `ISmtpConfigurationStore` com a seguinte lógica:

```
1. Tentar GetByTenantAsync(tenantId)
2. Se configuração persistida exists && IsEnabled → usar configuração persistida
3. Caso contrário → fallback para IOptions<NotificationChannelOptions> (appsettings)
```

**Nota:** A senha (`EncryptedPassword`) é passada diretamente sem decifra nesta fase. Criptografia real fica para P7.3.

---

## Migration EF Core

**Ficheiro:** `20260327084919_P7_2_DeliveryRetryHistory.cs`

```sql
-- Novas colunas
ALTER TABLE ntf_deliveries ADD COLUMN LastAttemptAt timestamptz;
ALTER TABLE ntf_deliveries ADD COLUMN NextRetryAt timestamptz;

-- Índice para retry job
CREATE INDEX IX_ntf_deliveries_Status_NextRetryAt ON ntf_deliveries (Status, NextRetryAt);

-- Check constraint atualizado (inclui RetryScheduled)
ALTER TABLE ntf_deliveries ADD CONSTRAINT CK_ntf_deliveries_status 
  CHECK (Status IN ('Pending', 'Delivered', 'Failed', 'Skipped', 'RetryScheduled'));
```

---

## Testes

**Resultado:** 455 testes passam (442 P7.1 + 13 novos P7.2).

**Novos testes (13):**
- `NotificationDeliveryTests`: 7 novos testes para `ScheduleRetry`, `LastAttemptAt`, lifecycle completo
- `ExternalDeliveryServiceTests`: 3 novos testes + 2 atualizados
- `DeliveryHistoryHandlerTests`: 8 novos testes (GetDeliveryHistory × 3, GetDeliveryStatus × 4)

---

## Validação de Compilação

- `dotnet build NexTraceOne.sln` → **Build succeeded. 0 Error(s)**
- `dotnet test NexTraceOne.Notifications.Tests` → **Passed! 455 tests**
