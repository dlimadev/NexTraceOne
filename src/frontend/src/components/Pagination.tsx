import { useMemo, useCallback } from 'react';
import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { cn } from '../lib/cn';

interface PaginationProps {
  /** Página atual (1-indexed). */
  page: number;
  /** Total de itens. */
  totalItems: number;
  /** Itens por página. */
  pageSize: number;
  /** Callback ao mudar de página. */
  onPageChange: (page: number) => void;
  /** Callback ao mudar page size. */
  onPageSizeChange?: (size: number) => void;
  /** Opções de page size. */
  pageSizeOptions?: number[];
  /** Variante visual. */
  variant?: 'compact' | 'full';
  className?: string;
}

/**
 * Paginação standalone com variantes compact e full.
 *
 * - compact: prev/next apenas
 * - full: first/prev/pages/next/last + items per page selector
 *
 * i18n compliant: usa chaves de tradução para labels.
 */
export function Pagination({
  page,
  totalItems,
  pageSize,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = [10, 25, 50, 100],
  variant = 'full',
  className,
}: PaginationProps) {
  const { t } = useTranslation();
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));

  const canPrev = page > 1;
  const canNext = page < totalPages;

  const visiblePages = useMemo(() => {
    const pages: number[] = [];
    const maxVisible = 5;
    let start = Math.max(1, page - Math.floor(maxVisible / 2));
    const end = Math.min(totalPages, start + maxVisible - 1);
    start = Math.max(1, end - maxVisible + 1);

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    return pages;
  }, [page, totalPages]);

  const navBtnClass = cn(
    'inline-flex items-center justify-center w-8 h-8 rounded-sm text-muted',
    'hover:bg-hover hover:text-body transition-colors disabled:opacity-40 disabled:cursor-not-allowed',
  );

  const pageBtnClass = (p: number) =>
    cn(
      'inline-flex items-center justify-center min-w-[32px] h-8 px-2 rounded-sm text-sm font-medium transition-colors',
      p === page
        ? 'bg-accent-muted text-cyan border border-edge-focus'
        : 'text-muted hover:bg-hover hover:text-body',
    );

  if (variant === 'compact') {
    return (
      <div className={cn('flex items-center justify-between gap-4', className)}>
        <span className="text-xs text-muted">
          {t('pagination.pageOf', { page, totalPages })}
        </span>
        <div className="flex items-center gap-1">
          <button
            type="button"
            onClick={() => onPageChange(page - 1)}
            disabled={!canPrev}
            className={navBtnClass}
            aria-label={t('pagination.previous')}
          >
            <ChevronLeft size={16} />
          </button>
          <button
            type="button"
            onClick={() => onPageChange(page + 1)}
            disabled={!canNext}
            className={navBtnClass}
            aria-label={t('pagination.next')}
          >
            <ChevronRight size={16} />
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className={cn('flex flex-col sm:flex-row items-center justify-between gap-4', className)}>
      {/* Info */}
      <div className="flex items-center gap-4">
        {onPageSizeChange && (
          <div className="flex items-center gap-2">
            <span className="text-xs text-muted">{t('pagination.itemsPerPage')}</span>
            <select
              value={pageSize}
              onChange={(e) => onPageSizeChange(Number(e.target.value))}
              className="h-8 rounded-sm bg-input border border-edge px-2 text-xs text-heading"
            >
              {pageSizeOptions.map((opt) => (
                <option key={opt} value={opt}>{opt}</option>
              ))}
            </select>
          </div>
        )}
        <span className="text-xs text-muted">
          {t('pagination.showing', {
            from: Math.min((page - 1) * pageSize + 1, totalItems),
            to: Math.min(page * pageSize, totalItems),
            total: totalItems,
          })}
        </span>
      </div>

      {/* Navigation */}
      <nav className="flex items-center gap-1" aria-label={t('pagination.navigation')}>
        <button
          type="button"
          onClick={() => onPageChange(1)}
          disabled={!canPrev}
          className={navBtnClass}
          aria-label={t('pagination.first')}
        >
          <ChevronsLeft size={16} />
        </button>
        <button
          type="button"
          onClick={() => onPageChange(page - 1)}
          disabled={!canPrev}
          className={navBtnClass}
          aria-label={t('pagination.previous')}
        >
          <ChevronLeft size={16} />
        </button>

        {visiblePages[0] > 1 && (
          <span className="w-8 h-8 flex items-center justify-center text-xs text-muted">…</span>
        )}

        {visiblePages.map((p) => (
          <button
            key={p}
            type="button"
            onClick={() => onPageChange(p)}
            className={pageBtnClass(p)}
            aria-current={p === page ? 'page' : undefined}
          >
            {p}
          </button>
        ))}

        {visiblePages[visiblePages.length - 1] < totalPages && (
          <span className="w-8 h-8 flex items-center justify-center text-xs text-muted">…</span>
        )}

        <button
          type="button"
          onClick={() => onPageChange(page + 1)}
          disabled={!canNext}
          className={navBtnClass}
          aria-label={t('pagination.next')}
        >
          <ChevronRight size={16} />
        </button>
        <button
          type="button"
          onClick={() => onPageChange(totalPages)}
          disabled={!canNext}
          className={navBtnClass}
          aria-label={t('pagination.last')}
        >
          <ChevronsRight size={16} />
        </button>
      </nav>
    </div>
  );
}
