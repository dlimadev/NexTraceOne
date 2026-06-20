import { useTranslation } from 'react-i18next';
import { Button } from '../../../../shared/ui';
import type { ContractSummary } from '../ContractIdentityCard';

interface ConfirmTabProps {
  summary: ContractSummary;
  description: string;
  canCreate: boolean;
  isCreating: boolean;
  isError: boolean;
  onCreate: () => void;
}

/**
 * Recapitulação final (read-only) do contrato + acção de criação do draft.
 * Componente presentacional: dispara a criação via callback `onCreate`.
 */
export function ConfirmTab({ summary, description, canCreate, isCreating, isError, onCreate }: ConfirmTabProps) {
  const { t } = useTranslation();

  return (
    <div className="space-y-5">
      <div>
        <h2 className="text-base font-semibold text-heading mb-1">
          {t('contracts.create.confirmTitle', 'Review and create')}
        </h2>
        <p className="text-xs text-muted">
          {t('contracts.create.confirmHint', 'Confirm the contract identity before creating the draft.')}
        </p>
      </div>

      {/* Read-only recap */}
      <div className="rounded-xl border border-edge bg-card divide-y divide-edge">
        <RecapRow label={t('contracts.create.linkedService', 'Service')} value={summary.serviceName || '—'} />
        <RecapRow
          label={t('contracts.create.selectType', 'Type')}
          value={summary.type ? t(`contracts.contractTypes.${summary.type}`, summary.type) : '—'}
        />
        <RecapRow label={t('contracts.create.cardProtocol', 'Protocol')} value={summary.protocol || '—'} />
        <RecapRow
          label={t('contracts.create.selectMode', 'Mode')}
          value={
            summary.mode
              ? t(`contracts.create.mode${summary.mode.charAt(0).toUpperCase() + summary.mode.slice(1)}`, summary.mode)
              : '—'
          }
        />
        <RecapRow label={t('contracts.create.name', 'Name')} value={summary.title || '—'} />
        <RecapRow label={t('contracts.create.description', 'Description')} value={description || '—'} />
      </div>

      {/* Error */}
      {isError && (
        <div className="text-xs text-critical bg-critical/15 border border-critical/25 rounded-lg px-4 py-3">
          {t('contracts.errors.createVersionFailed', 'Failed to create contract')}
        </div>
      )}

      {/* Action */}
      <div className="flex justify-end pt-1">
        <Button variant="primary" size="lg" onClick={onCreate} disabled={!canCreate} loading={isCreating}>
          {isCreating
            ? t('contracts.create.creatingDraft', 'Creating draft...')
            : t('contracts.create.createDraft', 'Criar draft')}
        </Button>
      </div>
    </div>
  );
}

function RecapRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-start justify-between gap-3 px-4 py-2.5 text-xs">
      <span className="text-muted shrink-0">{label}</span>
      <span className="text-heading font-medium text-right break-words">{value}</span>
    </div>
  );
}
