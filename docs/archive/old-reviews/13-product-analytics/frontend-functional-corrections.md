# PARTE 8 — Frontend Functional Corrections

> **Data**: 2026-03-25
> **Prompt**: N12 — Consolidação do módulo Product Analytics
> **Estado**: BACKLOG DE CORREÇÕES

---

## 1. Páginas do módulo

| # | Página | Ficheiro | LOC | Status |
|---|--------|---------|-----|--------|
| 1 | Overview Dashboard | `pages/ProductAnalyticsOverviewPage.tsx` | 199 | ✅ Funcional |
| 2 | Module Adoption | `pages/ModuleAdoptionPage.tsx` | 167 | ✅ Funcional |
| 3 | Persona Usage | `pages/PersonaUsagePage.tsx` | 190 | ⚠️ Dados mock |
| 4 | Journey Funnel | `pages/JourneyFunnelPage.tsx` | 179 | ⚠️ Dados limitados |
| 5 | Value Tracking | `pages/ValueTrackingPage.tsx` | 173 | ⚠️ Dados limitados |

**Nota**: Existe também `ProductAnalyticsOverviewPage.tsx` na raiz do feature (ficheiro vazio — possível duplicado).

### Ficheiros auxiliares

| Ficheiro | LOC | Papel |
|---------|-----|-------|
| `AnalyticsEventTracker.tsx` | 68 | Client-side event capture |
| `api/productAnalyticsApi.ts` | 50 | API client (7 funções) |

**Total frontend**: 908 LOC em 7 ficheiros

---

## 2. Revisão de rotas

| Rota | Página | Permissão | Status |
|------|--------|-----------|--------|
| `/analytics` | ProductAnalyticsOverviewPage | `analytics:read` | ✅ Definida em App.tsx |
| `/analytics/adoption` | ModuleAdoptionPage | `analytics:read` | ✅ Definida |
| `/analytics/personas` | PersonaUsagePage | `analytics:read` | ✅ Definida |
| `/analytics/journeys` | JourneyFunnelPage | `analytics:read` | ✅ Definida |
| `/analytics/value` | ValueTrackingPage | `analytics:read` | ✅ Definida |

**Avaliação**: Rotas bem organizadas, hierarquia clara sob `/analytics`.

---

## 3. Revisão de menu

| Localização | Label Key | Rota | Ícone | Permissão | Status |
|-------------|-----------|------|-------|-----------|--------|
| Sidebar | `sidebar.productAnalytics` | `/analytics` | BarChart3 | `analytics:read` | ✅ Presente |
| Command Palette | `sidebar.productAnalytics` | `/analytics` | BarChart3 | `analytics:read` | ✅ Presente |

**Lacunas identificadas**:
- ❌ Sem sub-itens no sidebar (sub-páginas apenas acessíveis via links internos)
- ❌ Sem breadcrumbs dedicados (se existir sistema de breadcrumbs)

---

## 4. Revisão de formulários

| Formulário | Página | Status |
|-----------|--------|--------|
| Filtro por persona | Overview, Adoption, Personas | ✅ Dropdowns funcionais |
| Filtro por módulo | Overview, Friction | ✅ Dropdowns funcionais |
| Filtro por range temporal | Todas as páginas | ✅ Seletor de período |
| Filtro por equipa | Overview, Adoption | ✅ Campo teamId |
| Pesquisa de módulos | ModuleAdoptionPage | ✅ Search input |

**Avaliação**: Formulários são filtros de consulta. Não há formulários de criação/edição (adequado para um módulo analítico read-heavy).

---

## 5. Revisão de telas de configuração

| Aspecto | Status | Detalhe |
|---------|--------|---------|
| Configuração de métricas | ❌ Não existe | Sem CRUD de AnalyticsDefinition |
| Configuração de journeys | ❌ Não existe | Sem CRUD de JourneyStep |
| Configuração de milestones | ❌ Não existe | Sem CRUD de ValueMilestone |
| Configuração de eventos custom | ❌ Não existe | Sem gestão de eventos |

---

## 6. Revisão de telas de status/health/histórico

| Aspecto | Status | Detalhe |
|---------|--------|---------|
| Histórico de eventos | ❌ Não existe | Sem log viewer de eventos capturados |
| Health do tracking | ❌ Não existe | Sem indicação se eventos estão a ser capturados |
| Status de ingestão | ❌ Não existe | Sem dashboard de volume de ingestão |
| Diagnóstico de ClickHouse | ❌ N/A | ClickHouse não implementado |

---

## 7. Revisão de integração com API real

| Página | API | Dados Reais | Status |
|--------|-----|-------------|--------|
| Overview | `getSummary()` | Parcialmente | ⚠️ Scores calculados com dados mistos |
| Adoption | `getModuleAdoption()` | Parcialmente | ⚠️ Repository real mas cálculos simplificados |
| Personas | `getPersonaUsage()` | **Não** | 🔴 Mock data no backend |
| Journeys | `getJourneys()` | Limitado | ⚠️ Dados insuficientes |
| Value | `getValueMilestones()` | Limitado | ⚠️ Dados insuficientes |

**Nota**: O `AnalyticsEventTracker.tsx` envia eventos reais via `recordEvent()`, mas apenas para `ModuleViewed`.

