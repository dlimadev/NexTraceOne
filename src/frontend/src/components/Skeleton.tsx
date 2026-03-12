interface SkeletonProps {
  className?: string;
}

/**
 * Skeleton de carregamento genérico — bloco animado com pulse.
 * Ajuste largura/altura via className para compor layouts de loading complexos.
 */
export function Skeleton({ className = '' }: SkeletonProps) {
  return (
    <div
      className={`animate-pulse rounded-md bg-elevated ${className}`}
      aria-hidden="true"
    />
  );
}

/**
 * Skeleton pré-configurado para linhas de texto (ex.: listas, tabelas).
 */
export function SkeletonLine({ className = '' }: SkeletonProps) {
  return <Skeleton className={`h-4 ${className}`} />;
}

/**
 * Skeleton pré-configurado para cards de métricas (StatCard).
 */
export function SkeletonCard() {
  return (
    <div className="bg-card rounded-lg border border-edge p-5 space-y-3">
      <Skeleton className="h-4 w-24" />
      <Skeleton className="h-8 w-16" />
    </div>
  );
}

/**
 * Skeleton pré-configurado para tabelas com n linhas.
 */
export function SkeletonTable({ rows = 5 }: { rows?: number }) {
  return (
    <div className="space-y-3 p-6">
      <Skeleton className="h-4 w-full" />
      {Array.from({ length: rows }).map((_, i) => (
        <Skeleton key={i} className="h-10 w-full" />
      ))}
    </div>
  );
}
