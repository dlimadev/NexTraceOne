# Dynatrace Migration — F0 + F1 + F2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Migrar o design system do NexTraceOne para o estilo Dynatrace Strato — near-black backgrounds, purple-blue primary (#adb0ff), border radius sutil, shadows flat/raised/floating, tipografia Roboto, status vertical lines.

**Architecture:** Foundation-first: F0 altera tokens CSS (impacto automático em 80% das páginas), F1 refina os 94 componentes base, F2 atualiza shell/navegação. Sem mudanças em páginas individuais — apenas tokens + componentes.

**Tech Stack:** React 19, TypeScript 5.9, Tailwind CSS 4, CSS Custom Properties, Vite 7

---

## File Map

| Tarefa | Arquivo |
|--------|---------|
| F0-T1 | `src/frontend/src/index.css` — dark/light tokens |
| F0-T2 | `src/frontend/src/index.css` — @theme, sidebar, gradients |
| F0-T3 | `src/frontend/src/design/tokens.json` |
| F1-T1 | `src/frontend/src/components/Button.tsx` |
| F1-T2 | `src/frontend/src/components/Badge.tsx` |
| F1-T3 | `src/frontend/src/components/Card.tsx` |
| F1-T4 | `src/frontend/src/components/Alert.tsx` |
| F1-T5 | `src/frontend/src/components/StatCard.tsx` |
| F1-T6 | `src/frontend/src/components/Tabs.tsx` |
| F1-T7 | `src/frontend/src/components/Modal.tsx` |
| F1-T8 | `src/frontend/src/components/TextField.tsx` |
| F2-T1 | `src/frontend/src/components/shell/AppSidebar.tsx` |
| F2-T2 | `src/frontend/src/components/shell/AppSidebarItem.tsx` |
| F2-T3 | `src/frontend/src/components/shell/AppSidebarFooter.tsx` |
| F2-T4 | `src/frontend/src/components/shell/AppTopbar.tsx` |

---

## FASE 0 — Design Tokens

### Task F0-T1: Dark theme CSS tokens (index.css)

**Files:**
- Modify: `src/frontend/src/index.css` (linhas 99–174, bloco `[data-theme="dark"]`)

- [ ] **Step 1: Substituir o bloco `[data-theme="dark"]` inteiro**

