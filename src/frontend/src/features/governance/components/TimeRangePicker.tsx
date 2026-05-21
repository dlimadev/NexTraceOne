/**
 * TimeRangePicker — seletor de período estilo Grafana com quick ranges e intervalo absoluto.
 * Suporta valores relativos ('1h', '6h', '24h', '7d', '30d', '90d') e
 * valores absolutos no formato 'abs:ISO|ISO'.
 */
import { useState, useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { Clock, ChevronDown, Calendar, Check } from 'lucide-react';

// ── Types ──────────────────────────────────────────────────────────────────

export interface TimeRangePickerProps {
  value: string;
  onChange: (value: string) => void;
  className?: string;
}

interface QuickRange {
  value: string;
  labelKey: string;
  label: string;
}

// ── Quick range definitions ────────────────────────────────────────────────

const QUICK_RANGES: QuickRange[] = [
  { value: '1h',  labelKey: 'timeRangePicker.last1Hour',   label: 'Last 1 hour' },
  { value: '6h',  labelKey: 'timeRangePicker.last6Hours',  label: 'Last 6 hours' },
  { value: '24h', labelKey: 'timeRangePicker.last24Hours', label: 'Last 24 hours' },
  { value: '7d',  labelKey: 'timeRangePicker.last7Days',   label: 'Last 7 days' },
  { value: '30d', labelKey: 'timeRangePicker.last30Days',  label: 'Last 30 days' },
  { value: '90d', labelKey: 'timeRangePicker.last90Days',  label: 'Last 90 days' },
];

// ── Helper functions (exported) ────────────────────────────────────────────

/**
 * Analisa um valor de período e retorna as datas absolutas correspondentes.
 * Suporta valores relativos ('24h', '7d') e absolutos ('abs:ISO|ISO').
 */
export function parseTimeRange(value: string): { from: Date; to: Date } {
  if (value.startsWith('abs:')) {
    const [fromIso, toIso] = value.slice(4).split('|');
    return { from: new Date(fromIso), to: new Date(toIso) };
  }

  const now = new Date();
  const match = value.match(/^(\d+)(h|d)$/);
  if (!match) {
    // Fallback: last 24 hours
    return { from: new Date(now.getTime() - 24 * 60 * 60 * 1000), to: now };
  }

  const amount = parseInt(match[1], 10);
  const unit = match[2];
  const ms = unit === 'h'
    ? amount * 60 * 60 * 1000
    : amount * 24 * 60 * 60 * 1000;

  return { from: new Date(now.getTime() - ms), to: now };
}

/**
 * Formata um valor de período em rótulo legível para o utilizador.
 */
export function formatTimeRangeLabel(value: string): string {
  if (value.startsWith('abs:')) {
    const [fromIso, toIso] = value.slice(4).split('|');
    const from = new Date(fromIso);
    const to = new Date(toIso);
    const opts: Intl.DateTimeFormatOptions = { month: 'short', day: 'numeric' };
    return `${from.toLocaleDateString(undefined, opts)} – ${to.toLocaleDateString(undefined, opts)}`;
  }

  const quick = QUICK_RANGES.find(r => r.value === value);
  return quick ? quick.label : value;
}

// ── Resolved range footer text ─────────────────────────────────────────────

function formatResolvedRange(value: string): string {
  const { from, to } = parseTimeRange(value);
  const opts: Intl.DateTimeFormatOptions = {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  };
  return `${from.toLocaleDateString(undefined, opts)} → ${to.toLocaleDateString(undefined, opts)}`;
}

// ── Convert Date to datetime-local input value ────────────────────────────

function toDatetimeLocal(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

// ── Component ──────────────────────────────────────────────────────────────

export function TimeRangePicker({ value, onChange, className = '' }: TimeRangePickerProps) {
  const { t } = useTranslation();
  const [isOpen, setIsOpen] = useState(false);
  const [activeTab, setActiveTab] = useState<'quick' | 'absolute'>('quick');
  const containerRef = useRef<HTMLDivElement>(null);

  // Absolute range state — initialised from current value if it's absolute
  const initAbsolute = () => {
    if (value.startsWith('abs:')) {
      const { from, to } = parseTimeRange(value);
      return { from: toDatetimeLocal(from), to: toDatetimeLocal(to) };
    }
    const { from, to } = parseTimeRange(value);
    return { from: toDatetimeLocal(from), to: toDatetimeLocal(to) };
  };

  const [absFrom, setAbsFrom] = useState(() => initAbsolute().from);
  const [absTo, setAbsTo] = useState(() => initAbsolute().to);

  // Close on outside click
  useEffect(() => {
    if (!isOpen) return;
    function handleOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    }
    document.addEventListener('mousedown', handleOutside);
    return () => document.removeEventListener('mousedown', handleOutside);
  }, [isOpen]);

  // Close on Escape key
  useEffect(() => {
    if (!isOpen) return;
    function handleKey(e: KeyboardEvent) {
      if (e.key === 'Escape') setIsOpen(false);
    }
    document.addEventListener('keydown', handleKey);
    return () => document.removeEventListener('keydown', handleKey);
  }, [isOpen]);

  function handleQuickSelect(rangeValue: string) {
    onChange(rangeValue);
    setIsOpen(false);
  }

  function handleAbsoluteApply() {
    if (!absFrom || !absTo) return;
    const fromIso = new Date(absFrom).toISOString();
    const toIso = new Date(absTo).toISOString();
    onChange(`abs:${fromIso}|${toIso}`);
    setIsOpen(false);
  }

  const label = formatTimeRangeLabel(value);
  const resolvedRange = formatResolvedRange(value);

  return (
    <div ref={containerRef} className={`relative inline-block ${className}`}>
      {/* Trigger button */}
      <button
        type="button"
        onClick={() => setIsOpen(prev => !prev)}
        aria-expanded={isOpen}
        aria-haspopup="dialog"
        aria-label={t('timeRangePicker.label', 'Time range: {{range}}', { range: label })}
        className="flex items-center gap-1.5 rounded border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 px-2 py-1 text-xs text-gray-700 dark:text-gray-300 hover:border-accent hover:text-accent transition-colors"
      >
        <Clock size={12} className="text-gray-400 dark:text-gray-500 shrink-0" />
        <span className="font-medium">{label}</span>
        <ChevronDown
          size={12}
          className={`shrink-0 transition-transform text-gray-400 ${isOpen ? 'rotate-180' : ''}`}
        />
      </button>

      {/* Dropdown panel */}
      {isOpen && (
        <div
          role="dialog"
          aria-label={t('timeRangePicker.panelLabel', 'Select time range')}
          className="absolute left-0 top-full z-50 mt-1 w-[360px] rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 shadow-xl"
        >
          {/* Tab bar */}
          <div className="flex border-b border-gray-200 dark:border-gray-700">
            <button
              type="button"
              onClick={() => setActiveTab('quick')}
              className={`flex items-center gap-1.5 px-4 py-2.5 text-xs font-medium transition-colors ${
                activeTab === 'quick'
                  ? 'border-b-2 border-accent text-accent -mb-px'
                  : 'text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300'
              }`}
            >
              <Clock size={12} />
              {t('timeRangePicker.quickRanges', 'Quick ranges')}
            </button>
            <button
              type="button"
              onClick={() => setActiveTab('absolute')}
              className={`flex items-center gap-1.5 px-4 py-2.5 text-xs font-medium transition-colors ${
                activeTab === 'absolute'
                  ? 'border-b-2 border-accent text-accent -mb-px'
                  : 'text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300'
              }`}
            >
              <Calendar size={12} />
              {t('timeRangePicker.absoluteRange', 'Absolute range')}
            </button>
          </div>

          {/* Tab content */}
          <div className="p-3">
            {activeTab === 'quick' ? (
              <div className="grid grid-cols-3 gap-2">
                {QUICK_RANGES.map((range) => {
                  const isSelected = value === range.value;
                  return (
                    <button
                      key={range.value}
                      type="button"
                      onClick={() => handleQuickSelect(range.value)}
                      className={`flex items-center justify-between gap-1 rounded px-2.5 py-2 text-xs font-medium transition-colors ${
                        isSelected
                          ? 'bg-accent text-white'
                          : 'bg-gray-50 dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-accent/10 hover:text-accent dark:hover:bg-accent/20'
                      }`}
                    >
                      <span>{t(range.labelKey, range.label)}</span>
                      {isSelected && <Check size={10} className="shrink-0" />}
                    </button>
                  );
                })}
              </div>
            ) : (
              <div className="flex flex-col gap-3">
                <label className="flex flex-col gap-1">
                  <span className="text-xs font-medium text-gray-600 dark:text-gray-400">
                    {t('timeRangePicker.from', 'From')}
                  </span>
                  <input
                    type="datetime-local"
                    value={absFrom}
                    onChange={(e) => setAbsFrom(e.target.value)}
                    className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1.5 text-xs text-gray-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-accent"
                  />
                </label>
                <label className="flex flex-col gap-1">
                  <span className="text-xs font-medium text-gray-600 dark:text-gray-400">
                    {t('timeRangePicker.until', 'Until')}
                  </span>
                  <input
                    type="datetime-local"
                    value={absTo}
                    onChange={(e) => setAbsTo(e.target.value)}
                    className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1.5 text-xs text-gray-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-accent"
                  />
                </label>
                <button
                  type="button"
                  onClick={handleAbsoluteApply}
                  disabled={!absFrom || !absTo}
                  className="w-full rounded bg-accent px-3 py-1.5 text-xs font-semibold text-white hover:bg-accent/90 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
                >
                  {t('timeRangePicker.apply', 'Apply')}
                </button>
              </div>
            )}
          </div>

          {/* Footer — resolved absolute range */}
          <div className="border-t border-gray-100 dark:border-gray-800 px-3 py-2">
            <p className="text-[10px] text-gray-400 dark:text-gray-500 font-mono">
              {resolvedRange}
            </p>
          </div>
        </div>
      )}
    </div>
  );
}
