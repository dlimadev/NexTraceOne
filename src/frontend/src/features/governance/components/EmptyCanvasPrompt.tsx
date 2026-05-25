/**
 * EmptyCanvasPrompt — estado vazio convidativo no canvas do builder.
 * Aparece quando não há widgets e nenhum drag está em progresso.
 */
import { LayoutGrid, MousePointerClick, ArrowDown } from 'lucide-react';
import { useTranslation } from 'react-i18next';

export interface EmptyCanvasPromptProps {
  className?: string;
}

export function EmptyCanvasPrompt({ className = '' }: EmptyCanvasPromptProps) {
  const { t } = useTranslation();

  return (
    <div className={`empty-canvas-prompt ${className}`}>
      <div className="empty-canvas-prompt__icon">
        <LayoutGrid size={28} strokeWidth={1.5} />
      </div>
      <p className="empty-canvas-prompt__title">
        {t('governance.dashboardBuilder.emptyTitle', 'Your dashboard is empty')}
      </p>
      <p className="empty-canvas-prompt__hint">
        {t(
          'governance.dashboardBuilder.emptyHint',
          'Drag widgets from the left palette, or click a widget to add it instantly.'
        )}
      </p>
      <div className="flex items-center gap-1.5 mt-1 text-muted opacity-60">
        <MousePointerClick size={14} />
        <ArrowDown size={14} className="animate-bounce" />
      </div>
    </div>
  );
}
