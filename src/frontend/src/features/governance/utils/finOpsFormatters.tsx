import { CheckCircle, AlertCircle, AlertTriangle, XCircle, TrendingUp, TrendingDown, Minus } from 'lucide-react';
import type { CostEfficiencyType } from '../../../types';

/** Formata um valor monetário com o locale indicado. */
export function formatCurrency(value: number, locale = 'en-US'): string {
  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency: 'USD',
    maximumFractionDigits: 0,
  }).format(value);
}

/** Devolve a variante de Badge correspondente à eficiência de custo. */
export function efficiencyBadgeVariant(
  eff: CostEfficiencyType,
): 'success' | 'warning' | 'danger' | 'default' {
  switch (eff) {
    case 'Efficient':
      return 'success';
    case 'Acceptable':
      return 'default';
    case 'Inefficient':
      return 'warning';
    case 'Wasteful':
      return 'danger';
    default:
      return 'default';
  }
}

/** Ícone JSX associado ao nível de eficiência. */
export function efficiencyIcon(eff: CostEfficiencyType): React.ReactElement | null {
  switch (eff) {
    case 'Efficient':
      return <CheckCircle size={14} className="text-success" />;
    case 'Acceptable':
      return <AlertCircle size={14} className="text-muted" />;
    case 'Inefficient':
      return <AlertTriangle size={14} className="text-warning" />;
    case 'Wasteful':
      return <XCircle size={14} className="text-critical" />;
    default:
      return null;
  }
}

/** Ícone JSX associado à direcção de tendência de custo. */
export function trendIcon(dir: string): React.ReactElement {
  switch (dir) {
    case 'Improving':
      return <TrendingUp size={14} className="text-success" />;
    case 'Declining':
      return <TrendingDown size={14} className="text-critical" />;
    default:
      return <Minus size={14} className="text-muted" />;
  }
}
