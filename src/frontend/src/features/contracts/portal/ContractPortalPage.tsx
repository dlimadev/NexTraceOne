import { useState, useMemo } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  ArrowLeft, ExternalLink, Copy, BookOpen,
  Code, Shield, GitCompare, Users, Clock,
  FileText, Target, MessageSquare, History,
  Zap, Cog, Globe, AlertTriangle, ChevronRight,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';
import { PageContainer } from '../../../components/shell';
import { ProtocolBadge, LifecycleBadge } from '../shared/components';
import { LoadingState, ErrorState } from '../shared/components/StateIndicators';
import { contractsApi } from '../api/contracts';
import { toStudioContract } from '../workspace/toStudioContract';
import { cn } from '../../../lib/cn';
import type { ContractVersion, ContractVersionDetail } from '../../../types';

type PortalTab =
  | 'overview'
  | 'endpoints'
  | 'schemas'
  | 'security'
  | 'versions'
  | 'changelog'
  | 'examples'
  | 'glossary';

/**
 * Portal de consumo de contrato — visão limpa, read-only e centrada no consumidor.
 * Separado do editor interno (studio), foca em overview, endpoints, schemas,
 * security, versões, changelog, examples, onboarding, owners e glossary.
 *
 * Suporta REST, SOAP, Event API e Workservice com informação adaptada.
 */
