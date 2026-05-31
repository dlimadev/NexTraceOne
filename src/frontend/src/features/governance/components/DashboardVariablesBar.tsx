// src/frontend/src/features/governance/components/DashboardVariablesBar.tsx
/**
 * DashboardVariablesBar — horizontal toolbar below the dashboard title bar.
 * Renders one dropdown per DashboardVariable, plus the global TimeRangePicker
 * and auto-refresh control. "+ Variável" button opens the add-variable modal.
 */
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus, RefreshCw, Clock } from 'lucide-react';
import { type DashboardVariable } from '../types/dashboardBuilder';
import { TimeRangePicker } from './TimeRangePicker';

const REFRESH_OPTIONS = [
  { label: 'Off', value: '' },
  { label: '5s', value: '5000' },
  { label: '30s', value: '30000' },
  { label: '1m', value: '60000' },
  { label: '5m', value: '300000' },
] as const;

const INTERVAL_OPTIONS = ['1m', '5m', '15m', '30m', '1h', '3h', '6h', '12h', '1d'] as const;

// ── Add Variable Modal ─────────────────────────────────────────────────────

interface AddVariableModalProps {
  onAdd: (variable: DashboardVariable) => void;
  onClose: () => void;
}

function AddVariableModal({ onAdd, onClose }: AddVariableModalProps) {
  const { t } = useTranslation();
  const [name, setName] = useState('');
  const [label, setLabel] = useState('');
  const [type, setType] = useState<'custom' | 'text' | 'interval'>('custom');
  const [optionsRaw, setOptionsRaw] = useState('');
  const [multi, setMulti] = useState(false);
  const [includeAll, setIncludeAll] = useState(false);
  const [error, setError] = useState('');

  const handleAdd = () => {
    const trimmedName = name.trim();
    if (!trimmedName) { setError(t('governance.dashboardBuilder.variablesBar.modal.nameRequired')); return; }
    if (trimmedName.includes('$')) { setError(t('governance.dashboardBuilder.variablesBar.modal.nameNoDollar')); return; }
    const options: string[] = type === 'interval'
      ? [...INTERVAL_OPTIONS]
      : optionsRaw.split(',').map((s) => s.trim()).filter(Boolean);
    onAdd({
      name: trimmedName,
      label: label.trim() || trimmedName,
      type,
      options,
      value: multi ? [] : (options[0] ?? ''),
      multi,
      includeAll,
    });
    onClose();
  };

  return (
    <div
      className="fixed inset-0 z-[60] flex items-center justify-center bg-black/60"
      onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}
    >
      <div className="bg-card rounded-lg shadow-2xl border border-edge w-[400px] p-5 flex flex-col gap-4">
        <h3 className="text-sm font-semibold text-heading">
          {t('governance.dashboardBuilder.variablesBar.addVariable', '+ Variable')}
        </h3>

        {error && <p className="text-xs text-red-500">{error}</p>}

        <div className="flex flex-col gap-3">
          <div>
            <label className="text-xs font-medium text-muted mb-1 block">
              {t('governance.dashboardBuilder.variablesBar.modal.nameLabel')}
            </label>
            <input
              type="text"
              value={name}
              onChange={(e) => { setName(e.target.value); setError(''); }}
              placeholder="service"
              className="w-full rounded border border-edge bg-card text-xs px-2 py-1.5 text-heading focus:outline-none focus:border-accent font-mono"
            />
          </div>
          <div>
            <label className="text-xs font-medium text-muted mb-1 block">
              {t('governance.dashboardBuilder.variablesBar.modal.labelLabel')}
            </label>
            <input
              type="text"
              value={label}
              onChange={(e) => setLabel(e.target.value)}
              placeholder="Serviço"
              className="w-full rounded border border-edge bg-card text-xs px-2 py-1.5 text-heading focus:outline-none focus:border-accent"
            />
          </div>
          <div>
            <label className="text-xs font-medium text-muted mb-1 block">
              {t('governance.dashboardBuilder.variablesBar.modal.typeLabel')}
            </label>
            <select
              value={type}
              onChange={(e) => setType(e.target.value as 'custom' | 'text' | 'interval')}
              className="w-full rounded border border-edge bg-card text-xs px-2 py-1.5 text-heading focus:outline-none focus:border-accent"
            >
              <option value="custom">Custom (fixed list)</option>
              <option value="text">Text (free input)</option>
              <option value="interval">Interval (1m, 5m, 1h…)</option>
            </select>
          </div>
          {type === 'custom' && (
            <div>
              <label className="text-xs font-medium text-muted mb-1 block">
                {t('governance.dashboardBuilder.variablesBar.modal.optionsLabel')}
              </label>
              <input
                type="text"
                value={optionsRaw}
                onChange={(e) => setOptionsRaw(e.target.value)}
                placeholder="production, staging, dev"
                className="w-full rounded border border-edge bg-card text-xs px-2 py-1.5 text-heading focus:outline-none focus:border-accent font-mono"
              />
            </div>
          )}
          <div className="flex items-center gap-4">
            <label className="flex items-center gap-1.5 text-xs text-muted cursor-pointer">
              <input type="checkbox" checked={multi} onChange={(e) => setMulti(e.target.checked)} className="rounded" />
              {t('governance.dashboardBuilder.variablesBar.modal.multiValue')}
            </label>
            <label className="flex items-center gap-1.5 text-xs text-muted cursor-pointer">
              <input type="checkbox" checked={includeAll} onChange={(e) => setIncludeAll(e.target.checked)} className="rounded" />
              {t('governance.dashboardBuilder.variablesBar.modal.includeAll')}
            </label>
          </div>
        </div>

        <div className="flex gap-2 justify-end">
          <button
            type="button"
            onClick={onClose}
            className="text-xs px-3 py-1.5 rounded border border-edge text-muted hover:bg-hover dark:hover:bg-elevated"
          >
            {t('governance.dashboardBuilder.variablesBar.modal.cancel')}
          </button>
          <button
            type="button"
            onClick={handleAdd}
            className="text-xs px-3 py-1.5 rounded bg-accent text-white hover:bg-accent/80 font-semibold"
          >
            {t('governance.dashboardBuilder.variablesBar.modal.add')}
          </button>
        </div>
      </div>
    </div>
  );
}

