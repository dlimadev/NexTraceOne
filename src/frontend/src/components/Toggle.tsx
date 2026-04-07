import { useCallback, type KeyboardEvent } from 'react';
import { cn } from '../lib/cn';

interface ToggleProps {
  checked: boolean;
  onChange: (checked: boolean) => void;
  label?: string;
  disabled?: boolean;
  size?: 'sm' | 'md';
}

/**
 * Toggle/Switch — DESIGN-SYSTEM.md §4.6
 * 44x24 (md) ou 36x20 (sm), thumb animado suavemente.
 * Estado ligado com cyan. Foco com ring.
 *
 * WCAG 2.1 AA compliant:
 * - role="switch" com aria-checked
 * - Suporte a teclado: Space e Enter para toggle
 * - Feedback visual para disabled
 */
export function Toggle({ checked, onChange, label, disabled = false, size = 'md' }: ToggleProps) {
  const trackSize = size === 'sm' ? 'w-9 h-5' : 'w-11 h-6';
  const thumbSize = size === 'sm' ? 'w-3.5 h-3.5' : 'w-4.5 h-4.5';
  const thumbTranslate = size === 'sm' ? 'translate-x-4' : 'translate-x-5';

  const handleKeyDown = useCallback(
    (e: KeyboardEvent<HTMLButtonElement>) => {
      if (e.key === ' ' || e.key === 'Enter') {
        e.preventDefault();
        if (!disabled) onChange(!checked);
      }
    },
    [checked, disabled, onChange],
  );

  return (
    <label className={cn('inline-flex items-center gap-3 cursor-pointer', disabled && 'opacity-50 cursor-not-allowed')}>
      <button
        role="switch"
        type="button"
        aria-checked={checked}
        aria-label={label}
        disabled={disabled}
        onClick={() => onChange(!checked)}
        onKeyDown={handleKeyDown}
        className={cn(
          'relative inline-flex shrink-0 rounded-pill border-2 border-transparent',
          'transition-colors duration-[var(--nto-motion-base)]',
          'focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-canvas',
          trackSize,
          checked ? 'bg-cyan' : 'bg-elevated',
          disabled && 'cursor-not-allowed',
        )}
      >
        <span
          className={cn(
            'pointer-events-none inline-block rounded-pill bg-heading shadow-sm',
            'transition-transform duration-[var(--nto-motion-base)]',
            thumbSize,
            checked ? thumbTranslate : 'translate-x-0.5',
          )}
          style={{ marginTop: '1px' }}
          aria-hidden="true"
        />
      </button>
      {label && <span className="text-sm text-body select-none">{label}</span>}
    </label>
  );
}
