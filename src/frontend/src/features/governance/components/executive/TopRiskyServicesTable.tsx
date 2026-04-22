import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { ShieldAlert, ArrowRight } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import { Badge } from '../../../../components/Badge';
import type { TopRiskyServicesWidgetResponse } from '../../api/executiveIntelligence';

interface Props {
  data: TopRiskyServicesWidgetResponse;
}

const riskVariant = (level: string): 'danger' | 'warning' | 'info' | 'default' => {
  if (level === 'Critical') return 'danger';
  if (level === 'High') return 'warning';
  if (level === 'Medium') return 'info';
  return 'default';
};

/**
 * TopRiskyServicesTable — widget do Executive Intelligence Dashboard que exibe
 * os top 5 serviços por score de risco com drill-down direto para o Risk Center.
 *
 * @see docs/FUTURE-ROADMAP.md Wave X.1
 */
export function TopRiskyServicesTable({ data }: Props) {
  const { t } = useTranslation();
  const navigate = useNavigate();

  return (
    <Card data-testid="top-risky-services-table">
      <CardHeader>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <ShieldAlert className="h-5 w-5 text-red-500" />
            <span className="font-semibold text-sm">{t('executiveDashboard.topRiskyServices')}</span>
          </div>
          {data.isSimulated && <Badge variant="default" size="sm">Simulated</Badge>}
        </div>
      </CardHeader>
      <CardBody>
        {data.services.length === 0 ? (
          <p className="text-xs text-muted-foreground">{t('executiveDashboard.noData')}</p>
        ) : (
          <div className="space-y-2">
            {data.services.map((svc) => (
              <button
                key={svc.serviceId}
                type="button"
                className="w-full flex items-center justify-between text-left hover:bg-muted/50 rounded p-1 transition-colors"
                onClick={() => navigate(`/governance/risk?service=${encodeURIComponent(svc.serviceId)}`)}
              >
                <div className="min-w-0">
                  <p className="text-xs font-medium truncate">{svc.serviceName}</p>
                  <p className="text-xs text-muted-foreground truncate">{svc.domain} · {svc.topRiskDimension}</p>
                </div>
                <div className="flex items-center gap-2 shrink-0 ml-2">
                  <Badge variant={riskVariant(svc.riskLevel)} size="sm">
                    {svc.riskScore.toFixed(0)}
                  </Badge>
                  <ArrowRight className="h-3 w-3 text-muted-foreground" />
                </div>
              </button>
            ))}
          </div>
        )}
      </CardBody>
    </Card>
  );
}