```css
[data-theme="dark"] {
  /* ── Backgrounds (Dynatrace Strato near-black) ── */
  --t-canvas: #141419;
  --t-deep: #19192c;
  --t-panel: #19192c;
  --t-card: #19192c;
  --t-elevated: #212135;
  --t-subtle: #1a1a2e;
  --t-hover: #2a2a40;
  --t-active: #32324e;
  --t-selected: #2a2a44;
  --t-input: #141419;
  --t-overlay: rgba(0, 0, 0, 0.72);

  /* ── Borders ── */
  --t-edge: rgba(255, 255, 255, 0.10);
  --t-edge-strong: rgba(255, 255, 255, 0.16);
  --t-edge-focus: rgba(173, 176, 255, 0.50);
  --t-divider: rgba(255, 255, 255, 0.06);

  /* ── Text ── */
  --t-heading: #ebecff;
  --t-body: #a0a1c0;
  --t-muted: #6b6b8a;
  --t-faded: #50506a;
  --t-on-accent: #19192c;

  /* ── Primary accent: purple-blue (Strato) ── */
  --t-accent: #adb0ff;
  --t-accent-hover: #c4c6ff;
  --t-accent-muted: rgba(173, 176, 255, 0.12);
  --t-focus-ring: rgba(173, 176, 255, 0.50);

  /* ── Cyan: mantido como cor de suporte (telemetria/health) ── */
  --t-cyan: #12C4E8;
  --t-cyan-hover: #3DD2F5;

  /* ── Mint: ajustado para Strato success teal ── */
  --t-mint: #47ae8f;
  --t-mint-hover: #60c5a6;

  /* ── Blue institucional (mantido) ── */
  --t-blue: #1B7FE8;
  --t-blue-hover: #3D96F2;
  --t-blue-muted: rgba(27, 127, 232, 0.14);
  --t-blue-glow: rgba(27, 127, 232, 0.20);

  /* ── Semantic status (Strato palette) ── */
  --t-success: #47ae8f;
  --t-success-muted: rgba(71, 174, 143, 0.12);
  --t-info: #65a2ce;
  --t-info-muted: rgba(101, 162, 206, 0.12);
  --t-warning: #e9b86e;
  --t-warning-muted: rgba(233, 184, 110, 0.12);
  --t-critical: #da7288;
  --t-critical-muted: rgba(218, 114, 136, 0.12);
  --t-danger: #da7288;
  --t-neutral: #6b6b8a;

  /* ── Data viz: 10-color Strato app palette ── */
  --t-data-1: #528dee;
  --t-data-2: #47ae8f;
  --t-data-3: #61bab1;
  --t-data-4: #e9b86e;
  --t-data-5: #da7288;
  --t-data-6: #7d6afa;
  --t-data-7: #8793af;
  --t-data-8: #65a2ce;
  --t-data-9: #c16aad;
  --t-data-10: #8261cf;

  /* ── Shadows: Strato 3-level system ── */
  --t-shadow-xs: 0px 0px 0px 1px rgba(240, 240, 245, 0.08);
  --t-shadow-sm: 0px 0px 0px 1px rgba(240, 240, 245, 0.08), 0px 1px 2px rgba(240, 240, 245, 0.05), 0px 4px 8px -2px rgba(240, 240, 245, 0.07);
  --t-shadow-md: 0px 0px 0px 1px rgba(240, 240, 245, 0.08), 0px 1px 2px rgba(240, 240, 245, 0.05), 0px 5px 11px -2px rgba(240, 240, 245, 0.10);
  --t-shadow-lg: 0px 0px 0px 1px rgba(240, 240, 245, 0.08), 0px 1px 2px rgba(240, 240, 245, 0.05), 0px 5px 11px -2px rgba(240, 240, 245, 0.16);
  --t-shadow-xl: 0px 0px 0px 1px rgba(240, 240, 245, 0.10), 0px 2px 4px rgba(240, 240, 245, 0.06), 0px 8px 20px -4px rgba(240, 240, 245, 0.20);
  --t-shadow-glow-cyan: 0 0 0 1px rgba(18, 196, 232, 0.20), 0 0 16px rgba(18, 196, 232, 0.12);
  --t-shadow-glow-mint: 0 0 0 1px rgba(71, 174, 143, 0.18), 0 0 16px rgba(71, 174, 143, 0.10);
  --t-shadow-glow-danger: 0 0 0 1px rgba(218, 114, 136, 0.22), 0 0 16px rgba(218, 114, 136, 0.12);
  --t-shadow-glow-blue: 0 0 0 1px rgba(27, 127, 232, 0.24), 0 0 16px rgba(27, 127, 232, 0.14);
  --t-shadow-glow: 0 0 16px rgba(173, 176, 255, 0.14);
  --t-shadow-glow-sm: 0 0 8px rgba(173, 176, 255, 0.08);

  /* ── Sidebar / Shell ── */
  --t-sidebar-bg: #141419;
  --t-sidebar-gradient: linear-gradient(180deg, #141419 0%, #141419 100%);
  --t-header-bg: rgba(20, 20, 25, 0.95);
  --t-scrollbar: rgba(173, 176, 255, 0.14);
  --t-scrollbar-hover: rgba(173, 176, 255, 0.24);
  --t-selection-bg: rgba(173, 176, 255, 0.20);
}
```

- [ ] **Step 2: Verificar no terminal que o arquivo foi salvo sem erros de syntax**

```bash
cd src/frontend && npx vite build --mode development 2>&1 | head -30
```

Expected: sem erros de CSS.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/index.css
git commit -m "style(tokens): migrate dark theme to Dynatrace Strato near-black palette"
```

---

### Task F0-T2: Tailwind @theme + sidebar overrides + gradients (index.css)

**Files:**
- Modify: `src/frontend/src/index.css` (linhas 244–287 @theme radius/shadows/font; linhas 369–395 [data-sidebar]; linhas 453–496 gradients)

- [ ] **Step 1: Atualizar radius e font no bloco @theme (linhas ~241–251)**

Substituir as linhas de `--font-sans`, `--font-mono` e `--radius-*` dentro do `@theme {}`:

```css
  /* Typography — Roboto (Dynatrace padrão) */
  --font-sans: 'Roboto', ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
  --font-mono: 'Roboto Mono', 'JetBrains Mono', ui-monospace, SFMono-Regular, monospace;

  /* Border Radius — Strato (sutil, profissional) */
  --radius-xs: 2px;
  --radius-sm: 4px;
  --radius-md: 6px;
  --radius-lg: 8px;
  --radius-xl: 12px;
  --radius-2xl: 16px;
  --radius-pill: 999px;
