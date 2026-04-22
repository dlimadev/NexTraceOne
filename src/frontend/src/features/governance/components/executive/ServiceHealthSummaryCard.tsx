import { useTranslation } from 'react-i18next';
import { TrendingUp, TrendingDown, Minus, AlertTriangle, Activity } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import { Badge } from '../../../../components/Badge';
import type { ServiceHealthSummaryResponse } from '../../api/executiveIntelligence';

interface Props {
  data: ServiceHealthSummaryResponse;
}

const trendIcon = (trend: string) => {
  if (trend === 'Improving') return <TrendingUp className="h-4 w-4 text-green-500" />;
  if (trend === 'Declining') return <TrendingDown className="h-4 w-4 text-red-500" />;
  return <Minus className="h-4 w-4 text-yellow-500" />;
};

const scoreColor = (score: number) => {
  if (score >= 80) return 'text-green-600';
  if (score >= 60) return 'text-yellow-600';
  return 'text-red-600';
};

/**
 * ServiceHealthSummaryCard — widget do Executive Intelligence Dashboard que exibe
 * o score de saúde global do tenant: SLO compliance + risk + ownership + deployment success.
 *
 * @see docs/FUTURE-ROADMAP.md Wave X.1
 */
export function ServiceHealthSummaryCard({ data }: Props) {
  const { t } = useTranslation();

  const dimensions = [
    { label: 'SLO', value: data.sloComplianceScore },
    { label: 'Risk', value: data.riskScore },
    { label: 'Ownership', value: data.ownershipHealthScore },
    { label: 'Deployment', value: data.deploymentSuccessRate },
  ];

  return (
    <Card data-testid="service-health-summary-card">
      <CardHeader>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Activity className="h-5 w-5 text-blue-500" />
            <span className="font-semibold text-sm">{t('executiveDashboard.serviceHealth')}</span>
          </div>
          <div className="flex items-center gap-1">
            {trendIcon(data.trend)}
            {data.isSimulated && <Badge variant="default" size="sm">Simulated</Badge>}
          </div>
        </div>
      </CardHeader>
      <CardBody>
        <div className={`text-3xl font-bold mb-2 ${scoreColor(data.overallScore)}`}>
          {data.overallScore.toFixed(0)}
          <span className="text-sm font-normal text-muted-foreground">/100</span>
        </div>
        <div className="grid grid-cols-2 gap-2 mt-3">
          {dimensions.map((dim) => (
            <div key={dim.label} className="flex justify-between items-center text-xs">
              <span className="text-muted-foreground">{dim.label}</span>
              <span className={`font-medium ${scoreColor(dim.value)}`}>{dim.value.toFixed(0)}%</span>
            </div>
          ))}
        </div>
        {data.criticalServicesCount > 0 && (
          <div className="mt-3 flex items-center gap-1 text-xs text-red-600">
            <AlertTriangle className="h-3 w-3" />
            <span>{data.criticalServicesCount} critical</span>
          </div>
        )}
      </CardBody>
    </Card>
  );
}
