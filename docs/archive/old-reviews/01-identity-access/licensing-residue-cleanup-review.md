# PARTE 10 — Revisão de Resíduos de Licensing

> Documento gerado em 2026-03-25 | Prompt N14 | Consolidação do módulo Identity & Access

---

## 1. Contexto

O módulo Licensing foi **removido do escopo do NexTraceOne** (documentado em `modular-review-master.md` secção 5). No entanto, existem resíduos em vários pontos do código que referenciam funcionalidades de licensing. Estes resíduos devem ser mapeados, classificados e limpos.

---

## 2. Resíduos no backend

### 2.1 RolePermissionCatalog.cs

**Ficheiro:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/RolePermissionCatalog.cs`

| Permissão | Linha (aprox.) | Role | Acção |
|---|---|---|---|
| `licensing:read` | ~125 | PlatformAdmin | ❌ Remover |
| `licensing:write` | ~126 | PlatformAdmin | ❌ Remover |
| `licensing:vendor:license:create` | ~127 | PlatformAdmin | ❌ Remover |
| `licensing:vendor:license:revoke` | ~128 | PlatformAdmin | ❌ Remover |
| `licensing:vendor:license:rehost` | ~129 | PlatformAdmin | ❌ Remover |
| `licensing:vendor:license:read` | ~130 | PlatformAdmin | ❌ Remover |
| `licensing:vendor:key:generate` | ~131 | PlatformAdmin | ❌ Remover |
| `licensing:vendor:trial:extend` | ~132 | PlatformAdmin | ❌ Remover |
| `licensing:vendor:activation:issue` | ~133 | PlatformAdmin | ❌ Remover |
| `licensing:vendor:tenant:manage` | ~134 | PlatformAdmin | ❌ Remover |
| `licensing:vendor:telemetry:view` | ~135 | PlatformAdmin | ❌ Remover |
| `licensing:vendor:plan:create` | ~136 | PlatformAdmin | ❌ Remover |
| `licensing:vendor:plan:read` | ~137 | PlatformAdmin | ❌ Remover |
| `licensing:vendor:featurepack:create` | ~138 | PlatformAdmin | ❌ Remover |
| `licensing:vendor:featurepack:read` | ~139 | PlatformAdmin | ❌ Remover |
| `licensing:vendor:license:manage` | ~140 | PlatformAdmin | ❌ Remover |
| `licensing:read` | ~TechLead | TechLead | ❌ Remover |

**Total: 17 permissões de licensing a remover**

### 2.2 PermissionConfiguration.cs (seed data)

**Ficheiro:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Persistence/Configurations/PermissionConfiguration.cs`

| Entrada seed | GUID | Acção |
|---|---|---|
| `licensing:read` — "View license information" | `2E91A557-FADE-46DF-B248-0F5F5899C080` | ❌ Remover |
| `licensing:write` — "Manage licenses" | `2E91A557-FADE-46DF-B248-0F5F5899C081` | ❌ Remover |

### 2.3 MfaPolicy.cs (referência textual)

**Ficheiro:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/ValueObjects/MfaPolicy.cs`

| Referência | Linha (aprox.) | Contexto | Acção |
|---|---|---|---|
| "alterações de licenciamento" | ~10 | Comentário sobre operações que requerem MFA step-up | ✅ Reescrever para "operações administrativas" |

### 2.4 CreateDelegation.cs (NonDelegablePermissions)

**Ficheiro:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/CreateDelegation/CreateDelegation.cs`

| Referência | Linha (aprox.) | Contexto | Acção |
|---|---|---|---|
| `"licensing:write"` | ~64 | Lista de permissões não delegáveis | ❌ Remover da lista |

### 2.5 Migrations (InitialCreate)

