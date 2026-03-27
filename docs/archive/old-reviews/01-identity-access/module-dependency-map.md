# PARTE 11 — Mapa de Dependências com Outros Módulos

> Documento gerado em 2026-03-25 | Prompt N14 | Consolidação do módulo Identity & Access

---

## 1. Visão geral

O módulo Identity & Access é **fundacional** — não depende de outros módulos mas todos os outros 12 módulos dependem dele.

```
                    ┌─────────────────────────┐
                    │   Identity & Access      │
                    │   (Módulo Fundacional)   │
                    └────────────┬────────────┘
                                 │
         ┌───────────────────────┼───────────────────────┐
         │           │           │           │           │
    ┌────▼────┐ ┌────▼────┐ ┌────▼────┐ ┌────▼────┐ ┌────▼────┐
    │Catalog  │ │Contracts│ │Change   │ │Ops Intel│ │AI &     │
    │  (03)   │ │  (04)   │ │Gov (05) │ │  (06)   │ │Know (07)│
    └─────────┘ └─────────┘ └─────────┘ └─────────┘ └─────────┘
         │           │           │           │           │
    ┌────▼────┐ ┌────▼────┐ ┌────▼────┐ ┌────▼────┐ ┌────▼────┐
    │Governan.│ │Config   │ │Audit    │ │Notific. │ │Integrat.│
    │  (08)   │ │  (09)   │ │  (10)   │ │  (11)   │ │  (12)   │
    └─────────┘ └─────────┘ └─────────┘ └─────────┘ └─────────┘
                                                          │
                                                    ┌─────▼─────┐
                                                    │Prod Analyt│
                                                    │   (13)    │
                                                    └───────────┘
    
    ⚠️ Acoplamento especial:
    ┌─────────────────────────┐
    │ Environment Mgmt (02)   │ ← Entities embedded in Identity DbContext
    └─────────────────────────┘
```

---

## 2. Dependência com Environment Management (02)

### Estado actual: ACOPLAMENTO FORTE

| Aspecto | Detalhe |
|---|---|
| Entidades de Environment | 5 entities no domain de Identity |
| DbContext | IdentityDbContext hospeda Environment + EnvironmentAccess |
| Endpoints | 6 endpoints de ambiente em IdentityAccess.API |
| Frontend | EnvironmentsPage.tsx dentro de features/identity-access/ |
| Enums | EnvironmentCriticality + EnvironmentProfile no domain de Identity |

### Estado target: DESACOPLAMENTO

| Aspecto | Plano |
|---|---|
| Entidades | Migrar para src/modules/environmentmanagement/ |
| DbContext | Criar EnvironmentDbContext com prefixo env_ |
| Endpoints | Mover para módulo 02 com rota /api/v1/environments |
| Frontend | Mover para features/environment-management/ |
| Dependência residual | Identity consome EnvironmentId como dimensão de autorização |

### Interface de dependência (pós-extracção)

| Identity consome do módulo 02 | Mecanismo |
|---|---|
| EnvironmentId (para authorization scoping) | Via JWT claims ou header |
| Environment profile/criticality (para policy decisions) | Via integration event ou query |

| Módulo 02 consome de Identity | Mecanismo |
|---|---|
| UserId, TenantId | Via JWT claims |
| Permissões env:* | Via authorization middleware |
| EnvironmentAccess grants | Via shared table ou integration event |

---

## 3. Dependência com Governance (08)

| Identity → Governance | Mecanismo |
|---|---|
| Não consome directamente | — |

| Governance → Identity | Mecanismo |
|---|---|
| Contexto de utilizador e tenant | JWT claims |
| Permissões governance:* | RolePermissionCatalog |
| Enforcement RBAC | Authorization middleware |

---

## 4. Dependência com Audit & Compliance (10)

| Identity → Audit | Mecanismo |
|---|---|
| Publica SecurityEvents | Integration events via outbox (SecurityAuditBridge) |
| SecurityEventType catalog | Define tipos de eventos de segurança |

