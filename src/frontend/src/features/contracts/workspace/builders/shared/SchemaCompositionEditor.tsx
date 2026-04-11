/**
 * Editor visual para schema composition: oneOf / anyOf / allOf.
 *
 * Renderiza a lista de sub-schemas de composição, permite adicionar/remover
 * variantes e configurar o discriminador polimórfico.
 */
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus, Trash2, ChevronDown, ChevronRight } from 'lucide-react';
import type { SchemaProperty } from './builderTypes';

// ── Helpers ───────────────────────────────────────────────────────────────────

let _idCounter = 1;
function genId() {
  return `comp-${crypto.randomUUID?.() ?? `fallback-${_idCounter++}`}`;
}

function createVariant(): SchemaProperty {
  return {
    id: genId(),
    name: '',
    type: 'object',
    description: '',
    required: false,
    constraints: {},
    properties: [],
  };
}

// ── Props ─────────────────────────────────────────────────────────────────────

interface SchemaCompositionEditorProps {
  compositionType: 'oneOf' | 'anyOf' | 'allOf';
  schemas: SchemaProperty[];
  discriminator?: { propertyName: string; mapping?: Record<string, string> };
  onChange: (schemas: SchemaProperty[], discriminator?: { propertyName: string; mapping?: Record<string, string> }) => void;
  isReadOnly?: boolean;
}

// ── Component ─────────────────────────────────────────────────────────────────

