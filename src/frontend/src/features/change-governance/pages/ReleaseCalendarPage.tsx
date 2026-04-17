import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Calendar,
  Snowflake,
  Plus,
  Search,
  CheckCircle2,
  AlertTriangle,
  Zap,
  Filter,
  Edit,
  Power,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import {
  changeIntelligenceApi,
  type CalendarReleaseDto,
  type FreezeWindowListDto,
  type CreateFreezeWindowRequest,
  type UpdateFreezeWindowRequest,
} from '../api/changeIntelligence';

// ─── Constants ───────────────────────────────────────────────────────────────

const INPUT_CLS =
  'w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors';

type TabId = 'calendar' | 'freezeWindows';

const SCOPE_OPTIONS = [
  { value: 0, labelKey: 'releaseCalendar.scopeGlobal' },
  { value: 1, labelKey: 'releaseCalendar.scopeTenant' },
  { value: 2, labelKey: 'releaseCalendar.scopeDomain' },
  { value: 3, labelKey: 'releaseCalendar.scopeEnvironment' },
  { value: 4, labelKey: 'releaseCalendar.scopeService' },
];

function defaultDateRange(): { from: string; to: string } {
  const now = new Date();
  const from = new Date(now);
  from.setDate(from.getDate() - 14);
  const to = new Date(now);
  to.setDate(to.getDate() + 14);
  return {
    from: from.toISOString().slice(0, 10),
    to: to.toISOString().slice(0, 10),
  };
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function confidenceVariant(status: string): 'default' | 'success' | 'warning' | 'danger' | 'info' {
  if (status === 'Stable') return 'success';
  if (status === 'SuspectedRegression' || status === 'CorrelatedWithIncident') return 'danger';
  if (status === 'Monitoring') return 'info';
  return 'default';
}

interface FreezeForm {
  name: string;
  reason: string;
  scope: number;
  scopeValue: string;
  startsAt: string;
  endsAt: string;
}

const emptyFreezeForm: FreezeForm = {
  name: '',
  reason: '',
  scope: 0,
  scopeValue: '',
  startsAt: '',
  endsAt: '',
};

// ─── Main Component ──────────────────────────────────────────────────────────

export function ReleaseCalendarPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const queryClient = useQueryClient();

  const defaults = defaultDateRange();
  const [activeTab, setActiveTab] = useState<TabId>('calendar');
  const [from, setFrom] = useState(defaults.from);
  const [to, setTo] = useState(defaults.to);
  const [envFilter, setEnvFilter] = useState('');

  // Freeze CRUD state
  const [showFreezeForm, setShowFreezeForm] = useState(false);
  const [editingFreezeId, setEditingFreezeId] = useState<string | null>(null);
  const [freezeForm, setFreezeForm] = useState<FreezeForm>(emptyFreezeForm);

  const fromIso = new Date(from).toISOString();
  const toIso = new Date(to).toISOString();

  // ── Queries ────────────────────────────────────────────────────────────────

  const calendarQuery = useQuery({
    queryKey: ['release-calendar', from, to, envFilter, activeEnvironmentId],
    queryFn: () =>
      changeIntelligenceApi.getReleaseCalendar(fromIso, toIso, envFilter || undefined),
    enabled: !!from && !!to,
  });

  const freezeListQuery = useQuery({
    queryKey: ['freeze-windows', from, to, envFilter, activeEnvironmentId],
    queryFn: () =>
      changeIntelligenceApi.listFreezeWindows(fromIso, toIso, envFilter || undefined),
    enabled: activeTab === 'freezeWindows' && !!from && !!to,
  });

  // ── Mutations ──────────────────────────────────────────────────────────────

  const createFreezeMutation = useMutation({
    mutationFn: (data: CreateFreezeWindowRequest) => changeIntelligenceApi.createFreezeWindow(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['freeze-windows'] });
      queryClient.invalidateQueries({ queryKey: ['release-calendar'] });
      closeFreezeForm();
    },
  });

  const updateFreezeMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateFreezeWindowRequest }) =>
      changeIntelligenceApi.updateFreezeWindow(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['freeze-windows'] });
      queryClient.invalidateQueries({ queryKey: ['release-calendar'] });
      closeFreezeForm();
    },
  });

  const deactivateFreezeMutation = useMutation({
    mutationFn: (id: string) => changeIntelligenceApi.deactivateFreezeWindow(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['freeze-windows'] });
      queryClient.invalidateQueries({ queryKey: ['release-calendar'] });
    },
  });

  // ── Handlers ───────────────────────────────────────────────────────────────

  const closeFreezeForm = () => {
    setShowFreezeForm(false);
    setEditingFreezeId(null);
    setFreezeForm(emptyFreezeForm);
  };

  const openCreateForm = () => {
    setEditingFreezeId(null);
    setFreezeForm(emptyFreezeForm);
    setShowFreezeForm(true);
  };

  const openEditForm = (fw: FreezeWindowListDto) => {
    setEditingFreezeId(fw.id);
    setFreezeForm({
      name: fw.name,
      reason: fw.reason,
      scope: SCOPE_OPTIONS.findIndex((s) => s.labelKey.endsWith(fw.scope)) ?? 0,
      scopeValue: fw.scopeValue ?? '',
      startsAt: fw.startsAt.slice(0, 16),
      endsAt: fw.endsAt.slice(0, 16),
    });
    setShowFreezeForm(true);
  };

  const handleFreezeSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const payload = {
      name: freezeForm.name,
      reason: freezeForm.reason,
      scope: freezeForm.scope,
      scopeValue: freezeForm.scopeValue || null,
      startsAt: new Date(freezeForm.startsAt).toISOString(),
      endsAt: new Date(freezeForm.endsAt).toISOString(),
    };
    if (editingFreezeId) {
      updateFreezeMutation.mutate({ id: editingFreezeId, data: payload });
    } else {
      createFreezeMutation.mutate(payload);
    }
  };

  // ── Computed data ──────────────────────────────────────────────────────────

  const calendar = calendarQuery.data;

  const releaseDays = useMemo(() => {
    if (!calendar) return new Map<string, CalendarReleaseDto[]>();
    const map = new Map<string, CalendarReleaseDto[]>();
    for (const r of calendar.releases) {
      const day = r.createdAt.slice(0, 10);
      const list = map.get(day) ?? [];
      list.push(r);
      map.set(day, list);
    }
    return map;
  }, [calendar]);

  // ── Tab config ─────────────────────────────────────────────────────────────

  const tabs: { id: TabId; icon: typeof Calendar; labelKey: string }[] = [
    { id: 'calendar', icon: Calendar, labelKey: 'releaseCalendar.tabs.calendar' },
    { id: 'freezeWindows', icon: Snowflake, labelKey: 'releaseCalendar.tabs.freezeWindows' },
  ];

  // ── Render ─────────────────────────────────────────────────────────────────

  return (
    <PageContainer>
      <PageHeader
        title={t('releaseCalendar.title')}
        subtitle={t('releaseCalendar.subtitle')}
        actions={
          activeTab === 'freezeWindows' ? (
            <Button onClick={openCreateForm}>
              <Plus size={16} />
              {t('releases.freeze.create')}
            </Button>
          ) : undefined
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
          </button>
        ))}
      </div>

      {/* Filters */}
      <PageSection>
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Filter size={16} className="text-muted" />
              <span className="text-sm font-medium text-heading">{t('releaseCalendar.filters')}</span>
            </div>
          </CardHeader>
          <CardBody>
            <div className="flex flex-wrap gap-4">
              <div className="flex flex-col gap-1">
                <label className="text-xs text-muted">{t('releaseCalendar.from')}</label>
                <input
                  type="date"
                  className={INPUT_CLS + ' w-44'}
                  value={from}
                  onChange={(e) => setFrom(e.target.value)}
                />
              </div>
              <div className="flex flex-col gap-1">
                <label className="text-xs text-muted">{t('releaseCalendar.to')}</label>
                <input
                  type="date"
                  className={INPUT_CLS + ' w-44'}
                  value={to}
                  onChange={(e) => setTo(e.target.value)}
                />
              </div>
              <div className="flex flex-col gap-1">
                <label className="text-xs text-muted">{t('releaseCalendar.environment')}</label>
                <input
                  type="text"
                  className={INPUT_CLS + ' w-48'}
                  placeholder={t('releases.freezeEnvironmentPlaceholder')}
                  value={envFilter}
                  onChange={(e) => setEnvFilter(e.target.value)}
                />
              </div>
            </div>
          </CardBody>
        </Card>
      </PageSection>

      {/* Tab content */}
      {activeTab === 'calendar' && (
        <CalendarTab
          calendar={calendar}
          releaseDays={releaseDays}
          isLoading={calendarQuery.isLoading}
          isError={calendarQuery.isError}
          t={t}
        />
      )}

      {activeTab === 'freezeWindows' && (
        <FreezeWindowsTab
          freezeList={freezeListQuery.data?.items ?? []}
          isLoading={freezeListQuery.isLoading}
          isError={freezeListQuery.isError}
          showForm={showFreezeForm}
          editingId={editingFreezeId}
          form={freezeForm}
          setForm={setFreezeForm}
          onSubmit={handleFreezeSubmit}
          onClose={closeFreezeForm}
          onEdit={openEditForm}
          onDeactivate={(id) => deactivateFreezeMutation.mutate(id)}
          isSaving={createFreezeMutation.isPending || updateFreezeMutation.isPending}
          t={t}
        />
      )}
    </PageContainer>
  );
}

