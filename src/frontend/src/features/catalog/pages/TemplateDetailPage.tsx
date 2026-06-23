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
  CheckCircle2,
  XCircle,
  Clock,
  BarChart2,
} from 'lucide-react';
import { Button } from '../../../shared/ui';
import { templatesApi } from '../api/templates';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';

// ── Info row ──────────────────────────────────────────────────────────────────

function InfoRow({ label, value }: { label: string; value?: string | number | null }) {
  if (!value && value !== 0) return null;
  return (
    <div className="flex flex-col gap-0.5">
      <span className="text-xs text-muted">{label}</span>
      <span className="text-sm text-body">{value}</span>
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
    <div className="flex flex-col gap-3 rounded-lg border border-edge bg-elevated p-4">
      <div className="flex items-center gap-2 text-sm font-medium text-body">
        <Icon className="h-4 w-4 text-muted" />
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

  const { data: template, isLoading, isError } = useQuery({
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
          <div key={i} className="h-32 animate-pulse rounded-lg border border-edge bg-elevated" />
        ))}
      </div>
    );
  }

  if (isError) {
    return (
      <div className="flex flex-col gap-4 p-6">
        <PageErrorState />
      </div>
    );
  }

  if (!template) {
    return (
      <div className="flex flex-col items-center gap-3 p-16 text-muted">
        <XCircle className="h-10 w-10 text-muted" />
        <p>{t('templates.detail.notFound')}</p>
        <Button variant="ghost" size="sm" onClick={() => navigate('/catalog/templates')}>
          {t('templates.detail.backToLibrary')}
        </Button>
      </div>
    );
  }

  const isToggling = activateMutation.isPending || deactivateMutation.isPending;

  return (
    <PageContainer>
      <PageHeader
        title={template.displayName}
        subtitle={template.description}
      />
      {/* Breadcrumb + actions */}
      <div className="flex items-center justify-between gap-4">
        <Button
          variant="ghost"
          size="sm"
          icon={<ArrowLeft className="h-4 w-4" />}
          onClick={() => navigate('/catalog/templates')}
        >
          {t('templates.detail.backToLibrary')}
        </Button>

        <div className="flex items-center gap-2">
          {template.isActive ? (
            <Button
              variant="outline"
              size="sm"
              icon={<PowerOff className="h-3.5 w-3.5" />}
              disabled={isToggling}
              onClick={() => deactivateMutation.mutate()}
            >
              {t('templates.detail.deactivate')}
            </Button>
          ) : (
            <Button
              variant="outline"
              size="sm"
              icon={<Power className="h-3.5 w-3.5" />}
              disabled={isToggling}
              onClick={() => activateMutation.mutate()}
            >
              {t('templates.detail.activate')}
            </Button>
          )}
          <Button
            variant="outline"
            size="sm"
            icon={<Edit className="h-3.5 w-3.5" />}
            onClick={() => navigate(`/catalog/templates/${id}/edit`)}
          >
            {t('templates.detail.edit')}
          </Button>
          {template.isActive && (
            <Button
              variant="primary"
              size="sm"
              icon={<Zap className="h-3.5 w-3.5" />}
              onClick={() => navigate(`/catalog/templates/${id}/scaffold`)}
            >
              {t('templates.detail.scaffoldWithAi')}
            </Button>
          )}
        </div>
      </div>

      {/* Hero */}
      <div className="flex flex-col gap-2 rounded-lg border border-edge bg-elevated p-5">
        <div className="flex items-start justify-between gap-4">
          <div className="flex flex-col gap-1">
            <h1 className="text-lg font-semibold text-body">{template.displayName}</h1>
            <code className="text-xs text-muted">{template.slug}</code>
          </div>
          <div className="flex shrink-0 items-center gap-2">
            {template.isActive ? (
              <span className="flex items-center gap-1.5 rounded-full bg-success/10 px-2.5 py-1 text-xs font-medium text-success">
                <CheckCircle2 className="h-3.5 w-3.5" />
                {t('templates.detail.active')}
              </span>
            ) : (
              <span className="flex items-center gap-1.5 rounded-full bg-card/50 px-2.5 py-1 text-xs font-medium text-muted">
                <XCircle className="h-3.5 w-3.5" />
                {t('templates.detail.inactive')}
              </span>
            )}
          </div>
        </div>
        <p className="text-sm text-muted">{template.description}</p>

        <div className="mt-2 flex flex-wrap gap-1.5">
          <span className="rounded border border-info/20 bg-info-muted px-1.5 py-0.5 text-xs text-info">
            {template.language}
          </span>
          <span className="rounded border border-accent/20 bg-accent/10 px-1.5 py-0.5 text-xs text-accent">
            {template.serviceType}
          </span>
          <span className="rounded border border-edge bg-elevated px-1.5 py-0.5 text-xs text-muted">
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
                  <span key={tag} className="rounded bg-elevated px-2 py-0.5 text-xs text-body">
                    {tag}
                  </span>
                ))}
              </div>
            </SectionCard>
          )}

          {template.hasBaseContract && template.baseContractSpec && (
            <SectionCard icon={BookOpen} title={t('templates.detail.baseContract')}>
              <pre className="max-h-64 overflow-auto rounded bg-elevated p-3 text-xs text-body">
                {template.baseContractSpec}
              </pre>
            </SectionCard>
          )}

          {template.hasScaffoldingManifest && template.scaffoldingManifestJson && (
            <SectionCard icon={Layers} title={t('templates.detail.scaffoldingManifest')}>
              <pre className="max-h-64 overflow-auto rounded bg-elevated p-3 text-xs text-body">
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
                <span className="text-2xl font-bold text-body">{template.usageCount}</span>
                <span className="text-xs text-muted">{t('templates.detail.timesUsed')}</span>
              </div>
              <div className="flex items-center gap-2 text-xs text-muted">
                <CheckCircle2 className="h-3.5 w-3.5 text-success" />
                {template.hasBaseContract ? t('templates.detail.hasContract') : t('templates.detail.noContract')}
              </div>
              <div className="flex items-center gap-2 text-xs text-muted">
                <Layers className="h-3.5 w-3.5 text-accent" />
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
                  <code key={pid} className="truncate text-xs text-muted">
                    {pid}
                  </code>
                ))}
              </div>
            </SectionCard>
          )}
        </div>
      </div>
    </PageContainer>
  );
}
