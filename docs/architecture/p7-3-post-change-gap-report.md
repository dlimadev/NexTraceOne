# P7.3 — Post-Change Gap Report

**Data:** 2026-03-27
**Fase:** P7.3 — Correlação auditável origem → notificação → entrega

---

## 1. O que foi resolvido

| Área | Estado após P7.3 |
|---|---|
| `Notification.SourceEventId` | ✅ Implementado — campo explícito para ligar à origem do evento |
| `NotificationAuditService` | ✅ Implementado — chama `IAuditModule.RecordEventAsync()` real com hash chain |
| Ponte para AuditCompliance | ✅ Implementado — `AuditCompliance.Contracts` referenciado em `Notifications.Infrastructure` |
| Auditoria na criação de notificação | ✅ Implementado — `NotificationOrchestrator` chama audit após cada criação |
| Auditoria na entrega/falha/retry | ✅ Implementado — `ExternalDeliveryService` chama audit em todos os pontos relevantes |
| Endpoint `/notifications/{id}/trail` | ✅ Implementado — retorna correlação completa: origem + notificação + entregas |
| Frontend types e hook | ✅ Implementado — `NotificationTrailResponse`, `useNotificationTrail` |
| EF migration | ✅ Implementado — `P7_3_NotificationSourceEventId` |
| Testes de correlação | ✅ Implementado — 15 novos testes; total: 470 passing |

---

## 2. O que ficou pendente

### 2.1 Correlação explícita no handler de eventos

Os event handlers (e.g., `IncidentNotificationHandler`, `ApprovalNotificationHandler`) ainda não passam `SourceEventId` no `NotificationRequest`. O campo existe e é suportado pelo modelo, mas os handlers precisam ser atualizados caso a caso para mapear o ID específico do evento de origem (e.g., `@event.IncidentId.ToString()`).

**Impacto:** `SourceEventId` fica `null` para notificações geradas automaticamente por event handlers, até cada handler ser explicitamente atualizado.

**Razão do adiamento:** Cada handler tem um tipo diferente de evento de origem. A atualização é mecânica mas envolve ~10 handlers e exige validação por tipo de evento.

### 2.2 Consulta de AuditEvent por NotificationId

Não existe ainda um endpoint dedicado para listar `AuditEvent` filtrados por `NotificationId` ou `SourceEventId`. Os AuditEvents são persistidos corretamente no `aud_audit_events` via hash chain, mas a consulta usa `ResourceId` (que é o `NotificationId` ou `DeliveryId`).

**Impacto:** A triagem forense completa requer chamada separada ao módulo Audit com `ResourceId` / `ResourceType`.

### 2.3 SourceEventId nos event handlers

| Handler | SourceEventId disponível? | Status |
|---|---|---|
| `IncidentNotificationHandler` | `@event.IncidentId` | ⏳ Não mapeado ainda |
| `ApprovalNotificationHandler` | `@event.WorkflowId` / `@event.ApprovalId` | ⏳ Não mapeado ainda |
| `SecurityNotificationHandler` | `@event.SessionId` / evento específico | ⏳ Não mapeado ainda |
| `CatalogNotificationHandler` | `@event.ContractId` | ⏳ Não mapeado ainda |
| `ComplianceNotificationHandler` | `@event.PolicyId` / `@event.CampaignId` | ⏳ Não mapeado ainda |
| `BudgetNotificationHandler` | `@event.BudgetId` | ⏳ Não mapeado ainda |

---

## 3. O que fica explicitamente para P7.4

| Item | Justificativa |
|---|---|
| Atualização dos event handlers para mapear `SourceEventId` | Mecânico mas cross-cutting; melhor fechar em fase dedicada |
| Endpoint `GET /audit-events?resourceId={notificationId}` na API Audit | Consulta forense completa no módulo Audit |
| UI de trilha em `NotificationCenterPage` | Componente de visualização da trilha (requer UX definido) |
| Indexação de `SourceEventId` para queries | Performance em produção com volume alto |
| Correlação reversa: dado um `SourceEventId`, listar notificações geradas | Útil para operadores investigando um incidente específico |

---

## 4. Limitações residuais

| Limitação | Impacto | Mitigação disponível |
|---|---|---|
| `SourceEventId` é `null` em notificações antigas (pré-P7.3) | Trilha incompleta para notificações antigas | Sem impacto em notificações futuras |
| `SourceEventId` não é indexado | Queries por `SourceEventId` podem ser lentas com volume alto | Adicionar índice em P7.4 |
| Auditoria é best-effort | Se `IAuditModule` falhar repetidamente, a trilha pode ter gaps | Outbox pattern resolveria; aceite como trade-off em MVP |
| Event handlers não passam `SourceEventId` | Notificações automáticas ficam sem correlação explícita ao evento | Legível via `SourceEntityId` + `EventType` até P7.4 |

---

## 5. Estado geral do módulo Notifications após P7.3

| Capacidade | Estado |
|---|---|
| Backend — persistência básica | ✅ Completo (P7.1) |
| Backend — templates, channels, SMTP config | ✅ Completo (P7.1) |
| Backend — retries, delivery status, history | ✅ Completo (P7.2) |
| Backend — correlação auditável origem → notificação → entrega | ✅ Completo (P7.3) |
| Backend — event handlers com SourceEventId | ⏳ Pendente (P7.4) |
| Frontend — páginas de notificação | ✅ Completo (existia antes) |
| Frontend — hook de trilha de correlação | ✅ Completo (P7.3) |
| SMTP — wiring funcional mínimo | ✅ Completo (P7.2) |
| SMTP — encriptação de password | ⏳ Pendente (P7.4) |
