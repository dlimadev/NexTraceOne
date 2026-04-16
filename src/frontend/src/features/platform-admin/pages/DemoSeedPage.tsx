import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { FlaskConical, Play, Trash2, CheckCircle, XCircle, RefreshCw, AlertTriangle } from 'lucide-react';
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
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <FlaskConical size={24} className="text-indigo-600" />
          <div>
            <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
            <p className="mt-1 text-sm text-slate-500">{t('subtitle')}</p>
          </div>
        </div>
        <button
          onClick={() => refetch()}
          className="flex items-center gap-2 px-4 py-2 text-sm border border-slate-300 rounded-lg hover:bg-slate-50"
        >
          <RefreshCw size={14} />
          {t('refresh')}
        </button>
      </div>

      {isLoading && (
        <div className="flex items-center justify-center h-48 text-slate-400 text-sm">
          {t('loading')}
        </div>
      )}

      {isError && (
        <div className="flex items-center gap-3 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          <XCircle size={18} />
          {t('error')}
        </div>
      )}

      {data && (
        <>
          {/* Status Banner */}
          {isSeeded ? (
            <div className="flex items-start gap-3 p-4 bg-amber-50 border border-amber-200 rounded-lg">
              <AlertTriangle size={18} className="text-amber-600 mt-0.5 shrink-0" />
              <div>
                <p className="text-sm font-medium text-amber-900">{t('seededBannerTitle')}</p>
                <p className="text-xs text-amber-700 mt-0.5">{t('seededBannerDesc')}</p>
              </div>
            </div>
          ) : (
            <div className="flex items-start gap-3 p-4 bg-slate-50 border border-slate-200 rounded-lg">
              <CheckCircle size={18} className="text-slate-400 mt-0.5 shrink-0" />
              <p className="text-sm text-slate-600">{t('notSeededMsg')}</p>
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
            <section className="border border-slate-200 rounded-lg p-5">
              <h2 className="text-base font-medium text-slate-800 mb-3">{t('includedTitle')}</h2>
              <ul className="space-y-2 text-sm text-slate-600">
                <li className="flex items-center gap-2"><CheckCircle size={14} className="text-indigo-500" />{t('inc1')}</li>
                <li className="flex items-center gap-2"><CheckCircle size={14} className="text-indigo-500" />{t('inc2')}</li>
                <li className="flex items-center gap-2"><CheckCircle size={14} className="text-indigo-500" />{t('inc3')}</li>
                <li className="flex items-center gap-2"><CheckCircle size={14} className="text-indigo-500" />{t('inc4')}</li>
                <li className="flex items-center gap-2"><CheckCircle size={14} className="text-indigo-500" />{t('inc5')}</li>
              </ul>
            </section>
          )}

          {/* Actions */}
          <div className="flex flex-wrap gap-3">
            {!isSeeded && (
              <button
                onClick={() => seedMutation.mutate()}
                disabled={seedMutation.isPending}
                className="flex items-center gap-2 px-5 py-2.5 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700 disabled:opacity-50 transition-colors"
              >
                <Play size={14} />
                {seedMutation.isPending ? t('seeding') : t('seedBtn')}
              </button>
            )}
            {isSeeded && (
              <>
                {!confirmClear ? (
                  <button
                    onClick={() => setConfirmClear(true)}
                    className="flex items-center gap-2 px-5 py-2.5 bg-red-600 text-white text-sm rounded-lg hover:bg-red-700 transition-colors"
                  >
                    <Trash2 size={14} />
                    {t('clearBtn')}
                  </button>
                ) : (
                  <div className="flex items-center gap-3 p-4 bg-red-50 border border-red-200 rounded-lg">
                    <p className="text-sm text-red-800">{t('confirmClearMsg')}</p>
                    <button
                      onClick={() => clearMutation.mutate()}
                      disabled={clearMutation.isPending}
                      className="px-3 py-1.5 bg-red-600 text-white text-xs rounded-lg hover:bg-red-700 disabled:opacity-50"
                    >
                      {clearMutation.isPending ? t('clearing') : t('confirmYes')}
                    </button>
                    <button
                      onClick={() => setConfirmClear(false)}
                      className="px-3 py-1.5 text-xs border border-slate-300 rounded-lg hover:bg-slate-50"
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
            <div className="flex items-center gap-2 text-sm text-emerald-700 bg-emerald-50 border border-emerald-200 rounded-lg p-3">
              <CheckCircle size={16} />
              {t('seedSuccess', { count: seedMutation.data.entitiesCreated, ms: seedMutation.data.durationMs })}
            </div>
          )}
          {clearMutation.isSuccess && (
            <div className="flex items-center gap-2 text-sm text-emerald-700 bg-emerald-50 border border-emerald-200 rounded-lg p-3">
              <CheckCircle size={16} />
              {t('clearSuccess', { count: clearMutation.data.entitiesRemoved })}
            </div>
          )}

          <p className="text-xs text-slate-400 italic">{data.simulatedNote}</p>
        </>
      )}
    </div>
  );
}

function StatCard({ label, value }: { label: string; value: number }) {
  return (
    <div className="border border-slate-200 rounded-lg p-4 bg-white">
      <p className="text-xs text-slate-500 uppercase tracking-wide">{label}</p>
      <p className="text-2xl font-semibold text-indigo-600 mt-1">{value}</p>
    </div>
  );
}
