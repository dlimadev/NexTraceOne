import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, ArrowRight, Check, X } from 'lucide-react';
import { Button } from '../../../shared/ui';
import { cn } from '../../../lib/cn';
import { allowedContractTypes } from '../shared/serviceContractPolicy';
import type { ServiceType } from '../../../types';
import { useContractDraftForm } from './useContractDraftForm';
import { ContractIdentityCard } from './ContractIdentityCard';
import { ServiceTab } from './tabs/ServiceTab';
import { TypeModeTab } from './tabs/TypeModeTab';
import { DetailsTab } from './tabs/DetailsTab';
import { ConfirmTab } from './tabs/ConfirmTab';
import { FORM_TABS, FORM_TAB_LABEL_KEY, type FormTab, type CreationMode } from './contractCreateConstants';
import type { ContractTypeValue } from '../shared/constants';

/** Rótulos default (português) por tab — fallback de i18n até as chaves landarem. */
const FORM_TAB_LABEL_DEFAULT: Record<FormTab, string> = {
  service: 'Serviço',
  typeMode: 'Tipo & Modo',
  details: 'Detalhes',
  confirm: 'Confirmar',
};

export interface ContractCreateFormProps {
  prefilledServiceId?: string;
  initialType?: ContractTypeValue | null;
  initialMode?: CreationMode | null;
  onCreated: (draftId: string) => void;
  onCancel: () => void;
  /** Oculta o cartão de identidade (usado no drawer, onde o contexto do serviço está atrás). */
  hideIdentityCard?: boolean;
}

/**
 * Formulário de criação de contrato — renderiza sem PageContainer nem back-link.
 * Pode ser hospedado em página completa (via CreateContractPage) ou num drawer.
 */
export function ContractCreateForm({
  prefilledServiceId = '',
  initialType = null,
  initialMode = null,
  onCreated,
  onCancel,
  hideIdentityCard,
}: ContractCreateFormProps) {
  const { t } = useTranslation();
  const form = useContractDraftForm({ prefilledServiceId, initialType, initialMode, onCreated });

  const [activeTab, setActiveTab] = useState<FormTab>(prefilledServiceId ? 'typeMode' : 'service');
  const tabIndex = FORM_TABS.indexOf(activeTab);
  const goNext = () => {
    const n = FORM_TABS[Math.min(tabIndex + 1, FORM_TABS.length - 1)];
    if (n) setActiveTab(n);
  };
  const goPrev = () => {
    const p = FORM_TABS[Math.max(tabIndex - 1, 0)];
    if (p) setActiveTab(p);
  };

  const handleSelectService = (svc: { serviceId: string; serviceType: string }) => {
    form.setLinkedServiceId(svc.serviceId);
    form.setSelectedServiceType(svc.serviceType as ServiceType);
    const allowed = allowedContractTypes(svc.serviceType as ServiceType);
    if (!form.selectedType || !allowed.includes(form.selectedType)) {
      form.setSelectedType(null);
      form.setSelectedMode(null);
      form.setSelectedProtocol('');
    }
  };

  const formColumn = (
    <div className="min-w-0">
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-lg font-bold text-heading">
          {t('contracts.create.title', 'Novo contrato')}
        </h1>
        <div className="flex items-center gap-2">
          <Button
            variant="ghost"
            size="sm"
            icon={<X size={14} />}
            onClick={onCancel}
          >
            {t('common.cancel', 'Cancelar')}
          </Button>
          <Button
            variant="primary"
            size="sm"
            icon={<Check size={14} />}
            loading={form.createMutation.isPending}
            disabled={!form.canCreate}
            onClick={() => form.createMutation.mutate()}
          >
            {t('contracts.create.createDraft', 'Criar draft')}
          </Button>
        </div>
      </div>

      <div className="flex gap-0.5 border-b border-edge overflow-x-auto">
        {FORM_TABS.map((tab, idx) => (
          <button
            key={tab}
            type="button"
            onClick={() => setActiveTab(tab)}
            className={cn(
              'flex items-center gap-2 px-4 py-2.5 text-sm font-semibold whitespace-nowrap border-b-2 transition-colors',
              activeTab === tab
                ? 'text-accent border-accent'
                : 'text-muted border-transparent hover:text-heading',
            )}
          >
            <span
              className={cn(
                'w-5 h-5 rounded-full text-[11px] flex items-center justify-center font-bold',
                activeTab === tab ? 'bg-accent text-white' : 'bg-elevated text-muted',
              )}
            >
              {idx + 1}
            </span>
            {t(FORM_TAB_LABEL_KEY[tab], FORM_TAB_LABEL_DEFAULT[tab])}
          </button>
        ))}
      </div>

      <div className="bg-card border border-edge border-t-0 rounded-b-xl p-5">
        {activeTab === 'service' && (
          <ServiceTab
            filteredServices={form.filteredServices}
            linkedServiceId={form.linkedServiceId}
            serviceSearch={form.serviceSearch}
            isLoading={form.servicesQuery.isLoading}
            onSearchChange={form.setServiceSearch}
            onSelectService={handleSelectService}
          />
        )}
        {activeTab === 'typeMode' && (
          <TypeModeTab
            filteredContractTypes={form.filteredContractTypes}
            selectedType={form.selectedType}
            onSelectType={form.selectType}
            selectedMode={form.selectedMode}
            onSelectMode={form.setSelectedMode}
          />
        )}
        {activeTab === 'details' && <DetailsTab form={form} />}
        {activeTab === 'confirm' && (
          <ConfirmTab
            summary={form.summary}
            description={form.description}
            canCreate={form.canCreate}
            isCreating={form.createMutation.isPending}
            isError={form.createMutation.isError}
            onCreate={() => form.createMutation.mutate()}
          />
        )}

        <div className="flex justify-between pt-4 mt-4 border-t border-edge">
          <Button
            variant="ghost"
            size="sm"
            icon={<ArrowLeft size={14} />}
            onClick={goPrev}
            disabled={tabIndex === 0}
          >
            {t('common.back', 'Anterior')}
          </Button>
          {tabIndex < FORM_TABS.length - 1 && (
            <Button variant="primary" size="sm" onClick={goNext}>
              {t('common.next', 'Próximo')} <ArrowRight size={14} />
            </Button>
          )}
        </div>
      </div>
    </div>
  );

  if (hideIdentityCard) {
    return formColumn;
  }

  return (
    <div className="grid grid-cols-1 lg:grid-cols-[300px_minmax(0,1fr)] gap-6 items-start">
      <div className="lg:sticky lg:top-4">
        <ContractIdentityCard summary={form.summary} />
      </div>
      {formColumn}
    </div>
  );
}
