# E7 — Notifications Module Execution Report

## Data de Execução
2026-03-25

## Resumo
Execução real de correções no módulo Notifications conforme a trilha N.
Adicionada concorrência otimista, check constraints, FK, e permissões registadas
no RolePermissionCatalog para todos os 7 roles.

---

## Ficheiros de Código Alterados

### Domain — Entidades
| Ficheiro | Alteração |
|----------|-----------|
| `Notification.cs` | Adicionado RowVersion (uint xmin). |
| `NotificationDelivery.cs` | Adicionado RowVersion (uint xmin). |

### Persistence — EF Core Configurations
| Ficheiro | Alteração |
|----------|-----------|
| `NotificationConfiguration.cs` | Adicionados 3 check constraints (Status, Category, Severity). Adicionado `IsRowVersion()`. |
| `NotificationDeliveryConfiguration.cs` | Adicionados 2 check constraints (Status, Channel). Adicionado FK → Notification (cascade). Adicionado `IsRowVersion()`. |
| `NotificationPreferenceConfiguration.cs` | Adicionados 2 check constraints (Category, Channel). |

### Security — Permissões
| Ficheiro | Alteração |
|----------|-----------|
| `RolePermissionCatalog.cs` | Registadas 4 permissões de notifications em 7 roles. |

### Documentação
| Ficheiro | Alteração |
|----------|-----------|
| `src/modules/notifications/README.md` | **CRIADO** — README completo com escopo, arquitetura, fluxo, DB, permissões, endpoints, handlers, testes. |

---

## Correções por Parte

### PART 1 — Fluxo Ponta a Ponta de Delivery
- ✅ Fluxo existente verificado: evento → template → orchestrator → routing → delivery → status
- ✅ FK Delivery→Notification adicionada para integridade referencial
- ✅ Check constraints adicionam guardrails nos estados do fluxo

### PART 2 — Templates, Canais, Retries, Status
- ✅ Check constraint em DeliveryStatus (Pending, Delivered, Failed, Skipped)
- ✅ Check constraint em DeliveryChannel (InApp, Email, MicrosoftTeams)
- ✅ RowVersion em NotificationDelivery para concorrência em retries

### PART 3 — Domínio
- ✅ RowVersion (uint) em Notification aggregate root
- ✅ RowVersion (uint) em NotificationDelivery entity

### PART 4 — Persistência
- ✅ ntf_ prefix já presente em todas as 3 tabelas (não necessitou alteração)
- ✅ 7 check constraints: Status/Category/Severity em Notification; Status/Channel em Delivery; Category/Channel em Preference
- ✅ FK Delivery→Notification com CASCADE
- ✅ `IsRowVersion()` xmin em 2 entidades

### PART 5 — Backend
- ✅ 7 endpoints verificados com permissões granulares corretas
- ✅ Fluxo CQRS completo (7 features)

### PART 6 — Frontend
- ✅ Frontend verificado com 3 pages, 3 hooks, 1 API client, 2 components
- ✅ Integração API correcta (React Query)

### PART 7 — Segurança
- ✅ Permissões `notifications:inbox:read/write` + `notifications:preferences:read/write` registadas
- ✅ PlatformAdmin: todas 4 permissões
- ✅ TechLead: todas 4 permissões
- ✅ Developer: todas 4 permissões
- ✅ Viewer: inbox:read + preferences:read (somente leitura)
- ✅ Auditor: inbox:read + preferences:read (somente leitura)
- ✅ SecurityReview: inbox:read + preferences:read (somente leitura)
- ✅ ApprovalOnly: inbox:read + inbox:write + preferences:read

### PART 8 — Dependências com Módulos Emissores
- ✅ 8 event handlers verificados (Approval, Catalog, Compliance, Budget, Incident, Integration, Security, AI)
- ✅ Documentados no README

### PART 9 — Documentação
- ✅ README.md criado com conteúdo completo

---

## Validação

- ✅ Build: 0 erros
- ✅ 412 testes Notifications: todos passam
- ✅ 290 testes Identity: todos passam (após alteração no RolePermissionCatalog)
- ✅ Sem migrations antigas removidas
- ✅ Sem nova baseline gerada

---

## Classes Alteradas

| Classe | Tipo de Alteração |
|--------|-------------------|
| `Notification` | RowVersion (uint xmin) |
| `NotificationDelivery` | RowVersion (uint xmin) |
| `NotificationConfiguration` | 3 check constraints, IsRowVersion() |
| `NotificationDeliveryConfiguration` | 2 check constraints, FK → Notification, IsRowVersion() |
| `NotificationPreferenceConfiguration` | 2 check constraints |
| `RolePermissionCatalog` | +4 notifications permissions × 7 roles (total +23 permission entries) |
