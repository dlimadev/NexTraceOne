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
  Plus, Trash2, ChevronDown, ChevronRight, AlertCircle, AlertTriangle, Copy, Sparkles, Zap, ClipboardCopy, Check,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import {
  Field, FieldArea, FieldSelect, FieldCheckbox, FieldTagInput,
} from './shared/BuilderFormPrimitives';
import { SchemaPropertyEditor } from './shared/SchemaPropertyEditor';
import { validateRestBuilder } from './shared/builderValidation';
import { restBuilderToYaml } from './shared/builderSync';
import { generateExampleFromSchema, formatExample } from './shared/ExampleGenerator';
import { generateOperationId } from './shared/operationIdUtils';
import type {
  RestBuilderState,
  RestEndpoint,
  RestParameter,
  RestResponse,
  SchemaProperty,
  PropertyConstraints,
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

/** Gera IDs únicos por instância do builder usando crypto.randomUUID(). */
function genId(prefix: string) {
  return `${prefix}-${crypto.randomUUID()}`;
}

/** Cria as propriedades RFC 7807 Problem Details para um conjunto de campos. */
function problemDetailsProps(fields: string[]): SchemaProperty[] {
  const typeMap: Record<string, SchemaProperty['type']> = {
    type: 'string',
    title: 'string',
    status: 'integer',
    detail: 'string',
    instance: 'string',
  };
  return fields.map((name) => ({
    id: genId('pd'),
    name,
    type: typeMap[name] ?? 'string',
    description: '',
    required: false,
    constraints: {},
  }));
}

/** Resposta HTTP com schema RFC 7807. */
function createProblemResponse(statusCode: string, description: string, fields: string[]): RestResponse {
  return {
    id: genId('res'),
    statusCode,
    description,
    contentType: 'application/problem+json',
    schema: '',
    example: '',
    properties: problemDetailsProps(fields),
  };
}

/** Colecção de respostas de erro comuns RFC 7807. */
const COMMON_ERROR_RESPONSES: RestResponse[] = [
  createProblemResponse('400', 'Bad Request', ['type', 'title', 'status', 'detail', 'instance']),
  createProblemResponse('401', 'Unauthorized', ['type', 'title', 'status']),
  createProblemResponse('403', 'Forbidden', ['type', 'title', 'status']),
  createProblemResponse('404', 'Not Found', ['type', 'title', 'status']),
  createProblemResponse('500', 'Internal Server Error', ['type', 'title', 'status', 'detail']),
];

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
    responses: [{ id: genId('res'), statusCode: '200', description: 'OK', contentType: 'application/json', schema: '', example: '', properties: [] }],
    authScopes: [],
    rateLimit: '',
    idempotencyKey: '',
    observabilityNotes: '',
  };
}

function createParameter(): RestParameter {
  return { id: genId('param'), name: '', in: 'query', required: false, type: 'string', description: '', constraints: {} };
}

