# E2 — Contracts Module Execution Report

## Data de Execução
2026-03-25

## Resumo
Execução real de correções no módulo Contracts conforme a trilha N.
Todas as alterações aproximam o módulo do desenho final, consolidam a fronteira
com o Catalog e preparam a persistência para a futura baseline com prefixo `ctr_`.

---

## Ficheiros de Código Alterados

### Domain — Entidades
| Ficheiro | Alteração |
|----------|-----------|
| `src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Entities/ContractVersion.cs` | Adicionado `RowVersion` (uint) para concorrência otimista xmin |
| `src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Entities/ContractDraft.cs` | Adicionado `RowVersion` (uint) para concorrência otimista xmin |
| `src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Entities/SpectralRuleset.cs` | Adicionado `RowVersion` (uint) para concorrência otimista xmin |

### Persistence — DbContext
| Ficheiro | Alteração |
|----------|-----------|
| `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Contracts/Persistence/ContractsDbContext.cs` | Adicionados 4 DbSets: SpectralRulesets, CanonicalEntities, ContractScorecards, ContractEvidencePacks. Outbox atualizado de `ct_outbox_messages` → `ctr_outbox_messages` |

### Persistence — EF Core Configurations (Atualizadas)
| Ficheiro | Alteração |
|----------|-----------|
| `ContractVersionConfiguration.cs` | Prefixo `ct_` → `ctr_`. IsRowVersion() para xmin. Check constraints para protocol e lifecycle_state. Índice filtrado IsDeleted. |
| `ContractDraftConfiguration.cs` | Prefixo `ct_` → `ctr_`. IsRowVersion() para xmin. Check constraint para status. Índice filtrado IsDeleted. |
| `ContractDiffConfiguration.cs` | Prefixo `ct_` → `ctr_`. |
| `ContractReviewConfiguration.cs` | Prefixo `ct_` → `ctr_`. |
| `ContractExampleConfiguration.cs` | Prefixo `ct_` → `ctr_`. |
| `ContractRuleViolationConfiguration.cs` | Prefixo `ct_` → `ctr_`. |
| `ContractArtifactConfiguration.cs` | Prefixo `ct_` → `ctr_`. |

### Persistence — EF Core Configurations (Novas — 4 ficheiros)
| Ficheiro | Descrição |
|----------|-----------|
| `SpectralRulesetConfiguration.cs` | **CRIADO** — Mapeamento completo com ctr_ prefix, xmin, check constraints, indexes |
| `CanonicalEntityConfiguration.cs` | **CRIADO** — Mapeamento completo com ctr_ prefix, check constraint state, text[] arrays |
| `ContractScorecardConfiguration.cs` | **CRIADO** — Mapeamento completo com FK→ContractVersion, precision(5,4) para scores |
| `ContractEvidencePackConfiguration.cs` | **CRIADO** — Mapeamento completo com FK→ContractVersion, text[] arrays |

### Frontend
| Ficheiro | Alteração |
|----------|-----------|
| `src/frontend/src/features/contracts/spectral/SpectralRulesetManagerPage.tsx` | Adicionado import de LoadingState/ErrorState. Adicionada verificação de isLoading e isError com early return. |
| `src/frontend/src/features/contracts/canonical/CanonicalEntityCatalogPage.tsx` | Adicionado import de LoadingState/ErrorState. Adicionada verificação de isLoading e isError com early return. |

### Documentação
| Ficheiro | Alteração |
|----------|-----------|
| `src/modules/catalog/README-contracts.md` | **CRIADO** — README completo do módulo Contracts (arquitetura, entidades, endpoints, DB, frontend, testes, segurança) |

---

## Correções por Parte

### PART 1 — Fronteira Catalog vs Contracts
- ✅ **Verificação**: Contracts já opera como bounded context próprio com DbContext separado (`ContractsDbContext`)
- ✅ **Verificação**: Contracts referencia Catalog apenas via `ApiAssetId` (Guid FK sem navigation property)
- ✅ **Verificação**: Comunicação cross-module via `IContractsModule` interface e Integration Events
- ✅ Fronteira documentada no README
- ⏳ Extração física (OI-01) mantida como pendente — fora do escopo deste E2

### PART 2 — Domínio
- ✅ `RowVersion` (uint) adicionado a ContractVersion, ContractDraft, SpectralRuleset
- ✅ Entidades já têm lifecycle validation no entity (TransitionTo, Lock, Sign, Deprecate)
- ✅ ContractDraft já tem métodos de transição (Submit, Approve, Reject, Publish)
- ✅ 13 entidades confirmadas e documentadas

