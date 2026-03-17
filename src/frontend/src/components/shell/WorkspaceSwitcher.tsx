import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronDown, Building2, Check } from 'lucide-react';
import { cn } from '../../lib/cn';
import { useAuth } from '../../contexts/AuthContext';

/** Default environment until multi-environment switching is implemented. */
const DEFAULT_ENVIRONMENT = 'Production';

const AVAILABLE_ENVIRONMENTS = ['Production', 'Staging', 'Development'] as const;

export function WorkspaceSwitcher() {
  const { t } = useTranslation();
  const { user } = useAuth();
  const [open, setOpen] = useState(false);

  const tenantName = t('shell.defaultWorkspace');
  // TODO: source from environment context once multi-env switching is implemented
  const environment = DEFAULT_ENVIRONMENT;

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
        <span className="text-[10px] text-faded border border-edge rounded px-1 py-0.5 hidden lg:inline">
          {environment}
        </span>
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
              <p className="type-overline text-faded mb-1.5">{t('shell.environment')}</p>
              <div className="flex gap-1">
                {AVAILABLE_ENVIRONMENTS.map(env => (
                  <span
                    key={env}
                    className={cn(
                      'text-[10px] px-2 py-1 rounded border',
                      env === environment
                        ? 'bg-accent/10 text-cyan border-cyan/30'
                        : 'text-faded border-edge hover:text-muted hover:border-edge-strong cursor-pointer',
                      'transition-all duration-[var(--nto-motion-base)]',
                    )}
                  >
                    {env}
                  </span>
                ))}
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
