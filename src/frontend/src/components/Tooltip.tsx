import { useState, useRef, useCallback, useId, type ReactNode, type ReactElement } from 'react';
import { cn } from '../lib/cn';

interface TooltipProps {
  /** Conteúdo do tooltip. */
  content: string;
  /** Posição do tooltip. */
  position?: 'top' | 'bottom' | 'left' | 'right';
  /** Delay antes de mostrar (ms). */
  delay?: number;
  /** Delay antes de esconder (ms). */
  hideDelay?: number;
  /** Elemento trigger. */
  children: ReactElement;
  className?: string;
}

/**
 * Tooltip acessível com suporte a hover, focus e teclado.
 *
 * WCAG 2.1 AA compliant:
 * - aria-describedby liga trigger ao tooltip
 * - Visível tanto em hover como em focus
 * - Escape fecha o tooltip
 * - Delay configurável para melhor UX
 *
 * @see docs/DESIGN-SYSTEM.md §4.8
 */
export function Tooltip({
  content,
  position = 'top',
  delay = 200,
  hideDelay = 0,
  children,
  className,
}: TooltipProps) {
  const [visible, setVisible] = useState(false);
  const showTimer = useRef<ReturnType<typeof setTimeout>>(null);
  const hideTimer = useRef<ReturnType<typeof setTimeout>>(null);
  const tooltipId = useId();

  const show = useCallback(() => {
    if (hideTimer.current) clearTimeout(hideTimer.current);
    showTimer.current = setTimeout(() => setVisible(true), delay);
  }, [delay]);

  const hide = useCallback(() => {
    if (showTimer.current) clearTimeout(showTimer.current);
    hideTimer.current = setTimeout(() => setVisible(false), hideDelay);
  }, [hideDelay]);

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === 'Escape' && visible) {
        setVisible(false);
      }
    },
    [visible],
  );

  return (
    <div
      className={cn('relative inline-flex', className)}
      onMouseEnter={show}
      onMouseLeave={hide}
      onFocus={show}
      onBlur={hide}
      onKeyDown={handleKeyDown}
    >
      <span aria-describedby={visible ? tooltipId : undefined}>
        {children}
      </span>
      {visible && (
        <div
          id={tooltipId}
          role="tooltip"
          className={cn(
            'pointer-events-none absolute z-[var(--z-dropdown)] whitespace-nowrap',
            'rounded-sm bg-elevated px-3 py-1.5 text-xs text-heading shadow-floating border border-edge',
            'animate-fade-in',
            position === 'top' && 'bottom-full left-1/2 -translate-x-1/2 mb-2',
            position === 'bottom' && 'top-full left-1/2 -translate-x-1/2 mt-2',
            position === 'left' && 'right-full top-1/2 -translate-y-1/2 mr-2',
            position === 'right' && 'left-full top-1/2 -translate-y-1/2 ml-2',
          )}
        >
          {content}
        </div>
      )}
    </div>
  );
}
