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
 * Visual enterprise com hierarquia clara entre label, valor e tendência.
 */
export function StatCard({ title, value, icon, color = 'text-accent', trend }: StatCardProps) {
  return (
    <div className="bg-card rounded-lg shadow-sm border border-edge p-4 flex items-start gap-3 transition-colors hover:border-edge-strong group">
      <div className={`${color} shrink-0 mt-0.5 opacity-80 group-hover:opacity-100 transition-opacity`}>{icon}</div>
      <div className="min-w-0 flex-1">
        <p className="text-xs text-muted truncate mb-0.5">{title}</p>
        <p className="text-xl font-bold text-heading">{value}</p>
        {trend && (
          <p className={`text-[11px] mt-1 font-medium ${
            trend.direction === 'up' ? 'text-success' : 'text-critical'
          }`}>
            {trend.direction === 'up' ? '↑' : '↓'} {trend.label}
          </p>
        )}
      </div>
    </div>
  );
}
