import { useState, useCallback, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Code, Columns, Eye, AlertTriangle, Check, List } from 'lucide-react';
import { VisualRestBuilder } from '../builders/VisualRestBuilder';
import { VisualSoapBuilder } from '../builders/VisualSoapBuilder';
import { VisualEventBuilder } from '../builders/VisualEventBuilder';
import { VisualWorkserviceBuilder } from '../builders/VisualWorkserviceBuilder';
import type { SyncResult } from '../builders/shared/builderTypes';

type EditorMode = 'visual' | 'source' | 'preview';

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
 * Secção de contrato com abas Visual / Source / Preview.
 * Round-trip entre modos de autoria para perfis técnicos e não técnicos.
 * Sync: visual builder gera source; source é carregado no preview.
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
  const [mode, setMode] = useState<EditorMode>('source');
  const [content, setContent] = useState(specContent);
  const [syncWarnings, setSyncWarnings] = useState<string[]>([]);
  const [syncSuccess, setSyncSuccess] = useState(false);

  const handleContentChange = useCallback(
    (newContent: string) => {
      setContent(newContent);
      onContentChange?.(newContent);
      setSyncSuccess(false);
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

  const modes: { id: EditorMode; labelKey: string; Icon: React.ComponentType<{ size?: number }> }[] = [
    { id: 'visual', labelKey: 'contracts.workspace.visualBuilder', Icon: Columns },
    { id: 'source', labelKey: 'contracts.workspace.sourceEditor', Icon: Code },
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
      <div className="flex-1 min-h-0 overflow-auto">
        {mode === 'source' && (
          <SourceEditor
            content={content}
            format={format}
            isReadOnly={isReadOnly}
            onChange={handleContentChange}
          />
        )}

        {mode === 'visual' && (
          <VisualBuilderByProtocol
            protocol={protocol}
            contractType={contractType}
            isReadOnly={isReadOnly}
            onSync={handleSyncFromVisual}
          />
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
      const line = lines[i];
      const yamlMatch = line.match(/^(\s*)(\w[\w-]*)\s*:/);
      if (yamlMatch) {
        entries.push({ line: i + 1, text: yamlMatch[2], depth: yamlMatch[1].length / 2 });
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
  isReadOnly,
  onSync,
}: {
  protocol: string;
  contractType?: string;
  isReadOnly?: boolean;
  onSync?: (result: SyncResult) => void;
}) {
  const resolvedType = contractType ?? resolveTypeFromProtocol(protocol);

  switch (resolvedType) {
    case 'RestApi':
      return <VisualRestBuilder isReadOnly={isReadOnly} onSync={onSync} />;
    case 'Soap':
      return <VisualSoapBuilder isReadOnly={isReadOnly} onSync={onSync} />;
    case 'Event':
      return <VisualEventBuilder isReadOnly={isReadOnly} onSync={onSync} />;
    case 'BackgroundService':
      return <VisualWorkserviceBuilder isReadOnly={isReadOnly} onSync={onSync} />;
    default:
      return <VisualRestBuilder isReadOnly={isReadOnly} onSync={onSync} />;
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

// ── Preview Panel (enhanced) ─────────────────────────────────────────────────

function PreviewPanel({ content, format, protocol }: { content: string; format: string; protocol: string }) {
  const { t } = useTranslation();

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

  const lineCount = content.split('\n').length;

  return (
    <div className="p-4">
      <div className="flex items-center justify-between mb-3">
        <div className="flex items-center gap-2">
          <span className="text-[10px] text-muted px-2 py-0.5 rounded bg-elevated border border-edge uppercase">
            {format}
          </span>
          <span className="text-[10px] text-muted px-2 py-0.5 rounded bg-elevated border border-edge">
            {protocol}
          </span>
        </div>
        <span className="text-[10px] text-muted">{lineCount} {t('contracts.workspace.lines', 'lines')}</span>
      </div>
      <pre className="text-xs font-mono text-body leading-5 whitespace-pre-wrap break-words bg-elevated/30 rounded-lg p-4 border border-edge max-h-[60vh] overflow-auto">
        {content}
      </pre>
    </div>
  );
}
