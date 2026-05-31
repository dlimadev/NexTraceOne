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
} from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Badge } from '../../../components/Badge';

// ── Contract Type Registry ────────────────────────────────────────────────────

interface ContractType {
  key: string;
  label: string;
  description: string;
  icon: React.ReactNode;
  accentClass: string;
  borderClass: string;
  badge: string;
  features: string[];
  route: string;
}

const CONTRACT_TYPES: ContractType[] = [
  {
    key: 'rest-openapi',
    label: 'REST / OpenAPI',
    description: 'Design REST APIs with OpenAPI 3.1 YAML/JSON. Visual editor with live diff and breaking-change detection.',
    icon: <Globe size={18} />,
    accentClass: 'text-accent',
    borderClass: 'border-l-accent',
    badge: 'OpenAPI 3.1',
    features: ['Live schema diff', 'Mock generation', 'Client SDK export'],
    route: '/contracts/studio/rest',
  },
  {
    key: 'asyncapi',
    label: 'AsyncAPI 3.x',
    description: 'Design event-driven APIs — Kafka, AMQP, SNS, WebSocket with channel schemas and payload definitions.',
    icon: <Zap size={18} />,
    accentClass: 'text-success',
    borderClass: 'border-l-success',
    badge: 'AsyncAPI 3.x',
    features: ['Channel bindings', 'Payload schemas', 'Broker metadata'],
    route: '/contracts/studio/async',
  },
  {
    key: 'soap-wsdl',
    label: 'SOAP / WSDL',
    description: 'Design SOAP services with visual operation, message and binding editor. WSDL 1.1 and 2.0 supported.',
    icon: <Code2 size={18} />,
    accentClass: 'text-warning',
    borderClass: 'border-l-warning',
    badge: 'WSDL 1.1 / 2.0',
    features: ['Operation builder', 'Message types', 'XSD validation'],
    route: '/contracts/studio/soap',
  },
  {
    key: 'graphql',
    label: 'GraphQL',
    description: 'Design GraphQL schemas with SDL editor, type explorer and breaking-change detection across versions.',
    icon: <Hash size={18} />,
    accentClass: 'text-info',
    borderClass: 'border-l-info',
    badge: 'SDL',
    features: ['Type explorer', 'Directive support', 'Breaking changes'],
    route: '/contracts/studio/graphql',
  },
  {
    key: 'protobuf',
    label: 'Protobuf / gRPC',
    description: 'Design .proto files and gRPC services with field numbering, reserved ranges and compatibility checks.',
    icon: <FileCode2 size={18} />,
    accentClass: 'text-accent',
    borderClass: 'border-l-accent',
    badge: '.proto',
    features: ['Field numbering', 'Reserved ranges', 'Compatibility check'],
    route: '/contracts/studio/protobuf',
  },
  {
    key: 'shared-schema',
    label: 'Shared Schema',
    description: 'Reusable data schemas (JSON Schema, Avro, XSD) referenced across contracts to ensure consistency.',
    icon: <GitMerge size={18} />,
    accentClass: 'text-muted',
    borderClass: 'border-l-edge',
    badge: 'Multi-format',
    features: ['JSON Schema', 'Avro / XSD', 'Cross-contract refs'],
    route: '/contracts/studio/shared-schema',
  },
];

// ── Publication workflow steps ────────────────────────────────────────────────

const WORKFLOW_STEPS = [
  { icon: <Eye size={12} />, label: 'Design' },
  { icon: <GitBranch size={12} />, label: 'Version' },
  { icon: <ShieldCheck size={12} />, label: 'Validate' },
  { icon: <CheckCircle2 size={12} />, label: 'Publish' },
];

// ── Contract Type Card ────────────────────────────────────────────────────────

function ContractTypeCard({ type, onSelect }: { type: ContractType; onSelect: () => void }) {
  return (
    <button
      type="button"
      onClick={onSelect}
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

        {/* Label + description */}
        <h3 className="text-sm font-semibold text-heading mb-1">{type.label}</h3>
        <p className="text-xs text-muted leading-relaxed mb-3">{type.description}</p>

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
    </button>
  );
}

// ── Main Page ─────────────────────────────────────────────────────────────────

export function ContractStudioPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  return (
    <PageContainer>
      <PageHeader
        title={t('contractStudio.title')}
        subtitle={t('contractStudio.subtitle')}
      />

      <PageSection>
        {/* Publication workflow banner */}
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

        {/* Section header */}
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-xs font-semibold text-muted uppercase tracking-wider">
            {t('contractStudio.chooseType')}
          </h2>
          <span className="text-xs text-faded">
            {CONTRACT_TYPES.length} types available
          </span>
        </div>

        {/* Contract type grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
          {CONTRACT_TYPES.map(type => (
            <ContractTypeCard
              key={type.key}
              type={type}
              onSelect={() => navigate(type.route)}
            />
          ))}
        </div>
      </PageSection>
    </PageContainer>
  );
}
