import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { ClipboardCheck, RefreshCw, XCircle, CheckCircle, AlertTriangle, ChevronDown, ChevronRight } from 'lucide-react';
import { platformAdminApi, type CompliancePack, type ComplianceControl } from '../api/platformAdmin';

export function CompliancePacksPage() {
  const { t } = useTranslation('compliancePacks');
  const [expandedPack, setExpandedPack] = useState<string | null>(null);

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['compliance-packs'],
    queryFn: platformAdminApi.getCompliancePacks,
  });

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <ClipboardCheck size={24} className="text-teal-600" />
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
          {/* Pack cards */}
          <div className="space-y-4">
            {data.packs.map((pack) => (
              <PackCard
                key={pack.id}
                pack={pack}
                expanded={expandedPack === pack.id}
                onToggle={() => setExpandedPack(expandedPack === pack.id ? null : pack.id)}
                t={t}
              />
            ))}
          </div>

          <p className="text-xs text-slate-400 italic">{data.simulatedNote}</p>
        </>
      )}
    </div>
  );
}

function PackCard({
  pack,
  expanded,
  onToggle,
  t,
}: {
  pack: CompliancePack;
  expanded: boolean;
  onToggle: () => void;
  t: (key: string, opts?: Record<string, unknown>) => string;
}) {
  const statusColor =
    pack.compliancePercent >= 90
      ? 'text-emerald-600 bg-emerald-50 border-emerald-200'
      : pack.compliancePercent >= 70
        ? 'text-amber-600 bg-amber-50 border-amber-200'
        : 'text-red-600 bg-red-50 border-red-200';

  return (
    <div className="border border-slate-200 rounded-lg overflow-hidden">
      {/* Pack header */}
      <button
        onClick={onToggle}
        className="w-full flex items-center justify-between p-5 hover:bg-slate-50 text-left"
        aria-label={`${t('togglePack')} ${pack.name}`}
      >
        <div className="flex items-center gap-4">
          <div>
            <p className="font-semibold text-slate-800">{pack.name}</p>
            <p className="text-xs text-slate-400">{pack.standard} · {pack.version}</p>
          </div>
          <span className={`px-2.5 py-1 text-sm font-semibold rounded-full border ${statusColor}`}>
            {pack.compliancePercent.toFixed(0)}%
          </span>
        </div>
        <div className="flex items-center gap-6">
          <div className="hidden md:flex gap-6 text-sm text-slate-500">
            <span className="flex items-center gap-1">
              <CheckCircle size={14} className="text-emerald-500" />
              {t('passing', { count: pack.passingControls })}
            </span>
            <span className="flex items-center gap-1">
              <XCircle size={14} className="text-red-500" />
              {t('failing', { count: pack.failingControls })}
            </span>
            {pack.warningControls > 0 && (
              <span className="flex items-center gap-1">
                <AlertTriangle size={14} className="text-amber-500" />
                {t('warnings', { count: pack.warningControls })}
              </span>
            )}
          </div>
          {expanded ? <ChevronDown size={16} className="text-slate-400" /> : <ChevronRight size={16} className="text-slate-400" />}
        </div>
      </button>

      {/* Progress bar */}
      <div className="h-1 bg-slate-100">
        <div
          className={`h-full transition-all ${pack.compliancePercent >= 90 ? 'bg-emerald-500' : pack.compliancePercent >= 70 ? 'bg-amber-500' : 'bg-red-500'}`}
          style={{ width: `${pack.compliancePercent}%` }}
        />
      </div>

      {/* Controls list */}
      {expanded && (
        <div className="divide-y divide-slate-100 border-t border-slate-200">
          {pack.controls.map((control) => (
            <ControlRow key={control.id} control={control} t={t} />
          ))}
        </div>
      )}
    </div>
  );
}

function ControlRow({
  control,
  t,
}: {
  control: ComplianceControl;
  t: (key: string) => string;
}) {
  const statusIcon = {
    Pass: <CheckCircle size={16} className="text-emerald-500 shrink-0" />,
    Fail: <XCircle size={16} className="text-red-500 shrink-0" />,
    Warning: <AlertTriangle size={16} className="text-amber-500 shrink-0" />,
    NotApplicable: <span className="w-4 h-4 rounded-full border border-slate-300 inline-block shrink-0" />,
  }[control.status];

  return (
    <div className="px-5 py-4 flex items-start gap-3">
      {statusIcon}
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2">
          <span className="text-xs font-mono text-slate-400 shrink-0">{control.code}</span>
          <span className="text-sm font-medium text-slate-800">{control.title}</span>
        </div>
        <p className="text-xs text-slate-500 mt-0.5">{control.description}</p>
        {control.actionRequired && (
          <p className="text-xs text-amber-700 mt-1">
            → {control.actionRequired}
          </p>
        )}
        {control.evidence && (
          <p className="text-xs text-emerald-700 mt-1">
            ✓ {control.evidence}
          </p>
        )}
      </div>
      <span
        className={`px-2 py-0.5 text-xs rounded border shrink-0 ${
          control.status === 'Pass'
            ? 'text-emerald-700 bg-emerald-50 border-emerald-200'
            : control.status === 'Fail'
              ? 'text-red-700 bg-red-50 border-red-200'
              : control.status === 'Warning'
                ? 'text-amber-700 bg-amber-50 border-amber-200'
                : 'text-slate-500 bg-slate-50 border-slate-200'
        }`}
      >
        {t(`status.${control.status}`)}
      </span>
    </div>
  );
}
