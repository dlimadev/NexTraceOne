# Betterstack Foundation Redesign — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Re-tematizar a Fundação do frontend NexTraceOne (tokens + shell + componentes base) para a estética Betterstack — dark como padrão, accent azul `#3b82f6`, Plus Jakarta Sans, sidebar rail+painel repaginada — sem tocar nas ~200 páginas.

**Architecture:** "Retheme no lugar". O tema é centralizado em `src/frontend/src/index.css` (vars `--t-*` → Tailwind 4 `@theme` → utilities semânticas `bg-canvas`/`text-heading`/`border-edge`/`bg-accent`). Reescrever os **valores** das vars (sem renomear utilities) reskina o app inteiro. Em seguida, ajustes estruturais nos 3 shells e ajustes finos de raio/sombra nos componentes base, que já consomem os tokens.

**Tech Stack:** React 19, TypeScript 5.9, Tailwind CSS 4 (config via `@theme` no CSS), Vite 7, Vitest, lucide-react.

**Branch:** `redesign/betterstack-foundation` (já criado; o spec está commitado nele).

**Spec de referência:** `docs/superpowers/specs/2026-06-14-betterstack-foundation-redesign-design.md`

---

## Convenções deste plano

- TDD estrito não se aplica a edições de CSS/tokens (não há unidade testável). Para essas tarefas, a verificação é **build + grep**. Para componentes de shell com testes existentes, rodamos os testes após editar.
- Todos os comandos rodam a partir de `src/frontend/` salvo indicação contrária.
- Comando de build: `npm run build`. Lint: `npm run lint`. Testes: `npm run test -- --run <arquivo>`.
- Commits frequentes, um por tarefa.

## Mapa de arquivos

| Arquivo | Responsabilidade | Ação |
|---|---|---|
| `src/index.css` | Fonte da verdade do tema (vars `--t-*`, `@theme`, fontes, sombras, gradientes) | Modify |
| `src/design/tokens.json` | Espelho documental dos tokens (desatualizado) | Modify |
| `src/components/shell/constants.ts` | Larguras do rail/painel | Modify |
| `src/components/shell/AppSidebar.tsx` | Rail + painel de navegação | Modify |
| `src/components/shell/AppTopbar.tsx` | Barra de topo | Modify |
| `src/components/shell/AppShell.tsx` | Layout raiz, margens, loader | Modify |
| `src/components/Card.tsx`, `src/components/ui/card.tsx` | Cards | Modify |
| `src/components/Button.tsx`, `src/components/ui/button.tsx`, `src/components/IconButton.tsx` | Botões | Modify |
| `src/components/Badge.tsx`, `src/components/TrendBadge.tsx`, `src/components/FilterChip.tsx`, `src/components/ui/badge.tsx` | Badges/pills | Modify |
| `src/components/DataTable.tsx` | Tabela de dados | Modify |
| `src/components/Modal.tsx`, `src/components/Drawer.tsx`, `src/components/ConfirmDialog.tsx` | Overlays | Modify |
| `src/components/FormField.tsx`, `SearchInput.tsx`, `ComboBox.tsx`, `DatePicker.tsx`, `Checkbox.tsx`, `PasswordInput.tsx` | Inputs | Modify |

---

## Task 1: Fontes (Plus Jakarta Sans + JetBrains Mono)

**Files:**
- Modify: `src/index.css:14` (import) e `:265-267` (font vars no `@theme`)
- Modify: `src/design/tokens.json` (`typography.fontFamily`)

- [ ] **Step 1: Trocar o `@import` de fontes**

Em `src/index.css`, substituir a linha 14:
```css
@import url('https://fonts.googleapis.com/css2?family=Roboto:wght@300;400;500;600;700&family=Roboto+Mono:wght@400;500&display=swap');
```
por:
```css
@import url('https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700&family=JetBrains+Mono:wght@400;500&display=swap');
```

- [ ] **Step 2: Atualizar as font vars no `@theme`**

Em `src/index.css`, substituir:
```css
  --font-sans: 'Roboto', ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
  --font-mono: 'Roboto Mono', 'JetBrains Mono', ui-monospace, SFMono-Regular, monospace;
```
por:
```css
  --font-sans: 'Plus Jakarta Sans', ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
  --font-mono: 'JetBrains Mono', ui-monospace, SFMono-Regular, monospace;
```

