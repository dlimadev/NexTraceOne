import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { EmptyState } from '../../../components/EmptyState';
import {
  Plus,
  Layers,
  Zap,
  Filter,
  BookOpen,
  Code2,
  Package,
  CheckCircle2,
  XCircle,
} from 'lucide-react';
import {
  templatesApi,
  type TemplateSummary,
  type TemplateLanguage,
  type TemplateServiceType,
} from '../api/templates';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button, SearchInput, Select, Checkbox } from '../../../shared/ui';

// ── Helper maps ───────────────────────────────────────────────────────────────

const LANGUAGE_LABELS: Record<TemplateLanguage, string> = {
  DotNet: '.NET',
  NodeJs: 'Node.js',
  Java: 'Java',
  Go: 'Go',
  Python: 'Python',
  Agnostic: 'Agnostic',
};

const SERVICE_TYPE_LABELS: Record<TemplateServiceType, string> = {
  RestApi: 'REST API',
  EventDriven: 'Event Driven',
  BackgroundWorker: 'Background Worker',
  Grpc: 'gRPC',
  Soap: 'SOAP',
  Generic: 'Generic',
};

// INTENTIONAL TAXONOMY: cores por linguagem (DotNet=purple, Java=orange) não têm
// tokens semânticos equivalentes no design system — mantidos como Tailwind raw.
// Go migrado para --color-cyan (token disponível em index.css).
const LANGUAGE_COLORS: Record<TemplateLanguage, string> = {
  DotNet: 'bg-purple-500/10 text-purple-400 border-purple-500/20',
  NodeJs: 'bg-success/10 text-success border-success/20',
  Java: 'bg-orange-500/10 text-orange-400 border-orange-500/20',
  Go: 'bg-cyan/10 text-cyan border-cyan/20',
  Python: 'bg-warning/10 text-warning border-warning/20',
  Agnostic: 'bg-elevated text-muted border-edge/50',
};

const SERVICE_TYPE_COLORS: Record<TemplateServiceType, string> = {
  RestApi: 'bg-accent/10 text-accent border-accent/20',
  EventDriven: 'bg-accent/10 text-accent border-accent/20',
  BackgroundWorker: 'bg-warning/10 text-warning border-warning/20',
  Grpc: 'bg-accent/10 text-accent border-accent/20',
  Soap: 'bg-info/10 text-info border-info/20',
  Generic: 'bg-elevated text-muted border-edge/50',
};

// ── Sub-components ────────────────────────────────────────────────────────────

function TemplateBadge({
  label,
  className,
}: {
  label: string;
  className: string;
}) {
  return (
    <span
      className={`inline-flex items-center rounded border px-1.5 py-0.5 text-xs font-medium ${className}`}
    >
      {label}
    </span>
  );
}

function TemplateCard({
  template,
  onView,
  onScaffold,
}: {
  template: TemplateSummary;
  onView: () => void;
  onScaffold: () => void;
}) {
  const { t } = useTranslation();

  return (
    <div
      className="group relative flex flex-col gap-3 rounded-lg border border-edge bg-elevated p-4 transition-colors hover:border-edge hover:bg-elevated/60 cursor-pointer"
      onClick={onView}
    >
      {/* Header */}
      <div className="flex items-start justify-between gap-2">
        <div className="flex min-w-0 flex-col gap-1">
          <h3 className="truncate text-sm font-semibold text-body">
            {template.displayName}
          </h3>
          <code className="text-xs text-muted">{template.slug}</code>
        </div>
        <div className="flex shrink-0 items-center gap-1.5">
          {template.isActive ? (
            <CheckCircle2 className="h-4 w-4 text-success" />
          ) : (
            <XCircle className="h-4 w-4 text-muted" />
          )}
        </div>
      </div>

      {/* Badges */}
      <div className="flex flex-wrap gap-1.5">
        <TemplateBadge
          label={LANGUAGE_LABELS[template.language]}
          className={LANGUAGE_COLORS[template.language]}
        />
        <TemplateBadge
          label={SERVICE_TYPE_LABELS[template.serviceType]}
          className={SERVICE_TYPE_COLORS[template.serviceType]}
        />
        <TemplateBadge
          label={`v${template.version}`}
          className="border-edge bg-elevated text-muted"
        />
      </div>

      {/* Description */}
      <p className="line-clamp-2 text-xs text-muted">{template.description}</p>

      {/* Footer metadata */}
      <div className="flex items-center gap-3 text-xs text-muted">
        <span className="flex items-center gap-1">
          <Package className="h-3 w-3" />
          {template.defaultDomain}
        </span>
        {template.hasBaseContract && (
          <span className="flex items-center gap-1">
            <BookOpen className="h-3 w-3" />
            {t('templates.library.hasContract')}
          </span>
        )}
        {template.hasScaffoldingManifest && (
          <span className="flex items-center gap-1">
            <Layers className="h-3 w-3" />
            {t('templates.library.hasManifest')}
          </span>
        )}
        <span className="ml-auto flex items-center gap-1">
          <Zap className="h-3 w-3" />
          {template.usageCount}x
        </span>
      </div>

      {/* Tags */}
      {template.tags.length > 0 && (
        <div className="flex flex-wrap gap-1">
          {template.tags.slice(0, 4).map(tag => (
            <span
              key={tag}
              className="rounded bg-elevated px-1.5 py-0.5 text-xs text-muted"
            >
              {tag}
            </span>
          ))}
          {template.tags.length > 4 && (
            <span className="text-xs text-muted">+{template.tags.length - 4}</span>
          )}
        </div>
      )}

      {/* Actions */}
      <div className="flex gap-2 border-t border-edge pt-3">
        <Button
          variant="ghost"
          size="xs"
          className="flex-1"
          icon={<Code2 className="h-3.5 w-3.5" />}
          onClick={e => {
            e.stopPropagation();
            onView();
          }}
        >
          {t('templates.library.viewDetails')}
        </Button>
        {template.isActive && (
          <Button
            variant="primary"
            size="xs"
            className="flex-1"
            icon={<Zap className="h-3.5 w-3.5" />}
            onClick={e => {
              e.stopPropagation();
              onScaffold();
            }}
          >
            {t('templates.library.scaffold')}
          </Button>
        )}
      </div>
    </div>
  );
}

