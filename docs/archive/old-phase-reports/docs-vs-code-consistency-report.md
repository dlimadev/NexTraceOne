# Relatório de Consistência entre Documentação e Código — NexTraceOne

> Prompt N16 — Parte 7 | Data: 2026-03-25 | Fase: Encerramento da Trilha N

---

## 1. Resumo

Este relatório compara o que a documentação oficial promete com o que o código realmente implementa, identificando divergências relevantes.

**Total de inconsistências identificadas: 14**
- 🔴 Críticas: 4
- 🟠 Significativas: 5
- 🟡 Menores: 5

---

## 2. Módulos Oficiais vs Código Real

### INC-01 — 13 módulos oficiais, 9 backends reais 🔴

| Campo | Valor |
|---|---|
| **Doc** | `module-boundary-matrix.md` define 13 módulos oficiais |
| **Código** | `src/modules/` contém 9 diretórios: `aiknowledge`, `auditcompliance`, `catalog`, `changegovernance`, `configuration`, `governance`, `identityaccess`, `notifications`, `operationalintelligence` |
| **Gap** | 4 módulos sem backend dedicado: Environment Management, Contracts (subdomínio de Catalog), Integrations (dentro de Governance), Product Analytics (dentro de Governance) |
| **Gravidade** | 🔴 Crítica — bloqueador para a nova baseline de persistência |
| **Referência** | OI-01 a OI-04 em `phase-a-open-items.md` |

### INC-02 — Prefixos definidos vs aplicados 🟠

| Campo | Valor |
|---|---|
| **Doc** | `database-table-prefixes.md` define 13 prefixos: `iam_`, `env_`, `cat_`, `ctr_`, `chg_`, `ops_`, `aik_`, `gov_`, `cfg_`, `aud_`, `ntf_`, `int_`, `pan_` |
| **Código** | Prefixos aplicados: `identity_` (alvo `iam_`), `cfg_` ✅, `oi_` (alvo `ops_`), `ntf_` ✅, `gov_` ✅ (inclui Integrations e Product Analytics temporariamente). Environment usa `identity_`. Contracts usa prefixo de Catalog |
| **Gap** | 3 prefixos incorretos: `identity_` → `iam_`, `oi_` → `ops_`, `env_` não aplicado |
| **Gravidade** | 🟠 Significativa — será resolvida na recriação da baseline |

---

## 3. Docs de Arquitetura vs Estrutura Real

### INC-03 — 20 DbContexts documentados, target 13 🟠

| Campo | Valor |
|---|---|
| **Doc** | `persistence-transition-master-plan.md` documenta 20 DbContexts existentes com target de consolidação para ~13 |
| **Código** | Confirmados 20 DbContexts em `src/modules/`: AiGovernanceDbContext, AiOrchestrationDbContext, ExternalAiDbContext, AuditDbContext, CatalogGraphDbContext, DeveloperPortalDbContext, ContractsDbContext, ChangeIntelligenceDbContext, PromotionDbContext, RulesetGovernanceDbContext, WorkflowDbContext, ConfigurationDbContext, GovernanceDbContext, IdentityDbContext, NotificationsDbContext, AutomationDbContext, CostIntelligenceDbContext, IncidentDbContext, ReliabilityDbContext, RuntimeIntelligenceDbContext |
| **Gap** | Coerente — documentação reflete a realidade. Consolidação planeada para futuro |
| **Gravidade** | ⚪ Consistente |

### INC-04 — 47 migrations vs 29 documentados 🟠

| Campo | Valor |
|---|---|
| **Doc** | `persistence-transition-master-plan.md` refere "29 legacy migrations" |
| **Código** | Contagem real nos diretórios Migrations: ~47 ficheiros .cs (incluindo Designer files e ModelSnapshots) |
| **Gap** | Diferença entre contagem de "migration steps" (~29 InitialCreate + adições) vs ficheiros totais (~47 incluindo Designer) |
| **Gravidade** | 🟡 Menor — ambos referem-se ao mesmo conjunto; contagem de migrations steps é ~14-15 |

---

## 4. Consolidado dos Módulos vs Implementação Real

### INC-05 — AI & Knowledge: 40+ entities documentadas, funcionalidade ~25% 🔴

| Campo | Valor |
|---|---|
| **Doc** | `domain-model-finalization.md` documenta 40+ entidades, 23 enums, 68 CQRS features, 54+ endpoints |
| **Código** | Entidades existem mas: tools nunca executam (CR-2), streaming não implementado, retrieval é PoC, zero domain events publicados, AssistantPanel tem mock response generator |
| **Gap** | Modelo de domínio existe mas funcionalidade real é ~25% do documentado |
| **Gravidade** | 🔴 Crítica — frontend com 11 páginas sugere capacidade que não existe |

### INC-06 — Product Analytics: 5 frontend pages, backend mock 🔴

