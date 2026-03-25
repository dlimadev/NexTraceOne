# PARTE 13 — Plano Final de Remediação do Módulo Product Analytics

> **Data**: 2026-03-25
> **Prompt**: N12 — Consolidação do módulo Product Analytics
> **Estado**: BACKLOG EXECUTÁVEL FINAL

---

## Resumo do módulo

| Aspecto | Valor |
|---------|-------|
| **Maturidade atual** | ~30% |
| **Maturidade alvo** | 80% |
| **Total de itens** | 52 |
| **Esforço total estimado** | ~195h |
| **Sprints estimados** | 7 sprints |
| **Blocker count** | 2 (P0) |
| **Critical count** | 10 (P1) |
| **High count** | 22 (P2) |
| **Medium count** | 18 (P3) |

---

## A. Quick Wins (Sprint 1 — ~12h)

Itens pequenos e de alto valor que podem ser executados imediatamente.

| # | ID | Item | Prioridade | Esforço | Ficheiro(s) |
|---|-----|------|-----------|---------|-------------|
| 1 | QW-01 | Remover `ProductAnalyticsOverviewPage.tsx` vazio da raiz do feature | P1 | 0.5h | `src/frontend/src/features/product-analytics/ProductAnalyticsOverviewPage.tsx` (raiz) |
| 2 | QW-02 | Corrigir mapeamento de módulo no AnalyticsEventTracker (`/analytics` → ProductAnalytics, não Governance) | P2 | 0.5h | `AnalyticsEventTracker.tsx` |
| 3 | QW-03 | Adicionar keys i18n ausentes (12+ keys) nos 3 idiomas | P2 | 1h | `pt-PT.json`, `es.json`, `en.json` |
| 4 | QW-04 | Adicionar empty states quando dados são 0 ou vazios nas páginas | P2 | 2h | 5 páginas frontend |
| 5 | QW-05 | Adicionar loading states consistentes | P2 | 1h | 5 páginas frontend |
| 6 | QW-06 | Adicionar error states com retry | P2 | 2h | 5 páginas frontend |
| 7 | QW-07 | Adicionar validação de range máximo em GET queries | P2 | 2h | 6 GET handlers |
| 8 | QW-08 | Documentar fórmulas dos scores compostos nos handlers | P2 | 1h | GetAnalyticsSummary handler |
| 9 | QW-09 | Adicionar aviso visual "dados parciais" nos dashboards com dados mistos | P1 | 2h | Overview, Personas, Journeys, Value pages |

---

## B. Correções Funcionais Obrigatórias (Sprints 2-3 — ~65h)

Itens necessários para o módulo ficar realmente pronto.

| # | ID | Item | Prioridade | Esforço | Ficheiro(s) |
|---|-----|------|-----------|---------|-------------|
| 1 | CF-01 | **Eliminar mock data em GetPersonaUsage** — substituir dados hardcoded por queries reais ao repository | P0_BLOCKER | 4h | `GetPersonaUsage/Handler.cs` |
| 2 | CF-02 | Melhorar GetAnalyticsSummary com dados mais reais (unique users, top modules já são reais; scores compostos precisam de fórmulas documentadas) | P2 | 4h | `GetAnalyticsSummary/Handler.cs` |
| 3 | CF-03 | Instrumentar EntityViewed em Catalog e Contracts frontend | P1 | 2h | Módulos Catalog e Contracts frontend |
| 4 | CF-04 | Instrumentar SearchExecuted e ZeroResultSearch no Search/CommandPalette | P1 | 2h | `CommandPalette.tsx`, Search components |
| 5 | CF-05 | Instrumentar QuickActionTriggered no CommandPalette | P2 | 1h | `CommandPalette.tsx` |
| 6 | CF-06 | Instrumentar ContractDraftCreated e ContractPublished | P2 | 1h | Contracts module frontend |
| 7 | CF-07 | Instrumentar ChangeViewed em Change Governance | P2 | 1h | Change Governance frontend |
| 8 | CF-08 | Instrumentar IncidentInvestigated em Operational Intelligence | P2 | 1h | OI frontend |
| 9 | CF-09 | Instrumentar AssistantPromptSubmitted em AI & Knowledge | P2 | 1h | AI frontend |
| 10 | CF-10 | Adicionar rate limiting no POST /events | P2 | 3h | Endpoint + middleware |
| 11 | CF-11 | Adicionar batch event ingestion (POST /events/batch) | P2 | 4h | Novo endpoint |
| 12 | CF-12 | Publicar domain events (AnalyticsEventRecorded) | P2 | 3h | `RecordAnalyticsEvent/Handler.cs` |
| 13 | CF-13 | Publicar audit events para ações administrativas futuras | P2 | 2h | Handlers |
| 14 | CF-14 | Melhorar GetJourneys com definições formais (quando AnalyticsDefinition criada) | P2 | 4h | `GetJourneys/Handler.cs` |
| 15 | CF-15 | Melhorar GetValueMilestones com definições formais | P2 | 4h | `GetValueMilestones/Handler.cs` |
| 16 | CF-16 | Adicionar caching para queries de dashboard | P2 | 3h | Query handlers |
| 17 | CF-17 | Adicionar logs específicos do módulo | P3 | 2h | Todos os handlers |
| 18 | CF-18 | Criar página de configuração de definições no frontend | P2 | 8h | Novo ficheiro |
| 19 | CF-19 | Validar que AnalyticsEventTracker não duplica eventos em SPA navigation | P2 | 2h | `AnalyticsEventTracker.tsx` |
| 20 | CF-20 | Adicionar sub-itens no sidebar para sub-páginas | P3 | 1h | `AppSidebar.tsx` |
| 21 | CF-21 | Criar indicador de health do tracking no Overview | P3 | 3h | Overview page |
| 22 | CF-22 | Criar log viewer de eventos capturados | P3 | 6h | Novo ficheiro |

