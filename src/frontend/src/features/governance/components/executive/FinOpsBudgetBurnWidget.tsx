import { useTranslation } from 'react-i18next';
import { DollarSign, AlertTriangle } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import { Badge } from '../../../../components/Badge';
import type { FinOpsBudgetBurnWidgetResponse } from '../../api/executiveIntelligence';

interface Props {
  data: FinOpsBudgetBurnWidgetResponse;
}

const burnColor = (pct: number, accelerated: boolean) => {
  if (accelerated || pct >= 90) return 'text-red-600';
  if (pct >= 75) return 'text-yellow-600';
  return 'text-green-600';
};

const burnBarColor = (pct: number, accelerated: boolean) => {
  if (accelerated || pct >= 90) return 'bg-red-500';
  if (pct >= 75) return 'bg-yellow-500';
  return 'bg-green-500';
};

/**
 * FinOpsBudgetBurnWidget — widget do Executive Intelligence Dashboard que exibe
 * % de budget consumido vs. período, com flag de burn rate acelerado.
 *
 * @see docs/FUTURE-ROADMAP.md Wave X.1
 */
export function FinOpsBudgetBurnWidget({ data }: Props) {
  const { t } = useTranslation();

  return (
    <Card data-testid="finops-budget-burn-widget">
      <CardHeader>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <DollarSign className="h-5 w-5 text-orange-500" />
            <span className="font-semibold text-sm">{t('executiveDashboard.finopsBudget')}</span>
          </div>
          <div className="flex items-center gap-1">
            {data.burnAccelerated && (
              <Badge variant="danger" size="sm">
                <AlertTriangle className="h-3 w-3 mr-1 inline" />
                {t('executiveDashboard.burnAccelerated')}
              </Badge>
            )}
            {data.isSimulated && <Badge variant="default" size="sm">Simulated</Badge>}
          </div>
        </div>
      </CardHeader>
      <CardBody>
        <div className={`text-3xl font-bold mb-2 ${burnColor(data.budgetConsumedPct, data.burnAccelerated)}`}>
          {data.budgetConsumedPct.toFixed(0)}%
        </div>
        <div className="w-full h-2 bg-muted rounded-full mb-3">
          <div
            className={`h-2 rounded-full transition-all ${burnBarColor(data.budgetConsumedPct, data.burnAccelerated)}`}
            style={{ width: `${Math.min(data.budgetConsumedPct, 100)}%` }}
          />
        </div>
        <div className="flex justify-between text-xs text-muted-foreground">
          <span>${data.totalSpent.toLocaleString()}</span>
          <span>/ ${data.totalBudget.toLocaleString()}</span>
        </div>
        <p className="text-xs text-muted-foreground mt-1">{data.periodLabel}</p>
      </CardBody>
    </Card>
  );
}
