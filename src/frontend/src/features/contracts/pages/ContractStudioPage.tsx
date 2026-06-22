import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import {
  Code2,
  Zap,
  Globe,
  Hash,
  FileCode2,
  GitMerge,
  ArrowRight,
  CheckCircle2,
  GitBranch,
  ShieldCheck,
  Eye,
  Clock,
  LayoutGrid,
  Plus,
} from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../shared/ui';
import { useContractsSummary, useContractList } from '../hooks';
import type { ContractListItem } from '../../../types';
import { HUB_KEY_TO_CONTRACT_TYPE, BEST_FOR_KEY } from '../create/contractCreateConstants';

// ── Contract Type Registry ────────────────────────────────────────────────────

interface ContractType {
  key: string;
  label: string;
  icon: React.ReactNode;
  accentClass: string;
  borderClass: string;
  badge: string;
  features: string[];
}

const CONTRACT_TYPES: ContractType[] = [
  {
    key: 'rest-openapi',
    label: 'REST / OpenAPI',
    icon: <Globe size={18} />,
    accentClass: 'text-accent',
    borderClass: 'border-l-accent',
    badge: 'OpenAPI 3.1',
    features: ['Live schema diff', 'Mock generation', 'Client SDK export'],
  },
  {
    key: 'asyncapi',
    label: 'AsyncAPI 3.x',
    icon: <Zap size={18} />,
    accentClass: 'text-success',
    borderClass: 'border-l-success',
    badge: 'AsyncAPI 3.x',
    features: ['Channel bindings', 'Payload schemas', 'Broker metadata'],
  },
  {
    key: 'soap-wsdl',
    label: 'SOAP / WSDL',
    icon: <Code2 size={18} />,
    accentClass: 'text-warning',
    borderClass: 'border-l-warning',
    badge: 'WSDL 1.1 / 2.0',
    features: ['Operation builder', 'Message types', 'XSD validation'],
  },
  {
    key: 'graphql',
    label: 'GraphQL',
    icon: <Hash size={18} />,
    accentClass: 'text-info',
    borderClass: 'border-l-info',
    badge: 'SDL',
    features: ['Type explorer', 'Directive support', 'Breaking changes'],
  },
  {
    key: 'protobuf',
    label: 'Protobuf / gRPC',
    icon: <FileCode2 size={18} />,
    accentClass: 'text-accent',
    borderClass: 'border-l-accent',
    badge: '.proto',
    features: ['Field numbering', 'Reserved ranges', 'Compatibility check'],
  },
  {
    key: 'shared-schema',
    label: 'Shared Schema',
    icon: <GitMerge size={18} />,
    accentClass: 'text-muted',
    borderClass: 'border-l-edge',
    badge: 'Multi-format',
    features: ['JSON Schema', 'Avro / XSD', 'Cross-contract refs'],
  },
];

// ── Publication workflow steps ────────────────────────────────────────────────

const WORKFLOW_STEPS = [
  { icon: <Eye size={12} />, label: 'Design' },
  { icon: <GitBranch size={12} />, label: 'Version' },
  { icon: <ShieldCheck size={12} />, label: 'Validate' },
  { icon: <CheckCircle2 size={12} />, label: 'Publish' },
];

// ── Stat Card ─────────────────────────────────────────────────────────────────

interface StatCardProps {
  testId: string;
  label: string;
  value: number | undefined;
  loading?: boolean;
}

function StatCard({ testId, label, value, loading }: StatCardProps) {
  return (
    <div
      data-testid={testId}
      className="rounded-md border border-edge bg-card p-4 flex flex-col gap-1"
    >
      <span className="text-xs text-muted font-medium">{label}</span>
      <span className="text-2xl font-bold text-heading">
        {loading ? '—' : (value ?? 0)}
      </span>
    </div>
  );
}

// ── Draft Resume Card ─────────────────────────────────────────────────────────

