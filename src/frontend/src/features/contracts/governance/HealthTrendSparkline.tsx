import { useTranslation } from 'react-i18next';

interface HealthTrendSparklineProps {
  points: { semVer: string; healthScore: number }[];
}

/**
 * Tendência do health score ao longo das versões — polyline SVG pura (sem libs de gráfico).
 * Honest-null: com menos de 2 pontos não há tendência a mostrar.
 */
export function HealthTrendSparkline({ points }: HealthTrendSparklineProps) {
  const { t } = useTranslation();
  if (points.length < 2) return null;

  const W = 100;
  const H = 32;
  const scores = points.map((p) => p.healthScore);
  const min = Math.min(...scores);
  const max = Math.max(...scores);
  const span = max - min || 1;
  const stepX = W / (points.length - 1);

  const coords = points.map((p, i) => {
    const x = i * stepX;
    const y = H - ((p.healthScore - min) / span) * H;
    return { x, y };
  });
  const polyPoints = coords.map((c) => `${c.x.toFixed(2)},${c.y.toFixed(2)}`).join(' ');

  return (
    <div className="rounded-lg border border-edge bg-panel p-4">
      <p className="text-xs font-medium uppercase tracking-wide text-muted mb-2">
        {t('contracts.healthTimeline.trend', 'Health score trend')}
      </p>
      <svg
        viewBox={`0 0 ${W} ${H}`}
        preserveAspectRatio="none"
        className="w-full h-16"
        role="img"
        aria-label={t('contracts.healthTimeline.trend', 'Health score trend')}
      >
        <polyline
          points={polyPoints}
          fill="none"
          stroke="var(--color-accent)"
          strokeWidth={1.5}
          vectorEffect="non-scaling-stroke"
        />
        {coords.map((c, i) => (
          <circle key={points[i].semVer} cx={c.x} cy={c.y} r={1.5} fill="var(--color-accent)" vectorEffect="non-scaling-stroke" />
        ))}
      </svg>
    </div>
  );
}
