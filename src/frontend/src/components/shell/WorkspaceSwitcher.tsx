import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronDown, Building2, Check, AlertTriangle } from 'lucide-react';
import { cn } from '../../lib/cn';
import { useEnvironment, type EnvironmentProfile } from '../../contexts/EnvironmentContext';

function getProfileBadgeClass(profile: EnvironmentProfile): string {
  switch (profile) {
    case 'production': return 'text-red-400 border-red-400/30 bg-red-400/10';
    case 'staging': return 'text-orange-400 border-orange-400/30 bg-orange-400/10';
    case 'uat': return 'text-yellow-400 border-yellow-400/30 bg-yellow-400/10';
    case 'qa': return 'text-blue-400 border-blue-400/30 bg-blue-400/10';
    case 'development': return 'text-green-400 border-green-400/30 bg-green-400/10';
    case 'sandbox': return 'text-purple-400 border-purple-400/30 bg-purple-400/10';
    default: return 'text-faded border-edge';
  }
}

export function WorkspaceSwitcher() {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const { activeEnvironment, availableEnvironments, selectEnvironment } = useEnvironment();

  const tenantName = t('shell.defaultWorkspace');

  return (
    <div className="relative hidden md:block">
      <button
        onClick={() => setOpen(!open)}
        className={cn(
          'flex items-center gap-2 px-2.5 py-1.5 rounded-md text-sm',
          'border border-edge hover:border-edge-strong',
          'transition-all duration-[var(--nto-motion-base)]',
          'text-body hover:text-heading',
        )}
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-label={t('shell.workspaceSwitcher')}
      >
        <Building2 size={14} className="text-muted shrink-0" />
        <span className="truncate max-w-[120px]">{tenantName}</span>
        {activeEnvironment && (
          <span
            className={cn(
              'text-[10px] px-1.5 py-0.5 rounded border hidden lg:inline',
              getProfileBadgeClass(activeEnvironment.profile),
            )}
          >
            {activeEnvironment.name}
          </span>
        )}
        {activeEnvironment && !activeEnvironment.isProductionLike && (
          <AlertTriangle size={12} className="text-yellow-400 shrink-0" aria-hidden="true" />
        )}
        <ChevronDown size={12} className="text-faded shrink-0" />
      </button>

      {open && (
        <>
          <div className="fixed inset-0 z-[var(--z-dropdown)]" onClick={() => setOpen(false)} aria-hidden="true" />
          <div
            className="absolute right-0 top-full mt-1.5 z-[calc(var(--z-dropdown)+1)] bg-elevated border border-edge rounded-lg shadow-floating py-2 min-w-[220px] animate-fade-in"
            role="listbox"
            aria-label={t('shell.workspaceSwitcher')}
          >
            <div className="px-3 pb-2 mb-1 border-b border-edge">
              <p className="type-overline text-faded">{t('shell.workspace')}</p>
            </div>
            <button
              className="w-full text-left px-3 py-2 text-sm text-body hover:bg-hover transition-colors flex items-center justify-between gap-2"
              role="option"
              aria-selected={true}
              onClick={() => setOpen(false)}
            >
              <div className="flex items-center gap-2 min-w-0">
                <Building2 size={14} className="text-cyan shrink-0" />
                <span className="truncate">{tenantName}</span>
              </div>
              <Check size={14} className="text-cyan shrink-0" />
            </button>

            <div className="px-3 pt-2 mt-1 border-t border-edge">
              <p className="type-overline text-faded mb-1.5">{t('environment.select')}</p>
              <div className="flex flex-col gap-1">
                {availableEnvironments.map(env => {
                  const isActive = activeEnvironment?.id === env.id;
                  return (
                    <button
                      key={env.id}
                      role="option"
                      aria-selected={isActive}
                      onClick={() => {
                        selectEnvironment(env.id);
                        setOpen(false);
                      }}
                      className={cn(
                        'flex items-center justify-between w-full text-left text-[11px] px-2 py-1.5 rounded border',
                        isActive
                          ? cn('bg-accent/10 text-cyan border-cyan/30')
                          : cn(
                              getProfileBadgeClass(env.profile),
                              'hover:border-edge-strong cursor-pointer',
                            ),
                        'transition-all duration-[var(--nto-motion-base)]',
                      )}
                    >
                      <span>{env.name}</span>
                      <div className="flex items-center gap-1">
                        {!env.isProductionLike && (
                          <AlertTriangle size={10} className="text-yellow-400" aria-hidden="true" />
                        )}
                        {isActive && <Check size={11} className="text-cyan" />}
                      </div>
                    </button>
                  );
                })}
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