// ─── Calendar Tab ────────────────────────────────────────────────────────────

function CalendarTab({
  calendar,
  releaseDays,
  isLoading,
  isError,
  t,
}: {
  calendar: ReturnType<typeof changeIntelligenceApi.getReleaseCalendar> extends Promise<infer R> ? R | undefined : never;
  releaseDays: Map<string, CalendarReleaseDto[]>;
  isLoading: boolean;
  isError: boolean;
  t: (key: string) => string;
}) {
  if (isLoading) return <PageLoadingState />;
  if (isError) return <PageErrorState message={t('common.errorLoading')} />;
  if (!calendar) return null;

  const { releases, freezeWindows, dailySummary } = calendar;

  return (
    <div className="space-y-6">
      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card>
          <CardBody>
            <div className="flex items-center gap-3">
              <Zap size={20} className="text-accent" />
              <div>
                <p className="text-2xl font-bold text-heading">{releases.length}</p>
                <p className="text-xs text-muted">{t('releaseCalendar.totalReleases')}</p>
              </div>
            </div>
          </CardBody>
        </Card>
        <Card>
          <CardBody>
            <div className="flex items-center gap-3">
              <AlertTriangle size={20} className="text-warning" />
              <div>
                <p className="text-2xl font-bold text-heading">
                  {releases.filter(
                    (r) =>
                      r.confidenceStatus === 'SuspectedRegression' ||
                      r.confidenceStatus === 'CorrelatedWithIncident',
                  ).length}
                </p>
                <p className="text-xs text-muted">{t('releaseCalendar.highRiskReleases')}</p>
              </div>
            </div>
          </CardBody>
        </Card>
        <Card>
          <CardBody>
            <div className="flex items-center gap-3">
              <Snowflake size={20} className="text-info" />
              <div>
                <p className="text-2xl font-bold text-heading">{freezeWindows.length}</p>
                <p className="text-xs text-muted">{t('releaseCalendar.activeFreezes')}</p>
              </div>
            </div>
          </CardBody>
        </Card>
        <Card>
          <CardBody>
            <div className="flex items-center gap-3">
              <CheckCircle2 size={20} className="text-success" />
              <div>
                <p className="text-2xl font-bold text-heading">
                  {dailySummary.length > 0
                    ? (dailySummary.reduce((a, d) => a + d.averageScore, 0) / dailySummary.length).toFixed(1)
                    : '–'}
                </p>
                <p className="text-xs text-muted">{t('releaseCalendar.avgScore')}</p>
              </div>
            </div>
          </CardBody>
        </Card>
      </div>

      {/* Active Freeze Windows */}
      {freezeWindows.length > 0 && (
        <PageSection>
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Snowflake size={16} className="text-info" />
                <span className="text-sm font-medium text-heading">
                  {t('releaseCalendar.activeFreezeWindows')}
                </span>
              </div>
            </CardHeader>
            <CardBody>
              <div className="space-y-2">
                {freezeWindows.map((fw) => (
                  <div
                    key={fw.freezeWindowId}
                    className="flex items-center justify-between p-3 rounded-lg bg-info/5 border border-info/20"
                  >
                    <div className="flex items-center gap-3">
                      <Snowflake size={14} className="text-info" />
                      <div>
                        <p className="text-sm font-medium text-heading">{fw.name}</p>
                        <p className="text-xs text-muted">
                          {formatDateTime(fw.startsAt)} → {formatDateTime(fw.endsAt)}
                        </p>
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <Badge variant="info">{fw.scope}</Badge>
                      {fw.scopeValue && (
                        <span className="text-xs text-muted">{fw.scopeValue}</span>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </CardBody>
          </Card>
        </PageSection>
      )}

      {/* Daily Heatmap / Summary */}
      {dailySummary.length > 0 && (
        <PageSection>
          <Card>
            <CardHeader>
              <span className="text-sm font-medium text-heading">
                {t('releaseCalendar.dailySummary')}
              </span>
            </CardHeader>
            <CardBody>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="text-left text-xs text-muted border-b border-edge">
                      <th className="pb-2 pr-4">{t('releaseCalendar.date')}</th>
                      <th className="pb-2 pr-4">{t('releaseCalendar.totalReleases')}</th>
                      <th className="pb-2 pr-4">{t('releaseCalendar.highRiskReleases')}</th>
                      <th className="pb-2">{t('releaseCalendar.avgScore')}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {dailySummary.map((d) => (
                      <tr key={d.date} className="border-b border-edge/50">
                        <td className="py-2 pr-4 text-heading">{formatDate(d.date)}</td>
                        <td className="py-2 pr-4">{d.totalReleases}</td>
                        <td className="py-2 pr-4">
                          {d.highRiskReleases > 0 ? (
                            <Badge variant="danger">{d.highRiskReleases}</Badge>
                          ) : (
                            <span className="text-muted">0</span>
                          )}
                        </td>
                        <td className="py-2">{d.averageScore.toFixed(1)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </CardBody>
          </Card>
        </PageSection>
      )}

      {/* Releases per day */}
      <PageSection>
        <Card>
          <CardHeader>
            <span className="text-sm font-medium text-heading">
              {t('releaseCalendar.releaseTimeline')}
            </span>
          </CardHeader>
          <CardBody>
            {releases.length === 0 ? (
              <div className="text-center py-8 text-muted">
                <Search size={32} className="mx-auto mb-2 opacity-50" />
                <p>{t('releaseCalendar.noReleases')}</p>
              </div>
            ) : (
              <div className="space-y-4">
                {[...releaseDays.entries()]
                  .sort(([a], [b]) => a.localeCompare(b))
                  .map(([day, dayReleases]) => (
                    <div key={day}>
                      <p className="text-xs font-semibold text-muted mb-2 uppercase">
                        {formatDate(day)} · {dayReleases.length} {t('releaseCalendar.releasesLabel')}
                      </p>
                      <div className="space-y-1">
                        {dayReleases.map((r) => (
                          <div
                            key={r.releaseId}
                            className="flex items-center justify-between p-2.5 rounded-md bg-surface border border-edge/50 hover:border-edge transition-colors"
                          >
                            <div className="flex items-center gap-3 min-w-0">
                              <Zap size={14} className="text-accent shrink-0" />
                              <div className="min-w-0">
                                <span className="text-sm font-medium text-heading truncate block">
                                  {r.serviceName}
                                </span>
                                <span className="text-xs text-muted">
                                  v{r.version} · {r.environment}
                                  {r.teamName && ` · ${r.teamName}`}
                                </span>
                              </div>
                            </div>
                            <div className="flex items-center gap-2 shrink-0">
                              <Badge variant={confidenceVariant(r.confidenceStatus)}>
                                {r.confidenceStatus}
                              </Badge>
                              <span className="text-xs text-muted">
                                {t('releaseCalendar.score')}: {r.changeScore}
                              </span>
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  ))}
              </div>
            )}
          </CardBody>
        </Card>
      </PageSection>
    </div>
  );
}

// ─── Freeze Windows Tab ──────────────────────────────────────────────────────

function FreezeWindowsTab({
  freezeList,
  isLoading,
  isError,
  showForm,
  editingId,
  form,
  setForm,
  onSubmit,
  onClose,
  onEdit,
  onDeactivate,
  isSaving,
  t,
}: {
  freezeList: FreezeWindowListDto[];
  isLoading: boolean;
  isError: boolean;
  showForm: boolean;
  editingId: string | null;
  form: FreezeForm;
  setForm: (f: FreezeForm) => void;
  onSubmit: (e: React.FormEvent) => void;
  onClose: () => void;
  onEdit: (fw: FreezeWindowListDto) => void;
  onDeactivate: (id: string) => void;
  isSaving: boolean;
  t: (key: string) => string;
}) {
  if (isLoading) return <PageLoadingState />;
  if (isError) return <PageErrorState message={t('common.errorLoading')} />;

  return (
    <div className="space-y-6">
      {/* Create/Edit Form */}
      {showForm && (
        <PageSection>
          <Card>
            <CardHeader>
              <span className="text-sm font-medium text-heading">
                {editingId ? t('releaseCalendar.editFreezeWindow') : t('releases.freeze.create')}
              </span>
            </CardHeader>
            <CardBody>
              <form onSubmit={onSubmit} className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="flex flex-col gap-1">
                    <label className="text-xs text-muted">{t('releases.freeze.name')}</label>
                    <input
                      type="text"
                      className={INPUT_CLS}
                      value={form.name}
                      onChange={(e) => setForm({ ...form, name: e.target.value })}
                      required
                    />
                  </div>
                  <div className="flex flex-col gap-1">
                    <label className="text-xs text-muted">{t('releases.freeze.scope')}</label>
                    <select
                      className={INPUT_CLS}
                      value={form.scope}
                      onChange={(e) => setForm({ ...form, scope: Number(e.target.value) })}
                    >
                      {SCOPE_OPTIONS.map((s) => (
                        <option key={s.value} value={s.value}>
                          {t(s.labelKey)}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div className="flex flex-col gap-1 md:col-span-2">
                    <label className="text-xs text-muted">{t('releases.freeze.reason')}</label>
                    <textarea
                      className={INPUT_CLS + ' min-h-[60px]'}
                      value={form.reason}
                      onChange={(e) => setForm({ ...form, reason: e.target.value })}
                      required
                    />
                  </div>
                  {form.scope > 0 && (
                    <div className="flex flex-col gap-1">
                      <label className="text-xs text-muted">{t('releases.freeze.scopeValue')}</label>
                      <input
                        type="text"
                        className={INPUT_CLS}
                        value={form.scopeValue}
                        onChange={(e) => setForm({ ...form, scopeValue: e.target.value })}
                      />
                    </div>
                  )}
                  <div className="flex flex-col gap-1">
                    <label className="text-xs text-muted">{t('releases.freeze.startsAt')}</label>
                    <input
                      type="datetime-local"
                      className={INPUT_CLS}
                      value={form.startsAt}
                      onChange={(e) => setForm({ ...form, startsAt: e.target.value })}
                      required
                    />
                  </div>
                  <div className="flex flex-col gap-1">
                    <label className="text-xs text-muted">{t('releases.freeze.endsAt')}</label>
                    <input
                      type="datetime-local"
                      className={INPUT_CLS}
                      value={form.endsAt}
                      onChange={(e) => setForm({ ...form, endsAt: e.target.value })}
                      required
                    />
                  </div>
                </div>
                <div className="flex gap-2 pt-2">
                  <Button type="submit" disabled={isSaving}>
                    {isSaving ? t('common.loading') : editingId ? t('common.save') : t('common.create')}
                  </Button>
                  <Button type="button" variant="ghost" onClick={onClose}>
                    {t('common.cancel')}
                  </Button>
                </div>
              </form>
            </CardBody>
          </Card>
        </PageSection>
      )}

      {/* Freeze Windows List */}
      <PageSection>
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Snowflake size={16} className="text-info" />
              <span className="text-sm font-medium text-heading">
                {t('releaseCalendar.freezeWindowsList')}
              </span>
              <Badge variant="default">{freezeList.length} {t('common.total')}</Badge>
            </div>
          </CardHeader>
          <CardBody>
            {freezeList.length === 0 ? (
              <div className="text-center py-8 text-muted">
                <Snowflake size={32} className="mx-auto mb-2 opacity-50" />
                <p>{t('releases.freeze.noWindows')}</p>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="text-left text-xs text-muted border-b border-edge">
                      <th className="pb-2 pr-4">{t('releases.freeze.name')}</th>
                      <th className="pb-2 pr-4">{t('releases.freeze.scope')}</th>
                      <th className="pb-2 pr-4">{t('releases.freeze.startsAt')}</th>
                      <th className="pb-2 pr-4">{t('releases.freeze.endsAt')}</th>
                      <th className="pb-2 pr-4">{t('releases.freeze.status')}</th>
                      <th className="pb-2">{t('common.actions')}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {freezeList.map((fw) => (
                      <tr key={fw.id} className="border-b border-edge/50">
                        <td className="py-2.5 pr-4">
                          <div>
                            <p className="text-heading font-medium">{fw.name}</p>
                            <p className="text-xs text-muted truncate max-w-[200px]">{fw.reason}</p>
                          </div>
                        </td>
                        <td className="py-2.5 pr-4">
                          <Badge variant="default">{fw.scope}</Badge>
                          {fw.scopeValue && (
                            <span className="text-xs text-muted ml-1">{fw.scopeValue}</span>
                          )}
                        </td>
                        <td className="py-2.5 pr-4 text-xs">{formatDateTime(fw.startsAt)}</td>
                        <td className="py-2.5 pr-4 text-xs">{formatDateTime(fw.endsAt)}</td>
                        <td className="py-2.5 pr-4">
                          {fw.isActive ? (
                            <Badge variant="success">{t('releases.freeze.active')}</Badge>
                          ) : (
                            <Badge variant="default">{t('releases.freeze.inactive')}</Badge>
                          )}
                        </td>
                        <td className="py-2.5">
                          <div className="flex gap-1">
                            {fw.isActive && (
                              <>
                                <button
                                  onClick={() => onEdit(fw)}
                                  className="p-1.5 rounded hover:bg-surface text-muted hover:text-heading transition-colors"
                                  title={t('common.edit')}
                                >
                                  <Edit size={14} />
                                </button>
                                <button
                                  onClick={() => onDeactivate(fw.id)}
                                  className="p-1.5 rounded hover:bg-danger/10 text-muted hover:text-danger transition-colors"
                                  title={t('releaseCalendar.deactivate')}
                                >
                                  <Power size={14} />
                                </button>
                              </>
                            )}
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </CardBody>
        </Card>
      </PageSection>
    </div>
  );
}

export { ReleaseCalendarPage as default };
