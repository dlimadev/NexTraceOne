import { useTranslation } from 'react-i18next';
import { Check, Circle, ArrowRight } from 'lucide-react';
import { Button } from '../../../shared/ui';
import { cn } from '../../../lib/cn';
import { supportsContracts } from '../../contracts/shared/serviceContractPolicy';
import type { ServiceType } from '../../../types';
import { deriveSetupItems, setupProgress, type SetupItemId, type SetupServiceInput } from './setupChecklist';

interface ServiceSetupChecklistProps {
  service: SetupServiceInput;
  contractCount: number;
  lifecycleStatus: string;
  onEditOwnership: () => void;
  onEditReferences: () => void;
  onAddInterface: () => void;
  onAddContract: () => void;
}

/** Checklist de setup guiado do serviço (detalhe). Honest-null, deep-link por lacuna. */
export function ServiceSetupChecklist({
  service, contractCount, lifecycleStatus,
  onEditOwnership, onEditReferences, onAddInterface, onAddContract,
}: ServiceSetupChecklistProps) {
  const { t } = useTranslation();
  const items = deriveSetupItems(service, contractCount, (ty) => supportsContracts(ty as ServiceType));
  if (items.length === 0) return null;

  const { done, total, allDone } = setupProgress(items);
  const isActive = lifecycleStatus === 'Active';
  const labelKey: Record<SetupItemId, string> = {
    ownership: 'serviceSetup.items.ownership',
    repository: 'serviceSetup.items.repository',
    documentation: 'serviceSetup.items.documentation',
    interface: 'serviceSetup.items.interface',
    contract: 'serviceSetup.items.contract',
  };
  const cta: Record<SetupItemId, () => void> = {
    ownership: onEditOwnership,
    repository: onEditReferences,
    documentation: onEditReferences,
    interface: onAddInterface,
    contract: onAddContract,
  };

  return (
    <div className="rounded-xl border border-edge bg-card p-4 mb-5">
      <div className="flex items-center justify-between mb-3">
        <div>
          <h3 className="text-sm font-semibold text-heading">{t('serviceSetup.title')}</h3>
          <p className="text-xs text-muted mt-0.5">{t('serviceSetup.subtitle')}</p>
        </div>
        <span className="text-xs font-medium text-muted shrink-0">{t('serviceSetup.progress', { done, total })}</span>
      </div>

      <div className="h-1.5 rounded-full bg-elevated overflow-hidden mb-3">
        <div className="h-full bg-accent transition-all" style={{ width: total ? `${(done / total) * 100}%` : '0%' }} />
      </div>

      <ul className="divide-y divide-edge/60">
        {items.map((item) => (
          <li key={item.id} className="flex items-center gap-3 py-2.5 text-sm">
            <span className={cn('flex items-center justify-center w-5 h-5 rounded-full shrink-0',
              item.done ? 'bg-success text-white' : 'bg-elevated text-muted')}>
              {item.done ? <Check size={12} /> : <Circle size={10} />}
            </span>
            <span className={cn('min-w-0 truncate', item.done ? 'text-muted line-through' : 'text-heading')}>
              {t(labelKey[item.id])}
            </span>
            <span className="ml-auto shrink-0">
              {!item.applicable ? (
                <span className="text-xs text-muted">{t('serviceSetup.na')}</span>
              ) : !item.done ? (
                <Button variant="ghost" size="xs" data-testid={`setup-cta-${item.id}`}
                  onClick={cta[item.id]} icon={<ArrowRight size={12} />}>
                  {t('serviceSetup.action')}
                </Button>
              ) : null}
            </span>
          </li>
        ))}
      </ul>

      {allDone && !isActive && (
        <p className="mt-3 text-xs text-success">{t('serviceSetup.complete')}</p>
      )}
    </div>
  );
}
