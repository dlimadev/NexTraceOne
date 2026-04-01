import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Rocket, CheckCircle2, XCircle, RotateCcw, Clock, Tag, User, Cpu } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import { EmptyState } from '../../../../components/EmptyState';
import { contractsApi } from '../../api/contracts';
import type { ContractDeploymentItem } from '../../../../types';

interface DeploymentsSectionProps {
  contractVersionId: string;
  className?: string;
}

// ── Status badge ────────────────────────────────────────────────────────────

const STATUS_CONFIG: Record<
  ContractDeploymentItem['status'],
  { icon: React.ComponentType<{ size?: number; className?: string }>; color: string; labelKey: string }
> = {
  Success:  { icon: CheckCircle2,  color: 'text-mint',   labelKey: 'contracts.deployments.status.success' },
  Failed:   { icon: XCircle,       color: 'text-error',  labelKey: 'contracts.deployments.status.failed' },
  Rollback: { icon: RotateCcw,     color: 'text-amber',  labelKey: 'contracts.deployments.status.rollback' },
  Pending:  { icon: Clock,         color: 'text-muted',  labelKey: 'contracts.deployments.status.pending' },
};

function StatusBadge({ status }: { status: ContractDeploymentItem['status'] }) {
  const { t } = useTranslation();
  const cfg = STATUS_CONFIG[status] ?? STATUS_CONFIG.Pending;
  const Icon = cfg.icon;
  return (
    <span className={`inline-flex items-center gap-1 text-[10px] font-medium ${cfg.color}`}>
      <Icon size={12} />
      {t(cfg.labelKey, status)}
    </span>
  );
}

// ── Environment chip ────────────────────────────────────────────────────────

function EnvChip({ env }: { env: string }) {
  const isProd = /^prod(uction)?$/i.test(env);
  return (
    <span
      className={`text-[10px] font-mono px-1.5 py-0.5 rounded border ${
        isProd
          ? 'text-error border-error/40 bg-error/5'
          : 'text-cyan border-cyan/30 bg-cyan/5'
      }`}
    >
      {env}
    </span>
  );
}

// ── Relative date ───────────────────────────────────────────────────────────

function relativeDate(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime();
  const mins = Math.floor(diff / 60_000);
  if (mins < 2) return 'just now';
  if (mins < 60) return `${mins}m ago`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h ago`;
  const days = Math.floor(hrs / 24);
  return `${days}d ago`;
}

// ── Deployment row ──────────────────────────────────────────────────────────

function DeploymentRow({ item }: { item: ContractDeploymentItem }) {
  return (
    <div className="px-4 py-3 space-y-1.5">
      <div className="flex items-center justify-between gap-3 flex-wrap">
        <div className="flex items-center gap-2 flex-wrap">
          <EnvChip env={item.environment} />
          <span className="font-mono text-[10px] text-muted">v{item.semVer}</span>
          <StatusBadge status={item.status} />
        </div>
        <span className="text-[10px] text-muted whitespace-nowrap">
          {relativeDate(item.deployedAt)}
        </span>
      </div>

      <div className="flex items-center gap-4 flex-wrap">
        <span className="flex items-center gap-1 text-[10px] text-muted">
          <User size={10} />
          {item.deployedBy}
        </span>
        <span className="flex items-center gap-1 text-[10px] text-muted">
          <Cpu size={10} />
          {item.sourceSystem}
        </span>
        {item.notes && (
          <span className="flex items-center gap-1 text-[10px] text-muted italic">
            <Tag size={10} />
            {item.notes}
          </span>
        )}
      </div>
    </div>
  );
}

// ── Main section ────────────────────────────────────────────────────────────

/**
 * Secção de Deployments do workspace de contrato.
 * Mostra o histórico de deployments desta versão por ambiente.
 * Alimenta rastreabilidade de mudanças e Change Intelligence.
 */
export function DeploymentsSection({ contractVersionId, className = '' }: DeploymentsSectionProps) {
  const { t } = useTranslation();

  const { data, isLoading, isError } = useQuery({
    queryKey: ['contract-deployments', contractVersionId],
    queryFn: () => contractsApi.getDeployments(contractVersionId),
    enabled: !!contractVersionId,
    staleTime: 30_000,
  });

  const deployments = data?.deployments ?? [];

  // ── Environment summary ─────────────────────────────────────────────────

  const envSet = Array.from(new Set(deployments.map((d) => d.environment)));
  const lastByEnv = envSet.map((env) => {
    const latest = deployments.find((d) => d.environment === env);
    return { env, status: latest?.status ?? 'Pending' as const };
  });

  return (
    <div className={`space-y-4 ${className}`}>

      {/* Environment status strip */}
      {lastByEnv.length > 0 && (
        <div className="flex items-center gap-2 flex-wrap">
          {lastByEnv.map(({ env, status }) => {
            const cfg = STATUS_CONFIG[status] ?? STATUS_CONFIG.Pending;
            const Icon = cfg.icon;
            return (
              <span
                key={env}
                className="inline-flex items-center gap-1.5 text-[10px] px-2 py-1 rounded-full bg-elevated border border-edge"
              >
                <Icon size={11} className={cfg.color} />
                <span className="text-body font-medium">{env}</span>
                <span className={`font-medium ${cfg.color}`}>
                  {t(cfg.labelKey, status)}
                </span>
              </span>
            );
          })}
        </div>
      )}

      {/* Deployment history */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Rocket size={14} className="text-accent" />
            <h3 className="text-xs font-semibold text-heading">
              {t('contracts.deployments.historyTitle', 'Deployment History')}
            </h3>
            <span className="text-[10px] text-muted px-2 py-0.5 rounded-full bg-elevated border border-edge">
              {deployments.length}
            </span>
          </div>
        </CardHeader>
        <CardBody className="p-0">
          {isLoading && (
            <div className="p-6 space-y-3">
              {[1, 2, 3].map((i) => (
                <div key={i} className="h-10 rounded bg-elevated animate-pulse" />
              ))}
            </div>
          )}

          {isError && (
            <div className="p-6">
              <p className="text-xs text-error">
                {t('contracts.deployments.loadError', 'Failed to load deployment history.')}
              </p>
            </div>
          )}

          {!isLoading && !isError && deployments.length === 0 && (
            <div className="p-6">
              <EmptyState
                title={t('contracts.deployments.noDeployments', 'No deployments registered')}
                description={t(
                  'contracts.deployments.noDeploymentsDescription',
                  'Deployment events for this contract version will appear here.',
                )}
                icon={<Rocket size={20} />}
                size="compact"
              />
            </div>
          )}

          {!isLoading && !isError && deployments.length > 0 && (
            <div className="divide-y divide-edge">
              {deployments.map((item) => (
                <DeploymentRow key={item.deploymentId} item={item} />
              ))}
            </div>
          )}
        </CardBody>
      </Card>
    </div>
  );
}
