# Integrations — Module Dependency Map

> **Module:** Integrations (12)  
> **Date:** 2026-03-25  
> **Status:** Dependency map completo

---

## 1. Dependência com Governance (08)

| Aspecto | Tipo | Detalhe |
|---------|------|---------|
| **Backend fisicamente dentro de Governance** | 🔴 COUPLING FORTE | Entidades, endpoints, features, DbSets todos em `src/modules/governance/` |
| **GovernanceDbContext partilhado** | 🔴 COUPLING FORTE | 3 DbSets de Integrations dentro de GovernanceDbContext |
| **Migrations partilhadas** | 🔴 COUPLING FORTE | 3 migrations de Integrations nos migrations de Governance |
| **Table prefix errado** | 🔴 COUPLING FORTE | Tabelas usam `gov_` em vez de `int_` |
| **Policy consumption** (futuro) | 🟡 INTEGRAÇÃO NORMAL | Integrations consumirá policies de Governance via eventos |
| **Compliance reporting** (futuro) | 🟡 INTEGRAÇÃO NORMAL | Governance consumirá dados de Integrations via read models |

**Acção:** Extrair toda a parte de Integrations de Governance para módulo independente.

---

## 2. Dependência com Notifications (11)

| Aspecto | Tipo | Detalhe |
|---------|------|---------|
| **IntegrationFailureNotificationHandler** | 🟡 INTEGRAÇÃO NORMAL | Handler em Notifications que consome eventos de falha de integração |
| **Publicação de eventos** | ❌ NÃO IMPLEMENTADO | Integrations não publica eventos — handler existe mas sem trigger |

**Acção:** Publicar `ConnectorFailedEvent`, `ConnectorHealthChangedEvent` para que Notifications os consuma.

---

## 3. Dependência com Operational Intelligence (06)

| Aspecto | Tipo | Detalhe |
|---------|------|---------|
| **Métricas de integração** | 🟡 INTEGRAÇÃO NORMAL (futuro) | OI consumirá métricas de health, execuções, freshness |
| **Alertas operacionais** | 🟡 INTEGRAÇÃO NORMAL (futuro) | OI poderá correlacionar falhas de integração com incidentes |
| **Implementação actual** | ❌ NÃO IMPLEMENTADO | Sem publicação de eventos de OI |

**Acção:** Publicar `ExecutionCompletedEvent` e `ConnectorHealthChangedEvent` para consumo por OI.

---

## 4. Dependência com Audit & Compliance (10)

| Aspecto | Tipo | Detalhe |
|---------|------|---------|
| **Auditoria de acções** | 🔴 OBRIGATÓRIO | Toda acção de escrita deve ser auditada |
| **Implementação actual** | ❌ NÃO IMPLEMENTADO | Zero acções auditadas |

**Acção:** Publicar audit events para create, update, delete, retry, reprocess, credential management.

---

## 5. Dependência com Identity & Access (01)

| Aspecto | Tipo | Detalhe |
|---------|------|---------|
| **JWT Authentication** | ✅ IMPLEMENTADO | Via building blocks |
| **TenantId / RLS** | ✅ IMPLEMENTADO | Via `TenantRlsInterceptor` |
| **Permissões** | ✅ IMPLEMENTADO | `integrations:read`, `integrations:write` em RolePermissionCatalog |
| **Roles** | ✅ IMPLEMENTADO | PlatformAdmin (read+write), TechLead (read), Developer (read) |

**Estado:** Dependência resolvida e funcional.

---

## 6. Dependência com Change Governance (05)

| Aspecto | Tipo | Detalhe |
|---------|------|---------|
| **Alterações a conectores como changes** | 🟢 OPCIONAL | Criação/edição de conectores pode ser registada como change |
| **Change-to-incident correlation** | 🟢 OPCIONAL | Alteração de integração pode causar incidente |
| **Implementação actual** | ❌ NÃO IMPLEMENTADO | — |

**Acção:** Futuro — publicar `ConnectorConfigurationChangedEvent` para consumo por Change Governance.

---

## 7. Dependência com Configuration (09)

| Aspecto | Tipo | Detalhe |
|---------|------|---------|
| **Parâmetros globais** | 🟡 INTEGRAÇÃO NORMAL (futuro) | Retry defaults, timeout defaults, freshness thresholds |
| **Implementação actual** | ❌ NÃO IMPLEMENTADO | Thresholds hardcoded |