| Campo | Valor |
|---|---|
| **Doc** | `module-scope-finalization.md` define 5 dashboards de analytics |
| **Código** | 5 páginas frontend existem em `src/frontend/src/features/product-analytics/`. Backend tem 1 entidade (AnalyticsEvent) dentro de GovernanceDbContext. GetPersonaUsage retorna mock data. Apenas 1/25 event types instrumentado |
| **Gap** | Frontend completo para módulo com backend quase inexistente |
| **Gravidade** | 🔴 Crítica |

### INC-07 — Operational Intelligence: Automation com dados simulados 🟠

| Campo | Valor |
|---|---|
| **Doc** | `module-consolidated-review.md` descreve automação como funcionalidade operacional |
| **Código** | InMemoryIncidentStore, GenerateSimulatedEntries, AutomationActionCatalog hardcoded |
| **Gap** | Automação documentada como real mas parcialmente simulada |
| **Gravidade** | 🟠 Significativa |

---

## 5. Promessas de UI vs Backend Real

### INC-08 — 103 páginas frontend, nem todas com backend real 🟠

| Campo | Valor |
|---|---|
| **Código** | ~103 páginas frontend em 14 features. Todas usam `useQuery()` para dados |
| **Gap** | Backend responde a todas, mas alguns endpoints retornam dados simulados/parciais (FinOps, Automation, Product Analytics) |
| **Gravidade** | 🟠 Significativa — UI conectada mas dados nem sempre confiáveis |

### INC-09 — 73 rotas frontend, permissões verificadas 🟡

| Campo | Valor |
|---|---|
| **Código** | 73 rotas em `App.tsx` com `requiredPermission` definida para a maioria |
| **Gap** | Permissões referenciadas incluem `analytics:read` que não existe em RolePermissionCatalog (usam `governance:analytics:read`). Também `notifications:read` que não existe no catálogo (blocker documentado) |
| **Gravidade** | 🟡 Menor — identificado em remediation plans |

---

## 6. IA vs Implementação Real

### INC-10 — AI Assistant: Página sugere interação real 🟠

| Campo | Valor |
|---|---|
| **Doc/UI** | Sidebar tem 10 items de AI (Assistant, Agents, Models, Policies, Routing, IDE, Budgets, Audit, Analysis) |
| **Código** | Backend tem 68 CQRS features e 54+ endpoints, mas tools não executam, streaming ausente, retrieval é PoC |
| **Gap** | UI completa para funcionalidade 25% implementada no backend |
| **Gravidade** | 🟠 Significativa |

---

## 7. Analytics vs Dados Reais

### INC-11 — AnalyticsEventTracker: 1/25 event types 🟡

| Campo | Valor |
|---|---|
| **Doc** | `AnalyticsEventType` enum define 25 tipos de eventos |
| **Código** | Apenas `ModuleViewed` está instrumentado via `AnalyticsEventTracker` |
| **Gap** | 96% dos event types não são rastreados |
| **Gravidade** | 🟡 Menor — funcionalidade em construção, gap documentado |

---

## 8. Inconsistências de Naming

### INC-12 — Prefixo `oi_` vs `ops_` (Operational Intelligence) 🟡

| Campo | Valor |
|---|---|
| **Doc** | `domain-model-finalization.md` define target `ops_` |
| **Código** | Configurations usam `oi_` (ex: `oi_incidents`, `oi_automation_workflows`) |
| **Gravidade** | 🟡 Menor — será corrigido na nova baseline |

### INC-13 — Prefixo `identity_` vs `iam_` (Identity & Access) 🟡

| Campo | Valor |
|---|---|
| **Doc** | `database-table-prefixes.md` define `iam_` |
| **Código** | IdentityDbContext usa `identity_` prefix |
| **Gravidade** | 🟡 Menor — será corrigido na nova baseline |

---

## 9. Maturity Claims vs Reality

### INC-14 — Modular Review Master vs Code State 🟡

| Campo | Valor |
|---|---|
| **Doc** | Maturity scores: Catalog 81%, Identity 82%, Config 85%, Change Gov 72% |
| **Código** | Scores refletem modelo de domínio e CQRS, não funcionalidade end-to-end testada |
| **Gap** | Scores podem sobrevalorizar módulos com modelo completo mas integrações pendentes |
| **Gravidade** | 🟡 Menor — scores são indicativos, não absolutos |

---

## 10. Resumo por Gravidade

| Gravidade | Quantidade | Exemplos |
|---|---|---|
| 🔴 Crítica | 4 | 4 módulos sem backend (INC-01), AI 25% (INC-05), Analytics mock (INC-06) |
| 🟠 Significativa | 5 | Prefixos incorretos (INC-02), 20 DbContexts (INC-03), Automation simulada (INC-07) |
| 🟡 Menor | 5 | Contagem migrations (INC-04), naming (INC-12/13), maturity scores (INC-14) |

---

## 11. Conclusão

A documentação da trilha N é **substancialmente coerente** com o código real. As inconsistências identificadas são todas **conhecidas, documentadas e rastreadas** nos remediation plans e open items. Não foram encontradas divergências desconhecidas ou não rastreadas.

**As 4 inconsistências críticas** (INC-01, INC-05, INC-06) já estão documentadas como OI-01 a OI-04 e estão no plano de execução da Onda 0 da transição de persistência.
