/**
 * Builder visual para Data Contracts (CC-03).
 *
 * Permite modelar:
 * - metadados (title, version, owner, sourceSystem, slaFreshnessHours)
 * - colunas com nome, tipo, nullable, classificação PII e descrição
 */
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus, Trash2, ChevronDown, ChevronRight, AlertCircle } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import {
  Field, FieldArea, FieldSelect, FieldCheckbox,
} from './shared/BuilderFormPrimitives';
import { validateDataContractBuilder } from './shared/builderValidation';
import { dataContractBuilderToJson } from './shared/builderSync';
import type {
  DataContractBuilderState,
  DataContractColumn,
  ColumnType,
  PiiLevel,
  BuilderValidationResult,
  SyncResult,
} from './shared/builderTypes';

const COLUMN_TYPES: readonly ColumnType[] = [
  'uuid', 'varchar', 'text', 'int', 'bigint', 'decimal',
  'boolean', 'timestamp', 'date', 'json', 'array', 'other',
];

const PII_LEVELS: readonly PiiLevel[] = ['None', 'Low', 'Medium', 'High', 'Critical'];

const PII_BADGE_CLASSES: Record<PiiLevel, string> = {
  None: 'bg-muted/15 text-muted border-muted/25',
  Low: 'bg-blue/15 text-blue border-blue/25',
  Medium: 'bg-yellow/15 text-yellow border-yellow/25',
  High: 'bg-orange/15 text-orange border-orange/25',
  Critical: 'bg-danger/15 text-danger border-danger/25',
};

let nextId = 1;
function genId() { return `dc-${nextId++}`; }

function createColumn(): DataContractColumn {
  return {
    id: genId(), name: '', type: 'varchar', nullable: true,
    pii: 'None', description: '',
  };
}

const DEFAULT_STATE: DataContractBuilderState = {
  title: '',
  version: '1.0.0',
  owner: '',
  sourceSystem: '',
  slaFreshnessHours: 24,
  description: '',
  columns: [],
};

interface VisualDataContractBuilderProps {
  initialState?: DataContractBuilderState;
  onChange?: (state: DataContractBuilderState) => void;
  onSync?: (result: SyncResult) => void;
  isReadOnly?: boolean;
  className?: string;
}

/**
 * Builder visual para Data Contracts — permite definir colunas com classificação PII,
 * tipo, nullability e SLA de freshness sem editar JSON manualmente.
 */