function createResponse(): RestResponse {
  return { id: genId('res'), statusCode: '200', description: '', contentType: 'application/json', schema: '', example: '', properties: [] };
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
  /** Estado de preview de exemplo por chave "epId.reqBody" ou "epId.res.resId". */
  const [previewKeys, setPreviewKeys] = useState<Set<string>>(new Set());
  /** Estado de "copiado!" por chave de preview. */
  const [copiedKeys, setCopiedKeys] = useState<Set<string>>(new Set());

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

  /** Alterna o painel de preview de um campo. */
  const togglePreview = (key: string) => {
    setPreviewKeys((prev) => {
      const next = new Set(prev);
      if (next.has(key)) next.delete(key); else next.add(key);
      return next;
    });
  };

  /** Copia texto para a área de transferência e mostra feedback temporário. */
  const copyToClipboard = (key: string, text: string) => {
    navigator.clipboard.writeText(text).then(() => {
      setCopiedKeys((prev) => new Set(prev).add(key));
      setTimeout(() => setCopiedKeys((prev) => { const n = new Set(prev); n.delete(key); return n; }), 2000);
    }).catch(() => {
      // Clipboard permission denied or unavailable — fail silently
    });
  };

  /**
   * Gera avisos de cross-validation inline para um endpoint.
   * Retorna lista de pares [warningKey, fallbackMessage].
   */
  function endpointWarnings(ep: RestEndpoint): { key: string; fallback: string }[] {
    const warns: { key: string; fallback: string }[] = [];

    // GET/DELETE com requestBody
    if (['GET', 'DELETE'].includes(ep.method) && ep.requestBody) {
      warns.push({
        key: 'contracts.builder.rest.warnBodyOnGet',
        fallback: `${ep.method} requests should not have a request body`,
      });
    }

    // POST/PUT/PATCH com body mas sem propriedades e sem schema ref
    if (['POST', 'PUT', 'PATCH'].includes(ep.method) && ep.requestBody) {
      const hasProps = (ep.requestBody.properties?.length ?? 0) > 0;
      const hasSchema = !!ep.requestBody.schema.trim();
      if (!hasProps && !hasSchema) {
        warns.push({
          key: 'contracts.builder.rest.warnEmptyBody',
          fallback: 'Request body exists but has no schema or properties defined',
        });
      }
    }

    // Path params declarados vs path template
    if (ep.path) {
      const templateParams = ep.path.match(/\{([^}]+)\}/g)?.map((m) => m.slice(1, -1)) ?? [];
      const declared = ep.parameters.filter((p) => p.in === 'path').map((p) => p.name);
      for (const pn of templateParams) {
        if (!declared.includes(pn)) {
          warns.push({
            key: 'contracts.builder.rest.warnMissingPathParam',
            fallback: `Path parameter '{${pn}}' is used in path but not declared`,
          });
        }
      }
    }

    // Nenhuma resposta 2xx
    const has2xx = ep.responses.some((r) => String(r.statusCode).startsWith('2'));
    if (ep.responses.length > 0 && !has2xx) {
      warns.push({
        key: 'contracts.builder.rest.warnNo2xx',
        fallback: 'No 2xx success response defined',
      });
    }

    return warns;
  }

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
              const epWarnings = endpointWarnings(ep);

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
                    {epWarnings.length > 0 && (
                      <span className="flex items-center gap-0.5 text-[9px] text-warning bg-warning/10 px-1.5 py-0.5 rounded" title={epWarnings.map((w) => t(w.key, w.fallback)).join('; ')}>
                        <AlertTriangle size={9} /> {epWarnings.length}
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

                      {/* ── Cross-validation warnings ── */}
                      {epWarnings.length > 0 && (
                        <div className="flex items-start gap-2 px-3 py-2 text-[10px] rounded-md bg-warning/8 border border-warning/20 text-warning">
                          <AlertTriangle size={12} className="flex-shrink-0 mt-0.5" />
                          <ul className="space-y-0.5">
                            {epWarnings.map((w) => (
                              <li key={w.key}>⚠ {t(w.key, w.fallback)}</li>
                            ))}
                          </ul>
                        </div>
                      )}

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
                        {/* OperationId with auto-suggest button */}
                        <div>
                          <label className="block text-[9px] font-semibold text-muted uppercase tracking-wider mb-1">
                            {t('contracts.builder.rest.operationId', 'Operation ID')}
                          </label>
                          <div className="flex items-center gap-1">
                            <input
                              type="text"
                              value={ep.operationId}
                              onChange={(e) => updateEndpoint(ep.id, { operationId: e.target.value })}
                              placeholder={t('contracts.builder.rest.operationIdPlaceholder', 'getUser')}
                              disabled={isReadOnly}
                              className="flex-1 min-w-0 text-[10px] font-mono bg-elevated border border-edge rounded px-2 py-1 text-body placeholder:text-muted/30"
                            />
                            {!isReadOnly && (
                              <button
                                type="button"
                                onClick={() => {
                                  const suggested = generateOperationId(ep.method, ep.path);
                                  updateEndpoint(ep.id, { operationId: suggested });
                                }}
                                title={t('contracts.builder.rest.autoSuggest', 'Auto-suggest')}
                                className="flex items-center gap-0.5 text-[9px] text-accent hover:text-accent/80 border border-accent/30 rounded px-1.5 py-1 transition-colors whitespace-nowrap"
                              >
                                <Zap size={9} />
                              </button>
                            )}
                          </div>
                        </div>
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
                          <div key={param.id} className="space-y-1">
                            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-2 items-end">
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
                              <div className="flex items-center gap-1 pb-1">
                                {!isReadOnly && (
                                  <button type="button" onClick={() => toggleSubSection(ep.id, `param-constraints-${param.id}`)}
                                    className="text-[9px] text-accent/70 hover:text-accent transition-colors whitespace-nowrap"
                                    title={t('contracts.builder.rest.constraintsToggle', 'Show constraints')}>
                                    {isSubExpanded(ep.id, `param-constraints-${param.id}`) ? '▾' : '▸'} {t('contracts.builder.rest.constraints', 'Constraints')}
                                  </button>
                                )}
                                {!isReadOnly && (
                                  <button type="button" onClick={() => updateEndpoint(ep.id, { parameters: ep.parameters.filter((_, j) => j !== pi) })}
                                    className="text-muted hover:text-danger transition-colors"><Trash2 size={11} /></button>
                                )}
                              </div>
                            </div>

                            {/* ── Parameter Constraints Panel ── */}
                            {isSubExpanded(ep.id, `param-constraints-${param.id}`) && (
                              <ParameterConstraintsPanel
                                constraints={param.constraints ?? {}}
                                paramType={param.type}
                                onChange={(c) => {
                                  const next = [...ep.parameters]; next[pi] = { ...param, constraints: c };
                                  updateEndpoint(ep.id, { parameters: next });
                                }}
                                isReadOnly={isReadOnly}
                              />
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

                              {/* Schema mode toggle: $ref vs Visual Properties */}
                              <div className="flex items-center gap-2 py-1">
                                <span className="text-[9px] font-semibold text-muted uppercase tracking-wider">
                                  {t('contracts.builder.rest.schemaMode', 'Schema Mode')}:
                                </span>
                                <button type="button"
                                  onClick={() => updateEndpoint(ep.id, { requestBody: { ...ep.requestBody!, properties: undefined } })}
                                  className={`text-[9px] px-2 py-0.5 rounded transition-colors ${
                                    (!ep.requestBody.properties || ep.requestBody.properties.length === 0) && ep.requestBody.schema
                                      ? 'bg-accent/15 text-accent border border-accent/25'
                                      : 'bg-muted/10 text-muted border border-edge hover:text-body'
                                  }`}>
                                  {t('contracts.builder.rest.modeRef', '$ref (Schema Reference)')}
                                </button>
                                <button type="button"
                                  onClick={() => updateEndpoint(ep.id, { requestBody: { ...ep.requestBody!, schema: '', properties: ep.requestBody!.properties ?? [] } })}
                                  className={`text-[9px] px-2 py-0.5 rounded transition-colors ${
                                    ep.requestBody.properties && ep.requestBody.properties.length >= 0 && !ep.requestBody.schema
                                      ? 'bg-accent/15 text-accent border border-accent/25'
                                      : 'bg-muted/10 text-muted border border-edge hover:text-body'
                                  }`}>
                                  {t('contracts.builder.rest.modeVisualProps', '✦ Visual Properties')}
                                </button>
                              </div>

                              {/* $ref mode */}
                              {(!ep.requestBody.properties || (ep.requestBody.properties.length === 0 && ep.requestBody.schema)) && (
                                <>
                                  <Field label={t('contracts.builder.rest.schema', 'Schema ($ref)')} value={ep.requestBody.schema}
                                    onChange={(v) => updateEndpoint(ep.id, { requestBody: { ...ep.requestBody!, schema: v } })}
                                    placeholder={t('contracts.builder.rest.schemaPlaceholder', '#/components/schemas/CreateUserRequest')} mono disabled={isReadOnly} />
                                  <p className="text-[8px] text-muted/50">
                                    {t('contracts.builder.rest.refModeHint', 'Reference a schema from Canonical Entities or define inline using Visual Properties mode')}
                                  </p>
                                </>
                              )}

                              {/* Visual properties mode */}
                              {ep.requestBody.properties && (!ep.requestBody.schema || ep.requestBody.properties.length > 0) && (
                                <div className="space-y-1">
                                  <label className="block text-[9px] font-semibold text-muted uppercase tracking-wider">
                                    {t('contracts.builder.rest.bodyProperties', 'Request Body Properties')}
                                  </label>
                                  <SchemaPropertyEditor
                                    properties={ep.requestBody.properties}
                                    onChange={(props) => updateEndpoint(ep.id, { requestBody: { ...ep.requestBody!, properties: props } })}
                                    isReadOnly={isReadOnly}
                                    addLabel={t('contracts.builder.rest.addBodyProp', 'Add Property')}
                                  />
                                </div>
                              )}

                              <FieldArea label={t('contracts.builder.rest.example', 'Example')} value={ep.requestBody.example}
                                onChange={(v) => updateEndpoint(ep.id, { requestBody: { ...ep.requestBody!, example: v } })}
                                rows={3} mono disabled={isReadOnly} />
                              {!isReadOnly && ep.requestBody.properties && ep.requestBody.properties.length > 0 && (
                                <div className="flex items-center gap-2 flex-wrap">
                                  <button type="button" onClick={() => {
                                    const example = generateExampleFromSchema(ep.requestBody!.properties!);
                                    updateEndpoint(ep.id, { requestBody: { ...ep.requestBody!, example: formatExample(example) } });
                                  }}
                                    className="inline-flex items-center gap-1 text-[9px] text-accent hover:text-accent/80 transition-colors">
                                    <Sparkles size={9} />
                                    {t('contracts.builder.rest.generateExample', 'Generate Example')}
                                  </button>
                                  <button type="button" onClick={() => togglePreview(`${ep.id}.reqBody`)}
                                    className="inline-flex items-center gap-1 text-[9px] text-muted hover:text-body transition-colors">
                                    {previewKeys.has(`${ep.id}.reqBody`) ? '▾' : '▸'}
                                    {t('contracts.builder.rest.previewExample', 'Preview Example')}
                                  </button>
                                </div>
                              )}
                              {/* Preview panel for request body */}
                              {previewKeys.has(`${ep.id}.reqBody`) && ep.requestBody.properties && ep.requestBody.properties.length > 0 && (
                                <div className="relative rounded border border-edge bg-base/50">
                                  <div className="flex items-center justify-between px-2 py-1 border-b border-edge">
                                    <span className="text-[8px] text-muted font-semibold uppercase tracking-wider">
                                      {t('contracts.builder.rest.previewExample', 'Preview Example')}
                                    </span>
                                    <button type="button"
                                      onClick={() => {
                                        const ex = formatExample(generateExampleFromSchema(ep.requestBody!.properties!));
                                        copyToClipboard(`${ep.id}.reqBody`, ex);
                                      }}
                                      className="inline-flex items-center gap-1 text-[8px] text-muted hover:text-body transition-colors"
                                    >
                                      {copiedKeys.has(`${ep.id}.reqBody`)
                                        ? <><Check size={8} className="text-mint" /> {t('contracts.builder.rest.exampleCopied', 'Copied!')}</>
                                        : <><ClipboardCopy size={8} /> {t('contracts.builder.rest.copyExample', 'Copy')}</>
                                      }
                                    </button>
                                  </div>
                                  <pre className="text-[9px] font-mono p-2 overflow-x-auto text-body/80 max-h-40">
                                    {formatExample(generateExampleFromSchema(ep.requestBody.properties))}
                                  </pre>
                                </div>
                              )}
                              {!isReadOnly && (
                                <button type="button" onClick={() => updateEndpoint(ep.id, { requestBody: null })}
                                  className="text-[10px] text-danger hover:text-danger/80 transition-colors">
                                  {t('contracts.builder.rest.removeBody', 'Remove Request Body')}
                                </button>
                              )}
                            </div>
                          ) : (!isReadOnly && (
                            <button type="button" onClick={() => updateEndpoint(ep.id, { requestBody: { contentType: 'application/json', schema: '', required: true, example: '', properties: [] } })}
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

                            {/* Schema mode toggle for response */}
                            <div className="flex items-center gap-2 py-1">
                              <span className="text-[9px] font-semibold text-muted uppercase tracking-wider">
                                {t('contracts.builder.rest.schemaMode', 'Schema Mode')}:
                              </span>
                              <button type="button"
                                onClick={() => {
                                  const next = [...ep.responses]; next[ri] = { ...res, properties: undefined };
                                  updateEndpoint(ep.id, { responses: next });
                                }}
                                className={`text-[9px] px-2 py-0.5 rounded transition-colors ${
                                  (!res.properties || res.properties.length === 0) && res.schema
                                    ? 'bg-accent/15 text-accent border border-accent/25'
                                    : 'bg-muted/10 text-muted border border-edge hover:text-body'
                                }`}>
                                {t('contracts.builder.rest.modeRef', '$ref (Schema Reference)')}
                              </button>
                              <button type="button"
                                onClick={() => {
                                  const next = [...ep.responses]; next[ri] = { ...res, schema: '', properties: res.properties ?? [] };
                                  updateEndpoint(ep.id, { responses: next });
                                }}
                                className={`text-[9px] px-2 py-0.5 rounded transition-colors ${
                                  res.properties && res.properties.length >= 0 && !res.schema
                                    ? 'bg-accent/15 text-accent border border-accent/25'
                                    : 'bg-muted/10 text-muted border border-edge hover:text-body'
                                }`}>
                                {t('contracts.builder.rest.modeVisualProps', '✦ Visual Properties')}
                              </button>
                            </div>

                            {/* $ref mode */}
                            {(!res.properties || (res.properties.length === 0 && res.schema)) && (
                              <Field label={t('contracts.builder.rest.schema', 'Schema ($ref)')} value={res.schema}
                                onChange={(v) => { const next = [...ep.responses]; next[ri] = { ...res, schema: v }; updateEndpoint(ep.id, { responses: next }); }}
                                placeholder={t('contracts.builder.rest.resSchemaPlaceholder', '#/components/schemas/User')} mono disabled={isReadOnly} />
                            )}

                            {/* Visual properties mode */}
                            {res.properties && (!res.schema || res.properties.length > 0) && (
                              <div className="space-y-1">
                                <label className="block text-[9px] font-semibold text-muted uppercase tracking-wider">
                                  {t('contracts.builder.rest.responseProperties', 'Response Properties')}
                                </label>
                                <SchemaPropertyEditor
                                  properties={res.properties}
                                  onChange={(props) => {
                                    const next = [...ep.responses]; next[ri] = { ...res, properties: props };
                                    updateEndpoint(ep.id, { responses: next });
                                  }}
                                  isReadOnly={isReadOnly}
                                  addLabel={t('contracts.builder.rest.addResponseProp', 'Add Property')}
                                />
                              </div>
                            )}

                            <FieldArea label={t('contracts.builder.rest.example', 'Example')} value={res.example}
                              onChange={(v) => { const next = [...ep.responses]; next[ri] = { ...res, example: v }; updateEndpoint(ep.id, { responses: next }); }}
                              rows={2} mono disabled={isReadOnly} />
                            {!isReadOnly && res.properties && res.properties.length > 0 && (
                              <div className="flex items-center gap-2 flex-wrap">
                                <button type="button" onClick={() => {
                                  const example = generateExampleFromSchema(res.properties!);
                                  const next = [...ep.responses]; next[ri] = { ...res, example: formatExample(example) };
                                  updateEndpoint(ep.id, { responses: next });
                                }}
                                  className="inline-flex items-center gap-1 text-[9px] text-accent hover:text-accent/80 transition-colors">
                                  <Sparkles size={9} />
                                  {t('contracts.builder.rest.generateExample', 'Generate Example')}
                                </button>
                                <button type="button" onClick={() => togglePreview(`${ep.id}.res.${res.id}`)}
                                  className="inline-flex items-center gap-1 text-[9px] text-muted hover:text-body transition-colors">
                                  {previewKeys.has(`${ep.id}.res.${res.id}`) ? '▾' : '▸'}
                                  {t('contracts.builder.rest.previewExample', 'Preview Example')}
                                </button>
                              </div>
                            )}
                            {/* Preview panel for response */}
                            {previewKeys.has(`${ep.id}.res.${res.id}`) && res.properties && res.properties.length > 0 && (
                              <div className="relative rounded border border-edge bg-base/50">
                                <div className="flex items-center justify-between px-2 py-1 border-b border-edge">
                                  <span className="text-[8px] text-muted font-semibold uppercase tracking-wider">
                                    {t('contracts.builder.rest.previewExample', 'Preview Example')}
                                  </span>
                                  <button type="button"
                                    onClick={() => {
                                      const ex = formatExample(generateExampleFromSchema(res.properties!));
                                      copyToClipboard(`${ep.id}.res.${res.id}`, ex);
                                    }}
                                    className="inline-flex items-center gap-1 text-[8px] text-muted hover:text-body transition-colors"
                                  >
                                    {copiedKeys.has(`${ep.id}.res.${res.id}`)
                                      ? <><Check size={8} className="text-mint" /> {t('contracts.builder.rest.exampleCopied', 'Copied!')}</>
                                      : <><ClipboardCopy size={8} /> {t('contracts.builder.rest.copyExample', 'Copy')}</>
                                    }
                                  </button>
                                </div>
                                <pre className="text-[9px] font-mono p-2 overflow-x-auto text-body/80 max-h-40">
                                  {formatExample(generateExampleFromSchema(res.properties))}
                                </pre>
                              </div>
                            )}
                            {!isReadOnly && (
                              <button type="button" onClick={() => updateEndpoint(ep.id, { responses: ep.responses.filter((_, j) => j !== ri) })}
                                className="text-[10px] text-danger hover:text-danger/80 transition-colors">
                                {t('contracts.builder.rest.removeResponse', 'Remove')}
                              </button>
                            )}
                          </div>
                        ))}
                        {!isReadOnly && (
                          <div className="flex items-center gap-3 flex-wrap pt-1">
                            <button type="button" onClick={() => updateEndpoint(ep.id, { responses: [...ep.responses, createResponse()] })}
                              className="text-[10px] text-accent hover:text-accent/80 transition-colors">
                              + {t('contracts.builder.rest.addResponse', 'Add Response')}
                            </button>
                            <button type="button" onClick={() => {
                              const existing = new Set(ep.responses.map((r) => r.statusCode));
                              const toAdd = COMMON_ERROR_RESPONSES.filter((r) => !existing.has(r.statusCode));
                              if (toAdd.length > 0) {
                                updateEndpoint(ep.id, { responses: [...ep.responses, ...toAdd.map((r) => ({ ...r, id: genId('res') }))] });
                              }
                            }}
                              className="inline-flex items-center gap-1 text-[10px] text-muted hover:text-body transition-colors border border-edge/50 rounded px-2 py-0.5">
                              <Plus size={9} />
                              {t('contracts.builder.rest.addCommonResponses', 'Add common error responses')}
                            </button>
                          </div>
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

// ── Parameter Constraints Panel ────────────────────────────────────────────────

const FORMAT_OPTIONS = ['', 'date', 'date-time', 'email', 'uri', 'uuid', 'hostname', 'ipv4', 'ipv6', 'byte', 'binary', 'password', 'int32', 'int64', 'float', 'double'] as const;

function ParameterConstraintsPanel({
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
                placeholder="^[a-z]+$" className="w-full text-[10px] bg-elevated border border-edge rounded px-2 py-1 text-body font-mono" disabled={isReadOnly} />
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
