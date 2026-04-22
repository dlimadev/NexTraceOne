import { useTranslation } from 'react-i18next';
import { CheckSquare } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import { Badge } from '../../../../components/Badge';
import type { ComplianceCoverageWidgetResponse } from '../../api/executiveIntelligence';

interface Props {
  data: ComplianceCoverageWidgetResponse;
}

const barColor = (pct: number) => {
  if (pct >= 80) return 'bg-green-500';
  if (pct >= 50) return 'bg-yellow-500';
  return 'bg-red-500';
};

/**
 * ComplianceCoverageWidget — widget do Executive Intelligence Dashboard que exibe
 * cobertura de compliance por standard (8 barras horizontais, % de serviços cobertos).
 *
 * @see docs/FUTURE-ROADMAP.md Wave X.1
 */
export function ComplianceCoverageWidget({ data }: Props) {
  const { t } = useTranslation();

  return (
    <Card data-testid="compliance-coverage-widget">
      <CardHeader>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <CheckSquare className="h-5 w-5 text-teal-500" />
            <span className="font-semibold text-sm">{t('executiveDashboard.complianceCoverage')}</span>
          </div>
          {data.isSimulated && <Badge variant="default" size="sm">Simulated</Badge>}
        </div>
      </CardHeader>
      <CardBody>
        <div className="flex items-baseline gap-1 mb-3">
          <span className="text-2xl font-bold">{data.overallCoveragePct.toFixed(0)}%</span>
          <span className="text-xs text-muted-foreground">overall</span>
        </div>
        <div className="space-y-2">
          {data.standards.map((std) => (
            <div key={std.name}>
              <div className="flex justify-between text-xs mb-1">
                <span className="text-muted-foreground font-medium">{std.name}</span>
                <span className="font-medium">{std.coveragePct.toFixed(0)}%</span>
              </div>
              <div className="w-full h-1.5 bg-muted rounded-full">
                <div
                  className={`h-1.5 rounded-full ${barColor(std.coveragePct)}`}
                  style={{ width: `${Math.min(std.coveragePct, 100)}%` }}
                />
              </div>
            </div>
          ))}
        </div>
      </CardBody>
    </Card>
  );
}
