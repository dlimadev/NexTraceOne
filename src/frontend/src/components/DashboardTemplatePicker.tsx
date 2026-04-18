import { useTranslation } from 'react-i18next';
import { X, Layout } from 'lucide-react';
import { Button } from './Button';
import type { WidgetType } from '../features/governance/widgets/WidgetRegistry';

export interface TemplatePreview {
  id: string;
  titleKey: string;
  descKey: string;
  /** Real WidgetType values used to pre-fill the dashboard create form */
  widgets: WidgetType[];
  persona: string;
  layout: string;
}

/** All persona templates. Widget IDs match WidgetType values in WidgetRegistry. */
const TEMPLATES: TemplatePreview[] = [
  {
    id: 'engineer',
    titleKey: 'dashboardTemplates.engineer.title',
    descKey: 'dashboardTemplates.engineer.description',
    widgets: ['service-scorecard', 'change-confidence', 'incident-summary', 'dora-metrics'],
    persona: 'Engineer',
    layout: 'two-column',
  },
  {
    id: 'tech_lead',
    titleKey: 'dashboardTemplates.techLead.title',
    descKey: 'dashboardTemplates.techLead.description',
    widgets: ['service-scorecard', 'change-confidence', 'reliability-slo', 'dora-metrics'],
    persona: 'TechLead',
    layout: 'two-column',
  },
  {
    id: 'architect',
    titleKey: 'dashboardTemplates.architect.title',
    descKey: 'dashboardTemplates.architect.description',
    widgets: ['service-scorecard', 'knowledge-graph', 'reliability-slo', 'change-confidence'],
    persona: 'Architect',
    layout: 'grid',
  },
  {
    id: 'executive',
    titleKey: 'dashboardTemplates.executive.title',
    descKey: 'dashboardTemplates.executive.description',
    widgets: ['cost-trend', 'incident-summary', 'dora-metrics', 'reliability-slo'],
    persona: 'Executive',
    layout: 'two-column',
  },
  {
    id: 'platform_admin',
    titleKey: 'dashboardTemplates.platformAdmin.title',
    descKey: 'dashboardTemplates.platformAdmin.description',
    widgets: ['dora-metrics', 'incident-summary', 'change-confidence', 'reliability-slo'],
    persona: 'PlatformAdmin',
    layout: 'grid',
  },
  {
    id: 'product',
    titleKey: 'dashboardTemplates.product.title',
    descKey: 'dashboardTemplates.product.description',
    widgets: ['dora-metrics', 'cost-trend', 'reliability-slo', 'incident-summary'],
    persona: 'Product',
    layout: 'two-column',
  },
  {
    id: 'auditor',
    titleKey: 'dashboardTemplates.auditor.title',
    descKey: 'dashboardTemplates.auditor.description',
    widgets: ['change-confidence', 'incident-summary', 'service-scorecard', 'cost-trend'],
    persona: 'Auditor',
    layout: 'two-column',
  },
];

interface Props {
  open: boolean;
  onClose: () => void;
  onSelect: (template: TemplatePreview) => void;
}

/** Modal para escolher um template de dashboard por persona. */
export function DashboardTemplatePicker({ open, onClose, onSelect }: Props) {
  const { t } = useTranslation();

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white dark:bg-gray-900 rounded-lg shadow-xl w-full max-w-2xl p-6">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <Layout className="w-5 h-5 text-blue-600" />
            <h2 className="text-lg font-semibold text-gray-900 dark:text-white">
              {t('dashboardTemplates.modal.title')}
            </h2>
          </div>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200">
            <X className="w-5 h-5" />
          </button>
        </div>
        <p className="text-sm text-gray-500 dark:text-gray-400 mb-5">
          {t('dashboardTemplates.modal.subtitle')}
        </p>
        <div className="grid grid-cols-1 gap-3">
          {TEMPLATES.map((tpl) => (
            <button
              key={tpl.id}
              onClick={() => { onSelect(tpl); onClose(); }}
              className="text-left border border-gray-200 dark:border-gray-700 rounded-lg p-4 hover:border-blue-400 hover:bg-blue-50 dark:hover:bg-blue-900/20 transition-colors"
            >
              <div className="font-medium text-gray-900 dark:text-white text-sm mb-1">
                {t(tpl.titleKey)}
              </div>
              <p className="text-xs text-gray-500 dark:text-gray-400 mb-2">{t(tpl.descKey)}</p>
              <div className="flex flex-wrap gap-1">
                {tpl.widgets.map((w) => (
                  <span key={w} className="text-xs bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-300 px-2 py-0.5 rounded">
                    {w}
                  </span>
                ))}
              </div>
            </button>
          ))}
        </div>
        <div className="mt-4 flex justify-end">
          <Button variant="outline" size="sm" onClick={onClose}>
            {t('common.cancel')}
          </Button>
        </div>
      </div>
    </div>
  );
}
