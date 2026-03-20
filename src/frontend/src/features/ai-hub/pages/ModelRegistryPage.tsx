import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Database,
  Search,
  Cpu,
  Shield,
  Globe,
  Lock,
  Plus,
  ChevronRight,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { Button } from '../../../components/Button';
import { OnboardingHints } from '../../../components/OnboardingHints';
import { PageContainer, StatsGrid } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Loader } from '../../../components/Loader';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { aiGovernanceApi } from '../api';

interface Model {
  id: string;
  name: string;
  displayName: string;
  provider: string;
  modelType: string;
  isInternal: boolean;
  isExternal: boolean;
  status: string;
  capabilities: string;
  sensitivityLevel: number;
}

const statusFilters = ['All', 'Active', 'Inactive', 'Deprecated', 'Blocked'] as const;

/**
 * Página do Model Registry — registo e gestão de modelos IA internos e externos.
 * Parte do módulo AI Hub do NexTraceOne.
 */
export function ModelRegistryPage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('All');

  const {
    data,
    isLoading,
    isError,
    refetch,
  } = useQuery({
    queryKey: ['ai-governance', 'models', statusFilter],
    queryFn: () => aiGovernanceApi.listModels({
      status: statusFilter === 'All' ? undefined : statusFilter,
    }),
    staleTime: 30_000,
  });

  const models: Model[] = useMemo(() => {
    const items = (data?.items ?? []) as Array<{
      modelId: string;
      name: string;
      displayName: string;
      provider: string;
      modelType: string;
      isInternal: boolean;
      isExternal: boolean;
      status: string;
      capabilities: string;
      sensitivityLevel: number;
    }>;

    return items.map((m) => ({
      id: m.modelId,
      name: m.name,
      displayName: m.displayName,
      provider: m.provider,
      modelType: m.modelType,
      isInternal: m.isInternal,
      isExternal: m.isExternal,
      status: m.status,
      capabilities: m.capabilities,
      sensitivityLevel: m.sensitivityLevel,
    }));
  }, [data]);

  const filtered = models.filter(m => {
    const matchesSearch = !search || m.displayName.toLowerCase().includes(search.toLowerCase()) || m.provider.toLowerCase().includes(search.toLowerCase());
    const matchesStatus = statusFilter === 'All' || m.status === statusFilter;
    return matchesSearch && matchesStatus;
  });

  const statusBadgeVariant = (status: string) => {
    switch (status) {
      case 'Active': return 'success';
      case 'Inactive': return 'default';
      case 'Deprecated': return 'warning';
      case 'Blocked': return 'danger';
      default: return 'default';
    }
  };

  const sensitivityLabel = (level: number) => {
    if (level <= 1) return t('aiHub.sensitivityLow');
    if (level === 2) return t('aiHub.sensitivityMedium');
    return t('aiHub.sensitivityHigh');
  };

  const sensitivityVariant = (level: number) => {
    if (level <= 1) return 'success';
    if (level === 2) return 'warning';
    return 'danger';
  };

  return (
    <PageContainer>
      {/* Onboarding hints — orientação contextual para novos utilizadores */}
      <OnboardingHints module="aiHub" />

      <PageHeader
        title={t('aiHub.modelsTitle')}
        subtitle={t('aiHub.modelsSubtitle')}
        actions={
          <Button variant="primary" size="md" disabled>
            <Plus size={16} />
            {t('aiHub.registerModel')}
          </Button>
        }
      />

      {/* Stat cards */}
      <StatsGrid columns={4}>
        <StatCard title={t('aiHub.totalModels')} value={models.length} icon={<Database size={20} />} color="text-accent" />
        <StatCard title={t('aiHub.activeModels')} value={models.filter(m => m.status === 'Active').length} icon={<Cpu size={20} />} color="text-success" />
        <StatCard title={t('aiHub.internalModels')} value={models.filter(m => m.isInternal).length} icon={<Lock size={20} />} color="text-info" />
        <StatCard title={t('aiHub.externalModels')} value={models.filter(m => m.isExternal).length} icon={<Globe size={20} />} color="text-warning" />
      </StatsGrid>

      {/* Filter bar */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center gap-3 mb-6">
        <div className="relative flex-1 max-w-md">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
          <input
            type="text"
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder={t('aiHub.searchModels')}
            className="w-full bg-elevated border border-edge rounded-lg pl-9 pr-4 py-2 text-sm text-body placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent"
          />
        </div>
        <div className="flex items-center gap-1">
          {statusFilters.map(f => (
            <button
              key={f}
              onClick={() => setStatusFilter(f)}
              className={`px-3 py-1.5 rounded-md text-xs font-medium transition-colors ${
                statusFilter === f
                  ? 'bg-accent text-heading'
                  : 'bg-elevated text-muted hover:text-body'
              }`}
            >
              {t(`aiHub.filter${f}`)}
            </button>
          ))}
        </div>
      </div>

      {/* Model list */}
      {isLoading && (
        <Card>
          <CardBody className="flex justify-center py-16">
            <Loader size="lg" />
          </CardBody>
        </Card>
      )}

      {isError && (
        <PageErrorState
          action={(
            <Button variant="secondary" size="sm" onClick={() => refetch()}>
              {t('common.retry', 'Retry')}
            </Button>
          )}
        />
      )}

      {!isLoading && !isError && filtered.length === 0 && (
        <EmptyState
          title={t('aiHub.noModelsFound', 'No models found')}
          description={t('aiHub.noModelsFoundDescription', 'Try adjusting your filters or search.')}
          size="compact"
        />
      )}

      {!isLoading && !isError && filtered.length > 0 && (
        <div className="grid grid-cols-1 gap-4">
          {filtered.map(model => (
            <Card key={model.id}>
              <CardBody>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-4 min-w-0">
                    <div className={`shrink-0 w-10 h-10 rounded-lg flex items-center justify-center ${model.isInternal ? 'bg-info/15 text-info' : 'bg-warning/15 text-warning'}`}>
                      {model.isInternal ? <Shield size={20} /> : <Globe size={20} />}
                    </div>
                    <div className="min-w-0">
                      <div className="flex items-center gap-2 mb-1">
                        <h3 className="text-sm font-semibold text-heading truncate">{model.displayName}</h3>
                        <Badge variant={statusBadgeVariant(model.status)}>{model.status}</Badge>
                        <Badge variant={model.isInternal ? 'info' : 'warning'}>
                          {model.isInternal ? t('aiHub.internal') : t('aiHub.external')}
                        </Badge>
                      </div>
                      <div className="flex items-center gap-3 text-xs text-muted">
                        <span>{model.provider}</span>
                        <span>·</span>
                        <span>{model.modelType}</span>
                        <span>·</span>
                        <span>{model.name}</span>
                      </div>
                      <div className="flex items-center gap-2 mt-2 flex-wrap">
                        {model.capabilities.split(',').map(cap => (
                          <Badge key={cap} variant="default">{cap.trim()}</Badge>
                        ))}
                        <Badge variant={sensitivityVariant(model.sensitivityLevel) as 'success' | 'warning' | 'danger'}>
                          {sensitivityLabel(model.sensitivityLevel)}
                        </Badge>
                      </div>
                    </div>
                  </div>
                  <ChevronRight size={16} className="text-muted shrink-0" />
                </div>
              </CardBody>
            </Card>
          ))}
        </div>
      )}
    </PageContainer>
  );
}
