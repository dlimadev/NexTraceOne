import { useState, useCallback, useMemo, useRef, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Code, Columns, Eye, AlertTriangle, Check, List, PanelLeftClose } from 'lucide-react';
import { VisualRestBuilder } from '../builders/VisualRestBuilder';
import { VisualSoapBuilder } from '../builders/VisualSoapBuilder';
import { VisualEventBuilder } from '../builders/VisualEventBuilder';
import { VisualWorkserviceBuilder } from '../builders/VisualWorkserviceBuilder';
import { VisualSharedSchemaBuilder } from '../builders/VisualSharedSchemaBuilder';
import { VisualWebhookBuilder } from '../builders/VisualWebhookBuilder';
import { VisualLegacyContractBuilder } from '../builders/VisualLegacyContractBuilder';
import { ContractEditorSplitPane } from '../editor/ContractEditorSplitPane';
import { LivePreviewRenderer } from '../editor/LivePreviewRenderer';
import { useSpecPreview } from '../../hooks/useSpecPreview';
import {
  parseOpenApiToRest,
  parseWsdlToSoap,
  parseAsyncApiToEvent,
  parseWorkserviceYaml,
  parseJsonSchemaToSharedSchema,
  parseWebhookYaml,
  parseLegacyContractYaml,
} from '../builders/shared/builderParse';
import {
  restBuilderToYaml,
  soapBuilderToXml,
  eventBuilderToYaml,
  workserviceBuilderToYaml,
  sharedSchemaBuilderToJson,
  webhookBuilderToYaml,
  legacyContractBuilderToYaml,
} from '../builders/shared/builderSync';
import type { SyncResult, LegacyContractKind } from '../builders/shared/builderTypes';

type EditorMode = 'visual' | 'source' | 'split' | 'preview';

interface ContractSectionProps {
  specContent: string;
  format: string;
  protocol: string;
  contractType?: string;
  isReadOnly?: boolean;
  onContentChange?: (content: string) => void;
  className?: string;
}

/**
 * Secção de contrato com abas Visual / Source / Split / Preview.
 * Round-trip entre modos de autoria para perfis técnicos e não técnicos.
 * Sync: visual builder gera source; source é carregado no preview.
 * O modo Split apresenta o editor Monaco à esquerda e o live preview à direita.
 */
export function ContractSection({
  specContent,
  format,
  protocol,
  contractType,
  isReadOnly = false,
  onContentChange,
  className = '',
}: ContractSectionProps) {
  const { t } = useTranslation();
  const [mode, setMode] = useState<EditorMode>('split');
  const [content, setContent] = useState(specContent);
  const [syncWarnings, setSyncWarnings] = useState<string[]>([]);
  const [syncSuccess, setSyncSuccess] = useState(false);
  const [builderKey, setBuilderKey] = useState(0);

  const handleContentChange = useCallback(
    (newContent: string) => {
      setContent(newContent);
      onContentChange?.(newContent);
      setSyncSuccess(false);
      setBuilderKey((k) => k + 1);
    },
    [onContentChange],
  );

  const handleSyncFromVisual = useCallback(
    (result: SyncResult) => {
      if (result.success) {
        setContent(result.content);
        onContentChange?.(result.content);
        setSyncWarnings(result.warnings);
        setSyncSuccess(true);
        setMode('source');
        setTimeout(() => setSyncSuccess(false), 3000);
      }
    },
    [onContentChange],
  );

  const handleAutoSyncFromVisual = useCallback(
    (newContent: string) => {
      setContent(newContent);
      onContentChange?.(newContent);
    },
    [onContentChange],
  );

  const modes: { id: EditorMode; labelKey: string; Icon: React.ComponentType<{ size?: number }> }[] = [
    { id: 'visual', labelKey: 'contracts.workspace.visualBuilder', Icon: Columns },
    { id: 'source', labelKey: 'contracts.workspace.sourceEditor', Icon: Code },
    { id: 'split', labelKey: 'contracts.workspace.splitEditor.title', Icon: PanelLeftClose },
    { id: 'preview', labelKey: 'contracts.workspace.preview', Icon: Eye },
  ];

  return (
    <div className={`flex flex-col h-full ${className}`}>
      {/* Mode tabs */}
      <div className="flex items-center gap-1 border-b border-edge px-1 bg-panel">
        {modes.map(({ id, labelKey, Icon }) => (
          <button
            key={id}
            onClick={() => setMode(id)}
            className={`
              inline-flex items-center gap-1.5 px-3 py-2 text-xs font-medium transition-colors border-b-2
              ${mode === id
                ? 'text-accent border-accent'
                : 'text-muted border-transparent hover:text-heading'}
            `}
          >
            <Icon size={13} />
            {t(labelKey, id)}
          </button>
        ))}

        {/* Sync status indicators */}
        <div className="ml-auto flex items-center gap-2">
          {syncSuccess && (
            <span className="inline-flex items-center gap-1 text-[10px] text-mint">
              <Check size={10} /> {t('contracts.workspace.syncSuccess', 'Source updated')}
            </span>
          )}
          {syncWarnings.length > 0 && (
            <span className="inline-flex items-center gap-1 text-[10px] text-warning" title={syncWarnings.map((w) => t(w, w)).join('\n')}>
              <AlertTriangle size={10} /> {t('contracts.workspace.syncWarnings', 'Sync warnings')}
            </span>
          )}
          {isReadOnly && (
            <span className="text-[10px] text-muted px-2">
              {t('contracts.readOnly', 'Read-only')}
            </span>
          )}
        </div>
      </div>

      {/* Content area */}
      <div className="flex-1 min-h-0 overflow-hidden">
        {mode === 'source' && (
          <SourceEditor
            content={content}
            format={format}
            isReadOnly={isReadOnly}
            onChange={handleContentChange}
          />
        )}

        {mode === 'split' && (
          <ContractEditorSplitPane
            content={content}
            format={format}
            protocol={protocol}
            isReadOnly={isReadOnly}
            onChange={handleContentChange}
          />
        )}

        {mode === 'visual' && (
          <div className="h-full overflow-y-auto">
            <VisualBuilderByProtocol
              protocol={protocol}
              contractType={contractType}
              content={content}
              format={format}
              isReadOnly={isReadOnly}
              onSync={handleSyncFromVisual}
              onAutoSync={handleAutoSyncFromVisual}
              builderKey={builderKey}
            />
          </div>
        )}

        {mode === 'preview' && (
          <PreviewPanel content={content} format={format} protocol={protocol} />
        )}
      </div>
    </div>
  );
}

