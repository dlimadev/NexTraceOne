# PARTE 13 — Plano Final de Remediação do Módulo Identity & Access

> Documento gerado em 2026-03-25 | Prompt N14 | Consolidação do módulo Identity & Access
>
> Maturidade actual: **82%** | Target: **95%**

---

## A. Quick Wins (esforço ≤ 4h cada)

| ID | Item | Prioridade | Esforço | Ficheiro(s) |
|---|---|---|---|---|
| QW-01 | Remover 17 licensing permissions do RolePermissionCatalog.cs | 🟠 P1 | 30 min | `Domain/Entities/RolePermissionCatalog.cs` |
| QW-02 | Remover 2 licensing seed entries de PermissionConfiguration.cs | 🟠 P1 | 15 min | `Infrastructure/Persistence/Configurations/PermissionConfiguration.cs` |
| QW-03 | Remover `licensing:write` de NonDelegablePermissions em CreateDelegation.cs | 🟠 P1 | 5 min | `Application/Features/CreateDelegation/CreateDelegation.cs` |
| QW-04 | Reescrever comentário licensing em MfaPolicy.cs | 🟡 P2 | 5 min | `Domain/ValueObjects/MfaPolicy.cs` |
| QW-05 | Remover licensing breadcrumbs de Breadcrumbs.tsx | 🟡 P2 | 15 min | `frontend/src/components/Breadcrumbs.tsx` |
| QW-06 | Remover licenseId i18n keys de en.json (+3 traduções) | 🟡 P2 | 30 min | `frontend/src/locales/en.json`, pt-PT, pt-BR, es |
| QW-07 | Reescrever guidanceAdmin i18n sem "licensing" | 🟡 P2 | 15 min | `frontend/src/locales/en.json` |
| QW-08 | Adicionar Environments ao sidebar (AppSidebar.tsx) | 🟠 P1 | 30 min | `frontend/src/components/shell/AppSidebar.tsx` |
| QW-09 | Adicionar i18n key `sidebar.environments` | 🟡 P2 | 15 min | Ficheiros de locales |
| QW-10 | Adicionar audit event para role assignment em AssignRole.cs | 🟠 P1 | 2h | `Application/Features/AssignRole/AssignRole.cs` |
| QW-11 | Adicionar audit event para user creation em CreateUser.cs | 🟠 P1 | 2h | `Application/Features/CreateUser/CreateUser.cs` |
| QW-12 | Adicionar audit event para user deactivation/activation | 🟠 P1 | 2h | `Features/DeactivateUser/`, `Features/ActivateUser/` |
| QW-13 | Adicionar audit event para Access Review decisions | 🟡 P2 | 1h | `Features/DecideAccessReviewItem/` |

**Esforço total Quick Wins: ~10h**

---

## B. Correcções Funcionais Obrigatórias

| ID | Item | Prioridade | Esforço | Dependências |
|---|---|---|---|---|
| CF-01 | Implementar MFA verification handler (ValidateMfa command) | 🔴 P0 | 2-3 semanas | Campos MFA em User entity |
| CF-02 | Enforce MFA no login flow (LocalLogin + OIDC) | 🔴 P0 | 1 semana | CF-01 |
| CF-03 | Adicionar campos MfaEnabled, MfaMethod, MfaSecret a User entity | 🔴 P0 | 2h | — |
| CF-04 | Criar entidade ApiKey (Id, UserId, TenantId, KeyHash, Name, CreatedAt, ExpiresAt, RevokedAt) | 🟠 P1 | 4h | — |
| CF-05 | Criar CRUD endpoints para ApiKey (create, list, revoke, rotate) | 🟠 P1 | 1 semana | CF-04 |
| CF-06 | Implementar API Key authentication middleware | 🟠 P1 | 3 dias | CF-04, CF-05 |
| CF-07 | Implementar background job para expiração de JIT/BreakGlass/Delegation | 🟠 P1 | 3 dias | — |
| CF-08 | Granularizar permissões: Break Glass request → `identity:break-glass:request` | 🟠 P1 | 2h | — |
| CF-09 | Granularizar permissões: JIT Access request → `identity:jit-access:request` | 🟠 P1 | 2h | — |
| CF-10 | Granularizar permissões: Delegation create → `identity:delegations:create` | 🟠 P1 | 2h | — |
| CF-11 | Corrigir permissão DecideJitAccess: `identity:sessions:revoke` → `identity:jit-access:decide` | 🟠 P1 | 1h | — |
| CF-12 | Verificar/implementar ForgotPassword handler | 🟡 P2 | 3 dias | — |
| CF-13 | Verificar/implementar ResetPassword handler | 🟡 P2 | 3 dias | CF-12 |
| CF-14 | Verificar/implementar Activation handler | 🟡 P2 | 2 dias | — |
| CF-15 | Verificar/implementar Invitation acceptance handler | 🟡 P2 | 2 dias | — |
| CF-16 | Adicionar password complexity policy | 🟡 P2 | 4h | — |
| CF-17 | Implementar rate limiting em endpoints de autenticação | 🟡 P2 | 2 dias | — |
| CF-18 | Validar IP/UserAgent consistency em token refresh | 🟡 P2 | 4h | — |

**Esforço total Correcções Funcionais: ~8 semanas**

---

## C. Ajustes Estruturais

