import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Server, Activity, CheckCircle, AlertTriangle,
  Inbox, Bell, Cpu, Database, Cog, BrainCircuit,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer, PageSection } from '../../../components/shell';

// ── Types ──

type Tab = 'health' | 'jobs' | 'queues' | 'events';

type SubsystemStatus = 'Healthy' | 'Degraded' | 'Unhealthy';

interface Subsystem {
  name: string;
  status: SubsystemStatus;
  description: string;
  lastChecked: string;
}

type JobStatus = 'Running' | 'Completed' | 'Failed';

interface BackgroundJob {
  name: string;
  status: JobStatus;
  lastRunAt: string;
  nextRunAt: string;
  executionCount: number;
  failureCount: number;
}

interface QueueInfo {
  name: string;
  pending: number;
  processing: number;
  failed: number;
  deadLetter: number;
  avgProcessingMs: number;
}

type EventSeverity = 'Info' | 'Warning' | 'Error' | 'Critical';

interface OperationalEvent {
  timestamp: string;
  severity: EventSeverity;
  subsystem: string;
  message: string;
  resolved: boolean;
}

// ── Mock data ──

const mockSubsystems: Subsystem[] = [
  { name: 'API', status: 'Healthy', description: 'REST API gateway responding normally', lastChecked: '2024-01-15T10:30:00Z' },
  { name: 'Database', status: 'Healthy', description: 'PostgreSQL cluster operational', lastChecked: '2024-01-15T10:30:00Z' },
  { name: 'BackgroundJobs', status: 'Healthy', description: 'All job processors running', lastChecked: '2024-01-15T10:29:00Z' },
  { name: 'Ingestion', status: 'Healthy', description: 'Data ingestion pipeline active', lastChecked: '2024-01-15T10:30:00Z' },
  { name: 'AI', status: 'Healthy', description: 'AI inference service available', lastChecked: '2024-01-15T10:28:00Z' },
];

const mockJobs: BackgroundJob[] = [
  { name: 'Outbox Processor', status: 'Running', lastRunAt: '2024-01-15T10:29:00Z', nextRunAt: '2024-01-15T10:30:00Z', executionCount: 14520, failureCount: 3 },
  { name: 'Identity Expiration', status: 'Completed', lastRunAt: '2024-01-15T10:00:00Z', nextRunAt: '2024-01-15T11:00:00Z', executionCount: 720, failureCount: 0 },
  { name: 'Analytics Aggregation', status: 'Completed', lastRunAt: '2024-01-15T10:15:00Z', nextRunAt: '2024-01-15T10:45:00Z', executionCount: 2880, failureCount: 12 },
  { name: 'Ingestion Pipeline', status: 'Running', lastRunAt: '2024-01-15T10:28:00Z', nextRunAt: '2024-01-15T10:31:00Z', executionCount: 43200, failureCount: 87 },
];

const mockQueues: QueueInfo[] = [
  { name: 'outbox', pending: 12, processing: 3, failed: 0, deadLetter: 0, avgProcessingMs: 45 },
  { name: 'ingestion', pending: 238, processing: 15, failed: 2, deadLetter: 1, avgProcessingMs: 120 },
  { name: 'ai-requests', pending: 5, processing: 2, failed: 0, deadLetter: 0, avgProcessingMs: 2300 },
  { name: 'analytics', pending: 42, processing: 8, failed: 1, deadLetter: 0, avgProcessingMs: 85 },
];

