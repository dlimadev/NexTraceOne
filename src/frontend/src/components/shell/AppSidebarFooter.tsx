import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { Settings, HelpCircle, Search, BookOpen } from 'lucide-react';
import { cn } from '../../lib/cn';
import { useEnvironment } from '../../contexts/EnvironmentContext';

interface AppSidebarFooterProps {
  collapsed?: boolean;
}

/**
 * AppSidebarFooter — secção inferior da sidebar com ações rápidas contextuais.
 *
 * Responsabilidades:
 * - Indicador compacto do ambiente ativo (com cor semântica)
 * - Atalho para pesquisa global (Ctrl+K)
 * - Atalho para documentação/knowledge hub
 * - Atalho para configurações da plataforma
 *
 * Nota: as informações do utilizador (nome, role, logout) estão no AppUserMenu
 * na barra de topo (AppTopbar), evitando duplicação.
 */
export function AppSidebarFooter({ collapsed = false }: AppSidebarFooterProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { activeEnvironment } = useEnvironment();

  const envName = activeEnvironment?.name ?? t('shell.environment');
  const envProfile = activeEnvironment?.profile ?? 'production';

  const profileColors: Record<string, string> = {
    production: 'bg-critical',
    staging: 'bg-warning',
    uat: 'bg-warning',
    qa: 'bg-info',
    development: 'bg-success',
    sandbox: 'bg-cyan',
  };

  const dotColor = profileColors[envProfile] ?? 'bg-faded';

  const handleOpenSearch = () => {
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'k', ctrlKey: true, bubbles: true }));
  };

  if (collapsed) {
    return (
      <div className="border-t border-edge shrink-0 p-2 flex flex-col items-center gap-1">
        {/* Environment dot indicator */}
        <button
          onClick={() => navigate('/environments')}
          className="w-10 h-10 rounded-xl flex items-center justify-center hover:bg-hover transition-all duration-[var(--nto-motion-base)]"
          title={`${t('shell.environment')}: ${envName}`}
          aria-label={`${t('shell.environment')}: ${envName}`}
        >
          <span className={cn('w-2.5 h-2.5 rounded-full', dotColor)} aria-hidden="true" />
        </button>

        {/* Quick search */}
        <button
          onClick={handleOpenSearch}
          className="w-10 h-10 rounded-xl flex items-center justify-center text-faded hover:text-body hover:bg-hover transition-all duration-[var(--nto-motion-base)]"
          title={t('sidebarFooter.quickSearch')}
          aria-label={t('sidebarFooter.quickSearch')}
        >
          <Search size={16} aria-hidden="true" />
        </button>

        {/* Settings */}
        <button
          onClick={() => navigate('/platform/configuration')}
          className="w-10 h-10 rounded-xl flex items-center justify-center text-faded hover:text-body hover:bg-hover transition-all duration-[var(--nto-motion-base)]"
          title={t('sidebarFooter.settings')}
          aria-label={t('sidebarFooter.settings')}
        >
          <Settings size={16} aria-hidden="true" />
        </button>
      </div>
    );
  }

  return (
    <div className="border-t border-edge shrink-0 px-3.5 py-3 space-y-1.5">
      {/* Environment indicator — compact */}
      <button
        onClick={() => navigate('/environments')}
        className="w-full flex items-center gap-2.5 px-2.5 py-2 rounded-lg text-left hover:bg-hover transition-colors group"
        aria-label={`${t('shell.environment')}: ${envName}`}
      >
        <span className={cn('w-2 h-2 rounded-full shrink-0', dotColor)} aria-hidden="true" />
        <div className="flex-1 min-w-0">
          <p className="text-xs font-medium text-body truncate leading-tight">{envName}</p>
          <p className="text-[10px] text-muted truncate leading-tight capitalize">{envProfile}</p>
        </div>
      </button>

      {/* Quick actions row */}
      <div className="flex items-center gap-0.5">
        {/* Search shortcut */}
        <button
          onClick={handleOpenSearch}
          className="flex-1 flex items-center gap-2 px-2.5 py-1.5 rounded-lg text-xs text-muted hover:text-body hover:bg-hover transition-colors"
          title={t('sidebarFooter.quickSearch')}
          aria-label={t('sidebarFooter.quickSearch')}
        >
          <Search size={13} aria-hidden="true" />
          <span className="truncate">{t('sidebarFooter.search')}</span>
          <kbd className="ml-auto text-[9px] text-faded bg-elevated px-1.5 py-0.5 rounded border border-edge font-mono hidden sm:inline">
            ⌘K
          </kbd>
        </button>
      </div>

      <div className="flex items-center gap-0.5">
        {/* Knowledge Hub */}
        <button
          onClick={() => navigate('/knowledge')}
          className="flex-1 flex items-center gap-2 px-2.5 py-1.5 rounded-lg text-xs text-muted hover:text-body hover:bg-hover transition-colors"
          title={t('sidebarFooter.knowledgeHub')}
          aria-label={t('sidebarFooter.knowledgeHub')}
        >
          <BookOpen size={13} aria-hidden="true" />
          <span className="truncate">{t('sidebarFooter.docs')}</span>
        </button>

        {/* Help */}
        <button
          onClick={() => navigate('/knowledge')}
          className="p-1.5 rounded-lg text-faded hover:text-body hover:bg-hover transition-colors shrink-0"
          title={t('sidebarFooter.help')}
          aria-label={t('sidebarFooter.help')}
        >
          <HelpCircle size={14} aria-hidden="true" />
        </button>

        {/* Settings */}
        <button
          onClick={() => navigate('/platform/configuration')}
          className="p-1.5 rounded-lg text-faded hover:text-body hover:bg-hover transition-colors shrink-0"
          title={t('sidebarFooter.settings')}
          aria-label={t('sidebarFooter.settings')}
        >
          <Settings size={14} aria-hidden="true" />
        </button>
      </div>

      {/* Version info — tiny */}
      <p className="text-[9px] text-faded text-center leading-tight pt-0.5" data-testid="sidebar-version">
        NexTraceOne v1.0.0
      </p>
    </div>
  );
}
