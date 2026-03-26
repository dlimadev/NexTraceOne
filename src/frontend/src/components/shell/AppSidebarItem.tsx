import { NavLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { cn } from '../../lib/cn';
import type { ReactNode } from 'react';

interface AppSidebarItemProps {
  to: string;
  icon: ReactNode;
  labelKey: string;
  collapsed?: boolean;
  preview?: boolean;
  /** Alert counter shown as a badge on the item. 0 hides the badge. */
  counter?: number;
}

export function AppSidebarItem({ to, icon, labelKey, collapsed = false, preview = false, counter = 0 }: AppSidebarItemProps) {
  const { t } = useTranslation();

  if (collapsed) {
    return (
      <NavLink
        to={to}
        end={to === '/'}
        title={preview ? `${t(labelKey)} (${t('preview.badge', 'Preview')})` : t(labelKey)}
        className={({ isActive }) =>
          cn(
            'relative flex items-center justify-center w-10 h-10 mx-auto rounded-md mb-0.5',
            'transition-all duration-[var(--nto-motion-base)]',
            isActive
              ? 'bg-accent/10 text-cyan shadow-glow-sm'
              : preview
                ? 'text-muted/60 hover:bg-hover hover:text-body'
                : 'text-muted hover:bg-hover hover:text-body',
          )
        }
      >
        {icon}
        {preview && (
          <span className="absolute -top-0.5 -right-0.5 w-2 h-2 rounded-full bg-amber-400" />
        )}
        {!preview && counter > 0 && (
          <span className="absolute -top-1 -right-1 min-w-[16px] h-4 px-0.5 rounded-full bg-critical text-[9px] font-bold text-white flex items-center justify-center leading-none">
            {counter > 99 ? '99+' : counter}
          </span>
        )}
      </NavLink>
    );
  }

  return (
    <li>
      <NavLink
        to={to}
        end={to === '/'}
        className={({ isActive }) =>
          cn(
            'flex items-center gap-3 px-3 py-2 rounded-md text-sm',
            'transition-all duration-[var(--nto-motion-base)]',
            isActive
              ? 'bg-accent/10 text-cyan font-medium border-l-2 border-cyan -ml-0.5 pl-[10px]'
              : preview
                ? 'text-muted/60 hover:bg-hover hover:text-body'
                : 'text-muted hover:bg-hover hover:text-body',
          )
        }
      >
        {icon}
        <span className="truncate">{t(labelKey)}</span>
        {preview && (
          <span className="ml-auto shrink-0 rounded px-1.5 py-0.5 text-[9px] font-semibold uppercase leading-none bg-amber-500/15 text-amber-400 border border-amber-500/25">
            {t('preview.badge', 'Preview')}
          </span>
        )}
        {!preview && counter > 0 && (
          <span className="ml-auto shrink-0 min-w-[18px] h-[18px] px-1 rounded-full bg-critical text-[9px] font-bold text-white flex items-center justify-center leading-none">
            {counter > 99 ? '99+' : counter}
          </span>
        )}
      </NavLink>
    </li>
  );
}
