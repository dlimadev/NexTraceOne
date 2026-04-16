import { useEffect, useMemo, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Plus,
  Search,
  Activity,
  Clock,
  Snowflake,
  Eye,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  GitCompare,
  FileText,
  ShieldCheck,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { changeIntelligenceApi } from '../api';
import type { ChangeLevel, DeploymentState } from '../../../types';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { ReleasesIntelligenceTab } from '../components/ReleasesIntelligenceTab';
import { PreProdComparisonPanel } from '../components/PreProdComparisonPanel';
import { ReleaseNotesPanel } from '../components/ReleaseNotesPanel';
import { DeployReadinessPanel } from '../components/DeployReadinessPanel';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

// ─── Constantes auxiliares ───────────────────────────────────────────────────

type TabId = 'overview' | 'intelligence' | 'timeline' | 'freeze' | 'pre-prod' | 'release-notes' | 'readiness';

const CHANGE_LEVEL_KEYS = [
  'releases.changeLevels.operational',
  'releases.changeLevels.nonBreaking',
  'releases.changeLevels.additive',
  'releases.changeLevels.breaking',
  'releases.changeLevels.publication',
] as const;

const INPUT_CLS =
  'w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors';

function changeLevelVariant(level: ChangeLevel): 'default' | 'success' | 'warning' | 'danger' | 'info' {
  if (level === 0) return 'default';
  if (level === 1) return 'success';
  if (level === 2) return 'info';
  if (level === 3) return 'danger';
  return 'warning';
}

function stateVariant(state: DeploymentState): 'default' | 'success' | 'warning' | 'danger' | 'info' {
  if (state === 'Succeeded') return 'success';
  if (state === 'Failed' || state === 'RolledBack') return 'danger';
  if (state === 'Running') return 'info';
  return 'default';
}

// ─── Formulário de notificação ───────────────────────────────────────────────

interface NotifyForm {
  apiAssetId: string;
  version: string;
  environment: string;
  commitSha: string;
}

const emptyForm: NotifyForm = { apiAssetId: '', version: '', environment: '', commitSha: '' };

// ─── Componente principal ────────────────────────────────────────────────────

export function ReleasesPage() {
  const { t } = useTranslation();
  const { availableEnvironments } = useEnvironment();
  const queryClient = useQueryClient();

  const [activeTab, setActiveTab] = useState<TabId>('overview');
  const [selectedReleaseId, setSelectedReleaseId] = useState<string | null>(null);
  const [apiAssetId, setApiAssetId] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<NotifyForm>(emptyForm);
  const [page] = useState(1);
  const [freezeCheckEnv, setFreezeCheckEnv] = useState('');

  const environmentOptions = useMemo(() => {
    const options = availableEnvironments.map((env) => env.name).filter(Boolean);
    return Array.from(new Set(options)).sort((a, b) => a.localeCompare(b));
  }, [availableEnvironments]);

  useEffect(() => {
    if (!environmentOptions.length) return;
    if (!form.environment || !environmentOptions.includes(form.environment)) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setForm((current) => ({ ...current, environment: environmentOptions[0] ?? '' }));
    }
  }, [environmentOptions, form.environment]);

  // ── Queries ────────────────────────────────────────────────────────────────

  const releasesQuery = useQuery({
    queryKey: ['releases', apiAssetId, page],
    queryFn: () => changeIntelligenceApi.listReleases(apiAssetId, page, 20),
    enabled: !!apiAssetId,
  });

  const intelligenceQuery = useQuery({
    queryKey: ['intelligence', selectedReleaseId],
    queryFn: () => changeIntelligenceApi.getIntelligenceSummary(selectedReleaseId!),
    enabled: !!selectedReleaseId,
  });

  const freezeCheckQuery = useQuery({
    queryKey: ['freeze-check', freezeCheckEnv],
    queryFn: () =>
      changeIntelligenceApi.checkFreezeConflict(
        new Date().toISOString(),
        freezeCheckEnv || undefined,
      ),
    enabled: false,
  });

  // ── Mutations ──────────────────────────────────────────────────────────────

  const notifyMutation = useMutation({
    mutationFn: changeIntelligenceApi.notifyDeployment,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['releases'] });
      setShowForm(false);
      setForm(emptyForm);
    },
  });

  const startReviewMutation = useMutation({
    mutationFn: (releaseId: string) => changeIntelligenceApi.startReview(releaseId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['intelligence', selectedReleaseId] });
    },
  });

  // ── Handlers ───────────────────────────────────────────────────────────────

  const handleNotify = (e: React.FormEvent) => {
    e.preventDefault();
    notifyMutation.mutate(form);
  };

  const selectRelease = (id: string) => {
    setSelectedReleaseId(id);
    setActiveTab('intelligence');
  };

  // ── Tab config ─────────────────────────────────────────────────────────────

  const tabs: { id: TabId; icon: typeof Activity; labelKey: string }[] = [
    { id: 'overview', icon: Eye, labelKey: 'releases.tabs.overview' },
    { id: 'intelligence', icon: Activity, labelKey: 'releases.tabs.intelligence' },
    { id: 'timeline', icon: Clock, labelKey: 'releases.tabs.timeline' },
    { id: 'freeze', icon: Snowflake, labelKey: 'releases.tabs.freeze' },
    { id: 'pre-prod', icon: GitCompare, labelKey: 'releases.tabs.preProd' },
    { id: 'release-notes', icon: FileText, labelKey: 'releases.tabs.releaseNotes' },
    { id: 'readiness', icon: ShieldCheck, labelKey: 'releases.tabs.readiness' },
  ];

  const data = releasesQuery.data;
  const intel = intelligenceQuery.data;

  return (
    <PageContainer>
      <PageHeader
        title={t('releases.title')}
        subtitle={t('releases.subtitle')}
        actions={
          <Button onClick={() => { setActiveTab('overview'); setShowForm((v) => !v); }}>
            <Plus size={16} />
            {t('releases.notifyDeployment')}
          </Button>
        }
      />

      {/* Tab Navigation */}
      <div className="flex gap-1 mb-6 border-b border-edge">
        {tabs.map(({ id, icon: Icon, labelKey }) => (
          <button
            key={id}
            onClick={() => setActiveTab(id)}
            className={`flex items-center gap-2 px-4 py-2.5 text-sm font-medium transition-colors border-b-2 -mb-px ${
              activeTab === id
                ? 'border-accent text-accent'
                : 'border-transparent text-muted hover:text-body hover:border-edge'
            }`}
          >
            <Icon size={16} />
            {t(labelKey)}
            {id === 'intelligence' && selectedReleaseId && (
              <span className="ml-1 inline-flex h-2 w-2 rounded-full bg-accent" />
            )}
          </button>
        ))}
      </div>

      {/* ═══════════════════ OVERVIEW TAB ═══════════════════ */}
      {activeTab === 'overview' && (
        <>
          {showForm && (
            <Card className="mb-6">
              <CardHeader>
                <h2 className="text-base font-semibold text-heading">
                  {t('releases.notifyNewDeployment')}
                </h2>
              </CardHeader>
              <CardBody>
                <form onSubmit={handleNotify} className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">
                      {t('releases.apiAssetId')}
                    </label>
                    <input
                      type="text"
                      value={form.apiAssetId}
                      onChange={(e) => setForm((f) => ({ ...f, apiAssetId: e.target.value }))}
                      required
                      className={INPUT_CLS}
                      placeholder={t('releases.apiAssetPlaceholder')}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">
                      {t('releases.version')}
                    </label>
                    <input
                      type="text"
                      value={form.version}
                      onChange={(e) => setForm((f) => ({ ...f, version: e.target.value }))}
                      required
                      className={INPUT_CLS}
                      placeholder={t('releases.versionPlaceholder')}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">
                      {t('releases.environment')}
                    </label>
                    <select
                      value={form.environment}
                      onChange={(e) => setForm((f) => ({ ...f, environment: e.target.value }))}
                      className={INPUT_CLS}
                    >
                      {environmentOptions.map((env) => (
                        <option key={env} value={env}>{env}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">
                      {t('releases.commitSha')}
                    </label>
                    <input
                      type="text"
                      value={form.commitSha}
                      onChange={(e) => setForm((f) => ({ ...f, commitSha: e.target.value }))}
                      className={INPUT_CLS}
                      placeholder={t('releases.commitPlaceholder')}
                    />
                  </div>
                  <div className="col-span-2 flex gap-2 justify-end">
                    <Button variant="secondary" type="button" onClick={() => setShowForm(false)}>
                      {t('common.cancel')}
                    </Button>
                    <Button type="submit" loading={notifyMutation.isPending}>
                      {t('releases.submit')}
                    </Button>
                  </div>
                </form>
              </CardBody>
            </Card>
          )}

          {/* Filter + Table */}
          <PageSection>
            {/* Search */}
            <Card className="mb-6">
            <CardBody>
              <div className="flex gap-3 items-center">
                <Search size={16} className="text-muted shrink-0" />
                <input
                  type="text"
                  value={apiAssetId}
                  onChange={(e) => setApiAssetId(e.target.value)}
                  placeholder={t('releases.filterPlaceholder')}
                  className="flex-1 text-sm bg-transparent text-heading placeholder:text-muted focus:outline-none"
                />
              </div>
            </CardBody>
          </Card>

          {/* Releases Table */}
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <h2 className="text-base font-semibold text-heading">{t('releases.releaseHistory')}</h2>
                {data && <span className="text-sm text-muted">{data.totalCount} {t('common.total')}</span>}
              </div>
            </CardHeader>
            <div className="overflow-x-auto">
              {!apiAssetId ? (
                <p className="px-6 py-12 text-sm text-muted text-center">
                  {t('releases.enterApiAssetId')}
                </p>
              ) : releasesQuery.isLoading ? (
                <PageLoadingState />
              ) : releasesQuery.isError ? (
                <PageErrorState message={t('releases.loadFailed')} />
              ) : !data?.items?.length ? (
                <EmptyState
                  title={t('releases.empty', 'No releases found')}
                  description={t('releases.emptyDescription', 'No releases match your current filters.')}
                />
              ) : (
                <table className="min-w-full text-sm">
                  <thead>
                    <tr className="border-b border-edge bg-panel text-left">
                      <th className="px-6 py-3 font-medium text-muted">{t('releases.version')}</th>
                      <th className="px-6 py-3 font-medium text-muted">{t('releases.environment')}</th>
                      <th className="px-6 py-3 font-medium text-muted">{t('releases.changeLevel')}</th>
                      <th className="px-6 py-3 font-medium text-muted">{t('releases.state')}</th>
                      <th className="px-6 py-3 font-medium text-muted">{t('releases.riskScore')}</th>
                      <th className="px-6 py-3 font-medium text-muted">{t('releases.date')}</th>
                      <th className="px-6 py-3 font-medium text-muted" />
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-edge">
                    {data.items.map((r) => (
                      <tr key={r.id} className="hover:bg-hover transition-colors">
                        <td className="px-6 py-3 font-mono text-xs text-body">{r.version}</td>
                        <td className="px-6 py-3 text-body capitalize">{r.environment}</td>
                        <td className="px-6 py-3">
                          <Badge variant={changeLevelVariant(r.changeLevel)}>
                            {t(CHANGE_LEVEL_KEYS[r.changeLevel] ?? 'releases.changeLevels.unknown')}
                          </Badge>
                        </td>
                        <td className="px-6 py-3">
                          <Badge variant={stateVariant(r.deploymentState ?? r.status)}>
                            {r.deploymentState ?? r.status}
                          </Badge>
                        </td>
                        <td className="px-6 py-3 text-body">
                          {r.riskScore != null ? (r.riskScore * 100).toFixed(0) + '%' : '—'}
                        </td>
                        <td className="px-6 py-3 text-muted text-xs">
                          {new Date(r.createdAt).toLocaleString()}
                        </td>
                        <td className="px-6 py-3">
                          <Button size="sm" variant="ghost" onClick={() => selectRelease(r.id)}>
                            <Activity size={14} />
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </Card>
          </PageSection>
        </>
      )}
      {activeTab === 'intelligence' && (
        <ReleasesIntelligenceTab
          intel={intel}
          selectedReleaseId={selectedReleaseId}
          isLoading={intelligenceQuery.isLoading}
          isError={intelligenceQuery.isError}
          onStartReview={(releaseId) => startReviewMutation.mutate(releaseId)}
          startReviewPending={startReviewMutation.isPending}
        />
      )}

      {/* ═══════════════════ TIMELINE TAB ═══════════════════ */}
      {activeTab === 'timeline' && (
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Clock size={16} className="text-accent" />
              <h2 className="text-base font-semibold text-heading">{t('releases.timeline.title')}</h2>
            </div>
          </CardHeader>
          <CardBody>
            {!selectedReleaseId ? (
              <p className="py-8 text-sm text-muted text-center">
                {t('releases.intelligence.selectRelease')}
              </p>
            ) : intelligenceQuery.isLoading ? (
              <PageLoadingState size="sm" />
            ) : !intel?.timeline?.length ? (
              <p className="py-8 text-sm text-muted text-center">
                {t('releases.timeline.noEvents')}
              </p>
            ) : (
              <div className="relative pl-6">
                <div className="absolute left-2 top-0 bottom-0 w-px bg-edge" />
                {intel.timeline.map((ev) => (
                  <div key={ev.id} className="relative mb-6 last:mb-0">
                    <div className="absolute -left-4 top-1 h-3 w-3 rounded-full border-2 border-accent bg-canvas" />
                    <div className="ml-4">
                      <div className="flex items-center gap-2 text-xs text-muted mb-1">
                        <span className="font-medium text-heading">{ev.eventType}</span>
                        <span>·</span>
                        <span>{ev.source}</span>
                        <span>·</span>
                        <span>{new Date(ev.occurredAt).toLocaleString()}</span>
                      </div>
                      <p className="text-sm text-body">{ev.description}</p>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardBody>
        </Card>
      )}

      {/* ═══════════════════ FREEZE CALENDAR TAB ═══════════════════ */}
      {activeTab === 'freeze' && (
        <div className="space-y-6">
          {/* Conflict Check */}
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Snowflake size={16} className="text-accent" />
                <h2 className="text-base font-semibold text-heading">{t('releases.freeze.title')}</h2>
              </div>
            </CardHeader>
            <CardBody>
              <div className="flex gap-3 items-end">
                <div className="flex-1">
                  <label className="block text-sm font-medium text-body mb-1">
                    {t('releases.environment')}
                  </label>
                  <input
                    type="text"
                    value={freezeCheckEnv}
                    onChange={(e) => setFreezeCheckEnv(e.target.value)}
                    className={INPUT_CLS}
                    placeholder={t('releases.freezeEnvironmentPlaceholder')}
                  />
                </div>
                <Button
                  variant="secondary"
                  loading={freezeCheckQuery.isFetching}
                  onClick={() => freezeCheckQuery.refetch()}
                >
                  <AlertTriangle size={14} />
                  {t('releases.freeze.checkConflict')}
                </Button>
              </div>

              {freezeCheckQuery.data && (
                <div className="mt-4">
                  {freezeCheckQuery.data.hasConflict ? (
                    <div className="flex items-center gap-2 text-sm text-critical bg-critical/10 rounded-md px-4 py-2">
                      <XCircle size={16} />
                      {t('releases.freeze.conflict')}
                    </div>
                  ) : (
                    <div className="flex items-center gap-2 text-sm text-success bg-success/10 rounded-md px-4 py-2">
                      <CheckCircle2 size={16} />
                      {t('releases.freeze.noConflict')}
                    </div>
                  )}
                </div>
              )}
            </CardBody>
          </Card>

          {/* Freeze Windows Table */}
          {freezeCheckQuery.data?.activeFreezes && freezeCheckQuery.data.activeFreezes.length > 0 && (
            <Card>
              <div className="overflow-x-auto">
                <table className="min-w-full text-sm">
                  <thead>
                    <tr className="border-b border-edge bg-panel text-left">
                      <th className="px-6 py-3 font-medium text-muted">{t('releases.freeze.name')}</th>
                      <th className="px-6 py-3 font-medium text-muted">{t('releases.freeze.reason')}</th>
                      <th className="px-6 py-3 font-medium text-muted">{t('releases.freeze.scope')}</th>
                      <th className="px-6 py-3 font-medium text-muted">{t('releases.freeze.startsAt')}</th>
                      <th className="px-6 py-3 font-medium text-muted">{t('releases.freeze.endsAt')}</th>
                      <th className="px-6 py-3 font-medium text-muted">{t('releases.freeze.status')}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-edge">
                    {freezeCheckQuery.data.activeFreezes.map((fw) => {
                      const now = new Date();
                      const isActive = new Date(fw.startsAt) <= now && now <= new Date(fw.endsAt);
                      return (
                        <tr key={fw.id} className="hover:bg-hover transition-colors">
                          <td className="px-6 py-3 text-heading font-medium">{fw.name}</td>
                          <td className="px-6 py-3 text-body">{fw.reason}</td>
                          <td className="px-6 py-3 text-body">
                            {fw.scope}
                            {fw.scopeValue ? `: ${fw.scopeValue}` : ''}
                          </td>
                          <td className="px-6 py-3 text-muted text-xs">
                            {new Date(fw.startsAt).toLocaleString()}
                          </td>
                          <td className="px-6 py-3 text-muted text-xs">
                            {new Date(fw.endsAt).toLocaleString()}
                          </td>
                          <td className="px-6 py-3">
                            <Badge variant={isActive ? 'danger' : 'default'}>
                              {isActive ? t('releases.freeze.active') : t('releases.freeze.inactive')}
                            </Badge>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </Card>
          )}

          {freezeCheckQuery.data && freezeCheckQuery.data.activeFreezes.length === 0 && (
            <Card>
              <CardBody>
                <p className="py-8 text-sm text-muted text-center">
                  {t('releases.freeze.noWindows')}
                </p>
              </CardBody>
            </Card>
          )}
        </div>
      )}

      {/* ═══════════════════ PRE-PROD COMPARISON TAB ═══════════════════ */}
      {activeTab === 'pre-prod' && (
        <PreProdComparisonPanel
          initialPreProdId={selectedReleaseId ?? ''}
          availableReleases={
            releasesQuery.data?.items?.map((r) => ({
              id: r.id,
              apiAssetId: r.apiAssetId,
              version: r.version,
              environment: r.environment ?? '',
            })) ?? []
          }
        />
      )}

      {/* ═══════════════════ RELEASE NOTES TAB ═══════════════════ */}
      {activeTab === 'release-notes' && (
        <ReleaseNotesPanel releaseId={selectedReleaseId} />
      )}

      {/* ═══════════════════ DEPLOY READINESS TAB ═══════════════════ */}
      {activeTab === 'readiness' && (
        <DeployReadinessPanel
          releaseId={selectedReleaseId}
          environmentName={form.environment || undefined}
        />
      )}
    </PageContainer>
  );
}
