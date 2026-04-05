import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  ArrowLeft,
  Edit,
  Zap,
  Package,
  Users,
  Tag,
  BookOpen,
  Layers,
  GitBranch,
  Power,
  PowerOff,
  CheckCircle,
  XCircle,
  Clock,
  BarChart2,
} from 'lucide-react';
import { templatesApi } from '../api/templates';

// ── Info row ──────────────────────────────────────────────────────────────────

function InfoRow({ label, value }: { label: string; value?: string | number | null }) {
  if (!value && value !== 0) return null;
  return (
    <div className="flex flex-col gap-0.5">
      <span className="text-xs text-neutral-500">{label}</span>
      <span className="text-sm text-neutral-200">{value}</span>
    </div>
  );
}

// ── Section card ──────────────────────────────────────────────────────────────

function SectionCard({
  icon: Icon,
  title,
  children,
}: {
  icon: React.ElementType;
  title: string;
  children: React.ReactNode;
}) {
  return (
    <div className="flex flex-col gap-3 rounded-lg border border-neutral-800 bg-neutral-900 p-4">
      <div className="flex items-center gap-2 text-sm font-medium text-neutral-300">
        <Icon className="h-4 w-4 text-neutral-500" />
        {title}
      </div>
      {children}
    </div>
  );
}

// ── Main page ─────────────────────────────────────────────────────────────────

