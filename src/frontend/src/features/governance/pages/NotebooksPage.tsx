import * as React from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { BookOpen, Plus, Bot, Clock, Layers } from 'lucide-react';
import { notebooksApi } from '../api/notebooks';
import { useAuth } from '../../../contexts/AuthContext';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { Card } from '../../../components/Card';
import { FormattedTimestamp } from '../../../components/FormattedTimestamp';
import { AiComposeDashboardModal } from '../components/AiComposeDashboardModal';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';

const STATUS_COLORS: Record<string, 'blue' | 'green' | 'gray'> = {
  Draft: 'blue',
  Published: 'green',
  Archived: 'gray',
};

export function NotebooksPage() {
  const { t } = useTranslation();
  const { user } = useAuth();
  const tenantId = user?.tenantId ?? '';

  const [composeOpen, setComposeOpen] = React.useState(false);

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['notebooks', tenantId],
    queryFn: () => notebooksApi.list({ tenantId, page: 1, pageSize: 50 }),
    enabled: !!tenantId,
  });

  if (isLoading) return <PageLoadingState />;
  if (isError) return <PageErrorState onRetry={() => refetch()} />;

  const notebooks = data?.items ?? [];

  return (
    <PageContainer>
      {/* Cabeçalho com CTAs no slot de ações do PageHeader */}
      <PageHeader
        title={t('notebook.title')}
        subtitle={t('notebook.emptyHint')}
        icon={<BookOpen className="h-6 w-6" />}
        actions={
          <>
            <Button
              variant="outline"
              size="sm"
              icon={<Bot className="h-4 w-4" />}
              onClick={() => setComposeOpen(true)}
            >
              {t('aiCompose.title')}
            </Button>
            {/* Link envolve o Button — asChild não suportado neste DS Button */}
            <Link to="/governance/notebooks/new">
              <Button size="sm" icon={<Plus className="h-4 w-4" />}>
                {t('notebook.new')}
              </Button>
            </Link>
          </>
        }
      />

      {/* Lista de notebooks */}
      {notebooks.length === 0 ? (
        <EmptyState
          icon={<BookOpen className="h-10 w-10" />}
          title={t('notebook.empty')}
          description={t('notebook.emptyHint')}
          action={
            <Link to="/governance/notebooks/new">
              <Button size="sm" icon={<Plus className="h-4 w-4" />}>
                {t('notebook.new')}
              </Button>
            </Link>
          }
        />
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          {notebooks.map((nb) => (
            <Card key={nb.notebookId} className="p-4 hover:shadow-md transition-shadow">
              <Link to={`/governance/notebooks/${nb.notebookId}`} className="block">
                <div className="flex items-start justify-between mb-2">
                  <h3 className="font-semibold text-heading truncate">
                    {nb.title}
                  </h3>
                  <Badge variant={STATUS_COLORS[nb.status] ?? 'gray'} size="sm">
                    {t(`notebook.status${nb.status}`)}
                  </Badge>
                </div>

                {nb.description && (
                  <p className="text-sm text-muted line-clamp-2 mb-3">
                    {nb.description}
                  </p>
                )}

                <div className="flex items-center gap-4 text-xs text-faded">
                  <span className="flex items-center gap-1">
                    <Layers className="h-3 w-3" />
                    {t('notebook.cellCount', { count: nb.cellCount })}
                  </span>
                  <span className="flex items-center gap-1">
                    <Clock className="h-3 w-3" />
                    <FormattedTimestamp value={nb.updatedAt} format="relative" />
                  </span>
                  {/* Persona badge com token semântico em vez de text-indigo-500 */}
                  <span className="capitalize text-accent">{nb.persona}</span>
                </div>
              </Link>
            </Card>
          ))}
        </div>
      )}

      {/* Modal AI Compose */}
      <AiComposeDashboardModal
        open={composeOpen}
        onClose={() => setComposeOpen(false)}
        tenantId={tenantId}
        userId={user?.id ?? ''}
        persona={user?.persona ?? 'Engineer'}
      />
    </PageContainer>
  );
}