- [ ] **Step 3: Atualizar `tokens.json`**

Em `src/design/tokens.json`, no objeto `typography.fontFamily`, substituir os valores:
```json
      "sans": { "$value": "Plus Jakarta Sans, ui-sans-serif, system-ui, sans-serif", "$type": "fontFamily" },
      "mono": { "$value": "JetBrains Mono, ui-monospace, monospace",     "$type": "fontFamily" }
```

- [ ] **Step 4: Build**

Run: `npm run build`
Expected: build conclui sem erro.

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/index.css src/frontend/src/design/tokens.json
git commit -m "feat(design): trocar fonte para Plus Jakarta Sans + JetBrains Mono"
```

---

## Task 2: Tokens do tema DARK (padrão)

**Files:**
- Modify: `src/index.css` bloco `[data-theme="dark"]` (linhas ~108-195)

- [ ] **Step 1: Reescrever backgrounds, edges e texto**

No bloco `[data-theme="dark"]`, aplicar estes valores (substituir os atuais):
```css
  --t-canvas: #0c0d11;
  --t-deep: #0a0b0e;
  --t-panel: #121419;
  --t-card: #15171c;
  --t-elevated: #1a1d23;
  --t-subtle: #121419;
  --t-hover: rgba(255, 255, 255, 0.04);
  --t-active: rgba(255, 255, 255, 0.07);
  --t-selected: rgba(59, 130, 246, 0.12);
  --t-input: #0f1014;
  --t-overlay: rgba(0, 0, 0, 0.72);

  --t-edge: rgba(255, 255, 255, 0.08);
  --t-edge-strong: rgba(255, 255, 255, 0.14);
  --t-edge-focus: rgba(59, 130, 246, 0.50);
  --t-divider: rgba(255, 255, 255, 0.06);

  --t-heading: #f3f4f6;
  --t-body: #b6bcc6;
  --t-muted: #8b909a;
  --t-faded: #6c7079;
  --t-on-accent: #ffffff;
```

- [ ] **Step 2: Reescrever accent (azul) e semânticas**

```css
  --t-accent: #3b82f6;
  --t-accent-hover: #2f6fe0;
  --t-accent-muted: rgba(59, 130, 246, 0.14);
  --t-focus-ring: rgba(59, 130, 246, 0.45);

  --t-cyan: #22d3ee;
  --t-cyan-hover: #67e8f9;
  --t-mint: #34d399;
  --t-mint-hover: #6ee7b7;

  --t-blue: #3b82f6;
  --t-blue-hover: #2f6fe0;
  --t-blue-muted: rgba(59, 130, 246, 0.14);
  --t-blue-glow: rgba(59, 130, 246, 0.18);

  --t-success: #34d399;
  --t-success-muted: rgba(52, 211, 153, 0.14);
  --t-info: #60a5fa;
  --t-info-muted: rgba(96, 165, 250, 0.14);
  --t-warning: #fbbf24;
  --t-warning-muted: rgba(251, 191, 36, 0.14);
  --t-critical: #f87171;
  --t-critical-muted: rgba(248, 113, 113, 0.14);
  --t-danger: #f87171;
  --t-neutral: #8b909a;
