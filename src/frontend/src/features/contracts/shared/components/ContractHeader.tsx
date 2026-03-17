import { useTranslation } from 'react-i18next';
import { ArrowLeft, ChevronRight, Lock, FileSignature } from 'lucide-react';
import { Link } from 'react-router-dom';
import { cn } from '../../../../lib/cn';
import { ProtocolBadge } from './ProtocolBadge';
import { LifecycleBadge } from './LifecycleBadge';
import { ServiceTypeBadge } from './ServiceTypeBadge';
import type { ContractProtocol, ContractLifecycleState, ContractType } from '../../types';

interface ContractHeaderProps {
  title: string;
  friendlyName?: string;
  version?: string;
  protocol?: ContractProtocol | string;
  lifecycleState?: ContractLifecycleState | string;
  contractType?: ContractType | string;
  serviceType?: string;
  domain?: string;
  owner?: string;
  isLocked?: boolean;
  isSigned?: boolean;
  backTo?: string;
  actions?: React.ReactNode;
  className?: string;
}

/**
 * Studio header forte para o workspace de contrato.
 * Mostra breadcrumbs, nome amigável + técnico, badges de contexto,
 * estado de lifecycle, indicadores de lock/sign e acções rápidas.
 */
export function ContractHeader({
  title,
  friendlyName,
  version,
  protocol,
  lifecycleState,
  contractType,
  serviceType,
  domain,
  owner,
  isLocked,
  isSigned,
  backTo = '/contracts',
  actions,
  className = '',
}: ContractHeaderProps) {
  const { t } = useTranslation();

  return (
    <header className={cn('px-6 py-4 border-b border-edge bg-card', className)}>
      {/* Breadcrumbs */}
      <nav className="flex items-center gap-1.5 text-[10px] text-muted mb-2">
        <Link to="/contracts" className="hover:text-heading transition-colors">
          {t('contracts.title', 'Contracts')}
        </Link>
        <ChevronRight size={10} />
        <Link to={backTo} className="hover:text-heading transition-colors">
          {t('contracts.catalog.title', 'Catalog')}
        </Link>
        <ChevronRight size={10} />
        <span className="text-body font-medium truncate max-w-[200px]">{friendlyName ?? title}</span>
      </nav>

      <div className="flex items-start justify-between gap-4">
        <div className="flex items-start gap-3 min-w-0">
          <Link
            to={backTo}
            className="flex-shrink-0 mt-1 w-8 h-8 rounded-md bg-elevated border border-edge flex items-center justify-center text-muted hover:text-heading hover:border-accent/40 transition-colors"
          >
            <ArrowLeft size={16} />
          </Link>

          <div className="min-w-0">
            <div className="flex items-center gap-2.5 mb-1">
              <h1 className="text-lg font-bold text-heading truncate">
                {friendlyName ?? title}
              </h1>
              {version && (
                <span className="text-xs font-mono text-accent bg-accent/10 px-2 py-0.5 rounded-md border border-accent/20">
                  v{version}
                </span>
              )}
              {isLocked && (
                <span className="inline-flex items-center gap-1 px-2 py-0.5 text-[10px] rounded-md bg-accent/10 text-accent border border-accent/20 font-medium">
                  <Lock size={10} />
                  {t('contracts.locked', 'Locked')}
                </span>
              )}
              {isSigned && (
                <span className="inline-flex items-center gap-1 px-2 py-0.5 text-[10px] rounded-md bg-mint/10 text-mint border border-mint/20 font-medium">
                  <FileSignature size={10} />
                  {t('contracts.signed', 'Signed')}
                </span>
              )}
            </div>

            {title !== friendlyName && friendlyName && (
              <p className="text-[11px] font-mono text-muted/70 mb-1.5">{title}</p>
            )}

            <div className="flex items-center gap-1.5 flex-wrap">
              {serviceType && <ServiceTypeBadge type={serviceType} />}
              {contractType && !serviceType && <ServiceTypeBadge type={contractType} />}
              {protocol && <ProtocolBadge protocol={protocol} />}
              {lifecycleState && <LifecycleBadge state={lifecycleState} />}
              {domain && (
                <span className="px-2 py-0.5 text-[10px] rounded-md bg-elevated border border-edge text-muted font-medium">
                  {domain}
                </span>
              )}
              {owner && (
                <span className="px-2 py-0.5 text-[10px] rounded-md bg-elevated border border-edge text-muted font-medium">
                  @{owner}
                </span>
              )}
            </div>
          </div>
        </div>

        {actions && <div className="flex items-center gap-2 flex-shrink-0">{actions}</div>}
      </div>
    </header>
  );
}
