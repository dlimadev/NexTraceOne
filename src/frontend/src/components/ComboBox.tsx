import { useState, useRef, useEffect, useCallback, useId, useMemo, type KeyboardEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronDown, X, Search, Plus } from 'lucide-react';
import { cn } from '../lib/cn';

/* ─── Types ─────────────────────────────────────────────────────────────────── */

export interface ComboBoxOption {
  value: string;
  label: string;
  disabled?: boolean;
}

interface ComboBoxProps {
  /** Opções disponíveis. */
  options: ComboBoxOption[];
  /** Valor selecionado (single-select). */
  value?: string;
  /** Valores selecionados (multi-select). */
  values?: string[];
  /** Callback ao mudar seleção (single). */
  onChange?: (value: string) => void;
  /** Callback ao mudar seleção (multi). */
  onMultiChange?: (values: string[]) => void;
  /** Placeholder do input. */
  placeholder?: string;
  /** Label. */
  label?: string;
  /** Erro. */
  error?: string;
  /** Permite criar novas opções. */
  creatable?: boolean;
  /** Callback ao criar opção. */
  onCreate?: (value: string) => void;
  /** Multi-select mode. */
  multi?: boolean;
  /** Desabilitado. */
  disabled?: boolean;
  className?: string;
}

/**
 * ComboBox / Searchable Select com suporte a single/multi select e criação inline.
 *
 * WCAG 2.1 AA compliant:
 * - role="combobox" com aria-expanded, aria-controls, aria-activedescendant
 * - Keyboard: Arrow keys, Enter, Escape
 */
