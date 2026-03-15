import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  Activity,
  Zap,
  AlertTriangle,
  FileText,
  Shield,
  Share2,
  TrendingUp,
  ClipboardList,
  Bot,
  Server,
  ShieldCheck,
  Users,
  ArrowRight,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from './Card';
import type { HomeWidget } from '../auth/persona';

/**
 * Metadados visuais e de navegação para cada tipo de widget.
 */
interface WidgetMetadata {
  icon: React.ReactNode;
  color: string;
  route: string;
  descKey: string;
}

/**
 * Mapeamento de tipo de widget para ícone, cor e rota de destino.
 * Fornece identidade visual e navegação contextual consistentes.
 */
const widgetMeta: Record<HomeWidget['type'], WidgetMetadata> = {
  services: { icon: <Server size={18} />, color: 'text-brand-blue', route: '/services', descKey: 'productPolish.guidanceServices' },
  changes: { icon: <Zap size={18} />, color: 'text-warning', route: '/changes', descKey: 'productPolish.guidanceChanges' },
  incidents: { icon: <AlertTriangle size={18} />, color: 'text-critical', route: '/operations/incidents', descKey: 'productPolish.guidanceOperations' },
  contracts: { icon: <FileText size={18} />, color: 'text-success', route: '/contracts', descKey: 'productPolish.guidanceContracts' },
  reliability: { icon: <Activity size={18} />, color: 'text-accent', route: '/operations/reliability', descKey: 'productPolish.guidanceOperations' },
  dependencies: { icon: <Share2 size={18} />, color: 'text-brand-blue', route: '/services/graph', descKey: 'productPolish.guidanceServices' },
  risk: { icon: <Shield size={18} />, color: 'text-warning', route: '/governance/risk', descKey: 'productPolish.guidanceGovernance' },
  trend: { icon: <TrendingUp size={18} />, color: 'text-accent', route: '/governance/reports', descKey: 'productPolish.guidanceGovernance' },
  governance: { icon: <ShieldCheck size={18} />, color: 'text-success', route: '/governance/reports', descKey: 'productPolish.guidanceGovernance' },
  audit: { icon: <ClipboardList size={18} />, color: 'text-muted', route: '/audit', descKey: 'productPolish.guidanceAdmin' },
  ownership: { icon: <Users size={18} />, color: 'text-warning', route: '/services', descKey: 'productPolish.guidanceServices' },
  releaseConfidence: { icon: <ShieldCheck size={18} />, color: 'text-success', route: '/changes', descKey: 'productPolish.guidanceChanges' },
  aiInsights: { icon: <Bot size={18} />, color: 'text-accent', route: '/ai/assistant', descKey: 'productPolish.guidanceAiHub' },
};

/**
 * Widget genérico da Home adaptável por persona.
 *
 * Renderiza um card com ícone, título i18n e descrição contextual.
 * Inclui link de navegação para o módulo relevante.
 * O conteúdo real será populado quando os endpoints específicos forem ligados.
 */
export function HomeWidgetCard({ widget }: { widget: HomeWidget }) {
  const { t } = useTranslation();
  const meta = widgetMeta[widget.type] ?? widgetMeta.services;

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-2">
          <span className={meta.color}>{meta.icon}</span>
          <h3 className="text-sm font-semibold text-heading">{t(widget.titleKey)}</h3>
        </div>
      </CardHeader>
      <CardBody>
        <div className="flex flex-col items-center justify-center py-6 px-4 text-center">
          <div className={`flex items-center justify-center w-10 h-10 rounded-full bg-elevated mb-3 ${meta.color}`}>
            {meta.icon}
          </div>
          <p className="text-xs text-muted max-w-xs mb-3 leading-relaxed">
            {t(meta.descKey)}
          </p>
          <Link
            to={meta.route}
            className="inline-flex items-center gap-1.5 text-xs text-accent hover:text-accent/80 transition-colors font-medium"
          >
            {t('common.explore')} <ArrowRight size={12} />
          </Link>
        </div>
      </CardBody>
    </Card>
  );
}