### PART 3 — Persistência
- ✅ 4 novos DbSets: SpectralRulesets, CanonicalEntities, ContractScorecards, ContractEvidencePacks
- ✅ 4 novas EF Configurations criadas com mapeamento completo
- ✅ Prefixo `ct_` → `ctr_` em TODAS as 11 tabelas + outbox
- ✅ `IsRowVersion()` em ContractVersion, ContractDraft, SpectralRuleset
- ✅ Check constraints: protocol (6 values), lifecycle_state (7 values), draft status (5 values), spectral origin (4 values), canonical state (4 values)
- ✅ FK: ContractScorecard → ContractVersion (Cascade)
- ✅ FK: ContractEvidencePack → ContractVersion (Cascade)
- ✅ Índices filtrados: IsDeleted em Version, Draft, SpectralRuleset
- ✅ Precisão decimal: score fields com `precision(5, 4)`
- ✅ Arrays PostgreSQL: `text[]` para Aliases, Tags, ImpactedConsumers

### PART 4 — Backend
- ✅ **Verificação**: 31+ endpoints mapeados com RequirePermission
- ✅ **Verificação**: Granularidade permissions: contracts:read, contracts:write, contracts:import
- ✅ **Verificação**: 36 CQRS features implementadas
- ✅ Handlers existentes para import, versioning, diff, review, approval, scorecard, evidence pack
- ⏳ Spectral CRUD handlers e Canonical CRUD handlers — pendentes (ver gap report)
- ⏳ DbUpdateConcurrencyException handling — pendente (ver gap report)

### PART 5 — Frontend
- ✅ P0 routes confirmadas corrigidas (governance, spectral, canonical, portal)
- ✅ LoadingState/ErrorState adicionados a SpectralRulesetManagerPage
- ✅ LoadingState/ErrorState adicionados a CanonicalEntityCatalogPage
- ✅ GovernancePage e PortalPage já tinham LoadingState/ErrorState
- ✅ 12 React Query hooks implementados
- ⏳ i18n: pt-PT, pt-BR, es significativamente incompletos (ver gap report)

### PART 6 — Segurança
- ✅ Todos os endpoints têm RequirePermission
- ✅ Todas as rotas frontend têm ProtectedRoute com permissão
- ✅ Soft-delete com filtro global em entidades auditable
- ✅ Auditoria: CreatedAt, CreatedBy, UpdatedAt, UpdatedBy em todas as AuditableEntity
- ⏳ Ownership validation em lifecycle transitions — pendente (ver gap report)

### PART 7 — Documentação
- ✅ `README-contracts.md` criado com conteúdo completo

---

## Validação

- ✅ Build completo da solução: 0 erros
- ✅ 468 testes do módulo Catalog/Contracts: todos passam
- ✅ Sem migrations antigas removidas
- ✅ Sem nova baseline gerada

---

## Classes Alteradas

| Classe | Tipo de Alteração |
|--------|-------------------|
| `ContractVersion` | Novo campo RowVersion |
| `ContractDraft` | Novo campo RowVersion |
| `SpectralRuleset` | Novo campo RowVersion |
| `ContractsDbContext` | 4 novos DbSets, outbox prefix ctr_ |
| `ContractVersionConfiguration` | ctr_ prefix, xmin, check constraints, filtered index |
| `ContractDraftConfiguration` | ctr_ prefix, xmin, check constraint, filtered index |
| `ContractDiffConfiguration` | ctr_ prefix |
| `ContractReviewConfiguration` | ctr_ prefix |
| `ContractExampleConfiguration` | ctr_ prefix |
| `ContractRuleViolationConfiguration` | ctr_ prefix |
| `ContractArtifactConfiguration` | ctr_ prefix |
| `SpectralRulesetConfiguration` | **NOVA** — mapeamento completo |
| `CanonicalEntityConfiguration` | **NOVA** — mapeamento completo |
| `ContractScorecardConfiguration` | **NOVA** — mapeamento completo |
| `ContractEvidencePackConfiguration` | **NOVA** — mapeamento completo |

## Páginas/Componentes Alterados

| Componente | Alteração |
|-----------|-----------|
| `SpectralRulesetManagerPage` | LoadingState + ErrorState |
| `CanonicalEntityCatalogPage` | LoadingState + ErrorState |
