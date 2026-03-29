import { Skeleton } from './Skeleton';
import { Card, CardBody } from './Card';

interface CardListSkeletonProps {
  /** Número de cards placeholder. */
  count?: number;
  /** Mostrar barra de stats no topo. */
  showStats?: boolean;
  /** Número de stat cards. */
  statsCount?: number;
}

/**
 * Placeholder animado para listas de cards com loading state.
 *
 * Substitui o padrão genérico de <Loader size="lg" /> centrado,
 * oferecendo feedback visual mais informativo sobre a estrutura
 * da página enquanto os dados são carregados.
 *
 * @see docs/DESIGN-SYSTEM.md §4.14
 */
export function CardListSkeleton({
  count = 4,
  showStats = true,
  statsCount = 4,
}: CardListSkeletonProps) {
  return (
    <div className="space-y-6">
      {/* Stats row */}
      {showStats && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {Array.from({ length: statsCount }).map((_, i) => (
            <Card key={`stat-${i}`}>
              <CardBody className="py-4">
                <div className="flex items-center gap-3">
                  <Skeleton variant="circular" width="w-8" height="h-8" />
                  <div className="flex-1 space-y-2">
                    <Skeleton width="w-20" />
                    <Skeleton width="w-12" height="h-5" />
                  </div>
                </div>
              </CardBody>
            </Card>
          ))}
        </div>
      )}

      {/* Filter bar skeleton */}
      <div className="flex items-center gap-3">
        <Skeleton width="w-64" height="h-9" variant="rectangular" />
        <Skeleton width="w-48" height="h-8" variant="rectangular" />
      </div>

      {/* Card list */}
      <div className="space-y-3">
        {Array.from({ length: count }).map((_, i) => (
          <Card key={`card-${i}`}>
            <CardBody>
              <div className="flex items-center gap-4">
                <Skeleton variant="rectangular" width="w-10" height="h-10" className="rounded-lg" />
                <div className="flex-1 space-y-2">
                  <div className="flex items-center gap-2">
                    <Skeleton width="w-40" height="h-4" />
                    <Skeleton width="w-16" height="h-5" variant="rectangular" className="rounded-full" />
                  </div>
                  <Skeleton width="w-56" />
                  <div className="flex gap-2">
                    <Skeleton width="w-14" height="h-5" variant="rectangular" className="rounded-full" />
                    <Skeleton width="w-14" height="h-5" variant="rectangular" className="rounded-full" />
                  </div>
                </div>
              </div>
            </CardBody>
          </Card>
        ))}
      </div>
    </div>
  );
}
