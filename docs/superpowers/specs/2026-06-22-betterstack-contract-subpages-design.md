# Betterstack — Sub-páginas de Contratos: passe de jornada v5 (lote 2) — Design

**Data:** 2026-06-22
**Branch:** `redesign/betterstack-contract-subpages`
**Memória de contexto:** `project_betterstack_redesign` (ciclos 5–14), `reference_button_aschild_not_implemented`

## Contexto

Auditoria rigorosa (full-palette) revelou que "usa `PageHeader`" ≠ "jornada refatorada": várias sub-páginas
de contratos têm header v5 mas **controlos HTML crus por dentro** e não importam DS de `shared/ui`. Este
é o lote 2 do passe long-tail (lote 1 = ciclo 14: CDCT/Playground/Portal/PublicationCenter).

### Páginas no escopo (7) — decisão do utilizador
- `governance/ContractMigrationPage.tsx` (311) — 7 controlos crus + **13 cores cruas** (a mais "legacy")
- `canonical/CanonicalEntityCatalogPage.tsx` (288) — 5 controlos
- `canonical/CanonicalEntityImpactCascadePage.tsx` (243) — 5 controlos
- `spectral/SpectralRulesetManagerPage.tsx` (214) — 4 controlos
- `governance/ContractHealthTimelinePage.tsx` (189) — 3 controlos
- `governance/ContractGovernancePage.tsx` (120) — 2 controlos
- `pages/ContractStudioPage.tsx` (376) — 4 botões de ação crus (Resume/Design/Import/New) — hub já
  redesenhado no ciclo 9, mas com botões raw

### Fora de escopo
- `create/CreateContractPage.tsx` — workspace v5 intencional (ciclo 9); 2 residuais ficam.
- `pages/SoapWsdlBuilderPage.tsx` — editor deferido (Contract Studio, plano próprio).
- Editores A–D (já feitos).

## Padrão de jornada v5 (igual ao lote 1 / ciclo 14; o `PageHeader` já existe → menos shell)

1. **Controlos crus → DS** (`from '../../../shared/ui'` ou profundidade equivalente): `<button>`→`Button`
   (variante por papel: `primary` accent-filled, `outline` bordered, `ghost` text-only, `danger`
   destrutivo); `<input>`→`TextField`; `<select>`→`Select`; `<textarea>`→`TextArea`.
2. **Navegação link-como-botão:** usar `useNavigate()` + `<Button onClick={() => navigate(path)}>`.
   **NUNCA** `<Link><Button>` — o `asChild` do DS `Button` está declarado mas **não implementado**
   (ver `reference_button_aschild_not_implemented`). Um `<Link>` de texto normal pode ficar.
3. **ContractMigrationPage — sweep completo de cores** (não é taxonomia; são status/accent à moda antiga):
   - severidade `text-red-500`/`text-yellow-500`/`text-green-500` → `text-critical`/`text-warning`/`text-success`
   - badges `bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300` (e yellow/green) →
     `bg-critical-muted text-critical` / `bg-warning-muted text-warning` / `bg-success-muted text-success`
   - links `text-blue-600 hover:text-blue-700 dark:text-blue-400` → `text-accent hover:text-accent/80`
   - tabs/botões `bg-blue-600 text-white` → DS `Button variant="primary"` (ou `bg-accent text-on-accent`)
   - banner de erro `bg-red-50 border-red-200 text-red-700 dark:...` → `bg-critical-muted border-critical/25 text-critical`
   - `text-green-300` (pre) → `text-success`
   Confirmar cada token contra o `@theme` ao implementar (existe `*-muted`? `success`/`warning`/`critical` sim).
4. **Estados:** garantir loading/error/empty (a maioria já tem; verificar).
5. **Preservar:** queries/mutations/onChange/i18n; mudança cirúrgica por página.

## Rede de segurança (testes existentes)

4 das 7 páginas **têm teste**: `ContractStudioPage.test.tsx`, `SpectralRulesetManagerPage.test.tsx`,
`ContractMigrationPage.test.tsx`, `CanonicalEntityCatalogPage.test.tsx`. As conversões para DS **têm de
manter esses testes verdes** — os testes podem selecionar por role/texto/testid; preservar texto e papéis
acessíveis (e qualquer `data-testid`). As outras 3 (CanonicalEntityImpactCascade, ContractHealthTimeline,
ContractGovernance) dependem de `tsc`/build/lint + smoke visual.

## Critérios de sucesso

1. Zero `<button>/<input>/<select>/<textarea>` crus nas 7 páginas — todos via DS de `shared/ui`.
2. `ContractMigrationPage` sem cores cruas (`*-{300..600}`, `bg-X-50/100`, `dark:*`, `text-white`) → tokens.
3. Comportamento idêntico: queries/mutations/onChange/i18n preservados; **testes das 4 páginas com teste
   continuam verdes**; suíte total verde.
4. Sem `<Link><Button>` introduzido; link-nav via `useNavigate`.
5. `npm run lint` + `npm run build` 0 erros.
6. `git diff --name-only` apenas as páginas tocadas (+ docs, + locales se chave nova). Nada não relacionado.

## Riscos

- **Testes existentes:** converter `<button>`→`Button` pode mudar a árvore acessível. Correr o teste de
  cada página COM teste após a conversão; se um selector partir (ex.: `getByRole('button',{name})`),
  preservar o nome/texto ou atualizar o teste de forma mínima e justificada.
- **ContractMigration `dark:` manual:** a página usa classes `dark:` explícitas; os tokens semânticos já
  são theme-aware (mudam por `:root`/`.dark`), por isso ao migrar **remover** os pares `light/dark:`
  manuais e usar o token único. Verificar visualmente (smoke) que dark+light continuam corretos.
- **Sem teste a renderizar 3 páginas:** meticulosidade + smoke.
- **Profundidade variável:** a Migration é a mais pesada (controlos + cores + dark sweep); as outras são
  sobretudo `<button>`→`Button`. Uma tarefa por página permite parar/rever individualmente.
