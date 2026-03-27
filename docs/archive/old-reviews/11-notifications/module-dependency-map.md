# Notifications — Mapa de Dependências do Módulo

> **Módulo:** 11 — Notifications  
> **Data:** 2026-03-25  
> **Fase:** N8-R — Reexecução completa  
> **Estado:** ✅ FINALIZADO

---

## 1. Visão geral das dependências

O módulo Notifications é **transversal por design** — é consumidor de eventos de praticamente todos os outros módulos, e expõe uma interface pública para submissão de notificações.

```
┌──────────────────────────────────────────────────────┐
│                   NOTIFICATIONS                       │
│                                                       │
│  CONSOME EVENTOS DE:              EXPÕE:             │
│  ├── Identity & Access            ├── INotificationModule │
│  ├── Change Governance            ├── 3 Integration Events │
│  ├── Operational Intelligence     └── 7 REST Endpoints    │
│  ├── Catalog (Contracts)                              │
│  ├── Governance (Compliance)      DEPENDE DE:        │
│  ├── Governance (FinOps)          ├── Configuration   │
│  ├── AI & Knowledge               ├── Identity (auth) │
│  └── Integrations                 └── BuildingBlocks  │
└──────────────────────────────────────────────────────┘
```

---

## 2. Dependência com Identity & Access

### O que consome

| Elemento | Tipo | Detalhes |
|---------|------|---------|
| `ICurrentUser` | Interface | Identifica o utilizador corrente para scoping |
| `ICurrentTenant` | Interface | Identifica o tenant corrente para RLS |
| Eventos de segurança | Domain Events | `SecurityNotificationHandler` consome BreakGlassActivated, SecurityIncident, UnauthorizedAccess |
| Autenticação JWT | Middleware | Endpoints protegidos por JWT |
| Permissões | Authorization | `notifications:inbox:read/write`, `notifications:preferences:read/write` |

### O que expõe a Identity

| Elemento | Tipo | Detalhes |
|---------|------|---------|
| Nada directamente | — | Identity não consome do Notifications |

### Gaps

| Gap | Impacto |
|-----|---------|
| 🔴 Permissões `notifications:*` não registadas no `RolePermissionCatalog` | Utilizadores não acedem a notificações |
| 🟠 RecipientUserId é Guid opaco — sem validação de existência no Identity | Notificações podem ser criadas para users inexistentes |

---

## 3. Dependência com Configuration

### O que consome

| Elemento | Tipo | Detalhes |
|---------|------|---------|
| `notifications.enabled` | Config key | Enable/disable módulo |
| `notifications.email.enabled` | Config key | Enable/disable canal Email |
| `notifications.teams.enabled` | Config key | Enable/disable canal Teams |
| `notifications.quiet_hours.start` | Config key | Início quiet hours |
| `notifications.quiet_hours.end` | Config key | Fim quiet hours |
| `notifications.types.enabled` | Config key | Tipos activos |
| `notifications.categories.enabled` | Config key | Categorias activas |
| `notifications.severity.default` | Config key | Severidade default |

### O que expõe a Configuration

| Elemento | Tipo | Detalhes |
|---------|------|---------|
| Nada directamente | — | Configuration não consome do Notifications |

### Gaps

| Gap | Impacto |
|-----|---------|
| ⚠️ Config keys podem não existir no seed do Configuration module | Funcionalidades podem não funcionar sem seed |

---

## 4. Dependência com Change Governance

### O que consome

| Elemento | Tipo | Detalhes |
|---------|------|---------|
| Approval events | Domain Events | `ApprovalNotificationHandler` consome ApprovalPending, ApprovalApproved, ApprovalRejected, ApprovalExpiring |

### O que expõe a Change Governance

| Elemento | Tipo | Detalhes |
|---------|------|---------|
| `INotificationModule.SubmitAsync()` | Interface | Change Gov pode submeter notificações directamente |
| Notificações de aprovação | In-app + Email/Teams | Alertas de aprovação pendente |

---

## 5. Dependência com Operational Intelligence

### O que consome

| Elemento | Tipo | Detalhes |
|---------|------|---------|
| Incident events | Domain Events | `IncidentNotificationHandler` consome IncidentCreated, IncidentEscalated, IncidentResolved |
| Anomaly events | Domain Events | AnomalyDetected, HealthDegradation |

### O que expõe a OI

| Elemento | Tipo | Detalhes |
|---------|------|---------|
| Notificações de incidente | In-app + Email/Teams | Alertas de incidentes |
| CorrelateWithIncident() | Domain method | Correlação notificação ↔ incidente |

---

## 6. Dependência com Audit & Compliance

### O que consome

| Elemento | Tipo | Detalhes |
|---------|------|---------|
| Nada directamente | — | Audit consome os integration events do Notifications |

### O que expõe a Audit