// ── Source Editor (enhanced) ──────────────────────────────────────────────────

interface SourceEditorProps {
  content: string;
  format: string;
  isReadOnly: boolean;
  onChange: (content: string) => void;
}

function SourceEditor({ content, format, isReadOnly, onChange }: SourceEditorProps) {
  const { t } = useTranslation();
  const [showOutline, setShowOutline] = useState(false);
  const lineCount = content ? content.split('\n').length : 0;
  const langHint = format === 'xml' ? 'xml' : format === 'json' ? 'json' : 'yaml';

  const validationStatus = useMemo(() => {
    if (!content) return null;
    try {
      if (format === 'json') {
        JSON.parse(content);
        return { valid: true, message: '' };
      }
      if (format === 'xml' && content.includes('<') && !content.includes('</')) {
        return { valid: false, message: t('contracts.workspace.sourceValidation.unclosedTag', 'Unclosed XML tag detected') };
      }
      return null;
    } catch (e) {
      return { valid: false, message: e instanceof Error ? e.message : t('contracts.workspace.sourceValidation.invalid', 'Invalid format') };
    }
  }, [content, format, t]);

  const outline = useMemo(() => {
    if (!content) return [];
    const entries: { line: number; text: string; depth: number }[] = [];
    const lines = content.split('\n');
    for (let i = 0; i < lines.length; i++) {
      const line = lines[i] ?? '';
      const yamlMatch = line.match(/^(\s*)(\w[\w-]*)\s*:/);
      if (yamlMatch) {
        const indent = yamlMatch[1] ?? '';
        const text = yamlMatch[2] ?? '';
        entries.push({ line: i + 1, text, depth: indent.length / 2 });
      }
    }
    return entries.filter((e) => e.depth <= 2).slice(0, 40);
  }, [content]);

  return (
    <div className="flex h-full">
      {/* Outline panel */}
      {showOutline && outline.length > 0 && (
        <div className="w-48 flex-shrink-0 border-r border-edge bg-panel overflow-y-auto py-2">
          <p className="px-3 py-1 text-[9px] font-semibold uppercase tracking-wider text-muted/60">
            {t('contracts.workspace.outline', 'Outline')}
          </p>
          {outline.map((entry, i) => (
            <button key={i} className="w-full text-left px-3 py-0.5 text-[10px] text-muted hover:text-heading hover:bg-elevated/30 transition-colors truncate"
              style={{ paddingLeft: `${12 + entry.depth * 8}px` }}
              title={`Line ${entry.line}`}
            >
              {entry.text}
            </button>
          ))}
        </div>
      )}

      <div className="flex-1 flex flex-col min-w-0">
        {/* Toolbar */}
        <div className="flex items-center gap-2 px-2 py-1 border-b border-edge bg-panel/50">
          <button onClick={() => setShowOutline(!showOutline)}
            className={`p-1 rounded transition-colors ${showOutline ? 'bg-accent/10 text-accent' : 'text-muted hover:text-heading'}`}
            title={t('contracts.workspace.toggleOutline', 'Toggle outline')}>
            <List size={12} />
          </button>
          <span className="text-[10px] text-muted px-2 py-0.5 rounded bg-elevated border border-edge uppercase">
            {langHint}
          </span>
          <span className="text-[10px] text-muted">
            {t('contracts.workspace.lineCount', '{{count}} lines', { count: lineCount })}
          </span>
          <div className="ml-auto">
            {validationStatus && (
              <span className={`text-[10px] ${validationStatus.valid ? 'text-mint' : 'text-danger'}`}>
                {validationStatus.valid
                  ? t('contracts.workspace.sourceValidation.valid', '✓ Valid')
                  : validationStatus.message}
              </span>
            )}
          </div>
        </div>

        <div className="flex flex-1 min-h-0">
          {/* Line numbers */}
          <div className="flex-shrink-0 py-3 px-2 bg-panel border-r border-edge text-right select-none overflow-y-auto">
            {Array.from({ length: Math.max(lineCount, 20) }, (_, i) => (
              <div key={i} className="text-[10px] leading-5 text-muted/50 font-mono">
                {i + 1}
              </div>
            ))}
          </div>

          {/* Editor area */}
          <textarea
            value={content}
            onChange={(e) => onChange(e.target.value)}
            readOnly={isReadOnly}
            spellCheck={false}
            data-language={langHint}
            className="flex-1 p-3 bg-transparent text-xs font-mono text-body leading-5 resize-none focus:outline-none placeholder:text-muted/40"
            placeholder={t('contracts.specContentPlaceholder', 'Paste your specification here...')}
          />
        </div>
      </div>
    </div>
  );
}