export function ContractPortalPage() {
  const { contractVersionId } = useParams<{ contractVersionId: string }>();
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState<PortalTab>('overview');

  const detailQuery = useQuery({
    queryKey: ['contract-detail', contractVersionId],
    queryFn: () => contractsApi.getDetail(contractVersionId!),
    enabled: !!contractVersionId,
  });

  const violationsQuery = useQuery({
    queryKey: ['contract-violations', contractVersionId],
    queryFn: () => contractsApi.listRuleViolations(contractVersionId!),
    enabled: !!contractVersionId,
  });

  const historyQuery = useQuery({
    queryKey: ['contract-history', detailQuery.data?.apiAssetId],
    queryFn: () => contractsApi.getHistory(detailQuery.data!.apiAssetId),
    enabled: !!detailQuery.data?.apiAssetId,
  });

  const studio = useMemo(() => {
    if (!detailQuery.data) return null;
    return toStudioContract(detailQuery.data);
  }, [detailQuery.data]);

  if (detailQuery.isLoading) return <PageContainer><LoadingState /></PageContainer>;
  if (detailQuery.isError || !detailQuery.data || !studio) return <PageContainer><ErrorState onRetry={() => detailQuery.refetch()} /></PageContainer>;

  const detail = detailQuery.data;
  const violations = violationsQuery.data ?? [];
  const versions = historyQuery.data ?? [];
  const baseUrl = detail.routePattern || '';

  const tabs: { id: PortalTab; labelKey: string; icon: React.ReactNode }[] = [
    { id: 'overview', labelKey: 'contracts.portal.tabs.overview', icon: <FileText size={13} /> },
    { id: 'endpoints', labelKey: 'contracts.portal.tabs.endpoints', icon: <GitCompare size={13} /> },
    { id: 'schemas', labelKey: 'contracts.portal.tabs.schemas', icon: <Code size={13} /> },
    { id: 'security', labelKey: 'contracts.portal.tabs.security', icon: <Shield size={13} /> },
    { id: 'versions', labelKey: 'contracts.portal.tabs.versions', icon: <History size={13} /> },
    { id: 'examples', labelKey: 'contracts.portal.tabs.examples', icon: <MessageSquare size={13} /> },
    { id: 'glossary', labelKey: 'contracts.portal.tabs.glossary', icon: <BookOpen size={13} /> },
  ];

  const serviceIcon = detail.protocol === 'AsyncApi' ? <Zap size={18} className="text-cyan" /> :
    detail.protocol === 'Wsdl' ? <Cog size={18} className="text-accent" /> :
    <Globe size={18} className="text-mint" />;

  return (
    <PageContainer>
      {/* Back link */}
      <Link
        to={`/contracts/${contractVersionId}`}
        className="inline-flex items-center gap-1.5 text-xs text-muted hover:text-accent transition-colors"
      >
        <ArrowLeft size={12} />
        {t('contracts.portal.backToWorkspace', 'Back to workspace')}
      </Link>

      {/* ── Hero header ── */}
      <div className="p-6 rounded-lg bg-card border border-edge">
        <div className="flex items-start justify-between gap-6">
          <div className="flex items-start gap-4">
            <div className="w-12 h-12 rounded-lg bg-elevated border border-edge flex items-center justify-center flex-shrink-0">
              {serviceIcon}
            </div>
            <div className="space-y-1.5">
              <h1 className="text-lg font-bold text-heading">{studio.friendlyName || detail.apiName || detail.apiAssetId}</h1>
              <p className="text-xs text-muted">{studio.functionalDescription || detail.routePattern || t('contracts.portal.noDescription', 'No description available.')}</p>
              <div className="flex items-center gap-2 flex-wrap">
                <ProtocolBadge protocol={detail.protocol} size="md" />
                <LifecycleBadge state={detail.lifecycleState} size="md" />
                <span className="text-[10px] font-mono text-muted px-2 py-0.5 bg-elevated border border-edge rounded">
                  v{detail.semVer}
                </span>
              </div>
            </div>
          </div>
          <div className="flex items-center gap-3 flex-shrink-0">
            <div className="rounded-lg border border-edge bg-elevated/40 px-3 py-2 text-right">
              <p className="text-[10px] text-muted uppercase tracking-wider">{t('contracts.portal.violations', 'Violations')}</p>
              <p className="text-sm font-semibold text-heading">{violations.length}</p>
            </div>
            <button
              onClick={async () => {
                try {
                  const result = await contractsApi.exportVersion(contractVersionId!);
                  const blob = new Blob([result.specContent], { type: 'text/plain' });
                  const url = URL.createObjectURL(blob);
                  const a = document.createElement('a');
                  a.href = url;
                  a.download = `${studio.technicalName || detail.apiAssetId}-${detail.semVer}.${result.format}`;
                  a.click();
                  URL.revokeObjectURL(url);
                } catch { /* toast */ }
              }}
              className="inline-flex items-center gap-1.5 px-3 py-2 text-xs font-medium rounded-md bg-accent/10 text-accent border border-accent/20 hover:bg-accent/20 transition-colors"
            >
              <ExternalLink size={12} />
              {t('contracts.portal.download', 'Download')}
            </button>
          </div>
        </div>
      </div>

      {/* ── Quick info cards ── */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <InfoCard icon={<Users size={16} />} label={t('contracts.portal.owner', 'Owner')} value={studio.owner || t('common.notAvailable', 'Not available')} />
        <InfoCard icon={<Target size={16} />} label={t('contracts.portal.domain', 'Domain')} value={studio.domain || t('common.notAvailable', 'Not available')} />
        <InfoCard icon={<Shield size={16} />} label={t('contracts.portal.criticality', 'Criticality')} value={studio.criticality || t('common.notAvailable', 'Not available')} />
        <InfoCard icon={<Clock size={16} />} label={t('contracts.portal.created', 'Created')} value={detail.createdAt ? new Date(detail.createdAt).toLocaleDateString() : t('common.notAvailable', 'Not available')} />
      </div>

      {/* ── Deprecation notice ── */}
      {detail.deprecationNotice && (
        <div className="p-4 rounded-lg bg-warning/10 border border-warning/25 flex items-start gap-3">
          <AlertTriangle size={16} className="text-warning flex-shrink-0 mt-0.5" />
          <div>
            <h3 className="text-xs font-semibold text-warning mb-0.5">
              {t('contracts.portal.deprecationNotice', 'Deprecation Notice')}
            </h3>
            <p className="text-xs text-body">{detail.deprecationNotice}</p>
            {detail.sunsetDate && (
              <p className="text-[10px] text-muted mt-1">
                {t('contracts.portal.sunsetDate', 'Sunset date')}: {detail.sunsetDate}
              </p>
            )}
          </div>
        </div>
      )}

      {/* ── Tab navigation ── */}
      <div className="flex items-center gap-1 border-b border-edge overflow-x-auto">
        {tabs.map((tab) => (
          <button
            key={tab.id}
            onClick={() => setActiveTab(tab.id)}
            className={cn(
              'inline-flex items-center gap-1.5 px-3 py-2.5 text-xs font-medium transition-colors border-b-2 flex-shrink-0',
              activeTab === tab.id
                ? 'text-accent border-accent'
                : 'text-muted border-transparent hover:text-heading',
            )}
          >
            {tab.icon}
            {t(tab.labelKey, tab.id)}
          </button>
        ))}
      </div>

      {/* ── Tab content ── */}
      <div className="min-h-[400px]">
        {activeTab === 'overview' && (
          <OverviewTab detail={detail} studio={studio} violations={violations} baseUrl={baseUrl} />
        )}
        {activeTab === 'endpoints' && (
          <EndpointsTab specContent={detail.specContent} protocol={detail.protocol} />
        )}
        {activeTab === 'schemas' && (
          <SchemasTab specContent={detail.specContent} />
        )}
        {activeTab === 'security' && (
          <SecurityTab detail={detail} studio={studio} violations={violations} />
        )}
        {activeTab === 'versions' && (
          <VersionsTab versions={versions} currentVersionId={contractVersionId!} />
        )}
        {activeTab === 'examples' && (
          <ExamplesTab />
        )}
        {activeTab === 'glossary' && (
          <GlossaryTab />
        )}
      </div>
    </PageContainer>
  );
}

// ── Info card ─────────────────────────────────────────────────────────────────

function InfoCard({ icon, label, value }: { icon: React.ReactNode; label: string; value: string }) {
  return (
    <Card>
      <CardBody className="py-3 px-4 flex items-center gap-3">
        <div className="text-accent">{icon}</div>
        <div>
          <p className="text-[10px] text-muted uppercase tracking-wider">{label}</p>
          <p className="text-sm font-medium text-heading truncate">{value}</p>
        </div>
      </CardBody>
    </Card>
  );
}

// ── Overview Tab ──────────────────────────────────────────────────────────────

function OverviewTab({
  detail, studio, violations, baseUrl,
}: {
  detail: ContractVersionDetail;
  studio: ReturnType<typeof toStudioContract>;
  violations: { ruleId?: string; message: string; severity: string }[];
  baseUrl: string;
}) {
  const { t } = useTranslation();

  return (
    <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
      {/* Left column — main info */}
      <div className="lg:col-span-2 space-y-6">
        {/* Onboarding */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <BookOpen size={14} className="text-accent" />
              <h2 className="text-xs font-semibold text-heading">
                {t('contracts.portal.onboarding', 'Getting Started')}
              </h2>
            </div>
          </CardHeader>
          <CardBody className="space-y-3">
            <div className="p-3 rounded bg-elevated border border-edge">
              <p className="text-[10px] text-muted uppercase tracking-wider mb-1.5">
                {t('contracts.portal.baseUrl', 'Base URL')}
              </p>
              {baseUrl ? (
                <div className="flex items-center gap-2">
                  <code className="flex-1 text-xs font-mono text-body bg-panel px-2 py-1 rounded border border-edge">
                    {baseUrl}
                  </code>
                  <button
                    onClick={() => navigator.clipboard?.writeText(baseUrl)}
                    className="p-1.5 rounded hover:bg-elevated transition-colors text-muted hover:text-accent"
                  >
                    <Copy size={12} />
                  </button>
                </div>
              ) : (
                <p className="text-xs text-muted">{t('common.notAvailable', 'Not available')}</p>
              )}
            </div>
            <p className="text-xs text-body leading-relaxed">
              {studio.functionalDescription || t('contracts.portal.noDescription', 'No description available.')}
            </p>
          </CardBody>
        </Card>

        {/* Specification preview */}
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Code size={14} className="text-accent" />
                <h2 className="text-xs font-semibold text-heading">
                  {t('contracts.portal.specification', 'Specification')}
                </h2>
              </div>
              <span className="text-[10px] font-mono text-muted px-2 py-0.5 bg-elevated border border-edge rounded uppercase">
                {detail.format}
              </span>
            </div>
          </CardHeader>
          <CardBody className="p-0">
            {detail.specContent ? (
              <pre className="p-4 text-xs font-mono text-body whitespace-pre-wrap break-words overflow-auto max-h-[300px] bg-panel/50">
                {detail.specContent}
              </pre>
            ) : (
              <EmptyState
                size="compact"
                title={t('contracts.portal.noSpec', 'No specification content')}
              />
            )}
          </CardBody>
        </Card>

        {/* Violations */}
        {violations.length > 0 && (
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <AlertTriangle size={14} className="text-warning" />
                <h2 className="text-xs font-semibold text-heading">
                  {t('contracts.portal.violations', 'Violations')} ({violations.length})
                </h2>
              </div>
            </CardHeader>
            <CardBody className="p-0">
              <div className="divide-y divide-edge">
                {violations.slice(0, 10).map((v, i) => (
                  <div key={i} className="flex items-start gap-2 px-4 py-2.5">
                    <span className={cn(
                      'w-1.5 h-1.5 rounded-full mt-1.5 flex-shrink-0',
                      v.severity === 'Error' ? 'bg-danger' : v.severity === 'Warning' ? 'bg-warning' : 'bg-cyan',
                    )} />
                    <div className="min-w-0">
                      <p className="text-xs text-body">{v.message}</p>
                      {v.ruleId && <p className="text-[10px] text-muted font-mono mt-0.5">{v.ruleId}</p>}
                    </div>
                  </div>
                ))}
              </div>
            </CardBody>
          </Card>
        )}
      </div>

      {/* Right column — sidebar info */}
      <div className="space-y-4">
        {/* Owners */}
        <Card>
          <CardHeader>
            <h3 className="text-xs font-semibold text-heading flex items-center gap-1.5">
              <Users size={12} className="text-muted" />
              {t('contracts.portal.owners', 'Owners & Support')}
            </h3>
          </CardHeader>
          <CardBody className="space-y-2">
            <PortalRow label={t('contracts.portal.ownerLabel', 'Owner')} value={studio.owner} />
            <PortalRow label={t('contracts.portal.team', 'Team')} value={studio.team} />
            <PortalRow label={t('contracts.portal.domain', 'Domain')} value={studio.domain} />
            <PortalRow label={t('contracts.portal.product', 'Product')} value={studio.product} />
            <PortalRow label="SLA" value={studio.sla} />
            <PortalRow label="SLO" value={studio.slo} />
          </CardBody>
        </Card>

        {/* Compliance summary */}
        <Card>
          <CardHeader>
            <h3 className="text-xs font-semibold text-heading flex items-center gap-1.5">
              <Shield size={12} className="text-muted" />
              {t('contracts.portal.complianceSummary', 'Compliance')}
            </h3>
          </CardHeader>
          <CardBody className="space-y-2">
            <PortalRow label={t('contracts.portal.violations', 'Violations')} value={String(violations.length)} />
            <p className="text-[10px] text-muted">
              {t('contracts.portal.validationOnlyHint', 'Validation results are real. A consolidated compliance score is not yet available for this contract.')}
            </p>
          </CardBody>
        </Card>

        {studio.consumers.length > 0 && (
          <Card>
            <CardHeader>
              <h3 className="text-xs font-semibold text-heading">
                {t('contracts.portal.relations', 'Consumers & Producers')}
              </h3>
            </CardHeader>
            <CardBody className="space-y-2">
              {studio.consumers.slice(0, 5).map((c) => (
                <div key={c.id} className="flex items-center gap-2">
                  <ChevronRight size={10} className="text-mint" />
                  <span className="text-xs text-body">{c.name}</span>
                  <span className="text-[9px] text-muted">{c.type}</span>
                </div>
              ))}
            </CardBody>
          </Card>
        )}
      </div>
    </div>
  );
}

// ── Endpoints Tab ─────────────────────────────────────────────────────────────

function EndpointsTab({ specContent, protocol }: { specContent: string; protocol: string }) {
  const { t } = useTranslation();
  const operations = useMemo(() => extractOperations(specContent, protocol), [specContent, protocol]);

  if (operations.length === 0) {
    return (
      <EmptyState
        title={t('contracts.portal.noEndpoints', 'No endpoints found')}
        description={t('contracts.portal.noEndpointsDesc', 'Endpoints will appear here once the specification is defined.')}
      />
    );
  }

  return (
    <Card>
      <CardBody className="p-0">
        <div className="divide-y divide-edge">
          {operations.map((op, i) => (
            <div key={i} className="flex items-center gap-3 px-4 py-3 hover:bg-elevated/20 transition-colors">
              {op.method && (
                <span className={cn(
                  'px-2 py-0.5 text-[10px] font-bold rounded min-w-[50px] text-center',
                  op.method === 'GET' ? 'bg-mint/15 text-mint border border-mint/25' :
                  op.method === 'POST' ? 'bg-cyan/15 text-cyan border border-cyan/25' :
                  op.method === 'PUT' ? 'bg-warning/15 text-warning border border-warning/25' :
                  op.method === 'PATCH' ? 'bg-accent/15 text-accent border border-accent/25' :
                  op.method === 'DELETE' ? 'bg-danger/15 text-danger border border-danger/25' :
                  'bg-muted/15 text-muted border border-muted/25',
                )}>
                  {op.method}
                </span>
              )}
              <span className="text-xs font-mono text-heading flex-1 truncate">{op.path || op.name}</span>
              {op.summary && <span className="text-[10px] text-muted truncate max-w-[300px]">{op.summary}</span>}
              {op.deprecated && (
                <span className="text-[9px] text-warning bg-warning/10 px-1.5 py-0.5 rounded">
                  {t('contracts.portal.deprecated', 'Deprecated')}
                </span>
              )}
            </div>
          ))}
        </div>
      </CardBody>
    </Card>
  );
}

// ── Schemas Tab ───────────────────────────────────────────────────────────────

function SchemasTab({ specContent }: { specContent: string }) {
  const { t } = useTranslation();
  const schemas = useMemo(() => extractSchemas(specContent), [specContent]);

  if (schemas.length === 0) {
    return (
      <EmptyState
        title={t('contracts.portal.noSchemas', 'No schemas found')}
        description={t('contracts.portal.noSchemasDesc', 'Schemas and models will appear once defined in the specification.')}
      />
    );
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
      {schemas.map((schema, i) => (
        <Card key={i}>
          <CardHeader>
            <h3 className="text-xs font-semibold text-heading font-mono">{schema.name}</h3>
          </CardHeader>
          <CardBody>
            <p className="text-[10px] text-muted">{schema.type} · {schema.propCount} {t('contracts.portal.properties', 'properties')}</p>
          </CardBody>
        </Card>
      ))}
    </div>
  );
}

// ── Security Tab ──────────────────────────────────────────────────────────────

function SecurityTab({
  detail,
  studio,
  violations,
}: {
  detail: ContractVersionDetail;
  studio: ReturnType<typeof toStudioContract>;
  violations: { message: string; severity: string }[];
}) {
  const { t } = useTranslation();
  const securityViolations = violations.filter((v) => v.message.toLowerCase().includes('security'));

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
      <Card>
        <CardHeader>
          <h3 className="text-xs font-semibold text-heading">{t('contracts.portal.securityClassification', 'Classification')}</h3>
        </CardHeader>
        <CardBody className="space-y-2">
          <PortalRow label={t('contracts.portal.visibility', 'Visibility')} value={studio.visibility} />
          <PortalRow label={t('contracts.portal.dataClassification', 'Data Classification')} value={studio.dataClassification} />
          <PortalRow label={t('contracts.portal.criticality', 'Criticality')} value={studio.criticality} />
          <PortalRow label={t('contracts.portal.owner', 'Owner')} value={studio.owner} />
        </CardBody>
      </Card>
      <Card>
        <CardHeader>
          <h3 className="text-xs font-semibold text-heading">{t('contracts.portal.securityRequirements', 'Requirements')}</h3>
        </CardHeader>
        <CardBody className="space-y-2">
          {securityViolations.length === 0 ? (
            <p className="text-xs text-muted">
              {t('contracts.portal.securityHint', 'No explicit security validation findings were returned for this contract version.')}
            </p>
          ) : (
            securityViolations.map((violation, index) => (
              <div key={`${detail.id}-security-${index}`} className="flex items-center gap-2">
                <AlertTriangle size={11} className="text-warning" />
                <span className="text-xs text-body">{violation.message}</span>
              </div>
            ))
          )}
        </CardBody>
      </Card>
    </div>
  );
}

// ── Versions Tab

function VersionsTab({ versions, currentVersionId }: { versions: ContractVersion[]; currentVersionId: string }) {
  const { t } = useTranslation();

  if (versions.length === 0) {
    return (
      <EmptyState
        title={t('contracts.portal.noVersions', 'No version history')}
        description={t('contracts.portal.noVersionsDesc', 'Version history will appear as the contract evolves.')}
      />
    );
  }

  return (
    <Card>
      <CardBody className="p-0">
        <div className="divide-y divide-edge">
          {versions.map((v) => (
            <Link
              key={v.id}
              to={`/contracts/${v.id}/portal`}
              className={cn(
                'flex items-center gap-3 px-4 py-3 hover:bg-elevated/20 transition-colors',
                v.id === currentVersionId && 'bg-accent/5 border-l-2 border-accent',
              )}
            >
              <span className="text-xs font-mono font-bold text-heading">v{v.version}</span>
              <LifecycleBadge state={v.lifecycleState} size="sm" />
              <span className="text-[10px] text-muted ml-auto">
                {v.createdAt ? new Date(v.createdAt).toLocaleDateString() : ''}
              </span>
              {v.id === currentVersionId && (
                <span className="text-[9px] text-accent bg-accent/10 px-1.5 py-0.5 rounded">
                  {t('contracts.portal.current', 'Current')}
                </span>
              )}
            </Link>
          ))}
        </div>
      </CardBody>
    </Card>
  );
}

// ── Examples Tab ──────────────────────────────────────────────────────────────

function ExamplesTab() {
  const { t } = useTranslation();

  return (
    <div className="space-y-4">
      <EmptyState
        title={t('contracts.portal.noExamples', 'No interaction examples yet')}
        description={t('contracts.portal.noExamplesDesc', 'Request/response examples will appear here when documented in the studio.')}
        icon={<MessageSquare size={20} />}
      />
    </div>
  );
}

// ── Glossary Tab ──────────────────────────────────────────────────────────────

function GlossaryTab() {
  const { t } = useTranslation();

  return (
    <EmptyState
      title={t('contracts.portal.noGlossary', 'No glossary terms')}
      description={t('contracts.portal.noGlossaryDesc', 'Domain terms will appear here when documented in the studio.')}
      icon={<BookOpen size={20} />}
    />
  );
}

// ── Shared helpers ────────────────────────────────────────────────────────────

function PortalRow({ label, value }: { label: string; value?: string | null }) {
  return (
    <div className="flex items-center justify-between gap-2">
      <span className="text-[10px] text-muted">{label}</span>
      <span className="text-xs text-body font-medium truncate max-w-[200px]">{value || '—'}</span>
    </div>
  );
}

interface ExtractedOp {
  method?: string;
  path?: string;
  name?: string;
  summary?: string;
  deprecated?: boolean;
}

function extractOperations(specContent: string, protocol: string): ExtractedOp[] {
  if (!specContent) return [];
  const ops: ExtractedOp[] = [];
  try {
    if (protocol === 'Wsdl' || specContent.trim().startsWith('<')) {
      const opMatches = specContent.matchAll(/<operation\s+name="([^"]*)"/g);
      for (const m of opMatches) {
        ops.push({ name: m[1], summary: 'SOAP Operation' });
      }
    } else {
      const lines = specContent.split('\n');
      const methods = ['get', 'post', 'put', 'patch', 'delete', 'head', 'options'];
      let currentPath = '';
      for (const line of lines) {
        const pathMatch = line.match(/^\s{2}(\/\S+)\s*:/);
        if (pathMatch) {
          const pathValue = pathMatch[1];
          if (pathValue) {
            currentPath = pathValue;
          }
        }

        const methodMatch = line.match(/^\s{4}(\w+)\s*:/);
        const method = methodMatch?.[1];
        if (method && methods.includes(method)) {
          ops.push({ method: method.toUpperCase(), path: currentPath });
        }

        const lastOperation = ops[ops.length - 1];
        if (line.includes('summary:') && lastOperation && !lastOperation.summary) {
          const val = line.split('summary:')[1]?.trim().replace(/^["']|["']$/g, '');
          if (val) {
            lastOperation.summary = val;
          }
        }
      }
    }
  } catch { /* silently handle parse errors */ }
  return ops;
}

interface ExtractedSchema {
  name: string;
  type: string;
  propCount: number;
}

function extractSchemas(specContent: string): ExtractedSchema[] {
  if (!specContent) return [];
  const schemas: ExtractedSchema[] = [];
  try {
    const lines = specContent.split('\n');
    let inSchemas = false;
    let currentSchema = '';
    let propCount = 0;
    for (const line of lines) {
      if (line.match(/^\s{2}schemas:\s*$/i) || line.match(/components:/i)) {
        inSchemas = true;
        continue;
      }
      if (inSchemas) {
        const schemaMatch = line.match(/^\s{4}(\w+)\s*:/);
        if (schemaMatch) {
          if (currentSchema) {
            schemas.push({ name: currentSchema, type: 'object', propCount });
          }

          const nextSchema = schemaMatch[1];
          if (!nextSchema) {
            continue;
          }

          currentSchema = nextSchema;
          propCount = 0;
          continue;
        }
        if (currentSchema && line.match(/^\s{8}\w+\s*:/)) {
          propCount++;
        }
      }
    }
    if (currentSchema) {
      schemas.push({ name: currentSchema, type: 'object', propCount });
    }
  } catch { /* silently handle parse errors */ }
  return schemas;
}