---

## C. Ajustes Estruturais (Sprints 3-5 — ~80h)

Itens ligados à separação de Governance, persistência `pan_`, uso de ClickHouse, e dependências.

### C.1 — Extração de Governance

| # | ID | Item | Prioridade | Esforço | Ficheiro(s) |
|---|-----|------|-----------|---------|-------------|
| 1 | ST-01 | **Extrair backend de Governance para `src/modules/productanalytics/`** — Domain, Application, Infrastructure, API | P0_BLOCKER | 8h | ~15 ficheiros |
| 2 | ST-02 | Criar `ProductAnalyticsDbContext` herdando `NexTraceDbContextBase` | P0_BLOCKER | 4h | Novo ficheiro |
| 3 | ST-03 | Criar `AnalyticsEventConfiguration` com prefixo `pan_events` | P1 | 2h | Novo ficheiro (mover+renomear) |
| 4 | ST-04 | Remover `AnalyticsEvents` DbSet do `GovernanceDbContext` | P1 | 1h | `GovernanceDbContext.cs` |
| 5 | ST-05 | Remover analytics DI registration do Governance | P1 | 1h | `DependencyInjection.cs` |
| 6 | ST-06 | Registar ProductAnalytics em DI e pipeline | P1 | 2h | Novo `DependencyInjection.cs` |

### C.2 — Persistência pan_

| # | ID | Item | Prioridade | Esforço | Ficheiro(s) |
|---|-----|------|-----------|---------|-------------|
| 7 | ST-07 | Criar `AnalyticsDefinition` entity | P2 | 3h | Novo ficheiro no Domain |
| 8 | ST-08 | Criar `JourneyStep` entity | P2 | 2h | Novo ficheiro no Domain |
| 9 | ST-09 | Criar `ValueMilestone` entity | P2 | 2h | Novo ficheiro no Domain |
| 10 | ST-10 | Criar EF configurations para novas entidades com `pan_` prefix | P2 | 3h | Novos ficheiros |
| 11 | ST-11 | Adicionar campos novos em AnalyticsEvent (EnvironmentId, Duration, ParentEventId, Source) | P2 | 2h | `AnalyticsEvent.cs` |
| 12 | ST-12 | Adicionar check constraints (persona_len, module_len, source_values) | P3 | 1h | Configurations |
| 13 | ST-13 | Adicionar índices compostos (tenant+occurred, tenant+module+occurred) | P2 | 1h | Configuration |

### C.3 — ClickHouse

| # | ID | Item | Prioridade | Esforço | Ficheiro(s) |
|---|-----|------|-----------|---------|-------------|
| 14 | ST-14 | Implementar ClickHouse writer service (PostgreSQL → ClickHouse) | P1 | 8h | Novo serviço |
| 15 | ST-15 | Implementar ClickHouse query adapter para dashboards | P1 | 8h | Novo serviço |
| 16 | ST-16 | Criar schema ClickHouse: `pan_events` table (MergeTree) | P1 | 2h | SQL script |
| 17 | ST-17 | Criar materialized view `pan_daily_module_stats` | P2 | 2h | SQL script |
| 18 | ST-18 | Criar materialized view `pan_daily_persona_stats` | P2 | 2h | SQL script |
| 19 | ST-19 | Criar materialized view `pan_daily_friction_stats` | P2 | 2h | SQL script |

### C.4 — Permissões

| # | ID | Item | Prioridade | Esforço | Ficheiro(s) |
|---|-----|------|-----------|---------|-------------|
| 20 | ST-20 | Renomear permissões de `governance:analytics:*` para `analytics:*` | P1 | 2h | `ProductAnalyticsEndpointModule.cs`, `RolePermissionCatalog.cs` |
| 21 | ST-21 | Adicionar `analytics:read`, `analytics:write` ao `RolePermissionCatalog.cs` | P1 | 1h | `RolePermissionCatalog.cs` |
| 22 | ST-22 | Criar `analytics:export` e `analytics:manage` | P2 | 0.5h | `RolePermissionCatalog.cs` |
| 23 | ST-23 | Definir role assignments para novas permissões | P1 | 1h | `RolePermissionCatalog.cs` |

