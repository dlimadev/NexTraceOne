import { useTranslation } from 'react-i18next';
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
} from 'lucide-react';
import { Card, CardHeader, CardBody } from './Card';
import { EmptyState } from './EmptyState';
import type { HomeWidget } from '../auth/persona';

/**
 * Mapeamento de tipo de widget para ícone e cor — fornece identidade visual consistente
 * independentemente da persona.
 */
const widgetMeta: Record<HomeWidget['type'], { icon: React.ReactNode; color: string }> = {
  services: { icon: <Server size={18} />, color: 'text-brand-blue' },
  changes: { icon: <Zap size={18} />, color: 'text-warning' },
  incidents: { icon: <AlertTriangle size={18} />, color: 'text-critical' },
  contracts: { icon: <FileText size={18} />, color: 'text-success' },
  reliability: { icon: <Activity size={18} />, color: 'text-accent' },
  dependencies: { icon: <Share2 size={18} />, color: 'text-brand-blue' },
  risk: { icon: <Shield size={18} />, color: 'text-warning' },
  trend: { icon: <TrendingUp size={18} />, color: 'text-accent' },
  governance: { icon: <ShieldCheck size={18} />, color: 'text-success' },
  audit: { icon: <ClipboardList size={18} />, color: 'text-muted' },
  ownership: { icon: <Users size={18} />, color: 'text-warning' },
  releaseConfidence: { icon: <ShieldCheck size={18} />, color: 'text-success' },
  aiInsights: { icon: <Bot size={18} />, color: 'text-accent' },
};

/**
 * Widget genérico da Home adaptável por persona.
 *
 * Renderiza um card com ícone, título i18n e conteúdo placeholder.
 * O conteúdo real será populado quando os endpoints específicos forem ligados.
 * A estrutura está pronta para receber dados de API de forma incremental.
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
        <EmptyState
          icon={meta.icon}
          title={t('persona.widgetComingSoon')}
        />
      </CardBody>
    </Card>
  );
}
