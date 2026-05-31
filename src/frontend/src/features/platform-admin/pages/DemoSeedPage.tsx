import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { FlaskConical, Play, Trash2, CheckCircle2, XCircle, RefreshCw, AlertTriangle } from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { platformAdminApi } from '../api/platformAdmin';

export function DemoSeedPage() {
  const { t } = useTranslation('demoSeed');
  const queryClient = useQueryClient();
  const [confirmClear, setConfirmClear] = useState(false);

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['demo-seed-status'],
    queryFn: platformAdminApi.getDemoSeedStatus,
  });

  const seedMutation = useMutation({
    mutationFn: () => platformAdminApi.runDemoSeed({}),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['demo-seed-status'] });
    },
  });

  const clearMutation = useMutation({
    mutationFn: platformAdminApi.clearDemoData,
    onSuccess: () => {
      setConfirmClear(false);
      queryClient.invalidateQueries({ queryKey: ['demo-seed-status'] });
    },
  });

  const isSeeded = data?.state === 'Seeded';

  return (
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          icon={<FlaskConical size={24} className="text-accent" />}
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
            {/* Status Banner */}
            {isSeeded ? (
              <div className="flex items-start gap-3 p-4 bg-warning/10 border border-warning/20 rounded-lg">
                <AlertTriangle size={18} className="text-warning mt-0.5 shrink-0" />
                <div>
                  <p className="text-sm font-medium text-warning">{t('seededBannerTitle')}</p>
                  <p className="text-xs text-warning/80 mt-0.5">{t('seededBannerDesc')}</p>
                </div>
              </div>
            ) : (
              <div className="flex items-start gap-3 p-4 bg-elevated border border-edge rounded-lg">
                <CheckCircle2 size={18} className="text-muted mt-0.5 shrink-0" />
                <p className="text-sm text-muted">{t('notSeededMsg')}</p>
              </div>
            )}

            {/* Stats */}
            {isSeeded && (
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <StatCard label={t('statServices')} value={data.servicesCount} />
                <StatCard label={t('statChanges')} value={data.changesCount} />
                <StatCard label={t('statIncidents')} value={data.incidentsCount} />
                <StatCard label={t('statTotal')} value={data.entitiesCount} />
              </div>
            )}

            {/* What's included */}
            {!isSeeded && (
              <section className="border border-edge rounded-lg p-5">
                <h2 className="text-base font-medium text-heading mb-3">{t('includedTitle')}</h2>
                <ul className="space-y-2 text-sm text-muted">
                  <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-accent" />{t('inc1')}</li>
                  <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-accent" />{t('inc2')}</li>
                  <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-accent" />{t('inc3')}</li>
                  <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-accent" />{t('inc4')}</li>
                  <li className="flex items-center gap-2"><CheckCircle2 size={14} className="text-accent" />{t('inc5')}</li>
                </ul>
              </section>
            )}

            {/* Actions */}
            <div className="flex flex-wrap gap-3">
              {!isSeeded && (
                <Button
                  variant="primary"
                  onClick={() => seedMutation.mutate()}
                  disabled={seedMutation.isPending}
                >
                  <Play size={14} />
                  {seedMutation.isPending ? t('seeding') : t('seedBtn')}
                </Button>
              )}
              {isSeeded && (
                <>
                  {!confirmClear ? (
                    <Button
                      variant="danger"
                      onClick={() => setConfirmClear(true)}
                    >
                      <Trash2 size={14} />
                      {t('clearBtn')}
                    </Button>
                  ) : (
                    <div className="flex items-center gap-3 p-4 bg-critical/10 border border-critical/20 rounded-lg">
                      <p className="text-sm text-critical">{t('confirmClearMsg')}</p>
                      <button
                        onClick={() => clearMutation.mutate()}
                        disabled={clearMutation.isPending}
                        className="px-3 py-1.5 bg-critical text-white text-xs rounded-lg hover:bg-critical/90 disabled:opacity-50"
                      >
                        {clearMutation.isPending ? t('clearing') : t('confirmYes')}
                      </button>
                      <button
                        onClick={() => setConfirmClear(false)}
                        className="px-3 py-1.5 text-xs border border-edge rounded-lg hover:bg-elevated"
                      >
                        {t('confirmNo')}
                      </button>
                    </div>
                  )}
                </>
              )}
            </div>

            {/* Mutation feedback */}
            {seedMutation.isSuccess && (
              <div className="flex items-center gap-2 text-sm text-success bg-success/10 border border-success/20 rounded-lg p-3">
                <CheckCircle2 size={16} />
                {t('seedSuccess', { count: seedMutation.data.entitiesCreated, ms: seedMutation.data.durationMs })}
              </div>
            )}
            {clearMutation.isSuccess && (
              <div className="flex items-center gap-2 text-sm text-success bg-success/10 border border-success/20 rounded-lg p-3">
                <CheckCircle2 size={16} />
                {t('clearSuccess', { count: clearMutation.data.entitiesRemoved })}
              </div>
            )}

            <p className="text-xs text-faded italic">{data.simulatedNote}</p>
          </>
        )}
      </div>
    </PageContainer>
  );
}

function StatCard({ label, value }: { label: string; value: number }) {
  return (
    <div className="border border-edge rounded-lg p-4 bg-card">
      <p className="text-xs text-muted uppercase tracking-wide">{label}</p>
      <p className="text-2xl font-semibold text-accent mt-1">{value}</p>
    </div>
  );
}
