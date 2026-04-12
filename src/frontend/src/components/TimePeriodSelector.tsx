import { cn } from '../lib/cn';

export interface TimePeriodOption {
  value: string;
  label: string;
}

export interface TimePeriodSelectorProps {
  /** Opções de período disponíveis. */
  options: TimePeriodOption[];
  /** Valor selecionado. */
  selected: string;
  /** Callback ao selecionar. */
  onSelect: (value: string) => void;
  /** Tamanho dos botões. */
  size?: 'sm' | 'md';
  className?: string;
}

const sizeClasses = {
  sm: 'text-[10px] px-2.5 py-1',
  md: 'text-xs px-3 py-1.5',
};

/**
 * TimePeriodSelector — grupo de botões pill para seleção de período temporal.
 * Inspirado pelos tabs `Today | Week | Month` do template NexLink.
 *
 * Fundo arredondado com seleção pill. Adequado para widgets de charts.
 */
export function TimePeriodSelector({
  options,
  selected,
  onSelect,
  size = 'sm',
  className,
}: TimePeriodSelectorProps) {
  return (
    <div
      className={cn(
        'inline-flex items-center gap-0.5 p-0.5 rounded-full bg-elevated border border-edge/50',
        className,
      )}
      role="tablist"
    >
      {options.map((opt) => (
        <button
          key={opt.value}
          role="tab"
          aria-selected={selected === opt.value}
          onClick={() => onSelect(opt.value)}
          className={cn(
            'rounded-full font-medium transition-all duration-[var(--nto-motion-base)]',
            sizeClasses[size],
            selected === opt.value
              ? 'bg-accent text-on-accent shadow-sm'
              : 'text-muted hover:text-heading hover:bg-hover',
          )}
        >
          {opt.label}
        </button>
      ))}
    </div>
  );
}