export function ComboBox({
  options,
  value,
  values,
  onChange,
  onMultiChange,
  placeholder,
  label,
  error,
  creatable = false,
  onCreate,
  multi = false,
  disabled = false,
  className,
}: ComboBoxProps) {
  const [open, setOpen] = useState(false);
  const { t } = useTranslation();
  const [query, setQuery] = useState('');
  const [activeIndex, setActiveIndex] = useState(-1);
  const inputRef = useRef<HTMLInputElement>(null);
  const listRef = useRef<HTMLDivElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const listboxId = useId();
  const inputId = useId();

  const filtered = useMemo(() => {
    if (!query.trim()) return options;
    const q = query.toLowerCase();
    return options.filter((o) => o.label.toLowerCase().includes(q));
  }, [options, query]);

  const showCreateOption = creatable && query.trim() && !options.some((o) => o.label.toLowerCase() === query.toLowerCase());

  // Close on outside click
  useEffect(() => {
    if (!open) return;
    const handleClick = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false);
        setQuery('');
      }
    };
    document.addEventListener('mousedown', handleClick);
    return () => document.removeEventListener('mousedown', handleClick);
  }, [open]);

  // Scroll active option into view
  useEffect(() => {
    if (activeIndex < 0 || !listRef.current) return;
    const el = listRef.current.querySelector(`[data-index="${activeIndex}"]`);
    el?.scrollIntoView({ block: 'nearest' });
  }, [activeIndex]);

  const selectOption = useCallback(
    (optValue: string) => {
      if (multi) {
        const current = values ?? [];
        const next = current.includes(optValue)
          ? current.filter((v) => v !== optValue)
          : [...current, optValue];
        onMultiChange?.(next);
      } else {
        onChange?.(optValue);
        setOpen(false);
        setQuery('');
      }
    },
    [multi, values, onChange, onMultiChange],
  );

  const removeValue = useCallback(
    (val: string) => {
      if (!multi || !onMultiChange) return;
      onMultiChange((values ?? []).filter((v) => v !== val));
    },
    [multi, values, onMultiChange],
  );

  const handleCreate = useCallback(() => {
    if (!creatable || !query.trim()) return;
    onCreate?.(query.trim());
    setQuery('');
    if (!multi) setOpen(false);
  }, [creatable, query, onCreate, multi]);

  const handleKeyDown = useCallback(
    (e: KeyboardEvent<HTMLInputElement>) => {
      const totalItems = filtered.length + (showCreateOption ? 1 : 0);

      switch (e.key) {
        case 'ArrowDown':
          e.preventDefault();
          if (!open) { setOpen(true); setActiveIndex(0); return; }
          setActiveIndex((prev) => (prev + 1) % totalItems);
          break;
        case 'ArrowUp':
          e.preventDefault();
          setActiveIndex((prev) => (prev - 1 + totalItems) % totalItems);
          break;
        case 'Enter':
          e.preventDefault();
          if (activeIndex >= 0 && activeIndex < filtered.length) {
            const opt = filtered[activeIndex];
            if (!opt.disabled) selectOption(opt.value);
          } else if (showCreateOption && activeIndex === filtered.length) {
            handleCreate();
          }
          break;
        case 'Escape':
          e.preventDefault();
          setOpen(false);
          setQuery('');
          break;
        case 'Backspace':
          if (multi && !query && values?.length) {
            removeValue(values[values.length - 1]);
          }
          break;
      }
    },
    [open, activeIndex, filtered, showCreateOption, selectOption, handleCreate, multi, query, values, removeValue],
  );

  const selectedLabels = useMemo(() => {
    if (!multi) return [];
    return (values ?? []).map((v) => {
      const opt = options.find((o) => o.value === v);
      return { value: v, label: opt?.label ?? v };
    });
  }, [multi, values, options]);

  const displayValue = useMemo(() => {
    if (multi) return query;
    if (open) return query;
    if (!value) return '';
    return options.find((o) => o.value === value)?.label ?? value;
  }, [multi, open, query, value, options]);

  const hasError = Boolean(error);
  const activeDescendant = activeIndex >= 0 ? `${listboxId}-opt-${activeIndex}` : undefined;

  return (
    <div className={cn('flex flex-col gap-1.5', className)} ref={containerRef}>
      {label && (
        <label htmlFor={inputId} className="text-sm font-medium text-body">
          {label}
        </label>
      )}

      <div
        className={cn(
          'flex flex-wrap items-center gap-1 rounded-lg bg-input border px-3 min-h-[44px]',
          'transition-colors',
          'focus-within:border-edge-focus focus-within:shadow-glow-cyan',
          disabled && 'opacity-50 cursor-not-allowed',
          hasError ? 'border-danger shadow-glow-danger' : 'border-edge hover:border-edge-strong',
        )}
      >
        {/* Multi-select tags */}
        {multi && selectedLabels.map(({ value: v, label: l }) => (
          <span
            key={v}
            className="inline-flex items-center gap-1 rounded-sm bg-accent-muted text-xs text-heading px-2 py-0.5"
          >
            {l}
            <button
              type="button"
              onClick={(e) => { e.stopPropagation(); removeValue(v); }}
              className="text-muted hover:text-heading"
              aria-label={`Remove ${l}`}
            >
              <X size={12} />
            </button>
          </span>
        ))}

        <div className="flex-1 flex items-center gap-1 min-w-[80px]">
          <Search size={14} className="text-muted shrink-0" />
          <input
            ref={inputRef}
            id={inputId}
            type="text"
            role="combobox"
            aria-expanded={open}
            aria-controls={open ? listboxId : undefined}
            aria-activedescendant={activeDescendant}
            aria-invalid={hasError || undefined}
            value={displayValue}
            placeholder={multi && selectedLabels.length > 0 ? '' : placeholder}
            disabled={disabled}
            onChange={(e) => {
              setQuery(e.target.value);
              if (!open) setOpen(true);
              setActiveIndex(-1);
            }}
            onFocus={() => setOpen(true)}
            onKeyDown={handleKeyDown}
            className="flex-1 bg-transparent text-sm text-heading outline-none placeholder:text-muted py-2"
          />
          <ChevronDown
            size={14}
            className={cn('text-muted shrink-0 transition-transform', open && 'rotate-180')}
          />
        </div>
      </div>

      {/* Listbox */}
      {open && (
        <div
          ref={listRef}
          id={listboxId}
          role="listbox"
          aria-multiselectable={multi || undefined}
          className={cn(
            'z-[var(--z-dropdown)] max-h-60 overflow-y-auto',
            'rounded-lg bg-panel border border-edge shadow-floating animate-fade-in',
            'py-1',
          )}
        >
          {filtered.length === 0 && !showCreateOption && (
            <div className="px-3 py-2 text-xs text-muted text-center">{t('common.noResults', 'No results')}</div>
          )}
          {filtered.map((opt, i) => {
            const isSelected = multi ? (values ?? []).includes(opt.value) : value === opt.value;
            return (
              <button
                key={opt.value}
                id={`${listboxId}-opt-${i}`}
                data-index={i}
                role="option"
                aria-selected={isSelected}
                aria-disabled={opt.disabled || undefined}
                tabIndex={-1}
                onClick={() => !opt.disabled && selectOption(opt.value)}
                className={cn(
                  'flex w-full items-center gap-2 px-3 py-2 text-sm transition-colors',
                  opt.disabled
                    ? 'opacity-40 cursor-not-allowed'
                    : 'hover:bg-hover cursor-pointer',
                  i === activeIndex && 'bg-hover',
                  isSelected && 'text-cyan font-medium',
                )}
              >
                {isSelected && <span className="shrink-0 text-cyan"><Check size={14} /></span>}
                <span className={cn(!isSelected && 'pl-[22px]')}>{opt.label}</span>
              </button>
            );
          })}
          {showCreateOption && (
            <button
              id={`${listboxId}-opt-${filtered.length}`}
              data-index={filtered.length}
              role="option"
              aria-selected={false}
              tabIndex={-1}
              onClick={handleCreate}
              className={cn(
                'flex w-full items-center gap-2 px-3 py-2 text-sm text-accent hover:bg-hover',
                activeIndex === filtered.length && 'bg-hover',
              )}
            >
              <Plus size={14} />
              Create &ldquo;{query}&rdquo;
            </button>
          )}
        </div>
      )}

      {hasError && (
        <p className="text-xs text-danger" role="alert">{error}</p>
      )}
    </div>
  );
}
