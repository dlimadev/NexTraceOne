# PARTE 12 — Documentação e Onboarding do Módulo Identity & Access

> Documento gerado em 2026-03-25 | Prompt N14 | Consolidação do módulo Identity & Access

---

## 1. Revisão de module-review.md

**Ficheiro:** `docs/11-review-modular/01-identity-access/module-review.md` (289 linhas)

| Aspecto | Estado | Notas |
|---|---|---|
| Inventário de frontend | ✅ | 15 páginas listadas |
| Inventário de backend | ✅ | 11 módulos/groups listados |
| Inventário de database | ✅ | 16 DbSets documentados |
| Inventário de autorização | ✅ | 73+ permissões mapeadas |
| Inventário de i18n | ✅ | Chaves documentadas |
| Inventário de audit | ✅ | SecurityEvent types listados |
| **Actualidade** | ⚠️ | Pode não reflectir últimas adições (Access Review, etc.) |
| **Lacunas identificadas** | ⚠️ | Não lista MFA como não-enforced |

**Recomendação:** Actualizar com informação do N14 consolidation.

---

## 2. Revisão de module-consolidated-review.md

**Ficheiro:** `docs/11-review-modular/01-identity-access/module-consolidated-review.md` (23.2 KB)

| Aspecto | Estado | Notas |
|---|---|---|
| Avaliação de maturidade (82%) | ✅ | Correcto |
| Problemas críticos identificados | ✅ | MFA, API keys mencionados |
| Layer-by-layer analysis | ✅ | Backend, frontend, database cobertos |
| Security gaps | ✅ | Documentados |
| Recomendações | ✅ | Presentes |
| **Profundidade** | 🟢 | Documento mais detalhado da pasta |
| **Actualidade** | ⚠️ | Verificar se inclui Access Review, JIT, Break Glass |

**Recomendação:** Manter como referência; adicionar link para N14 outputs.

---

## 3. Estrutura multicamada da pasta

| Subpasta | Ficheiros | Estado |
|---|---|---|
| `backend/` | endpoints.md, application-services.md, domain-rules.md, authorization-rules.md, validation-rules.md | ✅ 5 ficheiros |
| `database/` | schema-review.md, migrations-review.md, seed-data-review.md | ✅ 3 ficheiros |
| `frontend/` | (verificar conteúdo) | ⚠️ |
| `documentation/` | code-comments-review.md, developer-onboarding-notes.md | ✅ 2 ficheiros |
| `quality/` | technical-debt.md, bugs-and-gaps.md, test-scenarios.md, acceptance-checklist.md | ✅ 4 ficheiros |
| `ai/` | ai-capabilities.md, agents-review.md | ✅ 2 ficheiros |

**Total: ~20 ficheiros pre-existentes + 13 novos do N14**

---

## 4. Documentação ausente

| Documento | Importância | Descrição |
|---|---|---|
| README.md do módulo backend | 🔴 Alta | `src/modules/identityaccess/README.md` — não existe ou é template |
| Diagrama de fluxo de autenticação | 🟠 Alta | Fluxo visual de login → token → session |
| Diagrama de modelo de domínio | 🟠 Alta | Relações entre entities visualizadas |
| Guia de permissões para developers | 🟠 Alta | Como adicionar novas permissões |
| Guia de integração para outros módulos | 🟠 Alta | Como consumir Identity context |
| Glossário de termos | 🟡 Média | Break Glass, JIT, Delegation, MFA, RLS, etc. |
| Changelog do módulo | 🟡 Média | Evolução histórica |
| Security model documentation | 🔴 Alta | Documentação do modelo de segurança |

---

## 5. Classes e fluxos que precisam de explicação

### Classes complexas

| Classe | LOC | Razão |
|---|---|---|
| `OidcCallback.cs` | 275 | Fluxo mais complexo — OIDC token exchange + user linking |
| `RolePermissionCatalog.cs` | 261 | Catálogo central — precisa de documentação de como estender |
| `SecurityEventType.cs` | 146 | Catálogo de tipos de eventos — precisa de guia de uso |
| `SecurityAuditRecorder.cs` | 141 | Como auditar novas acções |
| `OidcProviderService.cs` | 206 | Detalhes de integração OIDC |
| `CreateDelegation.cs` | 139 | Regras de negócio complexas (NonDelegablePermissions) |
| `NexTraceDbContextBase` | — | RLS, tenant isolation, outbox — documentar para novos devs |

