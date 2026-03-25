# E5 — Service Catalog Post-Execution Gap Report

## Data
2026-03-25

## Resumo

Este relatório documenta o que foi resolvido, o que ficou pendente,
e o que depende de outras fases após a execução do E5 para o módulo Service Catalog.

---

## ✅ Resolvido Nesta Fase

| Item | Categoria | Estado |
|------|-----------|--------|
| RowVersion (xmin) em ServiceAsset, ApiAsset | Domínio + Persistência | ✅ Concluído |
| ServiceAsset lifecycle transition validation (`TransitionTo()`) | Domínio | ✅ Concluído |
| InvalidLifecycleTransition error | Domínio | ✅ Concluído |
| 14 tabelas + 2 outbox → `cat_` prefix | Persistência | ✅ Concluído |
| 4 check constraints em `cat_service_assets` | Persistência | ✅ Concluído |
| IsRowVersion() xmin em 2 aggregates | Persistência | ✅ Concluído |
| README do módulo | Documentação | ✅ Concluído |
| Build: 0 erros | Validação | ✅ |
| Testes: 468/468 Catalog | Validação | ✅ |

---

## ⏳ Pendente — Depende de Outras Fases

| Item | Categoria | Bloqueador | Fase Esperada | Esforço |
|------|-----------|------------|---------------|---------|
| Extração de Contracts para src/modules/contracts/ | Boundary (OI-01) | Wave 0 extraction | Wave 0 | 3-4 sprints |
| Gerar baseline migration (InitialCreate) | Persistência | Requer OI-01 | Wave 2 | 1 sprint |
| TransitionAssetLifecycle CQRS handler | Backend (FC-03) | New handler | E5+ | 4h |
| UpdateServiceAsset CQRS handler | Backend (FC-04) | New handler | E5+ | 2h |
| DecommissionAsset handler | Backend (FC-09) | New handler | E5+ | 2h |
| BulkImportAssets endpoint | Backend (FC-10) | New handler | E5+ | 4h |
| DbUpdateConcurrencyException handling in write handlers → 409 | Backend (FC-02) | Handler update | E5+ | 1h |
| Check constraints on other enums (ConsumerRelationship, DiscoverySource) | Persistence (FC-07) | Config update | E5+ | 2h |
| Filtered indexes WHERE is_deleted = false | Persistence (FC-08) | Config update | E5+ | 1h |
| Remove orphaned pages (ContractDetailPage, ContractListPage, ContractsPage) | Frontend (QW) | Page cleanup | E5+ | 1h |
| ContractVersionDetailPanel dead code removal | Frontend (SA-07) | Code cleanup | E5+ | 15min |
| Breadcrumb consistency across 9 pages | Frontend (SA-08) | UI cleanup | E5+ | 1h |
| Verify CatalogContractsConfigurationPage loading states | Frontend (FC-05) | Verification | E5+ | 30min |
| i18n completeness (pt-BR, es) | Frontend (FC-06) | i18n | E5+ | 2h |
| Consolidate DbContexts (CatalogGraph + DeveloperPortal → single CatalogDbContext) | Architecture (SA-06) | LOW priority | Future | 4h |
| Domain events for lifecycle transitions | Domain (SA-04) | New feature | E5+ | 1h |
| Integration event publishing | Infrastructure (SA-05) | New feature | E5+ | 2h |
| ApiAsset lifecycle transition validation | Domain | New method | E5+ | 1h |

---

## 🚫 Não Bloqueia Evolução

Todos os itens pendentes são incrementais e **não bloqueiam** a evolução para:

1. **OI-01** — Extração de Contracts para módulo próprio (Wave 0)
2. **E6+** — Próximos módulos da trilha E
3. **Baseline generation** — Quando OI-01 estiver concluído (Wave 2)

---

## 📊 Métricas de Maturidade

| Dimensão | Antes do E5 | Após E5 | Target |
|----------|-------------|---------|--------|
| Backend | 70% | 73% | 90% |
| Frontend | 60% | 62% | 85% |
| Persistência | 50% | 80% | 100% |
| Segurança | 75% | 78% | 90% |
| Documentação | 30% | 65% | 85% |
| Domínio | 65% | 80% | 95% |
| **Global** | **58%** | **73%** | **91%** |

A maturidade global subiu 15 pontos percentuais (58% → 73%), com os maiores ganhos na
persistência (50% → 80%) pela unificação de prefixo e concorrência otimista,
e domínio (65% → 80%) pela adição de lifecycle validation.

---

## Decisões Tomadas Durante E5

1. **xmin via IsRowVersion()**: Consistente com padrão E1 (Configuration), E2 (Contracts),
   E3 (Environment), E4 (Governance).

2. **cat_ como prefixo único**: Todas as 14 tabelas + 2 outbox tables agora usam `cat_`.
   Substituiu `eg_`, `dp_`, `graph_`, `node_`, `saved_`, `sot_`. Isto simplifica o modelo
   mental e prepara para baseline Wave 2.

3. **Lifecycle transitions no domínio**: O método `TransitionTo()` foi implementado
   diretamente em ServiceAsset como domain logic. Staging pode retroceder a Development
   e Deprecating pode reverter para Active (rollback paths comuns em cenários reais).

4. **DbContext consolidation adiada**: A consolidação de CatalogGraphDbContext +
   DeveloperPortalDbContext num único CatalogDbContext (SA-06) permanece como LOW priority
   porque a separação atual funciona corretamente e a consolidação seria melhor feita
   junto com a baseline (Wave 2).

5. **Frontend pages cleanup adiado**: A remoção de páginas órfãs (ContractDetailPage,
   ContractListPage, ContractsPage) é quick win mas depende da confirmação de que
   nenhuma rota active as utiliza. Adiado para E5+ com verificação.
