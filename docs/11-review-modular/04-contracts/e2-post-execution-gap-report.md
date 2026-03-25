# E2 — Contracts Module Post-Execution Gap Report

## Data
2026-03-25

## Resumo

Este relatório documenta o que foi resolvido, o que ainda ficou pendente,
e o que depende de outras fases após a execução do E2 para o módulo Contracts.

---

## ✅ Resolvido Nesta Fase

| Item | Categoria | Estado |
|------|-----------|--------|
| RowVersion (xmin) em ContractVersion, ContractDraft, SpectralRuleset | Domínio + Persistência | ✅ Concluído |
| 4 DbSets em falta: SpectralRuleset, CanonicalEntity, ContractScorecard, ContractEvidencePack | Persistência | ✅ Concluído |
| 4 EF Configurations criadas | Persistência | ✅ Concluído |
| Prefixo ct_ → ctr_ em todas as 11 tabelas + outbox | Persistência | ✅ Concluído |
| Check constraints: Protocol, LifecycleState, DraftStatus, Origin, CanonicalEntityState | Persistência | ✅ Concluído |
| FK: Scorecard→Version, EvidencePack→Version | Persistência | ✅ Concluído |
| Índices filtrados: IsDeleted em Version, Draft, SpectralRuleset | Persistência | ✅ Concluído |
| LoadingState/ErrorState em SpectralRulesetManagerPage | Frontend | ✅ Concluído |
| LoadingState/ErrorState em CanonicalEntityCatalogPage | Frontend | ✅ Concluído |
| P0 routes (governance, spectral, canonical, portal) confirmadas | Frontend | ✅ Verificado |
| Permissões RequirePermission em todos endpoints | Segurança | ✅ Verificado |
| ProtectedRoute em todas rotas frontend | Segurança | ✅ Verificado |
| README do módulo Contracts | Documentação | ✅ Concluído |
| Build: 0 erros | Validação | ✅ |
| Testes: 468/468 passam | Validação | ✅ |

---

## ⏳ Pendente — Depende de Outras Fases

| Item | Categoria | Bloqueador | Fase Esperada | Esforço |
|------|-----------|------------|---------------|---------|
| Extração física do código para `src/modules/contracts/` | Boundary (OI-01) | Wave 0 extraction | Wave 0 | 3-4 sprints |
| Gerar migration `InitialCreate` (baseline) | Persistência | Requer que TODOS os modelos estejam finais | E-Baseline (Wave 2) | 1 sprint |
| SpectralRuleset CRUD handlers (6 features) | Backend | Frontend hooks chamam endpoints inexistentes | E3 | 6h |
| CanonicalEntity CRUD handlers (5 features) | Backend | Frontend hooks chamam endpoints inexistentes | E3 | 6h |
| SpectralRuleset endpoints (6 endpoints) | Backend | Depende dos handlers acima | E3 | 2h |
| CanonicalEntity endpoints (5 endpoints) | Backend | Depende dos handlers acima | E3 | 2h |
| DbUpdateConcurrencyException handling | Backend | Funcionalidade de segurança, RowVersion já adicionado | E3 | 2h |
| Contract Portal dedicated read endpoint | Backend | ContractPortalPage funciona com useContractDetail | E3 | 2h |
| Ownership validation em lifecycle transitions | Segurança | Decisão de produto necessária | E3 | 2h |
| i18n: pt-PT (56 keys missing) | Frontend | QA de tradução | E3 | 2h |
| i18n: pt-BR (59 keys missing) | Frontend | QA de tradução | E3 | 2h |
| i18n: es (56 keys missing) | Frontend | QA de tradução | E3 | 2h |
| Rate limiting import endpoint → data-intensive | Backend | Low priority | E3 | 15min |
| Domain events para lifecycle transitions | Domain | Infra de eventos necessária | E3-E4 | 1h |
| Integration event publishing verificado | Backend | Depende de infra de eventos | E3-E4 | 2h |
| Remove legacy pages em catalog/pages/ | Frontend | Low priority, dead code | E3 | 30min |
| XML docs em ContractVersion lifecycle | Documentação | Esforço editorial | E3 | 1h |
| Diagrama de state machine lifecycle | Documentação | Esforço editorial | E3 | 1h |
| Concurrency conflict handling end-to-end | Backend | DbUpdateConcurrencyException catch | E3 | 2h |
| Integração com ClickHouse | Persistência | ClickHouse: NOT_REQUIRED para Contracts | N/A | - |

---

## 🚫 Não Bloqueia Evolução para E3

Todos os itens pendentes são incrementais e **não bloqueiam** a próxima fase.
O módulo Contracts pode avançar para:

1. **E3 corrections** — SpectralRuleset/CanonicalEntity CRUD, concurrency handling, i18n
2. **OI-01 extraction** — extração física para `src/modules/contracts/` (Wave 0)
3. **Baseline generation** — quando os modelos de TODOS os módulos do Wave 2 estiverem finalizados

---

## 📊 Métricas de Maturidade

| Dimensão | Antes do E2 | Após E2 | Target |
|----------|-------------|---------|--------|
| Backend | 75% | 78% | 90% |
| Frontend | 60% | 68% | 85% |
| Persistência | 55% | 85% | 100% |
| Documentação | 55% | 70% | 85% |
| Testes | 90% | 90% | 95% |
| **Global** | **68%** | **78%** | **90%** |

A maturidade subiu 10 pontos percentuais (68% → 78%), principalmente pela correção
significativa da persistência (55% → 85%) e do frontend (60% → 68%).

---

## Decisões Tomadas Durante E2

1. **xmin via IsRowVersion()**: Utilizada API `IsRowVersion()` do EF Core Npgsql 10.x (mesma decisão do E1 Configuration). A convenção Npgsql mapeia automaticamente `uint` + `IsRowVersion()` para a coluna `xmin` do PostgreSQL.

2. **ContractLock não mapeado como DbSet separado**: `ContractLock` é um sealed record (value object), não uma entidade com ciclo de vida próprio. Os campos `IsLocked`, `LockedAt`, `LockedBy` estão diretamente em `ContractVersion`. Não necessita de tabela própria.

3. **Prefixo ctr_ aplicado agora, migrations depois**: As EF Configurations já definem `ctr_` como prefixo de tabela. A migration que efetivamente cria as tabelas será gerada na fase de baseline (Wave 2). Isto evita conflitos com migrations existentes.

4. **text[] para arrays PostgreSQL**: Campos como `Aliases`, `Tags` e `ImpactedConsumers` são mapeados para `text[]` (array nativo PostgreSQL) em vez de JSON serializado, para melhor queryability.

5. **SpectralRuleset e CanonicalEntity CRUD handlers adiados**: Os handlers e endpoints CRUD para estas entidades são necessários (os hooks frontend chamam endpoints que não existem), mas a criação exige tempo significativo (6h cada). Foram documentados como gap prioritário para E3.

6. **i18n parcial aceite**: As traduções en.json estão completas (68 keys). pt-PT, pt-BR e es têm gaps significativos (12, 9 e 12 keys respetivamente vs 68 en.json). A correção é mecânica mas requer QA de tradução.
