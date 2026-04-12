/**
 * Builder visual completo para serviços SOAP.
 *
 * Permite criar/editar visualmente:
 * - service name, operations, input/output/fault messages
 * - bindings, namespaces, endpoints, security policies
 * - sincronização com WSDL via builderSync
 */
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus, Trash2, ChevronDown, ChevronRight, AlertCircle } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import {
  Field, FieldArea, FieldSelect, FieldTagInput,
} from './shared/BuilderFormPrimitives';
import { validateSoapBuilder } from './shared/builderValidation';
import { soapBuilderToXml } from './shared/builderSync';
import type {
  SoapBuilderState,
  SoapOperation,
  BuilderValidationResult,
  SyncResult,
} from './shared/builderTypes';

const BINDING_OPTIONS = ['SOAP 1.1', 'SOAP 1.2'] as const;

let nextId = 1;
function genId() { return `sop-${nextId++}`; }

function createOperation(): SoapOperation {
  return { id: genId(), name: '', soapAction: '', inputMessage: '', outputMessage: '', faultMessage: '', description: '' };
}

interface VisualSoapBuilderProps {
  initialState?: SoapBuilderState;
  onChange?: (state: SoapBuilderState) => void;
  onSync?: (result: SyncResult) => void;
  isReadOnly?: boolean;
  className?: string;
}

/**
 * Builder visual para serviços SOAP — permite definir operations, messages,
 * bindings, namespaces e endpoints sem editar WSDL manualmente.
 */
