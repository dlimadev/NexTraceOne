# Maturidade na consulta do Catálogo — Plano

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development. Steps usam checkbox (`- [ ]`).

**Goal:** Mostrar a maturidade de cada serviço como coluna na lista do Catálogo de Serviços (join client-side com o dashboard), com deep-link ao dashboard, e remover o item "Score & Maturidade" do menu.

**Architecture:** Enriquecer a `ServiceCatalogListPage` com uma 2ª query ao dashboard de maturidade e um mapa por `serviceId`; adicionar coluna + deep-link; remover o item da sidebar.

**Tech Stack:** React 19, TS 5.9, TanStack Query 5, react-i18next, Vitest + Testing Library.

## Global Constraints

- UI só via chaves i18n — nunca strings hardcoded. 4 locales flat `src/frontend/src/locales/{en,es,pt-BR,pt-PT}.json` — chaves novas em TODOS os 4, preservando o newline final.
- Testes Vitest só descobertos em `src/frontend/src/__tests__/**`.
- Gates por tarefa: `npm run build`, `npm run lint`, testes afetados. Suíte completa + `validate:i18n` no fim (correr de `src/frontend`).
- Mudanças cirúrgicas; Comentários/XML-docs em português, identificadores em inglês; TS strict, TreatWarningsAsErrors.
- Honest-null: sem dados de maturidade → coluna mostra "—"; erro/loading do dashboard NÃO bloqueiam a lista.

---

### Task 1: Coluna "Maturidade" + deep-link na ServiceCatalogListPage

**Files:**
- Modify: `src/frontend/src/features/catalog/pages/ServiceCatalogListPage.tsx`
- Modify: `src/frontend/src/locales/{en,es,pt-BR,pt-PT}.json`
- Test: `src/frontend/src/__tests__/catalog/ServiceCatalogListPage.maturity.test.tsx` (novo)

**Interfaces:**
- Consumes: `serviceCatalogApi.getMaturityDashboard` (`features/catalog/api/serviceCatalog`) → `MaturityDashboardResponse { services: ServiceMaturityItemDto[] }`; `ServiceMaturityItemDto` tem `serviceId`, `level`, `overallScore`. `Badge` de `../../../components/Badge` (ou o já importado na página). `Link` de `react-router-dom`.

- [ ] **Step 1: Escrever o teste que falha**

Criar `src/frontend/src/__tests__/catalog/ServiceCatalogListPage.maturity.test.tsx`. Ler primeiro a `ServiceCatalogListPage.tsx` e (se existir) um teste dela em `src/__tests__/` para reutilizar o scaffold de mocks (QueryClientProvider, MemoryRouter, mock de `serviceCatalogApi.listServices`, i18n). Mockar `serviceCatalogApi.listServices` → 2 serviços (`svc-1`, `svc-2`) e `serviceCatalogApi.getMaturityDashboard` → `{ summary: {}, services: [{ serviceId: 'svc-1', level: 'Managed', overallScore: 0.82, ...campos mínimos }], computedAt: '' }`. Mock de `react-i18next` (t = fallback). Asserir:

```tsx
// coluna de maturidade: badge de nível para svc-1, "—" para svc-2
expect(await screen.findByText('Managed')).toBeInTheDocument(); // ou o fallback do nível
// deep-link ao dashboard
const link = screen.getByRole('link', { name: /maturity dashboard|painel de maturidade|panel de madurez/i });
expect(link).toHaveAttribute('href', '/services/maturity');
```

(Definir os `ServiceMaturityItemDto` com todos os campos obrigatórios do tipo — ler a interface em `features/catalog/api/serviceCatalog.ts` ~linha 455.)

- [ ] **Step 2: Correr — deve falhar**

Run: `cd src/frontend && npx vitest run src/__tests__/catalog/ServiceCatalogListPage.maturity.test.tsx`
Expected: FAIL.

- [ ] **Step 3: Implementar**

Em `ServiceCatalogListPage.tsx`:
1. Importar `getMaturityDashboard` (via `serviceCatalogApi`) e o tipo se necessário; `Link` de `react-router-dom` (se não importado).
2. Adicionar query: `const { data: maturityDash } = useQuery({ queryKey: ['catalog-maturity-dashboard'], queryFn: () => serviceCatalogApi.getMaturityDashboard() });` e `const maturityById = useMemo(() => new Map((maturityDash?.services ?? []).map(s => [s.serviceId, s])), [maturityDash]);`.
3. Helper inline (topo do ficheiro, fora do componente):

```tsx
/** Variante de badge por nível de maturidade. */
function maturityBadgeVariant(level: string): 'success' | 'warning' | 'danger' | 'info' | 'default' {
  switch (level) {
    case 'Optimizing':
    case 'Managed': return 'success';
    case 'Defined': return 'info';
    case 'Developing': return 'warning';
    case 'Initial': return 'danger';
    default: return 'default';
  }
}
```

