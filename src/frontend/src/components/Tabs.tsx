import type { ReactNode } from 'react';
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
  className?: string;
}

/**
 * Tabs enterprise com duas variantes: underline (padrão) e pill.
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
  className,
}: TabsProps) {
  if (variant === 'pill') {
    return (
      <div
        role="tablist"
        className={cn(
          'inline-flex items-center gap-1 rounded-lg bg-elevated p-1',
          className,
        )}
      >
        {items.map((tab) => (
          <button
            key={tab.id}
            role="tab"
            aria-selected={tab.id === activeId}
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
      className={cn(
        'flex items-center border-b border-edge',
        className,
      )}
    >
      {items.map((tab) => (
        <button
          key={tab.id}
          role="tab"
          aria-selected={tab.id === activeId}
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