export function SchemaCompositionEditor({
  compositionType,
  schemas,
  discriminator,
  onChange,
  isReadOnly = false,
}: SchemaCompositionEditorProps) {
  const { t } = useTranslation();
  const [expandedIds, setExpandedIds] = useState<Set<string>>(new Set());
  const [showDiscriminator, setShowDiscriminator] = useState(!!discriminator?.propertyName);

  const toggleExpand = (id: string) => {
    setExpandedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id); else next.add(id);
      return next;
    });
  };

  const addVariant = () => {
    const v = createVariant();
    const next = [...schemas, v];
    onChange(next, discriminator);
    setExpandedIds((prev) => new Set(prev).add(v.id));
  };

  const removeVariant = (id: string) => {
    onChange(schemas.filter((s) => s.id !== id), discriminator);
    setExpandedIds((prev) => { const n = new Set(prev); n.delete(id); return n; });
  };

  const updateVariant = (id: string, patch: Partial<SchemaProperty>) => {
    onChange(schemas.map((s) => s.id === id ? { ...s, ...patch } : s), discriminator);
  };

  const updateDiscriminator = (patch: Partial<{ propertyName: string; mapping?: Record<string, string> }>) => {
    const next = { ...(discriminator ?? { propertyName: '' }), ...patch };
    onChange(schemas, next);
  };

  const compositionLabel: Record<string, string> = {
    oneOf: 'oneOf',
    anyOf: 'anyOf',
    allOf: 'allOf',
  };

  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between">
        <span className="text-[9px] font-semibold text-muted uppercase tracking-wider">
          {t('contracts.builder.rest.compositionType', 'Composition Type')}: <span className="text-accent font-mono">{compositionLabel[compositionType]}</span>
        </span>
        {!isReadOnly && (
          <button
            type="button"
            onClick={addVariant}
            className="inline-flex items-center gap-1 text-[9px] text-accent hover:text-accent/80 transition-colors"
          >
            <Plus size={9} />
            {t('contracts.builder.rest.addVariant', 'Add Variant')}
          </button>
        )}
      </div>

      {/* Variant list */}
      <div className="space-y-1 pl-1 border-l-2 border-accent/20">
        {schemas.length === 0 && (
          <p className="text-[9px] text-muted/50 py-1">
            {t('contracts.builder.rest.noVariants', 'No variants. Add at least one sub-schema.')}
          </p>
        )}
        {schemas.map((schema, idx) => {
          const isExpanded = expandedIds.has(schema.id);
          const label = schema.$ref
            ? schema.$ref
            : schema.name
              ? schema.name
              : `Variant ${idx + 1}`;

          return (
            <div key={schema.id} className="border border-edge rounded bg-surface/40">
              <div className="flex items-center gap-1.5 px-2 py-1.5">
                <button
                  type="button"
                  onClick={() => toggleExpand(schema.id)}
                  className="text-muted hover:text-body transition-colors flex-shrink-0"
                >
                  {isExpanded ? <ChevronDown size={10} /> : <ChevronRight size={10} />}
                </button>
                <span className="flex-1 text-[10px] font-mono text-body truncate">{label}</span>
                <span className="text-[8px] text-muted bg-elevated px-1 py-0.5 rounded">{schema.type}</span>
                {!isReadOnly && (
                  <button
                    type="button"
                    onClick={() => removeVariant(schema.id)}
                    className="text-muted/40 hover:text-danger transition-colors"
                    title={t('contracts.builder.rest.removeVariant', 'Remove Variant')}
                  >
                    <Trash2 size={10} />
                  </button>
                )}
              </div>

              {isExpanded && (
                <div className="px-2 pb-2 space-y-2 border-t border-edge">
                  {/* Type selector: $ref or object */}
                  <div className="flex items-center gap-2 pt-1.5">
                    <label className="text-[9px] text-muted">{t('contracts.builder.rest.variantType', 'Type')}:</label>
                    <select
                      value={schema.type}
                      onChange={(e) => {
                        const newType = e.target.value as SchemaProperty['type'];
                        updateVariant(schema.id, {
                          type: newType,
                          $ref: newType === '$ref' ? (schema.$ref ?? '') : undefined,
                          properties: newType === 'object' ? (schema.properties ?? []) : undefined,
                        });
                      }}
                      disabled={isReadOnly}
                      className="text-[9px] font-mono bg-elevated border border-edge rounded px-1.5 py-0.5 text-body"
                    >
                      <option value="object">object</option>
                      <option value="$ref">$ref</option>
                      <option value="string">string</option>
                      <option value="integer">integer</option>
                    </select>
                  </div>

                  {/* $ref input */}
                  {schema.type === '$ref' && (
                    <input
                      type="text"
                      value={schema.$ref ?? ''}
                      onChange={(e) => updateVariant(schema.id, { $ref: e.target.value })}
                      placeholder="#/components/schemas/MySchema"
                      disabled={isReadOnly}
                      className="w-full text-[10px] font-mono bg-elevated border border-edge rounded px-2 py-1 text-body placeholder:text-muted/30"
                    />
                  )}

                  {/* Description */}
                  <input
                    type="text"
                    value={schema.description}
                    onChange={(e) => updateVariant(schema.id, { description: e.target.value })}
                    placeholder={t('contracts.builder.rest.variantDescription', 'Variant description...')}
                    disabled={isReadOnly}
                    className="w-full text-[10px] bg-elevated border border-edge rounded px-2 py-1 text-body placeholder:text-muted/30"
                  />
                </div>
              )}
            </div>
          );
        })}
      </div>

      {/* Discriminator section (only relevant for oneOf/anyOf) */}
      {compositionType !== 'allOf' && (
        <div className="border border-edge/50 rounded p-2 space-y-2">
          <button
            type="button"
            onClick={() => setShowDiscriminator((p) => !p)}
            className="flex items-center gap-1 text-[9px] font-semibold text-muted uppercase tracking-wider hover:text-body transition-colors"
          >
            {showDiscriminator ? <ChevronDown size={9} /> : <ChevronRight size={9} />}
            {t('contracts.builder.rest.discriminator', 'Discriminator')}
          </button>

          {showDiscriminator && (
            <div className="space-y-1.5 pl-1">
              <div>
                <label className="block text-[9px] text-muted mb-0.5">
                  {t('contracts.builder.rest.discriminatorProperty', 'Property Name')}
                </label>
                <input
                  type="text"
                  value={discriminator?.propertyName ?? ''}
                  onChange={(e) => updateDiscriminator({ propertyName: e.target.value })}
                  placeholder={t('contracts.schema.placeholder.compositionType', 'type')}
                  disabled={isReadOnly}
                  className="w-full text-[10px] font-mono bg-elevated border border-edge rounded px-2 py-1 text-body placeholder:text-muted/30"
                />
              </div>
              <p className="text-[8px] text-muted/50">
                {t('contracts.builder.rest.discriminatorHint', 'The property used to distinguish between variants')}
              </p>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