export function TemplateDetailPage() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data: template, isLoading } = useQuery({
    queryKey: ['service-template', id],
    queryFn: () => templatesApi.getById(id!),
    enabled: !!id,
  });

  const activateMutation = useMutation({
    mutationFn: () => templatesApi.activate(id!),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['service-template', id] }),
  });

  const deactivateMutation = useMutation({
    mutationFn: () => templatesApi.deactivate(id!),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['service-template', id] }),
  });

  if (isLoading) {
    return (
      <div className="flex flex-col gap-4 p-6">
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="h-32 animate-pulse rounded-lg border border-neutral-800 bg-neutral-900" />
        ))}
      </div>
    );
  }

  if (!template) {
    return (
      <div className="flex flex-col items-center gap-3 p-16 text-neutral-400">
        <XCircle className="h-10 w-10 text-neutral-600" />
        <p>{t('templates.detail.notFound')}</p>
        <button onClick={() => navigate('/catalog/templates')} className="text-blue-400 hover:underline text-sm">
          {t('templates.detail.backToLibrary')}
        </button>
      </div>
    );
  }

  const isToggling = activateMutation.isPending || deactivateMutation.isPending;

  return (
    <div className="flex flex-col gap-5 p-6">
      {/* Breadcrumb + actions */}
      <div className="flex items-center justify-between gap-4">
        <button
          onClick={() => navigate('/catalog/templates')}
          className="flex items-center gap-1.5 text-sm text-neutral-400 hover:text-neutral-200"
        >
          <ArrowLeft className="h-4 w-4" />
          {t('templates.detail.backToLibrary')}
        </button>

        <div className="flex items-center gap-2">
          {template.isActive ? (
            <button
              disabled={isToggling}
              onClick={() => deactivateMutation.mutate()}
              className="flex items-center gap-1.5 rounded border border-neutral-700 bg-neutral-800 px-3 py-1.5 text-xs font-medium text-neutral-300 hover:border-red-500/50 hover:text-red-400 disabled:opacity-50"
            >
              <PowerOff className="h-3.5 w-3.5" />
              {t('templates.detail.deactivate')}
            </button>
          ) : (
            <button
              disabled={isToggling}
              onClick={() => activateMutation.mutate()}
              className="flex items-center gap-1.5 rounded border border-neutral-700 bg-neutral-800 px-3 py-1.5 text-xs font-medium text-neutral-300 hover:border-emerald-500/50 hover:text-emerald-400 disabled:opacity-50"
            >
              <Power className="h-3.5 w-3.5" />
              {t('templates.detail.activate')}
            </button>
          )}
          <button
            onClick={() => navigate(`/catalog/templates/${id}/edit`)}
            className="flex items-center gap-1.5 rounded border border-neutral-700 bg-neutral-800 px-3 py-1.5 text-xs font-medium text-neutral-300 hover:bg-neutral-700"
          >
            <Edit className="h-3.5 w-3.5" />
            {t('templates.detail.edit')}
          </button>
          {template.isActive && (
            <button
              onClick={() => navigate(`/catalog/templates/${id}/scaffold`)}
              className="flex items-center gap-1.5 rounded bg-blue-600 px-4 py-1.5 text-xs font-medium text-white hover:bg-blue-500"
            >
              <Zap className="h-3.5 w-3.5" />
              {t('templates.detail.scaffoldWithAi')}
            </button>
          )}
        </div>
      </div>

      {/* Hero */}
      <div className="flex flex-col gap-2 rounded-lg border border-neutral-800 bg-neutral-900 p-5">
        <div className="flex items-start justify-between gap-4">
          <div className="flex flex-col gap-1">
            <h1 className="text-lg font-semibold text-neutral-100">{template.displayName}</h1>
            <code className="text-xs text-neutral-500">{template.slug}</code>
          </div>
          <div className="flex shrink-0 items-center gap-2">
            {template.isActive ? (
              <span className="flex items-center gap-1.5 rounded-full bg-emerald-500/10 px-2.5 py-1 text-xs font-medium text-emerald-400">
                <CheckCircle className="h-3.5 w-3.5" />
                {t('templates.detail.active')}
              </span>
            ) : (
              <span className="flex items-center gap-1.5 rounded-full bg-neutral-700/50 px-2.5 py-1 text-xs font-medium text-neutral-400">
                <XCircle className="h-3.5 w-3.5" />
                {t('templates.detail.inactive')}
              </span>
            )}
          </div>
        </div>
        <p className="text-sm text-neutral-400">{template.description}</p>

        <div className="mt-2 flex flex-wrap gap-1.5">
          <span className="rounded border border-purple-500/20 bg-purple-500/10 px-1.5 py-0.5 text-xs text-purple-400">
            {template.language}
          </span>
          <span className="rounded border border-blue-500/20 bg-blue-500/10 px-1.5 py-0.5 text-xs text-blue-400">
            {template.serviceType}
          </span>
          <span className="rounded border border-neutral-700 bg-neutral-800 px-1.5 py-0.5 text-xs text-neutral-400">
            v{template.version}
          </span>
        </div>
      </div>

      {/* Content grid */}
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        {/* Left: details */}
        <div className="flex flex-col gap-4 lg:col-span-2">
          <SectionCard icon={Package} title={t('templates.detail.ownership')}>
            <div className="grid grid-cols-2 gap-4">
              <InfoRow label={t('templates.detail.defaultDomain')} value={template.defaultDomain} />
              <InfoRow label={t('templates.detail.defaultTeam')} value={template.defaultTeam} />
            </div>
          </SectionCard>

          {template.repositoryTemplateUrl && (
            <SectionCard icon={GitBranch} title={t('templates.detail.repository')}>
              <div className="grid grid-cols-2 gap-4">
                <InfoRow label={t('templates.detail.repoUrl')} value={template.repositoryTemplateUrl} />
                <InfoRow label={t('templates.detail.repoBranch')} value={template.repositoryTemplateBranch} />
              </div>
            </SectionCard>
          )}

          {template.tags.length > 0 && (
            <SectionCard icon={Tag} title={t('templates.detail.tags')}>
              <div className="flex flex-wrap gap-1.5">
                {template.tags.map(tag => (
                  <span key={tag} className="rounded bg-neutral-800 px-2 py-0.5 text-xs text-neutral-300">
                    {tag}
                  </span>
                ))}
              </div>
            </SectionCard>
          )}

          {template.hasBaseContract && template.baseContractSpec && (
            <SectionCard icon={BookOpen} title={t('templates.detail.baseContract')}>
              <pre className="max-h-64 overflow-auto rounded bg-neutral-950 p-3 text-xs text-neutral-300">
                {template.baseContractSpec}
              </pre>
            </SectionCard>
          )}

          {template.hasScaffoldingManifest && template.scaffoldingManifestJson && (
            <SectionCard icon={Layers} title={t('templates.detail.scaffoldingManifest')}>
              <pre className="max-h-64 overflow-auto rounded bg-neutral-950 p-3 text-xs text-neutral-300">
                {(() => {
                  try {
                    return JSON.stringify(JSON.parse(template.scaffoldingManifestJson), null, 2);
                  } catch {
                    return template.scaffoldingManifestJson;
                  }
                })()}
              </pre>
            </SectionCard>
          )}
        </div>

        {/* Right: stats + meta */}
        <div className="flex flex-col gap-4">
          <SectionCard icon={BarChart2} title={t('templates.detail.stats')}>
            <div className="flex flex-col gap-3">
              <div className="flex flex-col gap-0.5">
                <span className="text-2xl font-bold text-neutral-100">{template.usageCount}</span>
                <span className="text-xs text-neutral-500">{t('templates.detail.timesUsed')}</span>
              </div>
              <div className="flex items-center gap-2 text-xs text-neutral-500">
                <CheckCircle className="h-3.5 w-3.5 text-emerald-400" />
                {template.hasBaseContract ? t('templates.detail.hasContract') : t('templates.detail.noContract')}
              </div>
              <div className="flex items-center gap-2 text-xs text-neutral-500">
                <Layers className="h-3.5 w-3.5 text-blue-400" />
                {template.hasScaffoldingManifest ? t('templates.detail.hasManifest') : t('templates.detail.noManifest')}
              </div>
            </div>
          </SectionCard>

          <SectionCard icon={Clock} title={t('templates.detail.audit')}>
            <div className="flex flex-col gap-3">
              <InfoRow
                label={t('templates.detail.createdAt')}
                value={new Date(template.createdAt).toLocaleDateString()}
              />
              {template.updatedAt && (
                <InfoRow
                  label={t('templates.detail.updatedAt')}
                  value={new Date(template.updatedAt).toLocaleDateString()}
                />
              )}
            </div>
          </SectionCard>

          {template.governancePolicyIds.length > 0 && (
            <SectionCard icon={Users} title={t('templates.detail.governancePolicies')}>
              <div className="flex flex-col gap-1">
                {template.governancePolicyIds.map(pid => (
                  <code key={pid} className="truncate text-xs text-neutral-500">
                    {pid}
                  </code>
                ))}
              </div>
            </SectionCard>
          )}
        </div>
      </div>
    </div>
  );
}
