import type { ReactNode } from 'react';
import { cn } from '../lib/cn';
import { DetailPanel } from './shell/DetailPanel';

interface SplitViewProps {
  /** The list / master panel content. */
  list: ReactNode;
  /** The detail panel content. Rendered only when `selectedId` is set. */
  detail?: ReactNode;
  /** Title for the detail panel header. */
  detailTitle?: string;
  /** Called when the detail panel close button is clicked. */
  onCloseDetail?: () => void;
  /** Whether the detail panel is open. Defaults to `!!detail`. */
  detailOpen?: boolean;
  /** Extra CSS for the outer wrapper. */
  className?: string;
  /** Extra CSS for the list column. */
  listClassName?: string;
}

/**
 * SplitView — layout master/detail para listas com painel lateral de detalhe.
 * Usado em ServiceCatalogListPage, IncidentsPage, etc.
 *
 * No mobile o painel de detalhe sobrepõe o conteúdo (via DetailPanel).
 * No desktop ambos ficam lado a lado.
 *
 * @see docs/frontend-audit/frontend-prioritized-improvement-roadmap.md §F3-06
 */
export function SplitView({
  list,
  detail,
  detailTitle,
  onCloseDetail,
  detailOpen,
  className,
  listClassName,
}: SplitViewProps) {
  const open = detailOpen ?? !!detail;

  return (
    <div
      className={cn(
        'flex min-h-0 overflow-hidden',
        // When detail is open on large screens, show side-by-side
        open ? 'lg:divide-x lg:divide-edge' : '',
        className,
      )}
    >
      {/* List column */}
      <div
        className={cn(
          'flex-1 min-w-0 overflow-auto',
          // Hide list on mobile when detail is open
          open ? 'hidden lg:block' : 'block',
          listClassName,
        )}
      >
        {list}
      </div>

      {/* Detail panel */}
      {open && (
        <DetailPanel
          title={detailTitle}
          onClose={onCloseDetail}
          open={open}
          className="w-full lg:w-[400px] xl:w-[480px]"
        >
          {detail}
        </DetailPanel>
      )}
    </div>
  );
}