### C.5 — Interface cross-module

| # | ID | Item | Prioridade | Esforço | Ficheiro(s) |
|---|-----|------|-----------|---------|-------------|
| 24 | ST-24 | Criar `IUserCountProvider` interface para Identity expor count de utilizadores | P2 | 2h | Building blocks / Identity |

---

## D. Pré-condições para Recriar Migrations

O que precisa estar pronto antes de apagar migrations antigas do GovernanceDbContext e gerar migrations no ProductAnalyticsDbContext:

| # | Pré-condição | Item(s) Relacionados | Status |
|---|-------------|---------------------|--------|
| 1 | Backend extraído para `src/modules/productanalytics/` | ST-01 | ❌ |
| 2 | `ProductAnalyticsDbContext` criado com `pan_` prefix | ST-02, ST-03 | ❌ |
| 3 | `GovernanceDbContext` limpo de AnalyticsEvents | ST-04 | ❌ |
| 4 | Novas entidades criadas (Definition, JourneyStep, Milestone) | ST-07, ST-08, ST-09 | ❌ |
| 5 | EF configurations completas com pan_ prefix | ST-10, ST-13 | ❌ |
| 6 | Campos novos adicionados ao AnalyticsEvent | ST-11 | ❌ |
| 7 | Check constraints definidos | ST-12 | ❌ |
| 8 | RowVersion (xmin) configurado para definitions | ST-10 | ❌ |
| 9 | Modelo de domínio final validado | PARTE 4 (este doc) | ✅ |
| 10 | Persistência final validada | PARTE 5 (este doc) | ✅ |

**Regra**: NÃO gerar migrations até TODOS os pré-condições estarem ✅.

---

## E. Critérios de Aceite do Módulo

O que precisa estar resolvido para Product Analytics ser considerado fechado:

| # | Critério | Status Atual | Alvo |
|---|---------|-------------|------|
| 1 | Backend em módulo próprio (`src/modules/productanalytics/`) | ❌ Dentro de Governance | ✅ Extraído |
| 2 | ProductAnalyticsDbContext com `pan_` prefix | ❌ Usa GovernanceDbContext | ✅ Próprio |
| 3 | Zero mock data em handlers | ❌ GetPersonaUsage mock | ✅ Eliminado |
| 4 | ≥10 tipos de evento instrumentados | ❌ 1 de 25 (4%) | ✅ ≥10 (40%) |
| 5 | ClickHouse integration funcional | ❌ Não implementado | ✅ Writer + query adapter |
| 6 | Permissões próprias (`analytics:*`) | ❌ `governance:analytics:*` | ✅ Renomeadas |
| 7 | Dashboards com dados reais | ⚠️ Misto | ✅ ≥80% dados reais |
| 8 | Documentação mínima (4 docs) | ❌ 0 docs | ✅ ≥4 docs |
| 9 | Audit events publicados | ❌ Zero | ✅ Para ações admin |
| 10 | Rate limiting no POST /events | ❌ Não existe | ✅ Implementado |
| 11 | Fronteira Governance clara | ⚠️ Ambígua | ✅ Documentada e implementada |
| 12 | Testes ≥40% | ❌ ~15% | ✅ ≥40% |

---

## Timeline estimada

| Sprint | Foco | Itens | Esforço |
|--------|------|-------|---------|
| Sprint 1 | Quick wins | QW-01 a QW-09 | ~12h |
| Sprint 2 | Extração de Governance + mock elimination | ST-01 a ST-06, CF-01 | ~22h |
| Sprint 3 | Instrumentação de eventos + melhorias backend | CF-03 a CF-09, CF-10 a CF-13 | ~21h |
| Sprint 4 | Persistência pan_ + novas entidades | ST-07 a ST-13 | ~14h |
| Sprint 5 | ClickHouse integration | ST-14 a ST-19 | ~24h |
| Sprint 6 | Permissões + frontend melhorias | ST-20 a ST-24, CF-14 a CF-22 | ~35h |
| Sprint 7 | Documentação + testes + polish | Documentação, testes | ~17h + testing |

**Total**: ~195h across 7 sprints

---

## Riscos identificados

| # | Risco | Impacto | Mitigação |
|---|-------|---------|-----------|
| 1 | ClickHouse não disponível em ambiente de dev | Alto | Implementar fallback para PostgreSQL |
| 2 | Instrumentação em todos os módulos é disruptiva | Médio | Instrumentar progressivamente, começar pelos mais usados |
| 3 | Extração de Governance pode quebrar migrations existentes | Alto | Não apagar migrations existentes, criar novas |
| 4 | Volume de eventos pode crescer rapidamente | Médio | Rate limiting + batch ingestion + ClickHouse |
| 5 | Fórmulas de scores compostos podem ser contestadas | Baixo | Documentar fórmulas, tornar configuráveis |
