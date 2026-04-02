import { useTranslation } from 'react-i18next';
import { Globe, Server, Zap, Cog, Database, FileCode, MessageSquare, AlignJustify, Terminal, Webhook } from 'lucide-react';
import { SERVICE_TYPE_COLORS } from '../constants';
import type { ContractType } from '../../types';

const ICONS: Record<string, React.ComponentType<{ size?: number; className?: string }>> = {
  RestApi: Globe,
  Soap: Server,
  Event: Zap,
  BackgroundService: Cog,
  SharedSchema: Database,
  Copybook: FileCode,
  MqMessage: MessageSquare,
  FixedLayout: AlignJustify,
  CicsCommarea: Terminal,
  Webhook: Webhook,
};

interface ServiceTypeBadgeProps {
  type: ContractType | string;
  size?: 'sm' | 'md';
  showIcon?: boolean;
  className?: string;
}

/**
 * Badge visual para tipo de serviço.
 * Mostra ícone + label com cores centralizadas e i18n.
 */
export function ServiceTypeBadge({ type, size = 'sm', showIcon = true, className = '' }: ServiceTypeBadgeProps) {
  const { t } = useTranslation();
  const colors = SERVICE_TYPE_COLORS[type] ?? 'bg-elevated text-muted border border-edge';
  const sizeClass = size === 'sm' ? 'px-2 py-0.5 text-[10px]' : 'px-2.5 py-1 text-xs';
  const Icon = ICONS[type] ?? Server;
  const iconSize = size === 'sm' ? 10 : 12;
  const translatedType = t(`contracts.serviceTypes.${type}`, type);

  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full font-medium whitespace-nowrap ${colors} ${sizeClass} ${className}`}
      title={translatedType}
      aria-label={translatedType}
    >
      {showIcon && <Icon size={iconSize} />}
      {translatedType}
    </span>
  );
}
