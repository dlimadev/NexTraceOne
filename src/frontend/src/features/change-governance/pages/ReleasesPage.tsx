import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Plus,
  Search,
  RefreshCw,
  Activity,
  Clock,
  Snowflake,
  Eye,
  ShieldAlert,
  Target,
  Gauge,
  Undo2,
  Tag,
  BarChart3,
  CheckCircle2,
  XCircle,
  AlertTriangle,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { changeIntelligenceApi } from '../api';
import type { ChangeLevel, DeploymentState } from '../../../types';

// ─── Constantes auxiliares ───────────────────────────────────────────────────

type TabId = 'overview' | 'intelligence' | 'timeline' | 'freeze';

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

function riskLevel(score: number): 'low' | 'medium' | 'high' {
  if (score < 0.4) return 'low';
  if (score < 0.7) return 'medium';
  return 'high';
}

function riskVariant(score: number): 'success' | 'warning' | 'danger' {
  if (score < 0.4) return 'success';
  if (score < 0.7) return 'warning';
  return 'danger';
}

// ─── Formulário de notificação ───────────────────────────────────────────────

interface NotifyForm {
  apiAssetId: string;
  version: string;
  environment: string;
  commitSha: string;
}

const emptyForm: NotifyForm = { apiAssetId: '', version: '', environment: 'production', commitSha: '' };

// ─── Componente principal ────────────────────────────────────────────────────

