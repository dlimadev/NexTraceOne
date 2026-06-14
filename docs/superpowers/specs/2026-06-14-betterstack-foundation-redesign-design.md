# Betterstack Foundation Redesign — Design Spec

**Data:** 2026-06-14
**Autor:** brainstorming session (Diogo Lima + Claude)
**Escopo deste ciclo:** Fundação (shell + design tokens + componentes base). Jornada/UX por tela fica para ciclos seguintes.
**Prioridade:** fidelidade visual ao Betterstack (elegante, arejado, minimalista).

---

## 1. Contexto e objetivo

Reformular o layout do frontend NexTraceOne para a estética **Betterstack**, **substituindo** o design Dynatrace Strato recém-concluído (F0–F5). Betterstack é claro/arejado/minimalista; o NexTraceOne adota dele a linguagem visual — bordas finas, muito whitespace, sombras planas (sem glow), status dots, cards limpos — porém com **tema dark como padrão** e **accent azul**, por escolha do usuário.

Como a base já segue Strato (dark denso, roxo-azulado, Roboto, glows), este ciclo entrega **apenas a Fundação**: rethematizar o sistema de tokens, reestruturar o shell e afinar os componentes base. Isso muda o "feel" de todas as ~200 telas de uma vez, porque elas consomem tokens/componentes compartilhados.

### Direção visual travada (via visual companion)
- **Tema:** dark como padrão; tema claro também rethematizado (Betterstack light). Ambos no mesmo `index.css`.
- **Accent:** Electric Blue `#3b82f6` (continuidade com o azul NexTrace; não o verde Betterstack).
- **Navegação:** modelo **icon-rail + painel** (2 níveis) mantido, apenas repaginado. Não accordion, não section-switcher.
- **Tipografia:** Plus Jakarta Sans (sans) + JetBrains Mono (números/código). Substitui Roboto.

---

## 2. Arquitetura do tema (estado atual, confirmado no código)

O tema é **centralizado** em `src/frontend/src/index.css`:

```
[data-theme="light"] / :root  ──┐
[data-theme="dark"]           ──┼─►  define vars  --t-*
[data-sidebar]                ──┘     (canvas, panel, edge, heading, accent, …)
                                          │
                  @theme { --color-canvas: var(--t-canvas); … }   (Tailwind 4)
                                          │
                  utilities semânticas: bg-canvas, text-heading, border-edge, bg-accent, …
                                          │
                  consumidas por:  shell + componentes base + ~200 páginas
```

**Consequência-chave:** reescrever os **valores** das vars `--t-*` (sem renomear nenhuma utility) re-tematiza o app inteiro sem tocar nas páginas. É o que viabiliza a abordagem "retheme no lugar".

Notas de estado atual:
- `--font-sans` aponta para `Roboto` (o `tokens.json` diz Inter, mas está desatualizado — a fonte real é Roboto).
- A sidebar é "sempre dark" via overrides em `[data-sidebar]`.
- Larguras do shell: `SIDEBAR_RAIL_WIDTH = 80`, `SIDEBAR_CONTENT_WIDTH = 240` (em `components/shell/constants.ts`).

---

## 3. Abordagem

**Retheme no lugar** (escolhida sobre "tema paralelo + flag" e "rebuild de componentes"):
1. Reescrever os valores `--t-*` + fonte em `index.css`.
2. Reestruturar os 3 componentes de shell (`AppShell`, `AppSidebar`, `AppTopbar`).
3. Afinar os componentes base para a estética Betterstack (sem mudar API).

Descartado: **tema paralelo/flag** (dobra manutenção, contraria "substituir"); **rebuild de biblioteca** (churn enorme, quebra premissas de 200 telas).

---

## 4. Unidades de trabalho

### 4.1 Tokens (`src/frontend/src/index.css`)

Reescrever valores em `[data-theme="dark"]`, `:root`/`[data-theme="light"]` e `[data-sidebar]`. **Não renomear utilities.**

