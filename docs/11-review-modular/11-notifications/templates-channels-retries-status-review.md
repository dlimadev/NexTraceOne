# Notifications — Revisão de Templates, Canais, Retries e Status

> **Módulo:** 11 — Notifications  
> **Data:** 2026-03-25  
> **Fase:** N8-R — Reexecução completa  
> **Estado:** ✅ FINALIZADO

---

## 1. Templates existentes

### 1.1 Template resolver interno

**Ficheiro:** `src/modules/notifications/NexTraceOne.Notifications.Application/Engine/NotificationTemplateResolver.cs` (184 linhas)

| # | EventType | Title Template | Category | Severity | RequiresAction |
|---|-----------|---------------|----------|----------|---------------|
| T-01 | `IncidentCreated` | "🔴 Novo Incidente: {EntityName}" | Incident | Critical | ❌ |
| T-02 | `IncidentEscalated` | "⚠️ Incidente Escalado: {EntityName}" | Incident | High | ❌ |
| T-03 | `ApprovalPending` | "📋 Aprovação Pendente: {EntityName}" | Approval | Medium | ✅ |
| T-04 | `ApprovalApproved` | "✅ Aprovação Concedida: {EntityName}" | Approval | Low | ❌ |
| T-05 | `ApprovalRejected` | "❌ Aprovação Rejeitada: {EntityName}" | Approval | Medium | ❌ |
| T-06 | `BreakGlassActivated` | "🚨 Break Glass Activado" | Security | Critical | ✅ |
| T-07 | `JitAccessPending` | "🔑 Acesso JIT Pendente" | Security | High | ✅ |
| T-08 | `ComplianceCheckFailed` | "⚠️ Falha de Compliance: {EntityName}" | Compliance | High | ✅ |
| T-09 | `BudgetExceeded` | "💰 Orçamento Excedido: {EntityName}" | FinOps | High | ✅ |
| T-10 | `IntegrationFailed` | "🔌 Falha de Integração: {EntityName}" | Integration | High | ❌ |
| T-11 | `AiProviderUnavailable` | "🤖 Provider IA Indisponível" | AI | Medium | ❌ |
| T-12 | Generic (fallback) | "{EventType}: {EntityName}" | → from request | → from request | ❌ |
| T-13 | Override | Título/Mensagem do request | → from request | → from request | → from request |

### 1.2 Template resolver externo (canais)

**Ficheiro:** `src/modules/notifications/NexTraceOne.Notifications.Infrastructure/ExternalDelivery/ExternalChannelTemplateResolver.cs` (247 linhas)

| Canal | Formato | Método | Detalhes |
|-------|---------|--------|---------|
| **Email** | HTML + Plain Text | `ResolveEmailTemplate()` | Subject, HTML body com styling inline, plain text fallback |
| **Teams** | Adaptive Card JSON | `ResolveTeamsTemplate()` | Card com header, body, actions (deeplink) |

### 1.3 Parametrização

| Aspecto | Estado | Detalhes |
|---------|--------|---------|
| Substituição de `{EntityName}` | ✅ Real | Via Dictionary<string,string> |
| Substituição de `{EntityId}` | ✅ Real | Parâmetro automático |
| Substituição de `{SourceModule}` | ✅ Real | Parâmetro automático |
| Parâmetros adicionais de PayloadJson | ✅ Real | Deserializados automaticamente |
| Templates persistidos em BD | ❌ Ausente | Templates são in-code |
| Editor visual de templates | ❌ Ausente | Sem UI de gestão |
| Customização por tenant | ❌ Ausente | Templates são globais |

---

## 2. Canais existentes

### 2.1 Canais implementados

| Canal | Dispatcher | Estado | Detalhes |
|-------|-----------|--------|---------|
| **In-App** | `NotificationStore` (persistência directa) | ✅ Real | Central de notificações interna |
| **Email** | `EmailNotificationDispatcher.cs` | ✅ Real | HTML + plain text via SMTP |
| **Teams** | `TeamsNotificationDispatcher.cs` | ✅ Real | Adaptive Card via webhook |

### 2.2 Canais ausentes

| Canal | Estado | Prioridade |
|-------|--------|-----------|
| Webhook genérico | ❌ Ausente | P3 |
| SMS | ❌ Ausente | Fora do escopo |
| Slack | ❌ Ausente | Fora do escopo |
| Push mobile | ❌ Ausente | Fora do escopo |

### 2.3 Seleção de canal

**Ficheiro:** `src/modules/notifications/NexTraceOne.Notifications.Infrastructure/Routing/NotificationRoutingEngine.cs`

