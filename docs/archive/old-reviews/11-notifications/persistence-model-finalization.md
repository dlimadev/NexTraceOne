# Notifications — Finalização do Modelo de Persistência

> **Módulo:** 11 — Notifications  
> **Data:** 2026-03-25  
> **Fase:** N8-R — Reexecução completa  
> **Estado:** ✅ FINALIZADO

---

## 1. Estado actual da persistência

### 1.1 DbContext

- **Classe:** `NotificationsDbContext`
- **Ficheiro:** `src/modules/notifications/NexTraceOne.Notifications.Infrastructure/Persistence/NotificationsDbContext.cs`
- **Base:** Extends `NexTraceDbContextBase` (RLS, Audit, Encryption, Outbox)
- **Implements:** `IUnitOfWork`
- **DbSets:** 3 (Notifications, Deliveries, Preferences)
- **Migrations:** ❌ ZERO — nenhuma pasta Migrations existe
- **EnsureCreated:** ❌ NÃO presente no código (confirmado por análise directa)

### 1.2 Tabelas actuais (via EF Configurations)

| Tabela | Entidade | Config | Prefixo |
|--------|----------|--------|---------|
| `ntf_notifications` | `Notification` | `NotificationConfiguration.cs` | ✅ ntf_ |
| `ntf_deliveries` | `NotificationDelivery` | `NotificationDeliveryConfiguration.cs` | ✅ ntf_ |
| `ntf_preferences` | `NotificationPreference` | `NotificationPreferenceConfiguration.cs` | ✅ ntf_ |

---

## 2. Mapeamento detalhado entidade → tabela

### 2.1 ntf_notifications

| Coluna | Tipo PostgreSQL | .NET Type | Config EF |
|--------|----------------|-----------|-----------|
| `id` | `uuid` | `NotificationId` → Guid | PK, StronglyTypedId conversion |
| `tenant_id` | `uuid` | `Guid` | Required, Indexed |
| `recipient_user_id` | `uuid` | `Guid` | Required, Indexed |
| `event_type` | `varchar(300)` | `string` | Required, MaxLength(300), Indexed |
| `category` | `varchar(100)` | `NotificationCategory` → string | Required, enum→string conversion |
| `severity` | `varchar(100)` | `NotificationSeverity` → string | Required, enum→string conversion |
| `title` | `varchar(500)` | `string` | Required, MaxLength(500) |
| `message` | `varchar(4000)` | `string` | Required, MaxLength(4000) |
| `source_module` | `varchar(200)` | `string` | Required, MaxLength(200) |
| `source_entity_type` | `varchar(200)` | `string` | Optional, MaxLength(200) |
| `source_entity_id` | `varchar(500)` | `string` | Optional, MaxLength(500) |
| `environment_id` | `uuid` | `Guid?` | Optional |
| `action_url` | `varchar(2000)` | `string` | Optional, MaxLength(2000) |
| `requires_action` | `boolean` | `bool` | Required |
| `status` | `varchar(100)` | `NotificationStatus` → string | Required, enum→string conversion, Indexed |
| `payload_json` | `text` | `string` | Optional, ColumnType("text") |
| `created_at` | `timestamptz` | `DateTime` | Required, Indexed |
| `read_at` | `timestamptz` | `DateTime?` | Optional |
| `acknowledged_at` | `timestamptz` | `DateTime?` | Optional |
| `archived_at` | `timestamptz` | `DateTime?` | Optional |
| `expires_at` | `timestamptz` | `DateTime?` | Optional |

**Nota:** Campos Phase 6 (CorrelationKey, GroupId, OccurrenceCount, LastOccurrenceAt) + campos de Snooze/Escalation/Suppression estão na entidade mas **não confirmados na EF Configuration**. Necessita validação se são incluídos via ConfigurationsAssembly auto-discovery ou shadow properties.

**Índices configurados (5):**

| Índice | Colunas | Tipo |
|--------|---------|------|
| `IX_ntf_notifications_tenant_id` | `tenant_id` | Normal |
| `IX_ntf_notifications_recipient_user_id` | `recipient_user_id` | Normal |
| `IX_ntf_notifications_status` | `status` | Normal |
| `IX_ntf_notifications_created_at` | `created_at` | Normal |
| `IX_ntf_notifications_recipient_status` | `recipient_user_id, status` | Composite |

### 2.2 ntf_deliveries

| Coluna | Tipo PostgreSQL | .NET Type | Config EF |
|--------|----------------|-----------|-----------|
| `id` | `uuid` | `NotificationDeliveryId` → Guid | PK |
| `notification_id` | `uuid` | `NotificationId` → Guid | FK, Required, Indexed |
| `channel` | `varchar(100)` | `DeliveryChannel` → string | Required, enum→string, Indexed |
| `recipient_address` | `varchar(500)` | `string` | Optional |
| `status` | `varchar(100)` | `DeliveryStatus` → string | Required, enum→string, Indexed |
| `created_at` | `timestamptz` | `DateTime` | Required |
| `delivered_at` | `timestamptz` | `DateTime?` | Optional |
| `failed_at` | `timestamptz` | `DateTime?` | Optional |
| `error_message` | `varchar(4000)` | `string` | Optional |
| `retry_count` | `integer` | `int` | Required |

