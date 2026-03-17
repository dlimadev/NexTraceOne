import { useTranslation } from 'react-i18next';
import { PROTOCOL_COLORS } from '../constants';
import type { ContractProtocol } from '../../types';

interface ProtocolBadgeProps {
  protocol: ContractProtocol | string;
  size?: 'sm' | 'md';
  className?: string;
}

/**
 * Badge visual para protocolo de contrato.
 * Utiliza cores centralizadas e labels i18n.
 */
export function ProtocolBadge({ protocol, size = 'sm', className = '' }: ProtocolBadgeProps) {
  const { t } = useTranslation();
  const colors = PROTOCOL_COLORS[protocol] ?? 'bg-slate-800/40 text-slate-300 border border-slate-700/50';
  const sizeClass = size === 'sm' ? 'px-2 py-0.5 text-[10px]' : 'px-2.5 py-1 text-xs';

  return (
    <span className={`inline-flex items-center rounded-full font-medium whitespace-nowrap ${colors} ${sizeClass} ${className}`}>
      {t(`contracts.protocols.${protocol}`, protocol)}
    </span>
  );
}