function DraftCard({ item, onResume }: { item: ContractListItem; onResume: () => void }) {
  return (
    <Button
      variant="ghost"
      size="xs"
      onClick={onResume}
      className="group flex-shrink-0 w-56 flex-col items-start justify-start border border-edge bg-card hover:bg-elevated hover:border-edge-strong p-3 h-auto gap-0"
    >
      <div className="flex items-center justify-between mb-2 w-full">
        <Badge variant="warning" size="sm">{item.lifecycleState}</Badge>
        <Clock size={11} className="text-faded" />
      </div>
      <p className="text-xs font-semibold text-heading truncate mb-1 w-full">
        {item.apiName ?? item.name ?? 'Untitled'}
      </p>
      <p className="text-[11px] text-faded font-mono">{item.protocol}</p>
      <div className="flex items-center gap-1 text-[11px] font-medium text-accent mt-2 opacity-0 group-hover:opacity-100 transition-opacity">
        <span>Resume</span>
        <ArrowRight size={10} />
      </div>
    </Button>
  );
}

// ── Contract Type Card ────────────────────────────────────────────────────────

interface ContractTypeCardProps {
  type: ContractType;
  onSelect: () => void;
  onDesign: () => void;
  onImport: () => void;
}

function ContractTypeCard({ type, onSelect, onDesign, onImport }: ContractTypeCardProps) {
  const { t } = useTranslation();
  const contractType = HUB_KEY_TO_CONTRACT_TYPE[type.key];

  return (
    <div
      data-testid={`type-card-${type.key}`}
      role="button"
      tabIndex={0}
      onClick={onSelect}
      onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); onSelect(); } }}
      className={`
        group w-full text-left rounded-md border-l-2 border border-edge
        bg-card hover:bg-elevated hover:border-edge-strong
        transition-all duration-150 cursor-pointer
        focus:outline-none focus:ring-1 focus:ring-accent
        ${type.borderClass}
      `}
    >
      <div className="p-4">
        {/* Header row */}
        <div className="flex items-start justify-between gap-3 mb-3">
          <div className={`${type.accentClass} flex-shrink-0 mt-0.5`}>
            {type.icon}
          </div>
          <Badge variant="neutral" className="text-[10px] font-mono flex-shrink-0">
            {type.badge}
          </Badge>
        </div>

        {/* Label */}
        <h3 className="text-sm font-semibold text-heading mb-1">{type.label}</h3>

        {/* Best-for line */}
        <p className="text-xs text-muted leading-relaxed mb-3">
          {t(BEST_FOR_KEY(contractType))}
        </p>

        {/* Feature list */}
        <ul className="space-y-1 mb-4">
          {type.features.map(f => (
            <li key={f} className="flex items-center gap-1.5 text-xs text-faded">
              <span
                className="w-1 h-1 rounded-full flex-shrink-0"
                style={{ background: 'var(--t-divider)' }}
              />
              {f}
            </li>
          ))}
        </ul>

        {/* CTA row */}
        <div className="flex items-center gap-1 text-xs font-medium text-accent opacity-0 group-hover:opacity-100 transition-opacity">
          <span>Open builder</span>
          <ArrowRight size={11} />
        </div>
      </div>

      {/* Footer: Design / Import deep links */}
      <div
        className="px-4 pb-3 flex items-center gap-2"
        onClick={(e) => e.stopPropagation()}
      >
        <Button
          variant="ghost"
          size="xs"
          className="border border-edge hover:border-edge-strong"
          onClick={(e) => { e.stopPropagation(); onDesign(); }}
        >
          {t('contracts.create.modeVisual', 'Design')}
        </Button>
        <Button
          variant="ghost"
          size="xs"
          className="border border-edge hover:border-edge-strong"
          onClick={(e) => { e.stopPropagation(); onImport(); }}
        >
          {t('contracts.create.modeImport', 'Import')}
        </Button>
      </div>
    </div>
  );
}

// ── Main Page ─────────────────────────────────────────────────────────────────

