import { useParams, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { ChevronLeft } from 'lucide-react';
import { contractStudioApi } from '../api/contractStudio';
import { serviceCatalogApi } from '../../catalog/api/serviceCatalog';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { DraftIdentityCard } from './components/DraftIdentityCard';
import { ContractDraftEditor } from './components/ContractDraftEditor';

const draftKeys = { detail: (id: string) => ['contract-drafts', 'detail', id] as const };

/**
 * Página do studio de edição de draft de contrato.
 * Wrapper fino que fornece o shell (PageContainer, back-link, PageHeader, DraftIdentityCard)
 * e delega a edição ao ContractDraftEditor reutilizável.
 */
export function DraftStudioPage() {
  const { draftId } = useParams<{ draftId: string }>();
  const { t } = useTranslation();
  const draftQuery = useQuery({ queryKey: draftKeys.detail(draftId ?? ''), queryFn: () => contractStudioApi.getDraft(draftId!), enabled: !!draftId });
  const servicesQuery = useQuery({ queryKey: ['catalog-services-for-contract-drafts'], queryFn: () => serviceCatalogApi.listServices() });
  const draft = draftQuery.data;

  if (draftQuery.isLoading) return <PageContainer><PageLoadingState size="lg" /></PageContainer>;
  if (draftQuery.isError || !draft || !draftId) {
    return (
      <PageContainer>
        <PageErrorState
          message={t('contracts.studio.draftNotFound', 'Draft not found or failed to load.')}
          action={<Link to="/contracts" className="text-sm text-accent hover:underline">{t('contracts.studio.backToCatalog', 'Back to Contracts')}</Link>}
        />
      </PageContainer>
    );
  }

  const linkedServiceName = servicesQuery.data?.items?.find((s) => s.serviceId === draft.serviceId)?.displayName;

  return (
    <PageContainer className="animate-fade-in">
      <div className="mb-4">
        <Link to="/contracts" className="inline-flex items-center gap-1.5 text-sm text-muted hover:text-heading transition-colors">
          <ChevronLeft size={14} /> {t('contracts.title', 'Contracts')}
        </Link>
      </div>
      <PageHeader title={draft.title} subtitle={`${draft.protocol} · v${draft.proposedVersion}`} />
      <div className="grid grid-cols-1 lg:grid-cols-[300px_minmax(0,1fr)] gap-6 items-start">
        <div className="lg:sticky lg:top-4">
          <DraftIdentityCard draft={draft} serviceName={linkedServiceName} />
        </div>
        <div className="min-w-0">
          <ContractDraftEditor draftId={draftId} />
        </div>
      </div>
    </PageContainer>
  );
}