// ── Visual Builder by Protocol ────────────────────────────────────────────────

function VisualBuilderByProtocol({
  protocol,
  contractType,
  content,
  format,
  isReadOnly,
  onSync,
  onAutoSync,
  builderKey,
}: {
  protocol: string;
  contractType?: string;
  content: string;
  format: string;
  isReadOnly?: boolean;
  onSync?: (result: SyncResult) => void;
  onAutoSync?: (content: string) => void;
  builderKey?: number;
}) {
  const resolvedType = contractType ?? resolveTypeFromProtocol(protocol);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    return () => { if (timerRef.current) clearTimeout(timerRef.current); };
  }, []);

  const handleBuilderChange = useCallback((state: unknown) => {
    if (!onAutoSync) return;
    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(() => {
      try {
        let result: SyncResult | null = null;
        switch (resolvedType) {
          case 'RestApi':
            result = restBuilderToYaml(state as Parameters<typeof restBuilderToYaml>[0]);
            break;
          case 'Soap':
            result = soapBuilderToXml(state as Parameters<typeof soapBuilderToXml>[0]);
            break;
          case 'Event':
            result = eventBuilderToYaml(state as Parameters<typeof eventBuilderToYaml>[0]);
            break;
          case 'BackgroundService':
            result = workserviceBuilderToYaml(state as Parameters<typeof workserviceBuilderToYaml>[0]);
            break;
          case 'SharedSchema':
            result = sharedSchemaBuilderToJson(state as Parameters<typeof sharedSchemaBuilderToJson>[0]);
            break;
          case 'Webhook':
            result = webhookBuilderToYaml(state as Parameters<typeof webhookBuilderToYaml>[0]);
            break;
          case 'Copybook':
          case 'MqMessage':
          case 'FixedLayout':
          case 'CicsCommarea':
            result = legacyContractBuilderToYaml(state as Parameters<typeof legacyContractBuilderToYaml>[0]);
            break;
          default:
            result = restBuilderToYaml(state as Parameters<typeof restBuilderToYaml>[0]);
        }
        if (result?.success) onAutoSync(result.content);
      } catch {
        // Sync failure is non-critical — manual Generate Source still available
      }
    }, 500);
  }, [resolvedType, onAutoSync]);

  // Parse source content into initial state for the visual builder.
  // useMemo avoids re-parsing on every render; builderKey ensures
  // the builder re-mounts only when the source changes externally.
  const parsed = useMemo(() => {
    if (!content.trim()) return null;
    try {
      switch (resolvedType) {
        case 'RestApi':
          return parseOpenApiToRest(content, format);
        case 'Soap':
          return parseWsdlToSoap(content);
        case 'Event':
          return parseAsyncApiToEvent(content, format);
        case 'BackgroundService':
          return parseWorkserviceYaml(content, format);
        case 'SharedSchema':
          return parseJsonSchemaToSharedSchema(content, format);
        case 'Webhook':
          return parseWebhookYaml(content, format);
        case 'Copybook':
        case 'MqMessage':
        case 'FixedLayout':
        case 'CicsCommarea':
          return parseLegacyContractYaml(content, format, resolvedType as LegacyContractKind);
        default:
          return parseOpenApiToRest(content, format);
      }
    } catch {
      return null;
    }
  }, [content, format, resolvedType]);

  const stableKey = builderKey ?? content;

  switch (resolvedType) {
    case 'RestApi':
      return <VisualRestBuilder key={stableKey} initialState={parsed?.state as never} isReadOnly={isReadOnly} onChange={handleBuilderChange as never} onSync={onSync} />;
    case 'Soap':
      return <VisualSoapBuilder key={stableKey} initialState={parsed?.state as never} isReadOnly={isReadOnly} onChange={handleBuilderChange as never} onSync={onSync} />;
    case 'Event':
      return <VisualEventBuilder key={stableKey} initialState={parsed?.state as never} isReadOnly={isReadOnly} onChange={handleBuilderChange as never} onSync={onSync} />;
    case 'BackgroundService':
      return <VisualWorkserviceBuilder key={stableKey} initialState={parsed?.state as never} isReadOnly={isReadOnly} onChange={handleBuilderChange as never} onSync={onSync} />;
    case 'SharedSchema':
      return <VisualSharedSchemaBuilder key={stableKey} initialState={parsed?.state as never} isReadOnly={isReadOnly} onChange={handleBuilderChange as never} onSync={onSync} />;
    case 'Webhook':
      return <VisualWebhookBuilder key={stableKey} initialState={parsed?.state as never} isReadOnly={isReadOnly} onChange={handleBuilderChange as never} onSync={onSync} />;
    case 'Copybook':
    case 'MqMessage':
    case 'FixedLayout':
    case 'CicsCommarea':
      return <VisualLegacyContractBuilder key={stableKey} kind={resolvedType as LegacyContractKind} initialState={parsed?.state as never} isReadOnly={isReadOnly} onChange={handleBuilderChange as never} onSync={onSync} />;
    default:
      return <VisualRestBuilder key={stableKey} initialState={parsed?.state as never} isReadOnly={isReadOnly} onChange={handleBuilderChange as never} onSync={onSync} />;
  }
}

