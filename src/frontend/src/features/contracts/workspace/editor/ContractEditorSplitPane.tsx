import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import SimpleSplitPane from '../../../../components/SimpleSplitPane';
import { GripVertical, Check, AlertTriangle } from 'lucide-react';
import { MonacoEditorWrapper } from './MonacoEditorWrapper';
import { LivePreviewRenderer } from './LivePreviewRenderer';
import { useSpecPreview } from '../../hooks/useSpecPreview';

interface ContractEditorSplitPaneProps {
  content: string;
  format: string;
  protocol: string;
  isReadOnly?: boolean;
  onChange?: (content: string) => void;
  className?: string;
}

/**
 * Layout split-pane do Contract Studio Editor: editor Monaco à esquerda + live preview à direita.
 * Experiência inspirada no Swagger Editor (editor.swagger.io) com design system NexTraceOne.
 * O painel é resizable e o preview atualiza em tempo real via debounce.
 */
export function ContractEditorSplitPane({
  content,
  format,
  protocol,
  isReadOnly = false,
  onChange,
  className = '',
}: ContractEditorSplitPaneProps) {
  const { t } = useTranslation();
  // Use internal simple split pane to avoid dependency issues in dev.
  const { preview, error, isLoading } = useSpecPreview(content, protocol);

  const monacoLanguage = format === 'xml' ? 'xml' : format === 'json' ? 'json' : 'yaml';
  const lineCount = content ? content.split('\n').length : 0;

  return (
    <div className={`flex flex-col h-full ${className}`}>
      {/* ── Split panels ── */}
      <SimpleSplitPane
        className="flex-1 min-h-0"
        initialLeftPercent={50}
        left={(
          <>
            <div className="flex items-center gap-2 px-2 py-1 border-b border-edge bg-panel/50">
              <span className="text-[10px] text-muted px-2 py-0.5 rounded bg-elevated border border-edge uppercase">
                {monacoLanguage}
              </span>
              <span className="text-[10px] text-muted">
                {t('contracts.workspace.lineCount', '{{count}} lines', { count: lineCount })}
              </span>
              {isReadOnly && (
                <span className="text-[10px] text-muted px-2 ml-auto">
                  {t('contracts.readOnly', 'Read-only')}
                </span>
              )}
            </div>
            <div className="flex-1 min-h-0">
              <MonacoEditorWrapper
                value={content}
                language={monacoLanguage}
                isReadOnly={isReadOnly}
                onChange={onChange}
              />
            </div>
          </>
        )}
        right={(
          <>
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
          </>
        )}
      />

      {/* ── Status bar ── */}
      <div className="flex items-center gap-3 px-3 py-1 border-t border-edge bg-panel text-[10px] text-muted">
        <span>
          {preview
            ? `${preview.operationCount} ${t('contracts.workspace.splitEditor.operations', 'operations')} · ${preview.schemaCount} ${t('contracts.workspace.splitEditor.schemas', 'schemas')}`
            : t('contracts.workspace.splitEditor.noSpec', 'No specification loaded')}
        </span>
        {preview?.hasSecurityDefinitions && (
          <span className="text-mint">🔒 {t('contracts.workspace.splitEditor.securityDefined', 'Security defined')}</span>
        )}
      </div>
    </div>
  );
}