### Fluxos que precisam de diagrama

| Fluxo | Complexidade |
|---|---|
| Login local → session → JWT → refresh | Alta |
| OIDC redirect → callback → user link → session | Alta |
| Break Glass request → approval → activation → expiry | Média |
| JIT Access request → decision → activation → expiry | Média |
| Delegation creation → validation → activation → revocation | Média |
| Access Review campaign → items → decisions → completion | Média |
| Tenant selection → new JWT → context switch | Média |

---

## 6. XML docs necessárias

| Classe/Método | Tipo | Prioridade |
|---|---|---|
| `User.CreateLocal()` / `User.CreateFederated()` | Factory methods | 🟠 |
| `User.LinkFederatedIdentity()` | Domain method | 🟠 |
| `User.RecordFailedLogin()` / `ResetFailedAttempts()` | Lockout logic | 🟠 |
| `RolePermissionCatalog.GetPermissionsForRole()` | Static catalog | 🔴 |
| `SecurityEventType` constants | Event catalog | 🟠 |
| `MfaPolicy.ForSaaS()` etc. | Policy factories | 🟡 |
| `IOperationalExecutionContext` | Cross-module interface | 🔴 |
| `IEnvironmentAccessValidator` | Environment auth interface | 🟠 |
| `LoginSessionCreator` | Session creation service | 🟡 |
| `SecurityAuditBridge` | Event publication | 🟠 |

---

## 7. Notas de onboarding necessárias

### Para novos developers

1. **Arquitectura do módulo:** 5 projectos (.API, .Application, .Domain, .Infrastructure, .Contracts)
2. **Como adicionar um novo endpoint:** Exemplo passo-a-passo com handler + endpoint + permission
3. **Como adicionar uma nova permissão:** RolePermissionCatalog → PermissionConfiguration → Frontend permissions.ts
4. **Como usar o contexto de execução:** IOperationalExecutionContext injection
5. **Como auditar uma acção:** SecurityAuditRecorder pattern
6. **Como funciona RLS:** NexTraceDbContextBase → tenant_id → PostgreSQL policies
7. **Como funciona o OIDC flow:** StartOidcLogin → redirect → OidcCallback → session
8. **Feature flags:** Auth:CookieSession:Enabled e outros

### Para module maintainers

1. **Prefixo de tabelas:** Actualmente `identity_`, target `iam_`
2. **Migrations:** 2 existentes, não alterar, migration reset futuro
3. **Licensing cleanup:** 17 permissions a remover
4. **Environment extraction:** OI-04 tracking
5. **MFA gap:** P0 blocker para produção

---

## 8. Documentação mínima do módulo

### Tier 1 — Obrigatório (antes de produção)

| Documento | Localização | Esforço |
|---|---|---|
| README.md do módulo | `src/modules/identityaccess/README.md` | 4h |
| Security model doc | `docs/11-review-modular/01-identity-access/security-model.md` | 8h |
| Permission guide | `docs/11-review-modular/01-identity-access/permission-guide.md` | 4h |
| Auth flow diagrams | `docs/11-review-modular/01-identity-access/auth-flows.md` | 4h |

### Tier 2 — Recomendado

| Documento | Localização | Esforço |
|---|---|---|
| Integration guide | `docs/11-review-modular/01-identity-access/integration-guide.md` | 4h |
| Onboarding notes | Actualizar `documentation/developer-onboarding-notes.md` | 4h |
| Domain model diagram | `docs/11-review-modular/01-identity-access/domain-diagram.md` | 2h |
| Glossário | `docs/11-review-modular/01-identity-access/glossary.md` | 2h |

### Tier 3 — Nice to have

| Documento | Localização | Esforço |
|---|---|---|
| XML docs em todas as public APIs | Inline no código | 8h |
| Changelog do módulo | `docs/11-review-modular/01-identity-access/CHANGELOG.md` | 2h |
| Test coverage report | `quality/test-coverage.md` | 2h |

**Esforço total documentação: ~44h (20h Tier 1 + 12h Tier 2 + 12h Tier 3)**
