# Auditoria Enterprise do Frontend — NexTraceOne

> **Auditor:** Principal Frontend Architect / Enterprise UX Auditor
> **Data:** 2026-03-26
> **Stack auditada:** React 19 · TypeScript 5.9 · Vite 7 · Tailwind CSS 4 · React Router 7 · TanStack Query 5 · Lucide React
> **Escopo:** 101 páginas · 64 componentes base · 16 módulos de feature · 1 design system

---

## VEREDITO FINAL

### `ENTERPRISE_FOUNDATION_WITH_WEAK_EXECUTION`

O NexTraceOne possui uma **base enterprise genuína e sólida**: design tokens bem definidos, sistema de layering dark consistente, componentes base com boa estrutura, navegação persona-aware, RBAC implementado, i18n em 4 idiomas, arquitetura modular por feature e práticas de segurança corretas (token storage, CSRF, sanitização).

Contudo, a **execução está comprometida por inconsistências sistemáticas**: múltiplas páginas contornam o design system com cores Tailwind hardcoded em vez de usar os tokens definidos; componentes estratégicos que deveriam existir (EntityHeader, TimelinePanel, DiffViewer, OwnershipPanel) estão ausentes, sendo substituídos por código manual duplicado e divergente; e a hierarquia visual de páginas chave — especialmente Executive Overview e Service Detail — não acompanha a maturidade da arquitetura.

O produto transmite uma plataforma enterprise **funcional e sóbria** para quem entende o contexto técnico, mas ainda **não comunica claramente a profundidade da sua proposição de valor** para quem acessa pela primeira vez — especialmente executivos e auditores.

---

## 1. Objetivo da Auditoria

Avaliar com evidências concretas se o frontend do NexTraceOne:
- Transmite produto enterprise confiável, sóbrio e premium
- Possui design system consistente e sustentável
- Serve adequadamente cada persona (Engineer, Tech Lead, Architect, Product, Executive, Platform Admin, Auditor)
- Entrega contexto operacional real (serviço, ambiente, risco, mudança, ownership) em cada tela
- Está pronto para elevação visual e de UX ao padrão enterprise premium

---

## 2. Estado Atual Encontrado

### 2.1 O que funciona bem

| Área | Estado | Evidência |
|------|--------|-----------|
| Design tokens CSS | Excelente | `src/index.css` — 120+ tokens cobrindo canvas, surfaces, text, borders, shadows, motion |
| Paleta dark navy | Sólido | `#081120 → #0A1730 → #0F1E38 → #132543` — hierarquia de profundidade bem construída |
| Componente `Button` | Bom | `src/components/Button.tsx` — 5 variantes, 3 tamanhos, estados loading/disabled |
| Componente `Badge` | Bom | `src/components/Badge.tsx` — variantes semânticas corretas (success/warning/danger/info) |
| AppShell + Sidebar | Bom | `src/components/shell/AppShell.tsx` — sidebar colapsável, mobile drawer, Cmd+K palette |
| Persona-awareness | Bom | `PersonaContext`, `config.homeWidgets`, `config.sectionOrder` — adapta dashboard e sidebar |
| FilterBar/PageContainer | Bom | `src/components/shell/FilterBar.tsx`, `PageContainer.tsx` — padrões de layout presentes |
| Foundations.ts | Bom | `src/shared/design-system/foundations.ts` — padrões de classe reutilizáveis documentados |
| Token storage seguro | Excelente | `src/utils/tokenStorage.ts` — sessionStorage para access token, memória para refresh |
| CSRF Protection | Excelente | `src/api/client.ts` — header X-Csrf-Token injetado automaticamente em métodos state-changing |
| Internacionalização | Bom | 4 idiomas (en, pt-BR, pt-PT, es) com ficheiros de 125-186KB cada |
| Testes | Bom | Vitest + Playwright + MSW — cobertura de componentes, hooks, páginas e E2E |

### 2.2 O que está inconsistente ou fraco

