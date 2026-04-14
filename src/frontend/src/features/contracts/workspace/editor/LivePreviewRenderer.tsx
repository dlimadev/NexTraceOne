import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Globe,
  Shield,
  Tag,
  ChevronDown,
  ChevronRight,
  AlertTriangle,
  Loader2,
  Eye,
  Server,
  Database,
  Lock,
} from 'lucide-react';
import type { PreviewModel, PreviewOperation, PreviewSchemaElement, PreviewResponse, PreviewRequestBody } from '../../hooks/useSpecPreview';

interface LivePreviewRendererProps {
  preview: PreviewModel | null;
  error: string | null;
  isLoading: boolean;
  className?: string;
}

/**
 * Renderer de preview em tempo real para especificações de contrato.
 * Apresenta info, servers, endpoints/operations, schemas e security
 * com estética inspirada no Swagger UI mas usando o design system NexTraceOne.
 */
export function LivePreviewRenderer({
  preview,
  error,
  isLoading,
  className = '',
}: LivePreviewRendererProps) {
  const { t } = useTranslation();

  if (isLoading) {
    return (
      <div className={`flex flex-col items-center justify-center h-full text-muted gap-2 ${className}`}>
        <Loader2 size={20} className="animate-spin" />
        <span className="text-xs">{t('contracts.workspace.splitEditor.parsing', 'Parsing specification...')}</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className={`flex flex-col items-center justify-center h-full text-muted gap-3 p-6 ${className}`}>
        <AlertTriangle size={24} className="text-warning" />
        <p className="text-xs text-warning font-medium">{t('contracts.workspace.splitEditor.parseError', 'Parse error')}</p>
        <p className="text-[11px] text-muted text-center max-w-md">{error}</p>
      </div>
    );
  }

  if (!preview) {
    return (
      <div className={`flex flex-col items-center justify-center h-full text-muted gap-2 ${className}`}>
        <Eye size={24} className="text-muted/30" />
        <span className="text-xs">{t('contracts.workspace.splitEditor.emptyPreview', 'Start typing to see the live preview')}</span>
      </div>
    );
  }

  return (
    <div className={`h-full overflow-y-auto bg-surface ${className}`}>
      <div className="max-w-2xl mx-auto p-5 space-y-5">
        {/* ── Info Header ── */}
        <InfoSection preview={preview} />

        {/* ── Servers ── */}
        {preview.servers.length > 0 && <ServersSection servers={preview.servers} />}

        {/* ── Security ── */}
        {preview.securitySchemes.length > 0 && <SecuritySection schemes={preview.securitySchemes} />}

        {/* ── Operations / Endpoints ── */}
        {preview.operations.length > 0 && (
          <OperationsSection operations={preview.operations} protocol={preview.protocol} />
        )}

        {/* ── Schemas / Models ── */}
        {preview.schemas.length > 0 && <SchemasSection schemas={preview.schemas} />}

        {/* ── Tags ── */}
        {preview.tags.length > 0 && <TagsSection tags={preview.tags} />}
      </div>
    </div>
  );
}

// ── Sub-components ───────────────────────────────────────────────────────────

function InfoSection({ preview }: { preview: PreviewModel }) {
  const { t } = useTranslation();

  return (
    <div className="space-y-2">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h2 className="text-base font-semibold text-heading">{preview.title}</h2>
          <div className="flex items-center gap-2 mt-1">
            <span className="text-[10px] px-1.5 py-0.5 rounded bg-accent/10 text-accent font-medium">
              {preview.specVersion}
            </span>
            <span className="text-[10px] px-1.5 py-0.5 rounded bg-elevated border border-edge text-muted uppercase">
              {preview.protocol}
            </span>
          </div>
        </div>
        <div className="flex gap-3 text-[10px] text-muted">
          <span>{preview.operationCount} {t('contracts.workspace.splitEditor.operations', 'operations')}</span>
          <span>{preview.schemaCount} {t('contracts.workspace.splitEditor.schemas', 'schemas')}</span>
        </div>
      </div>
      {preview.description && (
        <p className="text-xs text-body leading-relaxed">{preview.description}</p>
      )}
    </div>
  );
}

function ServersSection({ servers }: { servers: string[] }) {
  const { t } = useTranslation();

  return (
    <section>
      <SectionHeader icon={Server} label={t('contracts.workspace.splitEditor.servers', 'Servers')} />
      <div className="space-y-1.5 mt-2">
        {servers.map((server, idx) => (
          <div key={idx} className="flex items-center gap-2 px-3 py-2 bg-elevated rounded-md border border-edge">
            <Globe size={12} className="text-mint flex-shrink-0" />
            <span className="text-xs font-mono text-body truncate">{server}</span>
          </div>
        ))}
      </div>
    </section>
  );
}

