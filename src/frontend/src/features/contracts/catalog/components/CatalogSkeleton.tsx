/**
 * Skeleton loading state para a tabela do catálogo.
 * Simula 11 colunas com shimmer para feedback visual imediato.
 */
import { Skeleton } from '../../../../components/Skeleton';

export function CatalogSkeleton() {
  return (
    <div className="p-1">
      {/* Header skeleton */}
      <div className="flex items-center gap-3 px-3 py-3 border-b border-edge">
        {Array.from({ length: 10 }).map((_, i) => (
          <Skeleton key={i} className="h-3 flex-1" />
        ))}
      </div>
      {/* Row skeletons */}
      {Array.from({ length: 8 }).map((_, row) => (
        <div
          key={row}
          className="flex items-center gap-3 px-3 py-3.5 border-b border-edge last:border-0"
        >
          {/* Name */}
          <div className="flex-[2] space-y-1.5">
            <Skeleton className="h-3 w-3/4" />
            <Skeleton className="h-2 w-1/2" />
          </div>
          {/* Type badge */}
          <Skeleton className="h-5 w-16 flex-1 rounded-full" />
          {/* Domain */}
          <Skeleton className="h-3 w-14 flex-1" />
          {/* Owner */}
          <div className="flex-1 space-y-1.5">
            <Skeleton className="h-3 w-16" />
            <Skeleton className="h-2 w-12" />
          </div>
          {/* Version */}
          <Skeleton className="h-3 w-10 flex-1" />
          {/* Lifecycle */}
          <Skeleton className="h-5 w-14 flex-1 rounded-full" />
          {/* Approval */}
          <Skeleton className="h-5 w-14 flex-1 rounded-full" />
          {/* Compliance */}
          <Skeleton className="h-3 w-8 flex-1" />
          {/* Criticality */}
          <Skeleton className="h-3 w-10 flex-1" />
          {/* Updated */}
          <Skeleton className="h-3 w-12 flex-1" />
        </div>
      ))}
    </div>
  );
}
