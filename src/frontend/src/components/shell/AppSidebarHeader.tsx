import { useTranslation } from 'react-i18next';
import { cn } from '../../lib/cn';
import { NexTraceLogo, NexTraceIcon } from '../NexTraceLogo';
import { useBranding } from '../../contexts/BrandingContext';
import { useTheme } from '../../contexts/ThemeContext';

interface AppSidebarHeaderProps {
  collapsed?: boolean;
}

/**
 * AppSidebarHeader — cabeçalho da sidebar com logo e wordmark oficial.
 *
 * Suporta logo dinâmico via branding.logo_url / branding.logo_dark_url.
 * Quando configurado, usa o logo do branding; caso contrário, usa o logo vectorial padrão.
 * Collapsed: apenas o ícone vetorial do globo (ou logo custom reduzido).
 * Expanded: ícone + wordmark "NexTraceOne" + tagline (ou logo custom completo).
 */
export function AppSidebarHeader({ collapsed = false }: AppSidebarHeaderProps) {
  const { t } = useTranslation();
  const { logoUrl, logoDarkUrl, instanceName } = useBranding();
  const { theme } = useTheme();

  // Select the appropriate logo URL based on theme
  const activeLogo = theme === 'dark' ? (logoDarkUrl || logoUrl) : logoUrl;

  return (
    <div className={cn(
      'shrink-0 border-b border-edge flex items-center',
      'transition-all duration-[var(--nto-motion-medium)]',
      collapsed
        ? 'justify-center px-2 py-4 h-20'
        : 'px-5 py-0 h-20',
    )}>
      {activeLogo ? (
        // Custom branding logo
        <div className={cn(
          'flex items-center',
          collapsed ? 'justify-center' : 'gap-2 min-w-0',
        )}>
          <img
            src={activeLogo}
            alt={instanceName || 'NexTraceOne'}
            className={cn(
              'object-contain',
              collapsed ? 'h-8 w-8' : 'h-8 max-w-[180px]',
            )}
            onError={(e) => {
              // Fallback to default logo on error
              (e.target as HTMLImageElement).style.display = 'none';
            }}
          />
          {!collapsed && instanceName && instanceName !== 'NexTraceOne' && (
            <div className="flex flex-col gap-0.5 min-w-0">
              <span className="text-sm font-bold text-heading truncate">
                {instanceName}
              </span>
              <p className="text-[10px] text-faded leading-tight truncate">
                {t('sidebar.tagline')}
              </p>
            </div>
          )}
        </div>
      ) : (
        // Default NexTraceOne logo
        collapsed ? (
          <NexTraceIcon size={32} />
        ) : (
          <div className="flex flex-col gap-0.5 min-w-0">
            <NexTraceLogo size={28} variant="compact" />
            <p className="text-[10px] text-faded leading-tight truncate pl-0.5">
              {t('sidebar.tagline')}
            </p>
          </div>
        )
      )}
    </div>
  );
}
