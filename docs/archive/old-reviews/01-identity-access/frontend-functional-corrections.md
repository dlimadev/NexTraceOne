# PARTE 7 — Revisão Frontend Funcional do Módulo Identity & Access

> Documento gerado em 2026-03-25 | Prompt N14 | Consolidação do módulo Identity & Access

---

## 1. Inventário de páginas

| # | Página | Ficheiro | LOC | Tipo |
|---|---|---|---|---|
| P-01 | LoginPage | `pages/LoginPage.tsx` | 154 | Auth (eager) |
| P-02 | TenantSelectionPage | `pages/TenantSelectionPage.tsx` | 107 | Auth (eager) |
| P-03 | ForgotPasswordPage | `pages/ForgotPasswordPage.tsx` | 113 | Auth (eager) |
| P-04 | ResetPasswordPage | `pages/ResetPasswordPage.tsx` | 148 | Auth (eager) |
| P-05 | ActivationPage | `pages/ActivationPage.tsx` | 129 | Auth (eager) |
| P-06 | MfaPage | `pages/MfaPage.tsx` | 113 | Auth (eager) |
| P-07 | InvitationPage | `pages/InvitationPage.tsx` | 187 | Auth (eager) |
| P-08 | UsersPage | `pages/UsersPage.tsx` | 199 | Admin (lazy) |
| P-09 | EnvironmentsPage | `pages/EnvironmentsPage.tsx` | 433 | Admin (lazy) |
| P-10 | BreakGlassPage | `pages/BreakGlassPage.tsx` | 179 | Enterprise (lazy) |
| P-11 | JitAccessPage | `pages/JitAccessPage.tsx` | 218 | Enterprise (lazy) |
| P-12 | DelegationPage | `pages/DelegationPage.tsx` | 250 | Enterprise (lazy) |
| P-13 | AccessReviewPage | `pages/AccessReviewPage.tsx` | 308 | Compliance (lazy) |
| P-14 | MySessionsPage | `pages/MySessionsPage.tsx` | 180 | Security (lazy) |
| P-15 | UnauthorizedPage | `pages/UnauthorizedPage.tsx` | 28 | Error |

**Total: 15 páginas, 2746 LOC**

### Componentes partilhados
| Componente | Ficheiro | LOC |
|---|---|---|
| AuthCard | `components/AuthCard.tsx` | 21 |
| AuthDivider | `components/AuthDivider.tsx` | 23 |
| AuthFeedback | `components/AuthFeedback.tsx` | 54 |
| AuthShell | `components/AuthShell.tsx` | 103 |

---

## 2. Revisão de rotas

| Rota | Permissão | Componente | Estado |
|---|---|---|---|
| `/login` | Pública | LoginPage | ✅ |
| `/tenant-selection` | Pública | TenantSelectionPage | ✅ |
| `/forgot-password` | Pública | ForgotPasswordPage | ⚠️ Backend? |
| `/reset-password` | Pública | ResetPasswordPage | ⚠️ Backend? |
| `/activate` | Pública | ActivationPage | ⚠️ Backend? |
| `/mfa` | Pública | MfaPage | ⚠️ Backend? |
| `/invite` | Pública | InvitationPage | ⚠️ Backend? |
| `/users` | `identity:users:read` | UsersPage | ✅ |
| `/environments` | `identity:users:read` | EnvironmentsPage | ⚠️ Sem sidebar |
| `/break-glass` | `identity:sessions:read` | BreakGlassPage | ✅ |
| `/jit-access` | `identity:users:read` | JitAccessPage | ✅ |
| `/delegations` | `identity:users:read` | DelegationPage | ✅ |
| `/access-reviews` | `identity:users:read` | AccessReviewPage | ✅ |
| `/my-sessions` | `identity:sessions:read` | MySessionsPage | ✅ |
| `/unauthorized` | Pública | UnauthorizedPage | ✅ |

---

## 3. Revisão de menu (sidebar)

### Entradas presentes no `AppSidebar.tsx` (secção Admin)

| Menu Item | Rota | Permissão | i18n key |
|---|---|---|---|
| Users | `/users` | `identity:users:read` | `sidebar.users` |
| Break Glass | `/break-glass` | `identity:sessions:read` | `sidebar.breakGlass` |
| JIT Access | `/jit-access` | `identity:users:read` | `sidebar.jitAccess` |
| Delegations | `/delegations` | `identity:users:read` | `sidebar.delegations` |
| Access Review | `/access-reviews` | `identity:users:read` | `sidebar.accessReview` |
| My Sessions | `/my-sessions` | `identity:sessions:read` | `sidebar.mySessions` |

### ❌ Ausente do sidebar

| Item | Rota | Problema |
|---|---|---|
| **Environments** | `/environments` | Página existe (433 LOC) mas não está no sidebar — undiscoverable |

---

## 4. Revisão de integração com API real

