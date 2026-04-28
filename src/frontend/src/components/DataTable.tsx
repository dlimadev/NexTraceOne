import { useState, useMemo, useCallback, useId, type ReactNode, type KeyboardEvent } from 'react';
import { ArrowUpDown, ArrowUp, ArrowDown } from 'lucide-react';
import { cn } from '../lib/cn';
import { EmptyState } from './EmptyState';

/* ─── Types ─────────────────────────────────────────────────────────────────── */

export interface DataTableColumn<T> {
  /** ID único da coluna. */
  id: string;
  /** Header da coluna. */
  header: string;
  /** Função que extrai o valor da célula. */
  accessor: (row: T) => ReactNode;
  /** Função que retorna valor sortable (string/number). */
  sortValue?: (row: T) => string | number;
  /** Largura da coluna (CSS string). */
  width?: string;
  /** Alinhamento do conteúdo. */
  align?: 'left' | 'center' | 'right';
  /** Se a coluna pode ser ordenada. */
  sortable?: boolean;
}

export type SortDirection = 'asc' | 'desc';

export interface SortState {
  columnId: string;
  direction: SortDirection;
}

interface DataTableProps<T> {
  /** Definição das colunas. */
  columns: DataTableColumn<T>[];
  /** Dados da tabela. */
  data: T[];
  /** Extrai a key de cada row. */
  rowKey: (row: T) => string | number;
  /** Estado de loading (exibe skeleton rows). */
  loading?: boolean;
  /** Número de skeleton rows no loading. */
  skeletonRows?: number;
  /** Callback ao clicar numa row. */
  onRowClick?: (row: T) => void;
  /** Sort controlado externamente. */
  sort?: SortState | null;
  /** Callback ao mudar sort. */
  onSortChange?: (sort: SortState | null) => void;
  /** Selecção de rows ativa. */
  selectable?: boolean;
  /** IDs dos rows selecionados. */
  selectedKeys?: Set<string | number>;
  /** Callback ao mudar seleção. */
  onSelectionChange?: (keys: Set<string | number>) => void;
  /** Mensagem quando não há dados. */
  emptyTitle?: string;
  /** Descrição da mensagem vazia. */
  emptyDescription?: string;
  /** Ação vazia customizada. */
  emptyAction?: ReactNode;
  /** Sticky header. */
  stickyHeader?: boolean;
  className?: string;
}

/**
 * DataTable enterprise com sorting, row selection, loading state e empty state.
 *
 * Tipagem forte para colunas e dados via generics.
 * Sorting client-side integrado (ou controlado externamente).
 *
 * @see docs/DESIGN-SYSTEM.md
 */
