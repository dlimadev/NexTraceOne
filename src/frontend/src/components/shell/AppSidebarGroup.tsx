import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronDown, ChevronRight } from 'lucide-react';
import { cn } from '../../lib/cn';
import type { NavSection } from '../../auth/persona';

interface AppSidebarGroupProps {
  sectionKey: NavSection;
  labelKey: string;
  highlighted?: boolean;
  collapsed?: boolean;
  expanded?: boolean;
  hasMultipleItems?: boolean;
  onToggle?: () => void;
  children: ReactNode;
}

/**
 * AppSidebarGroup — grupo de navegação com label de seção colapsável.
 *
 * Highlighted: acento lateral cyan para seções prioritárias da persona.
 * Labels em overline style (uppercase, tracking-wide, 10px).
 */
export function AppSidebarGroup({
  sectionKey,
  labelKey,
  highlighted = false,
  collapsed = false,
  expanded = true,
  hasMultipleItems = true,
  onToggle,
  children,
}: AppSidebarGroupProps) {
  const { t } = useTranslation();

  if (collapsed) {
    return (
      <div className="mb-1" role="group" aria-label={labelKey ? t(labelKey) : sectionKey}>
        {children}
      </div>
    );
  }

  return (
    <div
      className={cn('mb-1', highlighted && 'pl-1 border-l-2 border-cyan/40')}
      role="group"
      aria-label={labelKey ? t(labelKey) : sectionKey}
    >
      {labelKey && (
        <button
          onClick={onToggle}
          className={cn(
            'w-full flex items-center justify-between px-2.5 pt-3 pb-1',
            'text-[10px] font-semibold uppercase tracking-[0.06em]',
            highlighted ? 'text-cyan/80' : 'text-faded',
            hasMultipleItems ? 'hover:text-muted cursor-pointer' : 'cursor-default',
          )}
          aria-expanded={expanded}
          type="button"
        >
          <span>{t(labelKey)}</span>
          {hasMultipleItems && (
            <span className="text-faded/60" aria-hidden="true">
              {expanded ? <ChevronDown size={11} /> : <ChevronRight size={11} />}
            </span>
          )}
        </button>
      )}
      {(expanded || !hasMultipleItems) && (
        <ul className="space-y-px" role="list">
          {children}
        </ul>
      )}
    </div>
  );
}