// ── DashboardVariablesBar ──────────────────────────────────────────────────

export interface DashboardVariablesBarProps {
  variables: DashboardVariable[];
  timeRange: string;
  isReadOnly?: boolean;
  onVariableChange: (name: string, value: string | string[]) => void;
  onTimeRangeChange: (range: string) => void;
  onAddVariable: (variable: DashboardVariable) => void;
}

export function DashboardVariablesBar({
  variables,
  timeRange,
  isReadOnly = false,
  onVariableChange,
  onTimeRangeChange,
  onAddVariable,
}: DashboardVariablesBarProps) {
  const { t } = useTranslation();
  const [showAddModal, setShowAddModal] = useState(false);
  const [refreshInterval, setRefreshInterval] = useState('');

  // Don't render anything if readOnly and no variables
  if (variables.length === 0 && isReadOnly) return null;

  return (
    <>
      <div className="flex items-center gap-2 flex-wrap px-4 py-2 border-b border-edge bg-canvas/50">

        {/* Variable dropdowns */}
        {variables.map((v) => (
          <div key={v.name} className="flex items-center gap-1.5">
            <label className="text-[10px] text-muted whitespace-nowrap">
              {v.label}
            </label>

            {v.type === 'text' ? (
              <input
                type="text"
                value={typeof v.value === 'string' ? v.value : v.value.join(',')}
                onChange={(e) => onVariableChange(v.name, e.target.value)}
                className="rounded border border-edge bg-card text-[11px] px-2 py-1 text-purple-500 dark:text-purple-400 font-mono min-w-[80px] focus:outline-none focus:border-accent"
              />
            ) : (
              <select
                value={typeof v.value === 'string' ? v.value : (v.value[0] ?? '')}
                onChange={(e) => {
                  if (v.multi) {
                    const current = Array.isArray(v.value) ? v.value : [v.value];
                    const val = e.target.value;
                    const next = current.includes(val)
                      ? current.filter((x) => x !== val)
                      : [...current, val];
                    onVariableChange(v.name, next);
                  } else {
                    onVariableChange(v.name, e.target.value);
                  }
                }}
                className="rounded border border-edge bg-card text-[11px] px-2 py-1 text-purple-500 dark:text-purple-400 font-mono min-w-[100px] focus:outline-none focus:border-accent cursor-pointer"
              >
                {v.includeAll && (
                  <option value="*">{t('governance.dashboardBuilder.variablesBar.allValues', 'All')}</option>
                )}
                {v.options.map((opt) => (
                  <option key={opt} value={opt}>{opt}</option>
                ))}
              </select>
            )}
          </div>
        ))}

        {/* Divider (only when there are variables) */}
        {variables.length > 0 && (
          <div className="w-px h-5 bg-elevated mx-1 shrink-0" />
        )}

        {/* Time range picker */}
        <div className="flex items-center gap-1.5">
          <Clock size={11} className="text-yellow-500 shrink-0" />
          <TimeRangePicker
            value={timeRange}
            onChange={onTimeRangeChange}
          />
        </div>

        {/* Refresh selector */}
        <div className="flex items-center gap-1">
          <button
            type="button"
            className="flex items-center gap-1 px-2 py-1 rounded border border-edge bg-card text-[11px] text-muted hover:border-accent/50 transition-colors"
            title={t('governance.dashboardBuilder.variablesBar.refresh', 'Refresh')}
          >
            <RefreshCw size={10} />
            <select
              value={refreshInterval}
              onChange={(e) => setRefreshInterval(e.target.value)}
              className="bg-transparent border-none outline-none text-[11px] cursor-pointer"
              onClick={(e) => e.stopPropagation()}
            >
              {REFRESH_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>{opt.label}</option>
              ))}
            </select>
          </button>
        </div>

        <div className="flex-1" />

        {/* Add variable button */}
        {!isReadOnly && (
          <button
            type="button"
            onClick={() => setShowAddModal(true)}
            className="flex items-center gap-1 text-[11px] px-2.5 py-1 rounded border border-dashed border-edge text-faded hover:border-accent hover:text-accent transition-colors"
          >
            <Plus size={10} />
            {t('governance.dashboardBuilder.variablesBar.addVariable', '+ Variable')}
          </button>
        )}
      </div>

      {showAddModal && (
        <AddVariableModal
          onAdd={(v) => { onAddVariable(v); setShowAddModal(false); }}
          onClose={() => setShowAddModal(false)}
        />
      )}
    </>
  );
}
