# Contract Consumer Journey — Design (P2, fatia 3)

**Data:** 2026-07-09
**Módulo:** catalog / contracts (frontend)
**Persona:** integrador/consumidor de um contrato
**Ciclo:** P2 fatia 3 — jornada do consumidor (portal ↔ playground ↔ workspace)

---

## 1. Problema

As duas telas centradas no consumidor estão modernizadas em estilo mas nunca foram
ligadas como jornada:

- **`ContractPortalPage`** (`/contracts/portal/:contractVersionId`) — vista rica
  read-only do consumidor (tabs overview/endpoints/schemas/security/versions/
  examples/glossary), com back-link para o workspace. **Órfã:** o único acesso hoje
  é através das linhas de violação da health dashboard (adicionado na fatia 1).
- **`ContractPlaygroundPage`** (`/contracts/playground`) — testador interativo que
  gera respostas-mock a partir do spec. Força **colar manualmente o
  `contractVersionId`** e é **órfã** (sem entrada de navegação nem link de entrada).

Becos da jornada "descobrir → portal → testar → consumir":
1. **Descoberta → portal:** o catálogo (browse J1) abre o **workspace** (editor do
   produtor) via `/contracts/:contractVersionId`, não o portal limpo do consumidor.
2. **Portal → playground:** o portal não liga ao playground; e o playground pede o id
   à mão.
3. **Playground:** órfão e sem retorno (beco sem saída).

**Fator habilitador:** portal, playground e workspace usam **todos a mesma chave
`contractVersionId`** (`getDetail(contractVersionId)`) — sem mismatch de identidade
(ao contrário da fatia 2). A espinha do consumidor é limpa e honesta.

## 2. Objetivo (fatia 3)

Ligar a espinha do consumidor sobre `contractVersionId`, para que um integrador possa
ir da landing de um contrato → portal → testar no playground → voltar, sem becos nem
digitação manual de ids.

Critério de sucesso: (a) o playground abre **com o contrato já carregado** a partir do
portal; (b) o portal tem um caminho de um clique para testar no playground; (c) o
playground não é um beco (volta ao portal); (d) a partir do workspace (onde o catálogo
aterra) há um clique para o portal do consumidor.

## 3. Não-objetivos (deferidos)

- Loop de autoria/enforcement (Spectral/canónicas/publicação) → fatia 4.
- Redesenho interno do playground (ex. `methodColors` com tailwind cru
  `text-blue-400`/`text-yellow-400`/`text-purple-400`; execução real vs mock).
- Mudar o destino do `onOpen` do catálogo (quebraria a jornada do produtor) — o hop
  descoberta→portal é fechado via a landing existente (workspace).

## 4. Desenho

### 4.1 Playground — pré-carregar por `?contractVersionId=`

`ContractPlaygroundPage` passa a ler `contractVersionId` de `useSearchParams()`:
- Semeia o estado `contractVersionId` com o valor do param; a query
  `getDetail(contractVersionId)` (`enabled: !!contractVersionId`) carrega
  automaticamente.
- A entrada manual atual mantém-se como fallback. Nada da lógica de execução muda.

### 4.2 Portal → "Try in playground"

No header do `ContractPortalPage` (que já tem uma ação "Download"), envolver as ações
para adicionar um link:

```tsx
actions={
  <div className="flex items-center gap-2">
    <Link
      to={`/contracts/playground?contractVersionId=${contractVersionId}`}
      className="inline-flex items-center gap-1.5 text-sm text-accent hover:underline"
    >
      <Play size={14} />
      {t('contracts.portal.tryInPlayground', 'Try in playground')}
    </Link>
    <Button ...>Download</Button>
  </div>
}
```

Honest-null: só renderizar quando `contractVersionId` existe (a página já garante
isto — sem `contractVersionId` a query não corre e mostra erro/loading).

### 4.3 Playground → "Back to portal"

Quando o playground tem um `contractVersionId` carregado, mostrar um link de retorno
ao portal (acima do seletor de contrato):

```tsx
{contractVersionId && (
  <Link
    to={`/contracts/portal/${contractVersionId}`}
    className="inline-flex items-center gap-1.5 text-xs text-muted hover:text-accent transition-colors mb-3"
  >
    <ArrowLeft size={12} />
    {t('contracts.playground.backToPortal', 'Back to portal')}
  </Link>
)}
```

