import { useTranslation } from 'react-i18next';
import { Shield, TrendingUp, TrendingDown, Minus } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import { Badge } from '../../../../components/Badge';
import type { ChangeConfidenceGaugeResponse } from '../../api/executiveIntelligence';

interface Props {
  data: ChangeConfidenceGaugeResponse;
}

const trendIcon = (trend: string) => {
  if (trend === 'Improving') return <TrendingUp className="h-4 w-4 text-green-500" />;
  if (trend === 'Declining') return <TrendingDown className="h-4 w-4 text-red-500" />;
  return <Minus className="h-4 w-4 text-yellow-500" />;
};

const gaugeColor = (pct: number) => {
  if (pct >= 80) return 'text-green-600';
  if (pct >= 60) return 'text-yellow-600';
  return 'text-red-600';
};

const gaugeBg = (pct: number) => {
  if (pct >= 80) return 'bg-green-500';
  if (pct >= 60) return 'bg-yellow-500';
  return 'bg-red-500';
};

/**
 * ChangeConfidenceGauge — widget do Executive Intelligence Dashboard que exibe
 * a confiança média de deployment nas últimas 4 semanas por tier de serviço.
 *
 * @see docs/FUTURE-ROADMAP.md Wave X.1
 */
export function ChangeConfidenceGauge({ data }: Props) {
  const { t } = useTranslation();

  return (
    <Card data-testid="change-confidence-gauge">
      <CardHeader>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Shield className="h-5 w-5 text-purple-500" />
            <span className="font-semibold text-sm">{t('executiveDashboard.changeConfidence')}</span>
          </div>
          <div className="flex items-center gap-1">
            {trendIcon(data.trendDirection)}
            {data.isSimulated && <Badge variant="default" size="sm">Simulated</Badge>}
          </div>
        </div>
      </CardHeader>
      <CardBody>
        <div className={`text-3xl font-bold mb-3 ${gaugeColor(data.averageConfidencePct)}`}>
          {data.averageConfidencePct.toFixed(0)}%
        </div>
        {/* Progress bar gauge */}
        <div className="w-full h-2 bg-muted rounded-full mb-3">
          <div
            className={`h-2 rounded-full transition-all ${gaugeBg(data.averageConfidencePct)}`}
            style={{ width: `${Math.min(data.averageConfidencePct, 100)}%` }}
          />
        </div>
        {/* By tier breakdown */}
        <div className="space-y-1 mt-2">
          {data.byTier.map((tier) => (
            <div key={tier.tier} className="flex items-center justify-between text-xs">
              <span className="text-muted-foreground">{tier.tier}</span>
              <div className="flex items-center gap-2">
                <span className="text-muted-foreground text-xs">({tier.serviceCount})</span>
                <span className={`font-medium ${gaugeColor(tier.confidencePct)}`}>
                  {tier.confidencePct.toFixed(0)}%
                </span>
              </div>
            </div>
          ))}
        </div>
        <p className="text-xs text-muted-foreground mt-2">{data.periodLabel}</p>
      </CardBody>
    </Card>
  );
}
