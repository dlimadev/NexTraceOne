import { useTranslation } from 'react-i18next';
import { Badge } from '../../../../components/Badge';
import { cn } from '../../../../lib/cn';

export type DiffChangeKind = 'breaking' | 'non-breaking' | 'unchanged';

export interface GraphQlDiffLine {
  content: string;
  kind: DiffChangeKind;
}

export interface GraphQlDiffSide {
  label: string;
  lines: GraphQlDiffLine[];
}

interface Props {
  before: GraphQlDiffSide;
  after: GraphQlDiffSide;
  breakingCount: number;
  nonBreakingCount: number;
}

const lineClass = (kind: DiffChangeKind) => {
  switch (kind) {
    case 'breaking': return 'bg-red-50 text-red-800 dark:bg-red-950/30 dark:text-red-200';
    case 'non-breaking': return 'bg-green-50 text-green-800 dark:bg-green-950/30 dark:text-green-200';
    default: return 'text-foreground';
  }
};

/**
 * GraphQlSchemaDiffViewer — diff side-by-side entre dois snapshots SDL com highlighting de
 * breaking changes (vermelho), non-breaking additions (verde) e unchanged (normal).
 *
 * @see docs/FUTURE-ROADMAP.md Wave X.2
 */
export function GraphQlSchemaDiffViewer({ before, after, breakingCount, nonBreakingCount }: Props) {
  const { t } = useTranslation();

  return (
    <div className="flex flex-col h-full" data-testid="graphql-schema-diff-viewer">
      {/* Summary row */}
      <div className="flex items-center gap-3 mb-3 flex-wrap">
        <Badge variant="danger" size="sm">
          {breakingCount} {t('graphqlDiffViewer.breakingChanges')}
        </Badge>
        <Badge variant="success" size="sm">
          {nonBreakingCount} {t('graphqlDiffViewer.nonBreakingChanges')}
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
                  className={cn('px-3 py-0.5 whitespace-pre-wrap break-all', lineClass(line.kind))}
                >
                  {line.content || '\u00A0'}
                </div>
              ))}
              {side.lines.length === 0 && (
                <p className="text-xs text-muted-foreground p-3">{t('graphqlDiffViewer.noSnapshots')}</p>
              )}
            </div>
          </div>
        ))}
      </div>

      {breakingCount === 0 && nonBreakingCount === 0 && (
        <p className="text-xs text-green-600 mt-2 text-center">{t('graphqlDiffViewer.noBreakingChanges')}</p>
      )}
    </div>
  );
}
