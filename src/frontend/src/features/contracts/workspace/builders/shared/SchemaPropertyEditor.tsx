/**
 * Editor visual de propriedades de schema para REST API request/response.
 *
 * Permite definir propriedades com:
 * - Tipos: string, integer, number, boolean, array, object, $ref
 * - Constraints: minLength, maxLength, minimum, maximum, pattern, format, etc.
 * - Objectos aninhados com propriedades filhas
 * - Arrays com definição do tipo dos itens
 * - Referências a entidades canónicas ($ref)
 * - Drag-friendly reorder (via botões up/down)
 */
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Plus, Trash2, ChevronDown, ChevronRight, ArrowUp, ArrowDown,
  Braces, Hash, Type, ToggleLeft, List, Link2, FileJson,
} from 'lucide-react';
import type { SchemaProperty, PropertyConstraints } from './builderTypes';

// ── Constants ──────────────────────────────────────────────────────────────────

const PROPERTY_TYPES = ['string', 'integer', 'number', 'boolean', 'array', 'object', '$ref'] as const;

const FORMAT_OPTIONS = [
  '', 'date', 'date-time', 'email', 'uri', 'uuid', 'hostname',
  'ipv4', 'ipv6', 'byte', 'binary', 'password', 'int32', 'int64', 'float', 'double',
] as const;

const TYPE_ICONS: Record<string, typeof Type> = {
  string: Type,
  integer: Hash,
  number: Hash,
  boolean: ToggleLeft,
  array: List,
  object: Braces,
  '$ref': Link2,
};

const TYPE_COLORS: Record<string, string> = {
  string: 'text-mint',
  integer: 'text-cyan',
  number: 'text-cyan',
  boolean: 'text-warning',
  array: 'text-accent',
  object: 'text-purple-400',
  '$ref': 'text-pink-400',
};

// ── Helpers ────────────────────────────────────────────────────────────────────

let _nextId = 1;
function genPropId() {
  return `prop-${crypto.randomUUID?.() ?? `fallback-${_nextId++}`}`;
}

function createProperty(type: SchemaProperty['type'] = 'string'): SchemaProperty {
  return {
    id: genPropId(),
    name: '',
    type,
    description: '',
    required: false,
    constraints: {},
    properties: type === 'object' ? [] : undefined,
    items: type === 'array' ? { id: genPropId(), name: 'items', type: 'string', description: '', required: false, constraints: {} } : undefined,
  };
}

// ── Props ──────────────────────────────────────────────────────────────────────

interface SchemaPropertyEditorProps {
  properties: SchemaProperty[];
  onChange: (properties: SchemaProperty[]) => void;
  isReadOnly?: boolean;
  /** Depth level for nested objects (default 0). */
  depth?: number;
  /** Maximum nesting depth (default 4). */
  maxDepth?: number;
  /** Label override for add button. */
  addLabel?: string;
}

// ── Main Component ─────────────────────────────────────────────────────────────

