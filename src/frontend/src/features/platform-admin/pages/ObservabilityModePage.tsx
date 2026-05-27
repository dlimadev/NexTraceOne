import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Activity, CheckCircle, XCircle, RefreshCw, AlertTriangle } from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { platformAdminApi, type ObservabilityMode } from '../api/platformAdmin';

const MODES: ObservabilityMode[] = ['Full', 'Lite', 'Minimal'];

export function ObservabilityModePage() {
  const { t } = useTranslation('observabilityMode');
  const queryClient = useQueryClient();

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['observability-mode'],
    queryFn: platformAdminApi.getObservabilityMode,
  });

  const mutation = useMutation({
    mutationFn: (mode: ObservabilityMode) =>
      platformAdminApi.updateObservabilityMode({ mode }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['observability-mode'] });
    },
  });

  return (
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          icon={<Activity size={24} className="text-accent" />}
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
            {/* Current status */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <StatusCard
                label={t('currentMode')}
                value={t(`mode.${data.currentMode}`)}
                active={true}
              />
              <StatusCard
                label={t('elasticsearch')}
                value={data.elasticsearchConnected ? (data.elasticsearchVersion ?? t('connected')) : t('notConnected')}
                active={data.elasticsearchConnected}
              />
              <StatusCard
                label={t('otelCollector')}
                value={data.otelCollectorConnected ? t('connected') : t('notConnected')}
                active={data.otelCollectorConnected}
              />
              <StatusCard
                label={t('additionalRam')}
                value={`${data.additionalRamUsageGb} GB`}
                active={true}
              />
            </div>

            {/* Mode selector */}
            <section>
              <h2 className="text-lg font-medium text-heading mb-3">{t('selectModeTitle')}</h2>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {MODES.map((mode) => {
                  const isActive = data.currentMode === mode;
                  return (
                    <button
                      key={mode}
                      onClick={() => mutation.mutate(mode)}
                      disabled={isActive || mutation.isPending}
                      className={`text-left p-5 border rounded-lg transition-all ${
                        isActive
                          ? 'border-accent bg-accent/10 ring-2 ring-accent/20'
                          : 'border-edge hover:border-accent/40 hover:bg-accent/5'
                      } disabled:cursor-not-allowed`}
                      aria-label={`${t('selectMode')} ${t(`mode.${mode}`)}`}
                    >
                      <div className="flex items-center justify-between mb-2">
                        <span className="font-medium text-heading">{t(`mode.${mode}`)}</span>
                        {isActive && <CheckCircle size={16} className="text-accent" />}
                      </div>
                      <p className="text-xs text-muted">{t(`modeDesc.${mode}`)}</p>
                      <p className="text-xs text-faded mt-1">{t(`modeRam.${mode}`)}</p>
                    </button>
                  );
                })}
              </div>
            </section>

            {/* Trade-offs */}
            {data.tradeOffs.length > 0 && (
              <section className="p-4 bg-warning/10 border border-warning/20 rounded-lg">
                <div className="flex items-start gap-2">
                  <AlertTriangle size={16} className="text-warning mt-0.5 shrink-0" />
                  <div>
                    <p className="text-sm font-medium text-warning mb-2">{t('tradeOffsTitle')}</p>
                    <ul className="space-y-1">
                      {data.tradeOffs.map((note, idx) => (
                        <li key={idx} className="text-xs text-warning/80">
                          • {note}
                        </li>
                      ))}
                    </ul>
                  </div>
                </div>
              </section>
            )}

            {mutation.isSuccess && (
              <div className="flex items-center gap-2 text-sm text-success bg-success/10 border border-success/20 rounded-lg p-3">
                <CheckCircle size={16} />
                {t('saveSuccess')}
              </div>
            )}

            <p className="text-xs text-faded italic">{data.simulatedNote}</p>
          </>
        )}
      </div>
    </PageContainer>
  );
}

function StatusCard({
  label,
  value,
  active,
}: {
  label: string;
  value: string;
  active: boolean;
}) {
  return (
    <div className="border border-edge rounded-lg p-4 bg-card">
      <p className="text-xs text-muted uppercase tracking-wide">{label}</p>
      <p className={`text-sm font-semibold mt-1 ${active ? 'text-accent' : 'text-faded'}`}>
        {value}
      </p>
    </div>
  );
}