const mockEvents: OperationalEvent[] = [
  { timestamp: '2024-01-15T10:28:00Z', severity: 'Info', subsystem: 'API', message: 'Certificate renewal completed successfully', resolved: true },
  { timestamp: '2024-01-15T10:15:00Z', severity: 'Warning', subsystem: 'Ingestion', message: 'Ingestion lag exceeded 500ms threshold', resolved: true },
  { timestamp: '2024-01-15T09:45:00Z', severity: 'Error', subsystem: 'AI', message: 'Model inference timeout on gpt-4o endpoint', resolved: true },
  { timestamp: '2024-01-15T09:30:00Z', severity: 'Info', subsystem: 'BackgroundJobs', message: 'Analytics Aggregation job recovered after retry', resolved: true },
  { timestamp: '2024-01-15T08:10:00Z', severity: 'Critical', subsystem: 'Database', message: 'Connection pool exhaustion detected — auto-scaled', resolved: true },
  { timestamp: '2024-01-15T07:00:00Z', severity: 'Warning', subsystem: 'Ingestion', message: 'Dead-letter queue item detected in ingestion pipeline', resolved: false },
  { timestamp: '2024-01-15T06:30:00Z', severity: 'Error', subsystem: 'BackgroundJobs', message: 'Outbox Processor stalled for 120s before recovery', resolved: false },
];

// ── Helpers ──

const subsystemStatusBadge = (s: SubsystemStatus): 'success' | 'warning' | 'danger' => {
  switch (s) {
    case 'Healthy': return 'success';
    case 'Degraded': return 'warning';
    case 'Unhealthy': return 'danger';
  }
};

const jobStatusBadge = (s: JobStatus): 'success' | 'info' | 'danger' => {
  switch (s) {
    case 'Running': return 'info';
    case 'Completed': return 'success';
    case 'Failed': return 'danger';
  }
};

