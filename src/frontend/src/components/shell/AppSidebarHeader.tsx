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
      'py-3.5 border-b border-edge flex items-center shrink-0',
      collapsed ? 'justify-center px-3' : 'px-4',
    )}>
      {collapsed ? (
        <NexTraceIcon size={30} />
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
