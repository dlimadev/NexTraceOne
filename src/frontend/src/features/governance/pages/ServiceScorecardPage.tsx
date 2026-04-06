import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { useParams, useNavigate } from 'react-router-dom';
import { Star, Grid3x3, BarChart2, RefreshCw, Search } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, StatsGrid, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import client from '../../../api/client';

interface ScorecardDimension {
  name: string;
  score: number;
  note: string;
  weight: number;
}

interface ServiceScorecardResponse {
  serviceName: string;
  periodDays: number;
  computedAt: string;
  dimensions: ScorecardDimension[];
  finalScore: number;
  maturityLevel: string;
  summary: string;
}

interface ServiceScorecardSummary {
  serviceName: string;
  teamName: string;
  finalScore: number;
  maturityLevel: string;
  computedAt: string;
}

interface ListScorecardsResponse {
  items: ServiceScorecardSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
  generatedAt: string;
  averageScore: number;
  distributionByLevel: Record<string, number>;
}

const useServiceScorecard = (serviceName: string, periodDays: number) =>
  useQuery({
    queryKey: ['service-scorecard', serviceName, periodDays],
    queryFn: () =>
      client
        .get<ServiceScorecardResponse>(`/executive/service-scorecards/${serviceName}`, {
          params: { periodDays },
        })
        .then((r) => r.data),
    enabled: serviceName.length > 0,
  });

const useListScorecards = (teamName: string, maturityLevel: string, page: number) =>
  useQuery({
    queryKey: ['list-service-scorecards', teamName, maturityLevel, page],
    queryFn: () =>
      client
        .get<ListScorecardsResponse>('/executive/service-scorecards', {
          params: { teamName: teamName || undefined, maturityLevel: maturityLevel || undefined, page },
        })
        .then((r) => r.data),
  });

const LEVEL_VARIANT: Record<string, 'success' | 'warning' | 'danger' | 'secondary'> = {
  Gold: 'success',
  Silver: 'secondary',
  Bronze: 'warning',
  'Below Standard': 'danger',
};

const ScoreBar = ({ score }: { score: number }) => {
  const color =
    score >= 90 ? 'bg-emerald-500' :
    score >= 75 ? 'bg-green-400' :
    score >= 60 ? 'bg-amber-400' : 'bg-red-400';

  return (
    <div className="relative h-2 w-full rounded-full bg-gray-200 dark:bg-gray-700">
      <div
        className={`h-2 rounded-full transition-all ${color}`}
        style={{ width: `${score}%` }}
      />
    </div>
  );
};

