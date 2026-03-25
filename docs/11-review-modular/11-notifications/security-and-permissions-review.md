# Notifications — Revisão de Segurança e Permissões

> **Módulo:** 11 — Notifications  
> **Data:** 2026-03-25  
> **Fase:** N8-R — Reexecução completa  
> **Estado:** ✅ FINALIZADO

---

## 1. Permissões definidas nos endpoints

| Permissão | Endpoints | Tipo |
|-----------|----------|------|
| `notifications:inbox:read` | GET /notifications, GET /notifications/unread-count | Leitura |
| `notifications:inbox:write` | POST /{id}/read, POST /{id}/unread, POST /mark-all-read | Escrita |
| `notifications:preferences:read` | GET /preferences | Leitura |
| `notifications:preferences:write` | PUT /preferences | Escrita |

---

## 2. Permissões no RolePermissionCatalog

**Ficheiro:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/RolePermissionCatalog.cs`

| Role | Tem `notifications:*`? | Estado |
|------|----------------------|--------|
| PlatformAdmin | 🔴 NÃO | **BLOCKER** |
| TechLead | 🔴 NÃO | **BLOCKER** |
| Developer | 🔴 NÃO | **BLOCKER** |
| Viewer | 🔴 NÃO | **BLOCKER** |
| Auditor | 🔴 NÃO | **BLOCKER** |
| SecurityReview | 🔴 NÃO | **BLOCKER** |
| ApprovalOnly | 🔴 NÃO | **BLOCKER** |

### 🔴 BLOCKER CRÍTICO

**Nenhuma das 7 roles do sistema inclui permissões `notifications:*`.** Isto significa que:
1. Nenhum utilizador consegue aceder à central de notificações
2. Nenhum utilizador consegue ver preferências
3. O bell icon no header não retorna dados
4. As rotas frontend são bloqueadas pelo ProtectedRoute guard

**Acção obrigatória:** Adicionar `notifications:inbox:read`, `notifications:inbox:write`, `notifications:preferences:read`, `notifications:preferences:write` às roles apropriadas no `RolePermissionCatalog.cs`.

**Proposta de atribuição:**

| Role | inbox:read | inbox:write | preferences:read | preferences:write |
|------|-----------|------------|-----------------|------------------|
| PlatformAdmin | ✅ | ✅ | ✅ | ✅ |
| TechLead | ✅ | ✅ | ✅ | ✅ |
| Developer | ✅ | ✅ | ✅ | ✅ |
| Viewer | ✅ | ❌ | ✅ | ❌ |
| Auditor | ✅ | ❌ | ✅ | ❌ |
| SecurityReview | ✅ | ✅ | ✅ | ✅ |
| ApprovalOnly | ✅ | ✅ | ✅ | ✅ |

---

## 3. Permissões por página (frontend)

| Página | Rota | Guard | Permissão |
|--------|------|-------|-----------|
| NotificationCenterPage | `/notifications` | `ProtectedRoute` | `notifications:inbox:read` |
| NotificationPreferencesPage | `/notifications/preferences` | `ProtectedRoute` | `notifications:inbox:read` |
| NotificationConfigurationPage | `/platform/configuration/notifications` | `ProtectedRoute` | `platform:admin:read` |

---

## 4. Enforcement no backend

| Mecanismo | Estado | Detalhes |
|-----------|--------|---------|
| `RequirePermission()` nos endpoints | ✅ Real | Cada endpoint tem permissão explícita |
| `PermissionAuthorizationHandler` | ✅ Real | Via BuildingBlocks.Security |
| JWT token validation | ✅ Real | Via middleware de autenticação |
| TenantId isolation | ✅ Real | RLS via TenantRlsInterceptor na NexTraceDbContextBase |
| CurrentUser scoping | ✅ Real | Notificações filtradas por RecipientUserId |

---

## 5. Impacto do módulo em permissões por ambiente

| Aspecto | Estado | Detalhes |
|---------|--------|---------|
| EnvironmentId na notificação | ✅ Real | Campo opcional registado |
| Filtro por environment | ❌ Ausente | Sem filtro nos endpoints |
| Permissão por environment | ❌ Ausente | Notificações não filtradas por acesso ao environment |

**Nota:** Actualmente, um utilizador pode ver notificações de ambientes aos quais não tem acesso, desde que seja o RecipientUserId. Isto pode ser aceitável (notificação foi-lhe dirigida explicitamente) ou pode necessitar de revisão dependendo da política de segurança.

---

## 6. Acções sensíveis

| Acção | Sensibilidade | Auditoria | Detalhes |
|-------|-------------|----------|---------|
| Marcar todas como lidas | ⚠️ Média | Via audit interceptor | Pode mascarar notificações críticas |
| Desactivar preferência de canal | ⚠️ Média | Via audit interceptor | Pode silenciar alertas importantes |
| Mandatory notification override | 🔴 Alta | ✅ Bloqueado | MandatoryNotificationPolicy impede desactivação |
| Admin configuration | 🟠 Alta | Via audit interceptor | Configuração global de notificações |

---

## 7. Escopo por tenant

| Aspecto | Estado | Detalhes |
|---------|--------|---------|
| TenantId em Notification | ✅ Obrigatório | Campo required |
| TenantId em Preferences | ✅ Obrigatório | Campo required |
| RLS enforcement | ✅ Real | Via TenantRlsInterceptor |
| Cross-tenant isolation | ✅ Real | Notificações são tenant-scoped |

---

## 8. Auditoria de acções críticas

| Acção | Mecanismo de auditoria | Estado |
|-------|----------------------|--------|
| Notificação criada | NotificationCreatedEvent + Integration Event | ✅ Real |
| Notificação lida | NotificationReadEvent | ✅ Real |
| Delivery completada | NotificationDeliveryCompletedEvent | ✅ Real |
| Delivery falhada | NotificationDeliveryFailedIntegrationEvent | ✅ Real |
| Preferência alterada | AuditInterceptor (UpdatedAt/By) | ✅ Real |
| Configuração admin | AuditInterceptor | ⚠️ Parcial (sem endpoint dedicado) |

---

## 9. Backlog de segurança

| # | Correcção | Prioridade | Esforço |
|---|----------|-----------|---------|
| S-01 | 🔴 **Registar `notifications:*` no RolePermissionCatalog** | P0 | 2h |
| S-02 | 🔴 **Registar permissões na `PermissionConfiguration.cs`** | P0 | 1h |
| S-03 | 🟠 Avaliar filtro por EnvironmentAccess nas notificações | P2 | 4h |
| S-04 | 🟠 Adicionar rate limiting nos endpoints de write | P2 | 2h |
| S-05 | 🟡 Adicionar audit trail explícito para mark-all-read | P2 | 2h |
| S-06 | 🟡 Validar que delivery não expõe dados sensíveis em ErrorMessage | P2 | 2h |
| S-07 | 🟡 Adicionar permissão `notifications:admin:write` para config admin | P3 | 3h |

**Esforço total estimado:** ~16h