Honest-null: só com `contractVersionId` presente.

### 4.4 Workspace → "Consumer portal"

No header do `ContractWorkspacePage` (que já tem o link "Health timeline" da fatia 2 e
o `ContractLifecycleActions`), adicionar um link para o portal do consumidor:

```tsx
<Link
  to={`/contracts/portal/${contractVersionId}`}
  className="inline-flex items-center gap-1.5 text-sm text-accent hover:underline"
>
  <BookOpen size={14} />
  {t('contracts.workspace.consumerPortal', 'Consumer portal')}
</Link>
```

Colocado dentro do wrapper `flex items-center gap-3` já existente das ações, entre o
link "Health timeline" e o `ContractLifecycleActions`. `contractVersionId` vem de
`useParams` (já disponível); honest-null: só quando presente.

## 5. Componentes / ficheiros

- **Modificar:** `features/contracts/playground/ContractPlaygroundPage.tsx` — query
  param + link de retorno ao portal.
- **Modificar:** `features/contracts/portal/ContractPortalPage.tsx` — link "Try in
  playground".
- **Modificar:** `features/contracts/workspace/ContractWorkspacePage.tsx` — link
  "Consumer portal".
- **Modificar:** `locales/{en,es,pt-BR,pt-PT}.json` — chaves novas (§7).

## 6. Fluxo de dados

- Nenhuma query nova. Playground/portal usam `getDetail(contractVersionId)`;
  workspace usa `contractVersionId` de `useParams`. Todos os links usam a chave
  partilhada `contractVersionId` já disponível em cada página. Zero fetch novo, zero
  fabricação.

## 7. i18n (4 locales: en, es, pt-BR, pt-PT)

Chaves novas (com fallback inglês via `t('key','fallback')`):

- `contracts.portal.tryInPlayground` — en `Try in playground`
- `contracts.playground.backToPortal` — en `Back to portal`
- `contracts.workspace.consumerPortal` — en `Consumer portal`

`validate:i18n` tem de passar (4 locales completos e em paridade).

## 8. Testes

- **`ContractPlaygroundPage`**: com `?contractVersionId=cv-1` no router, o input
  reflete `cv-1` e a query `getDetail` é chamada com `cv-1`; com um `contractVersionId`
  presente, existe um `link` "Back to portal" com `href` `/contracts/portal/cv-1`.
- **`ContractPortalPage`**: o header expõe um `link` "Try in playground" com `href`
  `/contracts/playground?contractVersionId=<id>` (mock de `getDetail`/violations/
  history mínimos).
- **`ContractWorkspacePage`**: o header expõe um `link` "Consumer portal" com `href`
  `/contracts/portal/<id>` (reutilizar o padrão de mocks da fatia 2 —
  `WorkspaceLayout` só header, `MonacoEditorWrapper` mockado, hooks mockados).
- **e2e (`contract-consumer-journey.spec.ts`):** ir a
  `/contracts/playground?contractVersionId=cv-1` (rotas mockadas) → o playground
  carrega o contrato sem digitação e mostra o link "Back to portal".

Gates: `npm run test` (suite completa verde), `validate:i18n` PASS, `npm run build`
exit 0, `eslint` 0 erros nos ficheiros alterados, e2e verde.

## 9. Constraints globais

- DS de `../../../shared/ui`; componentes de `components/*`; ícones `lucide-react`;
  `Link`/`useSearchParams`/`useParams` de `react-router-dom`.
- Honest-null: links só quando o `contractVersionId` existe; nunca fabricar.
- i18n: nenhuma string de UI hardcoded; chaves nos 4 locales (NÃO há `fr`); ficheiros
  FLAT `src/frontend/src/locales/<l>.json`.
- Mudanças cirúrgicas: não refatorar a lógica de execução do playground nem o interior
  das tabs do portal; a entrada manual do playground mantém-se como fallback.
- Testes centralizados em `src/frontend/src/__tests__/**`; e2e em
  `src/frontend/e2e/**` (globs de URL Playwright usam `**`).
- Tooling: `npm run test` (não `npx vitest`); gate final `npm run build` (`tsc -b`).
- Rotas verbatim: portal `/contracts/portal/:contractVersionId`; playground
  `/contracts/playground`; workspace `/contracts/:contractVersionId`.
