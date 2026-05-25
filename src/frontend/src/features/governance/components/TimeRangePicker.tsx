/**
 * TimeRangePicker — seletor de período estilo Grafana com quick ranges, intervalo absoluto
 * e expressões relativas avançadas (now-1h, now/d, today, yesterday, etc.).
 */
import { useState, useEffect, useRef, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { Clock, ChevronDown, Calendar, Check, Terminal } from 'lucide-react';

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
  category: 'relative' | 'calendar';
}

// ── Quick range definitions (Grafana-like) ─────────────────────────────────

const QUICK_RANGES: QuickRange[] = [
  // Relative
  { value: 'now-1h',  labelKey: 'timeRangePicker.last1Hour',   label: 'Last 1 hour',   category: 'relative' },
  { value: 'now-6h',  labelKey: 'timeRangePicker.last6Hours',  label: 'Last 6 hours',  category: 'relative' },
  { value: 'now-24h', labelKey: 'timeRangePicker.last24Hours', label: 'Last 24 hours', category: 'relative' },
  { value: 'now-7d',  labelKey: 'timeRangePicker.last7Days',   label: 'Last 7 days',   category: 'relative' },
  { value: 'now-30d', labelKey: 'timeRangePicker.last30Days',  label: 'Last 30 days',  category: 'relative' },
  { value: 'now-90d', labelKey: 'timeRangePicker.last90Days',  label: 'Last 90 days',  category: 'relative' },
  // Calendar
  { value: 'today',     labelKey: 'timeRangePicker.today',     label: 'Today',         category: 'calendar' },
  { value: 'yesterday', labelKey: 'timeRangePicker.yesterday', label: 'Yesterday',     category: 'calendar' },
  { value: 'week',      labelKey: 'timeRangePicker.thisWeek',  label: 'This week',     category: 'calendar' },
  { value: 'month',     labelKey: 'timeRangePicker.thisMonth', label: 'This month',    category: 'calendar' },
];

// ── Helper functions (exported) ────────────────────────────────────────────

/**
 * Analisa um valor de período e retorna as datas absolutas correspondentes.
 * Suporta valores relativos ('24h', '7d', 'now-1h', 'now/d'), absolutos ('abs:ISO|ISO'),
 * e palavras-chave ('today', 'yesterday', 'week', 'month').
 */
