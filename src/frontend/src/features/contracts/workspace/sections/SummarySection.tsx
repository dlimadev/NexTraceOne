import { useTranslation } from 'react-i18next';
import {
  Globe, GitBranch, Shield, Users, ArrowUpRight,
  AlertTriangle, Clock, Package, Activity, Network,
} from 'lucide-react';
import { cn } from '../../../../lib/cn';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import { LifecycleBadge } from '../../shared/components/LifecycleBadge';
import { ProtocolBadge } from '../../shared/components/ProtocolBadge';
import { ServiceTypeBadge } from '../../shared/components/ServiceTypeBadge';
import type { StudioContract } from '../studioTypes';
import type { WorkspaceSectionId } from '../../types/workspace';

interface SummarySectionProps {
  contract: StudioContract;
  violationCount?: number;
  onNavigate?: (section: WorkspaceSectionId) => void;
  className?: string;
}

/**
 * Secção Resumo do studio — visão geral rica do contrato.
 * Mostra identidade, organização, governança, riscos, relações e actividade recente.
 */
export function SummarySection({
  contract,
  violationCount = 0,
  onNavigate,
  className = '',
}: SummarySectionProps) {
  const { t } = useTranslation();

  return (
    <div className={cn('space-y-6', className)}>
      {/* ── Identity Card ── */}
      <Card>
        <CardBody>
          <div className="flex items-start justify-between gap-4 mb-4">
            <div className="min-w-0">
              <h2 className="text-base font-bold text-heading mb-1">{contract.friendlyName}</h2>
              <p className="text-xs font-mono text-muted/70 mb-2">{contract.technicalName}</p>
              <p className="text-xs text-body leading-relaxed">{contract.functionalDescription}</p>
            </div>
            <div className="flex items-center gap-1.5 flex-shrink-0">
              <ServiceTypeBadge type={contract.serviceType} />
              <ProtocolBadge protocol={contract.protocol} />
            </div>
          </div>

          <div className="flex items-center gap-1.5 flex-wrap">
            <LifecycleBadge state={contract.lifecycleState} />
            <span className="text-xs font-mono text-accent bg-accent/10 px-2 py-0.5 rounded-md border border-accent/20">
              v{contract.semVer}
            </span>
            {contract.isLocked && (
              <span className="px-2 py-0.5 text-[10px] rounded-md bg-accent/10 text-accent border border-accent/20 font-medium">
                {t('contracts.locked', 'Locked')}
              </span>
            )}
            {contract.signedBy && (
              <span className="px-2 py-0.5 text-[10px] rounded-md bg-mint/10 text-mint border border-mint/20 font-medium">
                {t('contracts.signed', 'Signed')}
              </span>
            )}
          </div>
        </CardBody>
      </Card>

      {/* ── Key Metrics ── */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
        <MetricCard
          label={t('contracts.studio.summary.compliance', 'Compliance')}
          value={`${contract.complianceScore}%`}
          variant={contract.complianceScore >= 80 ? 'success' : contract.complianceScore >= 60 ? 'warning' : 'danger'}
          icon={<Shield size={14} />}
          onClick={() => onNavigate?.('compliance')}
        />
        <MetricCard
          label={t('contracts.studio.summary.violations', 'Violations')}
          value={String(violationCount)}
          variant={violationCount === 0 ? 'success' : 'danger'}
          icon={<AlertTriangle size={14} />}
          onClick={() => onNavigate?.('compliance')}
        />
        <MetricCard
          label={t('contracts.studio.summary.consumers', 'Consumers')}
          value={String(contract.consumers.length)}
          variant="neutral"
          icon={<Users size={14} />}
          onClick={() => onNavigate?.('consumers')}
        />
        <MetricCard
          label={t('contracts.studio.summary.dependencies', 'Dependencies')}
          value={String(contract.dependencies.length)}
          variant="neutral"
          icon={<Network size={14} />}
          onClick={() => onNavigate?.('dependencies')}
        />
      </div>

      {/* ── Organization ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Package size={14} className="text-accent" />
            <h3 className="text-xs font-semibold text-heading">
              {t('contracts.studio.summary.organization', 'Organization')}
            </h3>
          </div>
        </CardHeader>
        <CardBody>
          <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
            <InfoField label={t('contracts.studio.summary.domain', 'Domain')} value={contract.domain} />
            <InfoField label={t('contracts.studio.summary.product', 'Product')} value={contract.product} />
            <InfoField label={t('contracts.studio.summary.capability', 'Capability')} value={contract.capability} />
            <InfoField label={t('contracts.studio.summary.owner', 'Owner')} value={`@${contract.owner}`} />
            <InfoField label={t('contracts.studio.summary.team', 'Team')} value={contract.team} />
            <InfoField label={t('contracts.studio.summary.criticality', 'Criticality')} value={contract.criticality} />
          </div>
        </CardBody>
      </Card>

      {/* ── Quick Actions ── */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-2">
        <QuickActionButton
          label={t('contracts.studio.summary.goToContract', 'View Contract')}
          icon={<Globe size={14} />}
          onClick={() => onNavigate?.('contract')}
        />
        <QuickActionButton
          label={t('contracts.studio.summary.goToOperations', 'Operations')}
          icon={<GitBranch size={14} />}
          onClick={() => onNavigate?.('operations')}
        />
        <QuickActionButton
          label={t('contracts.studio.summary.goToSecurity', 'Security')}
          icon={<Shield size={14} />}
          onClick={() => onNavigate?.('security')}
        />
        <QuickActionButton
          label={t('contracts.studio.summary.goToVersioning', 'Versioning')}
          icon={<Clock size={14} />}
          onClick={() => onNavigate?.('versioning')}
        />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {/* ── Consumers / Producers ── */}
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Users size={14} className="text-accent" />
                <h3 className="text-xs font-semibold text-heading">
                  {t('contracts.studio.summary.consumersProducers', 'Consumers & Producers')}
                </h3>
              </div>
              <button
                onClick={() => onNavigate?.('consumers')}
                className="text-[10px] text-accent hover:underline"
              >
                {t('common.viewAll', 'View all')} →
              </button>
            </div>
          </CardHeader>
          <CardBody className="p-0">
            {contract.consumers.length === 0 && contract.producers.length === 0 ? (
              <p className="px-6 py-4 text-xs text-muted">{t('contracts.studio.summary.noRelations', 'No consumers or producers registered.')}</p>
            ) : (
              <div className="divide-y divide-edge">
                {contract.consumers.slice(0, 3).map((c) => (
                  <div key={c.id} className="flex items-center gap-2 px-6 py-2.5 text-xs">
                    <ArrowUpRight size={10} className="text-cyan" />
                    <span className="text-body flex-1">{c.name}</span>
                    <span className="text-[10px] text-muted">{c.type}</span>
                  </div>
                ))}
                {contract.producers.slice(0, 2).map((p) => (
                  <div key={p.id} className="flex items-center gap-2 px-6 py-2.5 text-xs">
                    <ArrowUpRight size={10} className="text-mint rotate-180" />
                    <span className="text-body flex-1">{p.name}</span>
                    <span className="text-[10px] text-muted">Producer</span>
                  </div>
                ))}
              </div>
            )}
          </CardBody>
        </Card>

        {/* ── Recent Activity ── */}
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Activity size={14} className="text-accent" />
                <h3 className="text-xs font-semibold text-heading">
                  {t('contracts.studio.summary.recentChanges', 'Recent Changes')}
                </h3>
              </div>
              <button
                onClick={() => onNavigate?.('changelog')}
                className="text-[10px] text-accent hover:underline"
              >
                {t('common.viewAll', 'View all')} →
              </button>
            </div>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {contract.recentActivity.slice(0, 4).map((item) => (
                <div key={item.id} className="flex items-start gap-2 px-6 py-2.5">
                  <div className="w-1.5 h-1.5 rounded-full bg-accent mt-1.5 flex-shrink-0" />
                  <div className="min-w-0">
                    <p className="text-xs text-body">{item.action}</p>
                    <p className="text-[10px] text-muted">
                      @{item.actor.split('.')[0]} · {formatDate(item.timestamp)}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>
      </div>

      {/* ── Risks ── */}
      {contract.risks.length > 0 && (
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <AlertTriangle size={14} className="text-warning" />
              <h3 className="text-xs font-semibold text-heading">
                {t('contracts.studio.summary.risks', 'Identified Risks')}
              </h3>
              <span className="text-[10px] text-muted">({contract.risks.length})</span>
            </div>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {contract.risks.map((risk) => (
                <div key={risk.id} className="flex items-start gap-3 px-6 py-3 text-xs">
                  <span className={cn(
                    'w-2 h-2 rounded-full mt-1 flex-shrink-0',
                    risk.level === 'Critical' ? 'bg-danger' :
                    risk.level === 'High' ? 'bg-warning' :
                    risk.level === 'Medium' ? 'bg-cyan' :
                    'bg-muted',
                  )} />
                  <div className="min-w-0 flex-1">
                    <p className="text-body">{risk.description}</p>
                    <div className="flex items-center gap-2 mt-0.5">
                      <span className={cn(
                        'text-[10px] font-medium',
                        risk.level === 'Critical' ? 'text-danger' :
                        risk.level === 'High' ? 'text-warning' :
                        'text-muted',
                      )}>{risk.level}</span>
                      <span className="text-[10px] text-muted">·</span>
                      <span className="text-[10px] text-muted">{risk.category}</span>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>
      )}

      {/* ── Deprecation Notice ── */}
      {contract.deprecationNotice && (
        <div className="flex items-start gap-3 p-4 rounded-lg border border-warning/25 bg-warning/5">
          <AlertTriangle size={16} className="text-warning flex-shrink-0 mt-0.5" />
          <div>
            <p className="text-xs font-semibold text-heading mb-0.5">
              {t('contracts.deprecation.title', 'Deprecation Notice')}
            </p>
            <p className="text-xs text-body">{contract.deprecationNotice}</p>
            {contract.sunsetDate && (
              <p className="text-[10px] text-muted mt-1">
                {t('contracts.portal.sunsetDate', 'Sunset date')}: {formatDate(contract.sunsetDate)}
              </p>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

// ── Internal components ───────────────────────────────────────────────────────

function MetricCard({
  label,
  value,
  variant,
  icon,
  onClick,
}: {
  label: string;
  value: string;
  variant: 'success' | 'warning' | 'danger' | 'neutral';
  icon: React.ReactNode;
  onClick?: () => void;
}) {
  const colors = {
    success: 'border-mint/20 text-mint',
    warning: 'border-warning/20 text-warning',
    danger: 'border-danger/20 text-danger',
    neutral: 'border-edge text-body',
  };

  return (
    <button
      onClick={onClick}
      className={cn(
        'flex items-center gap-3 px-4 py-3 rounded-lg border bg-card hover:bg-elevated/50 transition-colors text-left',
        colors[variant],
      )}
    >
      <div className="opacity-70">{icon}</div>
      <div>
        <p className="text-[10px] text-muted mb-0.5">{label}</p>
        <p className="text-sm font-bold">{value}</p>
      </div>
    </button>
  );
}

function QuickActionButton({
  label,
  icon,
  onClick,
}: {
  label: string;
  icon: React.ReactNode;
  onClick?: () => void;
}) {
  return (
    <button
      onClick={onClick}
      className="flex items-center gap-2 px-3 py-2.5 text-xs font-medium rounded-md bg-elevated border border-edge hover:border-accent/40 hover:bg-accent/5 text-body transition-colors"
    >
      <span className="text-accent">{icon}</span>
      {label}
    </button>
  );
}

function InfoField({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[10px] text-muted mb-0.5">{label}</p>
      <p className="text-xs text-body font-medium">{value}</p>
    </div>
  );
}

function formatDate(dateStr: string): string {
  try {
    return new Date(dateStr).toLocaleDateString(undefined, {
      year: 'numeric', month: 'short', day: 'numeric',
    });
  } catch {
    return dateStr;
  }
}
