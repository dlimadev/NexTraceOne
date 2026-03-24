# Revisão Modular — Notifications

> **Data:** 2026-03-24  
> **Prioridade:** P5 (Suporte)  
> **Módulo Backend:** `src/modules/notifications/`  
> **Módulo Frontend:** `src/frontend/src/features/notifications/`  
> **Fonte de verdade:** Código do repositório

---

## 1. Propósito do Módulo

O módulo **Notifications** é o centro de notificações do NexTraceOne, responsável por:

- Entrega de notificações multi-canal (Email, Teams)
- Preferências do utilizador
- Templates e routing
- Deduplicação, agrupamento, digest
- Escalação e quiet hours
- Integração com todos os módulos via event handlers

---

## 2. Aderência ao Produto

| Aspecto | Avaliação | Observação |
|---------|-----------|------------|
| Alinhamento | ✅ Forte | Notificações são transversais e essenciais |
| Completude backend | ✅ Alta | Orquestrador completo, 8 event handlers, 10+ serviços |
| Frontend | ✅ Funcional | 3 páginas, 4 hooks dedicados |
| Documentação | ⚠️ Fragmentada | 12 ficheiros execution/NOTIFICATIONS-* sem doc unificada |

---

## 3. Páginas Frontend

| Página | Rota | Estado | Funcionalidade |
|--------|------|--------|----------------|
| NotificationCenterPage | `/notifications` | ✅ Funcional | Centro de notificações (unread count polling 30s) |
| NotificationPreferencesPage | `/notifications/preferences` | ✅ Funcional | Preferências do utilizador |
| NotificationConfigurationPage | `/platform/configuration/notifications` | ✅ Funcional | 6 secções: types, channels, templates, routing, consumption, escalation |

### Nota de Navegação

Notificações **não têm item dedicado no menu** — acesso via bell icon no header e configuração admin.

---

## 4. Backend — Arquitetura

### 4.1 Serviços Core

| Serviço | Propósito |
|---------|-----------|
| NotificationOrchestrator | Orquestração principal |
| NotificationTemplateResolver | Resolução de templates |
| NotificationRoutingEngine | Routing por canal |
| NotificationChannelDispatcher | Dispatch para canais (Teams, Email) |
| NotificationEscalationService | Escalação automática |
| QuietHoursService | Horários silenciosos |
| NotificationGroupingService | Agrupamento de notificações |
| NotificationDigestService | Digests periódicos |
| NotificationDeduplicationService | Deduplicação |
| NotificationPreferenceService | Preferências do utilizador |

### 4.2 Event Handlers (8 handlers)

| Handler | Fonte |
|---------|-------|
| CatalogNotificationHandler | Eventos de catálogo |
| IncidentNotificationHandler | Eventos de incidentes |
| ApprovalNotificationHandler | Aprovações de workflow |
| ComplianceNotificationHandler | Eventos de compliance |
| AiGovernanceNotificationHandler | Eventos de IA |
| IntegrationFailureNotificationHandler | Falhas de integração |
| BudgetNotificationHandler | Alertas de budget |
| SecurityNotificationHandler | Eventos de segurança |

### 4.3 Entidades

| Entidade | Propósito |
|----------|-----------|
| Notification | Notificação individual |
| NotificationDelivery | Estado de entrega por canal |
| NotificationPreference | Preferências do utilizador |

### 4.4 Hooks Frontend (4 hooks)

| Hook | Propósito |
|------|-----------|
| useNotifications | Notificações com unread count (polling 30s) |
| useNotificationList | Lista com React Query |
| useNotificationPreferences | Gestão de preferências |
| useNotificationHelpers | Utilitários |

---

## 5. Banco de Dados

| DbContext | Entidades |
|-----------|-----------|
| NotificationsDbContext | Notification, NotificationDelivery, NotificationPreference |

---

## 6. Resumo de Ações

| # | Ação | Prioridade | Esforço |
|---|------|-----------|---------|
| 1 | Validar delivery end-to-end (criar evento → notificação → delivery) | P1 | 2h |
| 2 | Validar preferências do utilizador | P1 | 1h |
| 3 | Considerar adicionar Notifications ao menu (atualmente apenas bell icon) | P2 | 30 min |
| 4 | **Criar documentação unificada** — consolidar 12 ficheiros NOTIFICATIONS-* | P2 | 3h |
| 5 | Documentar todos os event handlers e seus triggers | P2 | 2h |
