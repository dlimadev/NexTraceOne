import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Award,
  Users,
  FileText,
  ScrollText,
  Activity,
  GitBranch,
  BookOpen,
  Shield,
  BarChart3,
  ChevronDown,
  ChevronUp,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { EmptyState } from '../../../components/EmptyState';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { Button, SearchInput, Select } from '../../../shared/ui';
import { sourceOfTruthApi } from '../api/sourceOfTruth';
import type { ScorecardDimensionDto } from '../api/sourceOfTruth';
import { queryKeys } from '../../../shared/api/queryKeys';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

// ── Constantes ──────────────────────────────────────────────────────

const maturityBadgeVariant = (level: string): 'success' | 'warning' | 'danger' | 'info' | 'default' => {
  switch (level) {
    case 'Optimizing': return 'success';
    case 'Managed': return 'success';
    case 'Defined': return 'info';
    case 'Developing': return 'warning';
    case 'Initial': return 'danger';
    default: return 'default';
  }
};

const scoreBarColor = (score: number): string => {
  const pct = score * 100;
  if (pct >= 80) return 'bg-success';
  if (pct >= 60) return 'bg-info';
  if (pct >= 40) return 'bg-warning';
  return 'bg-critical';
};

interface DimensionConfig {
  key: string;
  labelKey: string;
  icon: React.ReactNode;
}

const DIMENSIONS: DimensionConfig[] = [
  { key: 'ownership', labelKey: 'serviceScorecard.dimensions.ownership', icon: <Users size={16} /> },
  { key: 'documentation', labelKey: 'serviceScorecard.dimensions.documentation', icon: <FileText size={16} /> },
  { key: 'contracts', labelKey: 'serviceScorecard.dimensions.contracts', icon: <ScrollText size={16} /> },
  { key: 'slos', labelKey: 'serviceScorecard.dimensions.slos', icon: <Activity size={16} /> },
  { key: 'observability', labelKey: 'serviceScorecard.dimensions.observability', icon: <BarChart3 size={16} /> },
  { key: 'changeGovernance', labelKey: 'serviceScorecard.dimensions.changeGovernance', icon: <GitBranch size={16} /> },
  { key: 'runbooks', labelKey: 'serviceScorecard.dimensions.runbooks', icon: <BookOpen size={16} /> },
  { key: 'security', labelKey: 'serviceScorecard.dimensions.security', icon: <Shield size={16} /> },
];

// ── Componente de barra de score ────────────────────────────────────

function ScoreBar({ score }: { score: number }) {
  const pct = Math.round(score * 100);
  return (
    <div className="flex items-center gap-2">
      <div className="flex-1 h-2 rounded-full bg-subtle overflow-hidden">
        <div
          className={`h-full rounded-full transition-all duration-500 ${scoreBarColor(score)}`}
          style={{ width: `${pct}%` }}
        />
      </div>
      <span className="text-xs font-mono text-muted w-10 text-right">{pct}%</span>
    </div>
  );
}

// ── Componente de dimensão expandível ───────────────────────────────

function DimensionCard({
  config,
  data,
}: {
  config: DimensionConfig;
  data: ScorecardDimensionDto;
}) {
  const { t } = useTranslation();
  const [expanded, setExpanded] = useState(false);
  const pct = Math.round(data.score * 100);
  const weightPct = Math.round(data.weight * 100);

  return (
    <Card>
      <Button
        type="button"
        variant="ghost"
        onClick={() => setExpanded(!expanded)}
        className="w-full block h-auto p-0 rounded-none text-left"
      >
        <CardBody className="py-3">
          <div className="flex items-center gap-3">
            <span className="text-muted">{config.icon}</span>
            <div className="flex-1 min-w-0">
              <div className="flex items-center justify-between mb-1">
                <span className="text-sm font-medium text-heading">
                  {t(config.labelKey, config.key)}
                </span>
                <div className="flex items-center gap-2">
                  <Badge variant={pct >= 70 ? 'success' : pct >= 40 ? 'warning' : 'danger'}>
                    {pct}%
                  </Badge>
                  <span className="text-xs text-muted">
                    {t('serviceScorecard.weight', { weight: weightPct })}
                  </span>
                  {expanded ? <ChevronUp size={14} /> : <ChevronDown size={14} />}
                </div>
              </div>
              <ScoreBar score={data.score} />
            </div>
          </div>
          {expanded && (
            <div className="mt-3 ml-8 pl-1 border-l-2 border-divider">
              <p className="text-xs text-muted leading-relaxed">{data.justification}</p>
            </div>
          )}
        </CardBody>
      </Button>
    </Card>
  );
}

// ── Página principal ────────────────────────────────────────────────

/**
 * Página de Service Scorecard — maturidade cross-module de serviços.
 *
 * Calcula e apresenta scorecard de maturidade consultando dados de:
 * - Catalog (ownership, metadata, APIs)
 * - Contracts (publicação, quality score)
 * - Reliability (SLOs, error budget, status)
 * - Knowledge (documentos, runbooks)
 *
 * Diferencial NexTraceOne: scorecard que inclui contratos + change governance +
 * SLOs reais — visão que OpsLevel/Cortex não conseguem oferecer nativamente.
 */
