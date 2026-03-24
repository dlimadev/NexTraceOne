# Notifications — Consolidated Module Report

> Gerado a partir da consolidação de todos os relatórios de auditoria e revisão modular do NexTraceOne.
> Última atualização: 2026-03-24

---

## 1. Visão Geral do Módulo

O módulo **Notifications** é o centro de notificações transversal do NexTraceOne, responsável por:

- Entrega de notificações multi-canal (Email, Teams)
- Preferências do utilizador
- Templates e routing de notificações
- Deduplicação, agrupamento e digest
- Escalação e quiet hours
- Integração com todos os módulos via event handlers

### Posição na arquitetura

Backend em `src/modules/notifications/` com `NotificationsDbContext`. Frontend em `src/frontend/src/features/notifications/`. O módulo é consumidor transversal — recebe eventos de todos os outros módulos via 8 event handlers dedicados.

---

## 2. Estado Atual

| Dimensão | Valor |
|----------|-------|
| **Maturidade global** | **70%** |
| Backend | 85% (orquestrador completo, 10 serviços, 8 event handlers) |
| Frontend | 70% (3 páginas, 4 hooks) |
| Documentação | 30% (12 ficheiros NOTIFICATIONS-* fragmentados, sem doc unificada) |
| Testes | 65% |
| **Prioridade** | P5 (Suporte) |
| **Status** | ✅ Funcional |

---

## 3. Problemas Críticos e Bloqueadores

Não existem P0 blockers neste módulo. Problemas identificados:

### ⚠️ Documentação fragmentada

12 ficheiros `NOTIFICATIONS-*` nos diretórios de execução sem documentação consolidada unificada.

### ⚠️ Sem item dedicado no menu

Notificações são acessíveis apenas via bell icon no header. Não existe entrada no sidebar dedicada.

---

## 4. Arquitetura Backend

### Serviços Core (10 serviços)

| Serviço | Propósito |
|---------|-----------|
| NotificationOrchestrator | Orquestração principal |
| NotificationTemplateResolver | Resolução de templates |
| NotificationRoutingEngine | Routing por canal |
| NotificationChannelDispatcher | Dispatch para canais (Teams, Email) |
| NotificationEscalationService | Escalação automática |
| QuietHoursService | Horários silenciosos |
| NotificationGroupingService | Agrupamento |
| NotificationDigestService | Digests periódicos |
| NotificationDeduplicationService | Deduplicação |
| NotificationPreferenceService | Preferências do utilizador |

### Event Handlers (8 handlers)

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

### Entidades

| Entidade | Propósito |
|----------|-----------|
| Notification | Notificação individual |
| NotificationDelivery | Estado de entrega por canal |
| NotificationPreference | Preferências do utilizador |

---

## 5. Frontend

| Página | Rota | Estado |
|--------|------|--------|
| NotificationCenterPage | `/notifications` | ✅ Funcional (unread count polling 30s) |
| NotificationPreferencesPage | `/notifications/preferences` | ✅ Funcional |
| NotificationConfigurationPage | `/platform/configuration/notifications` | ✅ Funcional (6 secções) |

### Hooks (4 hooks)

| Hook | Propósito |
|------|-----------|
| useNotifications | Notificações com unread count |
| useNotificationList | Lista com React Query |
| useNotificationPreferences | Gestão de preferências |
| useNotificationHelpers | Utilitários |

---

## 6. Ações Recomendadas

| # | Ação | Prioridade | Esforço |
|---|------|-----------|---------|
| 1 | Validar delivery end-to-end (evento → notificação → delivery) | P1 | 2h |
| 2 | Validar preferências do utilizador | P1 | 1h |
| 3 | Considerar adicionar Notifications ao menu sidebar | P2 | 30min |
| 4 | Criar documentação unificada (consolidar 12 ficheiros NOTIFICATIONS-*) | P2 | 3h |
| 5 | Documentar todos os event handlers e seus triggers | P2 | 2h |

---

## 7. Dependências

| Módulo | Relação |
|--------|---------|
| Todos os módulos | **Forte** — Recebe eventos via event handlers |
| Identity & Access | **Forte** — Preferências vinculadas ao utilizador |
| Configuration | **Média** — Templates e routing configuráveis |

---

## 8. Estado do Consolidado

| Aspeto | Valor |
|--------|-------|
| Consolidado | `CONSOLIDATED_OK` |
| Razão | Module review substantivo com dados reais do código |
| Próximo passo | Consolidar documentação fragmentada e validar delivery end-to-end |
