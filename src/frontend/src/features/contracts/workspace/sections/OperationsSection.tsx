import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus, ChevronDown, ChevronRight, Trash2, Search } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import { EmptyState } from '../../../../components/EmptyState';
import { METHOD_COLORS as NTO_METHOD_COLORS } from '../../shared/constants';

// ── Local types ───────────────────────────────────────────────────────────────

interface OperationItem {
  id: string;
  method: string;
  path: string;
  operationId: string;
  summary: string;
  deprecated: boolean;
  tags: string[];
}

interface OperationsSectionProps {
  /** Raw spec content from which operations are parsed. */
  specContent: string;
  protocol: string;
  isReadOnly?: boolean;
  /** Called when the user requests to add an operation (navigates to spec editor). */
  onAddOperation?: () => void;
  className?: string;
}

const METHOD_COLORS: Record<string, string> = {
  ...NTO_METHOD_COLORS,
  // SOAP / Event / Workservice — NTO tokens
  OPERATION: 'bg-accent/15 text-accent border border-accent/25',
  PUBLISH: 'bg-cyan/15 text-cyan border border-cyan/25',
  SUBSCRIBE: 'bg-mint/15 text-mint border border-mint/25',
  TRIGGER: 'bg-warning/15 text-warning border border-warning/25',
};

/**
 * Secção de operações/endpoints do workspace.
 * Apresenta as operações extraídas do spec content de forma visual,
 * com pesquisa, filtros e agrupamento por tags.
 */
