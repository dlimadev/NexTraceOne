# Notifications — Correções Funcionais do Backend

> **Módulo:** 11 — Notifications  
> **Data:** 2026-03-25  
> **Fase:** N8-R — Reexecução completa  
> **Estado:** ✅ FINALIZADO

---

## 1. Endpoints actuais

**Ficheiro:** `src/modules/notifications/NexTraceOne.Notifications.API/Endpoints/NotificationCenterEndpointModule.cs` (109 linhas)

| # | Método | Rota | Permissão | Handler | Estado |
|---|--------|------|-----------|---------|--------|
| E-01 | `GET` | `/api/v1/notifications` | `notifications:inbox:read` | `ListNotifications` | ✅ Real |
| E-02 | `GET` | `/api/v1/notifications/unread-count` | `notifications:inbox:read` | `GetUnreadCount` | ✅ Real |
| E-03 | `POST` | `/api/v1/notifications/{id:guid}/read` | `notifications:inbox:write` | `MarkNotificationRead` | ✅ Real |
| E-04 | `POST` | `/api/v1/notifications/{id:guid}/unread` | `notifications:inbox:write` | `MarkNotificationUnread` | ✅ Real |
| E-05 | `POST` | `/api/v1/notifications/mark-all-read` | `notifications:inbox:write` | `MarkAllNotificationsRead` | ✅ Real |
| E-06 | `GET` | `/api/v1/notifications/preferences` | `notifications:preferences:read` | `GetPreferences` | ✅ Real |
| E-07 | `PUT` | `/api/v1/notifications/preferences` | `notifications:preferences:write` | `UpdatePreference` | ✅ Real |

---

## 2. Endpoints ausentes

| # | Método | Rota sugerida | Propósito | Prioridade |
|---|--------|--------------|----------|-----------|
| E-08 | `GET` | `/api/v1/notifications/{id}` | Detalhe de uma notificação | 🟠 Médio |
| E-09 | `POST` | `/api/v1/notifications/{id}/acknowledge` | Reconhecer notificação | 🟠 Médio |
| E-10 | `POST` | `/api/v1/notifications/{id}/archive` | Arquivar notificação | 🟠 Médio |
| E-11 | `POST` | `/api/v1/notifications/{id}/dismiss` | Dispensar notificação | 🟡 Baixo |
| E-12 | `POST` | `/api/v1/notifications/{id}/snooze` | Snooze notificação | 🟡 Baixo |
| E-13 | `GET` | `/api/v1/notifications/deliveries/{notificationId}` | Histórico de delivery | 🟠 Médio |
| E-14 | `POST` | `/api/v1/notifications/deliveries/{id}/retry` | Retry manual de delivery | 🟠 Médio |
| E-15 | `GET` | `/api/v1/notifications/stats` | Estatísticas de notificações | 🟡 Baixo |
| E-16 | `DELETE` | `/api/v1/notifications/expired` | Cleanup de expiradas | 🟡 Baixo |

---

## 3. Endpoints mortos

| Endpoint | Estado |
|---------|--------|
| Nenhum endpoint morto identificado | ✅ Todos os 7 endpoints estão ligados a handlers reais |

---

## 4. Requests/Responses

### 4.1 ListNotifications (E-01)

**Request (query params):**
```
status?: string (NotificationStatus)
category?: string (NotificationCategory)
severity?: string (NotificationSeverity)
page?: int (default 1)
pageSize?: int (default 20)
```

**Response:**
```json
{
  "items": [NotificationDto],
  "hasMore": boolean
}
```

**Gaps:** ❌ Sem filtro por EnvironmentId, ❌ Sem filtro por dateRange, ❌ Sem filtro por sourceModule

### 4.2 GetUnreadCount (E-02)

**Request:** Nenhum (usa CurrentUser do contexto)

**Response:**
```json
{ "unreadCount": int }
```

### 4.3 MarkNotificationRead/Unread (E-03, E-04)

**Request:** `{id:guid}` no path

**Response:** 204 No Content

### 4.4 MarkAllNotificationsRead (E-05)

**Request:** Nenhum

**Response:** 204 No Content

### 4.5 GetPreferences (E-06)

**Response:**
```json
{
  "preferences": [
    { "category": string, "channel": string, "enabled": boolean, "isMandatory": boolean, "updatedAt": datetime }
  ]
}
```

### 4.6 UpdatePreference (E-07)

**Request:**
```json
{ "category": string, "channel": string, "enabled": boolean }
```