function resolveTypeFromProtocol(protocol: string): string {
  switch (protocol) {
    case 'OpenApi':
    case 'Swagger':
      return 'RestApi';
    case 'Wsdl':
      return 'Soap';
    case 'AsyncApi':
      return 'Event';
    default:
      return 'RestApi';
  }
}

// ── Preview Panel (live preview only) ────────────────────────────────────────

function PreviewPanel({ content, format, protocol }: { content: string; format: string; protocol: string }) {
  const { t } = useTranslation();
  const { preview, error, isLoading } = useSpecPreview(content, protocol, format);

  if (!content) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-xs text-muted gap-2">
        <Eye size={24} className="text-muted/30" />
        {t('contracts.workspace.noContentToPreview', 'No content to preview')}
        <p className="text-[10px] text-muted/60">
          {t('contracts.workspace.previewHint', 'Use the Visual Builder or Source Editor to add content.')}
        </p>
      </div>
    );
  }

  return (
    <div className="flex flex-col h-full">
      <div className="flex items-center gap-2 px-2 py-1 border-b border-edge bg-panel/50">
        <span className="text-[10px] font-medium text-muted uppercase tracking-wider">
          {t('contracts.workspace.splitEditor.livePreview', 'Live Preview')}
        </span>
        <div className="ml-auto flex items-center gap-2">
          {isLoading && (
            <span className="text-[10px] text-muted animate-pulse">
              {t('contracts.workspace.splitEditor.updating', 'Updating...')}
            </span>
          )}
          {!isLoading && preview && !error && (
            <span className="inline-flex items-center gap-1 text-[10px] text-mint">
              <Check size={10} />
              {t('contracts.workspace.splitEditor.valid', 'Valid')}
            </span>
          )}
          {!isLoading && error && (
            <span className="inline-flex items-center gap-1 text-[10px] text-warning">
              <AlertTriangle size={10} />
              {t('contracts.workspace.splitEditor.invalid', 'Invalid')}
            </span>
          )}
        </div>
      </div>
      <div className="flex-1 min-h-0 overflow-hidden">
        <LivePreviewRenderer
          preview={preview}
          error={error}
          isLoading={isLoading}
        />
      </div>
      {preview && (
        <div className="flex items-center gap-3 px-3 py-1 border-t border-edge bg-panel text-[10px] text-muted">
          <span>
            {preview.operationCount} {t('contracts.workspace.splitEditor.operations', 'operations')} · {preview.schemaCount} {t('contracts.workspace.splitEditor.schemas', 'schemas')}
          </span>
          {preview.hasSecurityDefinitions && (
            <span className="text-mint">🔒 {t('contracts.workspace.splitEditor.securityDefined', 'Security defined')}</span>
          )}
        </div>
      )}
    </div>
  );
}
