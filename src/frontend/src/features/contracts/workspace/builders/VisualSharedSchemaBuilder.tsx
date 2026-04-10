/**
 * Builder visual para Shared Schemas (JSON Schema, Avro, Protobuf).
 *
 * Permite criar/editar visualmente:
 * - nome, versão, namespace, formato, compatibilidade, owner, tags
 * - propriedades do schema com tipos, constraints e hierarquia
 * - exemplo de payload
 * - sincronização com JSON Schema via builderSync
 */
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus, Trash2, ChevronDown, ChevronRight, AlertCircle } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import {
  Field, FieldArea, FieldSelect, FieldCheckbox, FieldTagInput,
} from './shared/BuilderFormPrimitives';
import { validateSharedSchemaBuilder } from './shared/builderValidation';
import { sharedSchemaBuilderToJson } from './shared/builderSync';
import type {
  SharedSchemaBuilderState,
  SharedSchemaProperty,
  BuilderValidationResult,
  SyncResult,
} from './shared/builderTypes';

const FORMAT_OPTIONS = ['json-schema', 'avro', 'protobuf'] as const;
const COMPAT_OPTIONS = ['BACKWARD', 'FORWARD', 'FULL', 'NONE'] as const;
const PROPERTY_TYPE_OPTIONS = ['string', 'integer', 'number', 'boolean', 'array', 'object', '$ref'] as const;

let nextId = 1;
function genId() { return `ssp-${nextId++}`; }

function createProperty(): SharedSchemaProperty {
  return { id: genId(), name: '', type: 'string', description: '', required: false, constraints: {} };
}

interface VisualSharedSchemaBuilderProps {
  initialState?: SharedSchemaBuilderState;
  onChange?: (state: SharedSchemaBuilderState) => void;
  onSync?: (result: SyncResult) => void;
  isReadOnly?: boolean;
  className?: string;
}

/**
 * Builder visual para Shared Schemas — permite definir propriedades,
 * constraints, compatibilidade e exemplos sem editar JSON Schema manualmente.
 */
