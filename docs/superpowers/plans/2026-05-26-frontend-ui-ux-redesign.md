# Frontend UI/UX Redesign — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the Redesign Expressivo spec — always-dark sidebar, gradient nav state, restored user card, 56px topbar, semantic StatCards, dot indicators, dashed EmptyStates, 4-column KPI dashboard, structured alerts, animated auth orbs, and differentiated chat bubbles.

**Architecture:** 13 atomic tasks in Approach A order: CSS tokens → Shell (sidebar footer + sidebar + topbar) → Shared components (Badge, Card, EmptyState, StatCard) → Dashboard → Auth → Feature pages. Each task produces a self-contained commit. Tests run after every step.

**Tech Stack:** React 19, TypeScript, TailwindCSS v4, Vitest + @testing-library/react, i18next, Lucide React, CSS custom properties.

---

## File Map

| Task | Files Modified |
|------|---------------|
| T1 | `src/frontend/src/index.css` |
| T2 | `src/frontend/src/components/shell/AppSidebarFooter.tsx` |
| T3 | `src/frontend/src/components/shell/AppSidebar.tsx` |
| T4 | `src/frontend/src/components/shell/AppTopbar.tsx` |
| T5 | `src/frontend/src/components/Badge.tsx` · `src/frontend/src/__tests__/components/Badge.test.tsx` |
| T6 | `src/frontend/src/components/Card.tsx` · `src/frontend/src/__tests__/components/Card.test.tsx` |
| T7 | `src/frontend/src/components/EmptyState.tsx` · `src/frontend/src/__tests__/components/EmptyState.test.tsx` |
| T8 | `src/frontend/src/components/MiniSparkline.tsx` · `src/frontend/src/components/StatCard.tsx` · `src/frontend/src/__tests__/components/StatCard.test.tsx` |
| T9 | `src/frontend/src/features/shared/pages/DashboardPage.tsx` |
| T10 | `src/frontend/src/features/identity-access/components/AuthShell.tsx` |
| T11 | `src/frontend/src/locales/en.json` · `src/frontend/src/locales/pt-PT.json` · `src/frontend/src/locales/pt-BR.json` · `src/frontend/src/locales/es.json` |
| T12 | `src/frontend/src/features/ai-hub/components/AssistantMessageBubble.tsx` |
| T13 | `src/frontend/src/features/catalog/components/ServiceCatalogServicesTab.tsx` |

---

## Task 1: CSS Design Tokens

**Files:**
- Modify: `src/frontend/src/index.css`

- [ ] **Step 1.1: Update header height token**

  Find line 294 (`--nto-header-height: 80px`) and change to 56px:

  ```css
  --nto-header-height: 56px;
  ```

- [ ] **Step 1.2: Add pulse-badge keyframe after pulse-soft**

  After the `@keyframes pulse-soft` block (around line 343), add:

  ```css
  @keyframes pulse-badge {
    0%, 100% { opacity: 1; transform: scale(1); }
    50% { opacity: 0.6; transform: scale(0.92); }
  }
  ```

- [ ] **Step 1.3: Add always-dark sidebar context block**

  After the `@keyframes progress-bar` block (around line 357), add:

  ```css
  /* ─── Sidebar — always dark regardless of user theme ───────────────────────
     Adding data-sidebar="dark" to the sidebar root div triggers these overrides.
     All Tailwind utilities inside (text-heading, bg-hover, etc.) resolve to
     dark values, enabling an always-dark sidebar without duplicating classes.
     ────────────────────────────────────────────────────────────────────────── */
  [data-sidebar] {
    --t-canvas: #081120;
    --t-deep: #0A1730;
    --t-panel: #0F1E38;
    --t-card: #0F1E38;
    --t-elevated: #132543;
    --t-subtle: #0D1B32;
    --t-hover: rgba(255, 255, 255, 0.04);
    --t-active: #1A2E50;
    --t-edge: rgba(129, 170, 214, 0.08);
    --t-edge-strong: rgba(129, 170, 214, 0.20);
    --t-divider: rgba(255, 255, 255, 0.06);
    --t-heading: #F2F7FF;
    --t-body: rgba(181, 196, 216, 0.9);
    --t-muted: rgba(129, 170, 214, 0.6);
    --t-faded: rgba(129, 170, 214, 0.4);
    --t-accent: #1B7FE8;
    --t-accent-hover: #1468CC;
    --t-accent-muted: rgba(27, 127, 232, 0.15);
    --t-blue: #1B7FE8;
    --t-blue-muted: rgba(27, 127, 232, 0.15);
    --t-success: #10B981;
    --t-warning: #F59E0B;
    --t-critical: #EF4444;
    --t-sidebar-gradient: linear-gradient(180deg, #0D1C35, #081120);
  }
  ```

- [ ] **Step 1.4: Run tests to verify no regressions**

  ```bash
  cd src/frontend && npm test
  ```
  Expected: All existing tests pass. (CSS-only change — no JS tests affected.)

- [ ] **Step 1.5: Commit**

  ```bash
  git add src/frontend/src/index.css
  git commit -m "style(tokens): header 80px→56px, pulse-badge keyframe, always-dark sidebar context"
  ```

---

## Task 2: AppSidebarFooter — Restore User Card

**Files:**
- Modify: `src/frontend/src/components/shell/AppSidebarFooter.tsx`

- [ ] **Step 2.1: Replace commented-out file with working implementation**

  The file is 100% commented out. Replace the entire file with:

  ```tsx
  import { useState } from 'react';
  import { useTranslation } from 'react-i18next';
  import { LogOut, User, Settings, ChevronUp } from 'lucide-react';
  import { cn } from '../../lib/cn';

  interface AppSidebarFooterProps {
    collapsed?: boolean;
    email?: string;
    persona?: string;
    roleName?: string;
    onLogout: () => void;
  }

  /**
   * AppSidebarFooter — rodapé da sidebar com info do utilizador.
   *
   * Collapsed: avatar (gradiente de marca, 30×30, radius 8px).
   * Expanded: avatar + display name + persona/role + chevron.
   * Clicável → mini-menu com Perfil, Preferências, Logout.
   *
   * Nota: usa cores hardcoded para manter sempre-dark independente do tema.
   */
  export function AppSidebarFooter({
    collapsed = false,
    email,
    persona,
    roleName,
    onLogout,
  }: AppSidebarFooterProps) {
    const { t } = useTranslation();
    const [menuOpen, setMenuOpen] = useState(false);

    const initial = email?.[0]?.toUpperCase() ?? 'U';
    const displayName = email?.split('@')[0] ?? t('common.user');

    const avatarStyle: React.CSSProperties = {
      background: 'linear-gradient(135deg, #1B7FE8, #12C4E8, #18E8B8)',
      borderRadius: '8px',
      width: 30,
      height: 30,
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      flexShrink: 0,
      fontSize: 12,
      fontWeight: 700,
      color: '#FFFFFF',
    };

    return (
      <div className="border-t border-[rgba(129,170,214,0.08)] shrink-0 relative">
        {collapsed ? (
          /* Rail mode — avatar only, click opens menu */
          <div className="p-2 flex justify-center">
            <button
              onClick={() => setMenuOpen((v) => !v)}
              style={avatarStyle}
              title={displayName}
              aria-label={t('shell.userMenu')}
              aria-expanded={menuOpen}
            >
              {initial}
            </button>
          </div>
        ) : (
          /* Expanded mode — avatar + name + role + chevron */
          <button
            onClick={() => setMenuOpen((v) => !v)}
            className={cn(
              'w-full flex items-center gap-2.5 px-3 py-3',
              'rounded-none transition-colors duration-150',
              'hover:bg-[rgba(255,255,255,.04)]',
            )}
            aria-expanded={menuOpen}
            aria-label={t('shell.userMenu')}
          >
            <div style={avatarStyle} aria-hidden="true">
              {initial}
            </div>
            <div className="flex-1 min-w-0 text-left">
              <p style={{ fontSize: 11, fontWeight: 600, color: '#F2F7FF' }} className="truncate leading-tight">
                {displayName}
              </p>
              <p style={{ fontSize: 9, color: 'rgba(142,160,183,.6)' }} className="truncate leading-tight">
                {persona ? t(`persona.${persona}.label`) : ''}
                {roleName ? ` · ${roleName}` : ''}
              </p>
            </div>
            <ChevronUp
              size={14}
              style={{ color: 'rgba(129,170,214,.4)', transform: menuOpen ? 'rotate(180deg)' : 'rotate(0deg)', transition: 'transform 150ms' }}
              aria-hidden="true"
            />
          </button>
        )}

        {/* Mini-menu */}
        {menuOpen && (
          <>
            {/* Backdrop */}
            <div
              className="fixed inset-0 z-[var(--z-dropdown)]"
              onClick={() => setMenuOpen(false)}
              aria-hidden="true"
            />
            <div
              className="absolute bottom-full left-2 right-2 mb-1 rounded-xl overflow-hidden z-[var(--z-dropdown)]"
              style={{
                background: '#0F1E38',
                border: '1px solid rgba(129,170,214,.14)',
                boxShadow: '0 8px 24px rgba(0,0,0,.4)',
              }}
              role="menu"
            >
              <button
                className="w-full flex items-center gap-2.5 px-3 py-2.5 text-left text-[12px] font-medium hover:bg-[rgba(255,255,255,.05)] transition-colors"
                style={{ color: 'rgba(181,196,216,.9)' }}
                onClick={() => setMenuOpen(false)}
                role="menuitem"
              >
                <User size={13} style={{ color: 'rgba(129,170,214,.6)' }} />
                {t('nav.profile', 'Perfil')}
              </button>
              <button
                className="w-full flex items-center gap-2.5 px-3 py-2.5 text-left text-[12px] font-medium hover:bg-[rgba(255,255,255,.05)] transition-colors"
                style={{ color: 'rgba(181,196,216,.9)' }}
                onClick={() => setMenuOpen(false)}
                role="menuitem"
              >
                <Settings size={13} style={{ color: 'rgba(129,170,214,.6)' }} />
                {t('nav.preferences', 'Preferências')}
              </button>
              <div style={{ height: 1, background: 'rgba(129,170,214,.08)', margin: '2px 0' }} />
              <button
                className="w-full flex items-center gap-2.5 px-3 py-2.5 text-left text-[12px] font-medium hover:bg-[rgba(239,68,68,.08)] transition-colors"
                style={{ color: 'rgba(239,68,68,.8)' }}
                onClick={() => { setMenuOpen(false); onLogout(); }}
                role="menuitem"
              >
                <LogOut size={13} />
                {t('auth.signOut')}
              </button>
            </div>
          </>
        )}
      </div>
    );
  }
  ```

