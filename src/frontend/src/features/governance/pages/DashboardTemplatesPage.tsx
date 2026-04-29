import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation } from '@tanstack/react-query';
import { LayoutDashboard, BarChart3, DollarSign, ShieldCheck, Users, Layers } from 'lucide-react';
import { Card, CardBody, CardFooter } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageHeader } from '../../../components/PageHeader';
import { EmptyState } from '../../../components/EmptyState';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { dashboardTemplatesApi, type DashboardTemplateDto } from '../api/dashboardTemplates';

// ── Types ─────────────────────────────────────────────────────────────────────

type TemplateCategory = 'All' | 'Services' | 'Operations' | 'FinOps' | 'Compliance' | 'Teams';

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
  template: DashboardTemplateDto;
  onUse: (id: string, name: string) => void;
  isUsingId: string | null;
}

function TemplateCard({ template, onUse, isUsingId }: TemplateCardProps) {
  const { t } = useTranslation();
  const isFeatured = template.tags.includes('featured');
  const isNew = template.tags.includes('new');

  return (
    <Card variant="interactive" className="flex flex-col h-full">
      <CardBody className="flex-1">
        <div className="flex items-start justify-between gap-2 mb-2">
          <div className="flex items-center gap-1.5 flex-wrap">
            {isFeatured && (
              <Badge variant="warning" size="sm">
                {t('governance.templates.featured', 'Featured')}
              </Badge>
            )}
            {isNew && (
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
            <Layers size={11} />
            {template.category}
          </span>
          <span className="flex items-center gap-1">
            <LayoutDashboard size={11} />
            {template.installCount} {t('governance.templates.installs', 'installs')}
          </span>
        </div>
      </CardBody>

      <CardFooter>
        <Button
          size="sm"
          className="w-full"
          disabled={isUsingId === template.id}
          onClick={() => onUse(template.id, template.name)}
        >
          {isUsingId === template.id
            ? t('governance.templates.applying', 'Applying…')
            : t('governance.templates.useTemplate', 'Use Template')}
        </Button>
      </CardFooter>
    </Card>
  );
}

// ── Page ──────────────────────────────────────────────────────────────────────

/**
 * DashboardTemplatesPage — V3.8 Marketplace/Templates gallery.
 *
 * Filterable grid of pre-built dashboard templates from the backend.
 * Falls back gracefully when API returns no templates.
 */
export function DashboardTemplatesPage() {
  const { t } = useTranslation();
  const [activeCategory, setActiveCategory] = useState<TemplateCategory>('All');
  const [usedId, setUsedId] = useState<string | null>(null);
  const [isUsingId, setIsUsingId] = useState<string | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['dashboard-templates'],
    queryFn: () => dashboardTemplatesApi.list('default'),
    staleTime: 5 * 60_000,
  });

  const instantiate = useMutation({
    mutationFn: ({ id, name }: { id: string; name: string }) =>
      dashboardTemplatesApi.instantiate(id, {
        tenantId: 'default',
        userId: 'current',
        customName: name,
      }),
    onSuccess: (result, vars) => {
      setUsedId(vars.id);
      setIsUsingId(null);
    },
    onError: () => setIsUsingId(null),
  });

  const allTemplates = data?.items ?? [];

  const filtered =
    activeCategory === 'All'
      ? allTemplates
      : allTemplates.filter(
          (tpl) => tpl.category.toLowerCase() === activeCategory.toLowerCase(),
        );

  const handleUse = (id: string, name: string) => {
    setIsUsingId(id);
    instantiate.mutate({ id, name });
  };

  return (
    <PageContainer>
      <PageHeader
        title={t('governance.templates.title', 'Dashboard Templates')}
        subtitle={t(
          'governance.templates.subtitle',
          'Start from a pre-built template and customise it for your team.',
        )}
        badge={
          <Badge variant="info">
            {allTemplates.length} {t('governance.templates.count', 'templates')}
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
              'Template applied — opening Dashboard Builder…',
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
        {isLoading ? (
          <PageLoadingState />
        ) : filtered.length === 0 ? (
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
              <TemplateCard
                key={tpl.id}
                template={tpl}
                onUse={handleUse}
                isUsingId={isUsingId}
              />
            ))}
          </div>
        )}
      </PageSection>
    </PageContainer>
  );
}