const severityBadge = (s: EventSeverity): 'info' | 'warning' | 'danger' | 'default' => {
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

const formatDate = (iso: string) => {
  try { return new Date(iso).toLocaleString(); }
  catch { return iso; }
};

// ── Page component ──

export function PlatformOperationsPage() {
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState<Tab>('health');
  const [jobFilter, setJobFilter] = useState<'all' | JobStatus>('all');
  const [severityFilter, setSeverityFilter] = useState<'all' | EventSeverity>('all');

  const healthyCount = mockSubsystems.filter(s => s.status === 'Healthy').length;
  const activeJobsCount = mockJobs.filter(j => j.status === 'Running').length;
  const totalPending = mockQueues.reduce((sum, q) => sum + q.pending, 0);
  const unresolvedCount = mockEvents.filter(e => !e.resolved).length;

  const filteredJobs = jobFilter === 'all'
    ? mockJobs
    : mockJobs.filter(j => j.status === jobFilter);

  const filteredEvents = severityFilter === 'all'
    ? mockEvents
    : mockEvents.filter(e => e.severity === severityFilter);

  const tabs: { key: Tab; label: string }[] = [
    { key: 'health', label: t('platformOps.tabHealth') },
    { key: 'jobs', label: t('platformOps.tabJobs') },
    { key: 'queues', label: t('platformOps.tabQueues') },
    { key: 'events', label: t('platformOps.tabEvents') },
  ];

  return (
    <PageContainer>
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('platformOps.title')}</h1>
        <p className="text-muted mt-1">{t('platformOps.subtitle')}</p>
      </div>

      {/* Stats */}
      <PageSection>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <StatCard title={t('platformOps.healthyCount')} value={`${healthyCount}/${mockSubsystems.length}`} icon={<CheckCircle size={20} />} color="text-success" />
          <StatCard title={t('platformOps.activeJobs')} value={activeJobsCount} icon={<Activity size={20} />} color="text-info" />
          <StatCard title={t('platformOps.totalPending')} value={totalPending} icon={<Inbox size={20} />} color="text-warning" />
          <StatCard title={t('platformOps.unresolvedEvents')} value={unresolvedCount} icon={<Bell size={20} />} color="text-critical" />
        </div>
      </PageSection>

      {/* Tabs */}
      <div className="flex gap-1 mb-6 border-b border-edge">
        {tabs.map(tab => (
          <button
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
                <Badge variant="success">{mockSubsystems.every(s => s.status === 'Healthy') ? 'Healthy' : 'Degraded'}</Badge>
              </div>
              <div>
                <span className="text-muted">{t('platformOps.healthVersion')}:</span>{' '}
                <span className="text-heading font-mono">1.0.0-preview</span>
              </div>
              <div>
                <span className="text-muted">{t('platformOps.healthUptime')}:</span>{' '}
                <span className="text-heading font-mono">12d 5h 32m</span>
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
              {mockSubsystems.map(sub => (
                <div key={sub.name} className="grid grid-cols-1 md:grid-cols-4 gap-2 px-4 py-3 items-center">
                  <span className="text-sm font-medium text-heading flex items-center gap-2">
                    {subsystemIcon(sub.name)}
                    {sub.name}
                  </span>
                  <span><Badge variant={subsystemStatusBadge(sub.status)}>{sub.status}</Badge></span>
                  <span className="text-xs text-muted">{sub.description}</span>
                  <span className="text-xs text-muted">{formatDate(sub.lastChecked)}</span>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>
        </PageSection>
      )}

      {/* ── Jobs Tab ── */}
      {activeTab === 'jobs' && (
        <PageSection>
          <div className="flex items-center gap-3 mb-4">
            <select
              value={jobFilter}
              onChange={e => setJobFilter(e.target.value as 'all' | JobStatus)}
              className="px-3 py-2 text-xs rounded-md bg-elevated border border-edge text-body focus:outline-none focus:ring-1 focus:ring-accent"
            >
              <option value="all">{t('platformOps.filterAll')}</option>
              <option value="Running">{t('platformOps.filterRunning')}</option>
              <option value="Completed">{t('platformOps.filterCompleted')}</option>
              <option value="Failed">{t('platformOps.filterFailed')}</option>
            </select>
          </div>
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
                {filteredJobs.length === 0 ? (
                  <div className="p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
                ) : (
                  filteredJobs.map(job => (
                    <div key={job.name} className="grid grid-cols-1 md:grid-cols-6 gap-2 px-4 py-3 items-center">
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
        </PageSection>
      )}

      {/* ── Queues Tab ── */}
      {activeTab === 'queues' && (
        <PageSection>
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
              {mockQueues.map(q => (
                <div key={q.name} className="grid grid-cols-1 md:grid-cols-6 gap-2 px-4 py-3 items-center">
                  <span className="text-sm font-medium text-heading font-mono">{q.name}</span>
                  <span className={`text-xs font-mono text-right ${q.pending > 100 ? 'text-warning' : 'text-heading'}`}>{q.pending.toLocaleString()}</span>
                  <span className="text-xs font-mono text-heading text-right">{q.processing.toLocaleString()}</span>
                  <span className={`text-xs font-mono text-right ${q.failed > 0 ? 'text-critical' : 'text-heading'}`}>{q.failed.toLocaleString()}</span>
                  <span className={`text-xs font-mono text-right ${q.deadLetter > 0 ? 'text-critical' : 'text-heading'}`}>{q.deadLetter.toLocaleString()}</span>
                  <span className="text-xs font-mono text-heading text-right">{q.avgProcessingMs.toLocaleString()}</span>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>
        </PageSection>
      )}

      {/* ── Events Tab ── */}
      {activeTab === 'events' && (
        <PageSection>
          <div className="flex items-center gap-3 mb-4">
            <select
              value={severityFilter}
              onChange={e => setSeverityFilter(e.target.value as 'all' | EventSeverity)}
              className="px-3 py-2 text-xs rounded-md bg-elevated border border-edge text-body focus:outline-none focus:ring-1 focus:ring-accent"
            >
              <option value="all">{t('platformOps.filterAll')}</option>
              <option value="Info">{t('platformOps.filterInfo')}</option>
              <option value="Warning">{t('platformOps.filterWarning')}</option>
              <option value="Error">{t('platformOps.filterError')}</option>
              <option value="Critical">{t('platformOps.filterCritical')}</option>
            </select>
          </div>
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
                {filteredEvents.length === 0 ? (
                  <div className="p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
                ) : (
                  filteredEvents.map((ev, idx) => (
                    <div key={idx} className="grid grid-cols-1 md:grid-cols-5 gap-2 px-4 py-3 items-center">
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
        </PageSection>
      )}
    </PageContainer>
  );
}
