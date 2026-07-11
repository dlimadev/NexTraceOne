# Redesign da Navegação do Módulo de Catálogo — Design

**Data:** 2026-07-11
**Estado:** Aprovado (brainstorming) — pronto para plano de implementação

## Problema

A secção "Catálogo" da sidebar tem ~19 itens planos em 4 sub-grupos, com
comportamento de "item ativo" baseado em prefixo (`NavLink` a fazer
`pathname.startsWith(to)`). Isto produz a confusão relatada pelo utilizador:

1. **Dois itens acendem ao mesmo tempo.** `/contracts` (Catálogo de
   Contratos) corresponde a *qualquer* caminho iniciado por `/contracts` —
   então em `/contracts/health` acendem "Catálogo de Contratos" **e**
   "Saúde dos Contratos". O mesmo para `/services` vs `/services/graph`,
   `/services/discovery`, etc.
2. **Ligações cruzadas "teletransportam" o utilizador.** A partir de um
   contrato, as ligações do workspace ("Portal do consumidor", "Ver fonte
   da verdade", "Cronologia de saúde") saltam o destaque da sidebar para
   *Portal do Programador* / *Fonte de Verdade* / *Saúde dos Contratos*,
   apesar de conceptualmente o utilizador nunca ter saído de "contratos".
3. **Raízes de rota inconsistentes dentro de uma secção** — `/services/*`,
   `/contracts/*`, `/catalog/*`, `/portal`, `/source-of-truth`,
   `/knowledge/*` — o que torna o estado ativo e as breadcrumbs frágeis.

## Decisões (validadas com o utilizador)

- **Esquema de rotas:** dois roots naturais — `/services` e `/contracts`;
  `/source-of-truth` fica transversal (root próprio). Modelo mental:
  "estou a ver um serviço OU um contrato".
- **Densidade do menu:** dois níveis — sidebar enxuta (5 roots) + sub-nav
  dentro de cada área para os drill-downs.
- **Compatibilidade:** rotas antigas continuam a funcionar via redirect.

## 1. Sidebar — de 19 itens planos para 5 roots ancorados

A secção "Catálogo" da sidebar passa a ter apenas os cinco destinos que são
lugares genuinamente distintos:

| Item (labelKey) | Rota |
|---|---|
| `sidebar.serviceCatalog` — Catálogo de Serviços | `/services` |
| `sidebar.contractCatalog` — Catálogo de Contratos | `/contracts` |
| `sidebar.developerPortal` — Portal do Programador | `/portal` |
| `sidebar.sourceOfTruth` — Fonte de Verdade | `/source-of-truth` |
| `sidebar.knowledgeHub` — Centro de Conhecimento | `/knowledge` |

Como os drill-downs saem da sidebar, o **bug do duplo-destaque desaparece
automaticamente**: com apenas roots na sidebar, o prefix-matching devolve
exatamente um item ativo.

Os `subGroup` do catálogo (`subGroupDiscovery`, `subGroupContractGovernance`)
e os respetivos itens são **removidos da lista de navegação da sidebar**
(`NAV_ITEMS` em `AppSidebar.tsx`). O sub-grupo `subGroupDeveloperEnablement`
(Knowledge Hub + Operational Notes) é substituído pelo item único "Centro de
Conhecimento" (`/knowledge`); "Operational Notes" passa a sub-nav de
`/knowledge`.

## 2. Sub-nav de área (navegação secundária dentro de cada área)

Uma barra de separadores persistente no topo de cada área, alimentada por um
wrapper de layout com `<Outlet/>`:

- **/services:** Catálogo (`/services`) · Grafo (`/services/graph`) ·
  Discovery (`/services/discovery`) · Maturidade (`/services/maturity`) ·
  Experiência (`/services/experience`) · Feature Flags
  (`/services/feature-flags`) · Legados (`/services/legacy`)
- **/contracts:** Catálogo (`/contracts`) · Governança
  (`/contracts/governance`) · Saúde (`/contracts/health`) · Regras
  (`/contracts/spectral`) · Canónicas (`/contracts/canonical`) · Publicação
  (`/contracts/publication`) · Pipeline (`/contracts/pipeline`) · CDCT
  (`/contracts/cdct`)

Um serviço específico (`/services/:id`) ou contrato (`/contracts/:id`)
mostra os **seus próprios separadores por-item** (as abas de detalhe/workspace
já existentes) e a sub-nav de área permanece em "Catálogo" — dois níveis
claramente separados, que nunca competem.

### Componente

- Novo componente reutilizável `AreaSubNav` (dirigido por configuração,
  chaves i18n, `NavLink` com `end` no root de lista para que "Catálogo" não
  fique ativo nas sub-páginas, e prefixo nos restantes).
- Dois wrappers de layout de área (`ServicesAreaLayout`, `ContractsAreaLayout`)
  que renderizam `<AreaSubNav items={...} />` seguido de `<Outlet/>`.
- As rotas de cada área passam a ser aninhadas sob o respetivo layout. As
  rotas de **detalhe** (`/services/:id`, `/contracts/:id`) ficam **fora** do
  wrapper de sub-nav (mostram os seus próprios separadores) mas mantêm o root
  destacado na sidebar.

## 3. Regras de estado ativo (elimina o "teletransporte")

A sidebar destaca o **root owner** do caminho atual, sempre um só:

- `/services/**` → Catálogo de Serviços
- `/contracts/**` (incl. `/contracts/portal/:id`) → Catálogo de Contratos
- `/source-of-truth/**` → Fonte de Verdade
- `/portal/**` → Portal do Programador
- `/knowledge/**` → Centro de Conhecimento

Navegar lista → governança → saúde dentro de contratos nunca move a sidebar.
A única "salto" cross-tool intencional que permanece é "Ver fonte da verdade"
(que genuinamente *é* uma ferramenta transversal diferente).

Como só os roots estão na sidebar, o `NavLink` já resolve o destaque único
sem lógica extra. **Verificação:** garantir que nenhum root é prefixo de
outro root de forma ambígua (não é o caso: `/services`, `/contracts`,
`/portal`, `/source-of-truth`, `/knowledge` são disjuntos).

## 4. Unificação de rotas + redirects (nada quebra)

Apenas dois strays alcançáveis pela sidebar mudam; tudo o resto já encaixa.
Caminhos antigos redirecionam para os novos via `<Navigate replace/>`, pelo
que deep links guardados e memórias continuam a funcionar:

| Antigo | Novo |
|---|---|
| `/catalog/developer-experience-score` | `/services/experience` |
| `/catalog/contracts/pipeline` | `/contracts/pipeline` |

Ligações internas (`navigate(...)`, `<Link to=...>`) que apontem para os
caminhos antigos são atualizadas para os novos.

### Fora de âmbito (rotas utilitárias `/catalog/*` não presentes na sidebar)

Estas rotas existem mas **não** são itens de sidebar e não contribuem para a
confusão de navegação relatada; permanecem inalteradas nesta iteração:
`/catalog/templates*`, `/catalog/security-gate`, `/catalog/self-service`,
`/catalog/dependency-dashboard`, `/catalog/license-compliance`. Podem ser
unificadas numa iteração futura.

## 5. Ficheiros afetados

- `src/components/shell/AppSidebar.tsx` — reduzir `NAV_ITEMS` da secção
  `catalog` a 5 roots; remover itens de drill-down e sub-grupos do catálogo.
- `src/features/catalog/components/AreaSubNav.tsx` — **novo** componente.
- `src/features/catalog/layouts/ServicesAreaLayout.tsx` e
  `ContractsAreaLayout.tsx` — **novos** wrappers com `<AreaSubNav/>` +
  `<Outlet/>`.
- `src/routes/catalogRoutes.tsx` — aninhar rotas de `/services/*` sob
  `ServicesAreaLayout`; renomear `/catalog/developer-experience-score` →
  `/services/experience` (+ redirect).
- Ficheiro de rotas de contratos (`contractsRoutes` equivalente) — aninhar
  `/contracts/*` de nível de área sob `ContractsAreaLayout`; mover
  `/catalog/contracts/pipeline` → `/contracts/pipeline` (+ redirect).
- `src/locales/{en,es,pt-BR,pt-PT}.json` — chaves i18n para os separadores
  da sub-nav (`catalog.areaNav.*`, `contracts.areaNav.*`).
- Ligações internas que referenciem os dois caminhos renomeados.

## 6. Faseamento

- **Fase 1 — Sidebar + sub-nav (corrige a queixa principal):** reduzir a
  sidebar a 5 roots; criar `AreaSubNav` + os dois layouts de área; aninhar as
  rotas existentes (apontando para os caminhos atuais). Só isto elimina o
  duplo-destaque e o teletransporte dentro da área.
- **Fase 2 — Unificação de rotas:** renomear os 2 strays + redirects +
  atualizar ligações internas.
- **Fase 3 — Testes, i18n e verificação:** atualizar testes de
  `SidebarComponents`/`AppShell`; adicionar testes de estado ativo da
  sub-nav; verificar redirects; varredura em browser no modo stub.

## 7. Testes

- **Sidebar:** `SidebarComponents.test.tsx` e `AppShell.test.tsx` — atualizar
  para os 5 roots; asserir que exatamente um root fica ativo em caminhos de
  drill-down (ex. `/contracts/health` → só "Catálogo de Contratos").
- **AreaSubNav:** novo teste — item de lista (`end`) só ativo no root exato;
  sub-itens ativos por prefixo; render de todos os separadores.
- **Redirects:** teste de rota — `/catalog/developer-experience-score` e
  `/catalog/contracts/pipeline` redirecionam para os novos caminhos.
- **Browser (modo stub):** percorrer /services e /contracts e cada sub-nav;
  confirmar destaque único na sidebar e ausência de crashes.

## Critérios de sucesso

1. Em qualquer caminho do catálogo, **exatamente um** item da sidebar está
   destacado.
2. Navegar entre sub-áreas de contratos (ou de serviços) **não** muda o item
   destacado na sidebar.
3. Os drill-downs de serviços e de contratos são alcançáveis por uma sub-nav
   consistente no topo de cada área.
4. Caminhos antigos (`/catalog/developer-experience-score`,
   `/catalog/contracts/pipeline`) continuam a funcionar via redirect.
5. `tsc`, `eslint`, `build` e a suite de testes a passar; sem regressões
   visuais na varredura em browser.
