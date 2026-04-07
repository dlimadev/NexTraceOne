import { useId, type ReactNode } from 'react';
import { Check } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { cn } from '../lib/cn';

/* ─── Types ────────────────────────────────────────────────────────────────── */

export interface StepDef {
  /** Step label (should be i18n-ready string). */
  label: string;
  /** Optional secondary text. */
  description?: string;
  /** Mark step as optional. */
  optional?: boolean;
  /** Custom icon for the step indicator. */
  icon?: ReactNode;
}

interface StepperProps {
  /** Step definitions. */
  steps: StepDef[];
  /** Zero-based index of the active step. */
  activeStep: number;
  /** Called when a completed step is clicked for navigation. */
  onStepClick?: (index: number) => void;
  /** Layout orientation. */
  orientation?: 'horizontal' | 'vertical';
  /** Visual size. */
  size?: 'default' | 'compact';
  className?: string;
}

/* ─── Constants ────────────────────────────────────────────────────────────── */

const sizeMap = {
  default: { indicator: 'w-9 h-9 text-sm', connector: 'h-0.5', connectorV: 'w-0.5', gap: 'gap-3' },
  compact: { indicator: 'w-7 h-7 text-xs', connector: 'h-px', connectorV: 'w-px', gap: 'gap-2' },
} as const;

/* ─── Component ────────────────────────────────────────────────────────────── */

/**
 * Stepper / Wizard progress indicator.
 *
 * Supports horizontal and vertical orientations, clickable completed steps,
 * optional steps, and full WCAG 2.1 AA compliance (aria-current, aria-label).
 */
export function Stepper({
  steps,
  activeStep,
  onStepClick,
  orientation = 'horizontal',
  size = 'default',
  className,
}: StepperProps) {
  const { t } = useTranslation();
  const baseId = useId();
  const isHorizontal = orientation === 'horizontal';
  const s = sizeMap[size];

  return (
    <ol
      className={cn(
        'flex list-none p-0 m-0',
        isHorizontal ? 'flex-row items-center' : 'flex-col',
        s.gap,
        className,
      )}
      aria-label={t('common.steps', 'Steps')}
    >
      {steps.map((step, idx) => {
        const status: 'completed' | 'active' | 'upcoming' =
          idx < activeStep ? 'completed' : idx === activeStep ? 'active' : 'upcoming';
        const clickable = status === 'completed' && !!onStepClick;

        return (
          <li
            key={idx}
            className={cn(
              'flex',
              isHorizontal ? 'flex-row items-center flex-1' : 'flex-col',
              idx === steps.length - 1 && isHorizontal && 'flex-none',
            )}
          >
            <div
              className={cn(
                'flex',
                isHorizontal ? 'flex-col items-center' : 'flex-row items-start gap-3',
              )}
            >
              {/* Step indicator circle */}
              <button
                type="button"
                disabled={!clickable}
                onClick={() => clickable && onStepClick?.(idx)}
                className={cn(
                  'rounded-full flex items-center justify-center font-semibold shrink-0 transition-all duration-[var(--nto-motion-base)]',
                  s.indicator,
                  status === 'completed' && 'bg-success text-white',
                  status === 'active' && 'bg-accent text-white ring-2 ring-accent/30',
                  status === 'upcoming' && 'bg-elevated border border-edge text-muted',
                  clickable && 'cursor-pointer hover:ring-2 hover:ring-accent/20',
                  !clickable && 'cursor-default',
                )}
                aria-current={status === 'active' ? 'step' : undefined}
                aria-label={`${step.label} — ${t(`common.step_${status}`, status)}`}
                id={`${baseId}-step-${idx}`}
              >
                {status === 'completed' ? (
                  <Check size={size === 'compact' ? 12 : 16} aria-hidden="true" />
                ) : (
                  step.icon ?? <span>{idx + 1}</span>
                )}
              </button>

              {/* Label block */}
              <div className={cn(isHorizontal ? 'mt-2 text-center' : '')}>
                <span
                  className={cn(
                    'block font-medium leading-tight',
                    size === 'compact' ? 'text-[11px]' : 'text-xs',
                    status === 'active' ? 'text-heading' : 'text-muted',
                  )}
                >
                  {step.label}
                </span>
                {step.description && (
                  <span className="block text-[10px] text-muted/70 mt-0.5 max-w-[120px]">{step.description}</span>
                )}
                {step.optional && (
                  <span className="block text-[10px] text-muted/60 italic mt-0.5">{t('common.optional', 'Optional')}</span>
                )}
              </div>
            </div>

            {/* Connector line */}
            {idx < steps.length - 1 && (
              <div
                className={cn(
                  'transition-colors duration-[var(--nto-motion-base)]',
                  isHorizontal
                    ? cn('flex-1 mx-2 min-w-[16px]', s.connector)
                    : cn('ml-[18px] my-1 min-h-[16px]', s.connectorV),
                  idx < activeStep ? 'bg-success' : 'bg-edge',
                )}
                aria-hidden="true"
              />
            )}
          </li>
        );
      })}
    </ol>
  );
}
