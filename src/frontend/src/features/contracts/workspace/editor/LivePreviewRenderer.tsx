import { useState } from 'react';
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
import type { PreviewModel, PreviewOperation, PreviewSchemaElement } from '../../hooks/useSpecPreview';

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
        {operation.isDeprecated && (
          <span className="text-[9px] px-1 py-0.5 bg-danger/10 text-danger rounded">deprecated</span>
        )}
        {expanded ? <ChevronDown size={12} className="text-muted" /> : <ChevronRight size={12} className="text-muted" />}
      </button>

      {expanded && (
        <div className="px-3 py-2 border-t border-edge bg-panel space-y-2">
          {operation.name && (
            <p className="text-[10px] text-muted">
              <span className="font-medium text-body">{operation.name}</span>
            </p>
          )}
          {operation.description && (
            <p className="text-[11px] text-body leading-relaxed">{operation.description}</p>
          )}
          {operation.inputParameters.length > 0 && (
            <div>
              <p className="text-[10px] font-semibold text-muted uppercase tracking-wider mb-1">{t('contracts.studio.livePreview.parameters', 'Parameters')}</p>
              <SchemaElementList elements={operation.inputParameters} />
            </div>
          )}
          {operation.outputFields.length > 0 && (
            <div>
              <p className="text-[10px] font-semibold text-muted uppercase tracking-wider mb-1">{t('contracts.studio.livePreview.response', 'Response')}</p>
              <SchemaElementList elements={operation.outputFields} />
            </div>
          )}
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
        {expanded ? <ChevronDown size={12} className="text-muted ml-auto" /> : <ChevronRight size={12} className="text-muted ml-auto" />}
      </button>

      {expanded && schema.children && schema.children.length > 0 && (
        <div className="px-3 py-2 border-t border-edge bg-panel">
          <SchemaElementList elements={schema.children} />
        </div>
      )}
    </div>
  );
}

function SchemaElementList({ elements }: { elements: PreviewSchemaElement[] }) {
  return (
    <div className="space-y-0.5">
      {elements.map((el) => (
        <div key={el.name} className="flex items-center gap-2 text-[11px] py-0.5">
          <span className="font-mono text-heading">{el.name}</span>
          <span className="text-muted">{el.dataType}{el.format ? ` (${el.format})` : ''}</span>
          {el.isRequired && (
            <span className="text-[9px] px-1 py-0 rounded bg-danger/10 text-danger">required</span>
          )}
          {el.isDeprecated && (
            <span className="text-[9px] px-1 py-0 rounded bg-warning/10 text-warning">deprecated</span>
          )}
        </div>
      ))}
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