export function VisualSoapBuilder({
  initialState,
  onChange,
  onSync,
  isReadOnly = false,
  className = '',
}: VisualSoapBuilderProps) {
  const { t } = useTranslation();

  const [state, setState] = useState<SoapBuilderState>(
    initialState ?? {
      serviceName: '',
      targetNamespace: 'http://example.com/service',
      endpoint: '',
      binding: 'SOAP 1.1',
      description: '',
      securityPolicy: '',
      namespaces: [],
      operations: [],
    },
  );
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [validation, setValidation] = useState<BuilderValidationResult | null>(null);

  const update = useCallback(
    (partial: Partial<SoapBuilderState>) => {
      const next = { ...state, ...partial };
      setState(next);
      onChange?.(next);
    },
    [state, onChange],
  );

  const addOperation = () => {
    const op = createOperation();
    update({ operations: [...state.operations, op] });
    setExpandedId(op.id);
  };

  const updateOp = (id: string, partial: Partial<SoapOperation>) => {
    update({ operations: state.operations.map((o) => (o.id === id ? { ...o, ...partial } : o)) });
  };

  const removeOp = (id: string) => {
    update({ operations: state.operations.filter((o) => o.id !== id) });
    if (expandedId === id) setExpandedId(null);
  };

  const handleValidate = () => { setValidation(validateSoapBuilder(state)); };

  const handleGenerateSource = () => {
    const result = soapBuilderToXml(state);
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

      {/* ── Service metadata ── */}
      <Card>
        <CardHeader>
          <h3 className="text-xs font-semibold text-heading">
            {t('contracts.builder.soap.serviceInfo', 'Service Information')}
          </h3>
        </CardHeader>
        <CardBody className="space-y-3">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <Field
              label={t('contracts.builder.soap.serviceName', 'Service Name')}
              value={state.serviceName}
              onChange={(v) => update({ serviceName: v })}
              placeholder={t('contracts.builder.soap.serviceNamePlaceholder', 'UserService')}
              required
              error={fieldError('serviceName') ? t(fieldError('serviceName')!.messageKey, fieldError('serviceName')!.fallback) : undefined}
              disabled={isReadOnly}
            />
            <FieldSelect
              label={t('contracts.builder.soap.binding', 'Binding')}
              value={state.binding}
              onChange={(v) => update({ binding: v as SoapBuilderState['binding'] })}
              options={BINDING_OPTIONS}
              disabled={isReadOnly}
            />
          </div>
          <Field
            label={t('contracts.builder.soap.namespace', 'Target Namespace')}
            value={state.targetNamespace}
            onChange={(v) => update({ targetNamespace: v })}
            placeholder={t('contracts.builder.soap.endpointPlaceholder', 'http://example.com/service')}
            required
            mono
            disabled={isReadOnly}
          />
          <Field
            label={t('contracts.builder.soap.endpoint', 'Endpoint URL')}
            value={state.endpoint}
            onChange={(v) => update({ endpoint: v })}
            placeholder={t('contracts.builder.soap.addressPlaceholder', 'https://api.example.com/service')}
            mono
            disabled={isReadOnly}
          />
          <FieldArea
            label={t('contracts.builder.soap.description', 'Description')}
            value={state.description}
            onChange={(v) => update({ description: v })}
            placeholder={t('contracts.builder.soap.descPlaceholder', 'Describe this SOAP service...')}
            disabled={isReadOnly}
          />
          <FieldTagInput
            label={t('contracts.builder.soap.namespaces', 'Additional Namespaces')}
            tags={state.namespaces}
            onChange={(v) => update({ namespaces: v })}
            placeholder={t('contracts.builder.soap.namespacePlaceholder', 'http://schemas.example.com/types')}
          />
          <FieldArea
            label={t('contracts.builder.soap.securityPolicy', 'Security Policy')}
            value={state.securityPolicy}
            onChange={(v) => update({ securityPolicy: v })}
            placeholder={t('contracts.builder.soap.securityPlaceholder', 'WS-Security, Transport Security, etc...')}
            rows={2}
            disabled={isReadOnly}
          />
        </CardBody>
      </Card>

      {/* ── Operations ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <h3 className="text-xs font-semibold text-heading">
              {t('contracts.builder.soap.operations', 'Operations')} ({state.operations.length})
            </h3>
            {!isReadOnly && (
              <button type="button" onClick={addOperation} className="inline-flex items-center gap-1 px-2 py-1 text-[10px] font-medium rounded-md bg-accent/10 text-accent hover:bg-accent/20 transition-colors">
                <Plus size={10} /> {t('contracts.builder.soap.addOperation', 'Add Operation')}
              </button>
            )}
          </div>
        </CardHeader>
        <CardBody className="p-0">
          {state.operations.length === 0 && (
            <div className="py-8 text-center text-xs text-muted">
              {t('contracts.builder.soap.noOperations', 'No operations yet. Click "Add Operation" to define a SOAP operation.')}
            </div>
          )}
          <div className="divide-y divide-edge">
            {state.operations.map((op) => {
              const isExpanded = expandedId === op.id;
              return (
                <div key={op.id} className="group">
                  <button type="button" onClick={() => setExpandedId(isExpanded ? null : op.id)} className="w-full flex items-center gap-3 px-4 py-3 text-left hover:bg-elevated/30 transition-colors">
                    {isExpanded ? <ChevronDown size={12} className="text-muted" /> : <ChevronRight size={12} className="text-muted" />}
                    <span className="px-2 py-0.5 text-[10px] font-bold rounded bg-accent/15 text-accent border border-accent/25">OP</span>
                    <span className="text-xs font-mono text-heading flex-1 truncate">{op.name || t('contracts.builder.soap.unnamed', 'Unnamed Operation')}</span>
                    {op.soapAction && <span className="text-[10px] text-muted truncate max-w-[160px]">{op.soapAction}</span>}
                    {!isReadOnly && (
                      <button type="button" onClick={(e) => { e.stopPropagation(); removeOp(op.id); }} className="opacity-0 group-hover:opacity-100 text-muted hover:text-danger transition-all">
                        <Trash2 size={12} />
                      </button>
                    )}
                  </button>
                  {isExpanded && (
                    <div className="px-4 pb-4 pt-1 bg-elevated/10 space-y-3">
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                        <Field label={t('contracts.builder.soap.operationName', 'Operation Name')} value={op.name}
                          onChange={(v) => updateOp(op.id, { name: v })} placeholder={t('contracts.builder.soap.operationNamePlaceholder', 'GetUser')} required disabled={isReadOnly} />
                        <Field label={t('contracts.builder.soap.soapAction', 'SOAP Action')} value={op.soapAction}
                          onChange={(v) => updateOp(op.id, { soapAction: v })} placeholder={t('contracts.builder.soap.soapActionPlaceholder', 'urn:GetUser')} mono disabled={isReadOnly} />
                      </div>
                      <Field label={t('contracts.builder.soap.inputMessage', 'Input Message')} value={op.inputMessage}
                        onChange={(v) => updateOp(op.id, { inputMessage: v })} placeholder={t('contracts.builder.soap.inputMessagePlaceholder', 'GetUserRequest')} mono disabled={isReadOnly} />
                      <Field label={t('contracts.builder.soap.outputMessage', 'Output Message')} value={op.outputMessage}
                        onChange={(v) => updateOp(op.id, { outputMessage: v })} placeholder={t('contracts.builder.soap.outputMessagePlaceholder', 'GetUserResponse')} mono disabled={isReadOnly} />
                      <Field label={t('contracts.builder.soap.faultMessage', 'Fault Message')} value={op.faultMessage}
                        onChange={(v) => updateOp(op.id, { faultMessage: v })} placeholder={t('contracts.builder.soap.faultMessagePlaceholder', 'ServiceFault')} mono disabled={isReadOnly} />
                      <FieldArea label={t('contracts.builder.soap.opDescription', 'Description')} value={op.description}
                        onChange={(v) => updateOp(op.id, { description: v })} rows={2} disabled={isReadOnly} />
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
          <button type="button" onClick={handleValidate}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-[10px] font-medium rounded-md bg-elevated border border-edge text-body hover:bg-elevated/80 transition-colors">
            {t('contracts.builder.soap.validate', 'Validate')}
          </button>
          <button type="button" onClick={handleGenerateSource}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-[10px] font-medium rounded-md bg-accent/10 text-accent border border-accent/20 hover:bg-accent/20 transition-colors">
            {t('contracts.builder.soap.generateSource', 'Generate WSDL')}
          </button>
        </div>
      )}

      {/* ── Sync warning ── */}
      <div className="px-3 py-2 text-[10px] text-muted bg-warning/5 border border-warning/15 rounded-md">
        {t('contracts.builder.soap.roundtripWarning', 'SOAP round-trip is partial — some WSDL features may not be editable visually. Use source editor for advanced WSDL constructs.')}
      </div>
    </div>
  );
}
