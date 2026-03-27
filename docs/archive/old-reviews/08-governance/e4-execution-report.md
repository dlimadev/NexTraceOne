# E4 — Governance Module Execution Report

## Data de Execução
2026-03-25

## Resumo
Execução real de correções no módulo Governance conforme a trilha N.
Todas as alterações contêm o escopo do módulo, adicionam concorrência otimista,
reforçam regras de domínio e melhoram a segurança com permissões granulares.

---

## Ficheiros de Código Alterados

### Domain — Entidades
| Ficheiro | Alteração |
|----------|-----------|
| `Team.cs` | Adicionado RowVersion (uint xmin) para concorrência otimista. |
| `GovernanceDomain.cs` | Adicionado RowVersion (uint xmin) para concorrência otimista. |
| `GovernancePack.cs` | Adicionado RowVersion (uint xmin). Adicionados guards de transição de estado: Publish (requer Draft), Deprecate (requer Published), Archive (requer Deprecated). |
| `GovernanceWaiver.cs` | Adicionado RowVersion (uint xmin). Adicionados guards de transição de estado: Approve/Reject (requer Pending), Revoke (requer Approved). |

### Persistence — EF Core Configurations
| Ficheiro | Alteração |
|----------|-----------|
| `TeamConfiguration.cs` | Adicionado IsRowVersion() para xmin. Adicionado check constraint CK_gov_teams_status. |
| `GovernanceDomainConfiguration.cs` | Adicionado IsRowVersion() para xmin. Adicionado check constraint CK_gov_domains_criticality. |
| `GovernancePackConfiguration.cs` | Adicionado IsRowVersion() para xmin. Adicionado check constraint CK_gov_packs_status. Adicionados mapeamentos de timestamp. |
| `GovernanceWaiverConfiguration.cs` | Adicionado IsRowVersion() para xmin. Adicionado check constraint CK_gov_waivers_status. Adicionada FK Waiver→GovernancePack (Restrict). |
| `GovernanceDbContext.cs` | Atualizada documentação XML: documenta explicitamente que Integrations e ProductAnalytics estão temporariamente no DbContext e serão extraídos em OI-02/OI-03. |

### Backend — Endpoints
| Ficheiro | Alteração |
|----------|-----------|
| `DelegatedAdminEndpointModule.cs` | Permissão GET: governance:teams:read → governance:admin:read. Permissão POST: governance:teams:write → governance:admin:write. XML docs atualizados. |

### Identity — Permissões
| Ficheiro | Alteração |
|----------|-----------|
| `RolePermissionCatalog.cs` | Adicionadas permissões governance:admin:read e governance:admin:write ao PlatformAdmin. |

### Frontend — Sidebar + i18n
| Ficheiro | Alteração |
|----------|-----------|
| `AppSidebar.tsx` | Substituída permissão genérica governance:read por permissões granulares em 12 itens (governance:reports:read, governance:compliance:read, governance:risk:read, etc.). Adicionadas 3 novas entradas: Waivers, Controls, Evidence. |
| `en.json` | Adicionadas chaves sidebar.controls e sidebar.evidence. |
| `pt-PT.json` | Adicionadas chaves sidebar.controls e sidebar.evidence. |
| `pt-BR.json` | Adicionadas chaves sidebar.controls e sidebar.evidence. |
| `es.json` | Adicionadas chaves sidebar.controls e sidebar.evidence. |

### Documentação
| Ficheiro | Alteração |
|----------|-----------|
| `src/modules/governance/README.md` | **CRIADO** — README completo com escopo, arquitetura, entidades, lifecycle, DB, permissões, frontend, consumidores. |

---

## Correções por Parte

### PART 1 — Contenção do Escopo
- ✅ GovernanceDbContext XML docs atualizado: documenta explicitamente que IntegrationConnectors, IngestionSources, IngestionExecutions, AnalyticsEvents são temporários e serão extraídos
- ✅ README documenta claramente o que pertence e o que não pertence ao módulo
- ⏳ Extração física de Integrations (OI-02) e Product Analytics (OI-03) → Wave 0

### PART 2 — Domínio
- ✅ RowVersion (uint) adicionado a Team, GovernanceDomain, GovernancePack, GovernanceWaiver
- ✅ GovernancePack: guards de transição Draft→Published→Deprecated→Archived
- ✅ GovernanceWaiver: guards de transição Pending→Approved/Rejected, Approved→Revoked
- ✅ InvalidOperationException com mensagem descritiva para transições inválidas

### PART 3 — Persistência
- ✅ IsRowVersion() xmin em 4 aggregate configs
- ✅ Check constraints em 4 tabelas (TeamStatus, DomainCriticality, GovernancePackStatus, WaiverStatus)
- ✅ FK: GovernanceWaiver → GovernancePack (Restrict)
- ✅ Timestamp mappings explícitos em GovernancePackConfiguration

### PART 4 — Backend
- ✅ DelegatedAdmin GET: governance:teams:read → governance:admin:read
- ✅ DelegatedAdmin POST: governance:teams:write → governance:admin:write
- ✅ Documentação XML atualizada nos endpoints

### PART 5 — Frontend
- ✅ 3 novas entradas de sidebar: Waivers, Controls, Evidence
- ✅ 12 itens de sidebar com permissões granulares (em vez de governance:read genérico)
- ✅ i18n em 4 locales para novas chaves

### PART 6 — Segurança
- ✅ governance:admin:read e governance:admin:write adicionados ao PlatformAdmin
- ✅ DelegatedAdmin protegido com governance:admin:write (ação sensível)
- ✅ Sidebar items protegidos com permissões granulares

### PART 7 — Dependências
- ✅ Documentado no README e DbContext que Integrations e ProductAnalytics são temporários
- ⏳ Integrações detalhadas → OI-02, OI-03

### PART 8 — Documentação
- ✅ README.md criado com conteúdo completo

---

## Validação

- ✅ Build completo da solução: 0 erros
- ✅ 163 testes Governance: todos passam
- ✅ 290 testes Identity: todos passam (após adição de governance:admin:*)
- ✅ Sem migrations antigas removidas
- ✅ Sem nova baseline gerada

---

## Classes Alteradas

| Classe | Tipo de Alteração |
|--------|-------------------|
| `Team` | RowVersion (uint xmin) |
| `GovernanceDomain` | RowVersion (uint xmin) |
| `GovernancePack` | RowVersion (uint xmin), status transition guards (Publish, Deprecate, Archive) |
| `GovernanceWaiver` | RowVersion (uint xmin), status transition guards (Approve, Reject, Revoke) |
| `TeamConfiguration` | IsRowVersion(), check constraint |
| `GovernanceDomainConfiguration` | IsRowVersion(), check constraint |
| `GovernancePackConfiguration` | IsRowVersion(), check constraint, timestamps |
| `GovernanceWaiverConfiguration` | IsRowVersion(), check constraint, FK |
| `GovernanceDbContext` | XML docs atualizados |
| `DelegatedAdminEndpointModule` | Permissões corrigidas |
| `RolePermissionCatalog` | governance:admin:read/write adicionados |

## Endpoints Alterados

| Endpoint | Alteração |
|----------|-----------|
| `GET /api/v1/admin/delegations` | governance:teams:read → governance:admin:read |
| `POST /api/v1/admin/delegations` | governance:teams:write → governance:admin:write |