- [ ] **Step 2.2: Run tests**

  ```bash
  cd src/frontend && npm test -- --reporter verbose 2>&1 | grep -E "PASS|FAIL|AppSidebarFooter|SidebarComponents"
  ```
  Expected: `SidebarComponents.test.tsx` passes (does not test AppSidebarFooter directly — no snapshot breakage).

- [ ] **Step 2.3: Commit**

  ```bash
  git add src/frontend/src/components/shell/AppSidebarFooter.tsx
  git commit -m "feat(sidebar): restore AppSidebarFooter — dark user card with avatar gradient and mini-menu"
  ```

---

## Task 3: AppSidebar — Always-Dark Shell

**Files:**
- Modify: `src/frontend/src/components/shell/AppSidebar.tsx`

- [ ] **Step 3.1: Uncomment AppSidebarFooter import**

  Find line 8:
  ```tsx
  // import { AppSidebarFooter } from './AppSidebarFooter';
  ```
  Change to:
  ```tsx
  import { AppSidebarFooter } from './AppSidebarFooter';
  ```

- [ ] **Step 3.2: Add data-sidebar attribute + fix icon rail background**

  Find the root `<div` at line 243 (the `return (` block). Add `data-sidebar="dark"` and change the icon rail style:

  **Root wrapper** — add `data-sidebar="dark"`:
  ```tsx
  <div
    data-sidebar="dark"
    className={cn(
      'flex h-full',
      !mobile && 'fixed inset-y-0 left-0 z-[var(--z-header)]',
      !mobile && 'transition-[width] duration-[var(--nto-motion-medium)] ease-[var(--ease-standard)]',
      mobile && 'w-[320px]',
      className,
    )}
    style={{
      ...(!mobile ? { width: collapsed ? SIDEBAR_WIDTH_COLLAPSED : SIDEBAR_WIDTH_EXPANDED } : {}),
    }}
    role="navigation"
    aria-label={t('shell.sidebarNav')}
  >
  ```

  **Icon rail** — change `background: 'var(--t-sidebar-gradient)'` to hardcoded dark:
  ```tsx
  <div
    className="flex flex-col h-full shrink-0 border-r border-edge"
    style={{ width: SIDEBAR_RAIL_WIDTH, background: 'linear-gradient(180deg, #0F1E38, #081120)' }}
  >
  ```

  **Content panel** — change `background: 'var(--t-sidebar-gradient)'` to hardcoded dark:
  ```tsx
  style={{
    ...(!collapsed || mobile ? { width: SIDEBAR_CONTENT_WIDTH } : {}),
    background: 'linear-gradient(180deg, #0D1C35, #081120)',
  }}
  ```

- [ ] **Step 3.3: Update icon rail active state to spec values**

  Find the `isActive ?` ternary in the icon rail button (around line 298). Replace:
  ```tsx
  isActive
    ? 'bg-blue/15 text-blue shadow-glow-blue'
    : isHighlighted
      ? 'text-cyan hover:bg-hover hover:text-cyan'
      : 'text-muted hover:bg-hover hover:text-body',
  ```
  With:
  ```tsx
  isActive
    ? 'bg-[rgba(27,127,232,.25)] text-[#3D96F2] shadow-[0_0_12px_rgba(27,127,232,.18),inset_0_0_0_1px_rgba(27,127,232,.4)]'
    : isHighlighted
      ? 'text-cyan hover:bg-hover hover:text-cyan'
      : 'text-[rgba(129,170,214,.5)] hover:bg-[rgba(255,255,255,.04)] hover:text-[rgba(129,170,214,.8)]',
  ```

- [ ] **Step 3.4: Update content panel nav item active state**

  Find the NavLink `className` function (around line 395). Replace:
  ```tsx
  isActive
    ? 'bg-blue text-white font-medium shadow-sm'
    : item.preview
      ? 'text-muted/50 hover:bg-hover hover:text-muted'
      : 'text-body hover:bg-hover hover:text-heading font-normal',
  ```
  With:
  ```tsx
  isActive
    ? 'bg-[linear-gradient(90deg,rgba(27,127,232,.22),rgba(18,196,232,.08))] text-[#EAF2FF] font-medium shadow-[inset_2px_0_0_#1B7FE8]'
    : item.preview
      ? 'text-[rgba(129,170,214,.4)] hover:bg-[rgba(255,255,255,.04)] hover:text-[rgba(181,196,216,.8)]'
      : 'text-[rgba(181,196,216,.6)] hover:bg-[rgba(255,255,255,.04)] hover:text-[#EAF2FF] font-normal',
  ```

- [ ] **Step 3.5: Push admin section to bottom of rail**

  In the icon rail section groups map (around line 273), change:
  ```tsx
  {sectionGroups.map((group, gi) => (
    <div key={gi}>
      {gi > 0 && (
        <div className="w-6 h-px bg-edge mx-auto my-3" />
      )}
  ```
  To:
  ```tsx
  {sectionGroups.map((group, gi) => (
    <div key={gi} className={gi === sectionGroups.length - 1 ? 'mt-auto' : undefined}>
      {gi > 0 && (
        <div className="w-6 h-px bg-edge mx-auto my-3" />
      )}
  ```

- [ ] **Step 3.6: Uncomment both AppSidebarFooter usages**

  **First usage** (icon rail, around line 333):
  ```tsx
  {/* User avatar — rail mode */}
  {/* <AppSidebarFooter
    collapsed
    email={user?.email}
    persona={persona}
    roleName={roleName}
    onLogout={handleLogout}
  /> */}
  ```
  Change to:
  ```tsx
  {/* User avatar — rail mode */}
  <AppSidebarFooter
    collapsed
    email={user?.email}
    persona={persona}
    roleName={roleName}
    onLogout={handleLogout}
  />
  ```

  **Second usage** (content panel, around line 427):
  ```tsx
  {/* User card — expanded mode */}
  {/* <AppSidebarFooter
    collapsed={false}
    email={user?.email}
    persona={persona}
    roleName={roleName}
    onLogout={handleLogout}
  /> */}
  ```
  Change to:
  ```tsx
  {/* User card — expanded mode */}
  <AppSidebarFooter
    collapsed={false}
    email={user?.email}
    persona={persona}
    roleName={roleName}
    onLogout={handleLogout}
  />
  ```

