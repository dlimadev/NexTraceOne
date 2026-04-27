import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { LayoutDashboard, BarChart3, DollarSign, ShieldCheck, Users, Layers, FlaskConical } from 'lucide-react';
import { Card, CardBody, CardFooter } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageHeader } from '../../../components/PageHeader';
import { EmptyState } from '../../../components/EmptyState';
import { PageContainer, PageSection } from '../../../components/shell';

// ── Types ─────────────────────────────────────────────────────────────────────

type TemplateCategory = 'All' | 'Services' | 'Operations' | 'FinOps' | 'Compliance' | 'Teams';

interface DashboardTemplate {
  id: string;
  name: string;
  description: string;
  category: Exclude<TemplateCategory, 'All'>;
  persona: string;
  widgetCount: number;
  layout: string;
  isNew?: boolean;
  isFeatured?: boolean;
}

// ── Simulated data ────────────────────────────────────────────────────────────

const TEMPLATES: DashboardTemplate[] = [
  // Services
  {
    id: 'tpl-svc-health',
    name: 'Service Health Overview',
    description: 'Real-time health status, SLO burn rates, error budgets, and alert counts for all services.',
    category: 'Services',
    persona: 'Engineer',
    widgetCount: 8,
    layout: 'two-column',
    isFeatured: true,
  },
  {
    id: 'tpl-svc-dep-map',
    name: 'Dependency Map',
    description: 'Visualise service dependency graphs, blast radius indicators, and upstream/downstream contract coverage.',
    category: 'Services',
    persona: 'Architect',
    widgetCount: 5,
    layout: 'single-column',
  },
  {
    id: 'tpl-svc-dora',
    name: 'DORA Metrics by Service',
    description: 'Deployment frequency, change failure rate, MTTR, and lead time broken down per service.',
    category: 'Services',
    persona: 'TechLead',
    widgetCount: 6,
    layout: 'two-column',
    isNew: true,
  },
  // Operations
  {
    id: 'tpl-ops-incidents',
    name: 'Incident Command Center',
    description: 'Active incidents, MTTR trend, on-call assignments, and SLO impact summary for Ops teams.',
    category: 'Operations',
    persona: 'Engineer',
    widgetCount: 9,
    layout: 'three-column',
    isFeatured: true,
  },
  {
    id: 'tpl-ops-oncall',
    name: 'On-Call Summary',
    description: 'Escalation chains, current on-call roster, recent pages, and runbook quick-links.',
    category: 'Operations',
    persona: 'Engineer',
    widgetCount: 5,
    layout: 'two-column',
  },
  {
    id: 'tpl-ops-change',
    name: 'Change Management Board',
    description: 'Pending changes, approval queue, Change Confidence scores, and deployment timeline.',
    category: 'Operations',
    persona: 'TechLead',
    widgetCount: 7,
    layout: 'two-column',
    isNew: true,
  },
  // FinOps
  {
    id: 'tpl-finops-budget',
    name: 'Budget Burn Tracker',
    description: 'Cloud spend vs. budget, forecast burn, anomaly flags, and cost-per-service breakdown.',
    category: 'FinOps',
    persona: 'Executive',
    widgetCount: 8,
    layout: 'two-column',
    isFeatured: true,
  },
  {
    id: 'tpl-finops-waste',
    name: 'Waste & Optimisation',
    description: 'Idle resource detection, rightsizing recommendations, and savings opportunity timeline.',
    category: 'FinOps',
    persona: 'Executive',
    widgetCount: 6,
    layout: 'two-column',
  },
  {
    id: 'tpl-finops-team',
    name: 'Team Cost Allocation',
    description: 'Cost attribution by team and service, with chargeback summaries and MoM trend.',
    category: 'FinOps',
    persona: 'TechLead',
    widgetCount: 5,
    layout: 'two-column',
  },
  // Compliance
  {
    id: 'tpl-comp-posture',
    name: 'Compliance Posture',
    description: 'Policy pass rates, open waivers, audit evidence gaps, and maturity scores by domain.',
    category: 'Compliance',
    persona: 'Auditor',
    widgetCount: 10,
    layout: 'three-column',
    isFeatured: true,
  },
  {
    id: 'tpl-comp-gates',
    name: 'Governance Gates',
    description: 'Gates status per service, blocked deployments, approval queue, and policy violations.',
    category: 'Compliance',
    persona: 'Auditor',
    widgetCount: 7,
    layout: 'two-column',
  },
  // Teams
  {
    id: 'tpl-team-health',
    name: 'Team Health Dashboard',
    description: 'Velocity, cycle time, blockers, SLO ownership gaps, and on-call burden for a team.',
    category: 'Teams',
    persona: 'TechLead',
    widgetCount: 8,
    layout: 'two-column',
    isFeatured: true,
  },
  {
    id: 'tpl-team-ownership',
    name: 'Ownership Coverage',
    description: 'Services missing owners, stale contacts, and team-to-service assignment overview.',
    category: 'Teams',
    persona: 'TechLead',
    widgetCount: 4,
    layout: 'two-column',
    isNew: true,
  },
];

// ── Constants ─────────────────────────────────────────────────────────────────

const CATEGORIES: TemplateCategory[] = [
  'All',
  'Services',
  'Operations',
  'FinOps',
  'Compliance',
  'Teams',
];

const CATEGORY_ICONS: Record<TemplateCategory, React.ReactNode> = {
  All: <Layers size={14} />,
  Services: <BarChart3 size={14} />,
  Operations: <LayoutDashboard size={14} />,
  FinOps: <DollarSign size={14} />,
  Compliance: <ShieldCheck size={14} />,
  Teams: <Users size={14} />,
};

