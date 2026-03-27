# Relatório de Resíduos Fora do Escopo — NexTraceOne

> Prompt N16 — Parte 5 | Data: 2026-03-25 | Fase: Encerramento da Trilha N

---

## 1. Resumo

Este relatório identifica código, documentos, referências e conceitos que já não fazem parte do escopo oficial do NexTraceOne e que devem ser removidos, arquivados ou reescritos.

**Total de resíduos identificados: 7**
- 🔴 REMOVE: 4
- 🟠 ARCHIVE: 1
- 🟡 REWRITE: 2
- ⚪ KEEP_FOR_HISTORY: 0

---

## 2. Resíduos de Módulos Removidos

### RES-01 — Licensing Permissions em RolePermissionCatalog

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/RolePermissionCatalog.cs` |
| **Descrição** | 17 permissões de Licensing ainda definidas no catálogo de permissões: `licensing:read`, `licensing:write`, `licensing:vendor:license:create/revoke/rehost/read/manage`, `licensing:vendor:key:generate`, `licensing:vendor:trial:extend`, `licensing:vendor:activation:issue`, `licensing:vendor:tenant:manage`, `licensing:vendor:telemetry:view`, `licensing:vendor:plan:create/read`, `licensing:vendor:featurepack:create/read` |
| **Impacto** | Poluem o catálogo de permissões; Admin role atribui 16 permissões que não servem nenhum endpoint real |
| **Classificação** | 🔴 **REMOVE** |
| **Referência** | OI-05 em `phase-a-open-items.md` |

### RES-02 — Licensing Permissions no Seed de EF (HasData)

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Persistence/Configurations/PermissionConfiguration.cs` (linhas 73-74) |
| **Descrição** | 2 permissões seedadas via HasData(): `licensing:read` (GUID `2E91A557-FADE-46DF-B248-0F5F5899C080`) e `licensing:write` (GUID `2E91A557-FADE-46DF-B248-0F5F5899C081`) com módulo "Licensing" |
| **Impacto** | Seeds criam registos de permissões para funcionalidade inexistente |
| **Classificação** | 🔴 **REMOVE** — será removido quando migrations forem recriadas |

### RES-03 — Referência a Licensing em Breadcrumbs

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/frontend/src/components/Breadcrumbs.tsx` (linha 34) |
| **Descrição** | Mapeamento de route segment `'licensing'` → i18n key `'sidebar.licensing'` |
| **Impacto** | Referência órfã — nenhuma rota `/licensing` existe |
| **Classificação** | 🔴 **REMOVE** |

### RES-04 — Referência a Vendor Licensing em Navigation Utils

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/frontend/src/utils/navigation.ts` (linha 48) |
| **Descrição** | Mapeamento de route segment `'vendor'` → i18n key `'sidebar.vendorLicensing'` |
| **Impacto** | Referência órfã — nenhuma rota `/vendor` existe |
| **Classificação** | 🔴 **REMOVE** |

---

## 3. Resíduos de Conceitos Antigos

### RES-05 — Referência a MFA no MfaPolicy com Licensing

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/ValueObjects/MfaPolicy.cs` (linha 58) |
| **Descrição** | Comentário XML: `"Indica se MFA step-up é exigido para operações de vendor/licensing"` |
| **Impacto** | Documentação inline referencia conceito fora do escopo |
| **Classificação** | 🟡 **REWRITE** — manter MFA step-up concept mas remover referência a licensing |

### RES-06 — Licensing em Admin Guidance (i18n)

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/frontend/src/locales/en.json` (linha ~3353) |
| **Descrição** | `"guidanceAdmin": "Administration covers user management, access controls, licensing, and audit trails."` |
| **Impacto** | Texto de orientação menciona licensing como capacidade do produto |
| **Classificação** | 🟡 **REWRITE** — remover "licensing" da descrição de admin |

---

## 4. Resíduos de Delegação com Licensing