```

- [ ] **Step 2: Atualizar referências de shadow no @theme**

Substituir as linhas `--shadow-*` para usar os 3 níveis Strato:

```css
  /* Shadows: Strato 3-level (flat, raised, floating) */
  --shadow-xs: var(--t-shadow-xs);
  --shadow-sm: var(--t-shadow-sm);
  --shadow-md: var(--t-shadow-md);
  --shadow-surface: var(--t-shadow-sm);
  --shadow-lg: var(--t-shadow-lg);
  --shadow-elevated: var(--t-shadow-md);
  --shadow-xl: var(--t-shadow-xl);
  --shadow-floating: var(--t-shadow-lg);
  --shadow-glow-cyan: var(--t-shadow-glow-cyan);
  --shadow-glow-mint: var(--t-shadow-glow-mint);
  --shadow-glow-danger: var(--t-shadow-glow-danger);
  --shadow-glow-blue: var(--t-shadow-glow-blue);
  --shadow-glow: var(--t-shadow-glow);
  --shadow-glow-sm: var(--t-shadow-glow-sm);
```

- [ ] **Step 3: Atualizar o bloco `[data-sidebar]`**

Substituir o bloco completo `[data-sidebar] { ... }`:

```css
[data-sidebar] {
  --t-canvas: #141419;
  --t-deep: #19192c;
  --t-panel: #141419;
  --t-card: #19192c;
  --t-elevated: #212135;
  --t-subtle: #1a1a2e;
  --t-hover: rgba(255, 255, 255, 0.05);
  --t-active: #2a2a40;
  --t-edge: rgba(255, 255, 255, 0.08);
  --t-edge-strong: rgba(255, 255, 255, 0.14);
  --t-edge-focus: rgba(173, 176, 255, 0.40);
  --t-divider: rgba(255, 255, 255, 0.06);
  --t-heading: #ebecff;
  --t-body: rgba(160, 161, 192, 0.9);
  --t-muted: rgba(107, 107, 138, 0.8);
  --t-faded: rgba(80, 80, 106, 0.7);
  --t-accent: #adb0ff;
  --t-accent-hover: #c4c6ff;
  --t-accent-muted: rgba(173, 176, 255, 0.12);
  --t-blue: #1B7FE8;
  --t-blue-muted: rgba(27, 127, 232, 0.15);
  --t-success: #47ae8f;
  --t-warning: #e9b86e;
  --t-critical: #da7288;
  --t-sidebar-gradient: linear-gradient(180deg, #141419, #141419);
}
```

- [ ] **Step 4: Atualizar `--nto-header-height` e gradients no `:root`**

Localizar e substituir no bloco `:root`:

```css
  --nto-header-height: 48px;
```

Atualizar `--nto-gradient-cta` para usar a nova primary:
```css
  --nto-gradient-cta: linear-gradient(135deg, #adb0ff 0%, #999bed 100%);
```

- [ ] **Step 5: Adicionar import do Roboto no topo do arquivo (antes de `@import "tailwindcss"`)**

```css
@import url('https://fonts.googleapis.com/css2?family=Roboto:wght@300;400;500;600;700&family=Roboto+Mono:wght@400;500&display=swap');
```

- [ ] **Step 6: Adicionar tokens data-7 a data-10 no @theme**

No bloco `@theme`, após `--color-data-6`:
```css
  --color-data-7: var(--t-data-7);
  --color-data-8: var(--t-data-8);
  --color-data-9: var(--t-data-9);
  --color-data-10: var(--t-data-10);
```

- [ ] **Step 7: Commit**

```bash
git add src/frontend/src/index.css
git commit -m "style(tokens): update Tailwind @theme radius/shadows/typography to Dynatrace Strato"
```

---

### Task F0-T3: Light theme tokens (index.css)

**Files:**
- Modify: `src/frontend/src/index.css` (linhas 22–97, bloco `:root, [data-theme="light"]`)

- [ ] **Step 1: Substituir o bloco light theme**

```css
:root,
[data-theme="light"] {
  /* ── Backgrounds (Strato light — levemente purple-tinted) ── */
  --t-canvas: #f8f9ff;
  --t-deep: #f0f0f8;
  --t-panel: #ffffff;
  --t-card: #ffffff;
  --t-elevated: #f4f4fc;
  --t-subtle: #f6f6fc;
  --t-hover: #ededf8;
  --t-active: #e4e4f2;
  --t-selected: #e8e8f5;
  --t-input: #ffffff;
  --t-overlay: rgba(15, 15, 30, 0.45);

  /* ── Borders ── */
  --t-edge: rgba(60, 60, 100, 0.12);
  --t-edge-strong: rgba(60, 60, 100, 0.20);
  --t-edge-focus: rgba(89, 101, 217, 0.42);
  --t-divider: rgba(60, 60, 100, 0.06);

  /* ── Text ── */
  --t-heading: #14142a;
  --t-body: #3c3c5a;
  --t-muted: #6b6b8a;
  --t-faded: #9090aa;
  --t-on-accent: #ffffff;

  /* ── Primary accent: Strato purple-blue (escuro no light) ── */
  --t-accent: #5965d9;
  --t-accent-hover: #4756c8;
  --t-accent-muted: rgba(89, 101, 217, 0.10);
  --t-focus-ring: rgba(89, 101, 217, 0.40);

  /* ── Cyan suporte ── */
  --t-cyan: #0891B2;
  --t-cyan-hover: #0E7490;

  /* ── Mint (success) ── */
  --t-mint: #1a7a5e;
  --t-mint-hover: #156649;

  /* ── Blue institucional ── */
  --t-blue: #1B7FE8;
  --t-blue-hover: #1468CC;
  --t-blue-muted: rgba(27, 127, 232, 0.10);
  --t-blue-glow: rgba(27, 127, 232, 0.15);

  /* ── Semantic status ── */
  --t-success: #1a7a5e;
  --t-success-muted: rgba(26, 122, 94, 0.10);
  --t-info: #2563aa;
  --t-info-muted: rgba(37, 99, 170, 0.10);
  --t-warning: #92580a;
  --t-warning-muted: rgba(146, 88, 10, 0.10);
  --t-critical: #b02244;
  --t-critical-muted: rgba(176, 34, 68, 0.10);
  --t-danger: #b02244;
  --t-neutral: #6b6b8a;

  /* ── Data viz (light equivalents) ── */
  --t-data-1: #2e5bd9;
  --t-data-2: #1a7a5e;
  --t-data-3: #2a8a84;
  --t-data-4: #92580a;
  --t-data-5: #b02244;
  --t-data-6: #5a42d4;
  --t-data-7: #5a6a8a;
  --t-data-8: #2563aa;
  --t-data-9: #8a3a7a;
  --t-data-10: #5a3a9a;

  /* ── Shadows (light) ── */
  --t-shadow-xs: 0px 0px 0px 1px rgba(60, 60, 100, 0.08);
  --t-shadow-sm: 0 1px 2px rgba(60, 60, 100, 0.08), 0 1px 3px rgba(60, 60, 100, 0.05);
  --t-shadow-md: 0 2px 8px rgba(60, 60, 100, 0.08), 0 1px 3px rgba(60, 60, 100, 0.05);
  --t-shadow-lg: 0 8px 24px rgba(60, 60, 100, 0.10), 0 2px 6px rgba(60, 60, 100, 0.06);
  --t-shadow-xl: 0 16px 40px rgba(60, 60, 100, 0.12), 0 4px 10px rgba(60, 60, 100, 0.08);
  --t-shadow-glow-cyan: 0 0 0 1px rgba(8, 145, 178, 0.16), 0 0 12px rgba(8, 145, 178, 0.08);
  --t-shadow-glow-mint: 0 0 0 1px rgba(26, 122, 94, 0.14), 0 0 12px rgba(26, 122, 94, 0.07);
  --t-shadow-glow-danger: 0 0 0 1px rgba(176, 34, 68, 0.18), 0 0 12px rgba(176, 34, 68, 0.08);
  --t-shadow-glow-blue: 0 0 0 1px rgba(27, 127, 232, 0.18), 0 0 12px rgba(27, 127, 232, 0.10);
  --t-shadow-glow: 0 0 12px rgba(89, 101, 217, 0.10);
  --t-shadow-glow-sm: 0 0 6px rgba(89, 101, 217, 0.06);

  /* ── Shell ── */
  --t-sidebar-bg: #1e1e2e;
  --t-sidebar-gradient: linear-gradient(180deg, #1e1e2e 0%, #141419 100%);
  --t-header-bg: rgba(248, 249, 255, 0.95);
  --t-scrollbar: rgba(60, 60, 100, 0.14);
  --t-scrollbar-hover: rgba(60, 60, 100, 0.24);
  --t-selection-bg: rgba(89, 101, 217, 0.18);
}
```

- [ ] **Step 2: Commit**

```bash
git add src/frontend/src/index.css
git commit -m "style(tokens): update light theme to Dynatrace Strato purple-tinted palette"
```

---

## FASE 1 — Core Components

### Task F1-T1: Button

**Files:**
- Modify: `src/frontend/src/components/Button.tsx`

- [ ] **Step 1: Substituir variantClasses e sizeClasses**

```typescript
const variantClasses: Record<NonNullable<ButtonProps['variant']>, string> = {
  institutional:
    'blue-gradient text-white shadow-sm hover:brightness-110 disabled:opacity-40',
  primary:
    'bg-accent text-on-accent shadow-sm hover:bg-accent-hover disabled:opacity-40',
  secondary:
    'bg-elevated text-body border border-edge hover:border-edge-strong hover:bg-hover disabled:opacity-40',
  outline:
    'bg-transparent text-body border border-edge-strong hover:bg-hover hover:text-heading disabled:opacity-40',
  danger:
    'bg-critical/15 text-critical border border-critical/25 hover:bg-critical/20 disabled:opacity-40',
  ghost:
    'text-muted hover:bg-hover hover:text-body disabled:opacity-40',
  subtle:
    'bg-accent-muted text-accent hover:bg-accent/15 hover:text-heading disabled:opacity-40',
};

const sizeClasses: Record<NonNullable<ButtonProps['size']>, string> = {
  xs: 'h-6 px-2 text-xs gap-1',
  sm: 'h-8 px-3 text-xs gap-1.5',
  md: 'h-9 px-4 text-sm gap-2',
  lg: 'h-11 px-6 text-sm font-semibold gap-2',
};
```

- [ ] **Step 2: Atualizar o className base do button**

```typescript
      className={cn(
        'inline-flex items-center justify-center rounded-sm font-medium',
        'transition-all duration-[var(--nto-motion-base)]',
        'focus:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-canvas',
        'disabled:cursor-not-allowed select-none',
        variantClasses[variant],
        sizeClasses[size],
        className,
      )}
```

Mudanças-chave: `rounded-lg` → `rounded-sm` (4px), `font-semibold` → `font-medium`, tamanhos mais compactos.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/components/Button.tsx
git commit -m "style(button): adopt Dynatrace Strato style — solid primary, 4px radius, compact sizing"
```

---

### Task F1-T2: Badge

**Files:**
- Modify: `src/frontend/src/components/Badge.tsx`

- [ ] **Step 1: Substituir variantClasses e sizeClasses**

```typescript
const variantClasses: Record<NonNullable<BadgeProps['variant']>, string> = {
  default:   'bg-elevated text-body border border-edge',
  neutral:   'bg-elevated text-muted border border-edge',
  success:   'bg-success/15 text-success',
  warning:   'bg-warning/15 text-warning',
  danger:    'bg-critical/15 text-critical',
  info:      'bg-info/15 text-info',
  secondary: 'bg-elevated text-muted border border-edge',
  error:     'bg-critical/15 text-critical',
  destructive: 'bg-critical/15 text-critical',
  critical:  'bg-critical/15 text-critical',
  outline:   'bg-transparent text-body border border-edge',
  muted:     'bg-elevated text-muted border border-edge',
  primary:   'bg-accent/15 text-accent',
  blue:      'bg-info/15 text-info',
  gray:      'bg-elevated text-muted border border-edge',
  green:     'bg-success/15 text-success',
  yellow:    'bg-warning/15 text-warning',
  purple:    'bg-accent/15 text-accent',
};

const sizeClasses: Record<NonNullable<BadgeProps['size']>, string> = {
  xs: 'px-1 py-px text-[9px]',
  sm: 'px-1.5 py-px text-[10px]',
  md: 'px-2 py-0.5 text-[11px]',
};
```

- [ ] **Step 2: Atualizar className base — trocar `rounded-pill` por `rounded-[3px]`**

```typescript
      className={cn(
        'inline-flex items-center gap-1 rounded-[3px] font-medium tracking-wide',
        sizeClasses[size],
        variantClasses[variant],
        className,
      )}
```

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/components/Badge.tsx
git commit -m "style(badge): Dynatrace Strato — rectangular 3px radius, semântica Strato"
```

---

### Task F1-T3: Card

**Files:**
- Modify: `src/frontend/src/components/Card.tsx`

- [ ] **Step 1: Substituir variantClasses**

```typescript
const variantClasses: Record<NonNullable<CardProps['variant']>, string> = {
  default:
    'bg-card rounded-md border border-edge shadow-sm overflow-hidden',
  interactive:
    'bg-card rounded-md border border-edge shadow-sm overflow-hidden cursor-pointer hover:shadow-md hover:border-edge-strong transition-all duration-[var(--nto-motion-base)]',
  elevated:
    'bg-elevated rounded-md border border-edge shadow-md overflow-hidden',
  flat:
    'bg-card rounded-md overflow-hidden',
  glass:
    'backdrop-blur-xl bg-white/4 rounded-md border border-white/10 shadow-sm overflow-hidden',
  gradient:
    'rounded-md border-0 shadow-md overflow-hidden text-white bg-gradient-to-br from-accent via-blue to-cyan',
};
```

- [ ] **Step 2: Atualizar CardHeader e CardFooter**

```typescript
export function CardHeader({ children, dot, pulsing, className, ...rest }: CardHeaderProps) {
  return (
    <div className={cn('px-4 py-3 border-b border-edge flex items-center gap-2', className)} {...rest}>
      {dot && (
        <span
          data-testid="card-header-dot"
          style={{
            width: 3,
            height: 20,
            borderRadius: 2,
            background: dot,
            flexShrink: 0,
            animation: pulsing ? 'pulse-badge 1.5s ease-in-out infinite' : undefined,
          }}
          aria-hidden="true"
        />
      )}
      {children}
    </div>
  );
}

export function CardBody({ children, className, ...rest }: HTMLAttributes<HTMLDivElement> & { children: ReactNode }) {
  return <div className={cn('px-4 py-4', className)} {...rest}>{children}</div>;
}

export function CardFooter({ children, className, ...rest }: CardFooterProps) {
  return (
    <div className={cn('px-4 py-2.5 border-t border-edge bg-elevated/20', className)} {...rest}>
      {children}
    </div>
  );
}
```

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/components/Card.tsx
git commit -m "style(card): Dynatrace Strato — 6px radius, subtle border, status vertical line"
```

---

### Task F1-T4: StatCard

**Files:**
- Modify: `src/frontend/src/components/StatCard.tsx`

- [ ] **Step 1: Ler o arquivo e identificar classes de cor**

```bash
cat src/frontend/src/components/StatCard.tsx
```

- [ ] **Step 2: Garantir que o valor principal usa `text-accent` (purple-blue)**

Localizar onde o valor numérico é renderizado. Se houver `text-cyan`, `text-mint`, ou hardcoded colors, substituir por `text-accent` para o valor primário, `text-success` para positivo, `text-critical` para negativo.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/components/StatCard.tsx
git commit -m "style(statcard): primary metric value uses accent (purple-blue)"
```

---

### Task F1-T5: Alert / InlineMessage

**Files:**
- Modify: `src/frontend/src/components/Alert.tsx` (se existir), `src/frontend/src/components/InlineMessage.tsx`

- [ ] **Step 1: Ler os arquivos**

```bash
cat src/frontend/src/components/Alert.tsx
cat src/frontend/src/components/InlineMessage.tsx
```

- [ ] **Step 2: Adicionar padrão Strato de left border vertical**

Para cada variante semântica do Alert, adicionar `border-l-2` com a cor semântica correspondente:

```typescript
// No variantClasses do Alert:
const variantClasses = {
  info:    'bg-info/8 border border-info/20 border-l-2 border-l-info text-heading',
  success: 'bg-success/8 border border-success/20 border-l-2 border-l-success text-heading',
  warning: 'bg-warning/8 border border-warning/20 border-l-2 border-l-warning text-heading',
  error:   'bg-critical/8 border border-critical/20 border-l-2 border-l-critical text-heading',
};
```

- [ ] **Step 3: Trocar border-radius para `rounded-sm` (4px)**

- [ ] **Step 4: Commit**

```bash
git add src/frontend/src/components/Alert.tsx src/frontend/src/components/InlineMessage.tsx
git commit -m "style(alert): Dynatrace Strato left-border semantic pattern"
```

---

### Task F1-T6: Tabs

**Files:**
- Modify: `src/frontend/src/components/Tabs.tsx`

- [ ] **Step 1: Ler o arquivo**

```bash
cat src/frontend/src/components/Tabs.tsx
```

- [ ] **Step 2: Garantir underline style (não pill)**

O tab ativo deve ter `border-b-2 border-accent` no estilo Strato — não background pill/highlight:

```typescript
// Tab item ativo:
'border-b-2 border-accent text-heading font-medium'
// Tab item inativo:
'border-b-2 border-transparent text-muted hover:text-body hover:border-edge-strong'
// Container de tabs:
'flex gap-0 border-b border-edge'
```

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/components/Tabs.tsx
git commit -m "style(tabs): underline style (Dynatrace Strato pattern)"
```

---

### Task F1-T7: Modal / Drawer

**Files:**
- Modify: `src/frontend/src/components/Modal.tsx`

- [ ] **Step 1: Ler o arquivo**

```bash
cat src/frontend/src/components/Modal.tsx
```

- [ ] **Step 2: Atualizar classes do modal panel**

```typescript
// Modal panel:
'bg-card rounded-md border border-edge shadow-floating'
// Header do modal:
'px-5 py-4 border-b border-edge flex items-center justify-between'
// Footer do modal:
'px-5 py-3 border-t border-edge bg-elevated/30 flex items-center justify-end gap-2'
```

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/components/Modal.tsx
git commit -m "style(modal): 6px radius, Strato floating shadow, solid header/footer"
```

---

### Task F1-T8: TextField / Input

**Files:**
- Modify: `src/frontend/src/components/TextField.tsx`

- [ ] **Step 1: Ler o arquivo**

```bash
cat src/frontend/src/components/TextField.tsx
```

- [ ] **Step 2: Atualizar o input base**

```typescript
// Input base class:
'w-full bg-input text-body placeholder:text-muted'
'border border-edge rounded-sm px-3 py-2 text-sm'
'focus:outline-none focus:border-edge-focus focus:ring-1 focus:ring-edge-focus'
'disabled:opacity-50 disabled:cursor-not-allowed'
```

Mudança principal: `rounded-lg` → `rounded-sm` (4px), border usa `--t-edge` padrão Strato.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/components/TextField.tsx
git commit -m "style(textfield): 4px radius, Strato border system"
```

---

## FASE 2 — Shell & Navigation

### Task F2-T1: AppSidebarItem

**Files:**
- Modify: `src/frontend/src/components/shell/AppSidebarItem.tsx`

- [ ] **Step 1: Ler o arquivo**

```bash
cat src/frontend/src/components/shell/AppSidebarItem.tsx
```

- [ ] **Step 2: Atualizar classes de item ativo/inativo/hover**

```typescript
// Item base:
'flex items-center gap-2.5 px-3 py-1.5 rounded-[3px] text-sm transition-colors duration-[var(--nto-motion-fast)]'
// Item inativo:
'text-muted hover:bg-hover hover:text-body'
// Item ativo (isActive):
'bg-accent/10 text-accent font-medium'
// Ícone:
'shrink-0 opacity-70' // inativo
'shrink-0 opacity-100' // ativo
```

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/components/shell/AppSidebarItem.tsx
git commit -m "style(sidebar-item): Dynatrace Strato — purple-blue active, 3px radius, compact"
```

---

### Task F2-T2: AppSidebarFooter

**Files:**
- Modify: `src/frontend/src/components/shell/AppSidebarFooter.tsx`

- [ ] **Step 1: Ler o arquivo**

```bash
cat src/frontend/src/components/shell/AppSidebarFooter.tsx
```

- [ ] **Step 2: Simplificar footer — user info minimalista Strato**

O footer deve ser compacto: avatar circular + nome + email (truncado), nenhuma info redundante.
Border top com `border-t border-edge`, padding `px-3 py-3`, background `bg-canvas`.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/components/shell/AppSidebarFooter.tsx
git commit -m "style(sidebar-footer): simplified Strato compact user footer"
```

---

### Task F2-T3: AppSidebar (grupos e section headers)

**Files:**
- Modify: `src/frontend/src/components/shell/AppSidebar.tsx`

- [ ] **Step 1: Ler linhas 1-200 do AppSidebar**

```bash
head -200 src/frontend/src/components/shell/AppSidebar.tsx
```

- [ ] **Step 2: Garantir near-black background nos containers do sidebar**

O rail e o panel devem usar `bg-canvas` (que agora resolve para `#141419` via token).
Se houver classes hardcoded como `bg-[#0F1E38]` ou `bg-panel`, confirmar que usam variáveis de token.

- [ ] **Step 3: Atualizar section headers (subGroup labels)**

Section headers devem seguir padrão Strato:
```typescript
// Section header (subGroup label):
'px-3 pt-4 pb-1 text-[10px] font-semibold uppercase tracking-[0.08em] text-muted'
```

- [ ] **Step 4: Commit**

```bash
git add src/frontend/src/components/shell/AppSidebar.tsx
git commit -m "style(sidebar): near-black Strato shell, section headers compact"
```

---

### Task F2-T4: AppTopbar

**Files:**
- Modify: `src/frontend/src/components/shell/AppTopbar.tsx`

- [ ] **Step 1: Atualizar height e background**

```typescript
// header element:
className={cn(
  'h-12 border-b border-edge',
  'flex items-center justify-between px-4 lg:px-5 gap-3',
  'sticky top-0 z-[var(--z-header)]',
  'bg-[var(--t-header-bg)]',
)}
```

Mudança: `h-14` → `h-12` (48px Dynatrace padrão), `backdrop-blur-md` removido, background sólido via token.

- [ ] **Step 2: Atualizar `--nto-header-height` no `:root` para `48px`**

(já feito na Task F0-T2, mas verificar se `AppShell.tsx` usa essa variável)

```bash
grep -r "nto-header-height\|h-14\|h-56" src/frontend/src/components/shell/
```

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/components/shell/AppTopbar.tsx
git commit -m "style(topbar): 48px height, solid background, Dynatrace Strato header"
```

---

## FASE 2b — AppSidebarGroup & AppSidebarHeader

### Task F2b-T1: AppSidebarGroup + AppSidebarHeader

**Files:**
- Modify: `src/frontend/src/components/shell/AppSidebarGroup.tsx`
- Modify: `src/frontend/src/components/shell/AppSidebarHeader.tsx`

- [ ] **Step 1: Ler ambos os arquivos**

```bash
cat src/frontend/src/components/shell/AppSidebarGroup.tsx
cat src/frontend/src/components/shell/AppSidebarHeader.tsx
```

- [ ] **Step 2: AppSidebarGroup — seção container**

Garantir que o separador entre grupos usa `border-t border-edge/50 my-1` (Strato divider sutil).

- [ ] **Step 3: AppSidebarHeader — logo área**

Header do sidebar deve ter `h-12` (alinhado com topbar 48px), `border-b border-edge`, `px-3`.

- [ ] **Step 4: Commit**

```bash
git add src/frontend/src/components/shell/AppSidebarGroup.tsx src/frontend/src/components/shell/AppSidebarHeader.tsx
git commit -m "style(sidebar-group/header): Strato dividers, 48px header alignment"
```

---

## Verificação Final

### Task VERIFY: Checar visual no browser

- [ ] **Step 1: Iniciar dev server**

```bash
cd src/frontend && npm run dev
```

Expected: servidor em `http://localhost:5173`, sem erros de compilação.

- [ ] **Step 2: Verificar dark theme**

Abrir a aplicação, confirmar:
- Background: #141419 (near-black, não navy)
- Botão primário: purple-blue sólido (não gradient cyan)
- Badges: retangulares (3px radius)
- Cards: 6px radius com border #3b3b52
- Sidebar: near-black com item ativo purple-blue
- Topbar: 48px, fundo sólido, sem blur

- [ ] **Step 3: Verificar light theme**

Alternar para light theme, confirmar:
- Background: levemente purple-tinted (#f8f9ff)
- Texto: #14142a (escuro com tom purple)
- Primary: #5965d9 (purple escuro legível)

- [ ] **Step 4: Commit de wrap-up**

```bash
git add -A
git commit -m "style: Dynatrace Strato migration F0+F1+F2 complete"
```