const PERSONA_VARIANT: Record<string, 'info' | 'success' | 'warning' | 'default'> = {
  Engineer: 'info',
  TechLead: 'success',
  Executive: 'warning',
  Architect: 'info',
  Auditor: 'default',
};

function personaVariant(persona: string): 'info' | 'success' | 'warning' | 'default' {
  return PERSONA_VARIANT[persona] ?? 'default';
}

// ── TemplateCard ──────────────────────────────────────────────────────────────

interface TemplateCardProps {
  template: DashboardTemplate;
  onUse: (id: string) => void;
}

function TemplateCard({ template, onUse }: TemplateCardProps) {
  const { t } = useTranslation();

  return (
    <Card variant="interactive" className="flex flex-col h-full">
      <CardBody className="flex-1">
        <div className="flex items-start justify-between gap-2 mb-2">
          <div className="flex items-center gap-1.5 flex-wrap">
            {template.isFeatured && (
              <Badge variant="warning" size="sm">
                {t('governance.templates.featured', 'Featured')}
              </Badge>
            )}
            {template.isNew && (
              <Badge variant="success" size="sm">
                {t('governance.templates.new', 'New')}
              </Badge>
            )}
          </div>
          <Badge variant={personaVariant(template.persona)} size="sm">
            {template.persona}
          </Badge>
        </div>

        <h3 className="text-sm font-semibold text-heading mb-1.5">{template.name}</h3>
        <p className="text-xs text-muted leading-relaxed mb-3">{template.description}</p>

        <div className="flex items-center gap-3 text-xs text-muted">
          <span className="flex items-center gap-1">
            <LayoutDashboard size={11} />
            {template.widgetCount}{' '}
            {t('governance.templates.widgets', 'widgets')}
          </span>
          <span className="flex items-center gap-1">
            <Layers size={11} />
            {template.layout}
          </span>
        </div>
      </CardBody>

      <CardFooter>
        <Button
          size="sm"
          className="w-full"
          onClick={() => onUse(template.id)}
        >
          {t('governance.templates.useTemplate', 'Use Template')}
        </Button>
      </CardFooter>
    </Card>
  );
}

// ── Page ──────────────────────────────────────────────────────────────────────

/**
 * DashboardTemplatesPage — V3.8 Marketplace/Templates gallery.
 *
 * Provides a filterable grid of pre-built dashboard templates organised by
 * category (Services, Operations, FinOps, Compliance, Teams). Users can
 * select a template to seed a new dashboard in the builder.
 */
export function DashboardTemplatesPage() {
  const { t } = useTranslation();
  const [activeCategory, setActiveCategory] = useState<TemplateCategory>('All');
  const [usedId, setUsedId] = useState<string | null>(null);

  const filtered =
    activeCategory === 'All'
      ? TEMPLATES
      : TEMPLATES.filter((tpl) => tpl.category === activeCategory);

  const handleUse = (id: string) => {
    setUsedId(id);
    // In production: navigate to /governance/dashboards/new?templateId=id
  };

  return (
    <PageContainer>
      {/* IsSimulated banner */}
      <div className="mb-4 rounded-lg border border-warning/30 bg-warning/8 px-4 py-2 text-xs text-warning font-medium flex items-center gap-2">
        <FlaskConical size={14} />
        {t(
          'governance.simulated',
          'Simulated data — templates are seeded locally; live marketplace in production',
        )}
      </div>

      <PageHeader
        title={t('governance.templates.title', 'Dashboard Templates')}
        subtitle={t(
          'governance.templates.subtitle',
          'Start from a pre-built template and customise it for your team.',
        )}
        badge={
          <Badge variant="info">
            {TEMPLATES.length} {t('governance.templates.count', 'templates')}
          </Badge>
        }
      />

      {/* Category filter tabs */}
      <div className="flex items-center gap-1 flex-wrap mb-6">
        {CATEGORIES.map((cat) => (
          <button
            key={cat}
            onClick={() => setActiveCategory(cat)}
            className={`
              inline-flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-xs font-semibold
              transition-all
              ${
                activeCategory === cat
                  ? 'bg-accent text-on-accent shadow-sm'
                  : 'bg-elevated text-muted border border-edge hover:text-body hover:border-edge-strong'
              }
            `}
          >
            {CATEGORY_ICONS[cat]}
            {cat}
          </button>
        ))}
      </div>

      {/* Use confirmation toast */}
      {usedId && (
        <div className="mb-4 rounded-lg border border-success/30 bg-success/8 px-4 py-2 text-xs text-success font-medium flex items-center justify-between">
          <span>
            {t(
              'governance.templates.usedConfirm',
              'Template applied — opening Dashboard Builder...',
            )}
          </span>
          <button
            onClick={() => setUsedId(null)}
            className="text-success hover:text-success/70 transition-colors"
            aria-label="Dismiss"
          >
            ×
          </button>
        </div>
      )}

      <PageSection>
        {filtered.length === 0 ? (
          <EmptyState
            icon={<LayoutDashboard size={24} />}
            title={t('governance.templates.empty', 'No templates in this category')}
            description={t(
              'governance.templates.emptyDesc',
              'Try selecting a different category or browse All.',
            )}
            action={
              <Button
                variant="secondary"
                size="sm"
                onClick={() => setActiveCategory('All')}
              >
                {t('governance.templates.showAll', 'Show All Templates')}
              </Button>
            }
          />
        ) : (
          <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
            {filtered.map((tpl) => (
              <TemplateCard key={tpl.id} template={tpl} onUse={handleUse} />
            ))}
          </div>
        )}
      </PageSection>
    </PageContainer>
  );
}
