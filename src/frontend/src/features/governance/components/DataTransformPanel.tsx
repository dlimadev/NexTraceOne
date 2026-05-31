/**
 * DataTransformPanel — painel de transformações de dados estilo Grafana panel transformations.
 * Permite aplicar transformações nos resultados de query widgets:
 * - Filter by name: filtrar campos por nome/padrão
 * - Organize fields: reordenar e renomear colunas
 * - Calculate field: criar novos campos via expressão simples
 * - Group by: agregar por campo
 */
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { ArrowUpDown, Filter, Calculator, Group, X, Plus, GripVertical } from 'lucide-react';

// ── Types ──────────────────────────────────────────────────────────────────

export type TransformType = 'filter' | 'organize' | 'calculate' | 'groupBy';

export interface DataTransform {
  id: string;
  type: TransformType;
  enabled: boolean;
  config: Record<string, unknown>;
}

interface DataTransformPanelProps {
  transforms: DataTransform[];
  onChange: (transforms: DataTransform[]) => void;
}

// ── Transform definitions ─────────────────────────────────────────────────

const TRANSFORM_DEFS: { type: TransformType; label: string; icon: React.ReactNode; description: string }[] = [
  { type: 'filter', label: 'Filter by name', icon: <Filter size={12} />, description: 'Keep or remove fields by name pattern' },
  { type: 'organize', label: 'Organize fields', icon: <ArrowUpDown size={12} />, description: 'Reorder and rename fields' },
  { type: 'calculate', label: 'Calculate field', icon: <Calculator size={12} />, description: 'Create a new field from expression' },
  { type: 'groupBy', label: 'Group by', icon: <Group size={12} />, description: 'Aggregate by field' },
];

// ── Sub-components ─────────────────────────────────────────────────────────

function FilterTransformConfig({ config, onChange }: { config: Record<string, unknown>; onChange: (c: Record<string, unknown>) => void }) {
  const { t } = useTranslation();
  const mode = (config.mode as string) ?? 'include';
  const pattern = (config.pattern as string) ?? '';

  return (
    <div className="space-y-2 mt-2">
      <div className="flex gap-2">
        <button
          type="button"
          onClick={() => onChange({ ...config, mode: 'include' })}
          className={`text-[10px] px-2 py-1 rounded border ${mode === 'include' ? 'bg-accent text-white border-accent' : 'border-edge'}`}
        >
          {t('transforms.include', 'Include')}
        </button>
        <button
          type="button"
          onClick={() => onChange({ ...config, mode: 'exclude' })}
          className={`text-[10px] px-2 py-1 rounded border ${mode === 'exclude' ? 'bg-accent text-white border-accent' : 'border-edge'}`}
        >
          {t('transforms.exclude', 'Exclude')}
        </button>
      </div>
      <input
        type="text"
        value={pattern}
        onChange={(e) => onChange({ ...config, pattern: e.target.value })}
        placeholder={t('transforms.patternPlaceholder', 'Field name pattern (e.g., cpu_* or *memory*)')}
        className="w-full rounded border border-edge bg-card text-xs px-2 py-1 text-heading focus:outline-none focus:border-accent"
      />
    </div>
  );
}

function CalculateTransformConfig({ config, onChange }: { config: Record<string, unknown>; onChange: (c: Record<string, unknown>) => void }) {
  const { t } = useTranslation();
  const alias = (config.alias as string) ?? '';
  const expression = (config.expression as string) ?? '';

  return (
    <div className="space-y-2 mt-2">
      <input
        type="text"
        value={alias}
        onChange={(e) => onChange({ ...config, alias: e.target.value })}
        placeholder={t('transforms.aliasPlaceholder', 'New field name')}
        className="w-full rounded border border-edge bg-card text-xs px-2 py-1 text-heading focus:outline-none focus:border-accent"
      />
      <input
        type="text"
        value={expression}
        onChange={(e) => onChange({ ...config, expression: e.target.value })}
        placeholder={t('transforms.expressionPlaceholder', 'Expression: $A + $B or $value * 100')}
        className="w-full rounded border border-edge bg-card text-xs px-2 py-1 text-heading font-mono focus:outline-none focus:border-accent"
      />
    </div>
  );
}

function GroupByTransformConfig({ config, onChange }: { config: Record<string, unknown>; onChange: (c: Record<string, unknown>) => void }) {
  const { t } = useTranslation();
  const field = (config.field as string) ?? '';
  const aggregation = (config.aggregation as string) ?? 'sum';

  return (
    <div className="space-y-2 mt-2">
      <input
        type="text"
        value={field}
        onChange={(e) => onChange({ ...config, field: e.target.value })}
        placeholder={t('transforms.groupByField', 'Group by field')}
        className="w-full rounded border border-edge bg-card text-xs px-2 py-1 text-heading focus:outline-none focus:border-accent"
      />
      <select
        value={aggregation}
        onChange={(e) => onChange({ ...config, aggregation: e.target.value })}
        className="w-full rounded border border-edge bg-card text-xs px-2 py-1 text-heading focus:outline-none focus:border-accent"
      >
        <option value="sum">Sum</option>
        <option value="avg">Average</option>
        <option value="min">Min</option>
        <option value="max">Max</option>
        <option value="count">Count</option>
      </select>
    </div>
  );
}

