import { useState, useEffect, useCallback } from 'react';
import * as yaml from 'js-yaml';
import SimpleSplitPane from '../../../components/SimpleSplitPane';
import { MonacoEditorWrapper } from '../workspace/editor/MonacoEditorWrapper';
import { BuilderHeader } from './components/BuilderHeader';
import type { BuilderValidationStatus } from './components/BuilderHeader';

export type BuilderLanguage = 'yaml' | 'json' | 'graphql' | 'proto' | 'xml';

export interface ContractBuilderLayoutProps {
  contractName: string;
  protocol: string;
  language: BuilderLanguage;
  initialContent: string;
  renderPreview: (content: string) => React.ReactNode;
  onSave?: (content: string) => void;
  onPublish?: (content: string) => void;
}

function tryParse(language: BuilderLanguage, content: string): { ok: boolean; line?: number } {
  if (!content.trim()) return { ok: true };
  try {
    if (language === 'yaml') {
      yaml.load(content);
      return { ok: true };
    }
    if (language === 'json') {
      JSON.parse(content);
      return { ok: true };
    }
    return { ok: true };
  } catch (e: unknown) {
    const line = (e as { mark?: { line?: number } })?.mark?.line;
    return { ok: false, line: line !== undefined ? line + 1 : undefined };
  }
}

export function ContractBuilderLayout({
  contractName,
  protocol,
  language,
  initialContent,
  renderPreview,
  onSave,
  onPublish,
}: ContractBuilderLayoutProps) {
  const [content, setContent] = useState(initialContent);
  const [debouncedContent, setDebouncedContent] = useState(initialContent);
  const [lastValidContent, setLastValidContent] = useState(initialContent);
  const [validationStatus, setValidationStatus] = useState<BuilderValidationStatus>('idle');
  const [errorLine, setErrorLine] = useState<number | undefined>(undefined);
  const getContent = useCallback(() => content, [content]);

  useEffect(() => {
    const t = setTimeout(() => setDebouncedContent(content), 400);
    return () => clearTimeout(t);
  }, [content]);

  useEffect(() => {
    const result = tryParse(language, debouncedContent);
    if (result.ok) {
      setLastValidContent(debouncedContent);
      setErrorLine(undefined);
      setValidationStatus(debouncedContent.trim() ? 'valid' : 'idle');
    } else {
      setErrorLine(result.line);
      setValidationStatus('errors');
    }
  }, [debouncedContent, language]);

  const handleFormat = useCallback(() => {
    if (language === 'yaml') {
      try {
        setContent(yaml.dump(yaml.load(content), { indent: 2 }));
      } catch { /* ignore: invalid YAML */ }
    } else if (language === 'json') {
      try {
        setContent(JSON.stringify(JSON.parse(content), null, 2));
      } catch { /* ignore: invalid JSON */ }
    }
  }, [content, language]);

  const monacoLanguage = language === 'proto' ? 'plaintext' : language;

  return (
    <div className="flex flex-col h-full" data-testid="contract-builder-layout">
      <BuilderHeader
        contractName={contractName}
        protocol={protocol}
        validationStatus={validationStatus}
        errorLine={errorLine}
        onFormat={handleFormat}
        onSave={onSave}
        onPublish={onPublish}
        getContent={getContent}
      />

      <div className="flex-1 min-h-0">
        <SimpleSplitPane
          className="h-full"
          initialLeftPercent={45}
          minLeftPercent={25}
          minRightPercent={25}
          left={
            <MonacoEditorWrapper
              value={content}
              language={monacoLanguage}
              onChange={setContent}
            />
          }
          right={
            <div className="h-full overflow-auto p-4 bg-elevated">
              {validationStatus === 'errors' && (
                <div
                  className="mb-3 px-3 py-2 rounded text-xs text-destructive bg-destructive/10 border border-destructive/20"
                  data-testid="parse-error-banner"
                >
                  contractBuilder.preview.parseError
                  {errorLine ? ` (line ${errorLine})` : ''}
                </div>
              )}
              {renderPreview(lastValidContent)}
            </div>
          }
        />
      </div>
    </div>
  );
}
