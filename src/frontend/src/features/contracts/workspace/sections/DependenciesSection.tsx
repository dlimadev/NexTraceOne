import { useTranslation } from 'react-i18next';
import { Network, ArrowUp, ArrowDown, ExternalLink, Workflow } from 'lucide-react';
import { cn } from '../../../../lib/cn';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import { EmptyState } from '../../../../components/EmptyState';
import type { StudioContract, StudioDependency } from '../studioTypes';

interface DependenciesSectionProps {
  contract: StudioContract;
  className?: string;
}

/**
 * Secção de Dependencies do studio — dependências upstream e downstream.
 * Mostra nome, direcção, tipo e permite navegação para serviços relacionados.
 */
export function DependenciesSection({ contract, className = '' }: DependenciesSectionProps) {
  const { t } = useTranslation();

  const upstream = contract.dependencies.filter((d) => d.direction === 'Upstream');
  const downstream = contract.dependencies.filter((d) => d.direction === 'Downstream');

  if (contract.dependencies.length === 0) {
    return (
      <div className={className}>
        <EmptyState
          title={t('contracts.studio.dependencies.emptyTitle', 'No dependencies mapped')}
          description={t('contracts.studio.dependencies.emptyDescription', 'Dependencies between this contract and other services will appear here.')}
          icon={<Network size={24} />}
        />
      </div>
    );
  }

  return (
    <div className={`space-y-6 ${className}`}>
      {/* ── Overview ── */}
      <div className="grid grid-cols-3 gap-3">
        <div className="rounded-lg border border-edge bg-card px-4 py-3">
          <p className="text-[10px] text-muted mb-0.5">{t('contracts.studio.dependencies.total', 'Total')}</p>
          <p className="text-lg font-bold text-heading">{contract.dependencies.length}</p>
        </div>
        <div className="rounded-lg border border-cyan/20 bg-cyan/5 px-4 py-3">
          <p className="text-[10px] text-muted mb-0.5">{t('contracts.studio.dependencies.upstream', 'Upstream')}</p>
          <p className="text-lg font-bold text-cyan">{upstream.length}</p>
        </div>
        <div className="rounded-lg border border-mint/20 bg-mint/5 px-4 py-3">
          <p className="text-[10px] text-muted mb-0.5">{t('contracts.studio.dependencies.downstream', 'Downstream')}</p>
          <p className="text-lg font-bold text-mint">{downstream.length}</p>
        </div>
      </div>

      {/* ── Upstream ── */}
      {upstream.length > 0 && (
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <ArrowUp size={14} className="text-cyan" />
              <h3 className="text-xs font-semibold text-heading">
                {t('contracts.studio.dependencies.upstreamTitle', 'Upstream Dependencies')}
              </h3>
              <span className="text-[10px] text-muted">({upstream.length})</span>
            </div>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {upstream.map((dep) => (
                <DependencyRow key={dep.id} dependency={dep} />
              ))}
            </div>
          </CardBody>
        </Card>
      )}

      {/* ── Downstream ── */}
      {downstream.length > 0 && (
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <ArrowDown size={14} className="text-mint" />
              <h3 className="text-xs font-semibold text-heading">
                {t('contracts.studio.dependencies.downstreamTitle', 'Downstream Dependencies')}
              </h3>
              <span className="text-[10px] text-muted">({downstream.length})</span>
            </div>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {downstream.map((dep) => (
                <DependencyRow key={dep.id} dependency={dep} />
              ))}
            </div>
          </CardBody>
        </Card>
      )}

      {/* ── Topology hint ── */}
      <div className="flex items-center gap-3 p-4 rounded-lg border border-edge bg-elevated/30">
        <Workflow size={16} className="text-accent flex-shrink-0" />
        <div>
          <p className="text-xs text-heading font-medium">
            {t('contracts.studio.dependencies.topologyHint', 'Service Topology')}
          </p>
          <p className="text-[10px] text-muted">
            {t('contracts.studio.dependencies.topologyDescription', 'A visual dependency graph will be available in a future release.')}
          </p>
        </div>
      </div>
    </div>
  );
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function DependencyRow({ dependency }: { dependency: StudioDependency }) {
  const typeColors: Record<string, string> = {
    Runtime: 'bg-cyan/10 text-cyan border-cyan/20',
    BuildTime: 'bg-warning/10 text-warning border-warning/20',
    Optional: 'bg-muted/10 text-muted border-muted/20',
  };

  return (
    <div className="flex items-center gap-3 px-6 py-3 text-xs hover:bg-elevated/30 transition-colors">
      <Network size={12} className="text-accent flex-shrink-0" />

      <div className="flex-1 min-w-0">
        <p className="text-xs font-medium text-heading">{dependency.name}</p>
      </div>

      <span className={cn(
        'text-[10px] font-medium px-2 py-0.5 rounded-md border',
        typeColors[dependency.type] ?? typeColors.Optional,
      )}>
        {dependency.type}
      </span>

      <span className={cn(
        'text-[10px] px-2 py-0.5 rounded-md',
        dependency.direction === 'Upstream'
          ? 'bg-cyan/10 text-cyan'
          : 'bg-mint/10 text-mint',
      )}>
        {dependency.direction}
      </span>

      <button className="text-muted hover:text-accent transition-colors">
        <ExternalLink size={12} />
      </button>
    </div>
  );
}
