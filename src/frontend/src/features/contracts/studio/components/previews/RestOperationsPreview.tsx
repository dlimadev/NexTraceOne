import * as yaml from 'js-yaml';
import { useTranslation } from 'react-i18next';
import { Badge } from '../../../../../components/Badge';

interface ParsedOperation {
  method: string;
  summary: string;
}

type PathsMap = Record<string, ParsedOperation[]>;

const HTTP_METHODS = ['get', 'post', 'put', 'patch', 'delete', 'head', 'options'];

const METHOD_VARIANT: Record<string, 'success' | 'default' | 'warning' | 'info' | 'danger' | 'neutral'> = {
  GET: 'success',
  POST: 'default',
  PUT: 'warning',
  PATCH: 'info',
  DELETE: 'danger',
  HEAD: 'neutral',
  OPTIONS: 'neutral',
};

function parsePaths(content: string): PathsMap | null {
  try {
    const doc = yaml.load(content) as {
      paths?: Record<string, Record<string, { summary?: string }>>;
    };
    if (!doc?.paths || typeof doc.paths !== 'object') return null;
    const result: PathsMap = {};
    for (const [path, methods] of Object.entries(doc.paths)) {
      if (!methods || typeof methods !== 'object') continue;
      const ops = Object.entries(methods)
        .filter(([m]) => HTTP_METHODS.includes(m.toLowerCase()))
        .map(([m, op]) => ({
          method: m.toUpperCase(),
          summary: (op as { summary?: string })?.summary ?? '',
        }));
      if (ops.length > 0) result[path] = ops;
    }
    return Object.keys(result).length > 0 ? result : null;
  } catch {
    return null;
  }
}

interface RestOperationsPreviewProps {
  content: string;
}

export function RestOperationsPreview({ content }: RestOperationsPreviewProps) {
  const { t } = useTranslation();
  const paths = parsePaths(content);

  if (!paths) {
    return (
      <div className="flex flex-col items-center justify-center h-32 text-muted" data-testid="preview-empty">
        <span className="text-xs">{t('contractBuilder.preview.empty')}</span>
      </div>
    );
  }

  const totalOps = Object.values(paths).reduce((sum, ops) => sum + ops.length, 0);

  return (
    <div className="space-y-3" data-testid="rest-operations-preview">
      {Object.entries(paths).map(([path, ops]) => (
        <div key={path}>
          <div className="text-xs font-mono font-semibold text-heading mb-1.5">{path}</div>
          <div className="space-y-1 pl-3">
            {ops.map((op) => (
              <div key={op.method} className="flex items-center gap-2">
                <Badge
                  variant={METHOD_VARIANT[op.method] ?? 'default'}
                  className="text-[10px] font-mono w-16 justify-center flex-shrink-0"
                >
                  {op.method}
                </Badge>
                <span className="text-xs text-muted truncate">{op.summary}</span>
              </div>
            ))}
          </div>
        </div>
      ))}

      <div className="pt-3 border-t border-edge text-xs text-faded">
        {Object.keys(paths).length} paths · {totalOps} operations
      </div>
    </div>
  );
}
