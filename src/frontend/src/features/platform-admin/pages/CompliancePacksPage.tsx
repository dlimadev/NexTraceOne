import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { ClipboardCheck, RefreshCw, XCircle, CheckCircle, AlertTriangle, ChevronDown, ChevronRight } from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { platformAdminApi, type CompliancePack, type ComplianceControl } from '../api/platformAdmin';

export function CompliancePacksPage() {
  const { t } = useTranslation('compliancePacks');
  const [expandedPack, setExpandedPack] = useState<string | null>(null);

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['compliance-packs'],
    queryFn: platformAdminApi.getCompliancePacks,
  });

  return (
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          icon={<ClipboardCheck size={24} className="text-accent" />}
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

            <p className="text-xs text-faded italic">{data.simulatedNote}</p>
          </>
        )}
      </div>
    </PageContainer>
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
      ? 'text-success bg-success/10 border-success/20'
      : pack.compliancePercent >= 70
        ? 'text-warning bg-warning/10 border-warning/20'
        : 'text-critical bg-critical/10 border-critical/20';

  return (
    <div className="border border-edge rounded-lg overflow-hidden">
      {/* Pack header */}
      <button
        onClick={onToggle}
        className="w-full flex items-center justify-between p-5 hover:bg-elevated text-left"
        aria-label={`${t('togglePack')} ${pack.name}`}
      >
        <div className="flex items-center gap-4">
          <div>
            <p className="font-semibold text-heading">{pack.name}</p>
            <p className="text-xs text-faded">{pack.standard} · {pack.version}</p>
          </div>
          <span className={`px-2.5 py-1 text-sm font-semibold rounded-full border ${statusColor}`}>
            {pack.compliancePercent.toFixed(0)}%
          </span>
        </div>
        <div className="flex items-center gap-6">
          <div className="hidden md:flex gap-6 text-sm text-muted">
            <span className="flex items-center gap-1">
              <CheckCircle size={14} className="text-success" />
              {t('passing', { count: pack.passingControls })}
            </span>
            <span className="flex items-center gap-1">
              <XCircle size={14} className="text-critical" />
              {t('failing', { count: pack.failingControls })}
            </span>
            {pack.warningControls > 0 && (
              <span className="flex items-center gap-1">
                <AlertTriangle size={14} className="text-warning" />
                {t('warnings', { count: pack.warningControls })}
              </span>
            )}
          </div>
          {expanded ? <ChevronDown size={16} className="text-muted" /> : <ChevronRight size={16} className="text-muted" />}
        </div>
      </button>

      {/* Progress bar */}
      <div className="h-1 bg-elevated">
        <div
          className={`h-full transition-all ${pack.compliancePercent >= 90 ? 'bg-success' : pack.compliancePercent >= 70 ? 'bg-warning' : 'bg-critical'}`}
          style={{ width: `${pack.compliancePercent}%` }}
        />
      </div>

      {/* Controls list */}
      {expanded && (
        <div className="divide-y divide-edge/50 border-t border-edge">
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
    Pass: <CheckCircle size={16} className="text-success shrink-0" />,
    Fail: <XCircle size={16} className="text-critical shrink-0" />,
    Warning: <AlertTriangle size={16} className="text-warning shrink-0" />,
    NotApplicable: <span className="w-4 h-4 rounded-full border border-edge inline-block shrink-0" />,
  }[control.status];

  return (
    <div className="px-5 py-4 flex items-start gap-3">
      {statusIcon}
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2">
          <span className="text-xs font-mono text-faded shrink-0">{control.code}</span>
          <span className="text-sm font-medium text-heading">{control.title}</span>
        </div>
        <p className="text-xs text-muted mt-0.5">{control.description}</p>
        {control.actionRequired && (
          <p className="text-xs text-warning mt-1">
            → {control.actionRequired}
          </p>
        )}
        {control.evidence && (
          <p className="text-xs text-success mt-1">
            ✓ {control.evidence}
          </p>
        )}
      </div>
      <span
        className={`px-2 py-0.5 text-xs rounded border shrink-0 ${
          control.status === 'Pass'
            ? 'text-success bg-success/10 border-success/20'
            : control.status === 'Fail'
              ? 'text-critical bg-critical/10 border-critical/20'
              : control.status === 'Warning'
                ? 'text-warning bg-warning/10 border-warning/20'
                : 'text-muted bg-elevated border-edge'
        }`}
      >
        {t(`status.${control.status}`)}
      </span>
    </div>
  );
}