function TransformConfig({ transform, onChange }: { transform: DataTransform; onChange: (t: DataTransform) => void }) {
  const handleConfigChange = useCallback((config: Record<string, unknown>) => {
    onChange({ ...transform, config });
  }, [transform, onChange]);

  switch (transform.type) {
    case 'filter':
      return <FilterTransformConfig config={transform.config} onChange={handleConfigChange} />;
    case 'calculate':
      return <CalculateTransformConfig config={transform.config} onChange={handleConfigChange} />;
    case 'groupBy':
      return <GroupByTransformConfig config={transform.config} onChange={handleConfigChange} />;
    default:
      return <p className="text-xs text-faded mt-2">Configure in query settings</p>;
  }
}

// ── Main component ─────────────────────────────────────────────────────────

export function DataTransformPanel({ transforms, onChange }: DataTransformPanelProps) {
  const { t } = useTranslation();
  const [showAdd, setShowAdd] = useState(false);

  const addTransform = useCallback((type: TransformType) => {
    const newTransform: DataTransform = {
      id: `tf-${Date.now()}`,
      type,
      enabled: true,
      config: {},
    };
    onChange([...transforms, newTransform]);
    setShowAdd(false);
  }, [transforms, onChange]);

  const removeTransform = useCallback((id: string) => {
    onChange(transforms.filter(t => t.id !== id));
  }, [transforms, onChange]);

  const toggleEnabled = useCallback((id: string) => {
    onChange(transforms.map(t => t.id === id ? { ...t, enabled: !t.enabled } : t));
  }, [transforms, onChange]);

  const moveTransform = useCallback((index: number, direction: number) => {
    const newIndex = index + direction;
    if (newIndex < 0 || newIndex >= transforms.length) return;
    const newTransforms = [...transforms];
    const [moved] = newTransforms.splice(index, 1);
    newTransforms.splice(newIndex, 0, moved);
    onChange(newTransforms);
  }, [transforms, onChange]);

  return (
    <div className="border-t border-edge pt-2 mt-2">
      <div className="flex items-center justify-between mb-2">
        <p className="text-xs font-semibold text-muted uppercase tracking-wide">
          {t('transforms.title', 'Transformations')}
        </p>
        <button
          type="button"
          onClick={() => setShowAdd(v => !v)}
          className="text-xs text-blue-500 hover:text-blue-400 flex items-center gap-0.5"
        >
          <Plus size={10} />
          {t('transforms.add', 'Add')}
        </button>
      </div>

      {showAdd && (
        <div className="grid grid-cols-2 gap-1 mb-2">
          {TRANSFORM_DEFS.map(def => (
            <button
              key={def.type}
              type="button"
              onClick={() => addTransform(def.type)}
              className="flex flex-col items-start gap-0.5 rounded border border-edge bg-elevated p-1.5 text-left hover:border-accent transition-colors"
            >
              <span className="flex items-center gap-1 text-[10px] font-medium text-body">
                {def.icon}
                {def.label}
              </span>
              <span className="text-[9px] text-faded leading-tight">{def.description}</span>
            </button>
          ))}
        </div>
      )}

      <div className="space-y-1">
        {transforms.map((transform, index) => {
          const def = TRANSFORM_DEFS.find(d => d.type === transform.type);
          return (
            <div
              key={transform.id}
              className={`rounded border ${transform.enabled ? 'border-edge' : 'border-edge opacity-50'} bg-card`}
            >
              <div className="flex items-center gap-1 px-2 py-1">
                <GripVertical size={10} className="text-faded cursor-grab" />
                <input
                  type="checkbox"
                  checked={transform.enabled}
                  onChange={() => toggleEnabled(transform.id)}
                  className="rounded"
                />
                <span className="text-[10px] font-medium text-body flex items-center gap-1">
                  {def?.icon}
                  {def?.label}
                </span>
                <div className="ml-auto flex items-center gap-0.5">
                  <button
                    type="button"
                    onClick={() => moveTransform(index, -1)}
                    disabled={index === 0}
                    className="text-faded hover:text-muted disabled:opacity-30 text-[10px]"
                  >
                    ↑
                  </button>
                  <button
                    type="button"
                    onClick={() => moveTransform(index, 1)}
                    disabled={index === transforms.length - 1}
                    className="text-faded hover:text-muted disabled:opacity-30 text-[10px]"
                  >
                    ↓
                  </button>
                  <button
                    type="button"
                    onClick={() => removeTransform(transform.id)}
                    className="text-faded hover:text-red-400 p-0.5"
                  >
                    <X size={10} />
                  </button>
                </div>
              </div>
              {transform.enabled && (
                <div className="px-2 pb-2">
                  <TransformConfig transform={transform} onChange={(t) => {
                    onChange(transforms.map(x => x.id === t.id ? t : x));
                  }} />
                </div>
              )}
            </div>
          );
        })}
      </div>

      {transforms.length === 0 && (
        <p className="text-xs text-faded italic">
          {t('transforms.none', 'No transformations. Click Add to create one.')}
        </p>
      )}
    </div>
  );
}
