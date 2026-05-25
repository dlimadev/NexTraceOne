/**
 * KibanaQueryBar — barra de query estilo Kibana no topo do dashboard.
 * Permite busca em linguagem natural ou sintaxe simplificada que filtra
 * todos os widgets do dashboard (logs, traces, métricas).
 */
import { useState, useRef, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { Search, X, Clock, Filter } from 'lucide-react';

// ── Types ──────────────────────────────────────────────────────────────────

export interface KibanaQueryBarProps {
  value: string;
  onChange: (value: string) => void;
  onSubmit: (value: string) => void;
  className?: string;
}

// ── Suggested queries ──────────────────────────────────────────────────────

const SUGGESTIONS = [
  { label: 'Errors only', query: 'level:error OR status:5xx' },
  { label: 'Slow traces', query: 'duration:>500ms' },
  { label: 'Service: auth', query: 'service:auth-service' },
  { label: 'Last hour', query: '@timestamp:[now-1h TO now]' },
];

// ── Component ──────────────────────────────────────────────────────────────

export function KibanaQueryBar({ value, onChange, onSubmit, className = '' }: KibanaQueryBarProps) {
  const { t } = useTranslation();
  const [isFocused, setIsFocused] = useState(false);
  const [showSuggestions, setShowSuggestions] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!showSuggestions) return;
    function handleOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setShowSuggestions(false);
      }
    }
    document.addEventListener('mousedown', handleOutside);
    return () => document.removeEventListener('mousedown', handleOutside);
  }, [showSuggestions]);

  const handleKeyDown = useCallback((e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      onSubmit(value);
      setShowSuggestions(false);
    }
    if (e.key === 'Escape') {
      setShowSuggestions(false);
      inputRef.current?.blur();
    }
  }, [value, onSubmit]);

  const handleSuggestionClick = useCallback((query: string) => {
    onChange(query);
    onSubmit(query);
    setShowSuggestions(false);
  }, [onChange, onSubmit]);

  return (
    <div ref={containerRef} className={`relative ${className}`}>
      <div
        className={`flex items-center gap-2 rounded-lg border bg-white dark:bg-gray-900 px-3 py-2 transition-all ${
          isFocused
            ? 'border-accent shadow-sm ring-1 ring-accent/20'
            : 'border-gray-200 dark:border-gray-700'
        }`}
      >
        <Search size={14} className="text-gray-400 shrink-0" />
        <input
          ref={inputRef}
          type="text"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          onFocus={() => { setIsFocused(true); setShowSuggestions(true); }}
          onBlur={() => setIsFocused(false)}
          onKeyDown={handleKeyDown}
          placeholder={t('dashboard.queryBarPlaceholder', 'Search logs, traces, metrics... (e.g., level:error service:auth)')}
          className="flex-1 bg-transparent text-xs text-gray-900 dark:text-white placeholder:text-gray-400 focus:outline-none"
        />
        {value && (
          <button
            type="button"
            onClick={() => { onChange(''); onSubmit(''); }}
            className="p-0.5 rounded hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-400"
          >
            <X size={12} />
          </button>
        )}
        <button
          type="button"
          onClick={() => onSubmit(value)}
          className="rounded bg-accent px-2.5 py-1 text-[10px] font-semibold text-white hover:bg-accent/90 transition-colors"
        >
          {t('common.search', 'Search')}
        </button>
      </div>

      {/* Suggestions dropdown */}
      {showSuggestions && (
        <div className="absolute top-full left-0 right-0 z-50 mt-1 rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 shadow-xl py-1">
          <div className="px-3 py-1.5 border-b border-gray-100 dark:border-gray-800">
            <span className="text-[10px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">
              {t('dashboard.querySuggestions', 'Suggested queries')}
            </span>
          </div>
          {SUGGESTIONS.map((s) => (
            <button
              key={s.query}
              type="button"
              onClick={() => handleSuggestionClick(s.query)}
              className="flex items-center gap-2 w-full px-3 py-2 text-xs text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors"
            >
              <Clock size={10} className="text-gray-400 shrink-0" />
              <span className="font-medium">{s.label}</span>
              <span className="text-gray-400 font-mono ml-auto">{s.query}</span>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
