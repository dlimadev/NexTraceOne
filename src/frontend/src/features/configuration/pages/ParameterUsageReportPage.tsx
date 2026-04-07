import { useTranslation } from 'react-i18next';
import { useState, useEffect } from 'react';
import {
  BarChart3, PieChart, Activity, TrendingUp, Settings, Layers, RefreshCw,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';

/**
 * ParameterUsageReportPage — relatório de utilização de parâmetros de configuração.
 * Mostra overrides por scope, parâmetros mais alterados e cobertura.
 * Pilar: Source of Truth, Operational Intelligence.
 * Persona: Platform Admin, Tech Lead, Auditor.
 */

interface ParameterOverrideSummary {
  key: string;
  displayName: string;
  overrideCount: number;
  lastChangedAt: string | null;
}

interface ScopeDistribution {
  scope: string;
  count: number;
}

interface UsageReport {
  totalDefinitions: number;
  totalOverrides: number;
  definitionsWithOverrides: number;
  definitionsUsingDefault: number;
  overrideCoveragePercent: number;
  mostOverridden: ParameterOverrideSummary[];
  recentlyChanged: ParameterOverrideSummary[];
  overridesByScope: ScopeDistribution[];
}

export function ParameterUsageReportPage() {
  const { t } = useTranslation();
  const [report, setReport] = useState<UsageReport | null>(null);
  const [loading, setLoading] = useState(true);

  const fetchReport = async () => {
    setLoading(true);
    try {
      const resp = await fetch('/api/v1/configuration/analytics/usage');
      if (resp.ok) setReport(await resp.json());
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void fetchReport(); }, []);

  return (
    <PageContainer>
      <PageHeader
        title={t('phase5.usage.title', 'Parameter Usage Report')}
        subtitle={t('phase5.usage.subtitle', 'Overview of configuration parameter utilization across the platform')}
        icon={<BarChart3 size={24} />}
        actions={
          <button
            onClick={() => void fetchReport()}
            className="flex items-center gap-2 px-3 py-2 rounded-lg bg-slate-700 hover:bg-slate-600 text-sm"
          >
            <RefreshCw size={14} />
            {t('phase5.usage.refresh', 'Refresh')}
          </button>
        }
      />

      {loading && (
        <div className="text-center py-12 text-slate-400">
          {t('phase5.usage.loading', 'Loading usage report...')}
        </div>
      )}

      {!loading && report && (
        <>
          {/* Summary Cards */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mt-6">
            <Card>
              <CardBody className="text-center">
                <Settings size={20} className="mx-auto mb-2 text-blue-400" />
                <div className="text-2xl font-bold">{report.totalDefinitions}</div>
                <div className="text-xs text-slate-400">{t('phase5.usage.totalDefinitions', 'Total Definitions')}</div>
              </CardBody>
            </Card>
            <Card>
              <CardBody className="text-center">
                <Layers size={20} className="mx-auto mb-2 text-green-400" />
                <div className="text-2xl font-bold">{report.totalOverrides}</div>
                <div className="text-xs text-slate-400">{t('phase5.usage.totalOverrides', 'Total Overrides')}</div>
              </CardBody>
            </Card>
            <Card>
              <CardBody className="text-center">
                <PieChart size={20} className="mx-auto mb-2 text-amber-400" />
                <div className="text-2xl font-bold">{report.overrideCoveragePercent}%</div>
                <div className="text-xs text-slate-400">{t('phase5.usage.overrideCoverage', 'Override Coverage')}</div>
              </CardBody>
            </Card>
            <Card>
              <CardBody className="text-center">
                <TrendingUp size={20} className="mx-auto mb-2 text-purple-400" />
                <div className="text-2xl font-bold">{report.definitionsUsingDefault}</div>
                <div className="text-xs text-slate-400">{t('phase5.usage.usingDefault', 'Using Default')}</div>
              </CardBody>
            </Card>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mt-6">
            {/* Most Overridden */}
            <Card>
              <CardHeader className="flex items-center gap-2">
                <BarChart3 size={18} />
                <span>{t('phase5.usage.mostOverridden', 'Most Overridden Parameters')}</span>
              </CardHeader>
              <CardBody>
                {report.mostOverridden.length === 0 ? (
                  <p className="text-sm text-slate-400">{t('phase5.usage.noOverrides', 'No overrides found.')}</p>
                ) : (
                  <div className="space-y-2">
                    {report.mostOverridden.map((item) => (
                      <div key={item.key} className="flex items-center justify-between py-1 border-b border-slate-700/40">
                        <div>
                          <span className="text-sm font-medium">{item.key}</span>
                        </div>
                        <Badge variant="info">{item.overrideCount} {t('phase5.usage.overrides', 'overrides')}</Badge>
                      </div>
                    ))}
                  </div>
                )}
              </CardBody>
            </Card>

            {/* Scope Distribution */}
            <Card>
              <CardHeader className="flex items-center gap-2">
                <PieChart size={18} />
                <span>{t('phase5.usage.scopeDistribution', 'Overrides by Scope')}</span>
              </CardHeader>
              <CardBody>
                {report.overridesByScope.length === 0 ? (
                  <p className="text-sm text-slate-400">{t('phase5.usage.noScopes', 'No scope data available.')}</p>
                ) : (
                  <div className="space-y-2">
                    {report.overridesByScope.map((scope) => (
                      <div key={scope.scope} className="flex items-center justify-between py-1 border-b border-slate-700/40">
                        <span className="text-sm">{scope.scope}</span>
                        <Badge variant="default">{scope.count}</Badge>
                      </div>
                    ))}
                  </div>
                )}
              </CardBody>
            </Card>

            {/* Recently Changed */}
            <Card className="lg:col-span-2">
              <CardHeader className="flex items-center gap-2">
                <Activity size={18} />
                <span>{t('phase5.usage.recentlyChanged', 'Recently Changed Parameters')}</span>
              </CardHeader>
              <CardBody>
                {report.recentlyChanged.length === 0 ? (
                  <p className="text-sm text-slate-400">{t('phase5.usage.noRecent', 'No recent changes.')}</p>
                ) : (
                  <div className="space-y-2">
                    {report.recentlyChanged.map((item) => (
                      <div key={item.key} className="flex items-center justify-between py-1 border-b border-slate-700/40">
                        <div>
                          <span className="text-sm font-medium">{item.key}</span>
                          {item.lastChangedAt && (
                            <span className="text-xs text-slate-500 ml-2">
                              {new Date(item.lastChangedAt).toLocaleString()}
                            </span>
                          )}
                        </div>
                        <Badge variant="info">{item.overrideCount} {t('phase5.usage.overrides', 'overrides')}</Badge>
                      </div>
                    ))}
                  </div>
                )}
              </CardBody>
            </Card>
          </div>
        </>
      )}
    </PageContainer>
  );
}

export default ParameterUsageReportPage;
