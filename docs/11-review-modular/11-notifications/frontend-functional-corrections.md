# Notifications — Correções Funcionais do Frontend

> **Módulo:** 11 — Notifications  
> **Data:** 2026-03-25  
> **Fase:** N8-R — Reexecução completa  
> **Estado:** ✅ FINALIZADO

---

## 1. Páginas existentes

| # | Página | Ficheiro | Rota | Permissão | Estado |
|---|--------|---------|------|-----------|--------|
| P-01 | NotificationCenterPage | `src/frontend/src/features/notifications/pages/NotificationCenterPage.tsx` | `/notifications` | `notifications:inbox:read` | ✅ Real |
| P-02 | NotificationPreferencesPage | `src/frontend/src/features/notifications/pages/NotificationPreferencesPage.tsx` | `/notifications/preferences` | `notifications:inbox:read` | ✅ Real |
| P-03 | NotificationConfigurationPage | `src/frontend/src/features/notifications/pages/NotificationConfigurationPage.tsx` | `/platform/configuration/notifications` | `platform:admin:read` | ⚠️ Parcial |

### Detalhes por página

#### P-01: NotificationCenterPage
- Lista de notificações com polling a cada 30 segundos
- Filtros: Status, Category, Severity
- Mark as read/unread individual
- Mark all as read
- Badge de contagem não lida
- Deeplink via actionUrl
- ✅ Integração real com API

#### P-02: NotificationPreferencesPage
- Toggle de canais por categoria
- Indicação de notificações mandatórias
- ✅ Integração real com API

#### P-03: NotificationConfigurationPage
- 6 secções de configuração: Types, Channels, Templates, Routing, Consumption, Escalation
- ⚠️ **Parcial**: Secções de UI sem backend dedicado de configuração — depende do módulo Configuration para settings
- Sem endpoints dedicados de admin configuration

---

## 2. Rotas (App.tsx)

| Rota | Componente | Permissão | Lazy | Estado |
|------|-----------|-----------|------|--------|
| `/notifications` | `NotificationCenterPage` | `notifications:inbox:read` | ✅ | ✅ Real |
| `/notifications/preferences` | `NotificationPreferencesPage` | `notifications:inbox:read` | ✅ | ✅ Real |
| `/platform/configuration/notifications` | `NotificationConfigurationPage` | `platform:admin:read` | ✅ | ⚠️ Parcial |

---

## 3. Menu / Sidebar

| Aspecto | Estado | Detalhes |
|---------|--------|---------|
| Sidebar menu item dedicado | ❌ Ausente | **Sem entrada no sidebar para Notifications** |
| Bell icon no header | ✅ Real | `NotificationBell.tsx` em `AppTopbarActions.tsx` |
| Admin config no sidebar | ✅ Real | Em Platform → Configuration → Notifications |

**⚠️ Gap:** O módulo não tem entrada no sidebar principal. O utilizador só acede via bell icon (dropdown) ou admin config. Recomendado adicionar item no sidebar.

---

## 4. Componentes

| Componente | Ficheiro | Propósito | Estado |
|-----------|---------|----------|--------|
| `NotificationBell` | `components/NotificationBell.tsx` | Bell icon + badge + dropdown | ✅ Real |
| `NotificationItem` | `components/NotificationItem.tsx` | Item individual de notificação | ✅ Real |

---

## 5. Hooks

| Hook | Ficheiro | Funções | Estado |
|------|---------|---------|--------|
| `useNotifications` | `hooks/useNotifications.ts` | useNotificationList, useUnreadCount (30s poll), useMarkAsRead, useMarkAsUnread, useMarkAllAsRead | ✅ Real |
| `useNotificationPreferences` | `hooks/useNotificationPreferences.ts` | useGetPreferences, useUpdatePreference | ✅ Real |
| `useNotificationHelpers` | `hooks/useNotificationHelpers.ts` | isUnread(), helper utilities | ✅ Real |

---

## 6. API Client

**Ficheiro:** `src/frontend/src/features/notifications/api/notifications.ts`

| Função | Endpoint | Estado |
|--------|---------|--------|
| `list(params?)` | GET /api/v1/notifications | ✅ Real |
| `getUnreadCount()` | GET /api/v1/notifications/unread-count | ✅ Real |
| `markAsRead(id)` | POST /api/v1/notifications/{id}/read | ✅ Real |
| `markAsUnread(id)` | POST /api/v1/notifications/{id}/unread | ✅ Real |
| `markAllAsRead()` | POST /api/v1/notifications/mark-all-read | ✅ Real |
| `getPreferences()` | GET /api/v1/notifications/preferences | ✅ Real |
| `updatePreference(data)` | PUT /api/v1/notifications/preferences | ✅ Real |

---

## 7. Integração real com API

| Aspecto | Estado | Detalhes |
|---------|--------|---------|
| API client alinhado com backend | ✅ Real | 7 funções → 7 endpoints |
| React Query para data fetching | ✅ Real | useQuery + useMutation |
| Invalidação após mutations | ✅ Real | Query keys invalidados on success |
| Polling automático | ✅ Real | 30 segundos para unread count |
| Error handling | ⚠️ Parcial | Basic error propagation, sem toasts dedicados |

---

## 8. i18n

| Aspecto | Estado | Detalhes |
|---------|--------|---------|
| Chaves i18n nas páginas | ⚠️ Parcial | Necessita validação se todas as strings estão externalizadas |
| Namespace `notifications` | ⚠️ Parcial | Depende de existência em en.json, pt-BR.json, es.json, pt-PT.json |
| Labels dos filtros | ⚠️ Parcial | Verificar se Category e Severity names são i18n |

---

## 9. Botões sem acção / Placeholders

| Elemento | Página | Estado | Detalhes |
|---------|--------|--------|---------|
| Config sections (Types, Channels, etc.) | NotificationConfigurationPage | ⚠️ Placeholder | Secções UI sem backend completo |
| Acknowledge button | NotificationCenterPage | ❌ Ausente | Método existe no domínio, sem endpoint/UI |
| Archive button | NotificationCenterPage | ❌ Ausente | Método existe no domínio, sem endpoint/UI |
| Snooze button | NotificationCenterPage | ❌ Ausente | Método existe no domínio, sem endpoint/UI |
| Delivery status view | — | ❌ Ausente | Sem página/componente de delivery status |

---

## 10. Backlog de correções frontend

| # | Correção | Prioridade | Esforço |
|---|---------|-----------|---------|
| F-01 | 🟠 Adicionar sidebar menu item para `/notifications` | P1 | 1h |
| F-02 | 🟠 Adicionar botões Acknowledge/Archive nas notificações | P1 | 3h |
| F-03 | 🟠 Criar página de delivery status/history | P1 | 8h |
| F-04 | 🟠 Adicionar filtro por dateRange na listagem | P2 | 2h |
| F-05 | 🟠 Adicionar filtro por sourceModule na listagem | P2 | 1h |
| F-06 | 🟠 Adicionar filtro por EnvironmentId na listagem | P2 | 2h |
| F-07 | 🟡 Adicionar botão Snooze com datepicker | P2 | 3h |
| F-08 | 🟡 Validar i18n em todas as páginas/componentes | P2 | 4h |
| F-09 | 🟡 Implementar toast notifications para mutations | P2 | 2h |
| F-10 | 🟡 Adicionar retry button na delivery history | P2 | 2h |
| F-11 | 🟡 Completar NotificationConfigurationPage com endpoints reais | P3 | 8h |
| F-12 | 🟡 Adicionar indicação visual de EnvironmentId nas notificações | P3 | 2h |

**Esforço total estimado:** ~38h
