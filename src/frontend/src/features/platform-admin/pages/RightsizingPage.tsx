import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Sliders, RefreshCw, XCircle, CheckCircle, AlertTriangle, TrendingDown } from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { platformAdminApi, type RightsizingRecommendation } from '../api/platformAdmin';

export function RightsizingPage() {
  const { t } = useTranslation('rightsizing');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['rightsizing-report'],
    queryFn: platformAdminApi.getRightsizingReport,
  });

  return (
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          icon={<Sliders size={24} className="text-accent" />}
          actions={
            <Button variant="ghost" onClick={() => refetch()}>
              <RefreshCw size={14} />
              {t('refresh')}
            </Button>
          }
        />

        {isLoading && (
          <div className="flex items-center justify-center h-48 text-faded text-sm">
            {t('loading')}
          </div>
        )}

        {isError && (
          <div className="flex items-center gap-3 p-4 bg-critical/10 border border-critical/20 rounded-lg text-critical text-sm">
            <XCircle size={18} />
            {t('error')}
          </div>
        )}

        {data && (
          <>
            {/* Summary */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <SummaryCard
                label={t('statServices')}
                value={String(data.totalServicesAnalysed)}
                color="accent"
              />
              <SummaryCard
                label={t('statRecs')}
                value={String(data.recommendations.length)}
                color="warning"
              />
              <SummaryCard
                label={t('statCpuSaving')}
                value={`${data.totalSavingEstimateCpuPercent.toFixed(0)}%`}
                color="success"
              />
              <SummaryCard
                label={t('statMemSaving')}
                value={`${data.totalSavingEstimateMemoryPercent.toFixed(0)}%`}
                color="accent"
              />
            </div>

            {/* Recommendations table */}
            {data.recommendations.length === 0 ? (
              <div className="flex flex-col items-center justify-center h-40 text-faded gap-2">
                <CheckCircle size={32} className="text-success" />
                <p className="text-sm">{t('noRecs')}</p>
              </div>
            ) : (
              <section>
                <h2 className="text-lg font-medium text-heading mb-3">{t('recsTitle')}</h2>
                <div className="border border-edge rounded-lg overflow-hidden">
                  <table className="w-full text-sm">
                    <thead className="bg-elevated border-b border-edge">
                      <tr>
                        <th className="text-left px-4 py-3 text-muted font-medium">{t('colService')}</th>
                        <th className="text-left px-4 py-3 text-muted font-medium">{t('colResource')}</th>
                        <th className="text-left px-4 py-3 text-muted font-medium">{t('colCurrent')}</th>
                        <th className="text-left px-4 py-3 text-muted font-medium">{t('colRecommended')}</th>
                        <th className="text-left px-4 py-3 text-muted font-medium">{t('colSaving')}</th>
                        <th className="text-left px-4 py-3 text-muted font-medium">{t('colImpact')}</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-edge/50">
                      {data.recommendations.map((rec) => (
                        <RecommendationRow key={`${rec.serviceId}-${rec.resource}`} rec={rec} t={t} />
                      ))}
                    </tbody>
                  </table>
                </div>
              </section>
            )}

            <p className="text-xs text-faded italic">
              {t('safetyMarginNote', { percent: data.safetyMarginPercent })} · {data.simulatedNote}
            </p>
          </>
        )}
      </div>
    </PageContainer>
  );
}

function RecommendationRow({
  rec,
  t,
}: {
  rec: RightsizingRecommendation;
  t: (key: string) => string;
}) {
  const impactColor = {
    Low: 'text-success bg-success/10 border-success/20',
    Medium: 'text-warning bg-warning/10 border-warning/20',
    High: 'text-critical bg-critical/10 border-critical/20',
  }[rec.reliabilityImpact];

  return (
    <tr className="hover:bg-elevated">
      <td className="px-4 py-3">
        <p className="font-medium text-heading">{rec.serviceName}</p>
        <p className="text-xs text-faded">{rec.teamName}</p>
      </td>
      <td className="px-4 py-3">
        <span className="px-2 py-0.5 rounded text-xs font-medium bg-accent/10 text-accent border border-accent/20">
          {rec.resource}
        </span>
      </td>
      <td className="px-4 py-3 text-muted">
        {rec.currentAllocation} {rec.unit}
      </td>
      <td className="px-4 py-3 font-medium text-heading">
        {rec.recommendedAllocation} {rec.unit}
      </td>
      <td className="px-4 py-3">
        <span className="flex items-center gap-1 text-success text-xs font-medium">
          <TrendingDown size={12} />
          {rec.savingPercent.toFixed(0)}%
        </span>
      </td>
      <td className="px-4 py-3">
        <span className={`px-2 py-0.5 rounded text-xs font-medium border ${impactColor}`}>
          {rec.sloAtRisk ? <AlertTriangle size={10} className="inline mr-1" /> : null}
          {t(`impact.${rec.reliabilityImpact}`)}
        </span>
      </td>
    </tr>
  );
}

function SummaryCard({
  label,
  value,
  color,
}: {
  label: string;
  value: string;
  color: 'accent' | 'warning' | 'success';
}) {
  const colorMap = {
    accent: 'text-accent',
    warning: 'text-warning',
    success: 'text-success',
  };
  return (
    <div className="border border-edge rounded-lg p-4 bg-card">
      <p className="text-xs text-muted uppercase tracking-wide">{label}</p>
      <p className={`text-2xl font-semibold mt-1 ${colorMap[color]}`}>{value}</p>
    </div>
  );
}