function SecuritySection({ schemes }: { schemes: string[] }) {
  const { t } = useTranslation();

  return (
    <section>
      <SectionHeader icon={Lock} label={t('contracts.workspace.splitEditor.security', 'Security')} />
      <div className="flex flex-wrap gap-1.5 mt-2">
        {schemes.map((scheme, idx) => (
          <span key={idx} className="inline-flex items-center gap-1 text-[10px] px-2 py-1 rounded bg-warning/10 text-warning border border-warning/20">
            <Shield size={10} />
            {scheme}
          </span>
        ))}
      </div>
    </section>
  );
}

function OperationsSection({ operations, protocol }: { operations: PreviewOperation[]; protocol: string }) {
  const { t } = useTranslation();

  return (
    <section>
      <SectionHeader
        icon={Globe}
        label={protocol === 'AsyncApi'
          ? t('contracts.workspace.splitEditor.channels', 'Channels')
          : protocol === 'Wsdl'
            ? t('contracts.workspace.splitEditor.soapOperations', 'SOAP Operations')
            : t('contracts.workspace.splitEditor.endpoints', 'Endpoints')}
      />
      <div className="space-y-1.5 mt-2">
        {operations.map((op) => (
          <OperationCard key={op.operationId} operation={op} />
        ))}
      </div>
    </section>
  );
}

