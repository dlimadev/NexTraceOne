# Inventário de Placeholders e UI Cosmética — NexTraceOne

> Prompt N16 — Parte 4 | Data: 2026-03-25 | Fase: Encerramento da Trilha N

---

## 1. Resumo

Este relatório inventaria telas, botões, componentes e secções que aparentam funcionalidade sem a entregar plenamente. Foca em UI que pode enganar o utilizador sobre capacidades reais do produto.

**Total de itens relevantes encontrados: 12**
- 🔴 IMPLEMENT_NOW: 2
- 🟠 HIDE_UNTIL_REAL: 4
- 🟡 REMOVE: 1
- ⚪ OUT_OF_SCOPE: 5

---

## 2. Páginas com Backend Parcial ou Simulado

### PH-01 — Product Analytics Overview (Mock Data)

| Campo | Valor |
|---|---|
| **Página** | `src/frontend/src/features/product-analytics/pages/ProductAnalyticsOverviewPage.tsx` |
| **Rota** | `/analytics` |
| **Problema** | Backend (`GetPersonaUsage`) retorna dados mock. Apenas 1/25 tipos de evento está instrumentado (ModuleViewed) |
| **Backend** | Dentro de GovernanceDbContext (OI-03) — não extraído |
| **Módulo** | Product Analytics |
| **Classificação** | 🔴 **IMPLEMENT_NOW** — dashboard principal de analytics mostra dados não confiáveis |

### PH-02 — Persona Usage Page (Mock Data)

| Campo | Valor |
|---|---|
| **Página** | `src/frontend/src/features/product-analytics/pages/PersonaUsagePage.tsx` |
| **Rota** | `/analytics/personas` |
| **Problema** | Consome mesmo endpoint com dados mock do GetPersonaUsage |
| **Módulo** | Product Analytics |
| **Classificação** | 🔴 **IMPLEMENT_NOW** — dados de persona usage são críticos para o produto |

### PH-03 — AI Assistant Page ("Coming Soon")

| Campo | Valor |
|---|---|
| **Página** | `src/frontend/src/features/ai-hub/pages/AiAssistantPage.tsx` |
| **Rota** | `/ai/assistant` |
| **Problema** | Título vazio diz "AI Assistant coming soon". Backend tem mock response generator. Streaming não implementado. Tools não executam |
| **Módulo** | AI & Knowledge |
| **Classificação** | 🟠 **HIDE_UNTIL_REAL** — menu item deve ser condicional até funcionalidade real existir |

### PH-04 — AI Analysis Page

| Campo | Valor |
|---|---|
| **Página** | `src/frontend/src/features/ai-hub/pages/AiAnalysisPage.tsx` |
| **Rota** | `/ai/analysis` |
| **Problema** | Grounding message explicitamente diz "under active development" |
| **Módulo** | AI & Knowledge |
| **Classificação** | 🟠 **HIDE_UNTIL_REAL** — análise AI depende de retrieval e grounding funcionais |

### PH-05 — Automation Workflows (Dados Parcialmente Simulados)

| Campo | Valor |
|---|---|
| **Página** | `src/frontend/src/features/operations/pages/AutomationWorkflowsPage.tsx` |
| **Rota** | `/operations/automation` |
| **Problema** | Audit trail retorna dados simulados (GenerateSimulatedEntries). Catálogo de ações é hardcoded |
| **Módulo** | Operational Intelligence |
| **Classificação** | 🟠 **HIDE_UNTIL_REAL** — ou substituir dados simulados antes de expor a funcionalidade |

---

## 3. Componentes Cosméticos

### PH-06 — DemoBanner (Componente Não Utilizado)

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/frontend/src/components/DemoBanner.tsx` |
| **Problema** | Componente para exibir banner de dados demonstrativos. Está implementado mas **não é usado em nenhuma página** |
| **Classificação** | ⚪ **OUT_OF_SCOPE** — componente infra pronto para uso futuro; não engana o utilizador |

### PH-07 — EmptyState Component

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/frontend/src/components/EmptyState.tsx` |
| **Problema** | Nenhum — componente utilizado corretamente para estados vazios |
| **Classificação** | ⚪ **OUT_OF_SCOPE** — comportamento correto |

---

## 4. Navegação e Menus

