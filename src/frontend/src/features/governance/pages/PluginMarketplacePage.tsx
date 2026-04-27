import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Package, Star, Download, Search, Filter, ExternalLink, ShieldCheck, AlertCircle } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { EmptyState } from '../../../components/EmptyState';
import { PageLoadingState } from '../../../components/PageLoadingState';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────────

interface DashboardTemplate {
  id: string;
  name: string;
  description?: string;
  persona?: string;
  category?: string;
  tags: string[];
  isSystem: boolean;
  widgetCount: number;
  previewImageUrl?: string;
  authorName?: string;
  installCount: number;
  rating?: number;
  tenantId: string;
  createdAt: string;
}

const CATEGORIES = ['all', 'operations', 'engineering', 'executive', 'compliance', 'finops', 'ai'];
const PERSONAS = ['all', 'engineer', 'tech-lead', 'architect', 'product', 'executive', 'platform-admin', 'auditor'];

// ── Hook ────────────────────────────────────────────────────────────────────────

const useTemplates = (persona?: string, category?: string) =>
  useQuery({
    queryKey: ['dashboard-templates', persona, category],
    queryFn: () =>
      client
        .get<{ items: DashboardTemplate[]; totalCount: number }>('/api/v1/governance/dashboard-templates', {
          params: { tenantId: 'default', persona: persona !== 'all' ? persona : undefined, category: category !== 'all' ? category : undefined },
        })
        .then((r) => r.data),
  });

// ── Template Card ──────────────────────────────────────────────────────────────

function TemplateCard({ template, onInstall }: { template: DashboardTemplate; onInstall: (id: string) => void }) {
  const { t } = useTranslation();

  return (
    <Card className="hover:border-accent/60 transition-colors">
      <CardBody>
        <div className="flex items-start justify-between gap-3">
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-1">
              <Package size={14} className="text-accent shrink-0" />
              <h3 className="text-sm font-semibold truncate">{template.name}</h3>
              {template.isSystem && (
                <Badge variant="secondary" className="text-xs shrink-0">
                  <ShieldCheck size={10} className="mr-1" />
                  {t('pluginMarketplace.system')}
                </Badge>
              )}
            </div>
            {template.description && (
              <p className="text-xs text-muted-foreground line-clamp-2 mb-2">{template.description}</p>
            )}
            <div className="flex flex-wrap gap-1 mb-2">
              {template.persona && (
                <Badge variant="outline" className="text-xs">{template.persona}</Badge>
              )}
              {template.category && (
                <Badge variant="outline" className="text-xs">{template.category}</Badge>
              )}
              {template.tags.slice(0, 2).map((tag) => (
                <Badge key={tag} variant="secondary" className="text-xs">{tag}</Badge>
              ))}
            </div>
            <div className="flex items-center gap-3 text-xs text-muted-foreground">
              <span className="flex items-center gap-1">
                <Download size={10} />
                {template.installCount}
              </span>
              {template.rating && (
                <span className="flex items-center gap-1">
                  <Star size={10} className="fill-warning text-warning" />
                  {template.rating.toFixed(1)}
                </span>
              )}
              <span>{template.widgetCount} {t('pluginMarketplace.widgets')}</span>
            </div>
          </div>
        </div>
        <div className="flex gap-2 mt-3">
          <Button size="sm" onClick={() => onInstall(template.id)} className="flex-1">
            <Download size={12} className="mr-1" />
            {t('pluginMarketplace.install')}
          </Button>
          <Button size="sm" variant="ghost">
            <ExternalLink size={12} />
          </Button>
        </div>
      </CardBody>
    </Card>
  );
}

// ── Main Page ──────────────────────────────────────────────────────────────────

export function PluginMarketplacePage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('all');
  const [selectedPersona, setSelectedPersona] = useState('all');

  const { data, isLoading } = useTemplates(
    selectedPersona !== 'all' ? selectedPersona : undefined,
    selectedCategory !== 'all' ? selectedCategory : undefined,
  );

  const filtered = (data?.items ?? []).filter(
    (t) =>
      !search ||
      t.name.toLowerCase().includes(search.toLowerCase()) ||
      t.description?.toLowerCase().includes(search.toLowerCase()),
  );

  const handleInstall = (templateId: string) => {
    client
      .post(`/api/v1/governance/dashboard-templates/${templateId}/instantiate`, {
        tenantId: 'default',
        userId: 'current-user',
        name: '',
      })
      .then(() => {});
  };

  return (
    <PageContainer>
      <PageHeader
        title={t('pluginMarketplace.title')}
        subtitle={t('pluginMarketplace.subtitle')}
      />

      {/* Simulated Banner */}
      <div className="mx-4 mb-4 p-3 rounded-lg border border-info/40 bg-info/5 flex items-center gap-2 text-xs text-muted-foreground">
        <AlertCircle size={12} className="text-info" />
        {t('pluginMarketplace.simulatedBanner')}
      </div>

      <PageSection>
        {/* Filters */}
        <div className="flex flex-wrap gap-3 mb-6">
          <div className="relative">
            <Search size={14} className="absolute left-2.5 top-1/2 -translate-y-1/2 text-muted-foreground" />
            <input
              className="pl-8 pr-3 py-1.5 text-sm rounded border border-border bg-background focus:outline-none focus:ring-1 focus:ring-accent"
              placeholder={t('pluginMarketplace.searchPlaceholder')}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>

          <div className="flex items-center gap-1 flex-wrap">
            <Filter size={12} className="text-muted-foreground" />
            {CATEGORIES.map((cat) => (
              <button
                key={cat}
                onClick={() => setSelectedCategory(cat)}
                className={`px-2 py-1 rounded text-xs transition-colors ${
                  selectedCategory === cat
                    ? 'bg-accent text-white'
                    : 'bg-muted text-muted-foreground hover:text-foreground'
                }`}
              >
                {t(`pluginMarketplace.category.${cat}`, { defaultValue: cat })}
              </button>
            ))}
          </div>

          <div className="flex items-center gap-1 flex-wrap">
            {PERSONAS.map((p) => (
              <button
                key={p}
                onClick={() => setSelectedPersona(p)}
                className={`px-2 py-1 rounded text-xs transition-colors ${
                  selectedPersona === p
                    ? 'bg-primary text-white'
                    : 'bg-muted text-muted-foreground hover:text-foreground'
                }`}
              >
                {t(`pluginMarketplace.persona.${p}`, { defaultValue: p })}
              </button>
            ))}
          </div>
        </div>

        {/* Grid */}
        {isLoading ? (
          <PageLoadingState />
        ) : filtered.length === 0 ? (
          <EmptyState
            icon={<Package size={24} />}
            title={t('pluginMarketplace.empty')}
            description={t('pluginMarketplace.emptyHint')}
          />
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            {filtered.map((template) => (
              <TemplateCard key={template.id} template={template} onInstall={handleInstall} />
            ))}
          </div>
        )}
      </PageSection>
    </PageContainer>
  );
}