**Dark (padrão):**
| Token | De (Strato) | Para (Betterstack) |
|---|---|---|
| `--t-canvas` | `#141419` | `#0c0d11` |
| `--t-deep` | `#19192c` | `#0a0b0e` |
| `--t-panel` | `#19192c` | `#121419` |
| `--t-card` | `#19192c` | `#15171c` |
| `--t-elevated` | `#212135` | `#1a1d23` |
| `--t-hover` | `#2a2a40` | `rgba(255,255,255,.04)` |
| `--t-active` | `#32324e` | `rgba(255,255,255,.07)` |
| `--t-edge` | `rgba(255,255,255,.10)` | `rgba(255,255,255,.08)` |
| `--t-edge-strong` | `rgba(255,255,255,.16)` | `rgba(255,255,255,.14)` |
| `--t-heading` | `#ebecff` | `#f3f4f6` |
| `--t-body` | `#a0a1c0` | `#b6bcc6` |
| `--t-muted` | `#6b6b8a` | `#8b909a` |
| `--t-faded` | `#50506a` | `#6c7079` |
| `--t-accent` | `#adb0ff` | `#3b82f6` |
| `--t-accent-hover` | `#c4c6ff` | `#2f6fe0` |
| `--t-accent-muted` | `rgba(173,176,255,.12)` | `rgba(59,130,246,.14)` |
| `--t-focus-ring` | `rgba(173,176,255,.50)` | `rgba(59,130,246,.45)` |
| `--t-on-accent` | `#19192c` | `#ffffff` |

**Light:** `--t-canvas` `#f6f6f7`, `--t-panel/card` `#ffffff`, `--t-elevated` `#fbfbfc`, `--t-edge` `rgba(20,22,30,.08)`, `--t-edge-strong` `rgba(20,22,30,.14)`, `--t-heading` `#1a1a1a`, `--t-body` `#41464f`, `--t-muted` `#6b7280`, `--t-faded` `#9aa0aa`, `--t-accent` `#2563eb`, `--t-accent-hover` `#1d4ed8`, `--t-on-accent` `#ffffff`.

**Semânticas (ambos os temas, valores dark→light):** `--t-success` `#34d399`/`#16a34a`, `--t-warning` `#fbbf24`/`#b45309`, `--t-critical`/`--t-danger` `#f87171`/`#dc2626`, `--t-info` `#60a5fa`/`#2563eb`. Atualizar `*-muted` correspondentes para rgba do mesmo hue.

**Sidebar (`[data-sidebar]`):** alinhar ao dark Betterstack — `--t-panel` `#0f1014`, `--t-card` `#15171c`, `--t-canvas` `#0a0b0e` (rail), `--t-edge` `rgba(255,255,255,.07)`, `--t-accent` `#3b82f6`, `--t-accent-muted` `rgba(59,130,246,.14)`. Remover gradiente: `--t-sidebar-gradient: #0f1014` (flat).

**Sombras / glow:** achatar `--t-shadow-sm/md/lg/xl` para sombras sutis sem o "ring" branco Strato (ex.: `--t-shadow-sm: 0 1px 2px rgba(0,0,0,.3)`). **Neutralizar** `--t-shadow-glow*` (apontar para sombra plana ou `none`) — sem brilho colorido. Manter os nomes das vars.

**Raios (`@theme`):** `--radius-lg: 8px` (controles), `--radius-xl: 12px` (cards) — ajuste fino; manter nomes.

**Gradientes de marca:** revisar `--nto-gradient-cta`/`--nto-gradient-logo`/`brand-gradient` para tons azul→cyan coerentes com o novo accent (sem roxo Strato).

### 4.2 Tipografia (`index.css` + `tokens.json`)
- Substituir o `@import` do Google Fonts: `Plus Jakarta Sans:wght@400;500;600;700` + `JetBrains Mono:wght@400;500`.
- `@theme`: `--font-sans: 'Plus Jakarta Sans', ui-sans-serif, system-ui, …`; `--font-mono: 'JetBrains Mono', ui-monospace, monospace`.
- Atualizar `src/frontend/src/design/tokens.json` (`typography.fontFamily`) para refletir — está desatualizado (diz Inter).