| Área | Gravidade | Evidência |
|------|-----------|-----------|
| Bypass do design system com cores Tailwind hardcoded | **Crítico** | `ServiceDetailPage.tsx:31-66` — 6 mapas de cores inline com `bg-red-900/40`, `bg-emerald-900/40`, `bg-indigo-900/40`, etc. |
| Cores não-tokenizadas em ExecutiveOverviewPage | **Alto** | `ExecutiveOverviewPage.tsx:122,160,184,219,230,269` — `text-emerald-500`, `text-orange-500`, `text-amber-500` |
| Cores não-tokenizadas em IncidentDetailPage | **Alto** | `IncidentDetailPage.tsx:388-389` — `text-emerald-400`, `text-amber-400` |
| ServiceDetailPage não usa PageContainer | **Alto** | `ServiceDetailPage.tsx:113` — `<div className="p-6 lg:p-8 animate-fade-in">` em vez de `<PageContainer>` |
| ExecutiveOverviewPage usa useEffect/useState em vez de TanStack Query | **Médio** | `ExecutiveOverviewPage.tsx:50-62` — fetch manual enquanto todas as outras páginas usam `useQuery` |
| Ausência de EntityHeader padronizado | **Alto** | Cada página de detalhe reinventa o cabeçalho da entidade manualmente |
| Ausência de TimelinePanel | **Alto** | `IncidentDetailPage.tsx:212-228` — timeline construída inline com divs e CSS manual |
| Ausência de OwnershipPanel | **Médio** | `ServiceDetailPage.tsx:351-375` — ownership em card genérico, sem padrão reutilizável |
| Ícones duplicados na sidebar | **Baixo** | `AppSidebar.tsx:57` — `aiAssistant` e `aiAgents` usam `<Bot size={18} />` idêntico |
| StatCard usa caracteres ASCII para tendência | **Baixo** | `StatCard.tsx:29` — `↑` e `↓` em vez de ícones Lucide |
| Badge tem variantes `default` e `neutral` idênticas | **Baixo** | `Badge.tsx:16-17` — `bg-elevated text-body` para ambas |
| Tailwind !important override no dashboard | **Baixo** | `DashboardPage.tsx:215` — `!grid-cols-2 lg:!grid-cols-5` |
| Executive Overview sem charts | **Alto** | `ExecutiveOverviewPage.tsx` — 348 linhas sem nenhuma visualização; ECharts está na stack mas não usado |
| KPIs do dashboard sem links de drill-down | **Médio** | `DashboardPage.tsx:78-119` — StatCards não são clicáveis nem roteiam para contexto |

---

## 3. Análise por Critério

### 3.1 O design atual transmite produto enterprise?

**Parcialmente — 6/10.**

A paleta navy escura, tipografia Inter, sistema de tokens e componentes base criam uma aparência profissional. Contudo, a falta de EntityHeader padronizado, a ausência de charts executivos e a inconsistência de cores entre páginas enfraquecem a percepção de maturidade.

### 3.2 O layout é coerente com a visão do NexTraceOne?

**Majoritariamente — 7/10.**

O AppShell, PageContainer, PageSection e ContentGrid estão bem concebidos. O problema é que **nem todas as páginas os usam** (evidência: `ServiceDetailPage.tsx:113`), gerando inconsistência de padding e ritmo visual.

### 3.3 As páginas parecem parte do mesmo produto?

**Não de forma consistente — 5/10.**

Páginas como `IncidentDetailPage` e `DashboardPage` têm aparência coerente. Já `ServiceDetailPage` e `ExecutiveOverviewPage` divergem em padrões de badge, cores e containers.

### 3.4 O design system está consistente?

**Estruturalmente sim, aplicação não — 5/10.**

Os tokens existem. O problema está na disciplina de uso: `ServiceDetailPage.tsx` define 6 mapas de cores locais (`criticalityColors`, `lifecycleColors`, `protocolColors`, `contractLifecycleColors`) com Tailwind hardcoded em vez de usar `Badge` + tokens.

### 3.5 As cores transmitem maturidade e sobriedade?

**Majoritariamente sim, com alertas — 7/10.**

A paleta base é sóbria. O risco está nos tokens `--color-mint: #1EF2C1` e `--color-cyan: #18CFF2`, que são saturados e podem tender ao estilo sci-fi em contextos densos. Os glow shadows (`--shadow-glow-cyan`) devem ser usados com disciplina extrema.

### 3.6 A hierarquia visual está clara?

**Em páginas simples sim, em páginas complexas não — 5/10.**

`DashboardPage` tem hierarquia razoável. `ExecutiveOverviewPage` empilha 5+ cards com `mb-6` entre eles sem hierarquia visual diferenciada — todos os blocos parecem ter o mesmo peso, sem destaque para o que exige atenção imediata.

### 3.7 A navegação ajuda a encontrar o que importa?

**Estruturalmente boa, volume excessivo — 6/10.**

A sidebar tem 50+ itens em 12 seções. Para personas como Executive ou Auditor, a quantidade de opções opera como ruído. O sistema de `config.sectionOrder` por persona está implementado mas precisa de agrupamento visual mais agressivo.

### 3.8 Cada página ajuda uma persona a tomar decisão real?

**Sim em algumas, não em outras — 6/10.**

- `IncidentDetailPage`: Sim — correlação, evidência, runbooks, serviços impactados estão presentes
- `DashboardPage`: Parcialmente — KPIs sem drill-down direto limitam a tomada de ação
- `ExecutiveOverviewPage`: Não completamente — dados sem visualização, sem hierarquia de urgência

### 3.9 Existem componentes fracos ou desnecessários?

Sim. Ver inventário completo em `frontend-component-inventory.csv`.

**Componentes a substituir ou refatorar:** `HomeWidgetCard`, `StatCard` (expansão necessária), `Badge` (variantes duplicadas).
**Componentes a criar:** `EntityHeader`, `TimelinePanel`, `OwnershipPanel`, `DiffViewer`, `EvidenceDrawer`, `HealthStrip`.

---

## 4. Análise de Persona

### Engineer
- `DashboardPage` com foco em serviços e mudanças: **Adequado**
- `ServiceDetailPage` com ownership, APIs, contratos: **Adequado mas cabeçalho inconsistente**