| Página | API calls | Backend existe? | Estado |
|---|---|---|---|
| LoginPage | `identityApi.login()` | ✅ LocalLogin handler | ✅ |
| TenantSelectionPage | `identityApi.selectTenant()` | ✅ SelectTenant handler | ✅ |
| UsersPage | `listTenantUsers, createUser, assignRole, activate/deactivate` | ✅ | ✅ |
| BreakGlassPage | `requestBreakGlass, revokeBreakGlass, listBreakGlassRequests` | ✅ | ✅ |
| JitAccessPage | `requestJitAccess, decideJitAccess, listPendingJitRequests` | ✅ | ✅ |
| DelegationPage | `createDelegation, revokeDelegation, listDelegations` | ✅ | ✅ |
| AccessReviewPage | `startCampaign, listCampaigns, getCampaign, decideItem` | ✅ | ✅ |
| MySessionsPage | `listActiveSessions, revoke` | ✅ | ✅ |
| EnvironmentsPage | `listEnvironments, create, update, setPrimary` | ✅ | ✅ |
| **ForgotPasswordPage** | `identityApi.forgotPassword()?` | ❌ **Não confirmado** | ⚠️ |
| **ResetPasswordPage** | `identityApi.resetPassword()?` | ❌ **Não confirmado** | ⚠️ |
| **ActivationPage** | `identityApi.activate()?` | ❌ **Não confirmado** | ⚠️ |
| **MfaPage** | `identityApi.verifyMfa()?` | ❌ **Não confirmado** | ⚠️ |
| **InvitationPage** | `identityApi.acceptInvitation()?` | ❌ **Não confirmado** | ⚠️ |

---

## 5. Revisão de i18n

| Aspecto | Estado | Notas |
|---|---|---|
| Sidebar labels | ✅ | `sidebar.users`, `sidebar.breakGlass`, etc. |
| Page headers | ✅ | Via i18n keys |
| Form labels | ✅ | Dentro das páginas |
| Error messages | ⚠️ Parcial | Verificar se todas as respostas de erro são i18n |
| Breadcrumbs | ✅ | Mapeados em Breadcrumbs.tsx |
| **Licensing breadcrumbs** | ❌ Resíduo | `'licensing' → 'sidebar.licensing'` — sem página |
| **Vendor breadcrumbs** | ❌ Resíduo | `'vendor' → 'sidebar.vendorLicensing'` — sem página |
| Environment i18n | ✅ | `identity.environment.*` keys presentes |

---

## 6. Revisão de botões sem acção

| Página | Botão | Problema |
|---|---|---|
| ForgotPasswordPage | Submit | ⚠️ Pode não ter backend handler |
| ResetPasswordPage | Submit | ⚠️ Pode não ter backend handler |
| ActivationPage | Activate | ⚠️ Pode não ter backend handler |
| MfaPage | Verify | ⚠️ Pode não ter backend handler |

---

## 7. Revisão de placeholders

| Item | Estado |
|---|---|
| Login form | ✅ Campos com placeholders i18n |
| User creation | ✅ |
| Environment creation | ✅ Profiles e criticality com dropdowns |
| Delegation | ✅ Permission picker, date picker |
| **No content states (empty lists)** | ⚠️ Verificar se existe empty state UX |

---

## 8. Revisão de campos técnicos indevidos

| Página | Campo | Problema |
|---|---|---|
| MySessionsPage | User-Agent raw | ⚠️ Poderia ser parsed para browser + OS legível |
| UsersPage | User ID | ⚠️ Se exposto directamente ao utilizador |
| EnvironmentsPage | Slug | ✅ Aceitável para admin |

---

## 9. Backlog de correcções frontend

| ID | Correcção | Prioridade | Esforço |
|---|---|---|---|
| F-01 | Adicionar Environments ao sidebar em AppSidebar.tsx | 🟠 P1 | 30 min |
| F-02 | Validar se ForgotPasswordPage tem backend funcional ou marcar como WIP | 🟡 P2 | 2h |
| F-03 | Validar se ResetPasswordPage tem backend funcional ou marcar como WIP | 🟡 P2 | 2h |
| F-04 | Validar se ActivationPage tem backend funcional ou marcar como WIP | 🟡 P2 | 2h |
| F-05 | Validar se MfaPage tem backend funcional ou marcar como WIP | 🟡 P2 | 2h |
| F-06 | Validar se InvitationPage tem backend funcional ou marcar como WIP | 🟡 P2 | 2h |
| F-07 | Remover licensing breadcrumbs residuais de Breadcrumbs.tsx | 🟡 P2 | 30 min |
| F-08 | Remover vendor licensing breadcrumbs residuais | 🟡 P2 | 30 min |
| F-09 | Adicionar empty state UX para listas vazias | 🟡 P3 | 2h |
| F-10 | Parse User-Agent em MySessionsPage para display legível | 🟡 P3 | 1h |
| F-11 | Preparar migração de EnvironmentsPage para feature folder de módulo 02 | 🟡 P3 | 2h |
| F-12 | Adicionar i18n key `sidebar.environments` | 🟡 P2 | 30 min |