### 4.3 Shell — `src/frontend/src/components/shell/`
- **`constants.ts`:** `SIDEBAR_RAIL_WIDTH` 80→60; `SIDEBAR_CONTENT_WIDTH` 240→248. (Recalcula expanded/collapsed automaticamente.)
- **`AppSidebar.tsx`:** rail near-black flat com borda hairline (remover `var(--t-sidebar-gradient)` gradiente); item ativo = `bg-accent-muted` + `text-accent` + barra `inset 2px accent` sutil (remover `box-shadow` glow/inset Strato e o `linear-gradient` do item ativo); estados hover suaves (`hover:bg-hover`); manter workspace switcher/header do painel. Ajustar larguras às constantes.
- **`AppTopbar.tsx`:** altura ~52px; fundo `bg-canvas`/`bg-panel` flat (sem gradiente); breadcrumb à esquerda, busca `⌘K` à direita, ações; borda inferior hairline.
- **`AppShell.tsx`:** ajustar `lg:ml-20`/`lg:ml-[320px]` às novas larguras (collapsed=60, expanded=308); spinner do loader no novo accent.

### 4.4 Componentes base — `src/frontend/src/components/`
Afinar apenas classes/tokens (sem mudar props/API):
- `Card.tsx`, `ui/card.tsx`: borda 1px `edge`, radius 12, fundo `card`, sem glow, sombra plana.
- `Button.tsx`, `ui/button.tsx`, `IconButton.tsx`: primário sólido `bg-accent`/`text-on-accent`, secundário outline `border-edge`, radius 8, foco azul; remover gradientes/glow.
- `Badge.tsx`, `TrendBadge.tsx`, `ui/badge.tsx`, `FilterChip.tsx`: pills com `*-muted` + texto da cor semântica.
- `DataTable.tsx`: linhas com divisória hairline, hover `bg-hover`, header `text-muted` uppercase sutil.
- `Modal.tsx`, `Drawer.tsx`, `ConfirmDialog.tsx`: overlay novo, painel `bg-panel` borda `edge`, sombra plana.
- Inputs (`FormField`, `SearchInput`, `ComboBox`, `DatePicker`, `Checkbox`, `PasswordInput`): `bg-input`, `border-edge`, foco ring azul.

Critério de seleção dos arquivos: priorizar os listados acima; expandir para outros em `components/*.tsx` apenas se exibirem resíduo visual Strato (glow/roxo/gradiente) em smoke test.

---

## 5. Fora de escopo (não mexer)
- Jornada/UX por tela, fluxo, empty/loading states específicos de página.
- Rotas, itens de navegação (`navItems`), chaves i18n, lógica de componentes.
- Backend, testes que não sejam do shell.
- Estrutura do tema claro (só re-tematizado, não removido).

---

## 6. Critérios de verificação
1. `cd src/frontend && npm run build` conclui sem erro.
2. `npm run lint` sem novos erros.
3. Testes unitários do shell passam: `AppShell.test.tsx`, `ShellComponents.test.tsx`, `SidebarComponents.test.tsx`.
4. Grep não encontra mais resíduo Strato nos tokens: `#adb0ff`, `#5965d9`, `#19192c`, `#141419` como valores de tema (exceto onde intencional), e nenhum `--t-shadow-glow*` com brilho colorido ativo.
5. Contrato de tokens intacto: nenhuma utility semântica renomeada (`bg-canvas`, `text-heading`, `border-edge`, `bg-accent`, etc. continuam existindo) → zero quebra de classe nas 200 telas.
6. Smoke visual (dev server): Dashboard e uma tela de lista (ex.: Incidents) renderizam no padrão Betterstack dark, coerentes com os mockups aprovados (rail+painel, accent azul, cards de borda fina, Plus Jakarta Sans).

---

## 7. Riscos e mitigações
- **Resíduo Strato hardcoded em páginas** (hex inline em vez de utility): fora do escopo da Fundação; capturar lista no smoke test para ciclos de jornada futuros. Mitigação: grep de hex Strato no fim.
- **Contraste/acessibilidade** no novo dark: validar `--t-body`/`--t-muted` sobre `--t-canvas`/`--t-card` (WCAG AA) durante o smoke.
- **Fonte não carrega offline (AirGap)**: o `@import` do Google Fonts já é o padrão atual; manter o mesmo mecanismo (sem regressão). Fallbacks de system-ui garantem degradação.
