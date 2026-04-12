import { useRef, useCallback, type ReactNode, type KeyboardEvent } from 'react';
import { cn } from '../lib/cn';

interface TabItem {
  id: string;
  label: string;
  icon?: ReactNode;
  disabled?: boolean;
}

interface TabsProps {
  /** Lista de abas. */
  items: TabItem[];
  /** ID da aba ativa. */
  activeId: string;
  /** Callback ao mudar de aba. */
  onChange: (id: string) => void;
  /** Variante visual. */
  variant?: 'underline' | 'pill';
  /** Tamanho. */
  size?: 'sm' | 'md';
  /** ID prefix para aria-controls linkage com tabpanel. */
  id?: string;
  className?: string;
}

/**
 * Tabs enterprise com duas variantes: underline (padrão) e pill.
 *
 * WCAG 2.1 AA compliant:
 * - Arrow keys (Left/Right) para navegar entre tabs
 * - Home/End para primeiro/último tab
 * - Roving tabindex para gestão de foco
 * - aria-controls liga cada tab ao seu painel
 * - aria-selected indica a aba ativa
 *
 * Underline: usado para navegação de conteúdo em seções de página.
 * Pill: usado para filtros rápidos e status toggles.
 *
 * @see docs/DESIGN-SYSTEM.md §4.10
 */
export function Tabs({
  items,
  activeId,
  onChange,
  variant = 'underline',
  size = 'md',
  id: tabsId,
  className,
}: TabsProps) {
  const tabRefs = useRef<Map<string, HTMLButtonElement>>(new Map());

  const enabledItems = items.filter((t) => !t.disabled);

  const focusTab = useCallback((tabId: string) => {
    const el = tabRefs.current.get(tabId);
    if (el) el.focus();
  }, []);

  const handleKeyDown = useCallback(
    (e: KeyboardEvent<HTMLDivElement>) => {
      const currentIndex = enabledItems.findIndex((t) => t.id === activeId);
      if (currentIndex === -1) return;

      let nextIndex: number | null = null;

      switch (e.key) {
        case 'ArrowRight':
        case 'ArrowDown':
          e.preventDefault();
          nextIndex = (currentIndex + 1) % enabledItems.length;
          break;
        case 'ArrowLeft':
        case 'ArrowUp':
          e.preventDefault();
          nextIndex = (currentIndex - 1 + enabledItems.length) % enabledItems.length;
          break;
        case 'Home':
          e.preventDefault();
          nextIndex = 0;
          break;
        case 'End':
          e.preventDefault();
          nextIndex = enabledItems.length - 1;
          break;
        default:
          return;
      }

      if (nextIndex !== null) {
        const nextTab = enabledItems[nextIndex];
        onChange(nextTab.id);
        focusTab(nextTab.id);
      }
    },
    [activeId, enabledItems, onChange, focusTab],
  );

  const setTabRef = useCallback((id: string) => (el: HTMLButtonElement | null) => {
    if (el) tabRefs.current.set(id, el);
    else tabRefs.current.delete(id);
  }, []);

  const panelId = (tabId: string) => tabsId ? `${tabsId}-panel-${tabId}` : undefined;
  const tabElId = (tabId: string) => tabsId ? `${tabsId}-tab-${tabId}` : undefined;

  if (variant === 'pill') {
    return (
      <div
        role="tablist"
        onKeyDown={handleKeyDown}
        className={cn(
          'inline-flex items-center gap-1 rounded-lg bg-elevated p-1',
          className,
        )}
      >
        {items.map((tab) => (
          <button
            key={tab.id}
            ref={setTabRef(tab.id)}
            id={tabElId(tab.id)}
            role="tab"
            aria-selected={tab.id === activeId}
            aria-controls={panelId(tab.id)}
            tabIndex={tab.id === activeId ? 0 : -1}
            disabled={tab.disabled}
            onClick={() => onChange(tab.id)}
            className={cn(
              'inline-flex items-center gap-2 rounded-sm px-3 font-medium transition-colors',
              size === 'sm' ? 'py-1 text-xs' : 'py-1.5 text-sm',
              tab.id === activeId
                ? 'bg-panel text-heading shadow-xs'
                : 'text-muted hover:text-body',
              tab.disabled && 'opacity-40 cursor-not-allowed',
            )}
            style={{ transitionDuration: 'var(--nto-motion-fast)' }}
          >
            {tab.icon}
            {tab.label}
          </button>
        ))}
      </div>
    );
  }

  return (
    <div
      role="tablist"
      onKeyDown={handleKeyDown}
      className={cn(
        'flex items-center border-b border-edge',
        className,
      )}
    >
      {items.map((tab) => (
        <button
          key={tab.id}
          ref={setTabRef(tab.id)}
          id={tabElId(tab.id)}
          role="tab"
          aria-selected={tab.id === activeId}
          aria-controls={panelId(tab.id)}
          tabIndex={tab.id === activeId ? 0 : -1}
          disabled={tab.disabled}
          onClick={() => onChange(tab.id)}
          className={cn(
            'inline-flex items-center gap-2 border-b-2 font-medium transition-colors',
            size === 'sm' ? 'px-3 pb-2 text-xs' : 'px-4 pb-3 text-sm',
            tab.id === activeId
              ? 'border-cyan text-cyan'
              : 'border-transparent text-muted hover:text-body hover:border-edge-strong',
            tab.disabled && 'opacity-40 cursor-not-allowed',
          )}
          style={{ transitionDuration: 'var(--nto-motion-fast)' }}
        >
          {tab.icon}
          {tab.label}
        </button>
      ))}
    </div>
  );
}

/* ─── TabPanel ──────────────────────────────────────────────────────────────── */

interface TabPanelProps {
  /** ID do tab que controla este painel (deve corresponder ao tab item id). */
  tabId: string;
  /** ID prefix do componente Tabs pai. */
  tabsId: string;
  /** Se este painel está ativo/visível. */
  active: boolean;
  children: ReactNode;
  className?: string;
}

/**
 * Painel de conteúdo associado a um Tab.
 * Usa role="tabpanel" e aria-labelledby para linkage WCAG.
 */
export function TabPanel({ tabId, tabsId, active, children, className }: TabPanelProps) {
  if (!active) return null;
  return (
    <div
      id={`${tabsId}-panel-${tabId}`}
      role="tabpanel"
      aria-labelledby={`${tabsId}-tab-${tabId}`}
      tabIndex={0}
      className={cn('animate-fade-in', className)}
    >
      {children}
    </div>
  );
}