export function VisualSharedSchemaBuilder({
  initialState,
  onChange,
  onSync,
  isReadOnly = false,
  className = '',
}: VisualSharedSchemaBuilderProps) {
  const { t } = useTranslation();

  const [state, setState] = useState<SharedSchemaBuilderState>(
    initialState ?? {
      name: '',
      version: '1.0.0',
      description: '',
      namespace: '',
      format: 'json-schema',
      compatibility: 'BACKWARD',
      owner: '',
      tags: [],
      properties: [],
      example: '',
    },
  );
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [validation, setValidation] = useState<BuilderValidationResult | null>(null);

  const update = useCallback(
    (partial: Partial<SharedSchemaBuilderState>) => {
      const next = { ...state, ...partial };
      setState(next);
      onChange?.(next);
    },
    [state, onChange],
  );

  const addProperty = () => {
    const prop = createProperty();
    update({ properties: [...state.properties, prop] });
    setExpandedId(prop.id);
  };

  const updateProp = (id: string, partial: Partial<SharedSchemaProperty>) => {
    update({ properties: state.properties.map((p) => (p.id === id ? { ...p, ...partial } : p)) });
  };

  const removeProp = (id: string) => {
    update({ properties: state.properties.filter((p) => p.id !== id) });
    if (expandedId === id) setExpandedId(null);
  };

  const handleValidate = () => { setValidation(validateSharedSchemaBuilder(state)); };

  const handleGenerateSource = () => {
    const result = sharedSchemaBuilderToJson(state);
    onSync?.(result);
  };

  const fieldError = (field: string) => validation?.errors.find((e) => e.field === field);

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

      {/* ── Schema metadata ── */}
      <Card>
        <CardHeader>
          <h3 className="text-xs font-semibold text-heading">
            {t('contracts.builder.sharedSchema.title', 'Shared Schema Builder')}
          </h3>
        </CardHeader>
        <CardBody className="space-y-3">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <Field
              label={t('contracts.builder.sharedSchema.name', 'Schema Name')}
              value={state.name}
              onChange={(v) => update({ name: v })}
              placeholder={t('contracts.builder.sharedSchema.namePlaceholder', 'UserProfile')}
              required
              error={fieldError('name') ? t(fieldError('name')!.messageKey, fieldError('name')!.fallback) : undefined}
              disabled={isReadOnly}
            />
            <Field
              label={t('contracts.builder.rest.version', 'Version')}
              value={state.version}
              onChange={(v) => update({ version: v })}
              placeholder="1.0.0"
              required
              disabled={isReadOnly}
            />
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <Field
              label={t('contracts.builder.sharedSchema.namespace', 'Namespace')}
              value={state.namespace}
              onChange={(v) => update({ namespace: v })}
              placeholder={t('contracts.builder.sharedSchema.namespacePlaceholder', 'com.example.schemas')}
              mono
              disabled={isReadOnly}
            />
            <Field
              label={t('contracts.builder.rest.contact', 'Owner')}
              value={state.owner}
              onChange={(v) => update({ owner: v })}
              placeholder={t('contracts.builder.sharedSchema.ownerPlaceholder', 'platform-team')}
              disabled={isReadOnly}
            />
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <FieldSelect
              label={t('contracts.builder.sharedSchema.format', 'Schema Format')}
              value={state.format}
              onChange={(v) => update({ format: v as SharedSchemaBuilderState['format'] })}
              options={FORMAT_OPTIONS}
              disabled={isReadOnly}
            />
            <FieldSelect
              label={t('contracts.builder.sharedSchema.compatibility', 'Compatibility Mode')}
              value={state.compatibility}
              onChange={(v) => update({ compatibility: v as SharedSchemaBuilderState['compatibility'] })}
              options={COMPAT_OPTIONS}
              disabled={isReadOnly}
            />
          </div>
          <FieldArea
            label={t('contracts.builder.rest.description', 'Description')}
            value={state.description}
            onChange={(v) => update({ description: v })}
            placeholder={t('contracts.builder.soap.descPlaceholder', 'Describe this schema...')}
            disabled={isReadOnly}
          />
          <FieldTagInput
            label={t('contracts.builder.rest.tags', 'Tags')}
            tags={state.tags}
            onChange={(v) => update({ tags: v })}
            placeholder={t('contracts.builder.rest.tagsPlaceholder', 'Add tag...')}
          />
        </CardBody>
      </Card>

      {/* ── Properties ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <h3 className="text-xs font-semibold text-heading">
              {t('contracts.builder.rest.parameters', 'Properties')} ({state.properties.length})
            </h3>
            {!isReadOnly && (
              <button type="button" onClick={addProperty} className="inline-flex items-center gap-1 px-2 py-1 text-[10px] font-medium rounded-md bg-accent/10 text-accent hover:bg-accent/20 transition-colors">
                <Plus size={10} /> {t('contracts.builder.sharedSchema.addProperty', 'Add Property')}
              </button>
            )}
          </div>
        </CardHeader>
        <CardBody className="p-0">
          {state.properties.length === 0 && (
            <div className="py-8 text-center text-xs text-muted">
              {t('contracts.builder.sharedSchema.noProperties', 'No properties defined yet. Add your first schema property.')}
            </div>
          )}
          <div className="divide-y divide-edge">
            {state.properties.map((prop) => {
              const isExpanded = expandedId === prop.id;
              return (
                <div key={prop.id} className="group">
                  <button type="button" onClick={() => setExpandedId(isExpanded ? null : prop.id)} className="w-full flex items-center gap-3 px-4 py-3 text-left hover:bg-elevated/30 transition-colors">
                    {isExpanded ? <ChevronDown size={12} className="text-muted" /> : <ChevronRight size={12} className="text-muted" />}
                    <span className="px-2 py-0.5 text-[10px] font-bold rounded bg-accent/15 text-accent border border-accent/25">{prop.type}</span>
                    <span className="text-xs font-mono text-heading flex-1 truncate">{prop.name || t('contracts.builder.soap.unnamed', 'Unnamed Property')}</span>
                    {prop.required && <span className="text-[10px] text-danger">*</span>}
                    {!isReadOnly && (
                      <button type="button" onClick={(e) => { e.stopPropagation(); removeProp(prop.id); }} className="opacity-0 group-hover:opacity-100 text-muted hover:text-danger transition-all">
                        <Trash2 size={12} />
                      </button>
                    )}
                  </button>
                  {isExpanded && (
                    <div className="px-4 pb-4 pt-1 bg-elevated/10 space-y-3">
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                        <Field label={t('contracts.builder.rest.paramName', 'Name')} value={prop.name}
                          onChange={(v) => updateProp(prop.id, { name: v })} placeholder={t('contracts.builder.sharedSchema.propertyNamePlaceholder', 'userId')} required disabled={isReadOnly} />
                        <FieldSelect label={t('contracts.builder.rest.paramType', 'Type')} value={prop.type}
                          onChange={(v) => updateProp(prop.id, { type: v as SharedSchemaProperty['type'] })} options={PROPERTY_TYPE_OPTIONS} disabled={isReadOnly} />
                      </div>
                      <FieldArea label={t('contracts.builder.rest.description', 'Description')} value={prop.description}
                        onChange={(v) => updateProp(prop.id, { description: v })} rows={2} disabled={isReadOnly} />
                      <FieldCheckbox label={t('contracts.builder.rest.required', 'Required')} checked={prop.required}
                        onChange={(v) => updateProp(prop.id, { required: v })} disabled={isReadOnly} />
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </CardBody>
      </Card>

      {/* ── Example payload ── */}
      <Card>
        <CardHeader>
          <h3 className="text-xs font-semibold text-heading">
            {t('contracts.builder.sharedSchema.example', 'Example Payload')}
          </h3>
        </CardHeader>
        <CardBody>
          <FieldArea
            label=""
            value={state.example}
            onChange={(v) => update({ example: v })}
            placeholder={t('contracts.builder.sharedSchema.examplePlaceholder', 'Paste an example JSON/Avro/Protobuf payload...')}
            rows={6}
            mono
            disabled={isReadOnly}
          />
        </CardBody>
      </Card>

      {/* ── Action bar ── */}
      {!isReadOnly && (
        <div className="flex items-center justify-end gap-2">
          <button type="button" onClick={handleValidate}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-[10px] font-medium rounded-md bg-elevated border border-edge text-body hover:bg-elevated/80 transition-colors">
            {t('contracts.builder.soap.validate', 'Validate')}
          </button>
          <button type="button" onClick={handleGenerateSource}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-[10px] font-medium rounded-md bg-accent/10 text-accent border border-accent/20 hover:bg-accent/20 transition-colors">
            {t('contracts.builder.workservice.generateSource', 'Generate Definition')}
          </button>
        </div>
      )}

      {/* ── Sync warning ── */}
      <div className="px-3 py-2 text-[10px] text-muted bg-warning/5 border border-warning/15 rounded-md">
        {t('contracts.builder.soap.roundtripWarning', 'Round-trip is partial — some features may not be editable visually. Use source editor for advanced constructs.')}
      </div>
    </div>
  );
}
