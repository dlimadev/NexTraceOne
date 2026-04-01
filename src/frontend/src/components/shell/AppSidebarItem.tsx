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

/**
 * AppSidebarItem — item de navegação da sidebar.
 *
 * Estado ativo (expandido): fundo azul sólido, ícone + texto brancos. Padrão Template NexLink.
 * Estado ativo (collapsed): fundo accent translúcido + ring cyan.
 * Estado hover: fundo elevado sutil, texto body.
 * Preview: texto atenuado, badge amber.
 */
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
            'relative flex items-center justify-center w-11 h-10 mx-auto rounded-lg mb-0.5',
            'transition-all duration-[var(--nto-motion-base)]',
            isActive
              ? 'bg-blue/20 text-cyan shadow-[inset_0_0_0_1px_rgba(18,196,232,0.25)]'
              : preview
                ? 'text-muted/50 hover:bg-hover hover:text-muted'
                : 'text-muted hover:bg-hover hover:text-body',
          )
        }
      >
        {icon}
        {preview && (
          <span className="absolute -top-0.5 -right-0.5 w-1.5 h-1.5 rounded-full bg-warning" />
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
            'flex items-center gap-2.5 px-3 py-2 rounded-lg text-sm',
            'transition-all duration-[var(--nto-motion-fast)]',
            isActive
              ? 'bg-blue text-white font-medium shadow-sm'
              : preview
                ? 'text-muted/50 hover:bg-hover hover:text-muted'
                : 'text-muted hover:bg-hover hover:text-body font-normal',
          )
        }
      >
        <span className="shrink-0">{icon}</span>
        <span className="truncate flex-1">{t(labelKey)}</span>
        {preview && (
          <span className="ml-auto shrink-0 rounded px-1.5 py-0.5 text-[9px] font-semibold uppercase leading-none bg-warning/15 text-warning border border-warning/25">
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