| Audit → Identity | Mecanismo |
|---|---|
| Contexto de utilizador/tenant | JWT claims |
| Permissões audit:* | RolePermissionCatalog |
| Quem fez o quê | UserId em audit trail |

### Lacuna
- Audit module deveria consumir **todos** os SecurityEvents de Identity
- Actualmente, Identity publica via SecurityAuditBridge mas não está confirmado que Audit os processa

---

## 5. Dependência com AI & Knowledge (07)

| Identity → AI | Mecanismo |
|---|---|
| Não consome directamente | — |

| AI → Identity | Mecanismo |
|---|---|
| Contexto de utilizador/tenant | JWT claims |
| Permissões ai:* (6 permissões) | RolePermissionCatalog |
| Enforcement RBAC | Authorization middleware |
| UserId para histórico de chat | Via execution context |

### Lacunas de capabilities AI
| Lacuna | Acção |
|---|---|
| Sem permissão granular por agent | Adicionar `ai:agents:{agentId}:execute` |
| Sem permissão para knowledge management | Adicionar `ai:knowledge:read/write` |
| Sem permissão para ver histórico de AI | Adicionar `ai:history:read` |

---

## 6. Dependência com Configuration (09)

| Identity → Configuration | Mecanismo |
|---|---|
| Feature flags (e.g., Auth:CookieSession:Enabled) | Via configuration system |
| JWT signing key config | Via appsettings / configuration |

| Configuration → Identity | Mecanismo |
|---|---|
| Contexto de utilizador/tenant | JWT claims |
| Permissões platform:* | RolePermissionCatalog |

---

## 7. Dependência com Catalog (03) e Contracts (04)

| Catalog/Contracts → Identity | Mecanismo |
|---|---|
| Contexto de utilizador/tenant | JWT claims |
| Permissões catalog:*, contracts:* | RolePermissionCatalog |
| RBAC enforcement | Authorization middleware |

---

## 8. Dependência com Change Governance (05)

| Change Governance → Identity | Mecanismo |
|---|---|
| Contexto de utilizador/tenant | JWT claims |
| Permissões change-intelligence:*, promotion:* | RolePermissionCatalog |
| UserId para change ownership | Via execution context |

---

## 9. Dependência com Operational Intelligence (06)

| Ops Intelligence → Identity | Mecanismo |
|---|---|
| Contexto de utilizador/tenant | JWT claims |
| Permissões operations:* | RolePermissionCatalog |
| UserId para incident assignment | Via execution context |

---

## 10. Dependência com Notifications (11)

| Identity → Notifications | Mecanismo |
|---|---|
| Não consome directamente | — |

| Notifications → Identity | Mecanismo |
|---|---|
| SecurityEvents publicados por Identity | Integration events via outbox |
| UserId para delivery targeting | Via event payload |

### Potencial
- Break Glass, JIT Access, Delegation events podem trigger notifications
- Actualmente: SecurityAuditBridge publica events, Notifications deveria subscrever

---

## 11. Resumo: O que Identity expõe

| Capacidade exposta | Mecanismo | Consumidores |
|---|---|---|
| JWT com userId, tenantId, permissions[] | Token | Todos os módulos |
| IOperationalExecutionContext | DI interface | Todos os módulos |
| RLS tenant isolation | NexTraceDbContextBase | Todos os DbContexts |
| SecurityEvents | Integration events (outbox) | Audit, Notifications |
| RolePermissionCatalog | Static catalog | Frontend permission check |
| Permission seed data | PermissionConfiguration | Database seed |

---

## 12. Resumo: O que Identity consome

| Capacidade consumida | Fonte | Mecanismo |
|---|---|---|
| Feature flags | Configuration (09) | App settings |
| EnvironmentId (futuro) | Environment Management (02) | JWT claims / header |
| Nenhuma dependência directa | — | Identity é autónomo |
