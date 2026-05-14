import * as React from 'react';
import { cn } from '@/lib/cn';

interface ProgressProps extends React.HTMLAttributes<HTMLDivElement> {
  value?: number;
  max?: number;
}

const Progress = React.forwardRef<HTMLDivElement, ProgressProps>(
  ({ className, value = 0, max = 100, ...props }, ref) => {
    const percentage = Math.min(100, Math.max(0, (value / max) * 100));

    return (
      <div
        ref={ref}
        role="progressbar"
        aria-valuemin={0}
        aria-valuemax={max}
        aria-valuenow={value}
        className={cn(
          'relative h-2 w-full overflow-hidden rounded-full bg-primary/20',
          className
        )}
        {...props}
      >
        <div
          className="h-full w-full flex-1 bg-primary transition-all"
          style={{ transform: `translateX(-${100 - percentage}%)` }}
        />
      </div>
    );
  }
);
Progress.displayName = 'Progress';

export { Progress };
export type { ProgressProps };

/* ─── Types ─────────────────────────────────────────────────────────────────── */

interface ProgressBarProps {
  /** Valor atual (0-100). undefined = indeterminate. */
  value?: number;
  /** Variante semântica. */
  variant?: 'default' | 'success' | 'warning' | 'danger';
  /** Tamanho. */
  size?: 'sm' | 'md' | 'lg';
  /** Label de acessibilidade. */
  label?: string;
  /** Mostra percentagem. */
  showValue?: boolean;
  className?: string;
}

interface ProgressCircularProps {
  /** Valor atual (0-100). */
  value: number;
  /** Tamanho em pixels. */
  size?: number;
  /** Espessura do traço. */
  strokeWidth?: number;
  /** Variante semântica. */
  variant?: 'default' | 'success' | 'warning' | 'danger';
  /** Label central customizado. */
  label?: string;
  className?: string;
}

interface ProgressStepsProps {
  /** Lista de steps. */
  steps: string[];
  /** Índice do step atual (0-based). */
  currentStep: number;
  /** Variante semântica. */
  variant?: 'default' | 'success' | 'warning' | 'danger';
  className?: string;
}

/* ─── Constants ─────────────────────────────────────────────────────────────── */

const barVariantClasses: Record<NonNullable<ProgressBarProps['variant']>, string> = {
  default: 'bg-accent',
  success: 'bg-success',
  warning: 'bg-warning',
  danger: 'bg-danger',
};

const barSizeClasses: Record<NonNullable<ProgressBarProps['size']>, string> = {
  sm: 'h-1',
  md: 'h-2',
  lg: 'h-3',
};

const circularVariantColors: Record<NonNullable<ProgressCircularProps['variant']>, string> = {
  default: 'var(--t-accent)',
  success: 'var(--t-success)',
  warning: 'var(--t-warning)',
  danger: 'var(--t-danger)',
};

/* ─── ProgressBar ───────────────────────────────────────────────────────────── */

/**
 * Barra de progresso linear (determinada e indeterminada).
 */
export function ProgressBar({
  value,
  variant = 'default',
  size = 'md',
  label,
  showValue = false,
  className,
}: ProgressBarProps) {
  const isDeterminate = value !== undefined;
  const clampedValue = isDeterminate ? Math.min(100, Math.max(0, value)) : 0;

  return (
    <div className={cn('w-full', className)}>
      {(label || showValue) && (
        <div className="flex items-center justify-between mb-1">
          {label && <span className="text-xs font-medium text-muted">{label}</span>}
          {showValue && isDeterminate && (
            <span className="text-xs font-semibold text-heading">{Math.round(clampedValue)}%</span>
          )}
        </div>
      )}
      <div
        className={cn('w-full rounded-pill bg-elevated overflow-hidden', barSizeClasses[size])}
        role="progressbar"
        aria-valuenow={isDeterminate ? clampedValue : undefined}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-label={label}
      >
        {isDeterminate ? (
          <div
            className={cn('h-full rounded-pill transition-all duration-300', barVariantClasses[variant])}
            style={{ width: `${clampedValue}%` }}
          />
        ) : (
          <div
            className={cn('h-full w-1/3 rounded-pill animate-pulse-soft', barVariantClasses[variant])}
            style={{
              animation: 'progress-indeterminate 1.5s ease-in-out infinite',
            }}
          />
        )}
      </div>
    </div>
  );
}

/* ─── ProgressCircular ──────────────────────────────────────────────────────── */

/**
 * Progresso circular.
 */
export function ProgressCircular({
  value,
  size: diameter = 48,
  strokeWidth = 4,
  variant = 'default',
  label,
  className,
}: ProgressCircularProps) {
  const radius = (diameter - strokeWidth) / 2;
  const circumference = 2 * Math.PI * radius;
  const clamped = Math.min(100, Math.max(0, value));
  const offset = circumference - (clamped / 100) * circumference;

  return (
    <div
      className={cn('inline-flex items-center justify-center relative', className)}
      role="progressbar"
      aria-valuenow={clamped}
      aria-valuemin={0}
      aria-valuemax={100}
      aria-label={label ?? `${Math.round(clamped)}%`}
    >
      <svg width={diameter} height={diameter} className="-rotate-90">
        <circle
          cx={diameter / 2}
          cy={diameter / 2}
          r={radius}
          fill="none"
          stroke="var(--t-elevated)"
          strokeWidth={strokeWidth}
        />
        <circle
          cx={diameter / 2}
          cy={diameter / 2}
          r={radius}
          fill="none"
          stroke={circularVariantColors[variant]}
          strokeWidth={strokeWidth}
          strokeLinecap="round"
          strokeDasharray={circumference}
          strokeDashoffset={offset}
          className="transition-all duration-300"
        />
      </svg>
      <span className="absolute text-xs font-semibold text-heading">
        {label ?? `${Math.round(clamped)}%`}
      </span>
    </div>
  );
}

/* ─── ProgressSteps ─────────────────────────────────────────────────────────── */

/**
 * Multi-step progress para wizards e fluxos multi-passo.
 */
export function ProgressSteps({
  steps,
  currentStep,
  variant = 'default',
  className,
}: ProgressStepsProps) {
  return (
    <div className={cn('flex items-center w-full', className)}>
      {steps.map((step, i) => {
        const isCompleted = i < currentStep;
        const isCurrent = i === currentStep;

        return (
          <div key={i} className="flex items-center flex-1 last:flex-initial">
            <div className="flex flex-col items-center gap-1">
              <div
                className={cn(
                  'w-8 h-8 rounded-full flex items-center justify-center text-xs font-semibold border-2 transition-colors',
                  isCompleted
                    ? cn(barVariantClasses[variant], 'border-transparent text-on-accent')
                    : isCurrent
                      ? 'border-accent bg-accent-muted text-accent'
                      : 'border-edge bg-elevated text-muted',
                )}
              >
                {isCompleted ? '✓' : i + 1}
              </div>
              <span
                className={cn(
                  'text-xs whitespace-nowrap',
                  isCurrent ? 'text-heading font-medium' : 'text-muted',
                )}
              >
                {step}
              </span>
            </div>
            {i < steps.length - 1 && (
              <div
                className={cn(
                  'flex-1 h-0.5 mx-2',
                  isCompleted ? barVariantClasses[variant] : 'bg-elevated',
                )}
              />
            )}
          </div>
        );
      })}
    </div>
  );
}
