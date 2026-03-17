# AUDIT-REPORT.md — NexTraceOne Frontend — Etapa 1

> **Data:** Junho 2025
> **Scope:** Auditoria completa do frontend React (src/frontend)
> **Referências:** GUIDELINE.md · DESIGN-SYSTEM.md · DESIGN.md

---

## 1. Visão Geral do Estado Atual

### Resumo executivo

O frontend do NexTraceOne está **estruturalmente forte** para um produto nesta fase de maturidade.
A arquitetura de pastas por feature, o uso de i18n pervasivo, o design system tokenizado e a
separação entre API, componentes e páginas são decisões sólidas. No entanto, existem **inconsistências
visuais significativas** entre os componentes base (alinhados ao design system) e as páginas internas
(que frequentemente usam cores hardcoded do Tailwind), além de lacunas em formulários, acessibilidade
e tooling.

### Métricas do codebase

| Métrica | Valor |
|---|---|
| Ficheiros .ts/.tsx (excl. locales, tests) | ~233 |
| Páginas (.tsx em features/*/pages) | 84 |
| Componentes base (src/components/) | 37 |
| Ficheiros de API por feature | 23 |
| Locales suportados | 4 (en, pt-BR, pt-PT, es) |
| Tamanho do locale en.json | ~159 KB |
| Testes unitários/integração | 29 |
| Ficheiros com cores Tailwind hardcoded | **54** |
| Uso de `react-hook-form` | **0 ficheiros** |
| Uso de `@tanstack/react-query-devtools` | **0 ficheiros** |
| Uso de `: any` | 1 ficheiro |

---

## 2. Principais Desvios do GUIDELINE.md

### ✅ O que está alinhado

- Tema dark-first com fundos navy profundos (`--color-canvas: #081120`)
- Accent colors controlados: cyan para foco, mint para sucesso, amber para warning
- CTA com gradiente institucional
- Sidebar como elemento de identidade com navegação persona-aware
- Topbar com busca global, seletor de idioma, perfil
- Login page com split-layout, hero forte, chips institucionais, trust signals
- Tipografia Inter + JetBrains Mono definida nos tokens

### ❌ O que está desalinhado

1. **54 ficheiros usam cores Tailwind hardcoded** (text-red-400, bg-emerald-900/40, etc.) em vez dos tokens semânticos (--color-critical, --color-success). Isto viola GUIDELINE.md §8 "manter coerência de semântica em todo o sistema"
2. **Badges de criticidade/lifecycle nas páginas de catálogo** usam paleta genérica Tailwind (red-900/40, orange-900/40, emerald-900/40) em vez dos tokens NTO — parecem de outro produto
3. **Função `statusIcon` em IncidentsPage** usa `text-red-400`, `text-amber-400`, `text-blue-400` — deveria usar tokens semânticos
4. **Ausência de gradientes de página** (`--nto-gradient-page`) na maioria das páginas internas
5. **Button radius** usa `rounded-lg` (=18px) em vez de `rounded-lg` do DESIGN-SYSTEM (que é 18px OK, mas o lg do Button deveria ser h-14 = 56px, que está correto)
6. **Density**: Algumas páginas carecem de respiro entre seções (gap de 32-40px entre seções maiores conforme GUIDELINE.md §6.1)

---

## 3. Principais Desvios do DESIGN-SYSTEM.md

### ✅ O que está alinhado

- Tokens de cor, spacing, radius, shadow, motion definidos em `index.css` via `@theme` — **excelente**
- Font stack correto (Inter + JetBrains Mono)
- Breakpoints, z-index layers e gradientes definidos
- Componentes base (Button, TextField, Badge, Card, Modal, Drawer, Tabs, Toggle, etc.) documentam referência ao DESIGN-SYSTEM.md
- Skeleton com shimmer animation
- EmptyState com título + descrição + ação

### ❌ O que está desalinhado

1. **Button height para `lg`**: DESIGN-SYSTEM.md especifica 56px (h-14) — o componente usa `h-14` ✅ mas `rounded-lg` onde DESIGN-SYSTEM.md diz `border-radius: 18px`. No Tailwind v4, `rounded-lg` = 18px se os tokens estiverem mapeados. Verificar se `--radius-lg` está a ser usado
2. **Input height**: DESIGN-SYSTEM.md diz 56px — TextField usa `h-14` ✅ mas LoginPage inline inputs também usam `h-14` ✅
3. **Select tamanho padrão**: `md` resulta em `h-11` (44px) — deveria ser `h-14` (56px) para manter consistência com TextField quando lado a lado em formulários
4. **Sidebar width**: DESIGN-SYSTEM.md diz 264-280px — código usa 272px (dentro do range ✅)
5. **Topbar height**: DESIGN-SYSTEM.md diz 64-72px — código usa `h-16` (64px) ✅
6. **Componentes em falta do DESIGN-SYSTEM.md**: TopologyGraph, InsightList, ComplianceCard, AuthHero (como componente reutilizável), DataTable genérico
7. **Type scale**: Não há classes utilitárias para display-01, heading-01, title-01, body-lg, etc. — dependem de classes ad-hoc em cada página
8. **Glow tokens**: Definidos no CSS (`--shadow-glow-cyan`, etc.) mas usados inconsistentemente nas páginas
9. **Motion tokens**: Definidos em `:root` como custom properties, mas muitos componentes usam `duration-[var(--nto-motion-base)]` inline em vez de utility classes

---

## 4. Principais Desvios do DESIGN.md

### ✅ O que está alinhado

- Login honra as 4 promessas visuais (enterprise, clareza, segurança, inteligência)
- North Star da experiência ("entrar, entender, localizar, agir") refletida na Dashboard persona-aware
- Navegação persona-aware na Sidebar com secções priorizadas
- Command Palette (Ctrl+K) para busca global
- Contexto de workspace/tenant mantido via header

### ❌ O que está desalinhado

1. **Drill-down visual**: DESIGN.md §4.3 diz "sair do macro para o detalhe sem ruptura visual" — mas a transição entre listagem e detalhe é abrupta (sem breadcrumbs contextuais ricos, sem painel lateral)
2. **Contexto sempre visível** (DESIGN.md §4.1): Falta indicador de ambiente ativo e janela temporal no topbar
3. **Hierarquia operacional** (DESIGN.md §4.2): Dashboard mostra KPIs mas não prioriza "o que exige ação" com destaque visual forte
4. **Famílias de telas** (DESIGN.md §8): As páginas de configuração/administração são visualmente idênticas às de operação — deveriam ter "menos cenografia, mais clareza"
5. **Inconsistência entre módulos** (DESIGN.md §4.4): Tabelas de ServiceCatalog, Incidents e Changes usam estilos diferentes de badges e status indicators
6. **Blueprint da experiência** (DESIGN.md §9): O detalhe de entidade (§9.4) carece de "header rico" com status, owner, criticidade e abas

---

## 5. Problemas de Arquitetura de Frontend

### ✅ Pontos fortes

- Feature-based folder structure (`features/{domain}/api|pages|components|hooks`)
- Lazy loading de TODAS as páginas exceto Login e TenantSelection — excelente
- Barrel exports por feature (`index.ts`)
- API client centralizado com interceptors (auth, refresh token, tenant header)
- Contextos separados (AuthContext, PersonaContext) sem acoplamento
- `cn()` helper com `clsx` + `tailwind-merge` — excelente

### ❌ Problemas

1. **Monolithic App.tsx router**: 400+ linhas com todas as rotas em flat list — dificulta manutenção. Deveria usar route manifests por feature ou layout routes
2. **Sem layout routes**: Todas as rotas protegidas repetem `<ProtectedRoute permission="...">` wrapper. Route-level guards deveriam ser layout routes com nested Outlet
3. **Sem error boundary por rota**: ErrorBoundary existe mas não está integrado com React Router (sem `errorElement` nas routes)
4. **Sem Suspense boundaries granulares**: Um único `<Suspense fallback={<PageLoader />}>` para toda a app — loading genérico
5. **Pasta `src/components/` monolítica**: 37 componentes sem subpastas — mistura componentes de UI pura (Button, Badge) com componentes de app shell (Sidebar, AppHeader) e componentes de domínio (DomainBadges, PersonaQuickstart)
6. **Sem aliases TypeScript**: Imports usam caminhos relativos profundos (`../../../components/Button`) em vez de `@/components/Button`
7. **react-hook-form + zod instalados mas não utilizados**: Dependências existem no package.json mas nenhum ficheiro as importa
8. **@tanstack/react-query-devtools instalado mas não activado**: DevDependency presente mas não integrado

---

## 6. Problemas Visuais

### Cores hardcoded (CRÍTICO)

54 ficheiros usam cores Tailwind genéricas em vez dos tokens NTO. Os piores ofensores:

| Ficheiro | Exemplo de violação |
|---|---|
| `ServiceCatalogListPage.tsx` | `bg-red-900/40 text-red-300 border-red-700/50` para criticidade |
| `IncidentsPage.tsx` | `text-red-400`, `text-amber-400`, `text-blue-400` para status icons |
| `CommandPalette.tsx` | Cores Tailwind inline |
| `ContractDetailPage.tsx` | Badges com Tailwind genérico |
| `PromotionPage.tsx` | Status colors hardcoded |
| `CreateServicePage.tsx` | Cores arbitrárias |
| Feature components (5+) | `ComplianceScoreCard`, `LifecycleBadge`, `ServiceTypeBadge`, etc. |

### Badges inconsistentes

Existem DOIS padrões de badges no sistema:
1. **Componente Badge** base (`src/components/Badge.tsx`) — usa tokens NTO ✅
2. **Inline badge styles** nas páginas — usam `bg-red-900/40 text-red-300` etc. ❌

### Variações de card

Card base é consistente, mas muitas páginas criam cards inline com estilos diferentes.

---

## 7. Problemas de UX

1. **Ausência de "Forgot Password"**: LoginPage não tem link para recuperação de senha (DESIGN.md §8.1 lista como tela obrigatória)
2. **Ausência de telas auth**: Faltam Forgot Password, Reset Password, Activation, MFA/2FA, Invite flow
3. **Spinner genérico para loading**: `PageLoader` usa spinner rotativo — DESIGN-SYSTEM.md §4.14 diz "não usar spinner como padrão principal para páginas densas"
4. **Breadcrumbs ocultos na Home**: Condição `pathname !== '/'` — correto, mas breadcrumbs não são contextuais o suficiente
5. **Tooltip baseado em CSS/hover**: Não funciona em touch/mobile e não é acessível por teclado

---

## 8. Problemas de Acessibilidade

### Parcialmente implementado ✅

- Labels associados a inputs via `htmlFor`
- `aria-invalid` em campos com erro
- `aria-describedby` para mensagens de erro
- `role="alert"` em mensagens de erro
- `role="switch"` em Toggle
- `role="tab"` e `aria-selected` em Tabs
- Focus ring via `focus-visible:ring-2` nos buttons
- `aria-label` nos botões de ícone

### Em falta ❌

1. **Contraste**: Textos `--color-faded` (#6D7E96) sobre `--color-canvas` (#081120) — ratio ~4.0:1. Atinge AA para texto grande mas falha para texto pequeno (caption, metadata). Necessita verificação detalhada com ferramentas
2. **Focus trap em Modal**: Modal usa `<dialog>` nativo (bom para focus trap) mas o focus management manual pode ter gaps
3. **Keyboard navigation em dropdown de idioma**: Dropdown do AppHeader não suporta arrow keys — apenas click
4. **Skip navigation link**: Ausente — necessário para screen readers
5. **Landmarks**: Faltam `role="navigation"` explícitos, `<main>` correto (existe), `role="banner"`
6. **Live regions**: Apenas mensagens de erro têm `role="alert"` — toasts e notificações precisam de `aria-live`
7. **@axe-core/playwright**: Não instalado — sem testes automatizados de acessibilidade
8. **Hit area**: Tooltip trigger não garante 40x40px mínimo

---

## 9. Problemas de Responsividade

### O que existe

- Login page tem breakpoint `lg:` para split layout → fallback mobile com logo central
- Sidebar colapsa para icon-only mode (64px)
- AppLayout usa `marginLeft` dinâmico

### Problemas

1. **Tabelas não são responsivas**: Não há overflow horizontal, truncamento ou layout alternativo em breakpoints menores
2. **Sidebar mobile**: Não há hamburger menu ou drawer — sidebar colapsa para 64px mas em mobile deveria desaparecer
3. **Topbar em mobile**: Search bar ocupa espaço fixo (w-72) sem adaptação
4. **Dashboard**: Grid de KPI cards não tem breakpoint para stack em mobile
5. **Formulários**: Não há adaptação de layout em mobile para formulários multi-coluna

---

## 10. Problemas de Organização de Código

### ✅ Bom

- Feature-based structure
- API layer por feature
- Contextos isolados
- Utils com propósito claro

### ❌ Problemas

1. **`src/components/` precisa de suborganização**:
   - `ui/` — primitivos puros (Button, Badge, Card, TextField, Select, etc.)
   - `layout/` — shell components (AppLayout, AppHeader, Sidebar, Breadcrumbs)
   - `feedback/` — estados (EmptyState, Skeleton, ErrorBoundary, StateDisplay)
   - `navigation/` — CommandPalette, PageHeader, etc.
2. **Duplicação de API files**: `features/contracts/api/contracts.ts` E `features/catalog/api/contracts.ts` — nomes iguais, propósitos sobrepostos
3. **Tipos centralizados demais**: `src/types/index.ts` é um ficheiro monolítico com tipos de TODOS os domínios — deveria ser split por feature
4. **Componentes de feature em `features/contracts/shared/components/`** — OK como padrão, mas outras features não seguem o mesmo modelo
5. **Sem diretório `shared/ui/`**: Componentes reutilizáveis entre features estão em `src/components/` sem diferenciação de nível

---

## 11. Problemas na Camada de Dados

### ✅ Bom

- `api/client.ts` centralizado com axios
- Token management seguro (sessionStorage + memória)
- Refresh token interceptor com subscriber pattern
- Tenant header injection automático
- Query client com defaults sensatos (retry: 1, staleTime: 30s, gcTime: 5min)

### ❌ Problemas

1. **Query keys inconsistentes**: Algumas queries usam `['incidents', filter, search]` (com params), outras apenas `['graph']` — sem padrão de factory
2. **Sem query key factory**: DESIGN-SYSTEM.md §4 recomenda consistência — query keys deveriam seguir factory pattern (`queryKeys.incidents.list(params)`)
3. **staleTime duplicado**: QueryClient default é 30s E queries individuais também declaram 30s — redundante
4. **Sem invalidação explícita documentada**: Mutations não mostram padrão claro de `queryClient.invalidateQueries`
5. **@tanstack/react-query-devtools**: Instalado como devDependency mas **não integrado** — deveria estar activo em development
6. **Sem hooks de domínio**: Queries são inline nos componentes — deveriam ser custom hooks (`useIncidents`, `useServiceCatalog`, etc.)

---

## 12. Problemas em Formulários

### Estado atual

- **react-hook-form + zod + @hookform/resolvers**: Instalados no package.json mas **não utilizados em NENHUM ficheiro**
- Todos os formulários usam `useState` manual
- Validação é apenas `required` HTML nativo ou lógica ad-hoc

### Problemas específicos

1. **LoginPage**: Usa `useState` para email/password — funcional mas sem validação de formato, sem debounce, sem form state management
2. **CreateServicePage**: Provavelmente usa estado local para cada campo — sem schema validation
3. **Sem padrão de form layout**: Não há componente `FormField` / `FormSection` / `FormActions` padronizado
4. **Mensagens de erro de validação**: Não existem — apenas erro de API é mostrado
5. **Sem client-side validation**: Apenas `required` e `maxLength` HTML nativos

---

## 13. Problemas em Tabelas/Listagens

### Estado atual

- Não existe componente `DataTable` genérico
- Cada página implementa sua própria tabela inline
- 84 páginas, muitas com listagens — cada uma com estilos diferentes

### Problemas específicos

1. **Sem componente DataTable**: DESIGN-SYSTEM.md §4.8 define specs claras (cabeçalho 44-48px, linha 52-64px, hover suave) mas não há implementação genérica
2. **Badge styles inconsistentes**: `ServiceCatalogListPage` usa `bg-red-900/40 text-red-300` enquanto `IncidentsPage` usa o componente `Badge` base
3. **Sem paginação padronizada**: Não existe componente `Pagination`
4. **Sem ordenação**: Nenhuma tabela parece suportar sort por coluna
5. **Sem overflow horizontal**: Tabelas largas não têm scroll horizontal

---

## 14. Problemas em Auth

### ✅ Implementado

- Login com email/password
- SSO/OIDC flow (startOidcLogin)
- Tenant selection
- Token refresh
- Password visibility toggle
- Trust signals
- Error handling seguro (não expõe detalhes técnicos)
- clearSensitiveState após login (limpa password)

### ❌ Em falta

1. **Forgot Password page**: Não existe
2. **Reset Password page**: Não existe
3. **Account Activation page**: Não existe
4. **MFA/2FA flow**: Não existe
5. **Invite flow**: Não existe
6. **Remember me / Keep session**: LoginPage não tem checkbox
7. **Rate limiting feedback**: Sem indicação de tentativas restantes
8. **Password strength indicator**: Ausente

---

## 15. Problemas no App Shell

### ✅ Bom

- Sidebar persona-aware com secções ordenadas e highlight
- Collapsible sidebar com persistência de estado
- Command Palette com Ctrl+K
- Breadcrumbs automáticos
- Brand stripe no topo
- User info no footer da sidebar

### ❌ Problemas

1. **Topbar falta**: Seletor de ambiente/workspace, Notificações bell icon, Perfil dropdown, Ações contextuais
2. **Sidebar não persiste collapsed state**: Estado perdido em navigation/refresh
3. **Sem indicador de módulo ativo no topbar**: DESIGN.md §7.1 diz Topbar deve mostrar contexto
4. **Sem environment badge**: DESIGN.md §4.1 diz "em qual ambiente está analisando"
5. **AppHeader não mostra tenant/workspace**: DESIGN-SYSTEM.md §4.1 diz "seletor de workspace/ambiente"

---

## 16. Dependências a Adicionar

| Pacote | Justificativa |
|---|---|
| `@axe-core/playwright` | Testes automatizados de acessibilidade (GUIDELINE.md §12) |

> **Nota**: `react-hook-form`, `zod`, `@hookform/resolvers`, `clsx`, `tailwind-merge`,
> `@tanstack/react-query-devtools` já estão instalados — apenas não estão integrados.

---

## 17. Dependências a Remover

| Pacote | Justificativa |
|---|---|
| `@testing-library/dom` | Já incluso como transitive dependency de `@testing-library/react`. A versão explícita é redundante. Verificar se algum test importa diretamente antes de remover. |

---

## 18. Dependências a Manter

| Pacote | Status |
|---|---|
| react, react-dom (19.x) | ✅ Atual |
| react-router-dom (7.x) | ✅ Atual |
| @tanstack/react-query (5.x) | ✅ Atual |
| axios | ✅ Bem utilizado com interceptors centralizados |
| i18next + react-i18next | ✅ Bem integrado |
| lucide-react | ✅ Boa escolha para icons enterprise |
| tailwindcss v4 + @tailwindcss/vite | ✅ Atual |
| clsx + tailwind-merge | ✅ Bem integrados via `cn()` |
| react-hook-form + zod + @hookform/resolvers | ✅ Manter — precisam de integração |
| msw | ✅ Para mocking em testes |
| vitest + testing-library | ✅ Stack de testes sólida |
| playwright | ✅ E2E testing |
| terser | ✅ Minificação de produção |
| typescript ~5.9 | ✅ Atual |

---

## 19. Riscos Técnicos

| Risco | Severidade | Impacto |
|---|---|---|
| 54 ficheiros com cores Tailwind hardcoded | **ALTO** | Inconsistência visual sistémica. Produto parece ter 2 design systems |
| 84 páginas sem componente DataTable padronizado | **ALTO** | Cada tabela é diferente. Manutenção e consistência impossíveis |
| Formulários sem react-hook-form/zod | **MÉDIO** | Validação frágil, sem schema, sem error management robusto |
| App.tsx com 400+ linhas de rotas | **MÉDIO** | Dificulta manutenção, onboarding e code review |
| Sem aliases TypeScript (@/) | **MÉDIO** | Imports profundos (`../../../`) frágeis a refactoring |
| react-query-devtools não activado | **BAIXO** | Dificulta debugging de cache/state |
| @axe-core/playwright não instalado | **MÉDIO** | Sem gate de acessibilidade automatizado |
| Tooltip inacessível por teclado | **MÉDIO** | Falha WCAG |
| Sem layout routes | **MÉDIO** | ProtectedRoute wrapper repetido em dezenas de rotas |
| `types/index.ts` monolítico | **BAIXO** | Dificulta manutenção — todos os domínios num ficheiro |

---

## 20. Prioridades Recomendadas

### P0 — Fundação (fazer PRIMEIRO)

1. Criar tokens semânticos como utility classes Tailwind (mapeamento direto)
2. Criar DataTable base component
3. Eliminar cores Tailwind hardcoded nas 54 ficheiros
4. Activar react-query-devtools em dev
5. Configurar aliases TypeScript (@/)

### P1 — Consistência Visual

6. Padronizar todos os badges para usar componente Badge base
7. Criar sistema de type scale como classes utilitárias
8. Criar PageLoader com skeleton em vez de spinner
9. Padronizar status indicators (um componente, tokens semânticos)

### P2 — Formulários e Interacção

10. Integrar react-hook-form + zod nos formulários existentes
11. Criar FormField, FormSection, FormActions padronizados
12. Adicionar Forgot Password, Reset Password, Activation pages

### P3 — Arquitetura

13. Refatorar App.tsx para route manifests por feature
14. Reorganizar src/components/ em subpastas (ui, layout, feedback)
15. Criar query key factories
16. Criar hooks de domínio para queries

### P4 — Qualidade

17. Instalar @axe-core/playwright
18. Adicionar skip-nav link
19. Melhorar Tooltip para suportar keyboard
20. Adicionar testes de acessibilidade nos E2E
