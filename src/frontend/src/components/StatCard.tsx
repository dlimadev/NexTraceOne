interface StatCardProps {
  title: string;
  value: string | number;
  icon: React.ReactNode;
  color?: string;
  /** Tendência opcional — positiva (up) ou negativa (down). */
  trend?: { direction: 'up' | 'down'; label: string };
}

/**
 * Card de métrica com ícone, valor numérico e tendência opcional.
 * Projetado para grids de KPIs no dashboard — superfície elevada sobre canvas escuro.
 */
export function StatCard({ title, value, icon, color = 'text-accent', trend }: StatCardProps) {
  return (
    <div className="bg-card rounded-lg shadow-sm border border-edge p-5 flex items-start gap-4 transition-colors hover:border-edge-strong">
      <div className={`${color} shrink-0 mt-0.5`}>{icon}</div>
      <div className="min-w-0">
        <p className="text-sm text-muted truncate">{title}</p>
        <p className="text-2xl font-bold text-heading mt-0.5">{value}</p>
        {trend && (
          <p className={`text-xs mt-1 font-medium ${
            trend.direction === 'up' ? 'text-success' : 'text-critical'
          }`}>
            {trend.direction === 'up' ? '↑' : '↓'} {trend.label}
          </p>
        )}
      </div>
    </div>
  );
}
