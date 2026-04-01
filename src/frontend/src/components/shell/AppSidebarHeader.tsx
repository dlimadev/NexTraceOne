import { useTranslation } from 'react-i18next';
import { cn } from '../../lib/cn';
import { NexTraceLogo, NexTraceIcon } from '../NexTraceLogo';

interface AppSidebarHeaderProps {
  collapsed?: boolean;
}

/**
 * AppSidebarHeader — cabeçalho da sidebar com logo e wordmark oficial.
 *
 * Collapsed: apenas o ícone vetorial do globo.
 * Expanded: ícone + wordmark "NexTraceOne" + tagline.
 */
export function AppSidebarHeader({ collapsed = false }: AppSidebarHeaderProps) {
  const { t } = useTranslation();

  return (
    <div className={cn(
      'shrink-0 border-b border-edge flex items-center',
      'transition-all duration-[var(--nto-motion-medium)]',
      collapsed
        ? 'justify-center px-2 py-4 h-20'
        : 'px-5 py-0 h-20',
    )}>
      {collapsed ? (
        <NexTraceIcon size={32} />
      ) : (
        <div className="flex flex-col gap-0.5 min-w-0">
          <NexTraceLogo size={28} variant="compact" />
          <p className="text-[10px] text-faded leading-tight truncate pl-0.5">
            {t('sidebar.tagline')}
          </p>
        </div>
      )}
    </div>
  );
}