- [ ] **Step 3.7: Run tests**

  ```bash
  cd src/frontend && npm test -- --reporter verbose 2>&1 | grep -E "PASS|FAIL|AppShell|ShellComponents|SidebarComponents"
  ```
  Expected: All shell tests pass.

- [ ] **Step 3.8: Commit**

  ```bash
  git add src/frontend/src/components/shell/AppSidebar.tsx
  git commit -m "feat(sidebar): always-dark shell, gradient active state, admin at bottom, footer restored"
  ```

---

## Task 4: AppTopbar — 56px Height

**Files:**
- Modify: `src/frontend/src/components/shell/AppTopbar.tsx`

- [ ] **Step 4.1: Reduce topbar height**

  In `AppTopbar.tsx`, find:
  ```tsx
  'h-20 border-b border-edge',
  ```
  Change to:
  ```tsx
  'h-14 border-b border-edge',
  ```

- [ ] **Step 4.2: Reduce divider height**

  Find:
  ```tsx
  <div className="w-px h-6 bg-edge mx-1.5" aria-hidden="true" />
  ```
  Change to:
  ```tsx
  <div className="w-px h-5 bg-edge mx-1.5" aria-hidden="true" />
  ```

- [ ] **Step 4.3: Run tests**

  ```bash
  cd src/frontend && npm test -- --reporter verbose 2>&1 | grep -E "PASS|FAIL|AppShell|AppTopbar"
  ```
  Expected: Pass.

- [ ] **Step 4.4: Commit**

  ```bash
  git add src/frontend/src/components/shell/AppTopbar.tsx
  git commit -m "style(topbar): reduce height from h-20 (80px) to h-14 (56px)"
  ```

---

## Task 5: Badge — Dot Indicator + Pulsing

**Files:**
- Modify: `src/frontend/src/components/Badge.tsx`
- Modify: `src/frontend/src/__tests__/components/Badge.test.tsx`

- [ ] **Step 5.1: Write the failing tests first**

  Open `src/frontend/src/__tests__/components/Badge.test.tsx`. Add after the last existing test:

  ```tsx
  it('renderiza dot indicator quando dot=true', () => {
    const { container } = render(<Badge dot>Active</Badge>);
    const dot = container.querySelector('[data-testid="badge-dot"]');
    expect(dot).toBeInTheDocument();
    expect(dot).toHaveStyle({ width: '5px', height: '5px', borderRadius: '50%' });
  });

  it('não renderiza dot quando dot=false (default)', () => {
    const { container } = render(<Badge>No dot</Badge>);
    expect(container.querySelector('[data-testid="badge-dot"]')).not.toBeInTheDocument();
  });

  it('aplica animação pulsing quando pulsing=true', () => {
    const { container } = render(<Badge dot pulsing>Critical</Badge>);
    const dot = container.querySelector('[data-testid="badge-dot"]');
    expect(dot).toHaveStyle({ animation: 'pulse-badge 1.5s ease-in-out infinite' });
  });
  ```

- [ ] **Step 5.2: Run tests to confirm failure**

  ```bash
  cd src/frontend && npx vitest run src/__tests__/components/Badge.test.tsx --reporter verbose
  ```
  Expected: FAIL — `dot` and `pulsing` props not yet on Badge component.

- [ ] **Step 5.3: Implement dot + pulsing in Badge.tsx**

  Replace the full `Badge.tsx` with:

  ```tsx
  import { memo } from 'react';
  import type { ReactNode } from 'react';
  import { cn } from '../lib/cn';

  export interface BadgeProps {
    children: ReactNode;
    variant?: 'default' | 'neutral' | 'success' | 'warning' | 'danger' | 'info'
      // Aliases for legacy call sites
      | 'secondary' | 'error' | 'destructive' | 'critical' | 'outline' | 'muted'
      | 'primary' | 'blue' | 'gray' | 'green' | 'yellow' | 'purple';
    size?: 'xs' | 'sm' | 'md';
    icon?: ReactNode;
    /** Exibe um indicador circular (5×5px) antes do texto, na cor da variante. */
    dot?: boolean;
    /** Anima o dot com pulse (requer dot=true). Útil para estados críticos/activos. */
    pulsing?: boolean;
    className?: string;
  }

  /**
   * Badge semântico — DESIGN-SYSTEM.md §4.9
   * Radius pill, fundo translúcido tonal por variante semântica + borda suave.
   * Alinhado com padrão Template NexLink: border visível, fundo tonal.
   * Altura: 24-28px (md) ou 20-22px (sm), peso 500-600.
   *
   * dot=true: indicador circular (5×5px, background: currentColor) antes do texto.
   * pulsing=true: anima o dot com pulse-badge 1.5s ease-in-out infinite.
   */
  const variantClasses: Record<NonNullable<BadgeProps['variant']>, string> = {
    default: 'bg-elevated text-body border border-edge',
    neutral: 'bg-elevated text-muted border border-edge',
    success: 'bg-success/12 text-success border border-success/25',
    warning: 'bg-warning/12 text-warning border border-warning/25',
    danger: 'bg-critical/12 text-critical border border-critical/25',
    info: 'bg-info/12 text-info border border-info/25',
    // Aliases
    secondary: 'bg-elevated text-muted border border-edge',
    error: 'bg-critical/12 text-critical border border-critical/25',
    destructive: 'bg-critical/12 text-critical border border-critical/25',
    critical: 'bg-critical/12 text-critical border border-critical/25',
    outline: 'bg-transparent text-body border border-edge',
    muted: 'bg-elevated text-muted border border-edge',
    primary: 'bg-info/12 text-info border border-info/25',
    blue: 'bg-info/12 text-info border border-info/25',
    gray: 'bg-elevated text-muted border border-edge',
    green: 'bg-success/12 text-success border border-success/25',
    yellow: 'bg-warning/12 text-warning border border-warning/25',
    purple: 'bg-info/12 text-info border border-info/25',
  };

  const sizeClasses: Record<NonNullable<BadgeProps['size']>, string> = {
    xs: 'px-1 py-px text-[10px]',
    sm: 'px-1.5 py-px type-micro',
    md: 'px-2.5 py-0.5 text-xs',
  };

  export const Badge = memo(function Badge({
    children,
    variant = 'default',
    size = 'md',
    icon,
    dot,
    pulsing,
    className,
  }: BadgeProps) {
    return (
      <span
        className={cn(
          'inline-flex items-center gap-1 rounded-pill font-semibold',
          sizeClasses[size],
          variantClasses[variant],
          className,
        )}
      >
        {dot && (
          <span
            data-testid="badge-dot"
            style={{
              width: 5,
              height: 5,
              borderRadius: '50%',
              background: 'currentColor',
              flexShrink: 0,
              animation: pulsing ? 'pulse-badge 1.5s ease-in-out infinite' : undefined,
            }}
            aria-hidden="true"
          />
        )}
        {icon && !dot && <span className="shrink-0" aria-hidden="true">{icon}</span>}
        {children}
      </span>
    );
  });
  ```

- [ ] **Step 5.4: Run tests to verify they pass**

  ```bash
  cd src/frontend && npx vitest run src/__tests__/components/Badge.test.tsx --reporter verbose
  ```
  Expected: All 10 tests PASS.

- [ ] **Step 5.5: Run full test suite**

  ```bash
  cd src/frontend && npm test 2>&1 | tail -5
  ```
  Expected: No new failures.

- [ ] **Step 5.6: Commit**

  ```bash
  git add src/frontend/src/components/Badge.tsx src/frontend/src/__tests__/components/Badge.test.tsx
  git commit -m "feat(badge): dot indicator + pulsing animation props"
  ```

---

## Task 6: Card — Colored Dot in CardHeader

**Files:**
- Modify: `src/frontend/src/components/Card.tsx`
- Modify: `src/frontend/src/__tests__/components/Card.test.tsx`

- [ ] **Step 6.1: Write failing tests**

  Open `src/frontend/src/__tests__/components/Card.test.tsx`. Add after existing tests:

  ```tsx
  import { CardHeader } from '../../components/Card';

  describe('CardHeader dot indicator', () => {
    it('renderiza dot quando dot prop é fornecida', () => {
      const { container } = render(<CardHeader dot="#1B7FE8">Serviços</CardHeader>);
      const dot = container.querySelector('[data-testid="card-header-dot"]');
      expect(dot).toBeInTheDocument();
      expect(dot).toHaveStyle({ background: '#1B7FE8' });
    });

    it('não renderiza dot quando prop omitida', () => {
      const { container } = render(<CardHeader>Sem dot</CardHeader>);
      expect(container.querySelector('[data-testid="card-header-dot"]')).not.toBeInTheDocument();
    });

    it('anima o dot quando pulsing=true', () => {
      const { container } = render(<CardHeader dot="#EF4444" pulsing>Incidentes</CardHeader>);
      const dot = container.querySelector('[data-testid="card-header-dot"]');
      expect(dot).toHaveStyle({ animation: 'pulse-badge 1.5s ease-in-out infinite' });
    });
  });
  ```

