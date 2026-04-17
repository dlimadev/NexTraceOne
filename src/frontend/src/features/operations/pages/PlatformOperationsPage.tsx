import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Server, Activity, CheckCircle, AlertTriangle,
  Inbox, Bell, Cpu, Database, Cog, BrainCircuit,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { platformOpsApi } from '../api/platformOps';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import type {
  PlatformSubsystemStatus,
  BackgroundJobStatus,
  PlatformEventSeverity,
} from '../../../types';

// ── Helpers ──

type Tab = 'health' | 'jobs' | 'queues' | 'events';

const subsystemStatusBadge = (s: PlatformSubsystemStatus): 'success' | 'warning' | 'danger' => {
  switch (s) {
    case 'Healthy': return 'success';
    case 'Degraded': return 'warning';
    case 'Unhealthy': return 'danger';
  }
};

const jobStatusBadge = (s: BackgroundJobStatus): 'success' | 'info' | 'danger' | 'default' => {
  switch (s) {
    case 'Running': return 'info';
    case 'Completed': return 'success';
    case 'Failed': return 'danger';
    default: return 'default';
  }
};

const severityBadge = (s: PlatformEventSeverity): 'info' | 'warning' | 'danger' | 'default' => {
  switch (s) {
    case 'Info': return 'info';
    case 'Warning': return 'warning';
    case 'Error': return 'danger';
    case 'Critical': return 'default';
  }
};

const subsystemIcon = (name: string) => {
  switch (name) {
    case 'API': return <Server size={14} className="text-accent shrink-0" />;
    case 'Database': return <Database size={14} className="text-accent shrink-0" />;
    case 'BackgroundJobs': return <Cog size={14} className="text-accent shrink-0" />;
    case 'Ingestion': return <Inbox size={14} className="text-accent shrink-0" />;
    case 'AI': return <BrainCircuit size={14} className="text-accent shrink-0" />;
    default: return <Cpu size={14} className="text-accent shrink-0" />;
  }
};

const formatDate = (iso: string | null | undefined) => {
  if (!iso) return '—';
  try { return new Date(iso).toLocaleString(); }
  catch { return iso; }
};

// ── Page component ──

