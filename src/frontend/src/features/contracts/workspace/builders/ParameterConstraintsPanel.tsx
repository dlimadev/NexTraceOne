/**
 * Painel de constraints para parâmetros e propriedades de schema REST.
 *
 * Apresenta campos condicionais por tipo (string, number) e flags booleanas,
 * alinhados com a especificação OpenAPI 3.x.
 */
import { useTranslation } from 'react-i18next';
import { FieldCheckbox } from './shared/BuilderFormPrimitives';
import { FORMAT_OPTIONS } from './RestBuilderHelpers';
import type { PropertyConstraints } from './shared/builderTypes';

export function ParameterConstraintsPanel({
  constraints,
  paramType,
  onChange,
  isReadOnly,
}: {
  constraints: PropertyConstraints;
  paramType: string;
  onChange: (c: PropertyConstraints) => void;
  isReadOnly?: boolean;
}) {
  const { t } = useTranslation();
  const isString = paramType === 'string';
  const isNumber = ['integer', 'number'].includes(paramType);

  const update = (patch: Partial<PropertyConstraints>) => onChange({ ...constraints, ...patch });

  return (
    <div className="ml-2 pl-3 border-l-2 border-accent/20 pb-2 space-y-2">
      <div className="grid grid-cols-2 md:grid-cols-4 gap-2">
        {/* String constraints */}
        {isString && (
          <>
            <div>
              <label className="block text-[9px] text-muted mb-0.5">{t('contracts.builder.rest.minLength', 'Min Length')}</label>
              <input type="number" min={0} value={constraints.minLength ?? ''} onChange={(e) => update({ minLength: e.target.value ? Number(e.target.value) : undefined })}
                className="w-full text-[10px] bg-elevated border border-edge rounded px-2 py-1 text-body" disabled={isReadOnly} />
            </div>
            <div>
              <label className="block text-[9px] text-muted mb-0.5">{t('contracts.builder.rest.maxLength', 'Max Length')}</label>
              <input type="number" min={0} value={constraints.maxLength ?? ''} onChange={(e) => update({ maxLength: e.target.value ? Number(e.target.value) : undefined })}
                className="w-full text-[10px] bg-elevated border border-edge rounded px-2 py-1 text-body" disabled={isReadOnly} />
            </div>
            <div>
              <label className="block text-[9px] text-muted mb-0.5">{t('contracts.builder.rest.pattern', 'Pattern (Regex)')}</label>
              <input type="text" value={constraints.pattern ?? ''} onChange={(e) => update({ pattern: e.target.value || undefined })}
                placeholder={t('contracts.builder.rest.placeholder.pattern', '^[a-z]+$')} className="w-full text-[10px] bg-elevated border border-edge rounded px-2 py-1 text-body font-mono" disabled={isReadOnly} />
            </div>
          </>
        )}

        {/* Number constraints */}
        {isNumber && (
          <>
            <div>
              <label className="block text-[9px] text-muted mb-0.5">{t('contracts.builder.rest.minimum', 'Minimum')}</label>
              <input type="number" value={constraints.minimum ?? ''} onChange={(e) => update({ minimum: e.target.value ? Number(e.target.value) : undefined })}
                className="w-full text-[10px] bg-elevated border border-edge rounded px-2 py-1 text-body" disabled={isReadOnly} />
            </div>
            <div>
              <label className="block text-[9px] text-muted mb-0.5">{t('contracts.builder.rest.maximum', 'Maximum')}</label>
              <input type="number" value={constraints.maximum ?? ''} onChange={(e) => update({ maximum: e.target.value ? Number(e.target.value) : undefined })}
                className="w-full text-[10px] bg-elevated border border-edge rounded px-2 py-1 text-body" disabled={isReadOnly} />
            </div>
          </>
        )}

        {/* Format */}
        <div>
          <label className="block text-[9px] text-muted mb-0.5">{t('contracts.builder.rest.format', 'Format')}</label>
          <select value={constraints.format ?? ''} onChange={(e) => update({ format: e.target.value || undefined })}
            className="w-full text-[10px] bg-elevated border border-edge rounded px-2 py-1 text-body" disabled={isReadOnly}>
            {FORMAT_OPTIONS.map((f) => <option key={f} value={f}>{f || '—'}</option>)}
          </select>
        </div>

        {/* Default Value */}
        <div>
          <label className="block text-[9px] text-muted mb-0.5">{t('contracts.builder.rest.defaultValue', 'Default Value')}</label>
          <input type="text" value={constraints.defaultValue ?? ''} onChange={(e) => update({ defaultValue: e.target.value || undefined })}
            className="w-full text-[10px] bg-elevated border border-edge rounded px-2 py-1 text-body" disabled={isReadOnly} />
        </div>
      </div>

      {/* Boolean flags */}
      <div className="flex flex-wrap gap-4">
        <FieldCheckbox label={t('contracts.builder.rest.readOnly', 'Read Only')} checked={constraints.readOnly ?? false} onChange={(v) => update({ readOnly: v || undefined })} disabled={isReadOnly} />
        <FieldCheckbox label={t('contracts.builder.rest.writeOnly', 'Write Only')} checked={constraints.writeOnly ?? false} onChange={(v) => update({ writeOnly: v || undefined })} disabled={isReadOnly} />
        <FieldCheckbox label={t('contracts.builder.rest.nullable', 'Nullable')} checked={constraints.nullable ?? false} onChange={(v) => update({ nullable: v || undefined })} disabled={isReadOnly} />
      </div>

      {/* Enum values */}
      <div>
        <label className="block text-[9px] text-muted mb-0.5">{t('contracts.builder.rest.enumValues', 'Enum Values')}</label>
        <input type="text" value={constraints.enumValues?.join(', ') ?? ''} onChange={(e) => {
          const values = e.target.value ? e.target.value.split(',').map((s) => s.trim()).filter(Boolean) : undefined;
          update({ enumValues: values?.length ? values : undefined });
        }} placeholder={t('contracts.builder.rest.enumPlaceholder', 'Comma-separated values')}
          className="w-full text-[10px] bg-elevated border border-edge rounded px-2 py-1 text-body" disabled={isReadOnly} />
      </div>
    </div>
  );
}
