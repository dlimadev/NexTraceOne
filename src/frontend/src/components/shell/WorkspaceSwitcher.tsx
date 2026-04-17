import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronDown, Building2, Check, AlertTriangle, ArrowRightLeft } from 'lucide-react';
import { cn } from '../../lib/cn';
import { useEnvironment } from '../../contexts/EnvironmentContext';
import { useAuth } from '../../contexts/AuthContext';
import { getProfileColor, getProfileIcon } from '../../lib/environmentProfile';

export function WorkspaceSwitcher() {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const { activeEnvironment, availableEnvironments, selectEnvironment } = useEnvironment();
  const { user, availableTenants, selectTenant } = useAuth();

  // Use real tenant name from current user profile
  const tenantName = user?.tenantName || user?.tenantId || t('shell.defaultWorkspace');

  // Show tenant switcher only when user has more than one tenant
  const hasMultipleTenants = availableTenants.length > 1;

  // Close on Escape key
  useEffect(() => {
    if (!open) return;
    const handleKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setOpen(false);
    };
    document.addEventListener('keydown', handleKey);
    return () => document.removeEventListener('keydown', handleKey);
  }, [open]);

  const activeColors = activeEnvironment ? getProfileColor(activeEnvironment.profile) : null;

  return (
    <div className="relative hidden md:block">
      <button
        onClick={() => setOpen(!open)}
        className={cn(
          'flex items-center gap-2 px-3 py-2 rounded-xl text-sm',
          'border border-edge hover:border-edge-strong',
          'transition-all duration-[var(--nto-motion-base)]',
          'text-body hover:text-heading',
          open && 'border-edge-strong bg-hover',
        )}
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-label={t('shell.workspaceSwitcher')}
      >
        <Building2 size={14} className="text-muted shrink-0" />
        <span className="truncate max-w-[120px] font-medium">{tenantName}</span>
        {activeEnvironment && (
          <span className="flex items-center gap-1.5">
            <span className={cn('w-2 h-2 rounded-full shrink-0 ring-2 ring-canvas', activeColors?.dot)} aria-hidden="true" />
            <span
              className={cn(
                'text-[10px] font-semibold px-2 py-0.5 rounded-md border hidden lg:inline',
                activeColors?.badge,
              )}
            >
              {activeEnvironment.name}
            </span>
          </span>
        )}
        {activeEnvironment && !activeEnvironment.isProductionLike && (
          <AlertTriangle size={12} className="text-warning shrink-0" aria-hidden="true" />
        )}
        <ChevronDown size={12} className={cn(
          'text-faded shrink-0 transition-transform duration-[var(--nto-motion-base)]',
          open && 'rotate-180',
        )} />
      </button>

      {open && (
        <>
          <div className="fixed inset-0 z-[var(--z-dropdown)]" onClick={() => setOpen(false)} aria-hidden="true" />
          <div
            className="absolute right-0 top-full mt-2 z-[calc(var(--z-dropdown)+1)] bg-elevated border border-edge rounded-xl shadow-floating py-2 min-w-[280px] animate-fade-in"
            role="listbox"
            aria-label={t('shell.workspaceSwitcher')}
          >
            {/* Workspace header */}
            <div className="px-4 pb-2 mb-2 border-b border-edge">
              <p className="type-overline text-faded">{t('shell.workspace')}</p>
            </div>

            {/* Current tenant */}
            <button
              className="w-full text-left px-4 py-2.5 text-sm text-body hover:bg-hover transition-colors flex items-center justify-between gap-2"
              role="option"
              aria-selected={true}
              onClick={() => setOpen(false)}
            >
              <div className="flex items-center gap-2.5 min-w-0">
                <div className="w-8 h-8 rounded-lg bg-accent/15 flex items-center justify-center shrink-0">
                  <Building2 size={14} className="text-cyan" />
                </div>
                <div className="min-w-0">
                  <div className="font-medium truncate">{tenantName}</div>
                  {user?.roleName && (
                    <div className="text-[10px] text-muted truncate">{user.roleName}</div>
                  )}
                </div>
              </div>
              <Check size={14} className="text-cyan shrink-0" />
            </button>

            {/* Switch tenant option — only when user has multiple tenants */}
            {hasMultipleTenants && (
              <div className="px-4 pt-2 border-t border-edge mt-1">
                <p className="type-overline text-faded mb-2">{t('tenants.switchTenant')}</p>
                <div className="flex flex-col gap-1">
                  {availableTenants
                    .filter((t) => t.id !== user?.tenantId && t.isActive)
                    .map((tenant) => (
                      <button
                        key={tenant.id}
                        role="option"
                        aria-selected={false}
                        onClick={() => {
                          void selectTenant(tenant.id);
                          setOpen(false);
                        }}
                        className={cn(
                          'flex items-center gap-2.5 w-full text-left text-sm px-3 py-2 rounded-lg border',
                          'border-edge hover:border-edge-strong hover:bg-hover transition-all cursor-pointer',
                        )}
                      >
                        <div className="w-7 h-7 rounded-md bg-elevated flex items-center justify-center shrink-0 font-bold text-xs text-muted border border-edge">
                          {tenant.name[0]?.toUpperCase() ?? 'T'}
                        </div>
                        <div className="flex-1 min-w-0">
                          <div className="font-medium truncate">{tenant.name}</div>
                          <div className="text-[10px] text-muted truncate">{tenant.roleName}</div>
                        </div>
                        <ArrowRightLeft size={12} className="text-faded shrink-0" />
                      </button>
                    ))}
                </div>
              </div>
            )}

            {/* Environment section */}
            <div className="px-4 pt-3 mt-2 border-t border-edge">
              <p className="type-overline text-faded mb-2">{t('environment.select')}</p>
              <div className="flex flex-col gap-1.5">
                {availableEnvironments.map(env => {
                  const isActive = activeEnvironment?.id === env.id;
                  const colors = getProfileColor(env.profile);
                  const ProfileIcon = getProfileIcon(env.profile);
                  const profileLabel = t(`environment.profile.${env.profile}`, env.profile);
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
                        'flex items-center gap-3 w-full text-left text-sm px-3 py-2.5 rounded-lg border',
                        'transition-all duration-[var(--nto-motion-base)]',
                        isActive
                          ? 'bg-accent/10 text-cyan border-cyan/30 shadow-sm'
                          : cn(
                              'border-edge hover:border-edge-strong hover:bg-hover cursor-pointer',
                            ),
                      )}
                    >
                      {/* Colored dot + icon */}
                      <div className={cn(
                        'w-8 h-8 rounded-lg flex items-center justify-center shrink-0',
                        isActive ? 'bg-accent/15' : colors.bg,
                      )}>
                        <ProfileIcon size={14} className={isActive ? 'text-cyan' : undefined} />
                      </div>

                      {/* Name + profile label */}
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2">
                          <span className={cn('w-2 h-2 rounded-full shrink-0', colors.dot)} aria-hidden="true" />
                          <span className="font-medium truncate">{env.name}</span>
                        </div>
                        <span className="text-[10px] text-muted ml-4 uppercase tracking-wider">{profileLabel}</span>
                      </div>

                      {/* Status indicators */}
                      <div className="flex items-center gap-1.5 shrink-0">
                        {!env.isProductionLike && (
                          <AlertTriangle size={11} className="text-warning" aria-hidden="true" />
                        )}
                        {isActive && <Check size={14} className="text-cyan" />}
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
