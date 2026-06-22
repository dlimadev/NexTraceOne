# Betterstack — Long-tail de Contratos: passe de jornada v5 — Design

**Data:** 2026-06-22
**Branch:** `redesign/betterstack-contract-longtail`
**Memória de contexto:** `project_betterstack_redesign` (ciclos 5–13)

## Contexto

Auditoria pós-conclusão dos editores de contrato (A–D) revelou que, embora o tema/tokens Betterstack
estejam aplicados app-wide e a grande maioria das telas de serviços/contratos use o padrão de jornada
v5 (`PageHeader`), **4 sub-páginas periféricas de contratos** não receberam o passe de jornada
dedicado — apanharam só o sweep global de tokens. Têm controlos crus, faltam-lhes estados, e várias
têm **cores de status cruas** (`text-green-400`/`red-400`/`orange-400`, `text-white`).

Páginas no escopo:
- `features/contracts/cdct/ConsumerDrivenContractPage.tsx` (340 linhas)
- `features/contracts/playground/ContractPlaygroundPage.tsx` (357 linhas)
- `features/contracts/portal/ContractPortalPage.tsx` (716 linhas)
- `features/contracts/publication/PublicationCenterPage.tsx` (243 linhas)

Decisão do utilizador (brainstorming): **jornada completa, com conversão de formulários** (não o passe
leve). Onde a CTA primária não é óbvia (ações contextuais dentro de cards, como CDCT/Playground), o
`PageHeader` fica **sem CTA** (só título+subtítulo).

## Padrão de jornada v5 (o mesmo dos ciclos 5–6), aplicado a cada página

1. **Shell:** root custom (`<div className="min-h-screen bg-canvas px-6 py-6">` ou equivalente) →
   `PageContainer`. Bloco de header custom (ícone+`<h1>`+subtítulo) → `<PageHeader title subtitle [actions]>`.
   `actions` = CTA primária como DS `Button` apenas quando há uma ação de página clara; caso contrário,
   PageHeader só com título+subtítulo.
2. **Controlos → DS** (`from '../../../shared/ui'`): `<button>` cru → `Button` (variante por papel:
   `primary` para CTA, `outline`/`secondary` para secundária, `ghost` para terciária/cancel);
   `<input>` → `TextField`; `<textarea>` → `TextArea`; `<select>` → `Select`. O `TextField`/`TextArea`/
   `Select` trazem `label` embutido → substituem o par `<label>`+controlo. Preservar `value`/`onChange`/
   `placeholder`/`disabled` exatos.
3. **Cores de status → tokens semânticos:** `text-green-400`→`text-success`, `text-red-400`→`text-critical`,
   `text-orange-400`→`text-warning`, `text-white` (em botão accent)→`text-on-accent`. (Os botões accent
   crus `bg-accent text-white` são substituídos por `<Button variant="primary">`, que já usa
   `text-on-accent`.)
4. **Estados:** garantir `PageLoadingState` (loading), `PageErrorState` (erro, com `onRetry`),
   `EmptyState`/empty inline (vazio). CDCT e Playground têm-nos em falta.
5. **Preservar:** todas as queries/mutations (`useQuery`/`useMutation`), lógica de estado, e **chaves
   i18n existentes** (reutilizar as keys já presentes; adicionar só se um label novo for introduzido).

### Tokens já válidos (não mexer)
- `bg-panel`, `bg-elevated`, `bg-canvas`, `border-edge`, `text-heading`/`body`/`muted`, `text-accent`,
  `text-on-accent` — todos existem no `@theme`. `bg-panel` mantém-se.

## Escopo por página

- **CDCT** (`ConsumerDrivenContractPage`): root `min-h-screen` → `PageContainer`; header → `PageHeader`
  (sem CTA — a ação "Register Expectation" é contextual no card). Seletor (2 `TextField` + Verify
  `Button`), form de nova expectativa (2 `TextField` + 1 `TextArea` + 1 `TextField` + Register/Cancel
  `Button`), botão "Register Expectation" (ghost). Status colors (green/red/orange ×stat cards e
  resultados) → tokens. Já tem `PageErrorState`; falta confirmar loading/empty inline (mantêm-se, com
  tokens).
- **Playground** (`ContractPlaygroundPage`): adicionar `PageContainer`+`PageHeader`; 5 inputs → DS;
  4 botões → DS; **adicionar** `PageErrorState` (tem `isLoading` mas falta tratamento de erro) e empty.
- **Portal** (`ContractPortalPage`): já usa `PageContainer`+`Card`+estados; **adicionar `PageHeader`**;
  3 botões → DS. Passe leve (sem forms). Atenção: ficheiro grande (716 linhas) — mudança cirúrgica só
  no header + botões + eventuais cores status.
- **PublicationCenter** (`PublicationCenterPage`): já `PageContainer`+`Card`+estados; **adicionar
  `PageHeader`**; 4 botões → DS; 1 input → `TextField`; cores status → tokens.

## Fora de escopo

- Lógica de negócio, endpoints, shape de dados.
- Outras telas de contratos/serviços (já v5 ou já feitas nos ciclos A–D).
- Reescrita de layout além do padrão de jornada (sem novas features).

## Critérios de sucesso

1. As 4 páginas usam `PageContainer` + `PageHeader`; zero `<button>`/`<input>`/`<select>`/`<textarea>`
   crus nelas (todos via DS `shared/ui`).
2. Zero cores de status cruas (`green/red/orange-400`, `text-white`) nas 4 páginas → tokens semânticos.
3. Estados loading/error/empty presentes em cada página.
4. Comportamento idêntico: queries/mutations/onChange/i18n preservados.
5. `npm run lint` + `npm run build` 0 erros; suíte total verde.
6. `git diff --name-only` apenas as 4 páginas (+ locales se chaves novas, + docs). Nada não relacionado.

## Riscos

- **Conversão de forms por-campo (não cega):** `TextField`/`TextArea` trazem `label` embutido — ao
  substituir o par `<label>`+`<input>`, garantir que `label`, `value`, `onChange`, `placeholder` ficam
  corretos e que o layout (grid `md:grid-cols-2`) se mantém. Verificar cada campo individualmente.
- **Sem teste a renderizar estas páginas:** rede de segurança = `tsc`/build/lint + smoke visual manual.
  Implementadores devem ser meticulosos; reviewers devem verificar preservação de `onChange`/lógica.
- **Portal (716 linhas):** mudança cirúrgica só no header/botões — não refatorar o resto.
- **Props dos DS:** confirmar a API de `TextField`/`TextArea`/`Select`/`Button` (`shared/ui`) ao
  implementar (label/value/onChange/variant/size) para não inventar props.
