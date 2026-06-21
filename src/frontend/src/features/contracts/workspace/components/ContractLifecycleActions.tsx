import type React from 'react';
import { useTranslation } from 'react-i18next';
import { Download, Send, Check, Lock, AlertTriangle, Sunset, Archive, Undo2 } from 'lucide-react';
import { Button } from '../../../../shared/ui';
import { LIFECYCLE_TRANSITIONS } from '../../shared/constants';
import type { ContractLifecycleState } from '../../types';

const TRANSITION_ICONS: Record<string, React.ComponentType<{ size?: number; className?: string }>> = {
  InReview: Send,
  Approved: Check,
  Locked: Lock,
  Deprecated: AlertTriangle,
  Sunset,
  Retired: Archive,
  Draft: Undo2,
};

interface ContractLifecycleActionsProps {
  lifecycleState: ContractLifecycleState;
  isLocked: boolean;
  onTransition: (state: ContractLifecycleState) => void;
  onExport: () => void;
}

/** Acções de lifecycle + export do workspace, em DS Button (para o PageHeader). */
export function ContractLifecycleActions({ lifecycleState, isLocked, onTransition, onExport }: ContractLifecycleActionsProps) {
  const { t } = useTranslation();
  const transitions = isLocked ? [] : (LIFECYCLE_TRANSITIONS[lifecycleState] ?? []);
  return (
    <div className="flex items-center gap-2 flex-wrap">
      <Button variant="outline" size="sm" icon={<Download size={14} />} onClick={onExport}>
        {t('contracts.export', 'Export')}
      </Button>
      {transitions.map(({ state, actionKey }) => {
        const Icon = TRANSITION_ICONS[state] ?? Send;
        return (
          <Button key={state} variant="primary" size="sm" icon={<Icon size={14} />} onClick={() => onTransition(state)}>
            {t(actionKey, state)}
          </Button>
        );
      })}
    </div>
  );
}
