import { useTranslation } from 'react-i18next';
import { Badge } from '../../../../components/Badge';
import { cn } from '../../../../lib/cn';

export type ProtobufDiffChangeKind = 'breaking' | 'non-breaking' | 'unchanged';

export interface ProtobufDiffLine {
  content: string;
  kind: ProtobufDiffChangeKind;
  changeType?: 'message' | 'field' | 'service' | 'rpc';
}

export interface ProtobufDiffSide {
  label: string;
  lines: ProtobufDiffLine[];
}

interface Props {
  before: ProtobufDiffSide;
  after: ProtobufDiffSide;
  breakingCount: number;
  nonBreakingCount: number;
}

const lineClass = (kind: ProtobufDiffChangeKind) => {
  switch (kind) {
    case 'breaking': return 'bg-red-50 text-red-800 dark:bg-red-950/30 dark:text-red-200';
    case 'non-breaking': return 'bg-green-50 text-green-800 dark:bg-green-950/30 dark:text-green-200';
    default: return 'text-foreground';
  }
};

const changeTypeLabel = (changeType?: string) => {
  if (!changeType) return null;
  const labels: Record<string, string> = {
    message: 'MSG',
    field: 'FLD',
    service: 'SVC',
    rpc: 'RPC',
  };
  return labels[changeType] ?? changeType.toUpperCase();
};

/**
 * ProtobufSchemaDiffViewer — diff side-by-side de .proto com highlighting por tipo de change
 * (message, field, service, rpc). Usado no Contract Studio (Wave X.2).
 *
 * @see docs/FUTURE-ROADMAP.md Wave X.2
 */
export function ProtobufSchemaDiffViewer({ before, after, breakingCount, nonBreakingCount }: Props) {
  const { t } = useTranslation();

  return (
    <div className="flex flex-col h-full" data-testid="protobuf-schema-diff-viewer">
      {/* Summary */}
      <div className="flex items-center gap-3 mb-3 flex-wrap">
        <Badge variant="danger" size="sm">
          {breakingCount} {t('protobufDiffViewer.breakingChanges')}
        </Badge>
        <Badge variant="success" size="sm">
          {nonBreakingCount} {t('protobufDiffViewer.nonBreakingChanges')}
        </Badge>
      </div>

      {/* Side-by-side diff */}
      <div className="grid grid-cols-2 gap-2 flex-1 min-h-0">
        {[before, after].map((side) => (
          <div key={side.label} className="flex flex-col min-h-0">
            <p className="text-xs font-semibold text-muted-foreground mb-1 px-2">{side.label}</p>
            <div className="flex-1 overflow-y-auto rounded border border-border bg-muted/30 font-mono text-xs">
              {side.lines.map((line, idx) => (
                <div
                  // eslint-disable-next-line react/no-array-index-key
                  key={idx}
                  className={cn('px-3 py-0.5 flex items-start gap-2 whitespace-pre-wrap break-all', lineClass(line.kind))}
                >
                  {line.changeType && (
                    <span className="shrink-0 text-xs opacity-60 font-bold">
                      [{changeTypeLabel(line.changeType)}]
                    </span>
                  )}
                  <span>{line.content || '\u00A0'}</span>
                </div>
              ))}
              {side.lines.length === 0 && (
                <p className="text-xs text-muted-foreground p-3">{t('protobufDiffViewer.noSnapshots')}</p>
              )}
            </div>
          </div>
        ))}
      </div>

      {breakingCount === 0 && nonBreakingCount === 0 && (
        <p className="text-xs text-green-600 mt-2 text-center">{t('protobufDiffViewer.noBreakingChanges')}</p>
      )}
    </div>
  );
}
