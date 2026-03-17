# REFACTORING-PLAN.md — NexTraceOne Frontend

> **Data:** Junho 2025
> **Estratégia:** Refatoração incremental orientada por produto (nunca reescrita total)

---

## Princípio Director

Toda alteração deve:
1. Aproximar o frontend da visão oficial do NexTraceOne
2. Preservar comportamento existente
3. Ser incremental e reversível
4. Respeitar os 3 documentos de design (GUIDELINE.md, DESIGN-SYSTEM.md, DESIGN.md)

---

## Fase 1 — Fundação Técnica (Pré-requisito para tudo)

> **Objectivo:** Preparar a base técnica para refatoração segura.
> **Risco:** Mínimo. Não altera comportamento visual ou funcional.

### 1.1 Aliases TypeScript

Configurar `baseUrl` e `paths` no `tsconfig.app.json`:
```
@/ → src/
@components/ → src/components/
@features/ → src/features/
@lib/ → src/lib/
@api/ → src/api/
```

**Impacto:** Imports mais limpos, refactoring seguro de pastas.

### 1.2 Activar React Query DevTools

Integrar `@tanstack/react-query-devtools` em `App.tsx` (só em dev).

**Impacto:** Debugging de cache/state durante desenvolvimento.

### 1.3 Scripts de qualidade

Adicionar no `package.json`:
- `"typecheck": "tsc --noEmit"`
- `"lint:fix": "eslint . --fix"`

### 1.4 tsconfig hardening

Adicionar:
- `noUncheckedIndexedAccess: true`
- `exactOptionalPropertyTypes: true`

---

## Fase 2 — Eliminação de Cores Hardcoded (Maior impacto visual)

> **Objectivo:** Todas as 54 ficheiros usando cores Tailwind genéricas passam a usar tokens NTO.
> **Risco:** Médio. Mudanças visuais que devem ser verificadas página a página.

### 2.1 Criar mapeamento de substituição

Documento de referência:
```
text-red-300    → text-critical
text-red-400    → text-critical
bg-red-900/40   → bg-critical-muted (ou bg-critical/15)
text-amber-400  → text-warning
text-emerald-400→ text-success
text-blue-400   → text-info
bg-emerald-900/40 → bg-success-muted (ou bg-success/15)
text-gray-400   → text-faded
bg-slate-800/40 → bg-elevated
```

### 2.2 Substituição metódica

Processar ficheiro a ficheiro, verificando cada mudança visualmente.
Começar pelos componentes partilhados, depois páginas.

### 2.3 Criar variantes de Badge para domínio

```
<Badge variant="critical">Critical</Badge>
<Badge variant="success">Active</Badge>
```

---

## Fase 3 — DataTable & Listagens

> **Objectivo:** Componente DataTable genérico enterprise.
> **Risco:** Médio. Componente novo — não quebra existente.

### 3.1 Criar componente DataTable

Specs (DESIGN-SYSTEM.md §4.8):
- Header: 44-48px, texto secondary, peso 600
- Row: 52-64px, hover suave, seleção clara
- Status com Badge component
- Ações em trailing area
- Slot para empty state
- Slot para loading (skeleton rows)

### 3.2 Criar componente Pagination

### 3.3 Migrar tabelas existentes (incremental)

Começar por: IncidentsPage → ServiceCatalogListPage → ContractCatalogPage

---

## Fase 4 — Formulários

> **Objectivo:** Integrar react-hook-form + zod nos formulários existentes.
> **Risco:** Médio. Substituição de state management em forms.

### 4.1 Criar componentes de formulário

- `FormField` — wrapper com label + input + error (integrado com react-hook-form)
- `PasswordInput` — extrair de LoginPage
- `FormSection` — agrupamento visual
- `FormActions` — botões de submit/cancel padronizados

### 4.2 Criar schemas zod

Começar por LoginForm, CreateServiceForm

### 4.3 Migrar forms (incremental)

Começar por: LoginPage → CreateServicePage → restantes

---

## Fase 5 — Reorganização de Componentes

> **Objectivo:** Suborganizar `src/components/` sem quebrar imports.
> **Risco:** Baixo se feito com aliases e barrel exports.

### Estrutura alvo

