import { useTranslation } from 'react-i18next';
import { CheckCircle, AlertTriangle, XCircle, Minus } from 'lucide-react';

interface ComplianceScoreCardProps {
  score: number;
  maxScore?: number;
  label?: string;
  className?: string;
}

/**
 * Card compacto de score de compliance.
 * score 0-100, com variantes visuais para pass/warning/violation.
 */
export function ComplianceScoreCard({ score, maxScore = 100, label, className = '' }: ComplianceScoreCardProps) {
  const { t } = useTranslation();
  const pct = maxScore > 0 ? Math.round((score / maxScore) * 100) : 0;

  const variant = pct >= 80
    ? { color: 'text-success', bg: 'bg-success/15', border: 'border-success/25', Icon: CheckCircle }
    : pct >= 50
      ? { color: 'text-warning', bg: 'bg-warning/15', border: 'border-warning/25', Icon: AlertTriangle }
      : pct > 0
        ? { color: 'text-critical', bg: 'bg-critical/15', border: 'border-critical/25', Icon: XCircle }
        : { color: 'text-faded', bg: 'bg-elevated', border: 'border-edge', Icon: Minus };

  return (
    <div className={`flex items-center gap-2 rounded-lg border px-3 py-2 ${variant.bg} ${variant.border} ${className}`}>
      <variant.Icon size={16} className={variant.color} />
      <div className="flex flex-col">
        <span className={`text-sm font-semibold ${variant.color}`}>{pct}%</span>
        <span className="text-[10px] text-muted">
          {label ?? t('contracts.compliance.score', 'Compliance')}
        </span>
      </div>
    </div>
  );
}
