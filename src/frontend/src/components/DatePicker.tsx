import { forwardRef, type InputHTMLAttributes } from 'react';
import { Calendar, X } from 'lucide-react';
import { cn } from '../lib/cn';

/* ─── DatePicker ───────────────────────────────────────────────────────────── */

interface DatePickerProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type' | 'onChange'> {
  /** Current ISO date string (YYYY-MM-DD). */
  value?: string;
  onChange?: (value: string) => void;
  /** Show clear button when value is set. */
  clearable?: boolean;
  /** Visual size variant. */
  size?: 'sm' | 'md';
  label?: string;
  error?: string;
}

/**
 * DatePicker — styled native date input with calendar icon, clear button, and error state.
 * DESIGN-SYSTEM.md aligned, i18n-ready through native locale support.
 */
export const DatePicker = forwardRef<HTMLInputElement, DatePickerProps>(
  function DatePicker({ value, onChange, clearable, size = 'md', label, error, className, id, ...rest }, ref) {
    const inputId = id ?? `dp-${label?.replace(/\s/g, '-') ?? 'date'}`;

    return (
      <div className={cn('flex flex-col gap-1', className)}>
        {label && (
          <label htmlFor={inputId} className="text-xs font-medium text-muted">
            {label}
          </label>
        )}
        <div className="relative">
          <Calendar
            size={size === 'sm' ? 14 : 16}
            className="absolute left-3 top-1/2 -translate-y-1/2 text-muted pointer-events-none"
            aria-hidden="true"
          />
          <input
            ref={ref}
            id={inputId}
            type="date"
            value={value ?? ''}
            onChange={(e) => onChange?.(e.target.value)}
            className={cn(
              'w-full rounded-lg border border-edge bg-elevated text-body',
              'pl-9 pr-8 focus:outline-none focus:ring-2 focus:ring-accent/40 focus:border-accent',
              'transition-colors duration-[var(--nto-motion-base)]',
              size === 'sm' ? 'h-8 text-xs' : 'h-10 text-sm',
              error && 'border-danger focus:ring-danger/40 focus:border-danger',
            )}
            aria-invalid={!!error}
            aria-describedby={error ? `${inputId}-error` : undefined}
            {...rest}
          />
          {clearable && value && (
            <button
              type="button"
              onClick={() => onChange?.('')}
              className="absolute right-2 top-1/2 -translate-y-1/2 p-0.5 text-muted hover:text-body rounded transition-colors"
              aria-label="Clear date"
            >
              <X size={14} aria-hidden="true" />
            </button>
          )}
        </div>
        {error && (
          <p id={`${inputId}-error`} className="text-xs text-danger" role="alert">
            {error}
          </p>
        )}
      </div>
    );
  },
);

/* ─── DateRangePicker ──────────────────────────────────────────────────────── */

interface DateRangePickerProps {
  startValue?: string;
  endValue?: string;
  onStartChange?: (value: string) => void;
  onEndChange?: (value: string) => void;
  startLabel?: string;
  endLabel?: string;
  clearable?: boolean;
  size?: 'sm' | 'md';
  className?: string;
  error?: string;
}

/**
 * DateRangePicker — two linked DatePicker inputs with automatic min/max constraints.
 */
export function DateRangePicker({
  startValue,
  endValue,
  onStartChange,
  onEndChange,
  startLabel,
  endLabel,
  clearable,
  size = 'md',
  className,
  error,
}: DateRangePickerProps) {
  return (
    <div className={cn('flex items-end gap-2', className)}>
      <DatePicker
        value={startValue}
        onChange={onStartChange}
        max={endValue}
        clearable={clearable}
        size={size}
        label={startLabel}
        className="flex-1"
      />
      <span className={cn('text-muted pb-2', size === 'sm' ? 'text-xs' : 'text-sm')}>—</span>
      <DatePicker
        value={endValue}
        onChange={onEndChange}
        min={startValue}
        clearable={clearable}
        size={size}
        label={endLabel}
        error={error}
        className="flex-1"
      />
    </div>
  );
}
