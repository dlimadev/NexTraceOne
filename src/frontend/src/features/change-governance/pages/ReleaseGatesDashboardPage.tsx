import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  GitMerge,
  ArrowRight,
  CheckCircle2,
  XCircle,
  ShieldAlert,
  Filter,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { EmptyState } from '../../../components/EmptyState';
import { changeIntelligenceApi, type PromotionGateItem } from '../api/changeIntelligence';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

// Pares de ambientes padrão cobertos pelo produto
const ENV_PAIRS: Array<{ from: string; to: string }> = [
  { from: 'Development', to: 'Pre-Production' },
  { from: 'Pre-Production', to: 'Production' },
];

/**
 * ReleaseGatesDashboardPage — painel de gates de promoção entre ambientes.
 *
 * Permite tech leads, architects e platform admins:
 * - Ver o estado dos gates de promoção entre Development → Pre-Production → Production
 * - Identificar gates bloqueadores (blockOnFailure=true) e inactivos
 * - Navegar para o detalhe de avaliação de cada gate
 *
 * Contextualizado por ambiente (conforme princípio do produto):
 * a análise de comportamento em ambientes não produtivos é mandatória
 * para prevenir problemas em produção.
 */
export function ReleaseGatesDashboardPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [selectedPair, setSelectedPair] = useState(ENV_PAIRS[0]);
  const [filterActive, setFilterActive] = useState<boolean | null>(null);

  const {
    data,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ['promotion-gates', selectedPair.from, selectedPair.to, activeEnvironmentId],
    queryFn: () =>
      changeIntelligenceApi.listPromotionGatesByEnvironment(selectedPair.from, selectedPair.to),
    staleTime: 30_000,
  });

  const gates: PromotionGateItem[] = data?.gates ?? [];
  const filtered = filterActive === null ? gates : gates.filter((g) => g.isActive === filterActive);

  const activeCount = gates.filter((g) => g.isActive).length;
  const blockerCount = gates.filter((g) => g.blockOnFailure && g.isActive).length;

  function gateStatusVariant(gate: PromotionGateItem): 'success' | 'danger' | 'default' {
    if (!gate.isActive) return 'default';
    if (gate.blockOnFailure) return 'danger';
    return 'success';
  }

  return (
    <PageContainer>
      <PageHeader
        icon={<GitMerge className="text-accent" />}
        title={t('releaseGatesDashboard.title', 'Release Gates Dashboard')}
        subtitle={t(
          'releaseGatesDashboard.subtitle',
          'Monitor promotion gates between environments to ensure controlled release progression',
        )}
      />

      {/* Environment pair selector */}
      <div className="mb-6 flex flex-wrap items-center gap-3">
        <span className="text-sm text-muted">{t('releaseGatesDashboard.environment', 'Environment path:')}</span>
        <div className="flex gap-2">
          {ENV_PAIRS.map((pair) => (
            <button
              key={`${pair.from}-${pair.to}`}
              type="button"
              onClick={() => setSelectedPair(pair)}
              className={`flex items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-medium transition-colors ${
                selectedPair.from === pair.from && selectedPair.to === pair.to
                  ? 'bg-accent text-white'
                  : 'bg-canvas border border-edge text-heading hover:bg-surface'
              }`}
            >
              <span>{pair.from}</span>
              <ArrowRight size={12} />
              <span>{pair.to}</span>
            </button>
          ))}
        </div>

        {/* Filter */}
        <div className="ml-auto flex items-center gap-2">
          <Filter size={14} className="text-muted" />
          <select
            className="rounded-md bg-canvas border border-edge px-2 py-1.5 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent"
            value={filterActive === null ? 'all' : filterActive ? 'active' : 'inactive'}
            onChange={(e) =>
              setFilterActive(e.target.value === 'all' ? null : e.target.value === 'active')
            }
          >
            <option value="all">{t('releaseGatesDashboard.filterAll', 'All gates')}</option>
            <option value="active">{t('releaseGatesDashboard.filterActive', 'Active only')}</option>
            <option value="inactive">{t('releaseGatesDashboard.filterInactive', 'Inactive only')}</option>
          </select>
        </div>
      </div>

      {/* Summary cards */}
      {!isLoading && !isError && (
        <div className="mb-6 grid grid-cols-2 md:grid-cols-3 gap-4">
          <Card>
            <CardBody>
              <div className="flex items-center gap-3">
                <CheckCircle2 className="text-success" size={20} />
                <div>
                  <p className="text-xs text-muted">{t('releaseGatesDashboard.activeGates', 'Active Gates')}</p>
                  <p className="text-2xl font-bold text-heading">{activeCount}</p>
                </div>
              </div>
            </CardBody>
          </Card>
          <Card>
            <CardBody>
              <div className="flex items-center gap-3">
                <ShieldAlert className="text-danger" size={20} />
                <div>
                  <p className="text-xs text-muted">{t('releaseGatesDashboard.blockers', 'Blocking Gates')}</p>
                  <p className="text-2xl font-bold text-heading">{blockerCount}</p>
                </div>
              </div>
            </CardBody>
          </Card>
          <Card>
            <CardBody>
              <div className="flex items-center gap-3">
                <GitMerge className="text-accent" size={20} />
                <div>
                  <p className="text-xs text-muted">{t('releaseGatesDashboard.totalGates', 'Total Gates')}</p>
                  <p className="text-2xl font-bold text-heading">{gates.length}</p>
                </div>
              </div>
            </CardBody>
          </Card>
        </div>
      )}

      {isLoading && <PageLoadingState />}

      {isError && (
        <EmptyState
          icon={<GitMerge size={40} />}
          title={t('releaseGatesDashboard.errorTitle', 'Could not load gates')}
          description={t('releaseGatesDashboard.errorDescription', 'Verify that promotion gates have been configured for this environment pair')}
        />
      )}

      {!isLoading && !isError && filtered.length === 0 && (
        <EmptyState
          icon={<GitMerge size={40} />}
          title={t('releaseGatesDashboard.emptyTitle', 'No gates found')}
          description={t('releaseGatesDashboard.emptyDescription', 'No promotion gates configured for this environment path and filter')}
        />
      )}

      {/* Gates list */}
      {!isLoading && filtered.length > 0 && (
        <div className="space-y-3">
          {filtered.map((gate) => (
            <Card key={gate.gateId}>
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    {gate.isActive ? (
                      <CheckCircle2 size={16} className="text-success" />
                    ) : (
                      <XCircle size={16} className="text-muted" />
                    )}
                    <span className="font-semibold text-heading">{gate.name}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Badge variant={gateStatusVariant(gate)}>
                      {gate.isActive
                        ? gate.blockOnFailure
                          ? t('releaseGatesDashboard.blocking', 'Blocking')
                          : t('releaseGatesDashboard.nonBlocking', 'Non-blocking')
                        : t('releaseGatesDashboard.inactive', 'Inactive')}
                    </Badge>
                  </div>
                </div>
              </CardHeader>
              <CardBody>
                <div className="flex flex-wrap items-center gap-4 text-sm">
                  <div className="flex items-center gap-1.5 text-muted">
                    <ArrowRight size={12} />
                    <span className="font-medium text-heading">{gate.environmentFrom}</span>
                    <ArrowRight size={12} />
                    <span className="font-medium text-heading">{gate.environmentTo}</span>
                  </div>
                  {gate.description && (
                    <p className="text-muted text-xs flex-1">{gate.description}</p>
                  )}
                  <p className="text-xs text-muted ml-auto">
                    {t('releaseGatesDashboard.created', 'Created')} {new Date(gate.createdAt).toLocaleDateString()}
                  </p>
                </div>
              </CardBody>
            </Card>
          ))}
        </div>
      )}
    </PageContainer>
  );
}