function OperationCard({ operation }: { operation: PreviewOperation }) {
  const [expanded, setExpanded] = useState(false);

  return (
    <div className="rounded-md border border-edge overflow-hidden">
      <button
        type="button"
        onClick={() => setExpanded(!expanded)}
        className="w-full flex items-center gap-2 px-3 py-2 bg-elevated hover:bg-elevated/80 transition-colors text-left"
      >
        <MethodBadge method={operation.method} />
        <span className="text-xs font-mono text-heading truncate flex-1">{operation.path}</span>
        {operation.description && (
          <span className="text-[10px] text-muted truncate max-w-[200px]">{operation.description}</span>
        )}
        {operation.isDeprecated && (
          <span className="text-[9px] px-1 py-0.5 bg-danger/10 text-danger rounded">deprecated</span>
        )}
        {expanded ? <ChevronDown size={12} className="text-muted" /> : <ChevronRight size={12} className="text-muted" />}
      </button>

      {expanded && (
        <div className="px-4 py-3 border-t border-edge bg-panel space-y-4">
          {operation.name && operation.name !== operation.operationId && (
            <p className="text-[10px] text-muted">
              <span className="font-medium text-body">{operation.name}</span>
            </p>
          )}
          {operation.description && (
            <p className="text-[11px] text-body leading-relaxed">{operation.description}</p>
          )}

          {/* ── Parameters ── */}
          <ParametersSection params={operation.inputParameters} />

          {/* ── Request Body ── */}
          {operation.requestBody && <RequestBodySection body={operation.requestBody} />}

          {/* ── Responses ── */}
          {operation.responses && operation.responses.length > 0 && (
            <ResponsesSection responses={operation.responses} />
          )}

          {/* ── Tags ── */}
          {operation.tags.length > 0 && (
            <div className="flex flex-wrap gap-1">
              {operation.tags.map((tag) => (
                <span key={tag} className="text-[9px] px-1.5 py-0.5 rounded bg-accent/10 text-accent">
                  {tag}
                </span>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function ParametersSection({ params }: { params: PreviewSchemaElement[] }) {
  const { t } = useTranslation();

  return (
    <div>
      <p className="text-[11px] font-semibold text-heading mb-2 pb-1 border-b border-edge">
        {t('contracts.studio.livePreview.parameters', 'Parameters')}
      </p>
      {params.length === 0 ? (
        <p className="text-[10px] text-muted italic">{t('contracts.studio.livePreview.noParameters', 'No parameters')}</p>
      ) : (
        <div className="space-y-1">
          {params.map((p) => (
            <div key={p.name} className="flex items-center gap-2 text-[11px] py-0.5">
              <span className="font-mono text-heading">{p.name}</span>
              <span className="text-muted">{p.dataType}{p.format ? ` (${p.format})` : ''}</span>
              {p.isRequired && (
                <span className="text-[9px] px-1 py-0 rounded bg-danger/10 text-danger">
                  {t('contracts.studio.livePreview.required', 'required')}
                </span>
              )}
              {p.description && (
                <span className="text-[10px] text-muted truncate max-w-[200px]">{p.description}</span>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function RequestBodySection({ body }: { body: PreviewRequestBody }) {
  const { t } = useTranslation();
  const [showSchema, setShowSchema] = useState(false);
  const example = useMemo(() => generateExampleJson(body.properties), [body.properties]);

  return (
    <div>
      <div className="flex items-center gap-2 mb-2 pb-1 border-b border-edge">
        <p className="text-[11px] font-semibold text-heading">
          {t('contracts.studio.livePreview.requestBody', 'Request body')}
        </p>
        {body.isRequired && (
          <span className="text-[9px] px-1 py-0 rounded bg-danger/10 text-danger">
            {t('contracts.studio.livePreview.required', 'required')}
          </span>
        )}
        <span className="ml-auto text-[10px] text-muted px-2 py-0.5 rounded bg-elevated border border-edge">
          {body.contentType}
        </span>
      </div>

      {body.properties.length > 0 && (
        <div>
          <div className="flex items-center gap-2 mb-1.5">
            <button
              type="button"
              onClick={() => setShowSchema(false)}
              className={`text-[10px] px-1.5 py-0.5 rounded transition-colors ${
                !showSchema ? 'text-accent bg-accent/10 border border-accent/20' : 'text-muted hover:text-body'
              }`}
            >
              {t('contracts.studio.livePreview.exampleValue', 'Example Value')}
            </button>
            <button
              type="button"
              onClick={() => setShowSchema(true)}
              className={`text-[10px] px-1.5 py-0.5 rounded transition-colors ${
                showSchema ? 'text-accent bg-accent/10 border border-accent/20' : 'text-muted hover:text-body'
              }`}
            >
              {t('contracts.studio.livePreview.schema', 'Schema')}
            </button>
          </div>

          {showSchema ? (
            <SchemaPropertyTable elements={body.properties} />
          ) : (
            <ExampleBlock json={example} />
          )}
        </div>
      )}

      {body.properties.length === 0 && body.schemaRef && (
        <p className="text-[10px] font-mono text-muted">{body.schemaRef}</p>
      )}
    </div>
  );
}

function ResponsesSection({ responses }: { responses: PreviewResponse[] }) {
  const { t } = useTranslation();

  return (
    <div>
      <p className="text-[11px] font-semibold text-heading mb-2 pb-1 border-b border-edge">
        {t('contracts.studio.livePreview.responses', 'Responses')}
      </p>
      <div className="space-y-3">
        {responses.map((res) => (
          <ResponseItem key={res.statusCode} response={res} />
        ))}
      </div>
    </div>
  );
}

function ResponseItem({ response }: { response: PreviewResponse }) {
  const { t } = useTranslation();
  const [showSchema, setShowSchema] = useState(false);
  const example = useMemo(() => generateExampleJson(response.properties), [response.properties]);
  const statusColor = response.statusCode.startsWith('2')
    ? 'text-mint'
    : response.statusCode.startsWith('4') || response.statusCode.startsWith('5')
      ? 'text-danger'
      : 'text-warning';

  return (
    <div className="space-y-1.5">
      <div className="flex items-center gap-3">
        <span className={`text-xs font-mono font-bold ${statusColor}`}>{response.statusCode}</span>
        <span className="text-[11px] text-body flex-1">{response.description}</span>
        {response.contentType && (
          <span className="text-[10px] text-muted px-2 py-0.5 rounded bg-elevated border border-edge">
            {response.contentType}
          </span>
        )}
      </div>

      {response.properties.length > 0 && (
        <div className="ml-4">
          <div className="flex items-center gap-2 mb-1.5">
            <button
              type="button"
              onClick={() => setShowSchema(false)}
              className={`text-[10px] px-1.5 py-0.5 rounded transition-colors ${
                !showSchema ? 'text-accent bg-accent/10 border border-accent/20' : 'text-muted hover:text-body'
              }`}
            >
              {t('contracts.studio.livePreview.exampleValue', 'Example Value')}
            </button>
            <button
              type="button"
              onClick={() => setShowSchema(true)}
              className={`text-[10px] px-1.5 py-0.5 rounded transition-colors ${
                showSchema ? 'text-accent bg-accent/10 border border-accent/20' : 'text-muted hover:text-body'
              }`}
            >
              {t('contracts.studio.livePreview.schema', 'Schema')}
            </button>
          </div>

          {showSchema ? (
            <SchemaPropertyTable elements={response.properties} />
          ) : (
            <ExampleBlock json={example} />
          )}
        </div>
      )}

      {response.properties.length === 0 && response.schemaRef && (
        <p className="ml-4 text-[10px] font-mono text-muted">{response.schemaRef}</p>
      )}
    </div>
  );
}

/** Renders a JSON example block with syntax-highlighted output. */
function ExampleBlock({ json }: { json: string }) {
  return (
    <pre className="text-[10px] font-mono p-3 rounded-md bg-base border border-edge overflow-x-auto text-body/80 leading-relaxed max-h-48">
      {json}
    </pre>
  );
}

/** Renders a table-like view of schema properties. */
function SchemaPropertyTable({ elements }: { elements: PreviewSchemaElement[] }) {
  return (
    <div className="rounded-md border border-edge overflow-hidden">
      <div className="divide-y divide-edge">
        {elements.map((el) => (
          <SchemaPropertyRow key={el.name} element={el} depth={0} />
        ))}
      </div>
    </div>
  );
}

function SchemaPropertyRow({ element, depth }: { element: PreviewSchemaElement; depth: number }) {
  const [expanded, setExpanded] = useState(false);
  const hasChildren = element.children && element.children.length > 0;

  return (
    <>
      <div
        className="flex items-center gap-2 px-3 py-1.5 text-[11px] hover:bg-elevated/30 transition-colors"
        style={{ paddingLeft: `${12 + depth * 16}px` }}
      >
        {hasChildren ? (
          <button type="button" onClick={() => setExpanded(!expanded)} className="text-muted hover:text-body">
            {expanded ? <ChevronDown size={10} /> : <ChevronRight size={10} />}
          </button>
        ) : (
          <span className="w-[10px]" />
        )}
        <span className="font-mono text-heading">{element.name}</span>
        <span className="text-muted">{element.dataType}{element.format ? ` (${element.format})` : ''}</span>
        {element.isRequired && (
          <span className="text-[9px] px-1 py-0 rounded bg-danger/10 text-danger">required</span>
        )}
        {element.isDeprecated && (
          <span className="text-[9px] px-1 py-0 rounded bg-warning/10 text-warning">deprecated</span>
        )}
        {element.description && (
          <span className="text-[10px] text-muted truncate max-w-[180px] ml-auto">{element.description}</span>
        )}
      </div>
      {expanded && hasChildren && element.children!.map((child) => (
        <SchemaPropertyRow key={child.name} element={child} depth={depth + 1} />
      ))}
    </>
  );
}

function SchemasSection({ schemas }: { schemas: PreviewSchemaElement[] }) {
  const { t } = useTranslation();

  return (
    <section>
      <SectionHeader icon={Database} label={t('contracts.workspace.splitEditor.models', 'Models')} />
      <div className="space-y-1.5 mt-2">
        {schemas.map((schema) => (
          <SchemaCard key={schema.name} schema={schema} />
        ))}
      </div>
    </section>
  );
}

function SchemaCard({ schema }: { schema: PreviewSchemaElement }) {
  const [expanded, setExpanded] = useState(false);
  const [showSchema, setShowSchema] = useState(false);
  const { t } = useTranslation();
  const hasChildren = schema.children && schema.children.length > 0;
  const example = useMemo(() => hasChildren ? generateExampleJson(schema.children!) : '{}', [schema.children, hasChildren]);

  return (
    <div className="rounded-md border border-edge overflow-hidden">
      <button
        type="button"
        onClick={() => setExpanded(!expanded)}
        className="w-full flex items-center gap-2 px-3 py-2 bg-elevated hover:bg-elevated/80 transition-colors text-left"
      >
        <Database size={12} className="text-accent flex-shrink-0" />
        <span className="text-xs font-medium text-heading">{schema.name}</span>
        <span className="text-[10px] text-muted ml-1">{schema.dataType}</span>
        {hasChildren && (
          <span className="text-[9px] text-muted ml-1">({schema.children!.length} {t('contracts.studio.livePreview.properties', 'properties')})</span>
        )}
        {expanded ? <ChevronDown size={12} className="text-muted ml-auto" /> : <ChevronRight size={12} className="text-muted ml-auto" />}
      </button>

      {expanded && hasChildren && (
        <div className="px-3 py-2 border-t border-edge bg-panel space-y-2">
          <div className="flex items-center gap-2 mb-1">
            <button
              type="button"
              onClick={() => setShowSchema(false)}
              className={`text-[10px] px-1.5 py-0.5 rounded transition-colors ${
                !showSchema ? 'text-accent bg-accent/10 border border-accent/20' : 'text-muted hover:text-body'
              }`}
            >
              {t('contracts.studio.livePreview.exampleValue', 'Example Value')}
            </button>
            <button
              type="button"
              onClick={() => setShowSchema(true)}
              className={`text-[10px] px-1.5 py-0.5 rounded transition-colors ${
                showSchema ? 'text-accent bg-accent/10 border border-accent/20' : 'text-muted hover:text-body'
              }`}
            >
              {t('contracts.studio.livePreview.schema', 'Schema')}
            </button>
          </div>

          {showSchema ? (
            <SchemaPropertyTable elements={schema.children!} />
          ) : (
            <ExampleBlock json={example} />
          )}
        </div>
      )}

      {expanded && !hasChildren && (
        <div className="px-3 py-2 border-t border-edge bg-panel">
          <p className="text-[10px] text-muted italic">{t('contracts.studio.livePreview.noProperties', 'No properties defined')}</p>
        </div>
      )}
    </div>
  );
}

function TagsSection({ tags }: { tags: string[] }) {
  const { t } = useTranslation();

  return (
    <section>
      <SectionHeader icon={Tag} label={t('contracts.workspace.splitEditor.tags', 'Tags')} />
      <div className="flex flex-wrap gap-1.5 mt-2">
        {tags.map((tag) => (
          <span key={tag} className="text-[10px] px-2 py-1 rounded bg-accent/10 text-accent border border-accent/20">
            {tag}
          </span>
        ))}
      </div>
    </section>
  );
}

// ── Primitives ───────────────────────────────────────────────────────────────

function SectionHeader({ icon: Icon, label }: { icon: React.ComponentType<{ size?: number; className?: string }>; label: string }) {
  return (
    <div className="flex items-center gap-2">
      <Icon size={14} className="text-accent" />
      <h3 className="text-xs font-semibold text-heading uppercase tracking-wider">{label}</h3>
    </div>
  );
}

const METHOD_COLORS: Record<string, string> = {
  get: 'bg-mint/10 text-mint border-mint/20',
  post: 'bg-accent/10 text-accent border-accent/20',
  put: 'bg-warning/10 text-warning border-warning/20',
  patch: 'bg-warning/10 text-warning border-warning/20',
  delete: 'bg-danger/10 text-danger border-danger/20',
  subscribe: 'bg-accent/10 text-accent border-accent/20',
  publish: 'bg-mint/10 text-mint border-mint/20',
};

function MethodBadge({ method }: { method: string }) {
  const colors = METHOD_COLORS[method.toLowerCase()] ?? 'bg-elevated text-muted border-edge';
  return (
    <span className={`inline-flex items-center text-[9px] font-bold uppercase px-1.5 py-0.5 rounded border flex-shrink-0 ${colors}`}>
      {method}
    </span>
  );
}

// ── Example JSON generation ─────────────────────────────────────────────────

const TYPE_EXAMPLES: Record<string, unknown> = {
  string: 'string',
  integer: 0,
  number: 0,
  boolean: true,
  object: {},
  array: [],
};

function exampleValueForElement(el: PreviewSchemaElement): unknown {
  if (el.defaultValue != null) {
    try { return JSON.parse(el.defaultValue); } catch { return el.defaultValue; }
  }

  if (el.format === 'date-time') return '2024-01-01T00:00:00Z';
  if (el.format === 'date') return '2024-01-01';
  if (el.format === 'email') return 'user@example.com';
  if (el.format === 'uuid') return '3fa85f64-5717-4562-b3fc-2c963f66afa6';
  if (el.format === 'uri' || el.format === 'url') return 'https://example.com';
  if (el.format === 'int64') return 0;
  if (el.format === 'int32') return 0;
  if (el.format === 'float' || el.format === 'double') return 0.0;

  if (el.children && el.children.length > 0) {
    const obj: Record<string, unknown> = {};
    for (const child of el.children) {
      obj[child.name] = exampleValueForElement(child);
    }
    if (el.dataType.startsWith('array')) return [obj];
    return obj;
  }

  if (el.dataType.startsWith('array')) {
    const inner = el.dataType.match(/^array<(.+)>$/)?.[1];
    if (inner && inner in TYPE_EXAMPLES) return [TYPE_EXAMPLES[inner]];
    return [];
  }

  return TYPE_EXAMPLES[el.dataType] ?? 'string';
}

function generateExampleJson(elements: PreviewSchemaElement[]): string {
  const obj: Record<string, unknown> = {};
  for (const el of elements) {
    obj[el.name] = exampleValueForElement(el);
  }
  return JSON.stringify(obj, null, 2);
}
