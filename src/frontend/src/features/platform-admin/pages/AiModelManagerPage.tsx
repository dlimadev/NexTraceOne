import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Cpu,
  HardDrive,
  MemoryStick,
  CheckCircle,
  XCircle,
  AlertTriangle,
  Zap,
  Monitor,
} from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { platformAdminApi, type ModelAdvice } from '../api/platformAdmin';

export function AiModelManagerPage() {
  const { t } = useTranslation('aiModelManager');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['hardware-assessment'],
    queryFn: platformAdminApi.getHardwareAssessment,
  });

  return (
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          actions={
            <Button variant="primary" onClick={() => refetch()}>
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
            {/* Hardware Summary */}
            <section>
              <h2 className="text-base font-medium text-heading mb-3">{t('hardwareTitle')}</h2>
              <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
                <HardwareCard
                  icon={<Cpu size={20} className="text-accent" />}
                  label={t('cpu')}
                  value={`${data.cpuModel}`}
                  sub={`${data.cpuCores} ${t('cores')}`}
                />
                <HardwareCard
                  icon={<MemoryStick size={20} className="text-accent" />}
                  label={t('ram')}
                  value={`${data.totalRamGb.toFixed(1)} GB ${t('total')}`}
                  sub={`${data.availableRamGb.toFixed(1)} GB ${t('available')}`}
                />
                <HardwareCard
                  icon={<HardDrive size={20} className="text-success" />}
                  label={t('disk')}
                  value={`${data.diskFreeGb.toFixed(1)} GB`}
                  sub={t('diskFree')}
                />
                <HardwareCard
                  icon={<Zap size={20} className={data.hasGpu ? 'text-warning' : 'text-faded'} />}
                  label={t('gpu')}
                  value={data.hasGpu ? (data.gpuModel ?? t('gpuDetected')) : t('noGpu')}
                  sub={data.hasGpu ? `${data.gpuVramGb} GB VRAM` : t('cpuInference')}
                />
              </div>
              <p className="mt-2 text-xs text-faded">{data.osDescription}</p>
            </section>

            {/* Model Compatibility */}
            <section>
              <h2 className="text-base font-medium text-heading mb-3">{t('modelsTitle')}</h2>
              <div className="bg-card border border-edge rounded-lg overflow-hidden">
                <table className="w-full text-sm">
                  <thead className="bg-elevated border-b border-edge">
                    <tr>
                      <th className="text-left px-4 py-3 font-medium text-muted">{t('colModel')}</th>
                      <th className="text-right px-4 py-3 font-medium text-muted">{t('colSize')}</th>
                      <th className="text-right px-4 py-3 font-medium text-muted">{t('colRam')}</th>
                      <th className="text-right px-4 py-3 font-medium text-muted">{t('colSpeed')}</th>
                      <th className="text-center px-4 py-3 font-medium text-muted">{t('colAccel')}</th>
                      <th className="text-center px-4 py-3 font-medium text-muted">{t('colStatus')}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-edge/50">
                    {data.models.map((m) => (
                      <ModelRow key={m.name} model={m} />
                    ))}
                  </tbody>
                </table>
              </div>
            </section>

            {/* OS Info */}
            <p className="text-xs text-faded">
              {t('assessedAt')}: {new Date(data.assessedAt).toLocaleString()}
            </p>
          </>
        )}
      </div>
    </PageContainer>
  );
}

function HardwareCard({
  icon,
  label,
  value,
  sub,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
  sub: string;
}) {
  return (
    <div className="bg-card border border-edge rounded-lg p-4 flex items-start gap-3">
      <div className="mt-0.5">{icon}</div>
      <div className="min-w-0">
        <p className="text-xs text-muted uppercase tracking-wide">{label}</p>
        <p className="text-sm font-medium text-heading truncate" title={value}>{value}</p>
        <p className="text-xs text-faded">{sub}</p>
      </div>
    </div>
  );
}

function ModelRow({ model }: { model: ModelAdvice }) {
  const compatible = model.status === 'Compatible';
  return (
    <tr className={compatible ? '' : 'opacity-60 bg-elevated'}>
      <td className="px-4 py-3">
        <p className="font-mono text-xs text-body">{model.name}</p>
        <p className="text-xs text-faded mt-0.5">{model.description}</p>
        {model.warning && (
          <p className="text-xs text-warning mt-0.5 flex items-center gap-1">
            <AlertTriangle size={11} />
            {model.warning}
          </p>
        )}
      </td>
      <td className="px-4 py-3 text-right text-xs text-muted">{model.sizeGb.toFixed(1)} GB</td>
      <td className="px-4 py-3 text-right text-xs text-muted">{model.requiredRamGb.toFixed(1)} GB</td>
      <td className="px-4 py-3 text-right text-xs text-muted">~{model.estTokPerSec} tok/s</td>
      <td className="px-4 py-3 text-center">
        {model.acceleratedByGpu ? (
          <span className="inline-flex items-center gap-1 text-xs text-warning bg-warning/10 px-2 py-0.5 rounded-full">
            <Zap size={10} />
            GPU
          </span>
        ) : (
          <span className="text-xs text-faded flex items-center justify-center gap-1">
            <Monitor size={10} />
            CPU
          </span>
        )}
      </td>
      <td className="px-4 py-3 text-center">
        {compatible ? (
          <CheckCircle size={16} className="text-success mx-auto" />
        ) : (
          <XCircle size={16} className="text-critical mx-auto" />
        )}
      </td>
    </tr>
  );
}