---

## 8. Revisão de i18n

### Keys existentes (3 idiomas: pt-PT, es, en)

| Categoria | Keys | Status |
|-----------|------|--------|
| Títulos principais | `analytics.title`, `analytics.subtitle` | ✅ Completo |
| Scores | `analytics.adoptionScore`, `analytics.valueScore`, `analytics.frictionScore` | ✅ Completo |
| Métricas | `analytics.uniqueUsers`, `analytics.timeToFirstValue`, `analytics.timeToCoreValue` | ✅ Completo |
| Unidades | `analytics.minutes` | ✅ Completo |
| Labels | `analytics.topModules`, `analytics.actions`, `analytics.users` | ✅ Completo |
| Navegação | `analytics.viewModuleAdoption`, `analytics.viewPersonaUsage`, etc. | ✅ Completo |
| Adoption page | `analytics.adoption.title`, `analytics.adoption.subtitle`, etc. | ✅ Completo |
| Trends | `analytics.trend.Improving`, `analytics.trend.Stable`, `analytics.trend.Declining` | ✅ Completo |

### Keys ausentes

| Key necessária | Contexto |
|---------------|---------|
| `analytics.personas.title` | Título da página de personas |
| `analytics.personas.subtitle` | Subtítulo da página de personas |
| `analytics.journeys.title` | Título da página de journeys |
| `analytics.journeys.subtitle` | Subtítulo |
| `analytics.value.title` | Título da página de value tracking |
| `analytics.value.subtitle` | Subtítulo |
| `analytics.noData` | Estado vazio genérico |
| `analytics.loading` | Loading state |
| `analytics.error` | Error state |
| `analytics.friction.title` | Título de friction (se criar página dedicada) |
| `analytics.export` | Label de exportação |
| `analytics.dateRange` | Label de filtro temporal |

---

## 9. Revisão de botões sem ação

| Botão/Ação | Página | Status |
|-----------|--------|--------|
| Links "View..." no Overview | Overview | ✅ Navegam para sub-páginas |
| Search input | Adoption | ✅ Filtra lista |
| Filtros | Todas | ✅ Enviam params à API |

**Avaliação**: Sem botões sem ação identificados. As páginas são read-only com filtros.

---

## 10. Revisão de placeholders

| Placeholder | Página | Status |
|------------|--------|--------|
| "Search modules..." | Adoption | ✅ Placeholder adequado |
| Stat cards com 0 | Overview | ⚠️ Mostra 0% quando sem dados (deveria mostrar empty state) |
| Listas vazias | Adoption, Personas | ⚠️ Sem empty state message (mostra lista vazia) |

---

## 11. Revisão de exposição de campos técnicos

| Campo | Página | Status |
|-------|--------|--------|
| UUIDs | Nenhuma | ✅ Não expõe IDs técnicos |
| Event types numéricos | Nenhuma | ✅ Usa labels |
| JSON metadata | Nenhuma | ✅ Não expõe |
| Tenant IDs | Nenhuma | ✅ Não expõe |

**Avaliação**: Frontend é limpo, sem exposição de campos técnicos.

---

## 12. Backlog de correções frontend

| # | ID | Correção | Prioridade | Esforço | Ficheiro(s) |
|---|-----|---------|-----------|---------|-------------|
| 1 | F-01 | Remover `ProductAnalyticsOverviewPage.tsx` vazio da raiz do feature | P1_CRITICAL | 0.5h | `ProductAnalyticsOverviewPage.tsx` (raiz) |
| 2 | F-02 | Adicionar empty states quando dados são 0 ou vazios | P2_HIGH | 2h | Todas as páginas |
| 3 | F-03 | Adicionar loading states consistentes | P2_HIGH | 1h | Todas as páginas |
| 4 | F-04 | Adicionar error states com retry | P2_HIGH | 2h | Todas as páginas |
| 5 | F-05 | Adicionar keys i18n ausentes (12+ keys) | P2_HIGH | 1h | `pt-PT.json`, `es.json`, `en.json` |
| 6 | F-06 | Adicionar aviso visual quando dados são mock/parciais | P1_CRITICAL | 2h | PersonaUsagePage, JourneyFunnelPage |
| 7 | F-07 | Adicionar sub-itens no sidebar para sub-páginas | P3_MEDIUM | 1h | `AppSidebar.tsx` |
| 8 | F-08 | Criar página de configuração de definições | P2_HIGH | 8h | Novo ficheiro |
| 9 | F-09 | Criar página/secção de event log viewer | P3_MEDIUM | 6h | Novo ficheiro |
| 10 | F-10 | Criar indicador de health do tracking | P3_MEDIUM | 3h | Overview page |
| 11 | F-11 | Instrumentar mais tipos de evento no AnalyticsEventTracker | P1_CRITICAL | 4h | `AnalyticsEventTracker.tsx` |
| 12 | F-12 | Adicionar exportação de dados (CSV/PDF) | P3_MEDIUM | 4h | Todas as páginas |
| 13 | F-13 | Corrigir mapeamento de módulo no AnalyticsEventTracker (`/analytics` → Analytics, não Governance) | P2_HIGH | 0.5h | `AnalyticsEventTracker.tsx` |

**Total frontend**: 13 itens, ~35h estimadas