**Response:** 204 No Content

---

## 5. Validações

| Handler | Validação | Estado |
|---------|----------|--------|
| `ListNotifications` | Paginação com defaults | ✅ Real |
| `MarkNotificationRead` | Verifica existência da notificação | ✅ Real |
| `MarkNotificationUnread` | Verifica existência | ✅ Real |
| `UpdatePreference` | Mandatory policy check | ✅ Real |
| `NotificationOrchestrator` | EventType, SourceModule, TenantId obrigatórios | ✅ Real |
| Todos os handlers | FluentValidation | ❌ Ausente — sem validadores FluentValidation dedicados |

---

## 6. Tratamento de erro

| Cenário | Tratamento | Estado |
|---------|-----------|--------|
| Notificação não encontrada | 404 Not Found | ⚠️ Depende da implementação do handler |
| Delivery falha | Log + MarkFailed, não bloqueia in-app | ✅ Real (graceful) |
| Deduplicação | Skip silencioso com log debug | ✅ Real |
| Preferência mandatória | Rejeição do update | ✅ Real |
| Erro de persistência | Exception propagada | ⚠️ Sem tratamento específico |

---

## 7. Permissões por acção

| Acção | Permissão | No RolePermissionCatalog? |
|-------|-----------|--------------------------|
| Listar notificações | `notifications:inbox:read` | 🔴 NÃO |
| Ver contagem não lidas | `notifications:inbox:read` | 🔴 NÃO |
| Marcar como lida | `notifications:inbox:write` | 🔴 NÃO |
| Marcar como não lida | `notifications:inbox:write` | 🔴 NÃO |
| Marcar todas como lidas | `notifications:inbox:write` | 🔴 NÃO |
| Ver preferências | `notifications:preferences:read` | 🔴 NÃO |
| Actualizar preferências | `notifications:preferences:write` | 🔴 NÃO |

**🔴 BLOCKER:** Nenhuma destas permissões está registada no `RolePermissionCatalog.cs`. Os utilizadores não conseguem aceder às notificações a menos que as permissões sejam atribuídas manualmente.

---

## 8. Auditoria

| Aspecto | Estado | Detalhes |
|---------|--------|---------|
| AuditInterceptor (timestamps) | ✅ | Via NexTraceDbContextBase |
| NotificationAuditService | ✅ | Serviço dedicado de auditoria |
| Domain events | ✅ | NotificationCreatedEvent, NotificationReadEvent |
| Integration events (outbox) | ✅ | 3 integration events publicados |
| Structured logging | ✅ | ILogger com contexto no orchestrator |

---

## 9. Backlog de correções backend

| # | Correção | Prioridade | Esforço |
|---|---------|-----------|---------|
| B-01 | 🔴 **Registar permissões `notifications:*` no RolePermissionCatalog** | P0 | 2h |
| B-02 | 🟠 Criar endpoint GET `/notifications/{id}` (detalhe) | P1 | 3h |
| B-03 | 🟠 Criar endpoint POST `/notifications/{id}/acknowledge` | P1 | 2h |
| B-04 | 🟠 Criar endpoint POST `/notifications/{id}/archive` | P1 | 2h |
| B-05 | 🟠 Criar endpoint GET `/notifications/deliveries/{notificationId}` | P1 | 3h |
| B-06 | 🟠 Criar endpoint POST `/notifications/deliveries/{id}/retry` | P1 | 4h |
| B-07 | 🟠 Adicionar filtro por EnvironmentId em ListNotifications | P2 | 2h |
| B-08 | 🟠 Adicionar filtro por dateRange em ListNotifications | P2 | 2h |
| B-09 | 🟠 Adicionar filtro por sourceModule em ListNotifications | P2 | 1h |
| B-10 | 🟡 Criar endpoint POST `/notifications/{id}/dismiss` | P2 | 2h |
| B-11 | 🟡 Criar endpoint POST `/notifications/{id}/snooze` | P2 | 3h |
| B-12 | 🟡 Adicionar FluentValidation a todos os handlers | P2 | 4h |
| B-13 | 🟡 Implementar background retry scheduler (HostedService) | P2 | 8h |
| B-14 | 🟡 Implementar background escalation scheduler | P2 | 6h |
| B-15 | 🟡 Implementar background digest scheduler | P3 | 6h |
| B-16 | 🟡 Implementar cleanup de notificações expiradas | P3 | 4h |

**Esforço total estimado:** ~54h
