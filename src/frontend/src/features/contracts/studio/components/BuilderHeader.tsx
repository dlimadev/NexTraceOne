import { Link } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Badge } from '../../../../components/Badge';
import { Button } from '../../../../components/Button';

export type BuilderValidationStatus = 'idle' | 'valid' | 'errors';

export interface BuilderHeaderProps {
  contractName: string;
  protocol: string;
  validationStatus: BuilderValidationStatus;
  errorLine?: number;
  onFormat?: () => void;
  /** If provided, shows the Save Draft button */
  onSave?: (content: string) => void;
  /** If provided, shows the Publish button */
  onPublish?: (content: string) => void;
  /** Returns current editor content at call time */
  getContent: () => string;
}

export function BuilderHeader({
  contractName,
  protocol,
  validationStatus,
  errorLine,
  onFormat,
  onSave,
  onPublish,
  getContent,
}: BuilderHeaderProps) {
  const { t } = useTranslation();

  const chipLabel =
    validationStatus === 'valid'
      ? t('contractBuilder.validation.valid')
      : validationStatus === 'errors'
        ? t('contractBuilder.validation.errors', { line: errorLine ?? '' })
        : null;

  const chipVariant: 'success' | 'destructive' =
    validationStatus === 'valid' ? 'success' : 'destructive';

  return (
    <div
      className="flex items-center justify-between h-11 px-4 border-b border-edge bg-card flex-shrink-0"
      data-testid="builder-header"
    >
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 min-w-0">
        <Link
          to="/contracts/studio"
          className="flex items-center gap-1 text-xs text-muted hover:text-heading transition-colors"
        >
          <ArrowLeft size={13} />
          Studio
        </Link>
        <span className="text-xs text-faded">/</span>
        <span className="text-xs font-medium text-heading truncate max-w-48">{contractName}</span>
        <Badge variant="neutral" className="text-[10px] font-mono ml-1">{protocol}</Badge>
        {chipLabel && (
          <Badge variant={chipVariant} className="text-[10px]">{chipLabel}</Badge>
        )}
      </div>

      {/* Actions */}
      <div className="flex items-center gap-2">
        {onFormat && (
          <Button size="sm" variant="ghost" onClick={onFormat} data-testid="btn-format">
            {t('contractBuilder.header.format')}
          </Button>
        )}
        {onSave && (
          <Button size="sm" variant="secondary" onClick={() => onSave(getContent())} data-testid="btn-save">
            {t('contractBuilder.header.saveDraft')}
          </Button>
        )}
        {onPublish && (
          <Button size="sm" onClick={() => onPublish(getContent())} data-testid="btn-publish">
            {t('contractBuilder.header.publish')}
          </Button>
        )}
      </div>
    </div>
  );
}
