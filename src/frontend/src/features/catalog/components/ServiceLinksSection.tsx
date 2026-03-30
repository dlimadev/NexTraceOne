import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  Plus,
  Trash2,
  Pencil,
  ExternalLink,
  GitBranch,
  FileText,
  Activity,
  BookOpen,
  LayoutDashboard,
  Workflow,
  AlertCircle,
  Globe,
  Layers,
  Link as LinkIcon,
  X,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { serviceCatalogApi } from '../api';
import type { LinkCategory, ServiceLinkItem, ServiceLinkPayload } from '../api/serviceCatalog';

/** Ícones por categoria de link. */
const categoryIcons: Record<LinkCategory, React.ComponentType<{ size?: number; className?: string }>> = {
  Repository: GitBranch,
  Documentation: FileText,
  CiCd: Workflow,
  Monitoring: Activity,
  Wiki: BookOpen,
  SwaggerUi: Globe,
  ApiPortal: Globe,
  Backstage: Layers,
  Adr: AlertCircle,
  Runbook: BookOpen,
  Changelog: FileText,
  Dashboard: LayoutDashboard,
  Other: LinkIcon,
};

/** Lista de categorias disponíveis para o select. */
const linkCategories: LinkCategory[] = [
  'Repository',
  'Documentation',
  'CiCd',
  'Monitoring',
  'Wiki',
  'SwaggerUi',
  'ApiPortal',
  'Backstage',
  'Adr',
  'Runbook',
  'Changelog',
  'Dashboard',
  'Other',
];

interface ServiceLinksSectionProps {
  serviceId: string;
  isReadOnly?: boolean;
}