export function OperationsSection({ specContent, protocol, isReadOnly = false, onAddOperation, className = '' }: OperationsSectionProps) {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const operations = useMemo(() => parseOperations(specContent, protocol), [specContent, protocol]);

  const filtered = useMemo(() => {
    if (!search.trim()) return operations;
    const q = search.toLowerCase();
    return operations.filter(
      (op) =>
        op.path.toLowerCase().includes(q) ||
        op.operationId.toLowerCase().includes(q) ||
        op.summary.toLowerCase().includes(q) ||
        op.method.toLowerCase().includes(q),
    );
  }, [operations, search]);

  const tagGroups = useMemo(() => {
    const groups = new Map<string, OperationItem[]>();
    for (const op of filtered) {
      const tag = op.tags[0] || t('contracts.operations.untagged', 'Untagged');
      if (!groups.has(tag)) groups.set(tag, []);
      groups.get(tag)!.push(op);
    }
    return groups;
  }, [filtered, t]);

  if (operations.length === 0) {
    return (
      <div className={className}>
        <EmptyState
          title={t('contracts.operations.emptyTitle', 'No operations found')}
          description={t('contracts.operations.emptyDescription', 'Operations will appear here once the contract specification is defined.')}
        />
      </div>
    );
  }

  return (
    <div className={`space-y-4 ${className}`}>
      {/* Header bar */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <h3 className="text-xs font-semibold text-heading">
            {t('contracts.operations.title', 'Operations')}
          </h3>
          <span className="text-[10px] text-muted px-2 py-0.5 rounded-full bg-elevated border border-edge">
            {operations.length}
          </span>
        </div>

        <div className="flex items-center gap-2">
          {/* Search */}
          <div className="relative">
            <Search size={12} className="absolute left-2 top-1/2 -translate-y-1/2 text-muted" />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder={t('contracts.operations.searchPlaceholder', 'Search operations...')}
              className="text-xs bg-elevated border border-edge rounded pl-7 pr-2 py-1.5 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent w-52"
            />
          </div>

          {!isReadOnly && (
            <button
              onClick={onAddOperation}
              className="inline-flex items-center gap-1 px-2.5 py-1.5 text-[10px] font-medium rounded bg-accent text-white hover:bg-accent/90 transition-colors"
            >
              <Plus size={11} />
              {t('contracts.operations.add', 'Add Operation')}
            </button>
          )}
        </div>
      </div>

      {/* Grouped operation list */}
      {Array.from(tagGroups.entries()).map(([tag, ops]) => (
        <Card key={tag}>
          <CardHeader className="py-2 px-4">
            <span className="text-[10px] font-semibold text-muted uppercase tracking-wider">{tag}</span>
          </CardHeader>
          <CardBody className="p-0 divide-y divide-edge">
            {ops.map((op) => (
              <OperationRow
                key={op.id}
                operation={op}
                isExpanded={expandedId === op.id}
                onToggle={() => setExpandedId(expandedId === op.id ? null : op.id)}
                isReadOnly={isReadOnly}
              />
            ))}
          </CardBody>
        </Card>
      ))}
    </div>
  );
}

// ── Operation Row ─────────────────────────────────────────────────────────────

function OperationRow({
  operation,
  isExpanded,
  onToggle,
  isReadOnly,
}: {
  operation: OperationItem;
  isExpanded: boolean;
  onToggle: () => void;
  isReadOnly: boolean;
}) {
  const { t } = useTranslation();
  const colors = METHOD_COLORS[operation.method.toUpperCase()] ?? METHOD_COLORS.GET;

  return (
    <div>
      <button onClick={onToggle} className="w-full flex items-center gap-3 px-4 py-2.5 text-left hover:bg-elevated/50 transition-colors">
        {isExpanded ? <ChevronDown size={12} className="text-muted flex-shrink-0" /> : <ChevronRight size={12} className="text-muted flex-shrink-0" />}

        <span className={`inline-flex items-center justify-center px-2 py-0.5 text-[10px] font-bold uppercase rounded border ${colors} min-w-[52px] text-center`}>
          {operation.method}
        </span>

        <span className="text-xs font-mono text-body flex-1 truncate">{operation.path}</span>

        {operation.deprecated && (
          <span className="text-[10px] text-warning px-1.5 py-0.5 rounded bg-warning/15 border border-warning/25">
            {t('contracts.deprecated', 'Deprecated')}
          </span>
        )}

        <span className="text-[10px] text-muted truncate max-w-[180px]">{operation.summary}</span>
      </button>

      {isExpanded && (
        <div className="px-4 pb-3 pl-12 space-y-2 bg-panel/50">
          <dl className="grid grid-cols-2 gap-x-6 gap-y-1 text-xs">
            <div>
              <dt className="text-[10px] text-muted">{t('contracts.operations.operationId', 'Operation ID')}</dt>
              <dd className="text-body font-mono">{operation.operationId || '-'}</dd>
            </div>
            <div>
              <dt className="text-[10px] text-muted">{t('contracts.operations.method', 'Method')}</dt>
              <dd className="text-body">{operation.method}</dd>
            </div>
            <div className="col-span-2">
              <dt className="text-[10px] text-muted">{t('contracts.operations.summary', 'Summary')}</dt>
              <dd className="text-body">{operation.summary || '-'}</dd>
            </div>
          </dl>

          {operation.tags.length > 0 && (
            <div className="flex items-center gap-1 flex-wrap">
              <span className="text-[10px] text-muted">{t('contracts.operations.tags', 'Tags')}:</span>
              {operation.tags.map((tag) => (
                <span key={tag} className="text-[10px] px-1.5 py-0.5 rounded bg-elevated border border-edge text-muted">{tag}</span>
              ))}
            </div>
          )}

          {!isReadOnly && (
            <div className="flex items-center gap-2 pt-1 border-t border-edge mt-1">
              <button className="text-[10px] text-critical hover:text-critical inline-flex items-center gap-1">
                <Trash2 size={10} /> {t('common.remove', 'Remove')}
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

// ── Parser ────────────────────────────────────────────────────────────────────

/**
 * Lightweight parser that extracts operations from raw spec content.
 * Supports JSON-based OpenAPI, basic YAML path detection, and returns
 * placeholder entries for SOAP/AsyncAPI/Workservice specs.
 */
function parseOperations(specContent: string, protocol: string): OperationItem[] {
  if (!specContent?.trim()) return [];

  try {
    // Attempt JSON parse for OpenAPI / Swagger / AsyncAPI
    const trimmed = specContent.trim();
    if (trimmed.startsWith('{') || trimmed.startsWith('[')) {
      const parsed = JSON.parse(trimmed);
      if (protocol === 'AsyncApi' && parsed.channels) {
        return parseAsyncApiChannels(parsed);
      }
      if (parsed.paths) {
        return parseOpenApiPaths(parsed.paths);
      }
    }
  } catch {
    // Fall through to empty
  }

  // YAML path heuristic for OpenAPI
  if (protocol === 'OpenApi' || protocol === 'Swagger') {
    return parseYamlPathsHeuristic(specContent);
  }

  return [];
}

function parseOpenApiPaths(paths: Record<string, Record<string, unknown>>): OperationItem[] {
  const ops: OperationItem[] = [];
  const methods = ['get', 'post', 'put', 'patch', 'delete', 'head', 'options'];

  for (const [path, pathObj] of Object.entries(paths)) {
    if (!pathObj || typeof pathObj !== 'object') continue;
    for (const method of methods) {
      const opDef = pathObj[method];
      if (!opDef || typeof opDef !== 'object') continue;
      const op = opDef as Record<string, unknown>;
      ops.push({
        id: `${method}-${path}`,
        method: method.toUpperCase(),
        path,
        operationId: (op.operationId as string) ?? '',
        summary: (op.summary as string) ?? '',
        deprecated: (op.deprecated as boolean) ?? false,
        tags: Array.isArray(op.tags) ? (op.tags as string[]) : [],
      });
    }
  }
  return ops;
}

function parseAsyncApiChannels(parsed: Record<string, unknown>): OperationItem[] {
  const channels = parsed.channels as Record<string, Record<string, unknown>> | undefined;
  if (!channels) return [];
  const ops: OperationItem[] = [];
  for (const [channel, def] of Object.entries(channels)) {
    if (def.publish) {
      ops.push({
        id: `publish-${channel}`,
        method: 'PUBLISH',
        path: channel,
        operationId: ((def.publish as Record<string, unknown>).operationId as string) ?? '',
        summary: ((def.publish as Record<string, unknown>).summary as string) ?? '',
        deprecated: false,
        tags: [],
      });
    }
    if (def.subscribe) {
      ops.push({
        id: `subscribe-${channel}`,
        method: 'SUBSCRIBE',
        path: channel,
        operationId: ((def.subscribe as Record<string, unknown>).operationId as string) ?? '',
        summary: ((def.subscribe as Record<string, unknown>).summary as string) ?? '',
        deprecated: false,
        tags: [],
      });
    }
  }
  return ops;
}

function parseYamlPathsHeuristic(content: string): OperationItem[] {
  const ops: OperationItem[] = [];
  const lines = content.split('\n');
  let currentPath = '';
  const methods = ['get', 'post', 'put', 'patch', 'delete', 'head', 'options'];

  for (const line of lines) {
    // Detect path lines (e.g. "  /users:")
    const pathMatch = line.match(/^ {2}(\/[^\s:]+):\s*$/);
    if (pathMatch) {
      const pathValue = pathMatch[1];
      if (!pathValue) {
        continue;
      }

      currentPath = pathValue;
      continue;
    }
    // Detect method lines (e.g. "    get:")
    if (currentPath) {
      const methodMatch = line.match(/^\s{4}(\w+):\s*$/);
      const method = methodMatch?.[1]?.toLowerCase();
      if (method && methods.includes(method)) {
        ops.push({
          id: `${method}-${currentPath}`,
          method: method.toUpperCase(),
          path: currentPath,
          operationId: '',
          summary: '',
          deprecated: false,
          tags: [],
        });
      }
    }
  }
  return ops;
}