**Índices configurados (4):**

| Índice | Colunas | Tipo |
|--------|---------|------|
| `IX_ntf_deliveries_notification_id` | `notification_id` | FK |
| `IX_ntf_deliveries_status` | `status` | Normal |
| `IX_ntf_deliveries_channel` | `channel` | Normal |
| `IX_ntf_deliveries_status_retry` | `status, retry_count` | Composite |

### 2.3 ntf_preferences

| Coluna | Tipo PostgreSQL | .NET Type | Config EF |
|--------|----------------|-----------|-----------|
| `id` | `uuid` | `NotificationPreferenceId` → Guid | PK |
| `tenant_id` | `uuid` | `Guid` | Required, Indexed |
| `user_id` | `uuid` | `Guid` | Required, Indexed |
| `category` | `varchar(100)` | `NotificationCategory` → string | Required, enum→string |
| `channel` | `varchar(100)` | `DeliveryChannel` → string | Required, enum→string |
| `enabled` | `boolean` | `bool` | Required |
| `updated_at` | `timestamptz` | `DateTime` | Required |

**Índices configurados (3):**

| Índice | Colunas | Tipo |
|--------|---------|------|
| `IX_ntf_preferences_unique` | `tenant_id, user_id, category, channel` | **UNIQUE** |
| `IX_ntf_preferences_tenant_id` | `tenant_id` | Normal |
| `IX_ntf_preferences_user_id` | `user_id` | Normal |

---

## 3. FKs e constraints

### 3.1 Foreign Keys

| FK | De | Para | Config |
|----|-----|------|--------|
| `ntf_deliveries.notification_id` | `ntf_deliveries` | `ntf_notifications.id` | ✅ Configurada via EF HasForeignKey |

### 3.2 Constraints

| Constraint | Tipo | Detalhes |
|-----------|------|---------|
| PK em todas as tabelas | ✅ | Guid via StronglyTypedId |
| FK delivery → notification | ✅ | `HasForeignKey(d => d.NotificationId)` |
| UNIQUE preferences | ✅ | (TenantId, UserId, Category, Channel) |
| CHECK constraints | ❌ Ausente | Sem check constraints em nenhuma tabela |
| RowVersion / xmin | ❌ Ausente | Sem optimistic concurrency |

---

## 4. Auditoria

| Mecanismo | Estado | Fonte |
|-----------|--------|-------|
| CreatedAt/UpdatedAt | ✅ Real | `AuditInterceptor` via `NexTraceDbContextBase` |
| CreatedBy/UpdatedBy | ✅ Real | `AuditInterceptor` via `NexTraceDbContextBase` |
| RLS (TenantId) | ✅ Real | `TenantRlsInterceptor` via `NexTraceDbContextBase` |
| Domain Events | ✅ Real | 3 domain events + outbox |
| Dedicated audit service | ✅ Real | `NotificationAuditService.cs` |

---

## 5. Divergências entre actual e final

| Aspecto | Estado actual | Estado final desejado | Acção |
|---------|-------------|----------------------|-------|
| Migrations | ❌ Zero | Initial migration criada | Criar migration inicial |
| RowVersion | ❌ Ausente | UseXminAsConcurrencyToken() em todas as configs | Adicionar |
| CHECK constraints | ❌ Ausentes | Status e Channel validados por CHECK | Adicionar na migration |
| Phase 6 fields no EF Config | ⚠️ Incerto | Todos mapeados explicitamente | Verificar e mapear |
| Soft delete | ❌ Ausente | Não necessário — notificações têm Archive/Dismiss | N/A |
| EnvironmentId FK | ❌ Sem FK | FK lógica (sem referential integrity cross-module) | Manter como Guid |
| Índice para expiração | ❌ Ausente | Filtered index para cleanup de notificações expiradas | Adicionar |
| TenantId como shadow property | ⚠️ | Explícito em todas as configs | Verificar |

---

## 6. Nomes finais com prefixo ntf_

| Entidade | Tabela final | Prefixo |
|----------|-------------|---------|
| `Notification` | `ntf_notifications` | ✅ Já aplicado |
| `NotificationDelivery` | `ntf_deliveries` | ✅ Já aplicado |
| `NotificationPreference` | `ntf_preferences` | ✅ Já aplicado |
| `NotificationTemplate` (futuro) | `ntf_templates` | 📋 Planeado |
| `NotificationChannelConfig` (futuro) | `ntf_channel_configs` | 📋 Planeado |
| `DeliveryAttempt` (futuro) | `ntf_delivery_attempts` | 📋 Planeado |
| `SuppressionRule` (futuro) | `ntf_suppression_rules` | 📋 Planeado |

---

## 7. Pré-condições para criar migrations

| Pré-condição | Estado | Acção |
|-------------|--------|-------|
| Prefixo ntf_ aplicado | ✅ | Já correcto |
| Phase 6 fields verificados | ⚠️ | Verificar se estão mapeados |
| RowVersion adicionado | ❌ | Adicionar antes da migration |
| CHECK constraints definidos | ❌ | Definir antes da migration |
| Filtered index para expiração | ❌ | Definir antes da migration |
| Soft delete policy decidida | ✅ | Não necessário (Archive/Dismiss) |
| TenantId verificado | ⚠️ | Confirmar em todas as configs |