4. Novo `<th>` "Maturidade" (`t('catalog.columns.maturity', 'Maturity')`) antes da última coluna do `<thead>`; novo `<td>` por linha:

```tsx
<td className="px-4 py-3">
  {(() => {
    const m = maturityById.get(svc.serviceId);
    return m ? (
      <span className="inline-flex items-center gap-1.5">
        <Badge variant={maturityBadgeVariant(m.level)} size="sm">{t(`serviceMaturity.level.${m.level}`)}</Badge>
        <span className="text-xs text-muted">{Math.round(m.overallScore * 100)}</span>
      </span>
    ) : <span className="text-xs text-muted">—</span>;
  })()}
</td>
```

5. Deep-link no cabeçalho do Card da tabela (junto ao título/contagem): `<Link to="/services/maturity" className="text-xs text-accent hover:underline">{t('catalog.maturity.viewDashboard', 'View maturity dashboard')} →</Link>`.

- [ ] **Step 4: Adicionar chaves i18n aos 4 locales**

Dentro de `catalog.columns` (ou `catalog`) e `catalog.maturity` em cada `locales/*.json`:
- `catalog.columns.maturity`: EN "Maturity" · ES "Madurez" · pt "Maturidade"
- `catalog.maturity.viewDashboard`: EN "View maturity dashboard" · ES "Ver panel de madurez" · pt-BR "Ver painel de maturidade" · pt-PT "Ver painel de maturidade"

Preservar o newline final de cada ficheiro.

- [ ] **Step 5: Correr — deve passar**

Run: `cd src/frontend && npx vitest run src/__tests__/catalog/ServiceCatalogListPage.maturity.test.tsx`
Expected: PASS.

- [ ] **Step 6: Build + lint**

Run: `cd src/frontend && npm run build && npm run lint`
Expected: 0 erros.

- [ ] **Step 7: Commit**

```bash
git add src/frontend/src/features/catalog/pages/ServiceCatalogListPage.tsx src/frontend/src/locales src/frontend/src/__tests__/catalog/ServiceCatalogListPage.maturity.test.tsx
git commit -m "feat(catalog): maturidade como coluna na lista do catálogo + deep-link ao dashboard"
```

---

### Task 2: Remover "Score & Maturidade" da sidebar

**Files:**
- Modify: `src/frontend/src/components/shell/AppSidebar.tsx`
- Test: `src/frontend/src/__tests__/components/AppSidebar.navItems.test.ts` (existente — estender)

**Interfaces:**
- Produces: sidebar sem o item `/services/maturity`.

- [ ] **Step 1: Estender o teste existente**

O teste `src/frontend/src/__tests__/components/AppSidebar.navItems.test.ts` já importa `{ navItems }` (exportado no ciclo 39). Adicionar um caso:

```ts
it('não contém o item Score & Maturidade (agora na lista do catálogo)', () => {
  expect(navItems.find((i) => i.to === '/services/maturity')).toBeUndefined();
});
```

- [ ] **Step 2: Correr — deve falhar**

Run: `cd src/frontend && npx vitest run src/__tests__/components/AppSidebar.navItems.test.ts`
Expected: FAIL (item ainda presente).

- [ ] **Step 3: Remover o item**

Em `AppSidebar.tsx`, remover a linha do item `sidebar.scoreMaturity` (`to: '/services/maturity'`, ~linha 61). **NÃO** remover o import `Award` (usado por outros 4 itens: linhas ~136/140/166/182).

- [ ] **Step 4: Correr — deve passar**

Run: `cd src/frontend && npx vitest run src/__tests__/components/AppSidebar.navItems.test.ts`
Expected: PASS.

- [ ] **Step 5: Build + lint**

Run: `cd src/frontend && npm run build && npm run lint`
Expected: 0 erros; sem import órfão.

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/components/shell/AppSidebar.tsx src/frontend/src/__tests__/components/AppSidebar.navItems.test.ts
git commit -m "feat(catalog): remove Score & Maturidade do menu (agora na lista do catálogo)"
```

---

### Task 3: Gates finais + verificação de stub

**Files:** nenhum (verificação, feita pelo controlador).

- [ ] **Step 1: Suíte completa** — `cd src/frontend && npm run test -- --run` → toda passa.
- [ ] **Step 2: i18n + build** — `cd src/frontend && npm run validate:i18n && npm run build` → PASS + sucesso.
- [ ] **Step 3: Stub** — `npm run stub`; verificar: (a) sidebar (Descoberta & Maturidade) sem "Score & Maturidade"; (b) lista do catálogo mostra a coluna Maturidade com badges de nível + score (e "—" onde não há); (c) deep-link "Ver painel de maturidade" navega para `/services/maturity` (página reachable); (d) 0 erros de consola.