// ── Main page ─────────────────────────────────────────────────────────────────

export function TemplateLibraryPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const [search, setSearch] = useState('');
  const [serviceType, setServiceType] = useState<TemplateServiceType | ''>('');
  const [language, setLanguage] = useState<TemplateLanguage | ''>('');
  const [showInactive, setShowInactive] = useState(false);

  const { data: templates = [], isLoading, isError } = useQuery({
    queryKey: ['service-templates', { search, serviceType, language, showInactive }],
    queryFn: () =>
      templatesApi.list({
        search: search || undefined,
        serviceType: (serviceType as TemplateServiceType) || undefined,
        language: (language as TemplateLanguage) || undefined,
        isActive: showInactive ? undefined : true,
      }),
  });

  const activeCount = templates.filter(t => t.isActive).length;

  return (
    <PageContainer>
      <PageHeader
        title={t('templates.library.title')}
        subtitle={t('templates.library.subtitle')}
      />
      {/* Create action */}
      <div className="flex justify-end">
        <Button
          variant="primary"
          icon={<Plus className="h-4 w-4" />}
          onClick={() => navigate('/catalog/templates/new')}
        >
          {t('templates.library.createTemplate')}
        </Button>
      </div>

      {/* Stats bar */}
      <div className="grid grid-cols-3 gap-4 sm:grid-cols-4">
        {[
          { label: t('templates.library.stats.total'), value: templates.length },
          { label: t('templates.library.stats.active'), value: activeCount },
          {
            label: t('templates.library.stats.dotnet'),
            value: templates.filter(t => t.language === 'DotNet').length,
          },
          {
            label: t('templates.library.stats.withContract'),
            value: templates.filter(t => t.hasBaseContract).length,
          },
        ].map(stat => (
          <div
            key={stat.label}
            className="flex flex-col gap-1 rounded-lg border border-edge bg-elevated p-3"
          >
            <span className="text-2xl font-bold text-body">{stat.value}</span>
            <span className="text-xs text-muted">{stat.label}</span>
          </div>
        ))}
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3">
        <SearchInput
          className="flex-1 min-w-[200px]"
          size="sm"
          placeholder={t('templates.library.searchPlaceholder')}
          value={search}
          onChange={e => setSearch(e.target.value)}
        />

        <div className="flex items-center gap-2">
          <Filter className="h-4 w-4 text-muted" />
          <Select
            value={serviceType}
            onChange={e => setServiceType(e.target.value as TemplateServiceType | '')}
            size="sm"
            options={[
              { value: '', label: t('templates.library.filters.allTypes') },
              ...Object.entries(SERVICE_TYPE_LABELS).map(([k, v]) => ({ value: k, label: v })),
            ]}
          />

          <Select
            value={language}
            onChange={e => setLanguage(e.target.value as TemplateLanguage | '')}
            size="sm"
            options={[
              { value: '', label: t('templates.library.filters.allLanguages') },
              ...Object.entries(LANGUAGE_LABELS).map(([k, v]) => ({ value: k, label: v })),
            ]}
          />

          <Checkbox
            checked={showInactive}
            onChange={e => setShowInactive(e.target.checked)}
            label={t('templates.library.filters.showInactive')}
          />
        </div>
      </div>

      {/* Grid */}
      {isError ? (
        <PageErrorState />
      ) : isLoading ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 6 }).map((_, i) => (
            <div
              key={i}
              className="h-56 animate-pulse rounded-lg border border-edge bg-elevated"
            />
          ))}
        </div>
      ) : templates.length === 0 ? (
        <EmptyState
          icon={<Layers className="h-5 w-5" />}
          title={t('templates.library.empty', 'No templates found')}
          description={t('templates.library.emptyHint', 'Create your first template to get started.')}
          action={
            <Button
              variant="primary"
              icon={<Plus className="h-4 w-4" />}
              onClick={() => navigate('/catalog/templates/new')}
            >
              {t('templates.library.createFirst')}
            </Button>
          }
        />
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {templates.map(template => (
            <TemplateCard
              key={template.templateId}
              template={template}
              onView={() => navigate(`/catalog/templates/${template.templateId}`)}
              onScaffold={() =>
                navigate(`/catalog/templates/${template.templateId}/scaffold`)
              }
            />
          ))}
        </div>
      )}
    </PageContainer>
  );
}