- [ ] **Step 6.2: Run tests to confirm failure**

  ```bash
  cd src/frontend && npx vitest run src/__tests__/components/Card.test.tsx --reporter verbose
  ```
  Expected: FAIL on the 3 new tests.

- [ ] **Step 6.3: Update CardHeader interface + implementation in Card.tsx**

  Find the `CardHeaderProps` interface:
  ```tsx
  export interface CardHeaderProps extends HTMLAttributes<HTMLDivElement> {
    children: ReactNode;
  }
  ```
  Change to:
  ```tsx
  export interface CardHeaderProps extends HTMLAttributes<HTMLDivElement> {
    children: ReactNode;
    /** Cor CSS do dot indicator (ex: 'var(--t-cyan)' ou '#1B7FE8'). Omitir para sem dot. */
    dot?: string;
    /** Anima o dot com pulse (requer dot). Para cards de incidentes/críticos. */
    pulsing?: boolean;
  }
  ```

  Find the `CardHeader` function:
  ```tsx
  export function CardHeader({ children, className, ...rest }: CardHeaderProps) {
    return (
      <div className={cn('px-5 py-4 border-b border-edge/60', className)} {...rest}>
        {children}
      </div>
    );
  }
  ```
  Change to:
  ```tsx
  export function CardHeader({ children, dot, pulsing, className, ...rest }: CardHeaderProps) {
    return (
      <div className={cn('px-5 py-4 border-b border-edge/60 flex items-center gap-2.5', className)} {...rest}>
        {dot && (
          <span
            data-testid="card-header-dot"
            style={{
              width: 7,
              height: 7,
              borderRadius: '50%',
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
  ```

- [ ] **Step 6.4: Run tests to verify they pass**

  ```bash
  cd src/frontend && npx vitest run src/__tests__/components/Card.test.tsx --reporter verbose
  ```
  Expected: All tests PASS.

- [ ] **Step 6.5: Commit**

  ```bash
  git add src/frontend/src/components/Card.tsx src/frontend/src/__tests__/components/Card.test.tsx
  git commit -m "feat(card): colored dot indicator + pulsing prop in CardHeader"
  ```

---

## Task 7: EmptyState — Dashed Accent Border

**Files:**
- Modify: `src/frontend/src/components/EmptyState.tsx`
- Modify: `src/frontend/src/__tests__/components/EmptyState.test.tsx`

- [ ] **Step 7.1: Write failing test**

  Open `src/frontend/src/__tests__/components/EmptyState.test.tsx`. Add:

  ```tsx
  it('aplica borda dashed accent no ícone default', () => {
    const { container } = render(<EmptyState title="Vazio" />);
    const iconWrapper = container.querySelector('[data-testid="empty-state-icon"]');
    expect(iconWrapper).toBeInTheDocument();
    // Dashed border styles are applied via style attribute
    expect(iconWrapper).toHaveAttribute('data-variant', 'default');
  });
  ```

- [ ] **Step 7.2: Run to confirm failure**

  ```bash
  cd src/frontend && npx vitest run src/__tests__/components/EmptyState.test.tsx --reporter verbose
  ```
  Expected: FAIL — `data-testid="empty-state-icon"` does not exist yet.

- [ ] **Step 7.3: Update EmptyState.tsx**

  Replace the `variantConfig` and the icon container in `EmptyState.tsx`:

  ```tsx
  import type { ReactNode } from 'react';
  import { Inbox, AlertCircle, Rocket, ShieldX } from 'lucide-react';
  import { cn } from '../lib/cn';

  type EmptyStateVariant = 'default' | 'error' | 'onboarding' | 'permission-denied';

  interface EmptyStateProps {
    icon?: ReactNode;
    title: string;
    description?: string;
    action?: ReactNode;
    size?: 'default' | 'compact';
    /** Semantic variant — controls default icon and accent color. */
    variant?: EmptyStateVariant;
  }

  const variantConfig: Record<EmptyStateVariant, {
    icon: ReactNode;
    /** Inline style for icon wrapper — dashed accent borders. */
    iconStyle: React.CSSProperties;
    iconColor: string;
  }> = {
    default: {
      icon: <Inbox size={24} aria-hidden="true" />,
      iconStyle: {
        border: '1.5px dashed rgba(27,127,232,.25)',
        background: 'rgba(27,127,232,.06)',
        borderRadius: 14,
      },
      iconColor: 'text-accent',
    },
    error: {
      icon: <AlertCircle size={24} aria-hidden="true" />,
      iconStyle: {
        border: '1.5px dashed rgba(220,38,38,.25)',
        background: 'rgba(220,38,38,.06)',
        borderRadius: 14,
      },
      iconColor: 'text-critical',
    },
    onboarding: {
      icon: <Rocket size={24} aria-hidden="true" />,
      iconStyle: {
        border: '1.5px dashed rgba(8,145,178,.25)',
        background: 'rgba(8,145,178,.06)',
        borderRadius: 14,
      },
      iconColor: 'text-info',
    },
    'permission-denied': {
      icon: <ShieldX size={24} aria-hidden="true" />,
      iconStyle: {
        border: '1.5px dashed rgba(217,119,6,.25)',
        background: 'rgba(217,119,6,.06)',
        borderRadius: 14,
      },
      iconColor: 'text-warning',
    },
  };

  /**
   * Estado vazio — DESIGN-SYSTEM.md §4.13
   * Título + explicação + ação recomendada. Nunca genérico, sempre contextual.
   *
   * Ícone com borda dashed accent (1.5px) + fundo tonal (6% opacity).
   * CTA inline fornecido pelo consumidor via `action` prop.
   */
  export function EmptyState({ icon, title, description, action, size = 'default', variant = 'default' }: EmptyStateProps) {
    const config = variantConfig[variant];

    return (
      <div className={cn(
        'flex flex-col items-center justify-center text-center animate-fade-in',
        size === 'compact' ? 'py-8 px-4' : 'py-16 px-6',
      )}>
        <div
          data-testid="empty-state-icon"
          data-variant={variant}
          className={cn(
            'flex items-center justify-center mb-4',
            config.iconColor,
            size === 'compact' ? 'w-10 h-10' : 'w-14 h-14',
          )}
          style={config.iconStyle}
        >
          {icon ?? (
            size === 'compact'
              ? <span className="[&>svg]:w-[18px] [&>svg]:h-[18px]">{config.icon}</span>
              : config.icon
          )}
        </div>
        <h3 className="text-sm font-semibold text-heading mb-1">{title}</h3>
        {description && (
          <p className="text-xs text-muted max-w-xs mb-4">{description}</p>
        )}
        {action}
      </div>
    );
  }
  ```

- [ ] **Step 7.4: Run tests**

  ```bash
  cd src/frontend && npx vitest run src/__tests__/components/EmptyState.test.tsx --reporter verbose
  ```
  Expected: All 8 tests PASS.

- [ ] **Step 7.5: Commit**

  ```bash
  git add src/frontend/src/components/EmptyState.tsx src/frontend/src/__tests__/components/EmptyState.test.tsx
  git commit -m "style(empty-state): dashed accent border + tonal bg on icon container"
  ```

---

## Task 8: StatCard — Semantic Border-Top + Tonal Icon + Bar Sparkline

**Files:**
- Modify: `src/frontend/src/components/MiniSparkline.tsx`
- Modify: `src/frontend/src/components/StatCard.tsx`
- Modify: `src/frontend/src/__tests__/components/StatCard.test.tsx`

- [ ] **Step 8.1: Write failing tests**

  Open `src/frontend/src/__tests__/components/StatCard.test.tsx`. Add:

  ```tsx
  import { render, screen } from '@testing-library/react';
  import { MemoryRouter } from 'react-router-dom';

  it('renderiza border-top semântico quando topBorderColor fornecido', () => {
    const { container } = render(
      <MemoryRouter>
        <StatCard
          title="Serviços"
          value={42}
          icon={<Activity size={22} />}
          topBorderColor="var(--t-cyan)"
        />
      </MemoryRouter>
    );
    const card = container.firstChild?.firstChild as HTMLElement;
    expect(card?.style.borderTop).toBe('3px solid var(--t-cyan)');
  });

  it('renderiza valor com cor da variante quando coloredValue=true', () => {
    const { container } = render(
      <MemoryRouter>
        <StatCard
          title="Incidentes"
          value={5}
          icon={<Activity size={22} />}
          color="text-critical"
          coloredValue
        />
      </MemoryRouter>
    );
    const value = screen.getByText('5');
    expect(value).toHaveClass('text-critical');
  });
  ```

