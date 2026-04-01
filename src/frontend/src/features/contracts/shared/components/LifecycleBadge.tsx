import { useTranslation } from 'react-i18next';
import { LIFECYCLE_COLORS } from '../constants';
import type { ContractLifecycleState } from '../../types';

interface LifecycleBadgeProps {
  state: ContractLifecycleState | string;
  size?: 'sm' | 'md';
  className?: string;
}

/**
 * Badge visual para estado do ciclo de vida de contrato.
 * Utiliza cores centralizadas e labels i18n.
 */
export function LifecycleBadge({ state, size = 'sm', className = '' }: LifecycleBadgeProps) {
  const { t } = useTranslation();
  const colors = LIFECYCLE_COLORS[state] ?? 'bg-elevated text-muted border border-edge';
  const sizeClass = size === 'sm' ? 'px-2 py-0.5 text-[10px]' : 'px-2.5 py-1 text-xs';

  return (
    <span className={`inline-flex items-center rounded-full font-medium whitespace-nowrap ${colors} ${sizeClass} ${className}`}>
      {t(`contracts.lifecycleStates.${state}`, state)}
    </span>
  );
}
