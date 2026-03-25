# NexTraceOne — Notifications Module

## Visão Geral

O módulo Notifications é o motor real de notificações e alertas do NexTraceOne.
Responsável pela entrega de notificações internas (inbox), entrega externa (email, Teams),
templates, canais, retries, status de entrega, preferências de utilizador e histórico auditável.

## Escopo do Módulo

### O que PERTENCE ao Notifications:
- **Notificações Internas** (inbox) — criação, leitura, acknowledge, arquivo, dismiss
- **Entrega Externa** — Email (SMTP), Microsoft Teams (webhook/Adaptive Cards)
- **Templates** — resolução de templates por tipo de evento
- **Canais** — roteamento por preferência, severidade e política
- **Retries** — tentativas de reentrega com backoff
- **Status de Entrega** — rastreabilidade completa (Pending→Delivered/Failed/Skipped)
- **Preferências** — opt-in/out por categoria × canal, com políticas mandatórias
- **Intelligence** — deduplicação, agrupamento, supressão, escalação, quiet hours, digest
- **Governança** — auditoria, métricas, health, catálogo de tipos

### O que NÃO PERTENCE ao Notifications:
- **Definição de incidentes** → Módulo Operational Intelligence
- **Workflow de aprovação** → Módulo Change Governance
- **Contratos** → Módulo Contracts
- **Gestão de ambientes** → Módulo Environment Management
- **Auditoria cross-module** → Módulo Audit & Compliance

## Arquitetura

```
NexTraceOne.Notifications.Domain/
├── Entities/          → 3 entidades (Notification, NotificationDelivery, NotificationPreference)
├── Enums/             → 6 enums (DeliveryChannel, DeliveryStatus, NotificationCategory, etc.)
├── Events/            → 3 domain events
└── StronglyTypedIds/  → 3 IDs tipados

NexTraceOne.Notifications.Application/
├── Abstractions/      → 18 interfaces de serviço
├── Engine/            → Orchestrator, TemplateResolver, ModuleService
├── ExternalDelivery/  → Contratos para entrega externa
└── Features/          → 7 features CQRS

NexTraceOne.Notifications.Infrastructure/
├── Persistence/       → NotificationsDbContext (3 DbSets) + 3 configs + 3 repos
├── EventHandlers/     → 8 handlers (Approval, Catalog, Compliance, Budget, etc.)
├── ExternalDelivery/  → Email + Teams dispatchers, routing engine
├── Governance/        → Audit, Catalog governance, Health, Metrics
├── Intelligence/      → Digest, Escalation, Grouping, Suppression, Quiet hours
├── Preferences/       → Mandatory policy, Preference service
└── Routing/           → Recipient resolver

NexTraceOne.Notifications.API/
└── Endpoints/         → NotificationCenterEndpointModule (7 endpoints)
```

## Entities

| Entidade | Tipo | Responsabilidade |
|----------|------|-----------------|
| `Notification` | Aggregate Root | Notificação interna com lifecycle completo |
| `NotificationDelivery` | Entity | Registo de entrega por canal externo |
| `NotificationPreference` | Entity | Preferência utilizador × categoria × canal |

## Fluxo Ponta a Ponta

1. **Evento de origem** (Change Governance, OpIntel, etc.) → EventHandler
2. **Resolução de template** → NotificationTemplateResolver
3. **Criação da Notification** → Orchestrator
4. **Deduplicação/Agrupamento** → Intelligence services
5. **Seleção de canais** → Routing engine + preferências
6. **Entrega InApp** → Persistência imediata
7. **Entrega externa** → ExternalDeliveryService (Email, Teams)
8. **Retry** → Backoff configurável
9. **Status tracking** → Pending → Delivered / Failed / Skipped
10. **Auditoria** → NotificationAuditService

## Base de Dados

### Tabelas (prefixo ntf_)
| Tabela | Entidade | Descrição |
|--------|---------|-----------|
| `ntf_notifications` | Notification | Central de notificações internas |
| `ntf_deliveries` | NotificationDelivery | Entregas por canal externo |
| `ntf_preferences` | NotificationPreference | Preferências dos utilizadores |

### Concorrência Otimista
PostgreSQL xmin via `RowVersion` em: Notification, NotificationDelivery.

### Check Constraints
- `CK_ntf_notifications_status`: Status IN (Unread, Read, Acknowledged, Archived, Dismissed)
- `CK_ntf_notifications_category`: Category IN (11 valores)
- `CK_ntf_notifications_severity`: Severity IN (Info, ActionRequired, Warning, Critical)
- `CK_ntf_deliveries_status`: Status IN (Pending, Delivered, Failed, Skipped)
- `CK_ntf_deliveries_channel`: Channel IN (InApp, Email, MicrosoftTeams)
- `CK_ntf_preferences_category`: Category IN (11 valores)
- `CK_ntf_preferences_channel`: Channel IN (InApp, Email, MicrosoftTeams)

### Foreign Keys
- `ntf_deliveries.NotificationId` → `ntf_notifications.Id` (CASCADE)

## Permissões

| Permissão | Escopo | Roles |
|-----------|--------|-------|
| `notifications:inbox:read` | Consultar notificações, unread count | Todos os roles |
| `notifications:inbox:write` | Marcar como lido/não lido, mark-all-read | PlatformAdmin, TechLead, Developer, ApprovalOnly |
| `notifications:preferences:read` | Consultar preferências | Todos os roles |
| `notifications:preferences:write` | Alterar preferências | PlatformAdmin, TechLead, Developer |

## Endpoints

| Método | Rota | Permissão |
|--------|------|-----------|
| GET | `/api/v1/notifications` | `notifications:inbox:read` |
| GET | `/api/v1/notifications/unread-count` | `notifications:inbox:read` |
| POST | `/api/v1/notifications/{id}/read` | `notifications:inbox:write` |
| POST | `/api/v1/notifications/{id}/unread` | `notifications:inbox:write` |
| POST | `/api/v1/notifications/mark-all-read` | `notifications:inbox:write` |
| GET | `/api/v1/notifications/preferences` | `notifications:preferences:read` |
| PUT | `/api/v1/notifications/preferences` | `notifications:preferences:write` |

## Event Handlers (Módulos Emissores)

| Handler | Módulo Emissor | Eventos |
|---------|---------------|---------|
| `ApprovalNotificationHandler` | Change Governance | ApprovalPending, ApprovalApproved, etc. |
| `CatalogNotificationHandler` | Catalog | ContractPublished, BreakingChangeDetected |
| `IncidentNotificationHandler` | Operational Intelligence | IncidentCreated, IncidentEscalated |
| `SecurityNotificationHandler` | Identity & Access | BreakGlassActivated, JitAccessPending |
| `ComplianceNotificationHandler` | Audit & Compliance | ComplianceCheckFailed, PolicyViolated |
| `BudgetNotificationHandler` | Governance | BudgetExceeded, BudgetThresholdReached |
| `IntegrationFailureNotificationHandler` | Integrations | IntegrationFailed, SyncFailed |
| `AiGovernanceNotificationHandler` | AI & Knowledge | AiProviderUnavailable, TokenBudgetExceeded |

## Testes

412 testes cobrindo: Domain, Application features, Engine, ExternalDelivery, Governance, Intelligence, Preferences, Routing, e todos os EventHandlers.