### Tech Lead
- Visão de equipa via `TeamsOverviewPage`: presente mas não avaliado profundamente
- Dashboard mostra serviços e incidentes: **Adequado**

### Architect
- `ServiceCatalogPage` (graph view) + `SourceOfTruthExplorerPage`: presente
- Dependência entre serviços visível: **Presente**

### Product
- `DashboardPage` mostra confiança de releases: **Parcialmente** — context raso

### Executive
- `ExecutiveOverviewPage`: **Fraco** — sem charts, sem hierarquia de urgência, sem FinOps integrado
- `ExecutiveFinOpsPage`: existe mas não integrado na visão principal

### Platform Admin
- Seção admin na sidebar: completa
- Páginas de configuração e auditoria: presentes

### Auditor
- `AuditPage`: existe
- `EvidencePackagesPage`: existe
- Integração entre auditoria e incidentes/contratos: **fraca** — não há caminho claro de investigação

---

## 5. Segurança e Qualidade Técnica

### Bom
- `tokenStorage.ts`: sem localStorage para tokens sensíveis
- `client.ts`: X-Csrf-Token automático, sem log de dados sensíveis
- `sanitize.ts`: utilitário de sanitização presente
- `vite.config.ts`: source maps desabilitados em produção, console.log removido, terser ativo
- `ProtectedRoute.tsx`: guarda de rota com verificação de permissão

### Atenção
- `ServiceDetailPage.tsx:113-570`: `serviceId` recebido via `useParams` e passado diretamente a APIs sem validação de formato visível no componente
- Ausência de validação de UUID no lado do cliente para parâmetros de rota (não é "GUID manual pelo utilizador", mas IDs de URL merecem verificação de formato)

---

## 6. Acessibilidade

### Bom
- `AppSidebar.tsx:149`: `role="navigation"` + `aria-label` presentes
- `Button.tsx`: `focus-visible:ring` implementado
- `index.css:213-218`: `:focus-visible` global com outline 2px
- `index.css:355-365`: `@media (prefers-reduced-motion: reduce)` implementado
- `Modal.tsx`: usa `<dialog>` nativo (semântica correta)

### Problema
- `StatCard.tsx`: não possui `role` adequado para leitores de ecrã — valor numérico sem contexto acessível
- `Badge` sem `aria-label` quando usado com ícone apenas
- Tabelas em `ServiceDetailPage.tsx` e `IncidentDetailPage.tsx`: sem `caption` ou `aria-describedby`

---

## 7. Responsividade

### Bom
- `PageContainer.tsx`: breakpoints `sm/lg/xl` com padding progressivo
- `ContentGrid.tsx`: 1 a 4 colunas com breakpoints
- `AppShell.tsx`: sidebar desktop + MobileDrawer para mobile
- `FilterBar.tsx`: `flex-wrap` para reflow em mobile

### Problema
- Tabelas em `ServiceDetailPage.tsx` e `IncidentDetailPage.tsx`: `overflow-x-auto` presente mas sem visualização prioritária de colunas em mobile
- `DashboardPage.tsx:215`: `!grid-cols-2 lg:!grid-cols-5` — 5 colunas em desktop pode ser denso em laptops de 1280px

---

## 8. Resumo Executivo

| Dimensão | Nota | Observação |
|----------|------|------------|
| Foundation técnica | 8/10 | Stack moderna, tokens sólidos, arquitetura limpa |
| Consistência visual | 5/10 | Inconsistências graves em ServiceDetailPage e ExecutiveOverviewPage |
| Identidade enterprise | 7/10 | Paleta correta, mas execução irregular |
| Hierarquia visual | 6/10 | Boa em páginas simples, fraca em páginas executivas |
| Componentes | 6/10 | Base boa, mas componentes estratégicos ausentes |
| Clareza de produto | 5/10 | Não é imediatamente claro o que o produto faz ao abrir |
| Utilidade operacional | 7/10 | IncidentDetail e ServiceDetail entregam valor real |
| Persona-awareness | 7/10 | Implementado mas Executive e Auditor subservidos |
| Acessibilidade | 6/10 | Base presente, tabelas e stat cards precisam de atenção |
| Responsividade | 7/10 | Boa cobertura, tabelas e grids densos precisam revisão |

**Nota Global: 6.4/10**

---

## 9. Próximos Passos Prioritários

1. **Corrigir bypass do design system** em `ServiceDetailPage.tsx` — substituir mapas de cor inline por `Badge` com variantes corretas
2. **Corrigir cores hardcoded** em `ExecutiveOverviewPage.tsx` e `IncidentDetailPage.tsx`
3. **Padronizar uso de PageContainer** em todas as páginas de detalhe
4. **Migrar ExecutiveOverviewPage para TanStack Query**
5. **Criar EntityHeader** como componente reutilizável
6. **Adicionar charts** ao ExecutiveOverviewPage (ECharts já está na stack)
7. **Tornar StatCards clicáveis** com roteamento para contexto
8. **Criar TimelinePanel** reutilizável

Ver roadmap completo em `frontend-prioritized-improvement-roadmap.md`.
