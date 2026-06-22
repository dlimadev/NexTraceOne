import { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { ChevronLeft } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import * as yaml from 'js-yaml';
import SimpleSplitPane from '../../../components/SimpleSplitPane';
import { MonacoEditorWrapper } from '../workspace/editor/MonacoEditorWrapper';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';

export type BuilderLanguage = 'yaml' | 'json' | 'graphql' | 'proto' | 'xml';
export type BuilderValidationStatus = 'idle' | 'valid' | 'errors';

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
  const { t } = useTranslation();
  const [content, setContent] = useState(initialContent);
  const [debouncedContent, setDebouncedContent] = useState(initialContent);
  const [lastValidContent, setLastValidContent] = useState(initialContent);
  const [validationStatus, setValidationStatus] = useState<BuilderValidationStatus>('idle');
  const [errorLine, setErrorLine] = useState<number | undefined>(undefined);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedContent(content), 400);
    return () => clearTimeout(timer);
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

  const chipLabel =
    validationStatus === 'valid'
      ? t('contractBuilder.validation.valid')
      : validationStatus === 'errors'
        ? t('contractBuilder.validation.errors', { line: errorLine ?? '' })
        : null;
  const chipVariant: 'success' | 'danger' = validationStatus === 'valid' ? 'success' : 'danger';

  return (
    <PageContainer className="animate-fade-in">
      <div data-testid="contract-builder-layout">
        <div className="mb-4">
          <Link
            to="/contracts/studio"
            className="inline-flex items-center gap-1.5 text-sm text-muted hover:text-heading transition-colors"
          >
            <ChevronLeft size={14} /> {t('contractBuilder.header.backToStudio', 'Studio')}
          </Link>
        </div>

        <PageHeader
          title={contractName}
          subtitle={protocol}
          badge={
            chipLabel ? (
              <Badge variant={chipVariant} className="text-[10px]">{chipLabel}</Badge>
            ) : undefined
          }
          actions={
            <>
              <Button size="sm" variant="ghost" onClick={handleFormat} data-testid="btn-format">
                {t('contractBuilder.header.format')}
              </Button>
              {onSave && (
                <Button
                  size="sm"
                  variant="secondary"
                  onClick={() => onSave(content)}
                  data-testid="btn-save"
                >
                  {t('contractBuilder.header.saveDraft')}
                </Button>
              )}
              {onPublish && (
                <Button size="sm" onClick={() => onPublish(content)} data-testid="btn-publish">
                  {t('contractBuilder.header.publish')}
                </Button>
              )}
            </>
          }
        />

        <div className="h-[60vh] min-h-[420px] border border-edge rounded-lg overflow-hidden">
          <SimpleSplitPane
            className="h-full"
            initialLeftPercent={45}
            minLeftPercent={25}
            minRightPercent={25}
            left={
              <MonacoEditorWrapper value={content} language={monacoLanguage} onChange={setContent} />
            }
            right={
              <div className="h-full overflow-auto p-4 bg-elevated">
                {validationStatus === 'errors' && (
                  <div
                    className="mb-3 px-3 py-2 rounded text-xs text-critical bg-critical/10 border border-critical/25"
                    data-testid="parse-error-banner"
                  >
                    {t('contractBuilder.preview.parseError')}
                    {errorLine ? ` (line ${errorLine})` : ''}
                  </div>
                )}
                {renderPreview(lastValidContent)}
              </div>
            }
          />
        </div>
      </div>
    </PageContainer>
  );
}