export function ServiceScorecardPage() {
  const { t } = useTranslation();
  const { serviceName: paramServiceName } = useParams<{ serviceName?: string }>();
  const navigate = useNavigate();

  const [activeTab, setActiveTab] = useState<'detail' | 'list'>(
    paramServiceName ? 'detail' : 'list'
  );
  const [serviceInput, setServiceInput] = useState(paramServiceName ?? '');
  const [searchedService, setSearchedService] = useState(paramServiceName ?? '');
  const [periodDays, setPeriodDays] = useState(30);
  const [teamFilter, setTeamFilter] = useState('');
  const [levelFilter, setLevelFilter] = useState('');
  const [page, setPage] = useState(1);

  const detailQuery = useServiceScorecard(searchedService, periodDays);
  const listQuery = useListScorecards(teamFilter, levelFilter, page);

  const handleSearch = () => setSearchedService(serviceInput.trim());

  return (
    <PageContainer>
      <PageHeader
        title={t('governance.scorecard.title')}
        subtitle={t('governance.scorecard.subtitle')}
        icon={<Star size={24} />}
        actions={
          <div className="flex items-center gap-2">
            <Button
              size="sm"
              variant={activeTab === 'list' ? 'primary' : 'secondary'}
              onClick={() => setActiveTab('list')}
            >
              <Grid3x3 size={14} className="mr-1" />
              {t('governance.scorecard.listTab')}
            </Button>
            <Button
              size="sm"
              variant={activeTab === 'detail' ? 'primary' : 'secondary'}
              onClick={() => setActiveTab('detail')}
            >
              <BarChart2 size={14} className="mr-1" />
              {t('governance.scorecard.detailTab')}
            </Button>
          </div>
        }
      />

      {/* ── LIST TAB ─────────────────────────────────────────────────────────── */}
      {activeTab === 'list' && (
        <>
          <Card className="mb-4">
            <CardBody className="p-4">
              <div className="flex flex-col sm:flex-row gap-3">
                <input
                  type="text"
                  value={teamFilter}
                  onChange={(e) => setTeamFilter(e.target.value)}
                  placeholder={t('governance.scorecard.filterByTeam')}
                  className="flex-1 rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm"
                />
                <select
                  value={levelFilter}
                  onChange={(e) => setLevelFilter(e.target.value)}
                  className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-2 py-2"
                >
                  <option value="">{t('governance.scorecard.allLevels')}</option>
                  {['Gold', 'Silver', 'Bronze', 'Below Standard'].map((l) => (
                    <option key={l} value={l}>{l}</option>
                  ))}
                </select>
                <Button onClick={() => { setPage(1); listQuery.refetch(); }}>
                  <RefreshCw size={14} className="mr-1" />
                  {t('common.refresh')}
                </Button>
              </div>
            </CardBody>
          </Card>

          {listQuery.isLoading && <PageLoadingState message={t('governance.scorecard.loading')} />}
          {listQuery.isError && (
            <PageErrorState message={t('governance.scorecard.error')} onRetry={() => listQuery.refetch()} />
          )}

          {listQuery.data && (
            <>
              {/* Distribution */}
              <div className="mb-4 flex flex-wrap gap-2">
                {Object.entries(listQuery.data.distributionByLevel).map(([level, count]) => (
                  <Badge key={level} variant={LEVEL_VARIANT[level] ?? 'secondary'}>
                    {level}: {count}
                  </Badge>
                ))}
                <Badge variant="secondary">
                  {t('governance.scorecard.avgScore', { score: listQuery.data.averageScore })}
                </Badge>
              </div>

              <div className="space-y-2">
                {listQuery.data.items.map((item) => (
                  <Card
                    key={item.serviceName}
                    className="cursor-pointer hover:shadow-md transition-shadow"
                    onClick={() => {
                      setSearchedService(item.serviceName);
                      setServiceInput(item.serviceName);
                      setActiveTab('detail');
                      navigate(`/governance/scorecards/${item.serviceName}`);
                    }}
                  >
                    <CardBody className="p-4">
                      <div className="flex items-center justify-between mb-2">
                        <div>
                          <p className="font-medium text-sm text-gray-900 dark:text-white">{item.serviceName}</p>
                          <p className="text-xs text-gray-500 dark:text-gray-400">{item.teamName}</p>
                        </div>
                        <div className="flex items-center gap-2">
                          <span className="text-lg font-bold text-gray-900 dark:text-white">{item.finalScore}</span>
                          <Badge variant={LEVEL_VARIANT[item.maturityLevel] ?? 'secondary'}>
                            {item.maturityLevel}
                          </Badge>
                        </div>
                      </div>
                      <ScoreBar score={item.finalScore} />
                    </CardBody>
                  </Card>
                ))}
              </div>

              {/* Pagination */}
              {listQuery.data.totalCount > listQuery.data.pageSize && (
                <div className="mt-4 flex items-center justify-center gap-2">
                  <Button
                    size="sm"
                    variant="secondary"
                    disabled={page === 1}
                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                  >
                    {t('common.previous')}
                  </Button>
                  <span className="text-sm text-gray-500 dark:text-gray-400">
                    {t('common.pageOf', { page, total: Math.ceil(listQuery.data.totalCount / listQuery.data.pageSize) })}
                  </span>
                  <Button
                    size="sm"
                    variant="secondary"
                    disabled={page * listQuery.data.pageSize >= listQuery.data.totalCount}
                    onClick={() => setPage((p) => p + 1)}
                  >
                    {t('common.next')}
                  </Button>
                </div>
              )}
            </>
          )}
        </>
      )}

      {/* ── DETAIL TAB ───────────────────────────────────────────────────────── */}
      {activeTab === 'detail' && (
        <>
          <Card className="mb-4">
            <CardBody className="p-4">
              <div className="flex flex-col sm:flex-row gap-3">
                <input
                  type="text"
                  value={serviceInput}
                  onChange={(e) => setServiceInput(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                  placeholder={t('governance.scorecard.serviceNamePlaceholder')}
                  className="flex-1 rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm"
                />
                <select
                  value={periodDays}
                  onChange={(e) => setPeriodDays(Number(e.target.value))}
                  className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-2 py-2"
                >
                  {[7, 30, 60, 90].map((d) => (
                    <option key={d} value={d}>{t('common.daysN', { count: d })}</option>
                  ))}
                </select>
                <Button onClick={handleSearch} disabled={!serviceInput.trim()}>
                  <Search size={14} className="mr-1" />
                  {t('governance.scorecard.compute')}
                </Button>
              </div>
            </CardBody>
          </Card>

          {detailQuery.isLoading && <PageLoadingState message={t('governance.scorecard.computing')} />}
          {detailQuery.isError && (
            <PageErrorState
              message={t('governance.scorecard.computeError')}
              onRetry={() => detailQuery.refetch()}
            />
          )}

          {detailQuery.data && (
            <>
              <div className="mb-4 flex items-center justify-between">
                <div>
                  <h2 className="text-xl font-bold text-gray-900 dark:text-white">
                    {detailQuery.data.serviceName}
                  </h2>
                  <p className="text-sm text-gray-500 dark:text-gray-400">{detailQuery.data.summary}</p>
                </div>
                <div className="text-right">
                  <p className="text-3xl font-bold text-gray-900 dark:text-white">{detailQuery.data.finalScore}</p>
                  <Badge variant={LEVEL_VARIANT[detailQuery.data.maturityLevel] ?? 'secondary'}>
                    {detailQuery.data.maturityLevel}
                  </Badge>
                </div>
              </div>

              <PageSection title={t('governance.scorecard.dimensions')}>
                <div className="space-y-3">
                  {detailQuery.data.dimensions.map((dim) => (
                    <Card key={dim.name}>
                      <CardBody className="p-4">
                        <div className="flex items-center justify-between mb-2">
                          <p className="text-sm font-medium text-gray-900 dark:text-white">{dim.name}</p>
                          <span className="text-lg font-bold text-gray-900 dark:text-white">{dim.score}</span>
                        </div>
                        <ScoreBar score={dim.score} />
                        <p className="mt-2 text-xs text-gray-500 dark:text-gray-400">{dim.note}</p>
                      </CardBody>
                    </Card>
                  ))}
                </div>
              </PageSection>
            </>
          )}

          {!searchedService && !detailQuery.isLoading && (
            <div className="text-center py-12 text-gray-500 dark:text-gray-400">
              <Star size={40} className="mx-auto mb-3 opacity-40" />
              <p>{t('governance.scorecard.enterServiceName')}</p>
            </div>
          )}
        </>
      )}
    </PageContainer>
  );
}