/** Secção de links categorizados de um serviço — substitui os campos fixos DocumentationUrl/RepositoryUrl. */
export function ServiceLinksSection({ serviceId, isReadOnly = false }: ServiceLinksSectionProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [editingLink, setEditingLink] = useState<ServiceLinkItem | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['service-links', serviceId],
    queryFn: () => serviceCatalogApi.listServiceLinks(serviceId),
    enabled: !!serviceId,
  });

  const addMutation = useMutation({
    mutationFn: (payload: ServiceLinkPayload) =>
      serviceCatalogApi.addServiceLink(serviceId, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['service-links', serviceId] });
      setShowForm(false);
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ linkId, payload }: { linkId: string; payload: ServiceLinkPayload }) =>
      serviceCatalogApi.updateServiceLink(serviceId, linkId, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['service-links', serviceId] });
      setEditingLink(null);
    },
  });

  const removeMutation = useMutation({
    mutationFn: (linkId: string) =>
      serviceCatalogApi.removeServiceLink(serviceId, linkId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['service-links', serviceId] });
    },
  });

  const links = data?.items ?? [];

  // Agrupar por categoria
  const grouped = links.reduce<Record<string, ServiceLinkItem[]>>((acc, link) => {
    const cat = link.category;
    if (!acc[cat]) acc[cat] = [];
    acc[cat].push(link);
    return acc;
  }, {});

  const handleSubmit = (payload: ServiceLinkPayload) => {
    if (editingLink) {
      updateMutation.mutate({ linkId: editingLink.linkId, payload });
    } else {
      addMutation.mutate(payload);
    }
  };

  const handleEdit = (link: ServiceLinkItem) => {
    setEditingLink(link);
    setShowForm(true);
  };

  const handleCancel = () => {
    setShowForm(false);
    setEditingLink(null);
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Layers size={16} className="text-accent" aria-hidden="true" />
            <h2 className="text-base font-semibold text-heading">
              {t('catalog.detail.links')}
            </h2>
            {links.length > 0 && (
              <span className="text-[10px] text-muted bg-elevated px-1.5 py-0.5 rounded">
                {links.length}
              </span>
            )}
          </div>
          {!isReadOnly && !showForm && (
            <button
              onClick={() => { setEditingLink(null); setShowForm(true); }}
              className="inline-flex items-center gap-1 text-xs text-accent hover:text-accent/80 transition-colors"
            >
              <Plus size={12} />
              {t('catalog.detail.serviceLinks.addLink')}
            </button>
          )}
        </div>
      </CardHeader>
      <CardBody>
        {/* Form (add/edit) */}
        {showForm && (
          <LinkForm
            initialData={editingLink}
            onSubmit={handleSubmit}
            onCancel={handleCancel}
            isSubmitting={addMutation.isPending || updateMutation.isPending}
          />
        )}

        {/* Loading */}
        {isLoading && (
          <div className="py-4 text-center text-xs text-muted">
            {t('common.loading')}
          </div>
        )}

        {/* Empty state */}
        {!isLoading && links.length === 0 && !showForm && (
          <div className="py-6 text-center">
            <LinkIcon size={20} className="mx-auto text-muted/30 mb-2" />
            <p className="text-xs text-muted">
              {t('catalog.detail.serviceLinks.noLinks')}
            </p>
            {!isReadOnly && (
              <button
                onClick={() => setShowForm(true)}
                className="mt-2 text-xs text-accent hover:underline"
              >
                {t('catalog.detail.serviceLinks.addFirstLink')}
              </button>
            )}
          </div>
        )}

        {/* Links grouped by category */}
        {!isLoading && Object.keys(grouped).length > 0 && (
          <div className="flex flex-col gap-3">
            {Object.entries(grouped).map(([category, categoryLinks]) => {
              const Icon = categoryIcons[category as LinkCategory] ?? LinkIcon;
              return (
                <div key={category}>
                  <p className="text-[9px] font-semibold uppercase tracking-wider text-muted/60 mb-1.5 flex items-center gap-1">
                    <Icon size={10} />
                    {t(`catalog.detail.serviceLinks.categories.${category}`, category)}
                  </p>
                  <div className="flex flex-col gap-1">
                    {categoryLinks.map((link) => (
                      <div
                        key={link.linkId}
                        className="group flex items-center gap-2 rounded px-2 py-1.5 hover:bg-elevated/50 transition-colors"
                      >
                        <a
                          href={link.url}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="flex-1 inline-flex items-center gap-1.5 text-sm text-accent hover:underline truncate"
                          title={link.description || link.url}
                        >
                          {link.title}
                          <ExternalLink size={10} className="flex-shrink-0 opacity-60" />
                        </a>
                        {!isReadOnly && (
                          <div className="hidden group-hover:flex items-center gap-1">
                            <button
                              onClick={() => handleEdit(link)}
                              className="p-0.5 text-muted hover:text-heading transition-colors"
                              title={t('common.edit')}
                            >
                              <Pencil size={11} />
                            </button>
                            <button
                              onClick={() => removeMutation.mutate(link.linkId)}
                              className="p-0.5 text-muted hover:text-danger transition-colors"
                              title={t('common.delete')}
                              disabled={removeMutation.isPending}
                            >
                              <Trash2 size={11} />
                            </button>
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </CardBody>
    </Card>
  );
}

/* ── Formulário de link (inline) ─────────────────────────────────────── */

interface LinkFormProps {
  initialData: ServiceLinkItem | null;
  onSubmit: (payload: ServiceLinkPayload) => void;
  onCancel: () => void;
  isSubmitting: boolean;
}

function LinkForm({ initialData, onSubmit, onCancel, isSubmitting }: LinkFormProps) {
  const { t } = useTranslation();
  const [category, setCategory] = useState(initialData?.category ?? 'Other');
  const [title, setTitle] = useState(initialData?.title ?? '');
  const [url, setUrl] = useState(initialData?.url ?? '');
  const [description, setDescription] = useState(initialData?.description ?? '');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim() || !url.trim()) return;
    onSubmit({ category, title: title.trim(), url: url.trim(), description: description.trim() || undefined });
  };

  return (
    <form onSubmit={handleSubmit} className="mb-4 p-3 rounded-lg bg-elevated/30 border border-edge">
      <div className="flex items-center justify-between mb-3">
        <p className="text-xs font-semibold text-heading">
          {initialData
            ? t('catalog.detail.serviceLinks.editLink')
            : t('catalog.detail.serviceLinks.addLink')}
        </p>
        <button type="button" onClick={onCancel} className="p-0.5 text-muted hover:text-heading">
          <X size={14} />
        </button>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 mb-2">
        <div>
          <label className="block text-[10px] text-muted mb-0.5">
            {t('catalog.detail.serviceLinks.form.category')}
          </label>
          <select
            value={category}
            onChange={(e) => setCategory(e.target.value as LinkCategory)}
            className="w-full text-xs bg-panel border border-edge rounded px-2 py-1.5 text-body focus:outline-none focus:ring-1 focus:ring-accent"
          >
            {linkCategories.map((cat) => (
              <option key={cat} value={cat}>
                {t(`catalog.detail.serviceLinks.categories.${cat}`, cat)}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-[10px] text-muted mb-0.5">
            {t('catalog.detail.serviceLinks.form.title')}
          </label>
          <input
            type="text"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder={t('catalog.detail.serviceLinks.form.titlePlaceholder')}
            className="w-full text-xs bg-panel border border-edge rounded px-2 py-1.5 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent"
            required
          />
        </div>
      </div>

      <div className="mb-2">
        <label className="block text-[10px] text-muted mb-0.5">
          {t('catalog.detail.serviceLinks.form.url')}
        </label>
        <input
          type="url"
          value={url}
          onChange={(e) => setUrl(e.target.value)}
          placeholder="https://..."
          className="w-full text-xs bg-panel border border-edge rounded px-2 py-1.5 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent"
          required
        />
      </div>

      <div className="mb-3">
        <label className="block text-[10px] text-muted mb-0.5">
          {t('catalog.detail.serviceLinks.form.description')}
        </label>
        <input
          type="text"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder={t('catalog.detail.serviceLinks.form.descriptionPlaceholder')}
          className="w-full text-xs bg-panel border border-edge rounded px-2 py-1.5 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent"
        />
      </div>

      <div className="flex items-center gap-2 justify-end">
        <button
          type="button"
          onClick={onCancel}
          className="text-xs text-muted hover:text-heading px-3 py-1.5 rounded transition-colors"
        >
          {t('common.cancel')}
        </button>
        <button
          type="submit"
          disabled={isSubmitting || !title.trim() || !url.trim()}
          className="text-xs bg-accent text-white px-3 py-1.5 rounded hover:bg-accent/90 transition-colors disabled:opacity-50"
        >
          {isSubmitting
            ? t('common.loading')
            : initialData
              ? t('common.save')
              : t('catalog.detail.serviceLinks.addLink')}
        </button>
      </div>
    </form>
  );
}
