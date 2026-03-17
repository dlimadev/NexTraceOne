import { NavLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { cn } from '../../lib/cn';
import type { ReactNode } from 'react';

interface AppSidebarItemProps {
  to: string;
  icon: ReactNode;
  labelKey: string;
  collapsed?: boolean;
}

export function AppSidebarItem({ to, icon, labelKey, collapsed = false }: AppSidebarItemProps) {
  const { t } = useTranslation();

  if (collapsed) {
    return (
      <NavLink
        to={to}
        end={to === '/'}
        title={t(labelKey)}
        className={({ isActive }) =>
          cn(
            'flex items-center justify-center w-10 h-10 mx-auto rounded-md mb-0.5',
            'transition-all duration-[var(--nto-motion-base)]',
            isActive
              ? 'bg-accent/10 text-cyan shadow-glow-sm'
              : 'text-muted hover:bg-hover hover:text-body',
          )
        }
      >
        {icon}
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
              : 'text-muted hover:bg-hover hover:text-body',
          )
        }
      >
        {icon}
        <span className="truncate">{t(labelKey)}</span>
      </NavLink>
    </li>
  );
}
