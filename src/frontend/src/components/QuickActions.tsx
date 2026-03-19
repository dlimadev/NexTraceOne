import * as React from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  Search,
  FileCode,
  FileText,
  AlertTriangle,
  ShieldAlert,
  Zap,
  UserCheck,
  Server,
  Share2,
  Globe,
  ShieldCheck,
  Activity,
  BarChart3,
  Scale,
  Database,
  Settings,
  ClipboardList,
  Download,
  ClipboardCheck,
  Bot,
} from 'lucide-react';
import { Badge } from './Badge';
import { usePersona } from '../contexts/PersonaContext';
import type { QuickAction } from '../auth/persona';

/**
 * Mapeamento de nomes de ícone para componentes lucide-react.
 * Extensível para novos ícones conforme novas quick actions forem adicionadas.
 */
const iconMap: Record<string, React.ReactNode> = {
  Search: <Search size={16} />,
  FileCode: <FileCode size={16} />,
  FileText: <FileText size={16} />,
  AlertTriangle: <AlertTriangle size={16} />,
  ShieldAlert: <ShieldAlert size={16} />,
  Zap: <Zap size={16} />,
  UserCheck: <UserCheck size={16} />,
  Server: <Server size={16} />,
  Share2: <Share2 size={16} />,
  Globe: <Globe size={16} />,
  ShieldCheck: <ShieldCheck size={16} />,
  Activity: <Activity size={16} />,
  BarChart3: <BarChart3 size={16} />,
  Scale: <Scale size={16} />,
  Database: <Database size={16} />,
  Settings: <Settings size={16} />,
  ClipboardList: <ClipboardList size={16} />,
  Download: <Download size={16} />,
  ClipboardCheck: <ClipboardCheck size={16} />,
  Bot: <Bot size={16} />,
};

/**
 * Componente de Quick Actions adaptável por persona.
 *
 * Exibe atalhos contextuais na Home, adaptados ao perfil do utilizador.
 * Cada persona vê acções relevantes ao seu foco (operacional, governança, etc.).
 *
 * @see docs/PERSONA-UX-MAPPING.md — secção de quick actions por persona
 */
export function QuickActions() {
  const { t } = useTranslation();
  const { config } = usePersona();
  const navigate = useNavigate();

  const handleAction = (action: QuickAction) => {
    navigate(action.to);
  };

  return (
    <div className="mb-6">
      <h2 className="text-sm font-semibold text-muted uppercase tracking-wider mb-3">
        {t('persona.quickActions')}
      </h2>
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
        {config.quickActions.map((action) => (
          <button
            key={action.id}
            onClick={() => handleAction(action)}
            className="flex items-center gap-3 px-4 py-3 rounded-lg bg-panel border border-edge
                       hover:bg-hover hover:border-accent/30 transition-all text-left group"
          >
            <span className="text-accent group-hover:text-accent/80 shrink-0">
              {iconMap[action.icon] ?? <Zap size={16} />}
            </span>
            <span className="min-w-0 flex-1">
              <span className="text-sm text-body group-hover:text-heading truncate block">
                {t(action.labelKey)}
              </span>
              {action.preview && (
                <span className="mt-1 inline-flex">
                  <Badge variant="warning" className="text-[10px]">
                    {t('preview.bannerTitle', 'Preview')}
                  </Badge>
                </span>
              )}
            </span>
          </button>
        ))}
      </div>
    </div>
  );
}
