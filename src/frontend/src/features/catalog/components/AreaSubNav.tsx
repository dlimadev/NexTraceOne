import { NavLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { cn } from '../../../lib/cn';

/** Item de uma sub-nav de área do catálogo. */
export interface AreaSubNavItem {
  labelKey: string;
  to: string;
  /** Só ativo no path exato (usar no separador de lista/raiz). */
  end?: boolean;
}

interface AreaSubNavProps {
  items: AreaSubNavItem[];
  ariaLabelKey: string;
}

/** Barra de separadores persistente no topo de uma área (services/contracts). */
export function AreaSubNav({ items, ariaLabelKey }: AreaSubNavProps) {
  const { t } = useTranslation();
  return (
    <nav
      aria-label={t(ariaLabelKey)}
      className="flex items-center gap-1 overflow-x-auto border-b border-edge px-6"
    >
      {items.map(item => (
        <NavLink
          key={item.to}
          to={item.to}
          end={item.end}
          className={({ isActive }) =>
            cn(
              'shrink-0 border-b-2 px-3 py-2.5 text-sm transition-colors duration-150',
              isActive
                ? 'border-accent text-accent font-medium'
                : 'border-transparent text-body hover:text-heading',
            )
          }
        >
          {({ isActive }) => (
            <span data-active={isActive ? 'true' : 'false'}>{t(item.labelKey)}</span>
          )}
        </NavLink>
      ))}
    </nav>
  );
}
