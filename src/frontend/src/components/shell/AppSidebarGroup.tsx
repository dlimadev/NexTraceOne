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
      className={cn('mb-1.5', highlighted && 'pl-0.5 border-l-2 border-cyan/30')}
      role="group"
      aria-label={labelKey ? t(labelKey) : sectionKey}
    >
      {labelKey && (
        <button
          onClick={onToggle}
          className={cn(
            'w-full flex items-center justify-between px-3 py-1.5 text-[11px] font-semibold uppercase tracking-wider',
            highlighted ? 'text-cyan' : 'text-faded',
            hasMultipleItems ? 'hover:text-muted cursor-pointer' : 'cursor-default',
          )}
          aria-expanded={expanded}
          type="button"
        >
          <span>{t(labelKey)}</span>
          {hasMultipleItems && (
            <span className="text-faded" aria-hidden="true">
              {expanded ? <ChevronDown size={12} /> : <ChevronRight size={12} />}
            </span>
          )}
        </button>
      )}
      {(expanded || !hasMultipleItems) && (
        <ul className="space-y-0.5" role="list">
          {children}
        </ul>
      )}
    </div>
  );
}