export function SchemaPropertyEditor({
  properties,
  onChange,
  isReadOnly = false,
  depth = 0,
  maxDepth = 4,
  addLabel,
}: SchemaPropertyEditorProps) {
  const { t } = useTranslation();
  const [expandedIds, setExpandedIds] = useState<Set<string>>(new Set());

  const toggleExpand = (id: string) => {
    setExpandedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const updateProperty = (id: string, patch: Partial<SchemaProperty>) => {
    onChange(properties.map((p) => (p.id === id ? { ...p, ...patch } : p)));
  };

  const removeProperty = (id: string) => {
    onChange(properties.filter((p) => p.id !== id));
    setExpandedIds((prev) => {
      const next = new Set(prev);
      next.delete(id);
      return next;
    });
  };

  const moveProperty = (index: number, direction: -1 | 1) => {
    const newIndex = index + direction;
    if (newIndex < 0 || newIndex >= properties.length) return;
    const next = [...properties];
    [next[index], next[newIndex]] = [next[newIndex], next[index]];
    onChange(next);
  };

  const addProperty = (type: SchemaProperty['type'] = 'string') => {
    const prop = createProperty(type);
    onChange([...properties, prop]);
    setExpandedIds((prev) => new Set(prev).add(prop.id));
  };

  const handleTypeChange = (id: string, newType: SchemaProperty['type']) => {
    const prop = properties.find((p) => p.id === id);
    if (!prop) return;
    const patch: Partial<SchemaProperty> = { type: newType };
    if (newType === 'object' && !prop.properties) patch.properties = [];
    else if (newType !== 'object') patch.properties = undefined;
    if (newType === 'array' && !prop.items) {
      patch.items = { id: genPropId(), name: 'items', type: 'string', description: '', required: false, constraints: {} };
    } else if (newType !== 'array') patch.items = undefined;
    if (newType === '$ref') patch.$ref = patch.$ref ?? '';
    else patch.$ref = undefined;
    // Reset constraints when type changes fundamentally
    if ((newType === 'object' || newType === 'array' || newType === '$ref') && prop.type !== newType) {
      patch.constraints = {};
    }
    updateProperty(id, patch);
  };

  const indentPx = depth * 16;

  return (
    <div className="space-y-1" style={{ marginLeft: indentPx > 0 ? `${indentPx}px` : undefined }}>
      {properties.map((prop, idx) => {
        const isExpanded = expandedIds.has(prop.id);
        const hasDetails = prop.type === 'object' || prop.type === 'array' || prop.type === '$ref';
        const IconComponent = TYPE_ICONS[prop.type] ?? FileJson;
        const typeColor = TYPE_COLORS[prop.type] ?? 'text-muted';

        return (
          <div key={prop.id} className="border border-edge rounded-md bg-surface/50">
            {/* ── Property header row ── */}
            <div className="flex items-center gap-1.5 px-2 py-1.5">
              {/* Expand/collapse for complex types */}
              {hasDetails ? (
                <button type="button" onClick={() => toggleExpand(prop.id)}
                  className="text-muted hover:text-body transition-colors flex-shrink-0">
                  {isExpanded ? <ChevronDown size={10} /> : <ChevronRight size={10} />}
                </button>
              ) : (
                <span className="w-[10px] flex-shrink-0" />
              )}

              {/* Type icon */}
              <IconComponent size={11} className={`flex-shrink-0 ${typeColor}`} />

              {/* Property name */}
              <input
                type="text"
                value={prop.name}
                onChange={(e) => updateProperty(prop.id, { name: e.target.value })}
                placeholder={t('contracts.builder.rest.propNamePlaceholder', 'propertyName')}
                disabled={isReadOnly}
                className="flex-1 min-w-0 text-[10px] font-mono bg-transparent border-none outline-none text-body placeholder:text-muted/30"
              />

              {/* Type selector */}
              <select
                value={prop.type}
                onChange={(e) => handleTypeChange(prop.id, e.target.value as SchemaProperty['type'])}
                disabled={isReadOnly}
                className={`text-[9px] font-mono bg-elevated border border-edge rounded px-1 py-0.5 ${typeColor}`}
              >
                {PROPERTY_TYPES.map((t) => (
                  <option key={t} value={t}>{t}</option>
                ))}
              </select>

              {/* Required toggle */}
              <button
                type="button"
                onClick={() => updateProperty(prop.id, { required: !prop.required })}
                disabled={isReadOnly}
                className={`text-[8px] font-bold px-1 py-0.5 rounded transition-colors ${
                  prop.required
                    ? 'bg-danger/15 text-danger border border-danger/25'
                    : 'bg-muted/10 text-muted/40 border border-edge hover:text-muted'
                }`}
                title={t('contracts.builder.rest.required', 'Required')}
              >
                {prop.required ? 'REQ' : 'OPT'}
              </button>

              {/* Move up/down */}
              {!isReadOnly && (
                <>
                  <button type="button" onClick={() => moveProperty(idx, -1)}
                    disabled={idx === 0}
                    className="text-muted/40 hover:text-muted transition-colors disabled:opacity-30">
                    <ArrowUp size={9} />
                  </button>
                  <button type="button" onClick={() => moveProperty(idx, 1)}
                    disabled={idx === properties.length - 1}
                    className="text-muted/40 hover:text-muted transition-colors disabled:opacity-30">
                    <ArrowDown size={9} />
                  </button>
                </>
              )}

              {/* Delete */}
              {!isReadOnly && (
                <button type="button" onClick={() => removeProperty(prop.id)}
                  className="text-muted/40 hover:text-danger transition-colors flex-shrink-0">
                  <Trash2 size={10} />
                </button>
              )}
            </div>

            {/* ── Expanded details ── */}
            {isExpanded && (
              <div className="px-2 pb-2 space-y-2 border-t border-edge">
                {/* Description */}
                <div className="pt-1.5">
                  <input
                    type="text"
                    value={prop.description}
                    onChange={(e) => updateProperty(prop.id, { description: e.target.value })}
                    placeholder={t('contracts.builder.rest.propDescPlaceholder', 'Description of this property...')}
                    disabled={isReadOnly}
                    className="w-full text-[10px] bg-elevated border border-edge rounded px-2 py-1 text-body placeholder:text-muted/30"
                  />
                </div>

                {/* $ref input */}
                {prop.type === '$ref' && (
                  <div>
                    <label className="block text-[9px] text-muted mb-0.5">
                      {t('contracts.builder.rest.refTarget', '$ref Target (Canonical Entity)')}
                    </label>
                    <input
                      type="text"
                      value={prop.$ref ?? ''}
                      onChange={(e) => updateProperty(prop.id, { $ref: e.target.value })}
                      placeholder="#/components/schemas/Address"
                      disabled={isReadOnly}
                      className="w-full text-[10px] font-mono bg-elevated border border-edge rounded px-2 py-1 text-body placeholder:text-muted/30"
                    />
                    <p className="text-[8px] text-muted/50 mt-0.5">
                      {t('contracts.builder.rest.refHint', 'Reference a shared/canonical schema defined in the Canonical Entity Catalog')}
                    </p>
                  </div>
                )}

                {/* Primitive type constraints */}
                {!['object', 'array', '$ref'].includes(prop.type) && (
                  <PropertyConstraintsEditor
                    constraints={prop.constraints}
                    propertyType={prop.type}
                    onChange={(c) => updateProperty(prop.id, { constraints: c })}
                    isReadOnly={isReadOnly}
                  />
                )}

                {/* Array items definition */}
                {prop.type === 'array' && prop.items && (
                  <div className="space-y-1">
                    <label className="block text-[9px] font-semibold text-muted uppercase tracking-wider">
                      {t('contracts.builder.rest.arrayItems', 'Array Item Type')}
                    </label>
                    <div className="ml-2 pl-2 border-l-2 border-accent/20">
                      <div className="flex items-center gap-2 mb-1">
                        <select
                          value={prop.items.type}
                          onChange={(e) => {
                            const newType = e.target.value as SchemaProperty['type'];
                            const newItems: SchemaProperty = {
                              ...prop.items!,
                              type: newType,
                              properties: newType === 'object' ? (prop.items!.properties ?? []) : undefined,
                              $ref: newType === '$ref' ? (prop.items!.$ref ?? '') : undefined,
                            };
                            updateProperty(prop.id, { items: newItems });
                          }}
                          disabled={isReadOnly}
                          className="text-[9px] font-mono bg-elevated border border-edge rounded px-1.5 py-0.5 text-body"
                        >
                          {PROPERTY_TYPES.map((t) => (
                            <option key={t} value={t}>{t}</option>
                          ))}
                        </select>
                        <span className="text-[9px] text-muted">
                          {t('contracts.builder.rest.arrayItemsDesc', 'Define the type of each item in the array')}
                        </span>
                      </div>

                      {/* Nested array item: $ref */}
                      {prop.items.type === '$ref' && (
                        <input
                          type="text"
                          value={prop.items.$ref ?? ''}
                          onChange={(e) => updateProperty(prop.id, { items: { ...prop.items!, $ref: e.target.value } })}
                          placeholder="#/components/schemas/OrderItem"
                          disabled={isReadOnly}
                          className="w-full text-[10px] font-mono bg-elevated border border-edge rounded px-2 py-1 text-body placeholder:text-muted/30"
                        />
                      )}

                      {/* Nested array item: object with properties */}
                      {prop.items.type === 'object' && depth < maxDepth && (
                        <SchemaPropertyEditor
                          properties={prop.items.properties ?? []}
                          onChange={(childProps) => updateProperty(prop.id, { items: { ...prop.items!, properties: childProps } })}
                          isReadOnly={isReadOnly}
                          depth={depth + 1}
                          maxDepth={maxDepth}
                        />
                      )}

                      {/* Primitive array item constraints */}
                      {!['object', 'array', '$ref'].includes(prop.items.type) && (
                        <PropertyConstraintsEditor
                          constraints={prop.items.constraints}
                          propertyType={prop.items.type}
                          onChange={(c) => updateProperty(prop.id, { items: { ...prop.items!, constraints: c } })}
                          isReadOnly={isReadOnly}
                        />
                      )}
                    </div>
                  </div>
                )}

                {/* Nested object properties */}
                {prop.type === 'object' && depth < maxDepth && (
                  <div className="space-y-1">
                    <label className="block text-[9px] font-semibold text-muted uppercase tracking-wider">
                      {t('contracts.builder.rest.nestedProperties', 'Nested Properties')}
                    </label>
                    <SchemaPropertyEditor
                      properties={prop.properties ?? []}
                      onChange={(childProps) => updateProperty(prop.id, { properties: childProps })}
                      isReadOnly={isReadOnly}
                      depth={depth + 1}
                      maxDepth={maxDepth}
                    />
                  </div>
                )}

                {prop.type === 'object' && depth >= maxDepth && (
                  <p className="text-[9px] text-warning">
                    {t('contracts.builder.rest.maxDepthReached', 'Maximum nesting depth reached. Use $ref for deeper structures.')}
                  </p>
                )}
              </div>
            )}
          </div>
        );
      })}

      {/* ── Add property buttons ── */}
      {!isReadOnly && (
        <div className="flex items-center gap-2 pt-1">
          <button type="button" onClick={() => addProperty('string')}
            className="inline-flex items-center gap-1 text-[9px] text-accent hover:text-accent/80 transition-colors">
            <Plus size={9} /> {addLabel ?? t('contracts.builder.rest.addProperty', 'Add Property')}
          </button>
          <span className="text-muted/20">|</span>
          <button type="button" onClick={() => addProperty('object')}
            className="inline-flex items-center gap-1 text-[9px] text-purple-400 hover:text-purple-300 transition-colors">
            <Braces size={9} /> {t('contracts.builder.rest.addObject', 'Object')}
          </button>
          <button type="button" onClick={() => addProperty('array')}
            className="inline-flex items-center gap-1 text-[9px] text-accent hover:text-accent/80 transition-colors">
            <List size={9} /> {t('contracts.builder.rest.addArray', 'Array')}
          </button>
          <button type="button" onClick={() => addProperty('$ref')}
            className="inline-flex items-center gap-1 text-[9px] text-pink-400 hover:text-pink-300 transition-colors">
            <Link2 size={9} /> {t('contracts.builder.rest.addRef', '$ref')}
          </button>
        </div>
      )}
    </div>
  );
}

// ── Property Constraints Editor ───────────────────────────────────────────────

function PropertyConstraintsEditor({
  constraints,
  propertyType,
  onChange,
  isReadOnly,
}: {
  constraints: PropertyConstraints;
  propertyType: string;
  onChange: (c: PropertyConstraints) => void;
  isReadOnly?: boolean;
}) {
  const { t } = useTranslation();
  const isStr = propertyType === 'string';
  const isNum = ['integer', 'number'].includes(propertyType);

  const update = (patch: Partial<PropertyConstraints>) => onChange({ ...constraints, ...patch });

  return (
    <div className="space-y-1.5">
      <label className="block text-[9px] font-semibold text-muted uppercase tracking-wider">
        {t('contracts.builder.rest.constraints', 'Constraints')}
      </label>
      <div className="grid grid-cols-2 md:grid-cols-4 gap-1.5">
        {/* String constraints */}
        {isStr && (
          <>
            <MiniField label={t('contracts.builder.rest.minLength', 'Min Length')} type="number"
              value={constraints.minLength ?? ''} onChange={(v) => update({ minLength: v !== '' ? Number(v) : undefined })}
              disabled={isReadOnly} />
            <MiniField label={t('contracts.builder.rest.maxLength', 'Max Length')} type="number"
              value={constraints.maxLength ?? ''} onChange={(v) => update({ maxLength: v !== '' ? Number(v) : undefined })}
              disabled={isReadOnly} />
            <MiniField label={t('contracts.builder.rest.pattern', 'Pattern')} type="text" mono
              value={constraints.pattern ?? ''} onChange={(v) => update({ pattern: v || undefined })}
              placeholder="^[a-z]+$" disabled={isReadOnly} />
          </>
        )}

        {/* Number constraints */}
        {isNum && (
          <>
            <MiniField label={t('contracts.builder.rest.minimum', 'Minimum')} type="number"
              value={constraints.minimum ?? ''} onChange={(v) => update({ minimum: v !== '' ? Number(v) : undefined })}
              disabled={isReadOnly} />
            <MiniField label={t('contracts.builder.rest.maximum', 'Maximum')} type="number"
              value={constraints.maximum ?? ''} onChange={(v) => update({ maximum: v !== '' ? Number(v) : undefined })}
              disabled={isReadOnly} />
            <div className="flex items-center gap-2 col-span-2">
              <label className="flex items-center gap-1 text-[9px] text-muted">
                <input type="checkbox" checked={constraints.exclusiveMinimum ?? false}
                  onChange={(e) => update({ exclusiveMinimum: e.target.checked || undefined })}
                  disabled={isReadOnly} className="rounded border-edge accent-accent" />
                {t('contracts.builder.rest.exclusiveMin', 'Exclusive Min')}
              </label>
              <label className="flex items-center gap-1 text-[9px] text-muted">
                <input type="checkbox" checked={constraints.exclusiveMaximum ?? false}
                  onChange={(e) => update({ exclusiveMaximum: e.target.checked || undefined })}
                  disabled={isReadOnly} className="rounded border-edge accent-accent" />
                {t('contracts.builder.rest.exclusiveMax', 'Exclusive Max')}
              </label>
            </div>
          </>
        )}

        {/* Format */}
        <div>
          <label className="block text-[8px] text-muted mb-0.5">{t('contracts.builder.rest.format', 'Format')}</label>
          <select value={constraints.format ?? ''} onChange={(e) => update({ format: e.target.value || undefined })}
            disabled={isReadOnly}
            className="w-full text-[9px] bg-elevated border border-edge rounded px-1.5 py-0.5 text-body">
            {FORMAT_OPTIONS.map((f) => <option key={f} value={f}>{f || '—'}</option>)}
          </select>
        </div>

        {/* Default Value */}
        <MiniField label={t('contracts.builder.rest.defaultValue', 'Default')} type="text"
          value={constraints.defaultValue ?? ''} onChange={(v) => update({ defaultValue: v || undefined })}
          disabled={isReadOnly} />

        {/* Example */}
        <MiniField label={t('contracts.builder.rest.example', 'Example')} type="text"
          value={constraints.example ?? ''} onChange={(v) => update({ example: v || undefined })}
          disabled={isReadOnly} />
      </div>

      {/* Boolean flags */}
      <div className="flex flex-wrap gap-3">
        <label className="flex items-center gap-1 text-[9px] text-muted">
          <input type="checkbox" checked={constraints.nullable ?? false}
            onChange={(e) => update({ nullable: e.target.checked || undefined })}
            disabled={isReadOnly} className="rounded border-edge accent-accent" />
          {t('contracts.builder.rest.nullable', 'Nullable')}
        </label>
        <label className="flex items-center gap-1 text-[9px] text-muted">
          <input type="checkbox" checked={constraints.readOnly ?? false}
            onChange={(e) => update({ readOnly: e.target.checked || undefined })}
            disabled={isReadOnly} className="rounded border-edge accent-accent" />
          {t('contracts.builder.rest.readOnly', 'Read Only')}
        </label>
        <label className="flex items-center gap-1 text-[9px] text-muted">
          <input type="checkbox" checked={constraints.writeOnly ?? false}
            onChange={(e) => update({ writeOnly: e.target.checked || undefined })}
            disabled={isReadOnly} className="rounded border-edge accent-accent" />
          {t('contracts.builder.rest.writeOnly', 'Write Only')}
        </label>
      </div>

      {/* Enum values */}
      <MiniField label={t('contracts.builder.rest.enumValues', 'Enum Values (comma-separated)')} type="text"
        value={constraints.enumValues?.join(', ') ?? ''}
        onChange={(v) => {
          const values = v ? v.split(',').map((s) => s.trim()).filter(Boolean) : undefined;
          update({ enumValues: values?.length ? values : undefined });
        }}
        placeholder={t('contracts.builder.rest.enumPlaceholder', 'active, inactive, pending')}
        disabled={isReadOnly} />
    </div>
  );
}

// ── Mini field for compact constraint rows ────────────────────────────────────

function MiniField({
  label, type, value, onChange, placeholder, disabled, mono,
}: {
  label: string; type: string; value: string | number; onChange: (v: string) => void;
  placeholder?: string; disabled?: boolean; mono?: boolean;
}) {
  return (
    <div>
      <label className="block text-[8px] text-muted mb-0.5">{label}</label>
      <input
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        disabled={disabled}
        className={`w-full text-[9px] bg-elevated border border-edge rounded px-1.5 py-0.5 text-body placeholder:text-muted/30 ${
          mono ? 'font-mono' : ''
        } ${disabled ? 'opacity-50 cursor-not-allowed' : ''}`}
      />
    </div>
  );
}
