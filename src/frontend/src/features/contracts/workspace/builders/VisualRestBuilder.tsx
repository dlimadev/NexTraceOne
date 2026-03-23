/**
 * Builder visual completo para REST APIs.
 *
 * Permite criar/editar sem YAML:
 * - base path, resources, endpoints, methods
 * - operationId, summary, description, tags
 * - headers, query params, path params
 * - request body, responses, examples
 * - auth/scopes, rate limits, idempotency
 * - observability metadata, deprecation notes
 */
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Plus, Trash2, ChevronDown, ChevronRight, AlertCircle, Copy,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import {
  Field, FieldArea, FieldSelect, FieldCheckbox, FieldTagInput,
} from './shared/BuilderFormPrimitives';
import { validateRestBuilder } from './shared/builderValidation';
import { restBuilderToYaml } from './shared/builderSync';
import type {
  RestBuilderState,
  RestEndpoint,
  RestParameter,
  RestResponse,
  BuilderValidationResult,
  SyncResult,
} from './shared/builderTypes';

const HTTP_METHODS = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'HEAD', 'OPTIONS'] as const;
const PARAM_LOCATIONS = ['query', 'path', 'header', 'cookie'] as const;
const PARAM_TYPES = ['string', 'integer', 'number', 'boolean', 'array', 'object'] as const;
const STATUS_CODES = ['200', '201', '204', '400', '401', '403', '404', '409', '422', '500', '502', '503'] as const;

const METHOD_COLORS: Record<string, string> = {
  GET: 'bg-mint/15 text-mint border border-mint/25',
  POST: 'bg-cyan/15 text-cyan border border-cyan/25',
  PUT: 'bg-warning/15 text-warning border border-warning/25',
  PATCH: 'bg-accent/15 text-accent border border-accent/25',
  DELETE: 'bg-danger/15 text-danger border border-danger/25',
  HEAD: 'bg-muted/15 text-muted border border-muted/25',
  OPTIONS: 'bg-muted/10 text-muted/60 border border-muted/15',
};

let nextId = 1;
function genId(prefix: string) { return `${prefix}-${nextId++}`; }

function createEndpoint(): RestEndpoint {
  return {
    id: genId('ep'),
    method: 'GET',
    path: '/resource',
    operationId: '',
    summary: '',
    description: '',
    tags: [],
    deprecated: false,
    deprecationNote: '',
    parameters: [],
    requestBody: null,
    responses: [{ id: genId('res'), statusCode: '200', description: 'OK', contentType: 'application/json', schema: '', example: '' }],
    authScopes: [],
    rateLimit: '',
    idempotencyKey: '',
    observabilityNotes: '',
  };
}

function createParameter(): RestParameter {
  return { id: genId('param'), name: '', in: 'query', required: false, type: 'string', description: '' };
}

function createResponse(): RestResponse {
  return { id: genId('res'), statusCode: '200', description: '', contentType: 'application/json', schema: '', example: '' };
}

interface VisualRestBuilderProps {
  initialState?: RestBuilderState;
  onChange?: (state: RestBuilderState) => void;
  onSync?: (result: SyncResult) => void;
  isReadOnly?: boolean;
  className?: string;
}

/**
 * Builder visual para REST APIs — permite criar endpoints, métodos, request/response,
 * parâmetros, auth, rate limits, observability e deprecation sem escrever OpenAPI manualmente.
 */
