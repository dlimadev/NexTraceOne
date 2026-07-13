import { useTranslation } from 'react-i18next';
import { Drawer } from '../../../components/Drawer';
import { ContractViewPanel } from '../../contracts/components/ContractViewPanel';
import { ContractCreateForm } from '../../contracts/create/ContractCreateForm';
import { ContractDraftEditor } from '../../contracts/studio/components/ContractDraftEditor';

export type ContractDrawerState =
  | { mode: 'view'; contractVersionId: string }
  | { mode: 'create' }
  | { mode: 'edit'; draftId: string }
  | { mode: 'closed' };

interface ServiceContractDrawerProps {
  state: ContractDrawerState;
  onClose: () => void;
  onModeChange: (next: ContractDrawerState) => void;
  serviceId: string;
}

/** Drawer in-place para ver/criar/editar contratos a partir do detalhe do serviço. */
export function ServiceContractDrawer({ state, onClose, onModeChange, serviceId }: ServiceContractDrawerProps) {
  const { t } = useTranslation();
  const open = state.mode !== 'closed';

  const title =
    state.mode === 'view' ? t('serviceContractDrawer.viewTitle', 'Ver contrato')
    : state.mode === 'create' ? t('serviceContractDrawer.createTitle', 'Novo contrato')
    : state.mode === 'edit' ? t('serviceContractDrawer.editTitle', 'Editar rascunho')
    : '';

  return (
    <Drawer open={open} onClose={onClose} title={title} size="xl">
      {state.mode === 'view' && <ContractViewPanel contractVersionId={state.contractVersionId} />}
      {state.mode === 'create' && (
        <ContractCreateForm
          prefilledServiceId={serviceId}
          onCreated={(draftId) => onModeChange({ mode: 'edit', draftId })}
          onCancel={onClose}
          hideIdentityCard
        />
      )}
      {state.mode === 'edit' && <ContractDraftEditor draftId={state.draftId} />}
    </Drawer>
  );
}