**Acção:** Ler parâmetros de retry e freshness de Configuration quando disponível.

---

## 8. Dependência com Environment Management (02)

| Aspecto | Tipo | Detalhe |
|---------|------|---------|
| **EnvironmentId** | 🟡 INTEGRAÇÃO NORMAL (futuro) | Conectores devem referenciar Environment formal |
| **Implementação actual** | ⚠️ PARCIAL | `Environment` é string livre ("Production", "Staging") |

**Acção:** Substituir string por `EnvironmentId?` (Guid) referenciando Environment Management.

---

## 9. O que o módulo expõe para outros

| Evento/Interface | Consumidores | Estado |
|-----------------|-------------|--------|
| `ConnectorCreatedEvent` | Governance (policy check), Audit | ❌ Não implementado |
| `ConnectorUpdatedEvent` | Audit | ❌ Não implementado |
| `ConnectorFailedEvent` | Notifications (alerta), OI (métrica) | ❌ Não implementado |
| `ConnectorHealthChangedEvent` | OI (dashboard), Governance (compliance) | ❌ Não implementado |
| `ExecutionCompletedEvent` | OI (métricas), Audit (se sensível) | ❌ Não implementado |
| `SourceTrustPromotedEvent` | Audit, Governance | ❌ Não implementado |
| Read endpoints (GET) | Qualquer módulo via HTTP | ✅ Implementado |

---

## 10. O que o módulo consome de outros

| Evento/Interface | Produtor | Estado |
|-----------------|---------|--------|
| JWT token / TenantId | Identity & Access (01) | ✅ Via building blocks |
| Permission claims | Identity & Access (01) | ✅ Via `PermissionAuthorizationHandler` |
| Integration policies | Governance (08) | ❌ Não implementado |
| Global configuration params | Configuration (09) | ❌ Não implementado |
| Environment definitions | Environment Management (02) | ❌ Não implementado |

---

## 11. Diagrama de dependências

```
                    ┌──────────────────┐
                    │  Identity &      │
                    │  Access (01)     │
                    │  [JWT, RLS,      │
                    │   Permissions]   │
                    └────────┬─────────┘
                             │ ✅ CONSUMES
                             ▼
┌─────────────┐    ┌──────────────────┐    ┌─────────────────┐
│ Environment │◄───│                  │───►│  Governance     │
│ Mgmt (02)   │ ❌ │  INTEGRATIONS    │ ❌ │  (08)           │
│ [Env refs]  │    │      (12)        │    │  [Policy check] │
└─────────────┘    │                  │    └─────────────────┘
                   │  🔴 Currently    │
┌─────────────┐    │  inside Gov!     │    ┌─────────────────┐
│ Config (09) │◄───│                  │───►│  Notifications  │
│ [Params]    │ ❌ │                  │ ❌ │  (11)           │
└─────────────┘    └──────┬──────┬────┘    │  [Alerts]       │
                          │      │         └─────────────────┘
                   ❌     │      │  ❌
                   ┌──────▼──┐ ┌─▼────────────┐
                   │  OI     │ │ Audit &       │
                   │  (06)   │ │ Compliance    │
                   │[Metrics]│ │ (10)          │
                   └─────────┘ └──────────────┘

Legend: ✅ = implemented, ❌ = not implemented, 🔴 = blocking
```

---

## 12. Resumo de prioridades de integração

| # | Integração | Prioridade | Bloqueador |
|---|-----------|-----------|-----------|
| 1 | **Extrair de Governance** | 🔴 P0_BLOCKER | SIM — tudo depende disto |
| 2 | **Publicar domain events** | 🔴 P0_BLOCKER | SIM — Notifications, Audit, OI dependem |
| 3 | **Audit event forwarding** | 🔴 P1_CRITICAL | Segurança — acções sem auditoria |
| 4 | **Notification event forwarding** | 🔴 P1_CRITICAL | Operacional — falhas sem alerta |
| 5 | **Environment reference** | 🟡 P2_HIGH | Correctness — string livre |
| 6 | **Configuration params** | 🟢 P3_MEDIUM | Quality — thresholds hardcoded |
| 7 | **Change Governance integration** | 🟢 P3_MEDIUM | Traceability |
| 8 | **OI metrics forwarding** | 🟢 P3_MEDIUM | Observability |