```
src/
  components/        (barrel re-exports for backwards compat)
    ui/              Button, Badge, Card, TextField, Select, SearchInput, Tabs, etc.
    layout/          AppLayout, AppHeader, Sidebar, Breadcrumbs, PageHeader
    feedback/        EmptyState, Skeleton, ErrorBoundary, StateDisplay, Modal, Drawer
  shared/
    ui/              (alias for components/ui — future migration target)
    lib/             cn.ts + futuras utilities
    api/             client.ts + index.ts
```

### 5.1 Mover ficheiros com re-exports de compatibilidade

### 5.2 Atualizar imports (facilitado pelos aliases da Fase 1)

---

## Fase 6 — Router Architecture

> **Objectivo:** Refatorar App.tsx monolítico para route manifests por feature.
> **Risco:** Médio. Mudança estrutural que exige teste de todas as rotas.

### 6.1 Criar route manifests por feature

Cada feature exporta as suas rotas:
```ts
// features/operations/routes.tsx
export const operationsRoutes = [
  { path: '/operations/incidents', element: <IncidentsPage />, permission: '...' },
  // ...
];
```

### 6.2 Layout routes com ProtectedRoute

```tsx
<Route element={<ProtectedLayout permission="operations:*">}>
  {operationsRoutes}
</Route>
```

### 6.3 Error boundaries por rota

Integrar `errorElement` nas routes.

---

## Fase 7 — Data Layer

> **Objectivo:** Query key factories + hooks de domínio.
> **Risco:** Baixo. Mudança interna sem impacto visual.

### 7.1 Criar query key factory

```ts
export const queryKeys = {
  incidents: {
    all: ['incidents'] as const,
    list: (params: IncidentListParams) => [...queryKeys.incidents.all, 'list', params] as const,
    detail: (id: string) => [...queryKeys.incidents.all, 'detail', id] as const,
    summary: () => [...queryKeys.incidents.all, 'summary'] as const,
  },
  // ...
};
```

### 7.2 Criar hooks de domínio

```ts
export function useIncidents(params: IncidentListParams) {
  return useQuery({
    queryKey: queryKeys.incidents.list(params),
    queryFn: () => incidentsApi.listIncidents(params),
  });
}
```

---

## Fase 8 — Auth Flows

> **Objectivo:** Implementar telas auth em falta.
> **Risco:** Baixo. Telas novas que não alteram existentes.

### 8.1 Forgot Password

### 8.2 Reset Password

### 8.3 Account Activation

### 8.4 MFA/2FA

---

## Fase 9 — App Shell Enhancements

> **Objectivo:** Completar Topbar e melhorar Sidebar.
> **Risco:** Médio. Mudanças no shell afetam todas as páginas.

### 9.1 Topbar: workspace selector, environment badge, notifications

### 9.2 Sidebar: persist collapsed state, mobile drawer

---

## Fase 10 — Acessibilidade

> **Objectivo:** WCAG 2.1 AA compliance.
> **Risco:** Baixo. Melhorias progressivas.

### 10.1 Skip navigation link

### 10.2 Tooltip reescrito com portal + keyboard

### 10.3 @axe-core/playwright tests

### 10.4 Keyboard navigation em dropdowns

---

## O que pode ser reaproveitado (NÃO reescrever)

- `cn()` helper — excelente, manter
- `api/client.ts` — excelente, manter
- `tokenStorage.ts` — seguro, manter
- `AuthContext` — funcional, manter
- `PersonaContext` — funcional, manter
- `Sidebar` — persona-aware, manter e evoluir
- `LoginPage` — alinhada ao design, manter
- `Button`, `Badge`, `Card`, `TextField`, `Select` — bons, manter e evoluir
- `Modal`, `Drawer`, `Tabs` — bons, manter
- Todos os 29 testes — manter e expandir

## O que deve ser removido

- Nada nesta fase. Toda remoção deve esperar migração completa.
- Futuramente: `@testing-library/dom` (redundante)
- Futuramente: inline badge styles (após migração para Badge base)

## O que deve ser consolidado

- Inline badge styles → componente `Badge` ou `StatusBadge`
- Inline table styles → componente `DataTable`
- Inline form state → react-hook-form hooks
- Inline query keys → query key factories
- Ficheiro `types/index.ts` → split por feature

## Riscos a tratar ANTES das próximas fases

1. **Cores hardcoded (Fase 2)** — Sem isto, qualquer mudança visual será inconsistente
2. **Aliases TypeScript (Fase 1.1)** — Sem isto, refatoração de pastas quebra imports
3. **React Query DevTools (Fase 1.2)** — Sem isto, debugging da camada de dados é difícil
