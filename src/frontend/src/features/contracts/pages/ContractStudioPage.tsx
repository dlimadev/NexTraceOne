import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { Code2, Zap, Globe, Hash, FileCode2, GitMerge, CheckCircle2, ArrowRight } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';

// ── Contract Type Registry ────────────────────────────────────────────────────

interface ContractType {
  key: string;
  label: string;
  description: string;
  icon: React.ReactNode;
  color: string;
  badge?: string;
  route: string;
}

const CONTRACT_TYPES: ContractType[] = [
  {
    key: 'rest-openapi',
    label: 'REST / OpenAPI',
    description: 'Design REST APIs with OpenAPI 3.1 YAML/JSON. Visual editor with live diff.',
    icon: <Globe size={20} />,
    color: 'text-accent',
    badge: 'OpenAPI 3.1',
    route: '/contracts/studio/rest',
  },
  {
    key: 'asyncapi',
    label: 'AsyncAPI 3.x',
    description: 'Design event-driven APIs — Kafka, AMQP, SNS, WebSocket with channel schemas.',
    icon: <Zap size={20} />,
    color: 'text-success',
    badge: 'AsyncAPI 3.x',
    route: '/contracts/studio/async',
  },
  {
    key: 'soap-wsdl',
    label: 'SOAP / WSDL',
    description: 'Design SOAP services with visual operation, message and binding editor.',
    icon: <Code2 size={20} />,
    color: 'text-warning',
    badge: 'WSDL 1.1/2.0',
    route: '/contracts/studio/soap',
  },
  {
    key: 'graphql',
    label: 'GraphQL',
    description: 'Design GraphQL schemas with SDL editor and breaking change detection.',
    icon: <Hash size={20} />,
    color: 'text-info',
    badge: 'SDL',
    route: '/contracts/studio/graphql',
  },
  {
    key: 'protobuf',
    label: 'Protobuf / gRPC',
    description: 'Design .proto files and gRPC services with compatibility checking.',
    icon: <FileCode2 size={20} />,
    color: 'text-primary',
    badge: '.proto',
    route: '/contracts/studio/protobuf',
  },
  {
    key: 'shared-schema',
    label: 'Shared Schema',
    description: 'Reusable data schemas (JSON Schema, Avro, XSD) shared across contracts.',
    icon: <GitMerge size={20} />,
    color: 'text-secondary',
    route: '/contracts/studio/shared-schema',
  },
];

// ── Contract Type Card ─────────────────────────────────────────────────────────

function ContractTypeCard({ type, onSelect }: { type: ContractType; onSelect: () => void }) {
  const { t } = useTranslation();

  return (
    <Card
      className="cursor-pointer hover:border-accent/60 hover:shadow-md transition-all group"
      onClick={onSelect}
    >
      <CardBody className="p-5">
        <div className="flex items-start justify-between gap-3 mb-3">
          <div className={`${type.color} mt-0.5`}>{type.icon}</div>
          {type.badge && (
            <Badge variant="secondary" className="text-xs font-mono">
              {type.badge}
            </Badge>
          )}
        </div>
        <h3 className="text-sm font-semibold mb-1">{type.label}</h3>
        <p className="text-xs text-muted-foreground leading-relaxed">{type.description}</p>
        <div className="mt-4 flex items-center gap-1 text-xs text-accent opacity-0 group-hover:opacity-100 transition-opacity">
          <span>{t('contractStudio.openBuilder')}</span>
          <ArrowRight size={12} />
        </div>
      </CardBody>
    </Card>
  );
}

// ── Main Page ──────────────────────────────────────────────────────────────────

export function ContractStudioPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  return (
    <PageContainer>
      <PageHeader
        title={t('contractStudio.title')}
        subtitle={t('contractStudio.subtitle')}
      />

      {/* Publication Workflow Banner */}
      <div className="mx-4 mb-4 p-3 rounded-lg border border-accent/40 bg-accent/5">
        <div className="flex items-center gap-2 text-xs">
          <CheckCircle2 size={12} className="text-accent" />
          <span className="font-medium">{t('contractStudio.unifiedPublication')}</span>
          <span className="text-muted-foreground">·</span>
          <span className="text-muted-foreground">{t('contractStudio.publicationFlow')}</span>
        </div>
      </div>

      <PageSection>
        <div className="mb-6">
          <h2 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider mb-4">
            {t('contractStudio.chooseType')}
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {CONTRACT_TYPES.map((type) => (
              <ContractTypeCard
                key={type.key}
                type={type}
                onSelect={() => navigate(type.route)}
              />
            ))}
          </div>
        </div>
      </PageSection>
    </PageContainer>
  );
}