export function parseTimeRange(value: string): { from: Date; to: Date } {
  const now = new Date();

  if (value.startsWith('abs:')) {
    const [fromIso, toIso] = value.slice(4).split('|');
    return { from: new Date(fromIso), to: new Date(toIso) };
  }

  // Keyword aliases
  const startOfDay = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  switch (value.toLowerCase()) {
    case 'today':
      return { from: startOfDay, to: now };
    case 'yesterday': {
      const yest = new Date(startOfDay);
      yest.setDate(yest.getDate() - 1);
      return { from: yest, to: new Date(startOfDay.getTime() - 1) };
    }
    case 'week': {
      const weekStart = new Date(startOfDay);
      weekStart.setDate(weekStart.getDate() - weekStart.getDay());
      return { from: weekStart, to: now };
    }
    case 'month': {
      const monthStart = new Date(now.getFullYear(), now.getMonth(), 1);
      return { from: monthStart, to: now };
    }
  }

  // Grafana-like: now-1h, now-7d, now/d
  const grafanaMatch = value.match(/^now(?:([+-]\d+[hdwMy]))?(?:\/([hdwMy]))?$/i);
  if (grafanaMatch) {
    let from = new Date(now);

    // Apply offset
    if (grafanaMatch[1]) {
      const offset = grafanaMatch[1];
      const sign = offset[0] === '+' ? 1 : -1;
      const amount = parseInt(offset.slice(1, -1), 10);
      const unit = offset.slice(-1).toLowerCase();
      switch (unit) {
        case 'h': from.setHours(from.getHours() + sign * amount); break;
        case 'd': from.setDate(from.getDate() + sign * amount); break;
        case 'w': from.setDate(from.getDate() + sign * amount * 7); break;
        case 'm': from.setMonth(from.getMonth() + sign * amount); break;
        case 'y': from.setFullYear(from.getFullYear() + sign * amount); break;
      }
    }

    // Apply snap
    if (grafanaMatch[2]) {
      const snap = grafanaMatch[2].toLowerCase();
      switch (snap) {
        case 'h': from = new Date(from.getFullYear(), from.getMonth(), from.getDate(), from.getHours(), 0, 0); break;
        case 'd': from = new Date(from.getFullYear(), from.getMonth(), from.getDate(), 0, 0, 0); break;
        case 'w': {
          const d = new Date(from.getFullYear(), from.getMonth(), from.getDate(), 0, 0, 0);
          d.setDate(d.getDate() - d.getDay());
          from = d;
          break;
        }
        case 'm': from = new Date(from.getFullYear(), from.getMonth(), 1, 0, 0, 0); break;
        case 'y': from = new Date(from.getFullYear(), 1, 1, 0, 0, 0); break;
      }
    }

    return { from, to: now };
  }

  // Simple relative: "1h", "6h", "24h", "7d", "30d", "90d"
  const match = value.match(/^(\d+)([hd])$/);
  if (match) {
    const amount = parseInt(match[1], 10);
    const unit = match[2].toLowerCase();
    const ms = unit === 'h'
      ? amount * 60 * 60 * 1000
      : amount * 24 * 60 * 60 * 1000;
    return { from: new Date(now.getTime() - ms), to: now };
  }

  // Fallback: last 24 hours
  return { from: new Date(now.getTime() - 24 * 60 * 60 * 1000), to: now };
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
  const [activeTab, setActiveTab] = useState<'quick' | 'absolute' | 'custom'>('quick');
  const [customExpr, setCustomExpr] = useState('');
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

  const handleQuickSelect = useCallback((rangeValue: string) => {
    onChange(rangeValue);
    setIsOpen(false);
  }, [onChange]);

  const handleAbsoluteApply = useCallback(() => {
    if (!absFrom || !absTo) return;
    const fromIso = new Date(absFrom).toISOString();
    const toIso = new Date(absTo).toISOString();
    onChange(`abs:${fromIso}|${toIso}`);
    setIsOpen(false);
  }, [absFrom, absTo, onChange]);

  const handleCustomApply = useCallback(() => {
    if (!customExpr.trim()) return;
    onChange(customExpr.trim());
    setIsOpen(false);
    setCustomExpr('');
  }, [customExpr, onChange]);

  const label = formatTimeRangeLabel(value);
  const resolvedRange = formatResolvedRange(value);

  const relativeRanges = QUICK_RANGES.filter(r => r.category === 'relative');
  const calendarRanges = QUICK_RANGES.filter(r => r.category === 'calendar');

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
          className="absolute left-0 top-full z-50 mt-1 w-[420px] rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 shadow-xl"
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
            <button
              type="button"
              onClick={() => setActiveTab('custom')}
              className={`flex items-center gap-1.5 px-4 py-2.5 text-xs font-medium transition-colors ${
                activeTab === 'custom'
                  ? 'border-b-2 border-accent text-accent -mb-px'
                  : 'text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300'
              }`}
            >
              <Terminal size={12} />
              {t('timeRangePicker.customExpression', 'Custom')}
            </button>
          </div>

          {/* Tab content */}
          <div className="p-3">
            {activeTab === 'quick' ? (
              <div className="space-y-3">
                {/* Relative ranges */}
                <div>
                  <p className="text-[10px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">
                    {t('timeRangePicker.relative', 'Relative')}
                  </p>
                  <div className="grid grid-cols-3 gap-2">
                    {relativeRanges.map((range) => {
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
                </div>

                {/* Calendar ranges */}
                <div>
                  <p className="text-[10px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">
                    {t('timeRangePicker.calendar', 'Calendar')}
                  </p>
                  <div className="grid grid-cols-4 gap-2">
                    {calendarRanges.map((range) => {
                      const isSelected = value === range.value;
                      return (
                        <button
                          key={range.value}
                          type="button"
                          onClick={() => handleQuickSelect(range.value)}
                          className={`flex items-center justify-center gap-1 rounded px-2.5 py-2 text-xs font-medium transition-colors ${
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
                </div>
              </div>
            ) : activeTab === 'absolute' ? (
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
            ) : (
              <div className="flex flex-col gap-3">
                <label className="flex flex-col gap-1">
                  <span className="text-xs font-medium text-gray-600 dark:text-gray-400">
                    {t('timeRangePicker.customExpressionLabel', 'Expression')}
                  </span>
                  <input
                    type="text"
                    value={customExpr}
                    onChange={(e) => setCustomExpr(e.target.value)}
                    placeholder="now-1h, now-7d, now/d, today..."
                    className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1.5 text-xs text-gray-900 dark:text-white font-mono focus:outline-none focus:ring-1 focus:ring-accent"
                  />
                </label>
                <div className="rounded bg-gray-50 dark:bg-gray-800 p-2 space-y-1">
                  <p className="text-[10px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    {t('timeRangePicker.examples', 'Examples')}
                  </p>
                  {['now-1h', 'now-7d', 'now/d', 'now-1d/d', 'today', 'yesterday'].map(ex => (
                    <button
                      key={ex}
                      type="button"
                      onClick={() => { setCustomExpr(ex); }}
                      className="block text-[10px] text-blue-500 hover:text-blue-400 font-mono"
                    >
                      {ex}
                    </button>
                  ))}
                </div>
                <button
                  type="button"
                  onClick={handleCustomApply}
                  disabled={!customExpr.trim()}
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