export function ContractStudioPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const { data: summary, isLoading: summaryLoading } = useContractsSummary();
  const { data: draftsData, isLoading: draftsLoading } = useContractList({
    lifecycleState: 'Draft',
    pageSize: 10,
  });

  const draftItems = draftsData?.items ?? [];

  return (
    <PageContainer>
      <PageHeader
        title={t('contractStudio.title')}
        subtitle={t('contractStudio.subtitle')}
        icon={<LayoutGrid size={20} />}
        actions={
          <Button
            variant="primary"
            size="sm"
            icon={<Plus size={13} />}
            onClick={() => navigate('/contracts/new')}
          >
            {t('contractStudio.newContract')}
          </Button>
        }
      />

      {/* ── Stats ─────────────────────────────────────────────────────────── */}
      <PageSection>
        <div className="grid grid-cols-3 gap-3 mb-6">
          <StatCard
            testId="stat-total"
            label={t('contractStudio.stats.total')}
            value={summary?.totalCount}
            loading={summaryLoading}
          />
          <StatCard
            testId="stat-published"
            label={t('contractStudio.stats.published')}
            value={summary?.approvedCount}
            loading={summaryLoading}
          />
          <StatCard
            testId="stat-draft"
            label={t('contractStudio.stats.draft')}
            value={summary?.draftCount}
            loading={summaryLoading}
          />
        </div>

        {/* ── In-progress drafts ───────────────────────────────────────────── */}
        {(draftsLoading || draftItems.length > 0) && (
          <div className="mb-6">
            <div className="flex items-center justify-between mb-3">
              <h2 className="text-xs font-semibold text-muted uppercase tracking-wider">
                {t('contractStudio.inProgress')}
              </h2>
              {draftItems.length > 0 && (
                <span className="text-xs text-faded">{draftItems.length} drafts</span>
              )}
            </div>
            {draftsLoading ? (
              <div className="h-24 rounded-md border border-edge bg-card animate-pulse" />
            ) : (
              <div className="flex gap-3 overflow-x-auto pb-1">
                {draftItems.map(item => (
                  <DraftCard
                    key={item.contractVersionId ?? item.id ?? item.apiAssetId}
                    item={item}
                    onResume={() =>
                      navigate(`/contracts/workspace/${item.contractVersionId ?? item.id}`)
                    }
                  />
                ))}
              </div>
            )}
          </div>
        )}

        {/* ── Publication workflow banner ──────────────────────────────────── */}
        <div className="mb-6 rounded-md border border-edge bg-elevated px-4 py-3">
          <div className="flex items-center gap-3 flex-wrap">
            <span className="text-xs font-semibold text-heading">
              {t('contractStudio.unifiedPublication')}
            </span>
            <div className="flex items-center gap-1 flex-wrap">
              {WORKFLOW_STEPS.map((step, i) => (
                <span key={step.label} className="flex items-center gap-1">
                  <span className="flex items-center gap-1 text-xs text-muted px-2 py-0.5 rounded-sm bg-card border border-edge">
                    <span className="text-accent">{step.icon}</span>
                    {step.label}
                  </span>
                  {i < WORKFLOW_STEPS.length - 1 && (
                    <ArrowRight size={10} className="text-faded flex-shrink-0" />
                  )}
                </span>
              ))}
            </div>
          </div>
        </div>

        {/* ── Type picker ─────────────────────────────────────────────────── */}
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-xs font-semibold text-muted uppercase tracking-wider">
            {t('contractStudio.chooseType')}
          </h2>
          <span className="text-xs text-faded">
            {CONTRACT_TYPES.length} types available
          </span>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
          {CONTRACT_TYPES.map(type => {
            const contractType = HUB_KEY_TO_CONTRACT_TYPE[type.key];
            return (
              <ContractTypeCard
                key={type.key}
                type={type}
                onSelect={() => navigate(`/contracts/new?type=${contractType}`)}
                onDesign={() => navigate(`/contracts/new?type=${contractType}&mode=visual`)}
                onImport={() => navigate(`/contracts/new?type=${contractType}&mode=import`)}
              />
            );
          })}
        </div>
      </PageSection>
    </PageContainer>
  );
}
