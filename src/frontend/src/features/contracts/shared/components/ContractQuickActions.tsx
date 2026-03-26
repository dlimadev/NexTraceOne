import { useTranslation } from 'react-i18next';
import {
  Send,
  Check,
  Lock,
  AlertTriangle,
  Sunset,
  Archive,
  Undo2,
  FileSignature,
  Download,
  GitCompare,
  Copy,
  Plus,
  Globe,
  EyeOff,
} from 'lucide-react';
import { LIFECYCLE_TRANSITIONS } from '../constants';
import type { ContractLifecycleState } from '../../types';

interface ContractQuickActionsProps {
  lifecycleState: ContractLifecycleState;
  isLocked?: boolean;
  isSigned?: boolean;
  isPublishedToPortal?: boolean;
  onTransition?: (targetState: ContractLifecycleState) => void;
  onSign?: () => void;
  onExport?: () => void;
  onDiff?: () => void;
  onClone?: () => void;
  onNewVersion?: () => void;
  onPublishToPortal?: () => void;
  onWithdrawFromPortal?: () => void;
  className?: string;
}

const TRANSITION_ICONS: Record<string, React.ComponentType<{ size?: number; className?: string }>> = {
  InReview: Send,
  Approved: Check,
  Locked: Lock,
  Deprecated: AlertTriangle,
  Sunset: Sunset,
  Retired: Archive,
  Draft: Undo2,
};

/**
 * Painel de acções rápidas para contrato — transições de lifecycle, assinatura, export, diff.
 */
export function ContractQuickActions({
  lifecycleState,
  isSigned,
  isPublishedToPortal = false,
  onTransition,
  onSign,
  onExport,
  onDiff,
  onClone,
  onNewVersion,
  onPublishToPortal,
  onWithdrawFromPortal,
  className = '',
}: ContractQuickActionsProps) {
  const { t } = useTranslation();
  const transitions = LIFECYCLE_TRANSITIONS[lifecycleState] ?? [];

  return (
    <div className={`flex items-center gap-1.5 flex-wrap ${className}`}>
      {/* Lifecycle transitions */}
      {transitions.map(({ state, actionKey }) => {
        const Icon = TRANSITION_ICONS[state] ?? Send;
        return (
          <button
            key={state}
            onClick={() => onTransition?.(state)}
            className="inline-flex items-center gap-1.5 px-2.5 py-1.5 text-xs font-medium rounded-md bg-accent/10 text-accent hover:bg-accent/20 transition-colors"
          >
            <Icon size={12} />
            {t(actionKey)}
          </button>
        );
      })}

      {/* Sign */}
      {!isSigned && lifecycleState === 'Locked' && onSign && (
        <button
          onClick={onSign}
          className="inline-flex items-center gap-1.5 px-2.5 py-1.5 text-xs font-medium rounded-md bg-mint/10 text-mint hover:bg-mint/20 transition-colors"
        >
          <FileSignature size={12} />
          {t('contracts.sign', 'Sign')}
        </button>
      )}

      {/* Diff */}
      {onDiff && (
        <button
          onClick={onDiff}
          className="inline-flex items-center gap-1.5 px-2.5 py-1.5 text-xs font-medium rounded-md bg-elevated text-muted hover:text-heading transition-colors"
        >
          <GitCompare size={12} />
          {t('contracts.diff.title', 'Diff')}
        </button>
      )}

      {/* Export */}
      {onExport && (
        <button
          onClick={onExport}
          className="inline-flex items-center gap-1.5 px-2.5 py-1.5 text-xs font-medium rounded-md bg-elevated text-muted hover:text-heading transition-colors"
        >
          <Download size={12} />
          {t('contracts.export', 'Export')}
        </button>
      )}

      {/* Clone */}
      {onClone && (
        <button
          onClick={onClone}
          className="inline-flex items-center gap-1.5 px-2.5 py-1.5 text-xs font-medium rounded-md bg-elevated text-muted hover:text-heading transition-colors"
        >
          <Copy size={12} />
          {t('contracts.clone', 'Clone')}
        </button>
      )}

      {/* New version */}
      {onNewVersion && (
        <button
          onClick={onNewVersion}
          className="inline-flex items-center gap-1.5 px-2.5 py-1.5 text-xs font-medium rounded-md bg-elevated text-muted hover:text-heading transition-colors"
        >
          <Plus size={12} />
          {t('contracts.newVersion', 'New Version')}
        </button>
      )}

      {/* Publish to Developer Portal — disponível para Approved e Locked */}
      {!isPublishedToPortal
        && (lifecycleState === 'Approved' || lifecycleState === 'Locked')
        && onPublishToPortal && (
        <button
          onClick={onPublishToPortal}
          className="inline-flex items-center gap-1.5 px-2.5 py-1.5 text-xs font-medium rounded-md bg-green/10 text-green hover:bg-green/20 transition-colors"
        >
          <Globe size={12} />
          {t('contracts.publishToPortal', 'Publish to Portal')}
        </button>
      )}

      {/* Withdraw from Developer Portal — disponível para contratos publicados */}
      {isPublishedToPortal && onWithdrawFromPortal && (
        <button
          onClick={onWithdrawFromPortal}
          className="inline-flex items-center gap-1.5 px-2.5 py-1.5 text-xs font-medium rounded-md bg-elevated text-muted hover:text-heading transition-colors"
        >
          <EyeOff size={12} />
          {t('contracts.withdrawFromPortal', 'Withdraw from Portal')}
        </button>
      )}
    </div>
  );
}