### RES-07 — CreateDelegation com Permissão Licensing

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/CreateDelegation/CreateDelegation.cs` (linha 64) |
| **Descrição** | Permissão `"licensing:write"` incluída nas permissões delegáveis |
| **Impacto** | Permite delegar permissão para funcionalidade inexistente |
| **Classificação** | 🔴 **REMOVE** — junto com limpeza geral de licensing permissions |

---

## 5. Validação de Módulos Removidos/Consolidados

Segundo o `modular-review-master.md`, 4 pastas de módulos obsoletos/duplicados foram removidas durante a trilha N:

| Pasta Removida | Destino | Estado |
|---|---|---|
| Licensing (módulo) | Fora do escopo | ✅ Pasta removida. **Resíduos em permissões permanecem** |
| Service Catalog (duplicado) | Consolidado em Catalog | ✅ Limpo |
| Contracts (duplicado) | Consolidado como subdomínio de Catalog | ✅ Limpo |
| Changes (duplicado) | Consolidado em Change Governance | ✅ Limpo |

**Verificação:** Nenhuma pasta de módulo obsoleto encontrada em `src/modules/`. Apenas resíduos em permissões, seeds e referências de navegação.

---

## 6. Referências Obsoletas em Menu, Rotas e Permissões

| Tipo | Resíduo | Ficheiro | Estado |
|---|---|---|---|
| Rota | `/licensing` | Nenhuma rota definida | ✅ Limpo (rota não existe) |
| Rota | `/vendor` | Nenhuma rota definida | ✅ Limpo (rota não existe) |
| Menu | Licensing sidebar | `AppSidebar.tsx` | ✅ Limpo (sem entrada no sidebar) |
| Breadcrumb | `'licensing'` segment | `Breadcrumbs.tsx` | ❌ Resíduo — RES-03 |
| Breadcrumb | `'vendor'` segment | `navigation.ts` | ❌ Resíduo — RES-04 |
| Permissões | 17 licensing permissions | `RolePermissionCatalog.cs` | ❌ Resíduo — RES-01 |
| Seeds | 2 licensing permissions | `PermissionConfiguration.cs` | ❌ Resíduo — RES-02 |
| i18n | `sidebar.licensing`, `sidebar.vendorLicensing` | Locale files | ✅ Limpo (chaves não existem) |

---

## 7. Resumo por Camada

| Camada | Resíduos | Impacto |
|---|---|---|
| Domain (permissões) | RES-01, RES-05 | 🔴 Alto — 17 permissões inválidas no catálogo |
| Infrastructure (seeds) | RES-02 | 🟠 Médio — seeds de permissões fantasma |
| Application (features) | RES-07 | 🟠 Médio — delegação de permissão inexistente |
| Frontend (navigation) | RES-03, RES-04 | 🟡 Baixo — breadcrumbs órfãos |
| Frontend (i18n) | RES-06 | 🟡 Baixo — texto de guidance |

---

## 8. Backlog de Ações

| ID | Ação | Prioridade | Estimativa |
|---|---|---|---|
| RES-01 | Remover 17 licensing permissions de RolePermissionCatalog.cs | P1_CRITICAL | 2h |
| RES-02 | Remover licensing permissions de PermissionConfiguration HasData (será feito na recriação de migrations) | P2_HIGH | 1h |
| RES-03 | Remover mapeamento `'licensing'` de Breadcrumbs.tsx | P2_HIGH | 0.5h |
| RES-04 | Remover mapeamento `'vendor'` de navigation.ts | P2_HIGH | 0.5h |
| RES-05 | Reescrever comentário de MfaPolicy sem referência a licensing | P3_MEDIUM | 0.5h |
| RES-06 | Reescrever guidanceAdmin sem "licensing" | P3_MEDIUM | 0.5h |
| RES-07 | Remover `licensing:write` das permissões delegáveis | P2_HIGH | 0.5h |

**Total estimado: ~5.5h**
