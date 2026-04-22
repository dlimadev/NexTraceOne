import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Search, ChevronRight } from 'lucide-react';
import { Badge } from '../../../../components/Badge';
import { cn } from '../../../../lib/cn';

export interface ProtobufElementItem {
  name: string;
  kind: 'MESSAGE' | 'ENUM' | 'SERVICE' | 'RPC' | 'FIELD';
  parent?: string;
  fieldCount?: number;
  isDeprecated?: boolean;
}

interface Props {
  items: ProtobufElementItem[];
  onSelect?: (item: ProtobufElementItem) => void;
}

const kindBadge = (kind: ProtobufElementItem['kind']): 'info' | 'success' | 'warning' | 'default' => {
  switch (kind) {
    case 'MESSAGE': return 'info';
    case 'SERVICE': return 'success';
    case 'RPC': return 'warning';
    case 'ENUM': return 'default';
    default: return 'default';
  }
};

/**
 * ProtobufSchemaExplorer — explorador de messages, fields, services e RPCs
 * com indicação de deprecated fields. Usado no Contract Studio (Wave X.2).
 *
 * @see docs/FUTURE-ROADMAP.md Wave X.2
 */
export function ProtobufSchemaExplorer({ items, onSelect }: Props) {
  const { t } = useTranslation();
  const [filter, setFilter] = useState('');

  const filtered = filter.trim()
    ? items.filter((i) => i.name.toLowerCase().includes(filter.toLowerCase()))
    : items;

  const grouped = filtered.reduce<Record<string, ProtobufElementItem[]>>((acc, item) => {
    const group = item.kind === 'FIELD' ? (item.parent ?? 'Fields') : `${item.kind}s`;
    (acc[group] ??= []).push(item);
    return acc;
  }, {});

  return (
    <div className="flex flex-col h-full" data-testid="protobuf-schema-explorer">
      {/* Search bar */}
      <div className="relative mb-3">
        <Search className="absolute left-2 top-2.5 h-3.5 w-3.5 text-muted-foreground" />
        <input
          type="text"
          className="w-full pl-7 pr-3 py-2 text-xs border border-border rounded bg-background"
          placeholder={t('protobufDiffViewer.filter')}
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
          aria-label={t('protobufDiffViewer.filter')}
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
                  key={`${item.kind}-${item.name}`}
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
                    {item.isDeprecated && (
                      <Badge variant="warning" size="sm">{t('protobufDiffViewer.deprecatedField')}</Badge>
                    )}
                  </div>
                  <Badge variant={kindBadge(item.kind)} size="sm">{item.kind}</Badge>
                </button>
              ))}
            </div>
          </div>
        ))}
        {filtered.length === 0 && (
          <p className="text-xs text-muted-foreground text-center py-4">{t('protobufDiffViewer.noSnapshots')}</p>
        )}
      </div>
    </div>
  );
}
