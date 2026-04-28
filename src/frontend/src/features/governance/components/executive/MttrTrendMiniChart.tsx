import { useTranslation } from 'react-i18next';
import { Clock, TrendingUp, TrendingDown, Minus } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import { Badge } from '../../../../components/Badge';
import type { MttrTrendWidgetResponse } from '../../api/executiveIntelligence';

interface Props {
  data: MttrTrendWidgetResponse;
}

const trendIcon = (trend: string) => {
  if (trend === 'Improving') return <TrendingDown className="h-3 w-3 text-green-500" aria-label="Improving" />;
  if (trend === 'Worsening') return <TrendingUp className="h-3 w-3 text-red-500" aria-label="Worsening" />;
  return <Minus className="h-3 w-3 text-yellow-500" aria-label="Stable" />;
};

/**
 * MttrTrendMiniChart — widget do Executive Intelligence Dashboard que exibe
 * sparkline de MTTR dos últimos 30 dias para os serviços mais críticos.
 * Renderiza uma mini visualização textual quando ECharts não estiver disponível.
 *
 * @see docs/FUTURE-ROADMAP.md Wave X.1
 */
export function MttrTrendMiniChart({ data }: Props) {
  const { t } = useTranslation();

  return (
    <Card data-testid="mttr-trend-mini-chart">
      <CardHeader>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Clock className="h-5 w-5 text-blue-500" />
            <span className="font-semibold text-sm">{t('executiveDashboard.mttrTrend')}</span>
          </div>
          {data.isSimulated && <Badge variant="default" size="sm">Simulated</Badge>}
        </div>
      </CardHeader>
      <CardBody>
        {data.services.length === 0 ? (
          <p className="text-xs text-muted-foreground">{t('executiveDashboard.noData')}</p>
        ) : (
          <div className="space-y-3">
            {data.services.map((svc) => {
              const max = Math.max(...svc.sparkline.map((p) => p.mttrHours), 1);
              return (
                <div key={svc.serviceId}>
                  <div className="flex items-center justify-between text-xs mb-1">
                    <span className="font-medium truncate max-w-[120px]">{svc.serviceName}</span>
                    <div className="flex items-center gap-1">
                      {trendIcon(svc.trend)}
                      <span className="text-muted-foreground">{svc.currentMttrHours.toFixed(1)}h</span>
                    </div>
                  </div>
                  {/* Mini sparkline using SVG */}
                  <svg
                    viewBox={`0 0 ${svc.sparkline.length} 20`}
                    className="w-full h-4 text-blue-400"
                    preserveAspectRatio="none"
                    aria-label={`MTTR sparkline for ${svc.serviceName}`}
                  >
                    <polyline
                      points={svc.sparkline
                        .map((p, i) => `${i},${20 - (p.mttrHours / max) * 18}`)
                        .join(' ')}
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="1.5"
                    />
                  </svg>
                </div>
              );
            })}
          </div>
        )}
      </CardBody>
    </Card>
  );
}