export function VisualDataContractBuilder({
  initialState,
  onChange,
  onSync,
  isReadOnly = false,
  className = '',
}: VisualDataContractBuilderProps) {
  const { t } = useTranslation();

  const [state, setState] = useState<DataContractBuilderState>(
    initialState ?? DEFAULT_STATE,
  );
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [validation, setValidation] = useState<BuilderValidationResult | null>(null);

  const update = useCallback(
    (partial: Partial<DataContractBuilderState>) => {
      const next = { ...state, ...partial };
      setState(next);
      onChange?.(next);
    },
    [state, onChange],
  );

  const addColumn = () => {
    const col = createColumn();
    update({ columns: [...state.columns, col] });
    setExpandedId(col.id);
  };

  const updateColumn = (id: string, partial: Partial<DataContractColumn>) => {
    update({ columns: state.columns.map((c) => (c.id === id ? { ...c, ...partial } : c)) });
  };

  const removeColumn = (id: string) => {
    update({ columns: state.columns.filter((c) => c.id !== id) });
    if (expandedId === id) setExpandedId(null);
  };

  const handleValidate = () => { setValidation(validateDataContractBuilder(state)); };

  const handleGenerateSource = () => {
    const result = dataContractBuilderToJson(state);
    onSync?.(result);
  };

  return (
    <div className={`space-y-4 p-4 ${className}`}>
      {/* ── Validation banner ── */}
      {validation && !validation.valid && (
        <div className="flex items-start gap-2 px-3 py-2.5 text-xs rounded-md bg-danger/10 border border-danger/20 text-danger">
          <AlertCircle size={14} className="flex-shrink-0 mt-0.5" />
          <div>
            <p className="font-medium">{t('contracts.builder.validation.hasErrors', 'Please fix the following issues:')}</p>
            <ul className="mt-1 space-y-0.5">
              {validation.errors.map((e, i) => (
                <li key={i} className="text-[10px]">• {t(e.messageKey, e.fallback)}</li>
              ))}
            </ul>
          </div>
        </div>
      )}

      {/* ── Data Contract metadata ── */}
      <Card>
        <CardHeader>
          <h3 className="text-xs font-semibold text-heading">
            {t('contracts.builder.dataContract.contractInfo', 'Data Contract Information')}
          </h3>
        </CardHeader>
        <CardBody className="space-y-3">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <Field
              label={t('contracts.builder.dataContract.title', 'Title')}
              value={state.title}
              onChange={(v) => update({ title: v })}
              placeholder={t('contracts.builder.dataContract.titlePlaceholder', 'User Profile Data Contract')}
              required
              disabled={isReadOnly}
            />
            <Field
              label={t('contracts.builder.dataContract.version', 'Version')}
              value={state.version}
              onChange={(v) => update({ version: v })}
              placeholder="1.0.0"
              mono
              disabled={isReadOnly}
            />
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <Field
              label={t('contracts.builder.dataContract.owner', 'Owner')}
              value={state.owner}
              onChange={(v) => update({ owner: v })}
              placeholder={t('contracts.builder.dataContract.ownerPlaceholder', 'team-data-platform')}
              required
              disabled={isReadOnly}
            />
            <Field
              label={t('contracts.builder.dataContract.sourceSystem', 'Source System')}
              value={state.sourceSystem}
              onChange={(v) => update({ sourceSystem: v })}
              placeholder={t('contracts.builder.dataContract.sourceSystemPlaceholder', 'postgres-users-db')}
              required
              mono
              disabled={isReadOnly}
            />
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <Field
              label={t('contracts.builder.dataContract.slaFreshnessHours', 'SLA Freshness (hours)')}
              value={String(state.slaFreshnessHours)}
              onChange={(v) => update({ slaFreshnessHours: parseInt(v, 10) || 0 })}
              placeholder="24"
              mono
              disabled={isReadOnly}
            />
          </div>
          <FieldArea
            label={t('contracts.builder.dataContract.description', 'Description')}
            value={state.description}
            onChange={(v) => update({ description: v })}
            disabled={isReadOnly}
          />
        </CardBody>
      </Card>

      {/* ── Columns ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <h3 className="text-xs font-semibold text-heading">
              {t('contracts.builder.dataContract.columns', 'Columns')} ({state.columns.length})
            </h3>
            {!isReadOnly && (
              <button
                type="button"
                onClick={addColumn}
                className="inline-flex items-center gap-1 px-2 py-1 text-[10px] font-medium rounded-md bg-accent/10 text-accent hover:bg-accent/20 transition-colors"
              >
                <Plus size={10} /> {t('contracts.builder.dataContract.addColumn', 'Add Column')}
              </button>
            )}
          </div>
        </CardHeader>
        <CardBody className="p-0">
          {state.columns.length === 0 && (
            <div className="py-8 text-center text-xs text-muted">
              {t('contracts.builder.dataContract.noColumns', 'No columns yet. Click "Add Column" to define the schema.')}
            </div>
          )}
          <div className="divide-y divide-edge">
            {state.columns.map((col) => {
              const isExpanded = expandedId === col.id;
              return (
                <div key={col.id} className="group">
                  <button
                    type="button"
                    onClick={() => setExpandedId(isExpanded ? null : col.id)}
                    className="w-full flex items-center gap-3 px-4 py-3 text-left hover:bg-elevated/30 transition-colors"
                  >
                    {isExpanded
                      ? <ChevronDown size={12} className="text-muted" />
                      : <ChevronRight size={12} className="text-muted" />}
                    <span className="px-2 py-0.5 text-[10px] font-bold rounded bg-violet/15 text-violet border border-violet/25 uppercase">
                      {col.type}
                    </span>
                    <span className="text-xs font-mono text-heading flex-1 truncate">
                      {col.name || t('contracts.builder.dataContract.unnamedColumn', 'Unnamed Column')}
                    </span>
                    {col.nullable && (
                      <span className="text-[10px] text-muted">nullable</span>
                    )}
                    <span className={`px-1.5 py-0.5 text-[9px] font-bold rounded border ${PII_BADGE_CLASSES[col.pii]}`}>
                      PII:{col.pii}
                    </span>
                    {!isReadOnly && (
                      <button
                        type="button"
                        onClick={(e) => { e.stopPropagation(); removeColumn(col.id); }}
                        className="opacity-0 group-hover:opacity-100 text-muted hover:text-danger transition-all"
                      >
                        <Trash2 size={12} />
                      </button>
                    )}
                  </button>
                  {isExpanded && (
                    <div className="px-4 pb-4 pt-1 bg-elevated/10 space-y-3">
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                        <Field
                          label={t('contracts.builder.dataContract.columnName', 'Column Name')}
                          value={col.name}
                          onChange={(v) => updateColumn(col.id, { name: v })}
                          placeholder={t('contracts.builder.dataContract.columnNamePlaceholder', 'e.g. user_id')}
                          required
                          mono
                          disabled={isReadOnly}
                        />
                        <FieldSelect
                          label={t('contracts.builder.dataContract.columnType', 'Type')}
                          value={col.type}
                          onChange={(v) => updateColumn(col.id, { type: v as ColumnType })}
                          options={COLUMN_TYPES}
                          disabled={isReadOnly}
                        />
                      </div>
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                        <FieldSelect
                          label={t('contracts.builder.dataContract.piiLevel', 'PII Classification')}
                          value={col.pii}
                          onChange={(v) => updateColumn(col.id, { pii: v as PiiLevel })}
                          options={PII_LEVELS}
                          disabled={isReadOnly}
                        />
                        <FieldCheckbox
                          label={t('contracts.builder.dataContract.nullable', 'Nullable')}
                          checked={col.nullable}
                          onChange={(v) => updateColumn(col.id, { nullable: v })}
                          disabled={isReadOnly}
                        />
                      </div>
                      <FieldArea
                        label={t('contracts.builder.dataContract.columnDescription', 'Description')}
                        value={col.description}
                        onChange={(v) => updateColumn(col.id, { description: v })}
                        rows={2}
                        disabled={isReadOnly}
                      />
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </CardBody>
      </Card>

      {/* ── Action bar ── */}
      {!isReadOnly && (
        <div className="flex items-center justify-end gap-2">
          <button
            type="button"
            onClick={handleValidate}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-[10px] font-medium rounded-md bg-elevated border border-edge text-body hover:bg-elevated/80 transition-colors"
          >
            {t('contracts.builder.dataContract.validate', 'Validate')}
          </button>
          <button
            type="button"
            onClick={handleGenerateSource}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-[10px] font-medium rounded-md bg-accent/10 text-accent border border-accent/20 hover:bg-accent/20 transition-colors"
          >
            {t('contracts.builder.dataContract.generateSource', 'Generate Schema JSON')}
          </button>
        </div>
      )}
    </div>
  );
}
