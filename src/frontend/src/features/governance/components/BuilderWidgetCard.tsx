/**
 * BuilderWidgetCard — card visual de widget no canvas do dashboard builder.
 * Inclui header bar com título e ações (configurar, remover), estados visuais
 * para hover/selected/dragging/resizing, e integração com react-grid-layout.
 */
import { X, GripVertical, Pencil } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import type { WidgetType } from '../widgets/WidgetRegistry';
import { WIDGET_META } from '../widgets/WidgetRegistry';

const WIDGET_ICONS: Record<string, string> = {
  'dora-metrics':          '📊',
  'service-scorecard':     '🏆',
  'incident-summary':      '🚨',
  'change-confidence':     '🎯',
  'cost-trend':            '💰',
  'reliability-slo':       '🛡️',
  'knowledge-graph':       '🕸️',
  'on-call-status':        '📞',
  'alert-status':          '⚠️',
  'change-timeline':       '📅',
  'slo-gauge':             '⏱️',
  'deployment-frequency':  '🚀',
  'stat':                  '📈',
  'text-markdown':         '📝',
  'top-services':          '🔝',
  'contract-coverage':     '📋',
  'blast-radius':          '💥',
  'team-health':           '💪',
  'release-calendar':      '📆',
  'query-widget':          '🔍',
  'obs-metrics':           '📡',
  'obs-logs':              '📜',
  'obs-traces':            '🔗',
  'obs-error-rate':        '🚦',
  'obs-service-map':       '🗺️',
  'obs-pie-chart':         '🥧',
  'obs-bar-gauge':         '📊',
  'obs-heatmap-calendar':  '🗓️',
  'obs-treemap':           '🟩',
  'obs-histogram':         '📉',
};

function widgetIcon(type: string): string {
  return WIDGET_ICONS[type] ?? '📦';
}

export interface BuilderWidgetCardProps {
  type: WidgetType;
  tempId: string;
  customTitle: string;
  w: number;
  h: number;
  isSelected: boolean;
  isReadOnly: boolean;
  /** Opens the Panel Editor overlay for this widget */
  onEditOpen: (tempId: string) => void;
  /** Opens the legacy config drawer (deprecated — will be removed in v2) */
  onConfigOpen?: (tempId: string) => void;
  onRemove: (tempId: string) => void;
  onSelect: (tempId: string) => void;
  children?: React.ReactNode;
}

export function BuilderWidgetCard({
  type,
  tempId,
  customTitle,
  w,
  h,
  isSelected,
  isReadOnly,
  onEditOpen,
  onRemove,
  onSelect,
  children,
}: BuilderWidgetCardProps) {
  const { t } = useTranslation();
  const meta = WIDGET_META[type];
  const label = customTitle || t(meta?.labelKey ?? type, type);

  return (
    <div
      className={`builder-widget-card ${isSelected ? 'builder-widget-card--selected' : ''}`}
      onClick={(e) => {
        // Only select if not clicking a button inside
        if ((e.target as HTMLElement).closest('button')) return;
        onSelect(tempId);
      }}
      data-temp-id={tempId}
    >
      {/* Header bar — serves as drag handle via the .drag-handle class on parent */}
      <div className="builder-widget-header">
        <div className="flex items-center gap-1.5 min-w-0 flex-1">
          <GripVertical size={12} className="text-muted shrink-0 opacity-50" />
          <span className="text-sm leading-none shrink-0">{widgetIcon(type)}</span>
          <span className="builder-widget-header__title" title={label}>
            {label}
          </span>
        </div>

        {!isReadOnly && (
          <div className="builder-widget-header__actions">
            <button
              type="button"
              className="builder-widget-header__btn"
              onClick={(e) => {
                e.stopPropagation();
                onEditOpen(tempId);
              }}
              title={t('governance.dashboardBuilder.panelEditor.title', 'Edit panel')}
              aria-label={t('governance.dashboardBuilder.panelEditor.title', 'Edit panel')}
            >
              <Pencil size={11} />
            </button>
            <button
              type="button"
              className="builder-widget-header__btn builder-widget-header__btn--danger"
              onClick={(e) => {
                e.stopPropagation();
                onRemove(tempId);
              }}
              title={t('governance.dashboardBuilder.removeWidget', 'Remove widget')}
              aria-label={t('governance.dashboardBuilder.removeWidget', 'Remove widget')}
            >
              <X size={11} />
            </button>
          </div>
        )}
      </div>

      {/* Body */}
      <div className="builder-widget-body">
        {children ?? (
          <>
            <span className="text-3xl opacity-40">{widgetIcon(type)}</span>
            <span className="text-xs font-medium text-muted text-center leading-tight line-clamp-2">
              {label}
            </span>
            <span className="text-[10px] text-faded tabular-nums">
              {w}×{h}
            </span>
          </>
        )}
      </div>
    </div>
  );
}
