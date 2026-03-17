import { cn } from '../lib/cn';

type DividerVariant = 'subtle' | 'strong';

interface DividerProps {
  /** Variante visual. */
  variant?: DividerVariant;
  /** Orientação. */
  orientation?: 'horizontal' | 'vertical';
  className?: string;
}

/**
 * Divider semântico — separador visual entre seções.
 *
 * Usa tokens de border para manter consistência com o dark theme.
 * Subtle: usa divider token (muito sutil). Strong: usa edge.
 *
 * @see docs/DESIGN-SYSTEM.md §2.2
 */
export function Divider({
  variant = 'subtle',
  orientation = 'horizontal',
  className,
}: DividerProps) {
  return (
    <div
      role="separator"
      className={cn(
        orientation === 'horizontal' ? 'w-full h-px' : 'h-full w-px',
        variant === 'strong' ? 'bg-edge' : 'bg-divider',
        className,
      )}
    />
  );
}
