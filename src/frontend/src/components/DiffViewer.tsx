import { useMemo } from 'react';
import { cn } from '../lib/cn';

export type DiffFormat = 'json' | 'yaml' | 'text';

interface DiffLine {
  type: 'added' | 'removed' | 'unchanged';
  content: string;
  lineNumberLeft?: number;
  lineNumberRight?: number;
}

interface DiffViewerProps {
  /** Content of the "before" version. */
  before: string;
  /** Content of the "after" version. */
  after: string;
  /** Label for the before column. */
  beforeLabel?: string;
  /** Label for the after column. */
  afterLabel?: string;
  /** Format hint (for future syntax highlighting extension). */
  format?: DiffFormat;
  /** If true, renders side-by-side; otherwise unified. */
  sideBySide?: boolean;
  /** Max visible lines before scroll. */
  maxLines?: number;
  className?: string;
}

/**
 * Performs a simple line-based LCS diff between two strings.
 * Returns a list of DiffLine records suitable for rendering.
 */
function computeDiff(before: string, after: string): DiffLine[] {
  const leftLines = before.split('\n');
  const rightLines = after.split('\n');
  const m = leftLines.length;
  const n = rightLines.length;

  // Build LCS table
  const dp: number[][] = Array.from({ length: m + 1 }, () => new Array(n + 1).fill(0));
  for (let i = 1; i <= m; i++) {
    for (let j = 1; j <= n; j++) {
      if (leftLines[i - 1] === rightLines[j - 1]) {
        dp[i][j] = dp[i - 1][j - 1] + 1;
      } else {
        dp[i][j] = Math.max(dp[i - 1][j], dp[i][j - 1]);
      }
    }
  }

  // Backtrack
  const result: DiffLine[] = [];
  let i = m;
  let j = n;
  let leftNum = m;
  let rightNum = n;

  while (i > 0 || j > 0) {
    if (i > 0 && j > 0 && leftLines[i - 1] === rightLines[j - 1]) {
      result.unshift({
        type: 'unchanged',
        content: leftLines[i - 1],
        lineNumberLeft: leftNum,
        lineNumberRight: rightNum,
      });
      i--; j--; leftNum--; rightNum--;
    } else if (j > 0 && (i === 0 || dp[i][j - 1] >= dp[i - 1][j])) {
      result.unshift({
        type: 'added',
        content: rightLines[j - 1],
        lineNumberRight: rightNum,
      });
      j--; rightNum--;
    } else {
      result.unshift({
        type: 'removed',
        content: leftLines[i - 1],
        lineNumberLeft: leftNum,
      });
      i--; leftNum--;
    }
  }

  return result;
}

const lineStyles: Record<DiffLine['type'], string> = {
  added: 'bg-success/10 text-success',
  removed: 'bg-critical/10 text-critical line-through opacity-75',
  unchanged: 'text-body',
};

const linePrefix: Record<DiffLine['type'], string> = {
  added: '+',
  removed: '-',
  unchanged: ' ',
};

/**
 * DiffViewer — renderiza diff lado a lado ou unificado de texto/JSON/YAML.
 * Usa LCS simples, adequado para payloads de contrato e schema diffs.
 *
 * @see docs/frontend-audit/frontend-prioritized-improvement-roadmap.md §F3-05
 */