### PH-08 — Sidebar Licensing/Vendor Routes

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/frontend/src/components/Breadcrumbs.tsx` (line 34), `src/frontend/src/utils/navigation.ts` (line 48) |
| **Problema** | Breadcrumbs mapeiam `'licensing'` → `'sidebar.licensing'` e `'vendor'` → `'sidebar.vendorLicensing'`, mas essas chaves i18n **não existem** nos ficheiros de locale |
| **Módulo** | Identity & Access (Licensing — fora do escopo) |
| **Classificação** | 🟡 **REMOVE** — referências a Licensing devem ser removidas |

---

## 5. Dashboards com Dados Parcialmente Reais

### PH-09 — FinOps Dashboards (Parcialmente Hardcoded)

| Campo | Valor |
|---|---|
| **Páginas** | `ServiceFinOpsPage.tsx`, `TeamFinOpsPage.tsx`, `DomainFinOpsPage.tsx`, `FinOpsPage.tsx`, `ExecutiveFinOpsPage.tsx` |
| **Rotas** | `/governance/finops/*` |
| **Problema** | Dados de custo são reais (CostIntelligence), mas waste signals, efficiency e optimizations são hardcoded como 0/empty |
| **Módulo** | Governance |
| **Classificação** | ⚪ **OUT_OF_SCOPE** — dados parciais com IsSimulated flag é padrão aceite na fase atual |

### PH-10 — Governance Executive Overview (Real Data)

| Campo | Valor |
|---|---|
| **Página** | `src/frontend/src/features/governance/pages/ExecutiveOverviewPage.tsx` |
| **Rota** | `/governance/executive` |
| **Problema** | Usa `useQuery` para dados reais. IsSimulated está no schema mas marcado como `false` |
| **Classificação** | ⚪ **OUT_OF_SCOPE** — funciona com dados reais |

---

## 6. Páginas sem Sidebar Entry

### PH-11 — Environments Page (Sem Entrada no Menu)

| Campo | Valor |
|---|---|
| **Página** | `src/frontend/src/features/identity-access/pages/EnvironmentsPage.tsx` |
| **Rota** | `/environments` |
| **Problema** | Rota existe e página funciona com backend, mas **não há entrada no sidebar** para a navegar. Utilizador só acessa via URL direto |
| **Módulo** | Environment Management (OI-04) |
| **Classificação** | 🟠 **HIDE_UNTIL_REAL** — adicionar ao sidebar quando módulo for extraído, ou documentar como acesso admin |

---

## 7. "Coming Soon" Strings em i18n

### PH-12 — i18n Strings Placeholder

| Campo | Valor |
|---|---|
| **Ficheiro** | `src/frontend/src/locales/en.json` |
| **Strings** | `"comingSoon": "Coming soon"`, `"assistantEmptyTitle": "AI Assistant coming soon"`, `"widgetComingSoon": "Data will be available when connected to backend services"` |
| **Classificação** | ⚪ **OUT_OF_SCOPE** — strings genéricas de i18n para estados vazios; comportamento defensivo aceitável |

---

## 8. Resumo por Módulo

| Módulo | Placeholders | Impacto |
|---|---|---|
| Product Analytics | PH-01, PH-02 | 🔴 Alto — dashboards com dados mock |
| AI & Knowledge | PH-03, PH-04 | 🟠 Médio — funcionalidades "coming soon" |
| Operational Intelligence | PH-05 | 🟠 Médio — automação com dados simulados |
| Environment Management | PH-11 | 🟠 Baixo — página sem sidebar entry |
| Governance (FinOps) | PH-09 | ⚪ Aceitável — dados parciais com flag |
| Licensing (fora do escopo) | PH-08 | 🟡 Baixo — remover referências |

---

## 9. Backlog de Ações

| ID | Ação | Prioridade | Estimativa |
|---|---|---|---|
| PH-01/02 | Implementar backend real de Product Analytics (requer extração OI-03) | P1_CRITICAL | 40h |
| PH-03 | Condicionar menu AI Assistant a feature flag real | P2_HIGH | 2h |
| PH-04 | Condicionar menu AI Analysis a feature flag real | P2_HIGH | 2h |
| PH-05 | Substituir dados simulados de Automation por dados reais | P2_HIGH | 12h |
| PH-08 | Remover referências de Licensing em Breadcrumbs e navigation | P2_HIGH | 1h |
| PH-11 | Adicionar Environments ao sidebar com permissão `env:*` | P3_MEDIUM | 2h |
