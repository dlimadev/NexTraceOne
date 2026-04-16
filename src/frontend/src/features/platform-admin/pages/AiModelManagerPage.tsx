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
import { platformAdminApi, type ModelAdvice } from '../api/platformAdmin';

export function AiModelManagerPage() {
  const { t } = useTranslation('aiModelManager');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['hardware-assessment'],
    queryFn: platformAdminApi.getHardwareAssessment,
  });

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
          <p className="mt-1 text-sm text-slate-500">{t('subtitle')}</p>
        </div>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 text-sm bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
        >
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
          {/* Hardware Summary */}
          <section>
            <h2 className="text-base font-medium text-slate-800 mb-3">{t('hardwareTitle')}</h2>
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
              <HardwareCard
                icon={<Cpu size={20} className="text-indigo-500" />}
                label={t('cpu')}
                value={`${data.cpuModel}`}
                sub={`${data.cpuCores} ${t('cores')}`}
              />
              <HardwareCard
                icon={<MemoryStick size={20} className="text-purple-500" />}
                label={t('ram')}
                value={`${data.totalRamGb.toFixed(1)} GB ${t('total')}`}
                sub={`${data.availableRamGb.toFixed(1)} GB ${t('available')}`}
              />
              <HardwareCard
                icon={<HardDrive size={20} className="text-emerald-500" />}
                label={t('disk')}
                value={`${data.diskFreeGb.toFixed(1)} GB`}
                sub={t('diskFree')}
              />
              <HardwareCard
                icon={<Zap size={20} className={data.hasGpu ? 'text-yellow-500' : 'text-slate-400'} />}
                label={t('gpu')}
                value={data.hasGpu ? (data.gpuModel ?? t('gpuDetected')) : t('noGpu')}
                sub={data.hasGpu ? `${data.gpuVramGb} GB VRAM` : t('cpuInference')}
              />
            </div>
            <p className="mt-2 text-xs text-slate-400">{data.osDescription}</p>
          </section>

          {/* Model Compatibility */}
          <section>
            <h2 className="text-base font-medium text-slate-800 mb-3">{t('modelsTitle')}</h2>
            <div className="bg-white border border-slate-200 rounded-lg overflow-hidden">
              <table className="w-full text-sm">
                <thead className="bg-slate-50 border-b border-slate-200">
                  <tr>
                    <th className="text-left px-4 py-3 font-medium text-slate-600">{t('colModel')}</th>
                    <th className="text-right px-4 py-3 font-medium text-slate-600">{t('colSize')}</th>
                    <th className="text-right px-4 py-3 font-medium text-slate-600">{t('colRam')}</th>
                    <th className="text-right px-4 py-3 font-medium text-slate-600">{t('colSpeed')}</th>
                    <th className="text-center px-4 py-3 font-medium text-slate-600">{t('colAccel')}</th>
                    <th className="text-center px-4 py-3 font-medium text-slate-600">{t('colStatus')}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {data.models.map((m) => (
                    <ModelRow key={m.name} model={m} t={t} />
                  ))}
                </tbody>
              </table>
            </div>
          </section>

          {/* OS Info */}
          <p className="text-xs text-slate-400">
            {t('assessedAt')}: {new Date(data.assessedAt).toLocaleString()}
          </p>
        </>
      )}
    </div>
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
    <div className="bg-white border border-slate-200 rounded-lg p-4 flex items-start gap-3">
      <div className="mt-0.5">{icon}</div>
      <div className="min-w-0">
        <p className="text-xs text-slate-500 uppercase tracking-wide">{label}</p>
        <p className="text-sm font-medium text-slate-800 truncate" title={value}>{value}</p>
        <p className="text-xs text-slate-400">{sub}</p>
      </div>
    </div>
  );
}

function ModelRow({ model, t }: { model: ModelAdvice; t: (key: string) => string }) {
  const compatible = model.status === 'Compatible';
  return (
    <tr className={compatible ? '' : 'opacity-60 bg-slate-50'}>
      <td className="px-4 py-3">
        <p className="font-mono text-xs text-slate-700">{model.name}</p>
        <p className="text-xs text-slate-400 mt-0.5">{model.description}</p>
        {model.warning && (
          <p className="text-xs text-amber-600 mt-0.5 flex items-center gap-1">
            <AlertTriangle size={11} />
            {model.warning}
          </p>
        )}
      </td>
      <td className="px-4 py-3 text-right text-xs text-slate-600">{model.sizeGb.toFixed(1)} GB</td>
      <td className="px-4 py-3 text-right text-xs text-slate-600">{model.requiredRamGb.toFixed(1)} GB</td>
      <td className="px-4 py-3 text-right text-xs text-slate-600">~{model.estTokPerSec} tok/s</td>
      <td className="px-4 py-3 text-center">
        {model.acceleratedByGpu ? (
          <span className="inline-flex items-center gap-1 text-xs text-yellow-700 bg-yellow-50 px-2 py-0.5 rounded-full">
            <Zap size={10} />
            GPU
          </span>
        ) : (
          <span className="text-xs text-slate-400 flex items-center justify-center gap-1">
            <Monitor size={10} />
            CPU
          </span>
        )}
      </td>
      <td className="px-4 py-3 text-center">
        {compatible ? (
          <CheckCircle size={16} className="text-emerald-500 mx-auto" />
        ) : (
          <XCircle size={16} className="text-red-400 mx-auto" />
        )}
      </td>
    </tr>
  );
}
