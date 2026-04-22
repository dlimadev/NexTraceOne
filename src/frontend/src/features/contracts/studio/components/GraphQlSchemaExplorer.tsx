import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Search, ChevronRight } from 'lucide-react';
import { Badge } from '../../../../components/Badge';
import { cn } from '../../../../lib/cn';

export interface GraphQlTypeItem {
  name: string;
  kind: 'OBJECT' | 'INTERFACE' | 'UNION' | 'ENUM' | 'INPUT' | 'SCALAR' | 'QUERY' | 'MUTATION' | 'SUBSCRIPTION';
  fieldCount: number;
  isDeprecated?: boolean;
}

interface Props {
  /** Lista de tipos/operations do schema SDL parseado. */
  items: GraphQlTypeItem[];
  /** Callback quando um tipo é selecionado. */
  onSelect?: (item: GraphQlTypeItem) => void;
}

const kindBadge = (kind: GraphQlTypeItem['kind']) => {
  const map: Record<GraphQlTypeItem['kind'], 'info' | 'success' | 'warning' | 'danger' | 'default'> = {
    OBJECT: 'info',
    INTERFACE: 'info',
    UNION: 'default',
    ENUM: 'warning',
    INPUT: 'default',
    SCALAR: 'default',
    QUERY: 'success',
    MUTATION: 'warning',
    SUBSCRIPTION: 'warning',
  };
  return map[kind] ?? 'default';
};

/**
 * GraphQlSchemaExplorer — explorador de tipos, fields, queries, mutations e subscriptions
 * com filtro e pesquisa. Usado no Contract Studio (Wave X.2).
 *
 * @see docs/FUTURE-ROADMAP.md Wave X.2
 */
export function GraphQlSchemaExplorer({ items, onSelect }: Props) {
  const { t } = useTranslation();
  const [filter, setFilter] = useState('');

  const filtered = filter.trim()
    ? items.filter((i) => i.name.toLowerCase().includes(filter.toLowerCase()))
    : items;

  const grouped = filtered.reduce<Record<string, GraphQlTypeItem[]>>((acc, item) => {
    const group = ['QUERY', 'MUTATION', 'SUBSCRIPTION'].includes(item.kind)
      ? 'Operations'
      : 'Types';
    (acc[group] ??= []).push(item);
    return acc;
  }, {});

  return (
    <div className="flex flex-col h-full" data-testid="graphql-schema-explorer">
      {/* Search bar */}
      <div className="relative mb-3">
        <Search className="absolute left-2 top-2.5 h-3.5 w-3.5 text-muted-foreground" />
        <input
          type="text"
          className="w-full pl-7 pr-3 py-2 text-xs border border-border rounded bg-background"
          placeholder={t('graphqlDiffViewer.filter')}
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
          aria-label={t('graphqlDiffViewer.filter')}
        />
      </div>

      {/* Groups */}
      <div className="flex-1 overflow-y-auto space-y-3">
        {Object.entries(grouped).map(([group, groupItems]) => (
          <div key={group}>
            <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-1">{group}</p>
            <div className="space-y-1">
              {groupItems.map((item) => (
                <button
                  key={item.name}
                  type="button"
                  className={cn(
                    'w-full flex items-center justify-between text-left px-2 py-1.5 rounded text-xs',
                    'hover:bg-muted/60 transition-colors',
                    item.isDeprecated && 'opacity-60',
                  )}
                  onClick={() => onSelect?.(item)}
                >
                  <div className="flex items-center gap-2 min-w-0">
                    <ChevronRight className="h-3 w-3 shrink-0 text-muted-foreground" />
                    <span className="font-mono truncate">{item.name}</span>
                    {item.isDeprecated && <Badge variant="default" size="sm">deprecated</Badge>}
                  </div>
                  <div className="flex items-center gap-2 shrink-0 ml-2">
                    <Badge variant={kindBadge(item.kind)} size="sm">{item.kind}</Badge>
                    <span className="text-muted-foreground">{item.fieldCount}f</span>
                  </div>
                </button>
              ))}
            </div>
          </div>
        ))}
        {filtered.length === 0 && (
          <p className="text-xs text-muted-foreground text-center py-4">{t('graphqlDiffViewer.noSnapshots')}</p>
        )}
      </div>
    </div>
  );
}