```

- [ ] **Step 3: Sidebar/header/scrollbar do bloco dark**

```css
  --t-sidebar-bg: #0f1014;
  --t-sidebar-gradient: linear-gradient(180deg, #0f1014 0%, #0f1014 100%);
  --t-header-bg: rgba(12, 13, 17, 0.85);
  --t-scrollbar: rgba(255, 255, 255, 0.12);
  --t-scrollbar-hover: rgba(255, 255, 255, 0.22);
  --t-selection-bg: rgba(59, 130, 246, 0.22);
```

> Deixar `--t-data-1..10` como estão por enquanto (paleta de gráficos é aceitável; ajuste fino fica para ciclo de jornada). Os `--t-shadow-*` deste bloco são tratados na Task 5.

- [ ] **Step 4: Build**

Run: `npm run build`
Expected: build sem erro.

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/index.css
git commit -m "feat(design): retheme dark para palette Betterstack (accent azul)"
```

---

## Task 3: Tokens do tema LIGHT

**Files:**
- Modify: `src/index.css` bloco `:root, [data-theme="light"]` (linhas ~23-105)

- [ ] **Step 1: Reescrever backgrounds/edges/texto do light**

```css
  --t-canvas: #f6f6f7;
  --t-deep: #efeff1;
  --t-panel: #ffffff;
  --t-card: #ffffff;
  --t-elevated: #fbfbfc;
  --t-subtle: #f6f6f7;
  --t-hover: #f1f1f3;
  --t-active: #e9e9ec;
  --t-selected: rgba(37, 99, 235, 0.10);
  --t-input: #ffffff;
  --t-overlay: rgba(15, 15, 30, 0.45);

  --t-edge: rgba(20, 22, 30, 0.08);
  --t-edge-strong: rgba(20, 22, 30, 0.14);
  --t-edge-focus: rgba(37, 99, 235, 0.42);
  --t-divider: rgba(20, 22, 30, 0.06);

  --t-heading: #1a1a1a;
  --t-body: #41464f;
  --t-muted: #6b7280;
  --t-faded: #9aa0aa;
  --t-on-accent: #ffffff;
```

- [ ] **Step 2: Reescrever accent e semânticas do light**

```css
  --t-accent: #2563eb;
  --t-accent-hover: #1d4ed8;
  --t-accent-muted: rgba(37, 99, 235, 0.10);
  --t-focus-ring: rgba(37, 99, 235, 0.40);
  --t-cyan: #0891b2;
  --t-cyan-hover: #0e7490;
  --t-mint: #16a34a;
  --t-mint-hover: #15803d;

  --t-blue: #2563eb;
  --t-blue-hover: #1d4ed8;
  --t-blue-muted: rgba(37, 99, 235, 0.10);
  --t-blue-glow: rgba(37, 99, 235, 0.15);

  --t-success: #16a34a;
  --t-success-muted: rgba(22, 163, 74, 0.10);
  --t-info: #2563eb;
  --t-info-muted: rgba(37, 99, 235, 0.10);
  --t-warning: #b45309;
  --t-warning-muted: rgba(180, 83, 9, 0.10);
  --t-critical: #dc2626;
  --t-critical-muted: rgba(220, 38, 38, 0.10);
  --t-danger: #dc2626;
  --t-neutral: #6b7280;
```

- [ ] **Step 3: Sidebar/header/scrollbar do light**

A sidebar permanece dark (ver Task 4). Ajustar:
```css
  --t-sidebar-bg: #0f1014;
  --t-sidebar-gradient: linear-gradient(180deg, #0f1014 0%, #0f1014 100%);
  --t-header-bg: rgba(246, 246, 247, 0.85);
  --t-scrollbar: rgba(20, 22, 30, 0.14);
  --t-scrollbar-hover: rgba(20, 22, 30, 0.24);
  --t-selection-bg: rgba(37, 99, 235, 0.16);
```

- [ ] **Step 4: Build**

Run: `npm run build`
Expected: build sem erro.

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/index.css
git commit -m "feat(design): retheme light para palette Betterstack"
```

---

## Task 4: Tokens da sidebar (`[data-sidebar]`)

**Files:**
- Modify: `src/index.css` bloco `[data-sidebar]` (linhas ~394-421)

- [ ] **Step 1: Alinhar `[data-sidebar]` ao dark Betterstack**

Substituir os valores do bloco `[data-sidebar]` por:
```css
[data-sidebar] {
  /* Betterstack dock — flat near-black */
  --t-canvas: #0a0b0e;
  --t-deep: #0a0b0e;
  --t-panel: #0f1014;
  --t-card: #15171c;
  --t-elevated: #1a1d23;
  --t-subtle: #121419;
  --t-hover: rgba(255, 255, 255, 0.05);
  --t-active: rgba(255, 255, 255, 0.08);
  --t-edge: rgba(255, 255, 255, 0.07);
  --t-edge-strong: rgba(255, 255, 255, 0.12);
  --t-edge-focus: rgba(59, 130, 246, 0.40);
  --t-divider: rgba(255, 255, 255, 0.06);
  --t-heading: #f3f4f6;
  --t-body: rgba(182, 188, 198, 0.9);
  --t-muted: rgba(139, 144, 154, 0.85);
  --t-faded: rgba(108, 112, 121, 0.7);
  --t-accent: #3b82f6;
  --t-accent-hover: #5a98f8;
  --t-accent-muted: rgba(59, 130, 246, 0.14);
  --t-blue: #3b82f6;
  --t-blue-muted: rgba(59, 130, 246, 0.15);
  --t-success: #34d399;
  --t-warning: #fbbf24;
  --t-critical: #f87171;
  --t-sidebar-gradient: linear-gradient(180deg, #0f1014, #0f1014);
}
```

- [ ] **Step 2: Build**

Run: `npm run build`
Expected: build sem erro.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/index.css
git commit -m "feat(design): retheme tokens da sidebar para Betterstack flat"
```

---

## Task 5: Achatar sombras, neutralizar glow, raios e gradientes de marca

**Files:**
- Modify: `src/index.css` — `--t-shadow-*` nos blocos dark e light; `@theme` (`--radius-*`); `--nto-gradient-*` em `:root`

- [ ] **Step 1: Sombras planas no bloco DARK**

No bloco `[data-theme="dark"]`, substituir as `--t-shadow-*` por sombras sutis sem "ring" branco, e neutralizar os glows:
```css
  --t-shadow-xs: 0 0 0 1px rgba(255, 255, 255, 0.05);
  --t-shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.30);
  --t-shadow-md: 0 2px 6px rgba(0, 0, 0, 0.35);
  --t-shadow-lg: 0 8px 24px rgba(0, 0, 0, 0.40);
  --t-shadow-xl: 0 16px 40px rgba(0, 0, 0, 0.50);
  --t-shadow-glow-cyan: 0 1px 2px rgba(0, 0, 0, 0.30);
  --t-shadow-glow-mint: 0 1px 2px rgba(0, 0, 0, 0.30);
  --t-shadow-glow-danger: 0 1px 2px rgba(0, 0, 0, 0.30);
  --t-shadow-glow-blue: 0 1px 2px rgba(0, 0, 0, 0.30);
  --t-shadow-glow: none;
  --t-shadow-glow-sm: none;
```

- [ ] **Step 2: Sombras planas no bloco LIGHT**

No bloco `:root, [data-theme="light"]`, substituir por:
```css
  --t-shadow-xs: 0 0 0 1px rgba(20, 22, 30, 0.06);
  --t-shadow-sm: 0 1px 2px rgba(20, 22, 30, 0.06), 0 1px 3px rgba(20, 22, 30, 0.04);
  --t-shadow-md: 0 2px 8px rgba(20, 22, 30, 0.08);
  --t-shadow-lg: 0 8px 24px rgba(20, 22, 30, 0.10);
  --t-shadow-xl: 0 16px 40px rgba(20, 22, 30, 0.12);
  --t-shadow-glow-cyan: 0 1px 2px rgba(20, 22, 30, 0.06);
  --t-shadow-glow-mint: 0 1px 2px rgba(20, 22, 30, 0.06);
  --t-shadow-glow-danger: 0 1px 2px rgba(20, 22, 30, 0.06);
  --t-shadow-glow-blue: 0 1px 2px rgba(20, 22, 30, 0.06);
  --t-shadow-glow: none;
  --t-shadow-glow-sm: none;
```

- [ ] **Step 3: Raios no `@theme`**

No bloco `@theme`, ajustar (manter os nomes):
```css
  --radius-xs: 3px;
  --radius-sm: 6px;
  --radius-md: 8px;
  --radius-lg: 10px;
  --radius-xl: 12px;
  --radius-2xl: 16px;
  --radius-pill: 999px;
```

- [ ] **Step 4: Gradientes de marca azul→cyan (sem roxo)**

No bloco `:root` (linhas ~333-340), substituir os gradientes que usam roxo Strato:
```css
  --nto-gradient-cta: linear-gradient(135deg, #3b82f6 0%, #2f6fe0 100%);
  --nto-gradient-blue: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
  --nto-gradient-logo: linear-gradient(135deg, #3b82f6 0%, #22d3ee 50%, #34d399 100%);
```
E na linha ~999 do final do arquivo (`.notebook-cell-ai`), trocar o fallback roxo:
```css
.notebook-cell-ai { border-left: 3px solid var(--t-accent, #3b82f6); }
```
Também atualizar as utilities de marca (`.brand-gradient`, `.brand-gradient-text`, `.logo-gradient*`) que hardcodam `#1B7FE8 ... #18E8B8` para usar `#3b82f6 0%, #22d3ee 50%, #34d399 100%`.

- [ ] **Step 5: Build**

Run: `npm run build`
Expected: build sem erro.

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/index.css
git commit -m "feat(design): achatar sombras, remover glow Strato e azular gradientes de marca"
```

---

## Task 6: Checkpoint visual da camada de tokens

- [ ] **Step 1: Subir o dev server e inspecionar**

Run: `npm run dev`
Abrir `http://localhost:5173`, logar e navegar ao Dashboard e a uma lista (ex.: `/operations/incidents`).
Expected: fundo near-black `#0c0d11`, cards `#15171c` com borda hairline, accent azul, fonte Plus Jakarta Sans, **sem** roxo Strato e **sem** glows coloridos. A sidebar ainda terá proporções/realces antigos — será corrigida nas Tasks 7-8.

- [ ] **Step 2: Grep de resíduo Strato nos tokens**

Run (de `src/frontend`): `grep -nE "#adb0ff|#5965d9|#ebecff|#a0a1c0|Roboto" src/index.css`
Expected: nenhuma ocorrência em valores de tema (o `@import` já foi trocado). Se aparecer, corrigir antes de seguir.

> Sem commit (apenas verificação).

---

## Task 7: Larguras do shell (`constants.ts`)

**Files:**
- Modify: `src/components/shell/constants.ts`

- [ ] **Step 1: Ajustar larguras**

Substituir:
```ts
export const SIDEBAR_RAIL_WIDTH = 80;
export const SIDEBAR_CONTENT_WIDTH = 240;
```
por:
```ts
export const SIDEBAR_RAIL_WIDTH = 60;
export const SIDEBAR_CONTENT_WIDTH = 248;
```
(`SIDEBAR_WIDTH_COLLAPSED` = 60 e `SIDEBAR_WIDTH_EXPANDED` = 308 são derivados automaticamente.)

- [ ] **Step 2: Build**

Run: `npm run build`
Expected: build sem erro.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/components/shell/constants.ts
git commit -m "feat(shell): rail mais fino (60px) e painel 248px"
```

---

## Task 8: Repaginar `AppSidebar`

**Files:**
- Modify: `src/components/shell/AppSidebar.tsx`

- [ ] **Step 1: Rail flat + logo height + tab icons sem glow**

No rail (div com `style={{ width: SIDEBAR_RAIL_WIDTH, background: 'var(--t-sidebar-gradient)' }}`):
- Manter `background: 'var(--t-sidebar-gradient)'` (já flat via Task 4).
- Reduzir a altura do header do logo de `h-[70px]` para `h-[56px]` (linha do bloco logo) e ajustar o do painel também (Step 3).
- No botão de section icon (bloco `isActive ? ...`), substituir a classe ativa com glow:
```
'bg-[rgba(27,127,232,.25)] text-[#3D96F2] shadow-[0_0_12px_rgba(27,127,232,.18),inset_0_0_0_1px_rgba(27,127,232,.4)]'
```
por (flat, sem glow):
```
'bg-accent-muted text-accent shadow-[inset_0_0_0_1px_var(--t-accent-muted)]'
```
- No estado inativo, substituir as cores hardcoded `rgba(129,170,214,...)` por tokens: `text-faded hover:bg-hover hover:text-body`. O estado `isHighlighted` pode usar `text-accent`.
- Ajustar o tamanho do botão de `w-[48px] h-[44px]` para `w-[40px] h-[40px]` (rail mais fino).

- [ ] **Step 2: Item de navegação ativo sem gradiente/glow**

No `NavLink` (className por `isActive`), substituir:
```
'bg-[linear-gradient(90deg,rgba(27,127,232,.22),rgba(18,196,232,.08))] text-[#EAF2FF] font-medium shadow-[inset_2px_0_0_#1B7FE8]'
```
por (flat Betterstack):
```
'bg-accent-muted text-accent font-medium shadow-[inset_2px_0_0_var(--t-accent)]'
```
E nos estados inativos, substituir as cores hardcoded `rgba(181,196,216,.6)` / `rgba(129,170,214,.4)` por tokens: inativo `text-body hover:bg-hover hover:text-heading`; preview `text-faded hover:bg-hover hover:text-body`.

- [ ] **Step 3: Header do painel e logo**

- No header do painel de conteúdo (div `h-[70px] px-5 ... border-b border-edge`), trocar `h-[70px]` por `h-[56px]` para casar com o rail.
- A cor hardcoded `text-[rgba(181,196,216,.9)]` do nome da marca → `text-heading`.
- A heading de seção `text-accent` (linha ~474) permanece (agora resolve para azul).

- [ ] **Step 4: Rodar testes do sidebar**

Run: `npm run test -- --run src/__tests__/components/shell/SidebarComponents.test.tsx`
Expected: PASS. Se algum teste asserta classes/cores antigas hardcoded, atualizar o teste para o novo token (anotar no commit).

- [ ] **Step 5: Build**

Run: `npm run build`
Expected: build sem erro.

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/components/shell/AppSidebar.tsx src/frontend/src/__tests__/components/shell/SidebarComponents.test.tsx
git commit -m "feat(shell): repaginar sidebar Betterstack (flat, accent azul, sem glow)"
```

---

## Task 9: Repaginar `AppTopbar`

**Files:**
- Modify: `src/components/shell/AppTopbar.tsx`

- [ ] **Step 1: Topbar flat hairline**

O componente já usa `h-12 border-b border-edge` e `bg` via `--t-header-bg` (agora translúcido near-black). Aplicar `backdrop-blur` sutil e remover qualquer realce:
- No `<header>`, adicionar `backdrop-blur-sm` à lista de classes do `cn(...)`.
- Confirmar que o divisor `w-px h-5 bg-edge` permanece (hairline) — manter.
- Nenhuma cor hardcoded a trocar aqui (já usa tokens).

- [ ] **Step 2: Build**

Run: `npm run build`
Expected: build sem erro.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/components/shell/AppTopbar.tsx
git commit -m "feat(shell): topbar flat com hairline e backdrop-blur"
```

---

## Task 10: Ajustar margens e loader em `AppShell`

**Files:**
- Modify: `src/components/shell/AppShell.tsx:114-119` (margens) e `:59` (loader)

- [ ] **Step 1: Margens conforme novas larguras**

Na div principal, substituir:
```
sidebarCollapsed ? 'lg:ml-20' : 'lg:ml-[320px]',
```
por:
```
sidebarCollapsed ? 'lg:ml-[60px]' : 'lg:ml-[308px]',
```

- [ ] **Step 2: Loader no novo accent**

A linha 59 já usa `border-accent` (resolve para azul). Nenhuma mudança necessária; confirmar visualmente.

- [ ] **Step 3: Rodar testes do shell**

Run: `npm run test -- --run src/__tests__/components/shell/AppShell.test.tsx src/__tests__/components/shell/ShellComponents.test.tsx`
Expected: PASS. Atualizar asserts de largura/classe se houver.

- [ ] **Step 4: Build**

Run: `npm run build`
Expected: build sem erro.

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/components/shell/AppShell.tsx src/frontend/src/__tests__/components/shell/AppShell.test.tsx src/frontend/src/__tests__/components/shell/ShellComponents.test.tsx
git commit -m "feat(shell): margens do conteúdo conforme novas larguras do rail/painel"
```

---

## Task 11: Afinar `Card` (+ `ui/card`)

**Files:**
- Modify: `src/components/Card.tsx:37-45`
- Modify: `src/components/ui/card.tsx`

- [ ] **Step 1: Raios maiores e glass/gradient sem glow no `Card.tsx`**

Substituir o `variantClasses` por (radius `rounded-xl` = 12px; sombras planas já vêm dos tokens):
```ts
const variantClasses: Record<NonNullable<CardProps['variant']>, string> = {
  default: 'bg-card rounded-xl border border-edge shadow-sm overflow-hidden',
  interactive:
    'bg-card rounded-xl border border-edge shadow-sm overflow-hidden cursor-pointer hover:border-edge-strong transition-all duration-[var(--nto-motion-base)]',
  elevated: 'bg-card rounded-xl border border-edge shadow-md overflow-hidden',
  flat: 'bg-card rounded-xl overflow-hidden',
  glass: 'backdrop-blur-xl bg-card/70 rounded-xl border border-edge shadow-sm overflow-hidden',
  gradient: 'rounded-xl border-0 shadow-md overflow-hidden text-white bg-gradient-to-br from-accent via-blue to-cyan',
};
```

- [ ] **Step 2: Alinhar `ui/card.tsx`**

Abrir `src/components/ui/card.tsx`. Onde houver `rounded-md`/`rounded-lg` no container do card, trocar por `rounded-xl`; garantir `border border-edge` e `bg-card`; remover qualquer `shadow-glow*`/`shadow-lg` permanente em favor de `shadow-sm`.

- [ ] **Step 3: Build**

Run: `npm run build`
Expected: build sem erro.

- [ ] **Step 4: Commit**

```bash
git add src/frontend/src/components/Card.tsx src/frontend/src/components/ui/card.tsx
git commit -m "feat(ui): cards Betterstack (radius 12, borda hairline, sem glow)"
```

---

## Task 12: Afinar `Button` (+ `ui/button`, `IconButton`)

**Files:**
- Modify: `src/components/Button.tsx:65`
- Modify: `src/components/ui/button.tsx`, `src/components/IconButton.tsx`

- [ ] **Step 1: Raio do botão base no `Button.tsx`**

Na linha do `cn(...)` base do botão, trocar `rounded-sm` por `rounded-md` (8px):
```
'inline-flex items-center justify-center rounded-md font-medium',
```
(As variantes já usam tokens — `bg-accent`/`text-on-accent`/`border-edge` — e ficam corretas após o retheme. Não alterar `variantClasses`.)

- [ ] **Step 2: Alinhar `ui/button.tsx` e `IconButton.tsx`**

Abrir cada arquivo. Trocar `rounded-sm`→`rounded-md` no container; garantir que o primário use `bg-accent text-on-accent` e o foco `ring-accent`; remover gradientes/glow se houver (exceto a variante `institutional` que pode manter `blue-gradient`).

- [ ] **Step 3: Build**

Run: `npm run build`
Expected: build sem erro.

- [ ] **Step 4: Commit**

```bash
git add src/frontend/src/components/Button.tsx src/frontend/src/components/ui/button.tsx src/frontend/src/components/IconButton.tsx
git commit -m "feat(ui): botões Betterstack (radius 8, primário azul sólido)"
```

---

## Task 13: Afinar badges/pills

**Files:**
- Modify: `src/components/Badge.tsx`, `src/components/TrendBadge.tsx`, `src/components/FilterChip.tsx`, `src/components/ui/badge.tsx`

- [ ] **Step 1: Aplicar checklist Betterstack a cada arquivo**

Para cada um, abrir e garantir:
- Formato pill: `rounded-pill` (ou `rounded-full`).
- Cores via tokens semânticos + `*-muted`: ex. sucesso = `bg-success-muted text-success`, crítico = `bg-critical-muted text-critical`, accent = `bg-accent-muted text-accent`.
- Remover qualquer borda/sombra com glow; borda opcional `border border-edge` apenas em badges neutros.
- Trocar cores hardcoded (hex/rgba Strato) por tokens.

- [ ] **Step 2: Build**

Run: `npm run build`
Expected: build sem erro.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/components/Badge.tsx src/frontend/src/components/TrendBadge.tsx src/frontend/src/components/FilterChip.tsx src/frontend/src/components/ui/badge.tsx
git commit -m "feat(ui): badges/pills Betterstack (muted + cor semântica)"
```

---

## Task 14: Afinar `DataTable`

**Files:**
- Modify: `src/components/DataTable.tsx`

- [ ] **Step 1: Aplicar estética de tabela Betterstack**

Abrir `DataTable.tsx` e garantir:
- Cabeçalho: `text-muted` com `text-xs font-semibold` e `border-b border-edge` (sem fundo forte; opcional `bg-subtle`).
- Linhas: divisória `border-b border-edge/60` (hairline), hover `hover:bg-hover`.
- Remover qualquer `shadow-glow*`, zebra forte ou borda dupla; container `border border-edge rounded-xl overflow-hidden` se aplicável.
- Trocar cores hardcoded por tokens.

- [ ] **Step 2: Build**

Run: `npm run build`
Expected: build sem erro.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/components/DataTable.tsx
git commit -m "feat(ui): DataTable Betterstack (linhas hairline, header muted)"
```

---

## Task 15: Afinar overlays (`Modal`, `Drawer`, `ConfirmDialog`)

**Files:**
- Modify: `src/components/Modal.tsx`, `src/components/Drawer.tsx`, `src/components/ConfirmDialog.tsx`

- [ ] **Step 1: Aplicar checklist a cada overlay**

Para cada um:
- Overlay/backdrop: `bg-overlay` (token) com `backdrop-blur-sm`.
- Painel: `bg-panel border border-edge rounded-xl shadow-lg` (sombra plana via token).
- Remover glow/gradiente Strato; foco e ações via tokens.
- Trocar cores hardcoded por tokens.

- [ ] **Step 2: Build**

Run: `npm run build`
Expected: build sem erro.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/components/Modal.tsx src/frontend/src/components/Drawer.tsx src/frontend/src/components/ConfirmDialog.tsx
git commit -m "feat(ui): overlays Betterstack (backdrop-blur, painel hairline)"
```

---

## Task 16: Afinar inputs

**Files:**
- Modify: `src/components/FormField.tsx`, `SearchInput.tsx`, `ComboBox.tsx`, `DatePicker.tsx`, `Checkbox.tsx`, `PasswordInput.tsx`

- [ ] **Step 1: Aplicar checklist a cada input**

Para cada um:
- Campo: `bg-input border border-edge rounded-md text-body placeholder:text-faded`.
- Foco: `focus-visible:ring-2 focus-visible:ring-accent` (azul); remover glow Strato.
- Checkbox/radio: estado checado `bg-accent border-accent`.
- Trocar cores hardcoded por tokens.

- [ ] **Step 2: Build**

Run: `npm run build`
Expected: build sem erro.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/components/FormField.tsx src/frontend/src/components/SearchInput.tsx src/frontend/src/components/ComboBox.tsx src/frontend/src/components/DatePicker.tsx src/frontend/src/components/Checkbox.tsx src/frontend/src/components/PasswordInput.tsx
git commit -m "feat(ui): inputs Betterstack (borda hairline, foco azul)"
```

---

## Task 17: Verificação final

- [ ] **Step 1: Build de produção**

Run (de `src/frontend`): `npm run build`
Expected: conclui sem erro.

- [ ] **Step 2: Lint**

Run: `npm run lint`
Expected: sem novos erros.

- [ ] **Step 3: Testes do shell + componentes**

Run: `npm run test -- --run src/__tests__/components/shell`
Expected: PASS.

- [ ] **Step 4: Grep de resíduo Strato nos tokens**

Run: `grep -nE "#adb0ff|#5965d9|#ebecff|#a0a1c0|1B7FE8|18E8B8|Roboto" src/index.css`
Expected: nenhuma ocorrência ativa (só comentários, se houver).

- [ ] **Step 5: Smoke visual (dev)**

Run: `npm run dev` → abrir Dashboard + `/operations/incidents`.
Expected (conforme mockups aprovados):
- Fundo near-black `#0c0d11`, cards `#15171c` borda hairline.
- Accent azul `#3b82f6` em botões primários, item ativo da sidebar e links.
- Fonte Plus Jakarta Sans; números/mono em JetBrains Mono.
- Rail fino (60px) + painel (248px), sem glow.
- Sem roxo Strato e sem brilhos coloridos.
- Contraste de texto legível (WCAG AA) sobre canvas/card.

- [ ] **Step 6: Anotar resíduos de página (para ciclos futuros)**

Run: `grep -rnE "#adb0ff|#5965d9|1B7FE8|#19192c" src/features | head -40`
Registrar a lista no spec (seção Riscos) como dívida para os ciclos de jornada — **não corrigir agora** (fora de escopo da Fundação).

- [ ] **Step 7: Commit final (se houver ajustes pendentes)**

```bash
git add -A src/frontend
git commit -m "chore(design): verificação final da Fundação Betterstack"
```

---

## Self-review (preenchido pelo autor do plano)

- **Cobertura do spec:** §4.1 tokens → Tasks 2-5; §4.2 fontes → Task 1; §4.3 shells → Tasks 7-10; §4.4 componentes base → Tasks 11-16; §6 verificação → Tasks 6, 17. ✅
- **Placeholders:** valores hex e edições concretas em todas as tasks de tokens/shell; tasks de componentes em lote (13-16) usam checklist + "abrir e aplicar" por serem N arquivos com mesmo padrão — aceitável e DRY.
- **Consistência de nomes:** larguras (60/248/308) consistentes entre `constants.ts` (Task 7) e margens do `AppShell` (Task 10). Tokens citados existem todos no `@theme`.