export function PlatformOperationsPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [activeTab, setActiveTab] = useState<Tab>('health');
  const [jobFilter, setJobFilter] = useState<'all' | BackgroundJobStatus>('all');
  const [severityFilter, setSeverityFilter] = useState<'all' | PlatformEventSeverity>('all');

  const healthQuery = useQuery({
    queryKey: ['platform-health', activeEnvironmentId],
    queryFn: () => platformOpsApi.getHealth(),
    staleTime: 15_000,
  });

  const jobsQuery = useQuery({
    queryKey: ['platform-jobs', jobFilter, activeEnvironmentId],
    queryFn: () => platformOpsApi.getJobs(jobFilter !== 'all' ? { status: jobFilter } : {}),
    staleTime: 15_000,
  });

  const queuesQuery = useQuery({
    queryKey: ['platform-queues', activeEnvironmentId],
    queryFn: () => platformOpsApi.getQueues(),
    staleTime: 15_000,
  });

  const eventsQuery = useQuery({
    queryKey: ['platform-events', severityFilter, activeEnvironmentId],
    queryFn: () => platformOpsApi.getEvents(severityFilter !== 'all' ? { severity: severityFilter } : {}),
    staleTime: 15_000,
  });

  const subsystems = healthQuery.data?.subsystems ?? [];
  const jobs = jobsQuery.data?.jobs ?? [];
  const queues = queuesQuery.data?.queues ?? [];
  const events = eventsQuery.data?.events ?? [];

  const healthyCount = subsystems.filter(s => s.status === 'Healthy').length;
  const activeJobsCount = jobs.filter(j => j.status === 'Running').length;
  const totalPending = queues.reduce((sum, q) => sum + q.pendingCount, 0);
  const unresolvedCount = events.filter(e => !e.resolved).length;

  const tabs: { key: Tab; label: string }[] = [
    { key: 'health', label: t('platformOps.tabHealth') },
    { key: 'jobs', label: t('platformOps.tabJobs') },
    { key: 'queues', label: t('platformOps.tabQueues') },
    { key: 'events', label: t('platformOps.tabEvents') },
  ];

  return (
    <PageContainer>
      {/* Header */}
      <PageHeader
        title={t('platformOps.title')}
        subtitle={t('platformOps.subtitle')}
      />

      {/* Stats — use real counts from loaded data */}
      <PageSection>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <StatCard
            title={t('platformOps.healthyCount')}
            value={healthQuery.isLoading ? '…' : `${healthyCount}/${subsystems.length}`}
            icon={<CheckCircle size={20} />}
            color="text-success"
          />
          <StatCard
            title={t('platformOps.activeJobs')}
            value={jobsQuery.isLoading ? '…' : activeJobsCount}
            icon={<Activity size={20} />}
            color="text-info"
          />
          <StatCard
            title={t('platformOps.totalPending')}
            value={queuesQuery.isLoading ? '…' : totalPending}
            icon={<Inbox size={20} />}
            color="text-warning"
          />
          <StatCard
            title={t('platformOps.unresolvedEvents')}
            value={eventsQuery.isLoading ? '…' : unresolvedCount}
            icon={<Bell size={20} />}
            color="text-critical"
          />
        </div>
      </PageSection>

      {/* Tabs */}
      <div className="flex gap-1 mb-6 border-b border-edge">
        {tabs.map(tab => (
          <button
            type="button"
            key={tab.key}
            onClick={() => setActiveTab(tab.key)}
            className={`px-4 py-2 text-sm font-medium transition-colors ${
              activeTab === tab.key
                ? 'text-accent border-b-2 border-accent -mb-px'
                : 'text-muted hover:text-body'
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* ── Health Tab ── */}
      {activeTab === 'health' && (
        <PageSection>
          {healthQuery.isLoading && <PageLoadingState message={t('common.loading')} />}
          {healthQuery.isError && (
            <PageErrorState
              action={
                <button type="button" onClick={() => healthQuery.refetch()} className="px-3 py-2 rounded-md bg-panel border border-edge text-heading text-xs hover:border-accent/50">
                  {t('common.retry')}
                </button>
              }
            />
          )}
          {!healthQuery.isLoading && !healthQuery.isError && healthQuery.data && (
            <Card>
              <CardHeader>
                <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                  <Server size={16} className="text-accent" />
                  {t('platformOps.healthTitle')}
                </h2>
              </CardHeader>
              <CardBody>
                {/* Overall status row */}
                <div className="flex flex-wrap gap-6 mb-6 text-sm">
                  <div>
                    <span className="text-muted">{t('platformOps.healthOverall')}:</span>{' '}
                    <Badge variant={subsystemStatusBadge(healthQuery.data.overallStatus)}>{healthQuery.data.overallStatus}</Badge>
                  </div>
                  <div>
                    <span className="text-muted">{t('platformOps.healthVersion')}:</span>{' '}
                    <span className="text-heading font-mono">{healthQuery.data.version}</span>
                  </div>
                  <div>
                    <span className="text-muted">{t('platformOps.healthUptime')}:</span>{' '}
                    <span className="text-heading font-mono">{formatUptime(healthQuery.data.uptimeSeconds)}</span>
                  </div>
                </div>

                {/* Subsystems table */}
                <div className="hidden md:grid grid-cols-4 gap-2 px-4 py-2 text-xs font-semibold text-muted uppercase tracking-wider border-b border-edge">
                  <span>{t('platformOps.healthSubsystem')}</span>
                  <span>{t('platformOps.healthStatus')}</span>
                  <span>{t('platformOps.healthDescription')}</span>
                  <span>{t('platformOps.healthLastChecked')}</span>
                </div>
                <div className="divide-y divide-edge">
                  {subsystems.map(sub => (
                    <div key={sub.name} className="grid grid-cols-1 md:grid-cols-4 gap-2 px-4 py-3 items-center">
                      <span className="text-sm font-medium text-heading flex items-center gap-2">
                        {subsystemIcon(sub.name)}
                        {sub.name}
                      </span>
                      <span><Badge variant={subsystemStatusBadge(sub.status)}>{sub.status}</Badge></span>
                      <span className="text-xs text-muted">{sub.description}</span>
                      <span className="text-xs text-muted">{formatDate(sub.lastCheckedAt)}</span>
                    </div>
                  ))}
                </div>
              </CardBody>
            </Card>
          )}
        </PageSection>
      )}

      {/* ── Jobs Tab ── */}
      {activeTab === 'jobs' && (
        <PageSection>
          <div className="flex items-center gap-3 mb-4">
            <select
              value={jobFilter}
              onChange={e => setJobFilter(e.target.value as 'all' | BackgroundJobStatus)}
              className="px-3 py-2 text-xs rounded-md bg-elevated border border-edge text-body focus:outline-none focus:ring-1 focus:ring-accent"
            >
              <option value="all">{t('platformOps.filterAll')}</option>
              <option value="Running">{t('platformOps.filterRunning')}</option>
              <option value="Completed">{t('platformOps.filterCompleted')}</option>
              <option value="Failed">{t('platformOps.filterFailed')}</option>
            </select>
          </div>
          {jobsQuery.isLoading && <PageLoadingState message={t('common.loading')} />}
          {jobsQuery.isError && (
            <PageErrorState
              action={
                <button type="button" onClick={() => jobsQuery.refetch()} className="px-3 py-2 rounded-md bg-panel border border-edge text-heading text-xs hover:border-accent/50">
                  {t('common.retry')}
                </button>
              }
            />
          )}
          {!jobsQuery.isLoading && !jobsQuery.isError && (
            <Card>
              <CardHeader>
                <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                  <Cog size={16} className="text-accent" />
                  {t('platformOps.jobsTitle')}
                </h2>
              </CardHeader>
              <CardBody className="p-0">
                <div className="hidden md:grid grid-cols-6 gap-2 px-4 py-2 text-xs font-semibold text-muted uppercase tracking-wider border-b border-edge">
                  <span>{t('platformOps.jobName')}</span>
                  <span>{t('platformOps.jobStatus')}</span>
                  <span>{t('platformOps.jobLastRun')}</span>
                  <span>{t('platformOps.jobNextRun')}</span>
                  <span className="text-right">{t('platformOps.jobExecutions')}</span>
                  <span className="text-right">{t('platformOps.jobFailures')}</span>
                </div>
                <div className="divide-y divide-edge">
                  {jobs.length === 0 ? (
                    <div className="p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
                  ) : (
                    jobs.map(job => (
                      <div key={job.jobId} className="grid grid-cols-1 md:grid-cols-6 gap-2 px-4 py-3 items-center">
                        <span className="text-sm font-medium text-heading flex items-center gap-2">
                          <Activity size={14} className="text-accent shrink-0 hidden md:inline" />
                          {job.name}
                        </span>
                        <span><Badge variant={jobStatusBadge(job.status)}>{job.status}</Badge></span>
                        <span className="text-xs text-muted">{formatDate(job.lastRunAt)}</span>
                        <span className="text-xs text-muted">{formatDate(job.nextRunAt)}</span>
                        <span className="text-xs font-mono text-heading text-right">{job.executionCount.toLocaleString()}</span>
                        <span className={`text-xs font-mono text-right ${job.failureCount > 0 ? 'text-critical' : 'text-heading'}`}>{job.failureCount.toLocaleString()}</span>
                      </div>
                    ))
                  )}
                </div>
              </CardBody>
            </Card>
          )}
        </PageSection>
      )}

      {/* ── Queues Tab ── */}
      {activeTab === 'queues' && (
        <PageSection>
          {queuesQuery.isLoading && <PageLoadingState message={t('common.loading')} />}
          {queuesQuery.isError && (
            <PageErrorState
              action={
                <button type="button" onClick={() => queuesQuery.refetch()} className="px-3 py-2 rounded-md bg-panel border border-edge text-heading text-xs hover:border-accent/50">
                  {t('common.retry')}
                </button>
              }
            />
          )}
          {!queuesQuery.isLoading && !queuesQuery.isError && (
            <Card>
              <CardHeader>
                <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                  <Inbox size={16} className="text-accent" />
                  {t('platformOps.queuesTitle')}
                </h2>
              </CardHeader>
              <CardBody className="p-0">
                <div className="hidden md:grid grid-cols-6 gap-2 px-4 py-2 text-xs font-semibold text-muted uppercase tracking-wider border-b border-edge">
                  <span>{t('platformOps.queueName')}</span>
                  <span className="text-right">{t('platformOps.queuePending')}</span>
                  <span className="text-right">{t('platformOps.queueProcessing')}</span>
                  <span className="text-right">{t('platformOps.queueFailed')}</span>
                  <span className="text-right">{t('platformOps.queueDeadLetter')}</span>
                  <span className="text-right">{t('platformOps.queueAvgMs')}</span>
                </div>
                <div className="divide-y divide-edge">
                  {queues.length === 0 ? (
                    <div className="p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
                  ) : (
                    queues.map(q => (
                      <div key={q.queueName} className="grid grid-cols-1 md:grid-cols-6 gap-2 px-4 py-3 items-center">
                        <span className="text-sm font-medium text-heading font-mono">{q.queueName}</span>
                        <span className={`text-xs font-mono text-right ${q.pendingCount > 100 ? 'text-warning' : 'text-heading'}`}>{q.pendingCount.toLocaleString()}</span>
                        <span className="text-xs font-mono text-heading text-right">{q.processingCount.toLocaleString()}</span>
                        <span className={`text-xs font-mono text-right ${q.failedCount > 0 ? 'text-critical' : 'text-heading'}`}>{q.failedCount.toLocaleString()}</span>
                        <span className={`text-xs font-mono text-right ${q.deadLetterCount > 0 ? 'text-critical' : 'text-heading'}`}>{q.deadLetterCount.toLocaleString()}</span>
                        <span className="text-xs font-mono text-heading text-right">{q.averageProcessingMs.toLocaleString()}</span>
                      </div>
                    ))
                  )}
                </div>
              </CardBody>
            </Card>
          )}
        </PageSection>
      )}

      {/* ── Events Tab ── */}
      {activeTab === 'events' && (
        <PageSection>
          <div className="flex items-center gap-3 mb-4">
            <select
              value={severityFilter}
              onChange={e => setSeverityFilter(e.target.value as 'all' | PlatformEventSeverity)}
              className="px-3 py-2 text-xs rounded-md bg-elevated border border-edge text-body focus:outline-none focus:ring-1 focus:ring-accent"
            >
              <option value="all">{t('platformOps.filterAll')}</option>
              <option value="Info">{t('platformOps.filterInfo')}</option>
              <option value="Warning">{t('platformOps.filterWarning')}</option>
              <option value="Error">{t('platformOps.filterError')}</option>
              <option value="Critical">{t('platformOps.filterCritical')}</option>
            </select>
          </div>
          {eventsQuery.isLoading && <PageLoadingState message={t('common.loading')} />}
          {eventsQuery.isError && (
            <PageErrorState
              action={
                <button type="button" onClick={() => eventsQuery.refetch()} className="px-3 py-2 rounded-md bg-panel border border-edge text-heading text-xs hover:border-accent/50">
                  {t('common.retry')}
                </button>
              }
            />
          )}
          {!eventsQuery.isLoading && !eventsQuery.isError && (
            <Card>
              <CardHeader>
                <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                  <AlertTriangle size={16} className="text-accent" />
                  {t('platformOps.eventsTitle')}
                </h2>
              </CardHeader>
              <CardBody className="p-0">
                <div className="hidden md:grid grid-cols-5 gap-2 px-4 py-2 text-xs font-semibold text-muted uppercase tracking-wider border-b border-edge">
                  <span>{t('platformOps.eventTimestamp')}</span>
                  <span>{t('platformOps.eventSeverity')}</span>
                  <span>{t('platformOps.eventSubsystem')}</span>
                  <span>{t('platformOps.eventMessage')}</span>
                  <span className="text-right">{t('platformOps.eventResolved')}</span>
                </div>
                <div className="divide-y divide-edge">
                  {events.length === 0 ? (
                    <div className="p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
                  ) : (
                    events.map((ev) => (
                      <div key={ev.eventId} className="grid grid-cols-1 md:grid-cols-5 gap-2 px-4 py-3 items-center">
                        <span className="text-xs text-muted font-mono">{formatDate(ev.timestamp)}</span>
                        <span><Badge variant={severityBadge(ev.severity)}>{ev.severity}</Badge></span>
                        <span className="text-xs text-muted">{ev.subsystem}</span>
                        <span className="text-sm text-body">{ev.message}</span>
                        <span className="text-right">
                          <Badge variant={ev.resolved ? 'success' : 'danger'}>
                            {ev.resolved ? t('platformOps.yes') : t('platformOps.no')}
                          </Badge>
                        </span>
                      </div>
                    ))
                  )}
                </div>
              </CardBody>
            </Card>
          )}
        </PageSection>
      )}
    </PageContainer>
  );
}

function formatUptime(seconds: number): string {
  if (seconds < 60) return `${seconds}s`;
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m`;
  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;
  if (hours < 24) return remainingMinutes > 0 ? `${hours}h ${remainingMinutes}m` : `${hours}h`;
  const days = Math.floor(hours / 24);
  const remainingHours = hours % 24;
  return remainingHours > 0 ? `${days}d ${remainingHours}h` : `${days}d`;
}