| Elemento | Tipo | Detalhes |
|---------|------|---------|
| `NotificationCreatedIntegrationEvent` | Integration Event | Registar criação de notificação |
| `NotificationDeliveredIntegrationEvent` | Integration Event | Registar entrega |
| `NotificationDeliveryFailedIntegrationEvent` | Integration Event | Registar falha |
| Domain events | Outbox pattern | NotificationCreatedEvent, NotificationReadEvent, NotificationDeliveryCompletedEvent |

---

## 7. Dependência com Governance (Compliance + FinOps)

### O que consome

| Elemento | Tipo | Detalhes |
|---------|------|---------|
| Compliance events | Domain Events | `ComplianceNotificationHandler` consome ComplianceViolation, PolicyViolation |
| Budget events | Domain Events | `BudgetNotificationHandler` consome BudgetAlert, AnomalyDetected |

---

## 8. Dependência com AI & Knowledge

### O que consome

| Elemento | Tipo | Detalhes |
|---------|------|---------|
| AI governance events | Domain Events | `AiGovernanceNotificationHandler` consome AiGovernancePolicyViolation, AiCostAnomaly |

---

## 9. Dependência com Catalog (Contracts)

### O que consome

| Elemento | Tipo | Detalhes |
|---------|------|---------|
| Contract events | Domain Events | `CatalogNotificationHandler` consome ContractPublished, BreakingChangeDetected, ContractValidationFailed |

---

## 10. Dependência com Integrations

### O que consome

| Elemento | Tipo | Detalhes |
|---------|------|---------|
| Integration failure events | Domain Events | `IntegrationFailureNotificationHandler` consome IntegrationFailure |
| SMTP/Teams webhook config | Infrastructure | Canal de delivery depende de configuração de integração |

---

## 11. Impacto em Notifications (incoming)

Nenhum módulo depende directamente do Notifications para funcionar. O módulo é um **consumidor passivo** que reage a eventos de outros módulos.

---

## 12. O que o módulo expõe

| Artefacto | Tipo | Consumidores |
|-----------|------|-------------|
| `INotificationModule` | Interface pública | Qualquer módulo que queira submeter notificação |
| `INotificationModule.SubmitAsync(NotificationRequest)` | Método | Submissão programática |
| `INotificationModule.GetUnreadCountAsync(userId)` | Método | Consulta de contagem |
| `NotificationCreatedIntegrationEvent` | Integration Event (outbox) | Audit, Analytics |
| `NotificationDeliveredIntegrationEvent` | Integration Event (outbox) | Audit, Analytics |
| `NotificationDeliveryFailedIntegrationEvent` | Integration Event (outbox) | Audit, Ops monitoring |
| 7 REST endpoints | API | Frontend, ferramentas externas |

---

## 13. O que o módulo consome

| Artefacto | Tipo | Fonte |
|-----------|------|-------|
| Domain events de 8 módulos | Events (MediatR) | Via event handlers |
| `ICurrentUser` / `ICurrentTenant` | Interfaces | BuildingBlocks |
| `IDateTimeProvider` | Interface | BuildingBlocks |
| `NexTraceDbContextBase` | Base class | BuildingBlocks.Infrastructure |
| Configuration keys | Settings | Configuration module |
| JWT / Auth middleware | Security | BuildingBlocks.Security |

---

## 14. Diagrama de dependências

```
                    ┌─────────────┐
                    │ Identity &  │──security events──→┐
                    │   Access    │←─auth/user context  │
                    └─────────────┘                     │
                    ┌─────────────┐                     │
                    │Configuration│──config keys──→     │
                    └─────────────┘                     │
                    ┌─────────────┐              ┌──────▼──────┐
                    │   Change    │──approval──→  │             │
                    │ Governance  │              │ NOTIFICATIONS │──integration events──→ Audit
                    └─────────────┘              │             │
                    ┌─────────────┐              │   8 Event   │──REST endpoints──→ Frontend
                    │ Operational │──incidents──→ │  Handlers   │
                    │Intelligence │              │             │
                    └─────────────┘              │   7 API     │
                    ┌─────────────┐              │  Endpoints  │
                    │  Catalog    │──contracts──→│             │
                    │ (Contracts) │              └─────────────┘
                    └─────────────┘                     ▲
                    ┌─────────────┐                     │
                    │ Governance  │──compliance/budget───┘
                    └─────────────┘                     ▲
                    ┌─────────────┐                     │
                    │AI & Knowledge──ai events──────────┘
                    └─────────────┘                     ▲
                    ┌─────────────┐                     │
                    │Integrations │──int. failures──────┘
                    └─────────────┘
```

---

## 15. Riscos de dependência

| # | Risco | Impacto | Mitigação |
|---|-------|---------|----------|
| D-01 | Event handlers falham silenciosamente se evento não é reconhecido | Notificações não geradas | Logging + alerting nos handlers |
| D-02 | Configuration keys não seedadas | Canais/quiet hours não funcionam | Incluir no seed do Configuration module |
| D-03 | Identity não valida RecipientUserId | Notificações criadas para users inexistentes | Validar user existence antes de criar |
| D-04 | Circular dependency potencial se Notifications emitir eventos para outros módulos | Loops infinitos | Não emitir domain events que triggrem notificações |