export function DiffViewer({
  before,
  after,
  beforeLabel = 'Before',
  afterLabel = 'After',
  sideBySide = true,
  maxLines = 300,
  className,
}: DiffViewerProps) {
  const lines = useMemo(() => computeDiff(before, after), [before, after]);

  const addedCount = lines.filter(l => l.type === 'added').length;
  const removedCount = lines.filter(l => l.type === 'removed').length;
  const hasChanges = addedCount > 0 || removedCount > 0;

  if (!hasChanges) {
    return (
      <div className={cn('rounded-lg border border-edge bg-elevated p-4 text-sm text-muted text-center', className)}>
        No differences detected.
      </div>
    );
  }

  if (sideBySide) {
    // Split into left/right columns
    const leftLines: DiffLine[] = lines.filter(l => l.type !== 'added');
    const rightLines: DiffLine[] = lines.filter(l => l.type !== 'removed');

    return (
      <div className={cn('rounded-lg border border-edge overflow-hidden text-xs font-mono', className)}>
        {/* Header */}
        <div className="flex border-b border-edge bg-elevated">
          <div className="flex-1 px-4 py-2 text-muted font-sans text-xs font-medium border-r border-edge">
            {beforeLabel}
            <span className="ml-2 text-critical">−{removedCount}</span>
          </div>
          <div className="flex-1 px-4 py-2 text-muted font-sans text-xs font-medium">
            {afterLabel}
            <span className="ml-2 text-success">+{addedCount}</span>
          </div>
        </div>

        {/* Content */}
        <div
          className="flex overflow-auto"
          style={{ maxHeight: `${maxLines * 1.5}rem` }}
        >
          {/* Left column */}
          <div className="flex-1 border-r border-edge bg-deep min-w-0">
            {leftLines.slice(0, maxLines).map((line, idx) => (
              <div
                key={idx}
                className={cn(
                  'flex items-start gap-2 px-2 py-0.5',
                  line.type === 'removed' ? 'bg-critical/10' : '',
                )}
              >
                <span className="w-7 shrink-0 text-right text-faded select-none">
                  {line.lineNumberLeft ?? ''}
                </span>
                <span className={cn('shrink-0 w-3', line.type === 'removed' ? 'text-critical' : 'text-faded')}>
                  {line.type === 'removed' ? '-' : ' '}
                </span>
                <span className={cn('break-all whitespace-pre-wrap flex-1', line.type === 'removed' ? 'text-critical' : 'text-body')}>
                  {line.content}
                </span>
              </div>
            ))}
          </div>

          {/* Right column */}
          <div className="flex-1 bg-deep min-w-0">
            {rightLines.slice(0, maxLines).map((line, idx) => (
              <div
                key={idx}
                className={cn(
                  'flex items-start gap-2 px-2 py-0.5',
                  line.type === 'added' ? 'bg-success/10' : '',
                )}
              >
                <span className="w-7 shrink-0 text-right text-faded select-none">
                  {line.lineNumberRight ?? ''}
                </span>
                <span className={cn('shrink-0 w-3', line.type === 'added' ? 'text-success' : 'text-faded')}>
                  {line.type === 'added' ? '+' : ' '}
                </span>
                <span className={cn('break-all whitespace-pre-wrap flex-1', line.type === 'added' ? 'text-success' : 'text-body')}>
                  {line.content}
                </span>
              </div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  // Unified view
  return (
    <div className={cn('rounded-lg border border-edge overflow-hidden text-xs font-mono', className)}>
      {/* Header */}
      <div className="flex items-center gap-4 px-4 py-2 border-b border-edge bg-elevated">
        <span className="font-sans text-xs font-medium text-muted">
          {beforeLabel} → {afterLabel}
        </span>
        <span className="text-critical">−{removedCount}</span>
        <span className="text-success">+{addedCount}</span>
      </div>

      {/* Lines */}
      <div className="overflow-auto bg-deep" style={{ maxHeight: `${maxLines * 1.5}rem` }}>
        {lines.slice(0, maxLines).map((line, idx) => (
          <div
            key={idx}
            className={cn(
              'flex items-start gap-2 px-2 py-0.5',
              lineStyles[line.type],
            )}
          >
            <span className="w-7 shrink-0 text-right text-faded select-none">
              {line.lineNumberLeft ?? line.lineNumberRight ?? ''}
            </span>
            <span className="shrink-0 w-3">{linePrefix[line.type]}</span>
            <span className="break-all whitespace-pre-wrap flex-1">{line.content}</span>
          </div>
        ))}
        {lines.length > maxLines && (
          <div className="px-4 py-2 text-faded font-sans text-xs">
            … {lines.length - maxLines} more lines hidden
          </div>
        )}
      </div>
    </div>
  );
}