export function DataTable<T>({
  columns,
  data,
  rowKey,
  loading = false,
  skeletonRows = 5,
  onRowClick,
  sort,
  onSortChange,
  selectable = false,
  selectedKeys,
  onSelectionChange,
  emptyTitle,
  emptyDescription,
  emptyAction,
  stickyHeader = false,
  className,
}: DataTableProps<T>) {
  const [internalSort, setInternalSort] = useState<SortState | null>(null);
  const tableId = useId();

  const currentSort = sort !== undefined ? sort : internalSort;
  const handleSort = onSortChange ?? setInternalSort;

  const toggleSort = useCallback(
    (columnId: string) => {
      if (currentSort?.columnId === columnId) {
        if (currentSort.direction === 'asc') {
          handleSort({ columnId, direction: 'desc' });
        } else {
          handleSort(null);
        }
      } else {
        handleSort({ columnId, direction: 'asc' });
      }
    },
    [currentSort, handleSort],
  );

  const sortedData = useMemo(() => {
    if (!currentSort) return data;
    const col = columns.find((c) => c.id === currentSort.columnId);
    if (!col?.sortValue) return data;

    return [...data].sort((a, b) => {
      const aVal = col.sortValue!(a);
      const bVal = col.sortValue!(b);
      const cmp = aVal < bVal ? -1 : aVal > bVal ? 1 : 0;
      return currentSort.direction === 'asc' ? cmp : -cmp;
    });
  }, [data, currentSort, columns]);

  const allSelected = selectable && selectedKeys && data.length > 0 && data.every((r) => selectedKeys.has(rowKey(r)));

  const toggleAll = useCallback(() => {
    if (!onSelectionChange) return;
    if (allSelected) {
      onSelectionChange(new Set());
    } else {
      onSelectionChange(new Set(data.map(rowKey)));
    }
  }, [allSelected, data, rowKey, onSelectionChange]);

  const toggleRow = useCallback(
    (key: string | number) => {
      if (!onSelectionChange || !selectedKeys) return;
      const next = new Set(selectedKeys);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      onSelectionChange(next);
    },
    [selectedKeys, onSelectionChange],
  );

  const alignClass = (align?: 'left' | 'center' | 'right') => {
    if (align === 'center') return 'text-center';
    if (align === 'right') return 'text-right';
    return 'text-left';
  };

  const SortIcon = ({ columnId }: { columnId: string }) => {
    if (currentSort?.columnId !== columnId) return <ArrowUpDown size={14} className="text-faded" />;
    return currentSort.direction === 'asc'
      ? <ArrowUp size={14} className="text-cyan" />
      : <ArrowDown size={14} className="text-cyan" />;
  };

  if (!loading && data.length === 0) {
    return (
      <div className={cn('bg-card rounded-2xl border border-edge shadow-surface overflow-hidden', className)}>
        <EmptyState
          title={emptyTitle ?? ''}
          description={emptyDescription}
          action={emptyAction}
        />
      </div>
    );
  }

  return (
    <div className={cn('bg-card rounded-2xl border border-edge shadow-surface overflow-hidden', className)}>
      <div className="overflow-x-auto">
        <table className="w-full text-sm" role="grid" aria-labelledby={`${tableId}-caption`}>
          <thead className={cn(stickyHeader && 'sticky top-0 z-10')}>
            <tr className="border-b border-edge bg-elevated/50">
              {selectable && (
                <th className="w-10 px-3 py-3">
                  <input
                    type="checkbox"
                    checked={allSelected}
                    onChange={toggleAll}
                    aria-label="Select all rows"
                    className="rounded border-edge"
                  />
                </th>
              )}
              {columns.map((col) => (
                <th
                  key={col.id}
                  className={cn(
                    'px-4 py-3 text-xs font-semibold text-muted uppercase tracking-wide',
                    alignClass(col.align),
                    col.sortable !== false && col.sortValue && 'cursor-pointer select-none hover:text-body',
                  )}
                  style={col.width ? { width: col.width } : undefined}
                  onClick={col.sortable !== false && col.sortValue ? () => toggleSort(col.id) : undefined}
                  aria-sort={
                    currentSort?.columnId === col.id
                      ? currentSort.direction === 'asc' ? 'ascending' : 'descending'
                      : undefined
                  }
                >
                  <span className="inline-flex items-center gap-1.5">
                    {col.header}
                    {col.sortable !== false && col.sortValue && <SortIcon columnId={col.id} />}
                  </span>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {loading
              ? Array.from({ length: skeletonRows }).map((_, i) => (
                  <tr key={`skeleton-${i}`} className="border-b border-edge/40">
                    {selectable && (
                      <td className="px-3 py-3">
                        <div className="skeleton h-4 w-4 rounded" />
                      </td>
                    )}
                    {columns.map((col) => (
                      <td key={col.id} className="px-4 py-3">
                        <div className="skeleton h-4 w-3/4 rounded" />
                      </td>
                    ))}
                  </tr>
                ))
              : sortedData.map((row) => {
                  const key = rowKey(row);
                  const isSelected = selectedKeys?.has(key);
                  return (
                    <tr
                      key={key}
                      className={cn(
                        'border-b border-edge/40 transition-colors',
                        onRowClick && 'cursor-pointer hover:bg-hover',
                        isSelected && 'bg-accent-muted',
                      )}
                      onClick={() => onRowClick?.(row)}
                      role={onRowClick ? 'button' : undefined}
                      tabIndex={onRowClick ? 0 : undefined}
                      onKeyDown={(e: KeyboardEvent<HTMLTableRowElement>) => {
                        if (onRowClick && (e.key === 'Enter' || e.key === ' ')) {
                          e.preventDefault();
                          onRowClick(row);
                        }
                      }}
                    >
                      {selectable && (
                        <td className="px-3 py-3" onClick={(e) => e.stopPropagation()}>
                          <input
                            type="checkbox"
                            checked={isSelected ?? false}
                            onChange={() => toggleRow(key)}
                            aria-label={`Select row ${key}`}
                            className="rounded border-edge"
                          />
                        </td>
                      )}
                      {columns.map((col) => (
                        <td key={col.id} className={cn('px-4 py-3 text-body', alignClass(col.align))}>
                          {col.accessor(row)}
                        </td>
                      ))}
                    </tr>
                  );
                })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
