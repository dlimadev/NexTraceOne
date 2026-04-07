import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { EmptyState } from '../../../components/EmptyState';
import {
  Plus,
  Search,
  Layers,
  Zap,
  Filter,
  BookOpen,
  Code2,
  Package,
  CheckCircle,
  XCircle,
} from 'lucide-react';
import {
  templatesApi,
  type TemplateSummary,
  type TemplateLanguage,
  type TemplateServiceType,
} from '../api/templates';
import { PageErrorState } from '../../../components/PageErrorState';

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

const LANGUAGE_COLORS: Record<TemplateLanguage, string> = {
  DotNet: 'bg-purple-500/10 text-purple-400 border-purple-500/20',
  NodeJs: 'bg-green-500/10 text-green-400 border-green-500/20',
  Java: 'bg-orange-500/10 text-orange-400 border-orange-500/20',
  Go: 'bg-cyan-500/10 text-cyan-400 border-cyan-500/20',
  Python: 'bg-yellow-500/10 text-yellow-400 border-yellow-500/20',
  Agnostic: 'bg-neutral-500/10 text-neutral-400 border-neutral-500/20',
};

const SERVICE_TYPE_COLORS: Record<TemplateServiceType, string> = {
  RestApi: 'bg-blue-500/10 text-blue-400 border-blue-500/20',
  EventDriven: 'bg-pink-500/10 text-pink-400 border-pink-500/20',
  BackgroundWorker: 'bg-amber-500/10 text-amber-400 border-amber-500/20',
  Grpc: 'bg-indigo-500/10 text-indigo-400 border-indigo-500/20',
  Soap: 'bg-teal-500/10 text-teal-400 border-teal-500/20',
  Generic: 'bg-neutral-500/10 text-neutral-400 border-neutral-500/20',
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
      className="group relative flex flex-col gap-3 rounded-lg border border-neutral-800 bg-neutral-900 p-4 transition-colors hover:border-neutral-600 hover:bg-neutral-800/60 cursor-pointer"
      onClick={onView}
    >
      {/* Header */}
      <div className="flex items-start justify-between gap-2">
        <div className="flex min-w-0 flex-col gap-1">
          <h3 className="truncate text-sm font-semibold text-neutral-100">
            {template.displayName}
          </h3>
          <code className="text-xs text-neutral-500">{template.slug}</code>
        </div>
        <div className="flex shrink-0 items-center gap-1.5">
          {template.isActive ? (
            <CheckCircle className="h-4 w-4 text-emerald-400" />
          ) : (
            <XCircle className="h-4 w-4 text-neutral-500" />
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
          className="border-neutral-700 bg-neutral-800 text-neutral-400"
        />
      </div>

      {/* Description */}
      <p className="line-clamp-2 text-xs text-neutral-400">{template.description}</p>

      {/* Footer metadata */}
      <div className="flex items-center gap-3 text-xs text-neutral-500">
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
              className="rounded bg-neutral-800 px-1.5 py-0.5 text-xs text-neutral-500"
            >
              {tag}
            </span>
          ))}
          {template.tags.length > 4 && (
            <span className="text-xs text-neutral-600">+{template.tags.length - 4}</span>
          )}
        </div>
      )}

      {/* Actions */}
      <div className="flex gap-2 border-t border-neutral-800 pt-3">
        <button
          className="flex flex-1 items-center justify-center gap-1.5 rounded bg-neutral-800 px-3 py-1.5 text-xs font-medium text-neutral-300 transition-colors hover:bg-neutral-700"
          onClick={e => {
            e.stopPropagation();
            onView();
          }}
        >
          <Code2 className="h-3.5 w-3.5" />
          {t('templates.library.viewDetails')}
        </button>
        {template.isActive && (
          <button
            className="flex flex-1 items-center justify-center gap-1.5 rounded bg-blue-600 px-3 py-1.5 text-xs font-medium text-white transition-colors hover:bg-blue-500"
            onClick={e => {
              e.stopPropagation();
              onScaffold();
            }}
          >
            <Zap className="h-3.5 w-3.5" />
            {t('templates.library.scaffold')}
          </button>
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
    <div className="flex flex-col gap-6 p-6">
      {/* Page header */}
      <div className="flex items-start justify-between gap-4">
        <div className="flex flex-col gap-1">
          <h1 className="text-xl font-semibold text-neutral-100">
            {t('templates.library.title')}
          </h1>
          <p className="text-sm text-neutral-400">{t('templates.library.subtitle')}</p>
        </div>
        <button
          className="flex items-center gap-2 rounded bg-blue-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-blue-500"
          onClick={() => navigate('/catalog/templates/new')}
        >
          <Plus className="h-4 w-4" />
          {t('templates.library.createTemplate')}
        </button>
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
            className="flex flex-col gap-1 rounded-lg border border-neutral-800 bg-neutral-900 p-3"
          >
            <span className="text-2xl font-bold text-neutral-100">{stat.value}</span>
            <span className="text-xs text-neutral-500">{stat.label}</span>
          </div>
        ))}
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-[200px]">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-neutral-500" />
          <input
            type="text"
            placeholder={t('templates.library.searchPlaceholder')}
            value={search}
            onChange={e => setSearch(e.target.value)}
            className="w-full rounded border border-neutral-700 bg-neutral-800 py-2 pl-9 pr-3 text-sm text-neutral-200 placeholder-neutral-500 outline-none focus:border-blue-500"
          />
        </div>

        <div className="flex items-center gap-2">
          <Filter className="h-4 w-4 text-neutral-500" />
          <select
            value={serviceType}
            onChange={e => setServiceType(e.target.value as TemplateServiceType | '')}
            className="rounded border border-neutral-700 bg-neutral-800 py-2 pl-3 pr-8 text-sm text-neutral-200 outline-none focus:border-blue-500"
          >
            <option value="">{t('templates.library.filters.allTypes')}</option>
            {Object.entries(SERVICE_TYPE_LABELS).map(([k, v]) => (
              <option key={k} value={k}>{v}</option>
            ))}
          </select>

          <select
            value={language}
            onChange={e => setLanguage(e.target.value as TemplateLanguage | '')}
            className="rounded border border-neutral-700 bg-neutral-800 py-2 pl-3 pr-8 text-sm text-neutral-200 outline-none focus:border-blue-500"
          >
            <option value="">{t('templates.library.filters.allLanguages')}</option>
            {Object.entries(LANGUAGE_LABELS).map(([k, v]) => (
              <option key={k} value={k}>{v}</option>
            ))}
          </select>

          <label className="flex cursor-pointer items-center gap-2 text-sm text-neutral-400">
            <input
              type="checkbox"
              checked={showInactive}
              onChange={e => setShowInactive(e.target.checked)}
              className="rounded border-neutral-700 bg-neutral-800"
            />
            {t('templates.library.filters.showInactive')}
          </label>
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
              className="h-56 animate-pulse rounded-lg border border-neutral-800 bg-neutral-900"
            />
          ))}
        </div>
      ) : templates.length === 0 ? (
        <EmptyState
          icon={<Layers className="h-5 w-5" />}
          title={t('templates.library.empty', 'No templates found')}
          description={t('templates.library.emptyHint', 'Create your first template to get started.')}
          action={
            <button
              className="flex items-center gap-2 rounded bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-500"
              onClick={() => navigate('/catalog/templates/new')}
            >
              <Plus className="h-4 w-4" />
              {t('templates.library.createFirst')}
            </button>
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
    </div>
  );
}