- [ ] **Step 8.2: Run to confirm failure**

  ```bash
  cd src/frontend && npx vitest run src/__tests__/components/StatCard.test.tsx --reporter verbose
  ```
  Expected: FAIL — `topBorderColor` and `coloredValue` props not yet on StatCard.

- [ ] **Step 8.3: Add bars support to MiniSparkline.tsx**

  In `MiniSparkline.tsx`, add a `type` prop and bars rendering. Replace the entire file:

  ```tsx
  import { useId, useMemo } from 'react';
  import { cn } from '../lib/cn';

  export interface MiniSparklineProps {
    /** Valores numéricos para o sparkline. */
    data: number[];
    /** 'line' renderiza traço contínuo; 'bars' renderiza barras verticais. */
    type?: 'line' | 'bars';
    /** Largura do SVG. */
    width?: number;
    /** Altura do SVG. */
    height?: number;
    /** Cor do traço/barras — CSS variable ou hex. */
    color?: string;
    /** Mostrar preenchimento abaixo da linha (só para type='line'). */
    filled?: boolean;
    className?: string;
  }

  /**
   * MiniSparkline — gráfico inline minimalista para stat cards.
   *
   * type='line' (padrão): traço com área preenchida abaixo.
   * type='bars': barras verticais com opacidade crescente da esquerda para a direita
   *   (histórico mais antigo mais transparente, valor actual mais opaco).
   */
  export function MiniSparkline({
    data,
    type = 'line',
    width = 80,
    height = 32,
    color = 'var(--t-accent)',
    filled = true,
    className,
  }: MiniSparklineProps) {
    const gradientId = `sparkline-${useId().replace(/:/g, '')}`;

    const linePathData = useMemo(() => {
      if (!data.length || type !== 'line') return { line: '', area: '' };
      const min = Math.min(...data);
      const max = Math.max(...data);
      const range = max - min || 1;
      const padding = 2;
      const drawWidth = width - padding * 2;
      const drawHeight = height - padding * 2;
      const points = data.map((v, i) => ({
        x: padding + (i / Math.max(data.length - 1, 1)) * drawWidth,
        y: padding + drawHeight - ((v - min) / range) * drawHeight,
      }));
      const line = points
        .map((p, i) => `${i === 0 ? 'M' : 'L'}${p.x.toFixed(1)},${p.y.toFixed(1)}`)
        .join(' ');
      const area = `${line} L${points[points.length - 1].x.toFixed(1)},${height} L${points[0].x.toFixed(1)},${height} Z`;
      return { line, area };
    }, [data, type, width, height]);

    if (!data.length) return null;

    if (type === 'bars') {
      const min = Math.min(...data);
      const max = Math.max(...data);
      const range = max - min || 1;
      const padding = 2;
      const totalBars = data.length;
      const barW = Math.floor((width - padding * 2) / totalBars * 0.65);
      const barGap = Math.floor((width - padding * 2) / totalBars * 0.35);
      const drawHeight = height - padding * 2;

      return (
        <svg
          width={width}
          height={height}
          viewBox={`0 0 ${width} ${height}`}
          className={cn('shrink-0', className)}
          aria-hidden="true"
        >
          {data.map((v, i) => {
            const barH = Math.max(2, ((v - min) / range) * drawHeight);
            const opacity = totalBars > 1 ? 0.25 + (i / (totalBars - 1)) * 0.75 : 1;
            const x = padding + i * (barW + barGap);
            const y = padding + drawHeight - barH;
            return (
              <rect
                key={i}
                x={x}
                y={y}
                width={barW}
                height={barH}
                fill={color}
                opacity={opacity}
                rx={1.5}
              />
            );
          })}
        </svg>
      );
    }

    // type === 'line'
    return (
      <svg
        width={width}
        height={height}
        viewBox={`0 0 ${width} ${height}`}
        className={cn('shrink-0', className)}
        aria-hidden="true"
      >
        {filled && (
          <defs>
            <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor={color} stopOpacity={0.2} />
              <stop offset="100%" stopColor={color} stopOpacity={0.02} />
            </linearGradient>
          </defs>
        )}
        {filled && linePathData.area && (
          <path d={linePathData.area} fill={`url(#${gradientId})`} />
        )}
        {linePathData.line && (
          <path
            d={linePathData.line}
            fill="none"
            stroke={color}
            strokeWidth={1.5}
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        )}
      </svg>
    );
  }
  ```

- [ ] **Step 8.4: Update StatCard.tsx**

  Replace the full `StatCard.tsx`:

  ```tsx
  import { Link } from 'react-router-dom';
  import { MoreVertical, ArrowRight } from 'lucide-react';
  import { cn } from '../lib/cn';
  import { TrendBadge } from './TrendBadge';
  import type { MiniSparklineProps } from './MiniSparkline';
  import { MiniSparkline } from './MiniSparkline';

  /** Maps Tailwind color class to its CSS variable for border-top. */
  const colorToCssVar: Record<string, string> = {
    'text-cyan': 'var(--t-cyan)',
    'text-success': 'var(--t-success)',
    'text-warning': 'var(--t-warning)',
    'text-critical': 'var(--t-critical)',
    'text-accent': 'var(--t-accent)',
    'text-blue': 'var(--t-blue)',
    'text-info': 'var(--t-info)',
    'text-mint': 'var(--t-mint)',
  };

  /** Maps Tailwind color class to its Tailwind bg tonal class (8% opacity). */
  const colorToTonalBg: Record<string, string> = {
    'text-cyan': 'bg-cyan/8',
    'text-success': 'bg-success/8',
    'text-warning': 'bg-warning/8',
    'text-critical': 'bg-critical/8',
    'text-accent': 'bg-accent/8',
    'text-blue': 'bg-blue/8',
    'text-info': 'bg-info/8',
    'text-mint': 'bg-mint/8',
  };

  interface StatCardProps {
    title: string;
    value: string | number;
    icon?: React.ReactNode;
    color?: string;
    trend?: { direction: 'up' | 'down'; label: string };
    href?: string;
    ariaLabel?: string;
    context?: string;
    sparkline?: Pick<MiniSparklineProps, 'data' | 'color'>;
    footer?: string;
    footerHref?: string;
    actions?: Array<{ label: string; onClick: () => void }>;
    /** CSS color value for the top border (ex: 'var(--t-cyan)' ou '#1B7FE8'). */
    topBorderColor?: string;
    /** Quando true, renderiza o valor com a cor semântica do `color` prop. */
    coloredValue?: boolean;
  }

  /**
   * KPI Card — DESIGN-SYSTEM.md §4.7
   * Título + métrica principal + tendência opcional.
   *
   * border-top: 3px solid {cor semântica}
   * Ícone com área tonal (cor/8) em vez de bg-elevated.
   * Valor em cor semântica quando coloredValue=true.
   * Sparkline de barras com opacidade crescente.
   */
  export function StatCard({
    title,
    value,
    icon,
    color = 'text-accent',
    trend,
    href,
    ariaLabel,
    context,
    sparkline,
    footer,
    footerHref,
    actions,
    topBorderColor,
    coloredValue = false,
  }: StatCardProps) {
    const tonalBg = colorToTonalBg[color] ?? 'bg-elevated';

    const content = (
      <div
        className={cn(
          'bg-card rounded-2xl border border-edge shadow-surface',
          'flex flex-col',
          'transition-all duration-[var(--nto-motion-base)]',
          'hover:border-edge-strong hover:shadow-elevated hover:-translate-y-0.5',
          href && 'cursor-pointer',
          'group',
        )}
        style={topBorderColor ? { borderTop: `3px solid ${topBorderColor}` } : undefined}
        aria-label={ariaLabel ?? `${title}: ${value}`}
        role="group"
      >
        {/* Corpo principal */}
        <div className="p-5 flex items-start gap-4 flex-1">
          {/* Ícone com fundo tonal da cor semântica */}
          <div
            className={cn(
              'shrink-0 w-11 h-11 rounded-xl flex items-center justify-center',
              tonalBg,
              'transition-all duration-[var(--nto-motion-base)]',
              color,
            )}
            aria-hidden="true"
          >
            {icon}
          </div>

          <div className="min-w-0 flex-1">
            {/* Header row: title + actions */}
            <div className="flex items-start justify-between gap-2">
              <p className="text-xs font-medium text-muted truncate mb-1">{title}</p>
              {actions && actions.length > 0 && (
                <div className="relative group/menu shrink-0">
                  <button
                    className="p-1 -mt-1 -mr-1 rounded-md text-muted hover:text-heading hover:bg-hover transition-colors opacity-0 group-hover:opacity-100"
                    aria-label="More options"
                    onClick={(e) => {
                      e.preventDefault();
                      e.stopPropagation();
                    }}
                  >
                    <MoreVertical size={14} />
                  </button>
                </div>
              )}
            </div>

            {/* Value + sparkline row */}
            <div className="flex items-end justify-between gap-2">
              <div>
                <p className={cn(
                  'text-2xl font-bold tabular-nums leading-none',
                  coloredValue ? color : 'text-heading',
                )}>
                  {value}
                </p>
                {trend && (
                  <TrendBadge
                    direction={trend.direction}
                    value={trend.label}
                    size="sm"
                    className="mt-2"
                  />
                )}
                {context && (
                  <p className="text-[11px] mt-1.5 text-muted">{context}</p>
                )}
              </div>
              {sparkline && (
                <MiniSparkline
                  data={sparkline.data}
                  color={sparkline.color ?? (topBorderColor ?? colorToCssVar[color] ?? 'var(--t-accent)')}
                  type="bars"
                  width={72}
                  height={28}
                  className="opacity-70 group-hover:opacity-100 transition-opacity"
                />
              )}
            </div>
          </div>
        </div>

        {/* Rodapé com contexto de comparação */}
        {footer && (
          <div className="px-5 py-2.5 border-t border-edge/40 flex items-center justify-between">
            <p className="text-[11px] text-muted">{footer}</p>
            {footerHref && (
              <Link
                to={footerHref}
                className="text-accent hover:text-accent-hover transition-colors"
                onClick={(e) => e.stopPropagation()}
              >
                <ArrowRight size={14} />
              </Link>
            )}
          </div>
        )}
      </div>
    );

    if (href) {
      return <Link to={href}>{content}</Link>;
    }

    return content;
  }
  ```

- [ ] **Step 8.5: Run StatCard tests**

  ```bash
  cd src/frontend && npx vitest run src/__tests__/components/StatCard.test.tsx --reporter verbose
  ```
  Expected: All tests PASS (including the 2 new ones).

- [ ] **Step 8.6: Run full test suite**

  ```bash
  cd src/frontend && npm test 2>&1 | tail -10
  ```
  Expected: No new failures.

- [ ] **Step 8.7: Commit**

  ```bash
  git add src/frontend/src/components/MiniSparkline.tsx src/frontend/src/components/StatCard.tsx src/frontend/src/__tests__/components/StatCard.test.tsx
  git commit -m "feat(stat-card): semantic border-top, tonal icon bg, colored value + MiniSparkline bar type"
  ```

---

## Task 9: Dashboard — 4 KPIs + Structured Alerts

**Files:**
- Modify: `src/frontend/src/features/shared/pages/DashboardPage.tsx`

- [ ] **Step 9.1: Remove registeredApis stat and change to 4 columns**

  In `DashboardPage.tsx`, find the `stats` array (around line 79). Remove the last object in the array:
  ```tsx
  // Remove this entire object:
  {
    title: t('dashboard.registeredApis'),
    value: graphLoading ? '…' : totalApis,
    icon: <GitBranch size={22} />,
    color: 'text-accent',
    sparkline: totalApis > 0 ? { data: [1, 2, 3, 4, 5, totalApis], color: 'var(--t-accent)' } : undefined,
  },
  ```

  Also remove the `totalApis` variable and its type import if unused (leave `graph?.apis?.length` if it's used elsewhere; if not, remove).

  Find `const totalApis = graph?.apis?.length ?? 0;` and remove it.

  Update the 4 remaining stats to include `topBorderColor` and `coloredValue` + 7 data points for sparklines:

  ```tsx
  const stats = [
    {
      title: t('dashboard.activeServices'),
      value: graphLoading ? '…' : totalServices,
      icon: <Activity size={22} />,
      color: 'text-cyan',
      topBorderColor: 'var(--t-cyan)',
      coloredValue: true,
      trend: totalServices > 0 ? { direction: 'up' as const, label: t('dashboard.trendHealthy') } : undefined,
      sparkline: totalServices > 0
        ? { data: [2, 3, 5, 8, 12, 15, totalServices], color: 'var(--t-cyan)' }
        : undefined,
      footer: totalServices > 0 ? t('dashboard.vsLastPeriod', { value: Math.max(0, totalServices - 2) }) : undefined,
      footerHref: '/services',
    },
    {
      title: t('dashboard.totalContracts'),
      value: contractsLoading ? '…' : totalContracts,
      icon: <FileText size={22} />,
      color: 'text-success',
      topBorderColor: 'var(--t-success)',
      coloredValue: true,
      trend: (contractsSummary?.inReviewCount ?? 0) > 0
        ? { direction: 'up' as const, label: `${contractsSummary?.inReviewCount} ${t('dashboard.inReviewShort')}` }
        : undefined,
      sparkline: totalContracts > 0
        ? { data: [1, 2, 4, 6, 8, 10, totalContracts], color: 'var(--t-success)' }
        : undefined,
    },
    {
      title: t('dashboard.recentChanges'),
      value: changesLoading ? '…' : totalChanges,
      icon: <ShieldCheck size={22} />,
      color: 'text-warning',
      topBorderColor: 'var(--t-warning)',
      coloredValue: true,
      trend: (changesSummary?.changesNeedingAttention ?? 0) > 0
        ? { direction: 'down' as const, label: `${changesSummary?.changesNeedingAttention} ${t('dashboard.needAttention')}` }
        : undefined,
      sparkline: totalChanges > 0
        ? { data: [2, 1, 3, 2, 5, 4, totalChanges], color: 'var(--t-warning)' }
        : undefined,
    },
    {
      title: t('dashboard.openIncidents'),
      value: incidentsLoading ? '…' : openIncidents,
      icon: <AlertTriangle size={22} />,
      color: 'text-critical',
      topBorderColor: 'var(--t-critical)',
      coloredValue: true,
      trend: openIncidents > 0
        ? { direction: 'down' as const, label: `${openIncidents} ${t('dashboard.activeNow')}` }
        : undefined,
      sparkline: openIncidents > 0
        ? { data: [6, 5, 3, 4, 2, 1, openIncidents], color: 'var(--t-critical)' }
        : undefined,
    },
  ];
  ```

- [ ] **Step 9.2: Change ContentGrid from 5 to 4 columns**

  Find:
  ```tsx
  <ContentGrid columns={5}>
  ```
  Change to:
  ```tsx
  <ContentGrid columns={4}>
  ```

- [ ] **Step 9.3: Also remove the GitBranch import if no longer used**

  Find the import line:
  ```tsx
  import {
    Zap, GitBranch, FileText, ShieldCheck, Activity, AlertTriangle,
    ArrowRight, AlertCircle, Clock, Server, Download,
  } from 'lucide-react';
  ```
  Remove `GitBranch` (and `Server`, `Download` if not used elsewhere in the file):
  ```tsx
  import {
    Zap, FileText, ShieldCheck, Activity, AlertTriangle,
    ArrowRight, AlertCircle, Clock,
  } from 'lucide-react';
  ```

  > Note: Before removing imports, search the file for their usage: `Ctrl+F` for `GitBranch`, `Server`, `Download` to confirm they're not used elsewhere in this file.

- [ ] **Step 9.4: Replace simple Link alerts with structured Alert component**

  Find the `attentionAlerts` rendering section (around line 203):
  ```tsx
  {attentionAlerts.length > 0 && (
    <div className="mb-6 space-y-2">
      {attentionAlerts.map((alert) => (
        <Link
          key={alert.to}
          to={alert.to}
          className={`flex items-center gap-3 rounded-lg border px-4 py-2.5 text-sm transition-colors hover:opacity-80 ${severityColors[alert.severity]}`}
        >
          {alert.icon}
          <span className="flex-1">{alert.text}</span>
          <ArrowRight size={14} className="opacity-60" />
        </Link>
      ))}
    </div>
  )}
  ```

  Replace with structured alerts that include a title, description, and icon-box:

  ```tsx
  {attentionAlerts.length > 0 && (
    <div className="mb-6 space-y-2">
      {attentionAlerts.map((alert) => (
        <Link
          key={alert.to}
          to={alert.to}
          className={cn(
            'flex items-center gap-3 rounded-lg border px-4 py-3 transition-colors hover:opacity-90',
            severityColors[alert.severity],
          )}
        >
          {/* Icon box */}
          <div className={cn(
            'flex items-center justify-center w-7 h-7 rounded-md shrink-0',
            alert.severity === 'critical' ? 'bg-critical/15' : alert.severity === 'warning' ? 'bg-warning/15' : 'bg-info/15',
          )}>
            {alert.icon}
          </div>
          {/* Text */}
          <div className="flex-1 min-w-0">
            <p className="text-sm font-semibold leading-tight">{alert.text}</p>
          </div>
          <ArrowRight size={14} className="opacity-60 shrink-0" />
        </Link>
      ))}
    </div>
  )}
  ```

  Also add `cn` import if not already present (check top of file — it likely isn't imported in DashboardPage):
  ```tsx
  import { cn } from '../../../lib/cn';
  ```

- [ ] **Step 9.5: Run tests**

  ```bash
  cd src/frontend && npm test 2>&1 | tail -10
  ```
  Expected: No failures. (DashboardPage has no dedicated unit test file — the change is validated via typecheck.)

- [ ] **Step 9.6: Run TypeScript type check**

  ```bash
  cd src/frontend && npx tsc --noEmit 2>&1 | grep -E "error TS|DashboardPage" | head -20
  ```
  Expected: No errors.

- [ ] **Step 9.7: Commit**

  ```bash
  git add src/frontend/src/features/shared/pages/DashboardPage.tsx
  git commit -m "feat(dashboard): 4-column KPI grid with semantic colors + structured alert components"
  ```

---

## Task 10: AuthShell — Animated Orbs + Logo Glow

**Files:**
- Modify: `src/frontend/src/features/identity-access/components/AuthShell.tsx`

- [ ] **Step 10.1: Add animation to the two orb divs**

  In `AuthShell.tsx`, find the background radial halos section (around line 64):
  ```tsx
  <div
    className="absolute top-[-15%] left-[-5%] w-[55%] h-[55%] rounded-full blur-[140px] bg-[radial-gradient(circle,rgba(27,127,232,0.10)_0%,transparent_70%)]"
  />
  <div
    className="absolute bottom-[-20%] right-[-10%] w-[45%] h-[50%] rounded-full blur-[120px] bg-[radial-gradient(circle,rgba(18,196,232,0.07)_0%,transparent_70%)]"
  />
  ```

  Change to (add `style` with animation):
  ```tsx
  <div
    className="absolute top-[-15%] left-[-5%] w-[55%] h-[55%] rounded-full blur-[140px] bg-[radial-gradient(circle,rgba(27,127,232,0.10)_0%,transparent_70%)]"
    style={{ animation: 'pulse-soft 4s ease-in-out infinite' }}
  />
  <div
    className="absolute bottom-[-20%] right-[-10%] w-[45%] h-[50%] rounded-full blur-[120px] bg-[radial-gradient(circle,rgba(18,196,232,0.07)_0%,transparent_70%)]"
    style={{ animation: 'pulse-soft 5s ease-in-out infinite 1s' }}
  />
  ```

- [ ] **Step 10.2: Add box-shadow glow to the brand logo**

  Find the brand logo `img` element (around line 94):
  ```tsx
  <img
    src="/brand/logo.svg"
    alt="NexTraceOne"
    className="h-16 xl:h-20 w-auto"
  />
  ```

  Change to:
  ```tsx
  <img
    src="/brand/logo.svg"
    alt="NexTraceOne"
    className="h-16 xl:h-20 w-auto"
    style={{ filter: 'drop-shadow(0 0 24px rgba(18,196,232,.25))' }}
  />
  ```

  > Note: Using `filter: drop-shadow` instead of `box-shadow` because SVG logos don't respond to `box-shadow`. The effect is equivalent for a glow.

- [ ] **Step 10.3: Run tests**

  ```bash
  cd src/frontend && npm test -- --reporter verbose 2>&1 | grep -E "PASS|FAIL|LoginPage|AuthShell"
  ```
  Expected: `LoginPage.test.tsx` passes.

- [ ] **Step 10.4: Commit**

  ```bash
  git add src/frontend/src/features/identity-access/components/AuthShell.tsx
  git commit -m "style(auth-shell): animated orbs with pulse-soft + logo glow filter"
  ```

---

## Task 11: i18n — "Welcome back" / "Bem-vindo de volta"

**Files:**
- Modify: `src/frontend/src/locales/en.json`
- Modify: `src/frontend/src/locales/pt-PT.json`
- Modify: `src/frontend/src/locales/pt-BR.json`
- Modify: `src/frontend/src/locales/es.json`

- [ ] **Step 11.1: Update en.json**

  Find `"welcomeTitle": "Welcome to NexTraceOne"` and change to:
  ```json
  "welcomeTitle": "Welcome back"
  ```

- [ ] **Step 11.2: Update pt-PT.json**

  Find `"welcomeTitle"` (whatever value it has) and change to:
  ```json
  "welcomeTitle": "Bem-vindo de volta"
  ```

- [ ] **Step 11.3: Update pt-BR.json**

  Find `"welcomeTitle"` and change to:
  ```json
  "welcomeTitle": "Bem-vindo de volta"
  ```

- [ ] **Step 11.4: Update es.json**

  Find `"welcomeTitle"` and change to:
  ```json
  "welcomeTitle": "Bienvenido de nuevo"
  ```

- [ ] **Step 11.5: Run LoginPage test**

  ```bash
  cd src/frontend && npx vitest run src/__tests__/pages/LoginPage.test.tsx --reporter verbose
  ```
  Expected: PASS (the test renders via i18n mock that returns the key, not the value).

- [ ] **Step 11.6: Validate i18n consistency**

  ```bash
  cd src/frontend && node scripts/validate-i18n.mjs 2>&1 | tail -10
  ```
  Expected: No missing keys reported.

- [ ] **Step 11.7: Commit**

  ```bash
  git add src/frontend/src/locales/en.json src/frontend/src/locales/pt-PT.json src/frontend/src/locales/pt-BR.json src/frontend/src/locales/es.json
  git commit -m "i18n(auth): welcomeTitle → 'Welcome back' / 'Bem-vindo de volta' / 'Bienvenido de nuevo'"
  ```

---

## Task 12: AssistantMessageBubble — Differentiated Chat Bubbles

**Files:**
- Modify: `src/frontend/src/features/ai-hub/components/AssistantMessageBubble.tsx`

- [ ] **Step 12.1: Update user bubble styles**

  Find the outer bubble `div` (around line 64):
  ```tsx
  <div
    className={`max-w-[85%] rounded-lg px-3 py-2.5 ${
      msg.role === 'assistant' ? 'bg-elevated' : 'bg-accent/20'
    }`}
  >
  ```

  Replace with:
  ```tsx
  <div
    className={cn(
      'max-w-[85%] px-3 py-2.5',
      msg.role === 'assistant'
        ? 'bg-card border border-edge'
        : 'bg-accent text-white',
    )}
    style={{
      borderRadius: msg.role === 'assistant'
        ? '12px 12px 12px 4px'
        : '12px 12px 4px 12px',
    }}
  >
  ```

  Add `cn` import at the top if not present:
  ```tsx
  import { cn } from '../../../lib/cn';
  ```

- [ ] **Step 12.2: Fix text color for user bubble content**

  Find the content `<p>` (around line 132):
  ```tsx
  <p className="text-xs text-body whitespace-pre-wrap leading-relaxed">{msg.content}</p>
  ```

  Change to:
  ```tsx
  <p className={cn(
    'text-xs whitespace-pre-wrap leading-relaxed',
    msg.role === 'user' ? 'text-white' : 'text-body',
  )}>
    {msg.content}
  </p>
  ```

- [ ] **Step 12.3: Update AI bubble header with model name badge**

  Find the AI header section (around line 70):
  ```tsx
  {msg.role === 'assistant' && (
    <div className="flex items-center gap-1.5 mb-1.5 flex-wrap">
      <Bot size={12} className="text-accent" />
      <span className="text-[10px] font-medium text-accent">{t('aiHub.assistant')}</span>
  ```

  Add model name badge after the assistant span:
  ```tsx
  {msg.role === 'assistant' && (
    <div className="flex items-center gap-1.5 mb-1.5 flex-wrap">
      <Bot size={12} className="text-accent" />
      <span className="text-[10px] font-medium text-accent">{t('aiHub.assistant')}</span>
      {msg.modelName && (
        <span
          className="px-1.5 py-px rounded text-[9px] font-semibold bg-success/10 text-success border border-success/20"
          style={{ fontFamily: 'var(--font-mono, monospace)' }}
        >
          {msg.modelName}
        </span>
      )}
  ```

- [ ] **Step 12.4: Update timestamp color for user bubble**

  Find the user header section (around line 124):
  ```tsx
  {msg.role === 'user' && (
    <div className="flex items-center gap-1.5 mb-1 justify-end">
      <span className="text-[10px] text-faded">{formatTime(msg.timestamp)}</span>
      <span className="text-[10px] font-medium text-body">{t('aiHub.you')}</span>
    </div>
  )}
  ```

  Change to (slightly lighter text in user bubble):
  ```tsx
  {msg.role === 'user' && (
    <div className="flex items-center gap-1.5 mb-1 justify-end">
      <span className="text-[10px] text-white/60">{formatTime(msg.timestamp)}</span>
      <span className="text-[10px] font-medium text-white/80">{t('aiHub.you')}</span>
    </div>
  )}
  ```

- [ ] **Step 12.5: Run tests**

  ```bash
  cd src/frontend && npm test -- --reporter verbose 2>&1 | grep -E "PASS|FAIL|AssistantPanel|AiCopilot"
  ```
  Expected: `AssistantPanel.test.tsx` and related tests PASS.

- [ ] **Step 12.6: Commit**

  ```bash
  git add src/frontend/src/features/ai-hub/components/AssistantMessageBubble.tsx
  git commit -m "style(ai-chat): differentiated user/assistant bubbles + model name badge in AI header"
  ```

---

## Task 13: ServiceCatalogServicesTab — Initial Avatar + Type Badge

**Files:**
- Modify: `src/frontend/src/features/catalog/components/ServiceCatalogServicesTab.tsx`

- [ ] **Step 13.1: Replace Server icon with initial letter avatar + add type badge**

  Replace the entire `ServiceCatalogServicesTab.tsx`:

  ```tsx
  import { useTranslation } from 'react-i18next';
  import { ChevronRight } from 'lucide-react';
  import { Card, CardBody } from '../../../components/Card';
  import { Badge } from '../../../components/Badge';
  import type { ServiceNode } from '../../../types';

  interface ServiceCatalogServicesTabProps {
    filteredServices: ServiceNode[];
    onSelectNode: (nodeId: string) => void;
  }

  /** Maps serviceType string to a short label for the type badge. */
  function getServiceTypeLabel(serviceType: string): string {
    const map: Record<string, string> = {
      RestApi: 'REST',
      GraphqlApi: 'GraphQL',
      GrpcService: 'gRPC',
      KafkaProducer: 'Kafka',
      KafkaConsumer: 'Kafka',
      BackgroundService: 'Worker',
      ScheduledProcess: 'Cron',
      Gateway: 'Gateway',
      LegacySystem: 'Legacy',
      SharedPlatformService: 'Platform',
    };
    return map[serviceType] ?? serviceType;
  }

  /**
   * Conteúdo da aba "Serviços" do Service Catalog.
   *
   * Linha: avatar (initial letter) + nome + equipa·domínio + badge tipo + badge criticidade + chevron.
   */
  export function ServiceCatalogServicesTab({ filteredServices, onSelectNode }: ServiceCatalogServicesTabProps) {
    const { t } = useTranslation();

    return (
      <Card>
        <CardBody className="p-0">
          {!filteredServices.length ? (
            <p className="px-6 py-12 text-sm text-muted text-center">{t('serviceCatalog.noServices')}</p>
          ) : (
            <ul className="divide-y divide-edge">
              {filteredServices.map((svc) => {
                const initial = svc.name?.[0]?.toUpperCase() ?? '?';
                return (
                  <li
                    key={svc.serviceAssetId}
                    role="button"
                    tabIndex={0}
                    className="px-5 py-3.5 flex items-center gap-4 hover:bg-hover transition-colors cursor-pointer"
                    onClick={() => onSelectNode(svc.serviceAssetId)}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter' || e.key === ' ') {
                        e.preventDefault();
                        onSelectNode(svc.serviceAssetId);
                      }
                    }}
                  >
                    {/* Initial letter avatar */}
                    <div
                      className="w-9 h-9 rounded-lg bg-accent/10 flex items-center justify-center text-accent font-bold text-sm shrink-0"
                      aria-hidden="true"
                    >
                      {initial}
                    </div>

                    {/* Name + team · domain */}
                    <div className="flex-1 min-w-0">
                      <p className="font-medium text-heading text-sm truncate">{svc.name}</p>
                      <p className="text-xs text-muted truncate">
                        {svc.teamName}
                        {svc.domain ? ` · ${svc.domain}` : ''}
                      </p>
                    </div>

                    {/* Type badge */}
                    {svc.serviceType && (
                      <Badge variant="info" size="sm">
                        {getServiceTypeLabel(svc.serviceType)}
                      </Badge>
                    )}

                    {/* Criticality badge */}
                    <Badge
                      variant={
                        svc.criticality === 'Critical' ? 'danger'
                          : svc.criticality === 'High' ? 'warning'
                          : svc.criticality === 'Low' ? 'neutral'
                          : 'default'
                      }
                      size="sm"
                    >
                      {svc.criticality ?? t('common.unknown')}
                    </Badge>

                    <ChevronRight size={16} className="text-muted shrink-0" />
                  </li>
                );
              })}
            </ul>
          )}
        </CardBody>
      </Card>
    );
  }
  ```

- [ ] **Step 13.2: Run ServiceCatalog tests**

  ```bash
  cd src/frontend && npx vitest run src/__tests__/catalog/ServiceCatalogListPage.test.tsx --reporter verbose
  ```
  Expected: PASS.

- [ ] **Step 13.3: Run full test suite**

  ```bash
  cd src/frontend && npm test 2>&1 | tail -10
  ```
  Expected: All tests pass.

- [ ] **Step 13.4: Run TypeScript typecheck**

  ```bash
  cd src/frontend && npx tsc --noEmit 2>&1 | head -20
  ```
  Expected: No errors.

- [ ] **Step 13.5: Commit**

  ```bash
  git add src/frontend/src/features/catalog/components/ServiceCatalogServicesTab.tsx
  git commit -m "feat(catalog): initial letter avatar + service type badge in services tab"
  ```

---

## Final Verification

- [ ] **Run full test suite one last time**

  ```bash
  cd src/frontend && npm test 2>&1 | tail -15
  ```
  Expected: All tests pass. Note any snapshot failures — update with `npm test -- --update-snapshots` if they are visual-only.

- [ ] **TypeScript typecheck**

  ```bash
  cd src/frontend && npx tsc --noEmit
  ```
  Expected: Zero errors.

- [ ] **Verify success criteria from spec**

  Check against `docs/superpowers/specs/2026-05-26-frontend-ui-ux-redesign-design.md` § 9:

  - [ ] `AppSidebarFooter` visível e funcional (user info + mini-menu logout) — Task 2+3
  - [ ] Sidebar mantém fundo dark em light mode — Task 1+3
  - [ ] Topbar com 56px de altura — Task 4
  - [ ] Dashboard com 4 KPIs e alertas estruturados — Task 9
  - [ ] StatCard com border-top semântico — Task 8
  - [ ] Empty states com ícone dashed-border — Task 7
  - [ ] Auth orbs com animação pulse — Task 10
  - [ ] Todos os testes existentes a passar — Final step above

---

## Notes for Executor

**Test command:** `cd src/frontend && npm test`  
**Typecheck command:** `cd src/frontend && npx tsc --noEmit`  
**Single file test:** `cd src/frontend && npx vitest run src/__tests__/components/FILE.test.tsx --reporter verbose`

**Spec reference:** `docs/superpowers/specs/2026-05-26-frontend-ui-ux-redesign-design.md`

**Intentional omissions (already implemented or out of scope for unit tasks):**
- `Button.tsx` — `institutional` variant already exists with correct blue gradient; loading spinner already implemented. No changes needed.
- `ErrorState.tsx` — already follows the alert pattern (icon-box + heading + message + action). No changes needed.
- `AuthCard` shadow — can be added inline in `AuthShell.tsx`'s right panel if desired; not tracked here since the card component is auto-styled.
