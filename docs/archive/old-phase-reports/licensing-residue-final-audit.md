# Auditoria Final de Resíduos de Licensing — NexTraceOne

> Prompt N16 — Parte 6 | Data: 2026-03-25 | Fase: Encerramento da Trilha N

---

## 1. Resumo

Licensing foi oficialmente removido do escopo do NexTraceOne. Este relatório audita todas as referências remanescentes a Licensing em todo o repositório.

**Total de ocorrências relevantes: 8 localizações**
- 🔴 REMOVE: 5
- 🟡 REWRITE: 2
- ⚪ KEEP_HISTORICAL_ONLY: 1

---

## 2. Referências no Backend

### LIC-01 — RolePermissionCatalog.cs (17 permissões)

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/RolePermissionCatalog.cs` |
| **Linhas** | 125-141 (Admin), 177 (TechLead) |
| **Conteúdo** | **Admin role (16 permissões):** `licensing:read`, `licensing:write`, `licensing:vendor:license:create`, `licensing:vendor:license:revoke`, `licensing:vendor:license:rehost`, `licensing:vendor:license:read`, `licensing:vendor:key:generate`, `licensing:vendor:trial:extend`, `licensing:vendor:activation:issue`, `licensing:vendor:tenant:manage`, `licensing:vendor:telemetry:view`, `licensing:vendor:plan:create`, `licensing:vendor:plan:read`, `licensing:vendor:featurepack:create`, `licensing:vendor:featurepack:read`, `licensing:vendor:license:manage` |
| | **TechLead role (1 permissão):** `licensing:read` |
| **Classificação** | 🔴 **REMOVE** |

### LIC-02 — PermissionConfiguration.cs (HasData seed)

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Persistence/Configurations/PermissionConfiguration.cs` |
| **Linhas** | 73-74 |
| **Conteúdo** | `Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C080")), "licensing:read", "View license information", "Licensing")` e `Permission.Create(PermissionId.From(new Guid("2E91A557-FADE-46DF-B248-0F5F5899C081")), "licensing:write", "Manage licenses", "Licensing")` |
| **Classificação** | 🔴 **REMOVE** — será eliminado na recriação das migrations |

### LIC-03 — CreateDelegation.cs (permissão delegável)

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/CreateDelegation/CreateDelegation.cs` |
| **Linha** | 64 |
| **Conteúdo** | `"licensing:write"` na lista de permissões que podem ser delegadas |
| **Classificação** | 🔴 **REMOVE** |

### LIC-04 — MfaPolicy.cs (comentário XML)

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/ValueObjects/MfaPolicy.cs` |
| **Linha** | 58 |
| **Conteúdo** | `/// <summary>Indica se MFA step-up é exigido para operações de vendor/licensing.</summary>` |
| **Classificação** | 🟡 **REWRITE** — manter propriedade, reescrever comentário sem "licensing" |

---

## 3. Referências no Frontend

### LIC-05 — Breadcrumbs.tsx (mapeamento de rota)

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/frontend/src/components/Breadcrumbs.tsx` |
| **Linha** | 34 |
| **Conteúdo** | `'licensing': 'sidebar.licensing'` |
| **Classificação** | 🔴 **REMOVE** |

### LIC-06 — navigation.ts (mapeamento de rota)

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/frontend/src/utils/navigation.ts` |
| **Linha** | 48 |
| **Conteúdo** | `'vendor': 'sidebar.vendorLicensing'` |
| **Classificação** | 🔴 **REMOVE** |

### LIC-07 — en.json (admin guidance text)

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/frontend/src/locales/en.json` |
| **Linha** | ~3353 |
| **Conteúdo** | `"guidanceAdmin": "Administration covers user management, access controls, licensing, and audit trails."` |
| **Classificação** | 🟡 **REWRITE** — remover "licensing" da frase |

---

## 4. Referências em Configuração/Seeds

### LIC-08 — ConfigurationDefinitionSeeder.cs (referência contextual)

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/modules/configuration/NexTraceOne.Configuration.Infrastructure/Seed/ConfigurationDefinitionSeeder.cs` |
| **Conteúdo** | Referências contextuais: `"license":"Proprietary"` (meta informação de API), `"UnusedLicenses"` (categoria de waste signal em FinOps) |
| **Classificação** | ⚪ **KEEP_HISTORICAL_ONLY** — "license" aqui refere-se a licenças de software de terceiros e licenças de recursos cloud (FinOps), não ao módulo Licensing removido |

---

## 5. Referências em Documentação

### Documentação de módulo

- ✅ `docs/11-review-modular/01-identity-access/licensing-residue-cleanup-review.md` — já existe e documenta a limpeza necessária
- ✅ Modular review master documenta remoção da pasta de Licensing
- ✅ `phase-a-open-items.md` lista OI-05 (Licensing residues)

### AIModel LicenseName/LicenseUrl

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Domain/Governance/Entities/AIModel.cs` |
| **Conteúdo** | Propriedades `LicenseName` e `LicenseUrl` — referem-se à licença do modelo de IA (MIT, Apache, etc.), **não** ao módulo Licensing |
| **Classificação** | ⚪ **KEEP_HISTORICAL_ONLY** — uso legítimo do termo "license" |

---

## 6. Resumo de Impacto

| Camada | Ocorrências | Ação |
|---|---|---|
| Domain (Identity) | LIC-01, LIC-04 | 17 permissões + 1 comentário |
| Infrastructure (Identity) | LIC-02 | 2 seeds em HasData |
| Application (Identity) | LIC-03 | 1 permissão delegável |
| Frontend (Navigation) | LIC-05, LIC-06 | 2 mapeamentos de rota |
| Frontend (i18n) | LIC-07 | 1 string de guidance |
| Configuração | LIC-08 | Uso legítimo — manter |
| AI Domain | AIModel | Uso legítimo — manter |

---

## 7. Backlog Consolidado de Limpeza

| Ordem | ID | Ficheiro | Ação | Estimativa |
|---|---|---|---|---|
| 1 | LIC-01 | `RolePermissionCatalog.cs` | Remover 17 permissões licensing:* | 1h |
| 2 | LIC-03 | `CreateDelegation.cs` | Remover `licensing:write` | 0.5h |
| 3 | LIC-02 | `PermissionConfiguration.cs` | Remover HasData licensing (na recriação migrations) | 0.5h |
| 4 | LIC-05 | `Breadcrumbs.tsx` | Remover `'licensing'` mapping | 0.5h |
| 5 | LIC-06 | `navigation.ts` | Remover `'vendor'` mapping | 0.5h |
| 6 | LIC-04 | `MfaPolicy.cs` | Reescrever comentário XML | 0.5h |
| 7 | LIC-07 | `en.json` (+ pt-BR, es) | Reescrever guidanceAdmin | 0.5h |

**Total estimado: ~4h**

---

## 8. Critério de Completude

A limpeza de Licensing será considerada completa quando:

- [ ] 0 referências a `licensing:` em `RolePermissionCatalog.cs`
- [ ] 0 referências a `licensing:` em `PermissionConfiguration.cs`
- [ ] 0 referências a `licensing:` em `CreateDelegation.cs`
- [ ] 0 mapeamentos de rota `'licensing'` ou `'vendor'` no frontend
- [ ] 0 menções a "licensing" em strings de guidance i18n
- [ ] Comentário de `MfaPolicy.cs` reescrito sem "licensing"
- [ ] `grep -ri "licensing:" src/modules/ src/frontend/src/` retorna 0 resultados relevantes (excluindo AIModel.LicenseName que é legítimo)
