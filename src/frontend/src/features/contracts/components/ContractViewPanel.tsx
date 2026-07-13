import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ExternalLink, Lock } from 'lucide-react';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { ContractSection } from '../workspace/sections/ContractSection';
import { useContractDetail } from '../hooks/useContractDetail';

/**
 * Vista read-only de uma versão de contrato, para o drawer in-place do serviço.
 * Exibe o resumo da versão (nome, protocolo, semVer, lifecycle, bloqueio)
 * e o conteúdo da spec em modo leitura via ContractSection.
 */
export function ContractViewPanel({ contractVersionId }: { contractVersionId: string }) {
  const { t } = useTranslation();
  const { data: detail, isLoading, isError } = useContractDetail(contractVersionId);

  if (isLoading) return <PageLoadingState size="md" />;
  if (isError || !detail) return <PageErrorState message={t('common.noResults', 'No results')} />;

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center gap-2">
        <span className="font-mono text-sm font-semibold text-heading">{detail.apiName ?? '—'}</span>
        <Badge variant="primary" size="sm">{detail.protocol}</Badge>
        <Badge variant="default" size="sm">v{detail.semVer ?? '—'}</Badge>
        <Badge variant="info" size="sm">{detail.lifecycleState}</Badge>
        {detail.isLocked && <Lock size={14} className="text-info" />}
      </div>
      <ContractSection
        specContent={detail.specContent ?? ''}
        format={detail.format ?? 'yaml'}
        protocol={detail.protocol}
        isReadOnly
        className="border border-edge rounded-lg overflow-hidden h-[60vh] min-h-[420px]"
      />
      <Link
        to={`/source-of-truth/contracts/${contractVersionId}`}
        className="inline-flex items-center gap-1.5 text-xs text-accent hover:underline"
      >
        <ExternalLink size={12} /> {t('serviceDetail.openSourceOfTruth', 'Abrir fonte de verdade')}
      </Link>
    </div>
  );
}