export function ReleasesPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const [activeTab, setActiveTab] = useState<TabId>('overview');
  const [selectedReleaseId, setSelectedReleaseId] = useState<string | null>(null);
  const [apiAssetId, setApiAssetId] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<NotifyForm>(emptyForm);
  const [page] = useState(1);
  const [freezeCheckEnv, setFreezeCheckEnv] = useState('');

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
  ];

  const data = releasesQuery.data;
  const intel = intelligenceQuery.data;

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-heading">{t('releases.title')}</h1>
          <p className="text-muted mt-1">{t('releases.subtitle')}</p>
        </div>
        <Button onClick={() => { setActiveTab('overview'); setShowForm((v) => !v); }}>
          <Plus size={16} />
          {t('releases.notifyDeployment')}
        </Button>
      </div>

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
                      <option value="development">{t('releases.environments.development')}</option>
                      <option value="staging">{t('releases.environments.staging')}</option>
                      <option value="production">{t('releases.environments.production')}</option>
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
                {data && <span className="text-sm text-muted">{data.totalCount} total</span>}
              </div>
            </CardHeader>
            <div className="overflow-x-auto">
              {!apiAssetId ? (
                <p className="px-6 py-12 text-sm text-muted text-center">
                  {t('releases.enterApiAssetId')}
                </p>
              ) : releasesQuery.isLoading ? (
                <div className="flex items-center justify-center py-12">
                  <RefreshCw size={20} className="animate-spin text-muted" />
                </div>
              ) : releasesQuery.isError ? (
                <p className="px-6 py-12 text-sm text-critical text-center">
                  {t('releases.loadFailed')}
                </p>
              ) : !data?.items?.length ? (
                <p className="px-6 py-12 text-sm text-muted text-center">
                  {t('releases.noReleases')}
                </p>
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
                          <Badge variant={stateVariant(r.deploymentState)}>
                            {r.deploymentState}
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
        </>
      )}

      {/* ═══════════════════ INTELLIGENCE TAB ═══════════════════ */}
      {activeTab === 'intelligence' && (
        <>
          {!selectedReleaseId ? (
            <Card>
              <CardBody>
                <p className="py-12 text-sm text-muted text-center">
                  {t('releases.intelligence.selectRelease')}
                </p>
              </CardBody>
            </Card>
          ) : intelligenceQuery.isLoading ? (
            <div className="flex items-center justify-center py-16">
              <RefreshCw size={24} className="animate-spin text-muted" />
            </div>
          ) : intelligenceQuery.isError ? (
            <Card>
              <CardBody>
                <p className="py-8 text-sm text-critical text-center">{t('releases.loadFailed')}</p>
              </CardBody>
            </Card>
          ) : intel ? (
            <div className="space-y-6">
              {/* Intelligence Header */}
              <div className="flex items-center gap-3 text-sm text-muted">
                <span className="font-mono text-heading">{intel.release.serviceName}</span>
                <span>v{intel.release.version}</span>
                <Badge variant="info">{intel.release.environment}</Badge>
                <Badge variant={stateVariant(intel.release.status as DeploymentState)}>
                  {intel.release.status}
                </Badge>
              </div>

              <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                {/* ── Score & Risk ──────────────────────────────────────── */}
                <Card>
                  <CardHeader>
                    <div className="flex items-center gap-2">
                      <ShieldAlert size={16} className="text-accent" />
                      <h3 className="text-sm font-semibold text-heading">
                        {t('releases.intelligence.score.title')}
                      </h3>
                    </div>
                  </CardHeader>
                  <CardBody>
                    {intel.score ? (
                      <div className="space-y-4">
                        <div className="flex items-center gap-4">
                          <div className="text-3xl font-bold text-heading">
                            {(intel.score.score * 100).toFixed(0)}%
                          </div>
                          <Badge variant={riskVariant(intel.score.score)}>
                            {t(`releases.intelligence.score.${riskLevel(intel.score.score)}`)}
                          </Badge>
                        </div>
                        <div className="h-2 rounded-full bg-elevated overflow-hidden">
                          <div
                            className={`h-full rounded-full transition-all ${
                              intel.score.score < 0.4
                                ? 'bg-success'
                                : intel.score.score < 0.7
                                  ? 'bg-warning'
                                  : 'bg-critical'
                            }`}
                            style={{ width: `${intel.score.score * 100}%` }}
                          />
                        </div>
                        <div className="grid grid-cols-3 gap-3 text-xs">
                          <div>
                            <span className="text-muted">{t('releases.intelligence.score.breakingChange')}</span>
                            <p className="text-heading font-medium mt-0.5">
                              {(intel.score.breakingChangeWeight * 100).toFixed(0)}%
                            </p>
                          </div>
                          <div>
                            <span className="text-muted">{t('releases.intelligence.score.blastRadius')}</span>
                            <p className="text-heading font-medium mt-0.5">
                              {(intel.score.blastRadiusWeight * 100).toFixed(0)}%
                            </p>
                          </div>
                          <div>
                            <span className="text-muted">{t('releases.intelligence.score.environment')}</span>
                            <p className="text-heading font-medium mt-0.5">
                              {(intel.score.environmentWeight * 100).toFixed(0)}%
                            </p>
                          </div>
                        </div>
                        <p className="text-xs text-muted">
                          {t('releases.intelligence.score.computedAt')}:{' '}
                          {new Date(intel.score.computedAt).toLocaleString()}
                        </p>
                      </div>
                    ) : (
                      <p className="py-4 text-sm text-muted text-center">—</p>
                    )}
                  </CardBody>
                </Card>

                {/* ── Blast Radius ──────────────────────────────────────── */}
                <Card>
                  <CardHeader>
                    <div className="flex items-center gap-2">
                      <Target size={16} className="text-accent" />
                      <h3 className="text-sm font-semibold text-heading">
                        {t('releases.intelligence.blastRadius.title')}
                      </h3>
                    </div>
                  </CardHeader>
                  <CardBody>
                    {intel.blastRadius ? (
                      <div className="space-y-4">
                        <div className="flex items-center gap-3">
                          <span className="text-3xl font-bold text-heading">
                            {intel.blastRadius.totalAffectedConsumers}
                          </span>
                          <span className="text-sm text-muted">
                            {t('releases.intelligence.blastRadius.totalAffected')}
                          </span>
                        </div>
                        <div className="space-y-2">
                          <div>
                            <p className="text-xs font-medium text-muted mb-1">
                              {t('releases.intelligence.blastRadius.direct')} ({intel.blastRadius.directConsumers.length})
                            </p>
                            <div className="flex flex-wrap gap-1">
                              {intel.blastRadius.directConsumers.map((c) => (
                                <Badge key={c} variant="danger">{c}</Badge>
                              ))}
                              {intel.blastRadius.directConsumers.length === 0 && (
                                <span className="text-xs text-muted">—</span>
                              )}
                            </div>
                          </div>
                          <div>
                            <p className="text-xs font-medium text-muted mb-1">
                              {t('releases.intelligence.blastRadius.transitive')} ({intel.blastRadius.transitiveConsumers.length})
                            </p>
                            <div className="flex flex-wrap gap-1">
                              {intel.blastRadius.transitiveConsumers.map((c) => (
                                <Badge key={c} variant="warning">{c}</Badge>
                              ))}
                              {intel.blastRadius.transitiveConsumers.length === 0 && (
                                <span className="text-xs text-muted">—</span>
                              )}
                            </div>
                          </div>
                        </div>
                        <p className="text-xs text-muted">
                          {t('releases.intelligence.blastRadius.calculatedAt')}:{' '}
                          {new Date(intel.blastRadius.calculatedAt).toLocaleString()}
                        </p>
                      </div>
                    ) : (
                      <p className="py-4 text-sm text-muted text-center">—</p>
                    )}
                  </CardBody>
                </Card>

                {/* ── External Markers ──────────────────────────────────── */}
                <Card>
                  <CardHeader>
                    <div className="flex items-center gap-2">
                      <Tag size={16} className="text-accent" />
                      <h3 className="text-sm font-semibold text-heading">
                        {t('releases.intelligence.markers.title')}
                      </h3>
                    </div>
                  </CardHeader>
                  <CardBody>
                    {intel.markers.length === 0 ? (
                      <p className="py-4 text-sm text-muted text-center">
                        {t('releases.intelligence.markers.noMarkers')}
                      </p>
                    ) : (
                      <div className="divide-y divide-edge -mx-6">
                        {intel.markers.map((m) => (
                          <div key={m.id} className="px-6 py-3 flex items-center justify-between text-xs">
                            <div className="space-y-0.5">
                              <p className="text-heading font-medium">{m.markerType}</p>
                              <p className="text-muted">
                                {t('releases.intelligence.markers.source')}: {m.sourceSystem}
                              </p>
                            </div>
                            <div className="text-right space-y-0.5">
                              <p className="text-body font-mono">{m.externalId}</p>
                              <p className="text-muted">
                                {new Date(m.occurredAt).toLocaleString()}
                              </p>
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                  </CardBody>
                </Card>

                {/* ── Baseline ──────────────────────────────────────────── */}
                <Card>
                  <CardHeader>
                    <div className="flex items-center gap-2">
                      <BarChart3 size={16} className="text-accent" />
                      <h3 className="text-sm font-semibold text-heading">
                        {t('releases.intelligence.baseline.title')}
                      </h3>
                    </div>
                  </CardHeader>
                  <CardBody>
                    {intel.baseline ? (
                      <div className="space-y-3">
                        <div className="grid grid-cols-3 gap-3 text-xs">
                          <div>
                            <span className="text-muted">{t('releases.intelligence.baseline.requestsPerMinute')}</span>
                            <p className="text-heading font-medium mt-0.5">
                              {intel.baseline.requestsPerMinute.toLocaleString()}
                            </p>
                          </div>
                          <div>
                            <span className="text-muted">{t('releases.intelligence.baseline.errorRate')}</span>
                            <p className="text-heading font-medium mt-0.5">
                              {(intel.baseline.errorRate * 100).toFixed(2)}%
                            </p>
                          </div>
                          <div>
                            <span className="text-muted">{t('releases.intelligence.baseline.throughput')}</span>
                            <p className="text-heading font-medium mt-0.5">
                              {intel.baseline.throughput.toLocaleString()}
                            </p>
                          </div>
                          <div>
                            <span className="text-muted">{t('releases.intelligence.baseline.avgLatency')}</span>
                            <p className="text-heading font-medium mt-0.5">
                              {intel.baseline.avgLatencyMs.toFixed(1)}
                            </p>
                          </div>
                          <div>
                            <span className="text-muted">{t('releases.intelligence.baseline.p95Latency')}</span>
                            <p className="text-heading font-medium mt-0.5">
                              {intel.baseline.p95LatencyMs.toFixed(1)}
                            </p>
                          </div>
                          <div>
                            <span className="text-muted">{t('releases.intelligence.baseline.p99Latency')}</span>
                            <p className="text-heading font-medium mt-0.5">
                              {intel.baseline.p99LatencyMs.toFixed(1)}
                            </p>
                          </div>
                        </div>
                        <p className="text-xs text-muted">
                          {t('releases.intelligence.baseline.period')}:{' '}
                          {new Date(intel.baseline.collectedFrom).toLocaleDateString()} –{' '}
                          {new Date(intel.baseline.collectedTo).toLocaleDateString()}
                        </p>
                      </div>
                    ) : (
                      <p className="py-4 text-sm text-muted text-center">
                        {t('releases.intelligence.baseline.noBaseline')}
                      </p>
                    )}
                  </CardBody>
                </Card>

                {/* ── Post-Release Review ──────────────────────────────── */}
                <Card>
                  <CardHeader>
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <Gauge size={16} className="text-accent" />
                        <h3 className="text-sm font-semibold text-heading">
                          {t('releases.intelligence.review.title')}
                        </h3>
                      </div>
                      {!intel.postReleaseReview && selectedReleaseId && (
                        <Button
                          size="sm"
                          variant="secondary"
                          loading={startReviewMutation.isPending}
                          onClick={() => startReviewMutation.mutate(selectedReleaseId)}
                        >
                          {t('releases.intelligence.review.startReview')}
                        </Button>
                      )}
                    </div>
                  </CardHeader>
                  <CardBody>
                    {intel.postReleaseReview ? (
                      <div className="space-y-3">
                        <div className="flex items-center gap-3">
                          {intel.postReleaseReview.isCompleted ? (
                            <CheckCircle2 size={18} className="text-success" />
                          ) : (
                            <RefreshCw size={18} className="text-info animate-spin" />
                          )}
                          <span className="text-sm text-heading font-medium">
                            {intel.postReleaseReview.isCompleted
                              ? t('releases.intelligence.review.completed')
                              : t('releases.intelligence.review.inProgress')}
                          </span>
                        </div>
                        <div className="grid grid-cols-2 gap-3 text-xs">
                          <div>
                            <span className="text-muted">{t('releases.intelligence.review.phase')}</span>
                            <p className="text-heading font-medium mt-0.5">
                              {intel.postReleaseReview.currentPhase}
                            </p>
                          </div>
                          <div>
                            <span className="text-muted">{t('releases.intelligence.review.outcome')}</span>
                            <p className="text-heading font-medium mt-0.5">
                              {intel.postReleaseReview.outcome}
                            </p>
                          </div>
                          <div>
                            <span className="text-muted">{t('releases.intelligence.review.confidence')}</span>
                            <p className="text-heading font-medium mt-0.5">
                              {(intel.postReleaseReview.confidenceScore * 100).toFixed(0)}%
                            </p>
                          </div>
                        </div>
                        {intel.postReleaseReview.summary && (
                          <p className="text-xs text-body bg-elevated rounded p-2">
                            {intel.postReleaseReview.summary}
                          </p>
                        )}
                      </div>
                    ) : (
                      <p className="py-4 text-sm text-muted text-center">
                        {t('releases.intelligence.review.noReview')}
                      </p>
                    )}
                  </CardBody>
                </Card>

                {/* ── Rollback Assessment ──────────────────────────────── */}
                <Card>
                  <CardHeader>
                    <div className="flex items-center gap-2">
                      <Undo2 size={16} className="text-accent" />
                      <h3 className="text-sm font-semibold text-heading">
                        {t('releases.intelligence.rollback.title')}
                      </h3>
                    </div>
                  </CardHeader>
                  <CardBody>
                    {intel.rollbackAssessment ? (
                      <div className="space-y-3">
                        <div className="flex items-center gap-3">
                          {intel.rollbackAssessment.isViable ? (
                            <Badge variant="success">
                              <CheckCircle2 size={12} className="mr-1" />
                              {t('releases.intelligence.rollback.viable')}
                            </Badge>
                          ) : (
                            <Badge variant="danger">
                              <XCircle size={12} className="mr-1" />
                              {t('releases.intelligence.rollback.notViable')}
                            </Badge>
                          )}
                        </div>
                        <div className="grid grid-cols-2 gap-3 text-xs">
                          <div>
                            <span className="text-muted">{t('releases.intelligence.rollback.readiness')}</span>
                            <p className="text-heading font-medium mt-0.5">
                              {(intel.rollbackAssessment.readinessScore * 100).toFixed(0)}%
                            </p>
                          </div>
                          <div>
                            <span className="text-muted">{t('releases.intelligence.rollback.previousVersion')}</span>
                            <p className="text-heading font-medium font-mono mt-0.5">
                              {intel.rollbackAssessment.previousVersion ?? '—'}
                            </p>
                          </div>
                          <div>
                            <span className="text-muted">{t('releases.intelligence.rollback.reversibleMigrations')}</span>
                            <p className="text-heading font-medium mt-0.5">
                              {intel.rollbackAssessment.hasReversibleMigrations ? '✓' : '✗'}
                            </p>
                          </div>
                          <div>
                            <span className="text-muted">{t('releases.intelligence.rollback.consumersMigrated')}</span>
                            <p className="text-heading font-medium mt-0.5">
                              {intel.rollbackAssessment.consumersAlreadyMigrated} / {intel.rollbackAssessment.totalConsumersImpacted}
                            </p>
                          </div>
                        </div>
                        <div className="text-xs">
                          <span className="text-muted">{t('releases.intelligence.rollback.recommendation')}</span>
                          <p className="text-body mt-0.5 bg-elevated rounded p-2">
                            {intel.rollbackAssessment.recommendation}
                          </p>
                        </div>
                      </div>
                    ) : (
                      <p className="py-4 text-sm text-muted text-center">
                        {t('releases.intelligence.rollback.noAssessment')}
                      </p>
                    )}
                  </CardBody>
                </Card>
              </div>
            </div>
          ) : null}
        </>
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
              <div className="flex items-center justify-center py-8">
                <RefreshCw size={20} className="animate-spin text-muted" />
              </div>
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
                    placeholder="production"
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
    </div>
  );
}