| Passo | Acção | Estado |
|-------|-------|--------|
| 1 | Verificar preferências do utilizador | ✅ Real |
| 2 | Verificar mandatory notification policy | ✅ Real |
| 3 | Verificar quiet hours | ✅ Real |
| 4 | Verificar suppression rules | ✅ Real |
| 5 | Determinar canais activos | ✅ Real |
| 6 | Dispatch para cada canal | ✅ Real |

---

## 3. Política de retry

### 3.1 Configuração

**Ficheiro:** `src/modules/notifications/NexTraceOne.Notifications.Application/ExternalDelivery/DeliveryRetryOptions.cs`

| Propriedade | Descrição | Estado |
|------------|-----------|--------|
| MaxRetries | Número máximo de retries | ✅ Configurável |
| RetryDelaySeconds | Delay entre retries | ✅ Configurável |
| ExponentialBackoff | Backoff exponencial | ⚠️ Não confirmado |

### 3.2 Mecanismo de retry

| Aspecto | Estado | Detalhes |
|---------|--------|---------|
| `RetryCount` field na entidade | ✅ Real | `NotificationDelivery.RetryCount` |
| `IncrementRetry()` método | ✅ Real | Incrementa counter |
| Background retry scheduler | ❌ **Ausente** | **Sem HostedService/Hangfire para retry automático** |
| Retry manual via API | ❌ **Ausente** | **Sem endpoint de retry** |
| Dead letter queue | ❌ Ausente | Sem conceito de dead letter |

### 3.3 Fluxo actual de retry

```
Delivery falha → MarkFailed(errorMessage) → Status = Failed → FIM
```

**O retry NÃO acontece automaticamente.** O campo RetryCount existe mas não há processo que o incremente e re-tente.

---

## 4. Estados de entrega

### 4.1 Delivery Status

| Status | Transição | Significado |
|--------|----------|-------------|
| `Pending` | Estado inicial | Delivery criada, aguardando envio |
| `Delivered` | Pending → Delivered | Entrega confirmada pelo canal |
| `Failed` | Pending → Failed | Falha no envio (erro registado) |
| `Skipped` | Pending → Skipped | Canal desactivado, quiet hours, ou supressão |

### 4.2 Distinção entre falha técnica e funcional

| Tipo | Estado | Detalhes |
|------|--------|---------|
| Falha técnica (timeout, SMTP down) | `Failed` com ErrorMessage técnica | ✅ Real |
| Falha funcional (canal desactivado) | `Skipped` | ✅ Real |
| Supressão por regra | `Skipped` | ✅ Real |
| Quiet hours | `Skipped` | ✅ Real |

✅ **Existe distinção clara** entre Failed (erro técnico) e Skipped (decisão funcional).

### 4.3 Notification Status (in-app)

| Status | Transição | Significado |
|--------|----------|-------------|
| `Unread` | Estado inicial | Notificação não lida |
| `Read` | Unread → Read | Lida pelo utilizador |
| `Acknowledged` | Read → Acknowledged | Reconhecida (acção tomada) |
| `Archived` | Qualquer → Archived | Arquivada |
| `Dismissed` | Qualquer → Dismissed | Dispensada |

---

## 5. Histórico de envio

| Aspecto | Estado | Detalhes |
|---------|--------|---------|
| Delivery records persistidos | ✅ Real | Tabela `ntf_deliveries` |
| Timestamps completos | ✅ Real | CreatedAt, DeliveredAt, FailedAt |
| ErrorMessage tracking | ✅ Real | Max 4000 chars |
| RetryCount tracking | ✅ Real | Incrementável via IncrementRetry() |
| API para consulta | ❌ Ausente | Sem endpoint de delivery history |
| Dashboard visual | ❌ Ausente | Sem UI de delivery tracking |

---

## 6. Lacunas e mínimo obrigatório

### 6.1 Templates

| Obrigatório | Estado |
|------------|--------|
| Templates para todos os 25+ event types | ⚠️ Parcial — 13 de 25+ implementados |
| Fallback genérico | ✅ Real |
| Override por request | ✅ Real |
| Templates para Email | ✅ Real |
| Templates para Teams | ✅ Real |
| Template customizável por tenant | ❌ Ausente (Phase 2) |

### 6.2 Canais

| Obrigatório | Estado |
|------------|--------|
| In-app | ✅ Real |
| Email | ✅ Real |
| Teams | ✅ Real |

### 6.3 Retries

| Obrigatório | Estado |
|------------|--------|
| Retry tracking | ✅ Real (campo + método) |
| Retry automático | ❌ **Ausente** (CRÍTICO) |
| Retry manual | ❌ **Ausente** |

### 6.4 Status

| Obrigatório | Estado |
|------------|--------|
| 4 delivery statuses | ✅ Real |
| 5 notification statuses | ✅ Real |
| Distinção Failed vs Skipped | ✅ Real |
