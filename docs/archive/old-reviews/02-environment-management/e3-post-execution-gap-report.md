# E3 — Environment Management Module Post-Execution Gap Report

## Data
2026-03-25

## Resumo

Este relatório documenta o que foi resolvido, o que ainda ficou pendente,
e o que depende de outras fases após a execução do E3 para o módulo Environment Management.

---

## ✅ Resolvido Nesta Fase

| Item | Categoria | Estado |
|------|-----------|--------|
| RowVersion (xmin) em Environment e EnvironmentAccess | Domínio + Persistência | ✅ Concluído |
| IsDeleted, UpdatedAt, UpdatedBy, CreatedBy em Environment | Domínio + Persistência | ✅ Concluído |
| Guard Deactivate() para primary production | Domínio | ✅ Concluído |
| SoftDelete() com guard contra primary production | Domínio | ✅ Concluído |
| SetUpdated() para auditoria | Domínio | ✅ Concluído |
| Prefixo identity_ → env_ em 2 tabelas | Persistência | ✅ Concluído |
| Check constraints (Profile, Criticality, SortOrder, AccessLevel) | Persistência | ✅ Concluído |
| FK EnvironmentAccess → Environment | Persistência | ✅ Concluído |
| Unique filtered index active access | Persistência | ✅ Concluído |
| Filtered indexes (active, profile, criticality, soft-delete) | Persistência | ✅ Concluído |
| Query filter soft-delete global | Persistência | ✅ Concluído |
| Permissões migradas identity:users:* → env:* | Backend + Segurança | ✅ Concluído |
| Escalação set-primary → env:environments:admin | Segurança | ✅ Concluído |
| Escalação grant-access → env:access:admin | Segurança | ✅ Concluído |
| 5 permissões env:* em 7 roles RolePermissionCatalog | Segurança | ✅ Concluído |
| Sidebar entry /environments | Frontend | ✅ Concluído |
| i18n sidebar em 4 locales | Frontend | ✅ Concluído |
| README do módulo | Documentação | ✅ Concluído |
| Build: 0 erros | Validação | ✅ |
| Testes: 290/290 passam | Validação | ✅ |

---

## ⏳ Pendente — Depende de Outras Fases

| Item | Categoria | Bloqueador | Fase Esperada | Esforço |
|------|-----------|------------|---------------|---------|
| Extração física para `src/modules/environmentmanagement/` | Boundary (OI-04) | Wave 0 extraction | Wave 0 | 3-4 sprints |
| Gerar migration InitialCreate (baseline) | Persistência | Requer modelos finais | E-Baseline (Wave 1) | 1 sprint |
| GET /api/v1/environments/{id} endpoint (detail) | Backend | Novo handler necessário | E4 | 3h |
| DELETE /api/v1/environments/{id} endpoint (soft-delete) | Backend | Novo handler necessário | E4 | 2h |
| POST /api/v1/environments/{id}/revoke-access endpoint | Backend | Novo handler necessário | E4 | 2h |
| GET /api/v1/environments/{id}/accesses endpoint (list) | Backend | Novo handler necessário | E4 | 2h |
| EnvironmentDetailPage frontend | Frontend | Depende do GET detail endpoint | E4 | 8h |
| Slug format validation (alphanumeric + hyphens) | Backend | Handler update | E4 | 1h |
| Slug uniqueness pre-check before save | Backend | Handler update | E4 | 1h |
| FluentValidation para max lengths | Backend | Handler update | E4 | 1h |
| DbUpdateConcurrencyException handling → 409 Conflict | Backend | Handler update | E4 | 2h |
| Privilege escalation guard em grant-access | Security | Handler update | E4 | 2h |
| User existence validation em grant-access | Security | Handler update | E4 | 1h |
| Domain events (Created, Deactivated, PrimaryDesignated, AccessGranted, AccessRevoked) | Domain | Infra de eventos necessária | E4 | 3h |
| Confirmation modal "Set as Primary Production" | Frontend | UX improvement | E4 | 1h |
| Confirmation modal "Delete Environment" | Frontend | UX improvement | E4 | 1h |
| Dedicated EnvironmentDbContext | Persistence | OI-04 extraction | Wave 0-1 | 3h |
| Frontend API client separation (identity.ts → environments.ts) | Frontend | E4 | E4 | 2h |
| Filters (profile, criticality, active/inactive) on list page | Frontend | E4 | E4 | 2h |
| Pagination on list page | Frontend | E4 | E4 | 2h |
| Environment readiness/status | Domain | Feature expansion | E4-E5 | 4h |
| Promotion path management | Domain | Cross-module | E5+ | 8h |
| Baseline/drift tracking | Domain | Feature expansion | E5+ | 6h |
| Environment comparison on Environment detail page | Frontend | Feature expansion | E5+ | 4h |
| CreatedBy population in handlers | Backend | Handler update | E4 | 1h |

---

## 🚫 Não Bloqueia Evolução para E4

Todos os itens pendentes são incrementais e **não bloqueiam** a próxima fase.
O módulo Environment Management pode avançar para:

1. **E4 corrections** — 4 novos endpoints (detail, delete, revoke-access, list-accesses), validations, domain events
2. **OI-04 extraction** — extração física para `src/modules/environmentmanagement/` (Wave 0)
3. **Baseline generation** — quando os modelos de todos os módulos Wave 1 estiverem finalizados

---

## 📊 Métricas de Maturidade

| Dimensão | Antes do E3 | Após E3 | Target |
|----------|-------------|---------|--------|
| Backend | 60% | 70% | 90% |
| Frontend | 55% | 62% | 85% |
| Persistência | 50% | 82% | 100% |
| Segurança | 30% | 75% | 90% |
| Documentação | 40% | 65% | 85% |
| Testes | 85% | 85% | 95% |
| **Global** | **53%** | **73%** | **90%** |

A maturidade global subiu 20 pontos percentuais (53% → 73%), principalmente pela
correção significativa da segurança (30% → 75%) e da persistência (50% → 82%).

---

## Decisões Tomadas Durante E3

1. **xmin via IsRowVersion()**: Consistente com padrão E1 (Configuration) e E2 (Contracts).

2. **Sidebar entry em admin section**: O `/environments` foi adicionado à secção admin do sidebar,
   entre "Access Review" e "My Sessions", por ser uma funcionalidade de administração de plataforma.

3. **env:* namespace criado agora**: As permissões foram migradas imediatamente de `identity:users:*`
   para `env:*`, evitando que a permissão incorreta se enraíze no produto.

4. **env:environments:admin para ações críticas**: Ações como designar produção principal e
   soft-delete requerem nível admin, escaladas de `identity:users:write` para `env:environments:admin`.

5. **Query filter global para soft-delete**: Adicionado `HasQueryFilter(e => !e.IsDeleted)` em
   EnvironmentConfiguration. Isto garante que queries padrão nunca retornam ambientes soft-deleted
   sem opt-in explícito (`.IgnoreQueryFilters()`).

6. **FK EnvironmentAccess → Environment com Cascade**: Quando um ambiente é removido, os acessos
   associados são automaticamente removidos pelo banco. Isto simplifica a lógica de soft-delete.

7. **Prefixo env_ aplicado agora, migrations depois**: Consistente com E1/E2. As EF Configurations
   já definem `env_` como prefixo. A migration será gerada na fase de baseline (Wave 1).