| ID | Item | Prioridade | Esforço | Dependências |
|---|---|---|---|---|
| AE-01 | Preparar renomeação de prefixo `identity_` → `iam_` em todas as EF configs | 🟠 P1 | 1 dia | Migration reset plan |
| AE-02 | Adicionar `UseXminAsConcurrencyToken()` a todas as configs de entidades mutáveis | 🟠 P1 | 2 dias | — |
| AE-03 | Preparar extracção de Environment entities para módulo 02 | 🟠 P1 | 2 semanas | Coordenação com N4 plan |
| AE-04 | Criar EnvironmentDbContext stub para migração gradual | 🟡 P2 | 1 semana | AE-03 |
| AE-05 | Definir interface de dependência Identity ← Environment (EnvironmentId claims) | 🟡 P2 | 3 dias | AE-03 |
| AE-06 | Migrar EnvironmentsPage.tsx para features/environment-management/ | 🟡 P2 | 2h | AE-03 |
| AE-07 | Implementar token blacklist ou session-bound validation | 🟡 P3 | 1 semana | — |
| AE-08 | Adicionar permissões granulares para AI capabilities | 🟡 P3 | 4h | Coordenação com N13 |
| AE-09 | Sistematizar environment-aware authorization em endpoints | 🟡 P2 | 1 semana | AE-03 |

**Esforço total Ajustes Estruturais: ~6 semanas**

---

## D. Pré-condições para Recriar Migrations

| # | Pré-condição | Estado | Blocker? |
|---|---|---|---|
| D-01 | Modelo de domínio finalizado (domain-model-finalization.md) | ✅ Feito (N14) | — |
| D-02 | Persistência com prefixo iam_ desenhada (persistence-model-finalization.md) | ✅ Feito (N14) | — |
| D-03 | Licensing permissions removidas do catálogo | ❌ Pendente (QW-01..03) | SIM |
| D-04 | ApiKey entity definida e mapeada | ❌ Pendente (CF-04) | SIM |
| D-05 | Campos MFA adicionados a User entity | ❌ Pendente (CF-03) | SIM |
| D-06 | RowVersion/xmin em entidades mutáveis | ❌ Pendente (AE-02) | SIM |
| D-07 | Environment entities migradas para módulo 02 | ❌ Pendente (AE-03) | SIM |
| D-08 | Seed data de permissions actualizado (sem licensing) | ❌ Pendente (QW-02) | SIM |
| D-09 | Outbox table com prefixo iam_ | ❌ Pendente (AE-01) | SIM |

**Nenhuma migration será criada até D-01..D-09 estarem resolvidos.**

---

## E. Critérios de Aceite do Módulo

O módulo Identity & Access será considerado **fechado para produção** quando:

| # | Critério | Estado actual |
|---|---|---|
| CA-01 | MFA enforcement implementado e funcional | ❌ |
| CA-02 | API Key management (CRUD + middleware) | ❌ |
| CA-03 | Todos os 73+ endpoints com permissão granular correcta | ⚠️ 90% |
| CA-04 | Licensing residues removidos | ❌ |
| CA-05 | Audit events para todas as acções sensíveis | ⚠️ 60% |
| CA-06 | Background job de expiração (JIT/BreakGlass/Delegation) | ❌ |
| CA-07 | RowVersion em entidades mutáveis | ❌ |
| CA-08 | Environment entities migradas para módulo 02 | ❌ |
| CA-09 | Prefixo iam_ aplicado em todas as tabelas | ❌ |
| CA-10 | Forgot/Reset password funcional | ⚠️ |
| CA-11 | Rate limiting em endpoints de autenticação | ❌ |
| CA-12 | Documentação mínima Tier 1 | ❌ |
| CA-13 | EnvironmentsPage visível no sidebar | ❌ |

---

## F. Cronograma sugerido

| Sprint | Itens | Esforço |
|---|---|---|
| Sprint 1 | QW-01..QW-13 (Quick Wins) | ~10h |
| Sprint 2 | CF-03 (MFA fields), CF-04 (ApiKey entity), CF-08..CF-11 (permissions) | ~12h |
| Sprint 3 | CF-01 + CF-02 (MFA enforcement) | ~3 semanas |
| Sprint 4 | CF-05 + CF-06 (API Key endpoints + middleware) | ~2 semanas |
| Sprint 5 | CF-07 (background jobs), AE-01 + AE-02 (iam_ prefix + xmin) | ~1 semana |
| Sprint 6 | CF-12..CF-15 (password reset, activation, invitation) | ~2 semanas |
| Sprint 7 | AE-03..AE-06 (Environment extraction) | ~3 semanas |
| Sprint 8 | CF-16..CF-18 (hardening), AE-07..AE-09, documentação Tier 1 | ~3 semanas |

**Esforço total estimado: ~240h (~16 semanas / 4 meses)**
**Maturidade target: 95%**

---

## G. Resumo de contagens

| Categoria | Itens | Esforço |
|---|---|---|
| Quick Wins (A) | 13 | ~10h |
| Correcções Funcionais (B) | 18 | ~8 semanas |
| Ajustes Estruturais (C) | 9 | ~6 semanas |
| Pré-condições migrations (D) | 9 blockers | — |
| Critérios de aceite (E) | 13 | — |
| **Total** | **55 itens** | **~240h** |

---

## H. Conflitos entre relatório consolidado e código real

| # | Conflito | Relatório diz | Código mostra | Resolução |
|---|---|---|---|---|
| C-01 | EnsureCreated | Relatórios antigos mencionam EnsureCreated | Não encontrado no código actual | ✅ Resolvido — não existe |
| C-02 | Maturidade 82% | Consolidado atribui 82% | Código confirma — robusto mas com lacunas de MFA e API Key | ✅ Correcto |
| C-03 | API Keys | Boundary matrix menciona API Key auth | Não existe entidade nem endpoint | ❌ Ausente — adicionar |
| C-04 | MFA | MfaPolicy VO existe | MFA não é enforced no login flow | ❌ Gap confirmado |
| C-05 | Environment coupling | OI-04 documenta | Código confirma 5 entities + 2 enums + 6 endpoints em Identity | ✅ Correcto — migração necessária |
