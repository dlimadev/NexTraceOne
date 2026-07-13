import { useNavigate, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ChevronLeft } from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { ContractCreateForm } from './ContractCreateForm';
import type { ContractTypeValue } from '../shared/constants';
import type { CreationMode } from './contractCreateConstants';

/**
 * Wrapper de página para o formulário de criação de contrato.
 * Lê os searchParams, envolve em PageContainer + back-link e delega ao ContractCreateForm.
 */
export function CreateContractPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const prefilledServiceId = searchParams.get('serviceId') ?? '';
  const initialType = (searchParams.get('type') as ContractTypeValue | null) ?? null;
  const initialMode = (searchParams.get('mode') as CreationMode | null) ?? null;

  return (
    <PageContainer className="animate-fade-in">
      <div className="flex items-center justify-between mb-4">
        <button
          onClick={() => navigate('/contracts/studio/new')}
          className="inline-flex items-center gap-1.5 text-sm text-muted hover:text-heading transition-colors"
        >
          <ChevronLeft size={14} /> {t('contractStudio.title', 'Contract Studio')}
        </button>
      </div>
      <ContractCreateForm
        prefilledServiceId={prefilledServiceId}
        initialType={initialType}
        initialMode={initialMode}
        onCreated={(draftId) => navigate(`/contracts/studio/${draftId}`)}
        onCancel={() => navigate('/contracts')}
      />
    </PageContainer>
  );
}