export function VisualRestBuilder({
  initialState,
  onChange,
  onSync,
  isReadOnly = false,
  className = '',
}: VisualRestBuilderProps) {
  const { t } = useTranslation();

  const [state, setState] = useState<RestBuilderState>(
    initialState ?? {
      basePath: '/api/v1',
      title: '',
      version: '1.0.0',
      description: '',
      contact: '',
      license: '',
      servers: [],
      endpoints: [],
    },
  );
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [expandedSection, setExpandedSection] = useState<Record<string, string>>({});
  const [validation, setValidation] = useState<BuilderValidationResult | null>(null);

  const update = useCallback(
    (partial: Partial<RestBuilderState>) => {
      const next = { ...state, ...partial };
      setState(next);
      onChange?.(next);
    },
    [state, onChange],
  );

  const addEndpoint = () => {
    const ep = createEndpoint();
    update({ endpoints: [...state.endpoints, ep] });
    setExpandedId(ep.id);
  };

  const updateEndpoint = (id: string, partial: Partial<RestEndpoint>) => {
    update({ endpoints: state.endpoints.map((ep) => (ep.id === id ? { ...ep, ...partial } : ep)) });
  };

  const removeEndpoint = (id: string) => {
    update({ endpoints: state.endpoints.filter((ep) => ep.id !== id) });
    if (expandedId === id) setExpandedId(null);
  };

  const duplicateEndpoint = (ep: RestEndpoint) => {
    const dup = { ...ep, id: genId('ep'), operationId: ep.operationId ? `${ep.operationId}Copy` : '' };
    update({ endpoints: [...state.endpoints, dup] });
    setExpandedId(dup.id);
  };

  const handleValidate = () => {
    setValidation(validateRestBuilder(state));
  };

  const handleGenerateSource = () => {
    const result = restBuilderToYaml(state);
    onSync?.(result);
  };

  const toggleSubSection = (epId: string, section: string) => {
    const key = `${epId}.${section}`;
    setExpandedSection((prev) => ({ ...prev, [key]: prev[key] ? '' : section }));
  };

  const isSubExpanded = (epId: string, section: string) => expandedSection[`${epId}.${section}`] === section;

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
              {validation.errors.slice(0, 5).map((e, i) => (
                <li key={i} className="text-[10px]">• {t(e.messageKey, e.fallback)}</li>
              ))}
              {validation.errors.length > 5 && (
                <li className="text-[10px] text-muted">
                  {t('contracts.builder.validation.moreErrors', `...and ${validation.errors.length - 5} more`)}
                </li>
              )}
            </ul>
          </div>
        </div>
      )}

      {/* ── API Metadata ── */}
      <Card>
        <CardHeader>
          <h3 className="text-xs font-semibold text-heading">
            {t('contracts.builder.rest.apiInfo', 'API Information')}
          </h3>
        </CardHeader>
        <CardBody className="space-y-3">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <Field
              label={t('contracts.builder.rest.title', 'Title')}
              value={state.title}
              onChange={(v) => update({ title: v })}
              placeholder={t('contracts.builder.rest.titlePlaceholder', 'User Management API')}
              required
              error={fieldError('title') ? t(fieldError('title')!.messageKey, fieldError('title')!.fallback) : undefined}
              disabled={isReadOnly}
            />
            <Field
              label={t('contracts.builder.rest.version', 'Version')}
              value={state.version}
              onChange={(v) => update({ version: v })}
              placeholder={t('contracts.builder.rest.versionPlaceholder', '1.0.0')}
              mono
              disabled={isReadOnly}
            />
          </div>
          <Field
            label={t('contracts.builder.rest.basePath', 'Base Path')}
            value={state.basePath}
            onChange={(v) => update({ basePath: v })}
            placeholder={t('contracts.builder.rest.basePathPlaceholder', '/api/v1')}
            required
            mono
            error={fieldError('basePath') ? t(fieldError('basePath')!.messageKey, fieldError('basePath')!.fallback) : undefined}
            disabled={isReadOnly}
          />
          <FieldArea
            label={t('contracts.builder.rest.description', 'Description')}
            value={state.description}
            onChange={(v) => update({ description: v })}
            placeholder={t('contracts.builder.rest.descPlaceholder', 'Describe what this API does...')}
            disabled={isReadOnly}
          />
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <Field
              label={t('contracts.builder.rest.contact', 'Contact')}
              value={state.contact}
              onChange={(v) => update({ contact: v })}
              placeholder={t('contracts.builder.rest.contactPlaceholder', 'API Team')}
              disabled={isReadOnly}
            />
            <Field
              label={t('contracts.builder.rest.license', 'License')}
              value={state.license}
              onChange={(v) => update({ license: v })}
              placeholder={t('contracts.builder.rest.licensePlaceholder', 'MIT')}
              disabled={isReadOnly}
            />
          </div>
          <FieldTagInput
            label={t('contracts.builder.rest.servers', 'Servers')}
            tags={state.servers}
            onChange={(v) => update({ servers: v })}
            placeholder={t('contracts.builder.rest.serverPlaceholder', 'https://api.example.com')}
          />
        </CardBody>
      </Card>

      {/* ── Endpoints ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <h3 className="text-xs font-semibold text-heading">
              {t('contracts.builder.rest.endpoints', 'Endpoints')} ({state.endpoints.length})
            </h3>
            {!isReadOnly && (
              <button type="button"
                onClick={addEndpoint}
                className="inline-flex items-center gap-1 px-2 py-1 text-[10px] font-medium rounded-md bg-accent/10 text-accent hover:bg-accent/20 transition-colors"
              >
                <Plus size={10} /> {t('contracts.builder.rest.addEndpoint', 'Add Endpoint')}
              </button>
            )}
          </div>
        </CardHeader>
        <CardBody className="p-0">
          {state.endpoints.length === 0 && (
            <div className="py-8 text-center text-xs text-muted">
              {t('contracts.builder.rest.noEndpoints', 'No endpoints yet. Click "Add Endpoint" to get started.')}
            </div>
          )}

          <div className="divide-y divide-edge">
            {state.endpoints.map((ep) => {
              const isExpanded = expandedId === ep.id;

              return (
                <div key={ep.id} className="group">
                  {/* Collapsed row */}
                  <button type="button"
                    onClick={() => setExpandedId(isExpanded ? null : ep.id)}
                    className="w-full flex items-center gap-3 px-4 py-3 text-left hover:bg-elevated/30 transition-colors"
                  >
                    {isExpanded ? <ChevronDown size={12} className="text-muted" /> : <ChevronRight size={12} className="text-muted" />}
                    <span className={`px-2 py-0.5 text-[10px] font-bold rounded ${METHOD_COLORS[ep.method]}`}>
                      {ep.method}
                    </span>
                    <span className="text-xs font-mono text-heading flex-1 truncate">
                      {state.basePath}{ep.path}
                    </span>
                    {ep.deprecated && (
                      <span className="text-[9px] text-warning bg-warning/10 px-1.5 py-0.5 rounded">
                        {t('contracts.builder.rest.deprecated', 'Deprecated')}
                      </span>
                    )}
                    {ep.summary && <span className="text-[10px] text-muted truncate max-w-[180px]">{ep.summary}</span>}
                    <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-all">
                      {!isReadOnly && (
                        <>
                          <button type="button"
                            onClick={(e) => { e.stopPropagation(); duplicateEndpoint(ep); }}
                            className="text-muted hover:text-accent transition-colors"
                            title={t('contracts.builder.rest.duplicate', 'Duplicate')}
                          >
                            <Copy size={12} />
                          </button>
                          <button type="button"
                            onClick={(e) => { e.stopPropagation(); removeEndpoint(ep.id); }}
                            className="text-muted hover:text-danger transition-colors"
                          >
                            <Trash2 size={12} />
                          </button>
                        </>
                      )}
                    </div>
                  </button>

                  {/* Expanded detail */}
                  {isExpanded && (
                    <div className="px-4 pb-4 pt-1 bg-elevated/10 space-y-4">
                      {/* Basic info */}
                      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
                        <FieldSelect
                          label={t('contracts.builder.rest.method', 'Method')}
                          value={ep.method}
                          onChange={(v) => updateEndpoint(ep.id, { method: v as RestEndpoint['method'] })}
                          options={HTTP_METHODS}
                          disabled={isReadOnly}
                        />
                        <Field
                          label={t('contracts.builder.rest.path', 'Path')}
                          value={ep.path}
                          onChange={(v) => updateEndpoint(ep.id, { path: v })}
                          placeholder={t('contracts.builder.rest.pathPlaceholder', '/users/{id}')}
                          required
                          mono
                          disabled={isReadOnly}
                        />
                        <Field
                          label={t('contracts.builder.rest.operationId', 'Operation ID')}
                          value={ep.operationId}
                          onChange={(v) => updateEndpoint(ep.id, { operationId: v })}
                          placeholder={t('contracts.builder.rest.operationIdPlaceholder', 'getUser')}
                          mono
                          disabled={isReadOnly}
                        />
                      </div>

                      <Field
                        label={t('contracts.builder.rest.summary', 'Summary')}
                        value={ep.summary}
                        onChange={(v) => updateEndpoint(ep.id, { summary: v })}
                        placeholder={t('contracts.builder.rest.summaryPlaceholder', 'Brief description of this endpoint')}
                        disabled={isReadOnly}
                      />

                      <FieldArea
                        label={t('contracts.builder.rest.description', 'Description')}
                        value={ep.description}
                        onChange={(v) => updateEndpoint(ep.id, { description: v })}
                        rows={2}
                        disabled={isReadOnly}
                      />

                      <FieldTagInput
                        label={t('contracts.builder.rest.tags', 'Tags')}
                        tags={ep.tags}
                        onChange={(v) => updateEndpoint(ep.id, { tags: v })}
                        placeholder={t('contracts.builder.rest.tagsPlaceholder', 'Add tag...')}
                      />

                      {/* ── Parameters ── */}
                      <CollapsibleSubSection
                        title={t('contracts.builder.rest.parameters', 'Parameters')}
                        count={ep.parameters.length}
                        isOpen={isSubExpanded(ep.id, 'params')}
                        onToggle={() => toggleSubSection(ep.id, 'params')}
                      >
                        {ep.parameters.map((param, pi) => (
                          <div key={param.id} className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-2 items-end">
                            <Field label={pi === 0 ? t('contracts.builder.rest.paramName', 'Name') : ''} value={param.name} onChange={(v) => {
                              const next = [...ep.parameters]; next[pi] = { ...param, name: v };
                              updateEndpoint(ep.id, { parameters: next });
                            }} placeholder={t('contracts.builder.rest.paramNamePlaceholder', 'id')} mono disabled={isReadOnly} />
                            <FieldSelect label={pi === 0 ? t('contracts.builder.rest.paramIn', 'In') : ''} value={param.in} onChange={(v) => {
                              const next = [...ep.parameters]; next[pi] = { ...param, in: v as RestParameter['in'] };
                              updateEndpoint(ep.id, { parameters: next });
                            }} options={PARAM_LOCATIONS} disabled={isReadOnly} />
                            <FieldSelect label={pi === 0 ? t('contracts.builder.rest.paramType', 'Type') : ''} value={param.type as typeof PARAM_TYPES[number]} onChange={(v) => {
                              const next = [...ep.parameters]; next[pi] = { ...param, type: v };
                              updateEndpoint(ep.id, { parameters: next });
                            }} options={PARAM_TYPES} disabled={isReadOnly} />
                            <FieldCheckbox label={t('contracts.builder.rest.required', 'Required')} checked={param.required} onChange={(v) => {
                              const next = [...ep.parameters]; next[pi] = { ...param, required: v };
                              updateEndpoint(ep.id, { parameters: next });
                            }} disabled={isReadOnly} />
                            {!isReadOnly && (
                              <button type="button" onClick={() => updateEndpoint(ep.id, { parameters: ep.parameters.filter((_, j) => j !== pi) })}
                                className="text-muted hover:text-danger transition-colors pb-1"><Trash2 size={11} /></button>
                            )}
                          </div>
                        ))}
                        {!isReadOnly && (
                          <button type="button" onClick={() => updateEndpoint(ep.id, { parameters: [...ep.parameters, createParameter()] })}
                            className="text-[10px] text-accent hover:text-accent/80 transition-colors">
                            + {t('contracts.builder.rest.addParam', 'Add Parameter')}
                          </button>
                        )}
                      </CollapsibleSubSection>

                      {/* ── Request Body ── */}
                      {['POST', 'PUT', 'PATCH'].includes(ep.method) && (
                        <CollapsibleSubSection
                          title={t('contracts.builder.rest.requestBody', 'Request Body')}
                          count={ep.requestBody ? 1 : 0}
                          isOpen={isSubExpanded(ep.id, 'reqBody')}
                          onToggle={() => toggleSubSection(ep.id, 'reqBody')}
                        >
                          {ep.requestBody ? (
                            <div className="space-y-2">
                              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                                <Field label={t('contracts.builder.rest.contentType', 'Content Type')} value={ep.requestBody.contentType}
                                  onChange={(v) => updateEndpoint(ep.id, { requestBody: { ...ep.requestBody!, contentType: v } })}
                                  placeholder={t('contracts.builder.rest.contentTypePlaceholder', 'application/json')} mono disabled={isReadOnly} />
                                <FieldCheckbox label={t('contracts.builder.rest.required', 'Required')} checked={ep.requestBody.required}
                                  onChange={(v) => updateEndpoint(ep.id, { requestBody: { ...ep.requestBody!, required: v } })} disabled={isReadOnly} />
                              </div>
                              <Field label={t('contracts.builder.rest.schema', 'Schema ($ref)')} value={ep.requestBody.schema}
                                onChange={(v) => updateEndpoint(ep.id, { requestBody: { ...ep.requestBody!, schema: v } })}
                                placeholder={t('contracts.builder.rest.schemaPlaceholder', '#/components/schemas/CreateUserRequest')} mono disabled={isReadOnly} />
                              <FieldArea label={t('contracts.builder.rest.example', 'Example')} value={ep.requestBody.example}
                                onChange={(v) => updateEndpoint(ep.id, { requestBody: { ...ep.requestBody!, example: v } })}
                                rows={3} mono disabled={isReadOnly} />
                              {!isReadOnly && (
                                <button type="button" onClick={() => updateEndpoint(ep.id, { requestBody: null })}
                                  className="text-[10px] text-danger hover:text-danger/80 transition-colors">
                                  {t('contracts.builder.rest.removeBody', 'Remove Request Body')}
                                </button>
                              )}
                            </div>
                          ) : (!isReadOnly && (
                            <button type="button" onClick={() => updateEndpoint(ep.id, { requestBody: { contentType: 'application/json', schema: '', required: true, example: '' } })}
                              className="text-[10px] text-accent hover:text-accent/80 transition-colors">
                              + {t('contracts.builder.rest.addBody', 'Add Request Body')}
                            </button>
                          ))}
                        </CollapsibleSubSection>
                      )}

                      {/* ── Responses ── */}
                      <CollapsibleSubSection
                        title={t('contracts.builder.rest.responses', 'Responses')}
                        count={ep.responses.length}
                        isOpen={isSubExpanded(ep.id, 'responses')}
                        onToggle={() => toggleSubSection(ep.id, 'responses')}
                      >
                        {ep.responses.map((res, ri) => (
                          <div key={res.id} className="space-y-2 pb-2 mb-2 border-b border-edge last:border-0">
                            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-2">
                              <FieldSelect label={t('contracts.builder.rest.statusCode', 'Status Code')} value={res.statusCode as typeof STATUS_CODES[number]}
                                onChange={(v) => { const next = [...ep.responses]; next[ri] = { ...res, statusCode: v }; updateEndpoint(ep.id, { responses: next }); }}
                                options={STATUS_CODES} disabled={isReadOnly} />
                              <Field label={t('contracts.builder.rest.resDescription', 'Description')} value={res.description}
                                onChange={(v) => { const next = [...ep.responses]; next[ri] = { ...res, description: v }; updateEndpoint(ep.id, { responses: next }); }}
                                placeholder={t('contracts.builder.rest.resDescPlaceholder', 'OK')} disabled={isReadOnly} />
                              <Field label={t('contracts.builder.rest.contentType', 'Content Type')} value={res.contentType}
                                onChange={(v) => { const next = [...ep.responses]; next[ri] = { ...res, contentType: v }; updateEndpoint(ep.id, { responses: next }); }}
                                placeholder={t('contracts.builder.rest.contentTypePlaceholder', 'application/json')} mono disabled={isReadOnly} />
                            </div>
                            <Field label={t('contracts.builder.rest.schema', 'Schema ($ref)')} value={res.schema}
                              onChange={(v) => { const next = [...ep.responses]; next[ri] = { ...res, schema: v }; updateEndpoint(ep.id, { responses: next }); }}
                              placeholder={t('contracts.builder.rest.resSchemaPlaceholder', '#/components/schemas/User')} mono disabled={isReadOnly} />
                            <FieldArea label={t('contracts.builder.rest.example', 'Example')} value={res.example}
                              onChange={(v) => { const next = [...ep.responses]; next[ri] = { ...res, example: v }; updateEndpoint(ep.id, { responses: next }); }}
                              rows={2} mono disabled={isReadOnly} />
                            {!isReadOnly && (
                              <button type="button" onClick={() => updateEndpoint(ep.id, { responses: ep.responses.filter((_, j) => j !== ri) })}
                                className="text-[10px] text-danger hover:text-danger/80 transition-colors">
                                {t('contracts.builder.rest.removeResponse', 'Remove')}
                              </button>
                            )}
                          </div>
                        ))}
                        {!isReadOnly && (
                          <button type="button" onClick={() => updateEndpoint(ep.id, { responses: [...ep.responses, createResponse()] })}
                            className="text-[10px] text-accent hover:text-accent/80 transition-colors">
                            + {t('contracts.builder.rest.addResponse', 'Add Response')}
                          </button>
                        )}
                      </CollapsibleSubSection>

                      {/* ── Auth & Security ── */}
                      <CollapsibleSubSection
                        title={t('contracts.builder.rest.authSecurity', 'Auth & Security')}
                        count={ep.authScopes.length}
                        isOpen={isSubExpanded(ep.id, 'auth')}
                        onToggle={() => toggleSubSection(ep.id, 'auth')}
                      >
                        <FieldTagInput label={t('contracts.builder.rest.authScopes', 'OAuth2 Scopes')} tags={ep.authScopes}
                          onChange={(v) => updateEndpoint(ep.id, { authScopes: v })} placeholder={t('contracts.builder.rest.authScopePlaceholder', 'read:users')} />
                      </CollapsibleSubSection>

                      {/* ── Rate Limits & Behavior ── */}
                      <CollapsibleSubSection
                        title={t('contracts.builder.rest.behavior', 'Rate Limits & Behavior')}
                        count={[ep.rateLimit, ep.idempotencyKey].filter(Boolean).length}
                        isOpen={isSubExpanded(ep.id, 'behavior')}
                        onToggle={() => toggleSubSection(ep.id, 'behavior')}
                      >
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                          <Field label={t('contracts.builder.rest.rateLimit', 'Rate Limit')} value={ep.rateLimit}
                            onChange={(v) => updateEndpoint(ep.id, { rateLimit: v })} placeholder={t('contracts.builder.rest.rateLimitPlaceholder', '100/min')} disabled={isReadOnly} />
                          <Field label={t('contracts.builder.rest.idempotencyKey', 'Idempotency Key')} value={ep.idempotencyKey}
                            onChange={(v) => updateEndpoint(ep.id, { idempotencyKey: v })} placeholder={t('contracts.builder.rest.idempotencyPlaceholder', 'X-Idempotency-Key')} mono disabled={isReadOnly} />
                        </div>
                      </CollapsibleSubSection>

                      {/* ── Observability ── */}
                      <CollapsibleSubSection
                        title={t('contracts.builder.rest.observability', 'Observability')}
                        count={ep.observabilityNotes ? 1 : 0}
                        isOpen={isSubExpanded(ep.id, 'obs')}
                        onToggle={() => toggleSubSection(ep.id, 'obs')}
                      >
                        <FieldArea label={t('contracts.builder.rest.observabilityNotes', 'Observability Notes')} value={ep.observabilityNotes}
                          onChange={(v) => updateEndpoint(ep.id, { observabilityNotes: v })}
                          placeholder={t('contracts.builder.rest.obsPlaceholder', 'Metrics, SLOs, tracing expectations...')}
                          rows={2} disabled={isReadOnly} />
                      </CollapsibleSubSection>

                      {/* ── Deprecation ── */}
                      <div className="flex items-center gap-4 pt-1 border-t border-edge">
                        <FieldCheckbox label={t('contracts.builder.rest.deprecated', 'Deprecated')} checked={ep.deprecated}
                          onChange={(v) => updateEndpoint(ep.id, { deprecated: v })} disabled={isReadOnly} />
                        {ep.deprecated && (
                          <div className="flex-1">
                            <Field label={t('contracts.builder.rest.deprecationNote', 'Deprecation Note')} value={ep.deprecationNote}
                              onChange={(v) => updateEndpoint(ep.id, { deprecationNote: v })}
                              placeholder={t('contracts.builder.rest.deprecationPlaceholder', 'Use v2 endpoint instead...')} disabled={isReadOnly} />
                          </div>
                        )}
                      </div>
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
            {t('contracts.builder.rest.validate', 'Validate')}
          </button>
          <button type="button" onClick={handleGenerateSource}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-[10px] font-medium rounded-md bg-accent/10 text-accent border border-accent/20 hover:bg-accent/20 transition-colors">
            {t('contracts.builder.rest.generateSource', 'Generate Source')}
          </button>
        </div>
      )}
    </div>
  );
}

// ── Collapsible sub-section ───────────────────────────────────────────────────

function CollapsibleSubSection({
  title, count, isOpen, onToggle, children,
}: {
  title: string; count: number; isOpen: boolean; onToggle: () => void; children: React.ReactNode;
}) {
  return (
    <div className="border border-edge rounded-md">
      <button type="button" onClick={onToggle} className="w-full flex items-center gap-2 px-3 py-2 text-left hover:bg-elevated/20 transition-colors">
        {isOpen ? <ChevronDown size={10} className="text-muted" /> : <ChevronRight size={10} className="text-muted" />}
        <span className="text-[10px] font-semibold uppercase tracking-wider text-muted/70 flex-1">{title}</span>
        {count > 0 && <span className="text-[9px] text-accent bg-accent/10 px-1.5 py-0.5 rounded">{count}</span>}
      </button>
      {isOpen && <div className="px-3 pb-3 space-y-2">{children}</div>}
    </div>
  );
}
