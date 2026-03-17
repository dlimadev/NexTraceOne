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
    ? { color: 'text-emerald-400', bg: 'bg-emerald-900/20', border: 'border-emerald-700/30', Icon: CheckCircle }
    : pct >= 50
      ? { color: 'text-amber-400', bg: 'bg-amber-900/20', border: 'border-amber-700/30', Icon: AlertTriangle }
      : pct > 0
        ? { color: 'text-red-400', bg: 'bg-red-900/20', border: 'border-red-700/30', Icon: XCircle }
        : { color: 'text-slate-500', bg: 'bg-slate-800/20', border: 'border-slate-700/30', Icon: Minus };

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