**Ficheiro:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Persistence/Migrations/20260321160222_InitialCreate.cs`

| Referência | Contexto | Acção |
|---|---|---|
| Licensing permission seed GUIDs | Dados iniciais de permissões | ⚠️ Não alterar migration — limpar na migration reset futura |

---

## 3. Resíduos no frontend

### 3.1 Breadcrumbs.tsx

**Ficheiro:** `src/frontend/src/components/Breadcrumbs.tsx`

| Referência | Linha (aprox.) | Acção |
|---|---|---|
| `'licensing': 'sidebar.licensing'` | ~34 | ❌ Remover mapeamento |
| `'vendor': 'sidebar.vendorLicensing'` | ~48 | ❌ Remover mapeamento |

### 3.2 en.json (i18n)

**Ficheiro:** `src/frontend/src/locales/en.json`

| Chave | Valor | Acção |
|---|---|---|
| `"license"` | `"License"` | ⚠️ Manter — usado no Contracts (OpenAPI license field) |
| `"licensePlaceholder"` | `"MIT"` | ⚠️ Manter — usado no Contracts |
| `"licenseId"` | `"License ID"` | ❌ Remover — resíduo de licensing module |
| `"licenseIdPlaceholder"` | `"Enter license UUID"` | ❌ Remover — resíduo de licensing module |
| `"guidanceAdmin"` | Menciona "licensing" | ✅ Reescrever sem menção a licensing |

### 3.3 Outros ficheiros de i18n (pt-PT.json, pt-BR.json, es.json)

Verificar as mesmas chaves nos outros ficheiros de tradução e aplicar as mesmas acções.

---

## 4. Classificação dos resíduos

### 4.1 A remover imediatamente (Quick win)

| # | Ficheiro | Acção | Esforço |
|---|---|---|---|
| L-01 | RolePermissionCatalog.cs | Remover 17 licensing permissions | 30 min |
| L-02 | PermissionConfiguration.cs | Remover 2 licensing seed entries | 15 min |
| L-03 | CreateDelegation.cs | Remover `licensing:write` de NonDelegablePermissions | 5 min |
| L-04 | Breadcrumbs.tsx | Remover 2 licensing breadcrumb mappings | 5 min |
| L-05 | en.json | Remover `licenseId` e `licenseIdPlaceholder` | 5 min |
| L-06 | en.json | Reescrever `guidanceAdmin` sem "licensing" | 5 min |

### 4.2 A reescrever

| # | Ficheiro | Acção | Esforço |
|---|---|---|---|
| L-07 | MfaPolicy.cs | Reescrever comentário "alterações de licenciamento" | 5 min |

### 4.3 A manter sem impacto

| # | Ficheiro | Justificação |
|---|---|---|
| L-08 | en.json: `"license"` e `"licensePlaceholder"` | Usado no Contracts (OpenAPI license field) — não é resíduo |
| L-09 | InitialCreate migration | Não alterar migrations existentes — será limpo no migration reset |

---

## 5. Resumo de impacto

| Dimensão | Ficheiros afectados | Mudanças |
|---|---|---|
| Backend domain | 1 (RolePermissionCatalog.cs) | Remover 17 permissões |
| Backend infrastructure | 1 (PermissionConfiguration.cs) | Remover 2 seed entries |
| Backend application | 1 (CreateDelegation.cs) | Remover 1 referência |
| Backend domain VO | 1 (MfaPolicy.cs) | Reescrever 1 comentário |
| Frontend components | 1 (Breadcrumbs.tsx) | Remover 2 mappings |
| Frontend i18n | 4 (en.json + 3 traduções) | Remover 2 keys, reescrever 1 key cada |
| Migrations | 0 (não alterar) | — |

**Esforço total estimado: ~2 horas**

---

## 6. Dependências externas

Antes de remover licensing permissions:
1. ✅ Confirmar que nenhum outro módulo referencia `licensing:*` permissions
2. ✅ Confirmar que nenhuma UI depende de `licensing:*` para routing
3. ✅ Confirmar que nenhum teste depende de licensing permissions
4. ⚠️ A migration reset futura irá limpar o seed data na base de dados
