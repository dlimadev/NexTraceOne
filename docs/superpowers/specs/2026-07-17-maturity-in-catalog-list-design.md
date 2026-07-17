# Maturidade na consulta do Catálogo de Serviços (menu→consulta) — Design

**Data:** 2026-07-17
**Contexto:** Ciclo 40. Segundo passo da iniciativa "menu→consulta". Sucede o ciclo 39 (Feature Flags no detalhe).

---

## Motivação

O "Score & Maturidade" é hoje um item de menu que abre um dashboard de portefólio. A maturidade de um serviço deve ser visível **na consulta do catálogo** (a lista de serviços), não só numa página separada. O per-serviço já está no detalhe (tab Score).

**Restrição honesta (verificada):** a lista `GET /catalog/services` (`ServiceListItem`) **não traz maturidade** e é **paginada no servidor**; um sort/filter *fiável* por maturidade precisaria de backend (adicionar `maturity` ao item + parâmetro de sort). **Fora de âmbito** — decisão do utilizador (opção A). Este ciclo entrega a maturidade **visível como coluna** (join client-side com o dashboard, que devolve todos os serviços), sem prometer sort/filter server-side.

---

## Fonte de dados (verificado)

`serviceCatalogApi.getMaturityDashboard(params?: { teamName?, domain? })` → `MaturityDashboardResponse { summary, services: ServiceMaturityItemDto[], computedAt }`.
`ServiceMaturityItemDto` tem `serviceId`, `level` ('Initial'|'Developing'|'Defined'|'Managed'|'Optimizing'), `overallScore` (0–1), entre outros. Devolve **todos** os serviços (não paginado) → permite um `Map<serviceId, { level, overallScore }>` para enriquecer a página visível da lista.

Nível → label i18n já existe: `serviceMaturity.level.${level}`. Variante de badge: `maturityBadgeVariant(level)` (Optimizing/Managed→success, Defined→info, Developing→warning, Initial→danger/default) — replicado inline na lista (helper trivial; ServiceMaturityPage mantém o seu, não tocado).

---

## Arquitetura

### 1. Coluna "Maturidade" na `ServiceCatalogListPage`

- Nova query `useQuery(['catalog-maturity-dashboard'], () => serviceCatalogApi.getMaturityDashboard())` (sem params — dashboard completo). Erro/loading não bloqueiam a lista (honest-null: sem dados → coluna mostra "—").
- `const maturityById = new Map(dashboard?.services.map(s => [s.serviceId, s]) ?? [])`.
- Nova coluna **"Maturidade"** (novo `<th>` antes da coluna de ações/última, e `<td>` por linha): badge com `t(\`serviceMaturity.level.${item.level}\`)` (variante via helper) + o score como `Math.round(overallScore*100)`; "—" quando o serviço não está no mapa.
- Helper `maturityBadgeVariant(level: string)` definido inline no ficheiro (6 linhas, igual ao da ServiceMaturityPage — duplicação trivial intencional para manter a mudança contida).

### 2. Deep-link ao dashboard

- No cabeçalho da lista (junto ao título do Card da tabela), um link "Ver dashboard de maturidade →" → `/services/maturity` (`catalog.maturity.viewDashboard`). Mantém o dashboard **e a aba Ownership Audit** acessíveis após remover o item de menu (padrão ciclo 39).

### 3. Remover do menu

- Remover o item `sidebar.scoreMaturity` (`/services/maturity`) de `navItems` em `AppSidebar.tsx` (linha ~61). **Não** remover o import `Award` — usado por outros 4 itens (operations/intelligence/compliance). Rota + página `ServiceMaturityPage` mantêm-se, reachable pelo deep-link.

---

## i18n (4 locales: en, es, pt-BR, pt-PT)

Reusar `serviceMaturity.level.*` e badges existentes. Chaves novas:
- `catalog.columns.maturity` — EN "Maturity" · ES "Madurez" · pt-BR "Maturidade" · pt-PT "Maturidade"
- `catalog.maturity.viewDashboard` — EN "View maturity dashboard" · ES "Ver panel de madurez" · pt-BR "Ver painel de maturidade" · pt-PT "Ver painel de maturidade"

---

## Testes (Vitest, só em `src/__tests__/**`)

- **`ServiceCatalogListPage`:** com `listServices` e `getMaturityDashboard` mockados (uma maturidade para um serviço, nenhuma para outro) → a coluna mostra o badge de nível para o primeiro e "—" para o segundo; o deep-link "Ver dashboard de maturidade" existe (`href="/services/maturity"`). Reutilizar o scaffold de mocks de um teste existente da página, se houver.
- **Sidebar (`navItems`):** o item `/services/maturity` já **não** aparece; `/services` mantém-se.
- Gates: `tsc`, `eslint`, `build`, suíte completa, `validate:i18n`. Verificação no stub.

---

## Fora de âmbito

- Sort/filter server-side por maturidade (opção B — backend: `maturity` no `ServiceListItem` + param de sort/filter em `/catalog/services`). Follow-up.
- Apagar/redesenhar a `ServiceMaturityPage` (mantém-se como dashboard + audit, deep-linkada).
- Dedupe do helper `maturityBadgeVariant` (fica um por página — follow-up de baixo valor).
