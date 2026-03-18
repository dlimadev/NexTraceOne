import { useTranslation } from 'react-i18next';
import { FlaskConical } from 'lucide-react';

/**
 * Banner de preview — indica que o módulo ainda não está homologado para produção.
 *
 * Usado pela Fase 5 do plano operacional para proteger o aceite contra
 * escopo falso-positivo. Módulos marcados como preview são acessíveis
 * mas não fazem parte do scope de homologação.
 */
export function PreviewBanner() {
  const { t } = useTranslation();

  return (
    <div
      role="status"
      className="mb-4 flex items-center gap-3 rounded-lg border border-amber-500/25 bg-amber-500/5 px-4 py-3"
    >
      <FlaskConical size={16} className="shrink-0 text-amber-400" />
      <div className="min-w-0">
        <p className="text-sm font-medium text-amber-300">
          {t('preview.bannerTitle', 'Preview')}
        </p>
        <p className="text-xs text-amber-400/70">
          {t(
            'preview.bannerDescription',
            'This module is in preview and not yet part of the certified scope. Data and features may be incomplete.',
          )}
        </p>
      </div>
    </div>
  );
}