export function ServiceScorecardPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [serviceName, setServiceName] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [environment, setEnvironment] = useState('Production');

  const {
    data: scorecard,
    isLoading,
    isError,
    refetch,
  } = useQuery({
    queryKey: [...queryKeys.catalog.services.scorecard(serviceName, environment), activeEnvironmentId],
    queryFn: () => sourceOfTruthApi.getServiceScorecard(serviceName, environment),
    enabled: serviceName.length > 0,
    staleTime: 60_000,
    retry: 1,
  });

  const handleSearch = () => {
    if (searchInput.trim()) {
      setServiceName(searchInput.trim());
    }
  };

  const overallPct = scorecard ? Math.round(scorecard.overallScore * 100) : 0;

  return (
    <PageContainer>
      <PageHeader
        title={t('serviceScorecard.title', 'Service Scorecards')}
        subtitle={t('serviceScorecard.subtitle', 'Cross-module maturity scoring for services')}
      />

      {/* Search bar */}
      <PageSection>
        <Card>
          <CardBody>
            <div className="flex gap-3 items-end">
              <div className="flex-1">
                <label className="block text-xs font-medium text-muted mb-1">
                  {t('serviceScorecard.serviceName', 'Service Name')}
                </label>
                <SearchInput
                  size="sm"
                  value={searchInput}
                  onChange={(e) => setSearchInput(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                  placeholder={t('serviceScorecard.searchPlaceholder', 'Enter service name...')}
                  aria-label={t('serviceScorecard.serviceName', 'Service Name')}
                />
              </div>
              <div className="w-40">
                <Select
                  label={t('serviceScorecard.environment', 'Environment')}
                  value={environment}
                  onChange={(e) => setEnvironment(e.target.value)}
                  options={[
                    { value: 'Production', label: t('environment.profile.production', 'Production') },
                    { value: 'Staging', label: t('environment.profile.staging', 'Staging') },
                    { value: 'Development', label: t('environment.profile.development', 'Development') },
                  ]}
                  size="sm"
                />
              </div>
              <Button variant="primary" onClick={handleSearch}>
                {t('serviceScorecard.compute', 'Compute')}
              </Button>
            </div>
          </CardBody>
        </Card>
      </PageSection>

      {/* States */}
      {!serviceName && (
        <EmptyState
          icon={<Award size={40} />}
          title={t('serviceScorecard.emptyTitle', 'No service selected')}
          description={t('serviceScorecard.emptyDescription', 'Enter a service name above to compute its maturity scorecard.')}
        />
      )}

      {serviceName && isLoading && <PageLoadingState />}

      {serviceName && isError && (
        <PageErrorState
          message={t('serviceScorecard.errorMessage', 'Failed to compute scorecard.')}
          onRetry={() => refetch()}
        />
      )}

      {/* Scorecard results */}
      {scorecard && (
        <>
          {/* Overall score header */}
          <PageSection>
            <Card>
              <CardBody>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-4">
                    <div
                      className={`w-16 h-16 rounded-full flex items-center justify-center text-xl font-bold ${
                        overallPct >= 70
                          ? 'bg-success/10 text-success'
                          : overallPct >= 40
                            ? 'bg-warning/10 text-warning'
                            : 'bg-critical/10 text-critical'
                      }`}
                    >
                      {overallPct}
                    </div>
                    <div>
                      <h2 className="text-lg font-semibold text-heading">
                        {scorecard.serviceName}
                      </h2>
                      <div className="flex items-center gap-2 mt-1">
                        <Badge variant={maturityBadgeVariant(scorecard.maturityLevel)}>
                          {scorecard.maturityLevel}
                        </Badge>
                        {scorecard.teamName && (
                          <span className="text-xs text-muted">
                            {t('serviceScorecard.team', 'Team')}: {scorecard.teamName}
                          </span>
                        )}
                        {scorecard.domain && (
                          <span className="text-xs text-muted">
                            {t('serviceScorecard.domain', 'Domain')}: {scorecard.domain}
                          </span>
                        )}
                      </div>
                    </div>
                  </div>
                  <div className="text-right">
                    <p className="text-xs text-muted">
                      {t('serviceScorecard.computedAt', 'Computed at')}
                    </p>
                    <p className="text-sm text-body">
                      {new Date(scorecard.computedAt).toLocaleString()}
                    </p>
                  </div>
                </div>
              </CardBody>
            </Card>
          </PageSection>

          {/* Dimension cards */}
          <PageSection>
            <Card>
              <CardHeader>
                <h3 className="text-sm font-semibold text-heading">
                  {t('serviceScorecard.dimensionsTitle', 'Maturity Dimensions')}
                </h3>
              </CardHeader>
              <CardBody>
                <div className="space-y-2">
                  {DIMENSIONS.map((dim) => {
                    const dimData = scorecard.dimensions[
                      dim.key as keyof typeof scorecard.dimensions
                    ];
                    return (
                      <DimensionCard
                        key={dim.key}
                        config={dim}
                        data={dimData}
                      />
                    );
                  })}
                </div>
              </CardBody>
            </Card>
          </PageSection>
        </>
      )}
    </PageContainer>
  );
}
