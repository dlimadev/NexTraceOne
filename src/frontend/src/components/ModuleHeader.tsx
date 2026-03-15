import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Info } from 'lucide-react';
import { useState } from 'react';

interface ModuleHeaderProps {
  /** Chave i18n do título da página. */
  titleKey: string;
  /** Chave i18n do subtítulo (descrição curta). */
  subtitleKey?: string;
  /** Chave i18n da orientação contextual do módulo. */
  guidanceKey?: string;
  /** Conteúdo adicional (ex: botões, badges) na mesma linha do título. */
  actions?: ReactNode;
}

/**
 * Cabeçalho padronizado para páginas de módulo.
 *
 * Fornece título, subtítulo e orientação contextual opcional.
 * A orientação aparece como tooltip discreto, respeitando a UX enterprise.
 *
 * Garante consistência visual entre todos os módulos do produto.
 */
export function ModuleHeader({ titleKey, subtitleKey, guidanceKey, actions }: ModuleHeaderProps) {
  const { t } = useTranslation();
  const [showGuidance, setShowGuidance] = useState(false);

  return (
    <div className="mb-6">
      <div className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <h1 className="text-2xl font-bold text-heading">{t(titleKey)}</h1>
          {guidanceKey && (
            <div className="relative">
              <button
                type="button"
                onClick={() => setShowGuidance((prev) => !prev)}
                className="text-muted hover:text-accent transition-colors p-1 rounded-md hover:bg-accent/10"
                aria-label={t('productPolish.moduleGuidanceTitle')}
              >
                <Info className="h-4 w-4" />
              </button>
              {showGuidance && (
                <div className="absolute left-0 top-8 z-20 w-80 bg-elevated border border-edge rounded-lg p-4 shadow-lg animate-fade-in">
                  <p className="text-xs font-medium text-heading mb-1">
                    {t('productPolish.moduleGuidanceTitle')}
                  </p>
                  <p className="text-xs text-muted leading-relaxed">
                    {t(guidanceKey)}
                  </p>
                  <button
                    type="button"
                    onClick={() => setShowGuidance(false)}
                    className="mt-2 text-xs text-accent hover:text-accent/80 transition-colors"
                  >
                    {t('common.close')}
                  </button>
                </div>
              )}
            </div>
          )}
        </div>
        {actions && <div className="flex items-center gap-2">{actions}</div>}
      </div>
      {subtitleKey && (
        <p className="text-muted mt-1">{t(subtitleKey)}</p>
      )}
    </div>
  );
}
