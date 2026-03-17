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
 */
export function Toggle({ checked, onChange, label, disabled = false, size = 'md' }: ToggleProps) {
  const trackSize = size === 'sm' ? 'w-9 h-5' : 'w-11 h-6';
  const thumbSize = size === 'sm' ? 'w-3.5 h-3.5' : 'w-4.5 h-4.5';
  const thumbTranslate = size === 'sm' ? 'translate-x-4' : 'translate-x-5';

  return (
    <label className={cn('inline-flex items-center gap-3 cursor-pointer', disabled && 'opacity-50 cursor-not-allowed')}>
      <button
        role="switch"
        type="button"
        aria-checked={checked}
        disabled={disabled}
        onClick={() => onChange(!checked)}
        className={cn(
          'relative inline-flex shrink-0 rounded-pill border-2 border-transparent',
          'transition-colors duration-[var(--nto-motion-base)]',
          'focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-canvas',
          trackSize,
          checked ? 'bg-cyan' : 'bg-elevated',
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
        />
      </button>
      {label && <span className="text-sm text-body select-none">{label}</span>}
    </label>
  );
}
