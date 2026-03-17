import { useTranslation } from 'react-i18next';
import { cn } from '../../lib/cn';

interface AppSidebarHeaderProps {
  collapsed?: boolean;
}

export function AppSidebarHeader({ collapsed = false }: AppSidebarHeaderProps) {
  const { t } = useTranslation();

  return (
    <div className={cn(
      'py-4 border-b border-edge flex items-center shrink-0',
      collapsed ? 'justify-center px-3' : 'gap-3 px-5',
    )}>
      <div className="w-9 h-9 rounded-lg bg-accent/12 flex items-center justify-center shrink-0 shadow-glow-sm">
        <span className="text-cyan font-bold text-base" aria-hidden="true">N</span>
      </div>
      {!collapsed && (
        <div className="flex-1 min-w-0">
          <span className="font-semibold text-sm text-heading tracking-tight">NexTraceOne</span>
          <p className="text-[10px] text-muted leading-tight truncate">{t('sidebar.tagline')}</p>
        </div>
      )}
    </div>
  );
}
