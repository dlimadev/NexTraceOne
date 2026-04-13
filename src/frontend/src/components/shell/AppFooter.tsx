import { useTranslation } from 'react-i18next';
import { useBranding } from '../../contexts/BrandingContext';

/**
 * AppFooter — rodapé da aplicação com texto de branding dinâmico.
 *
 * Exibe o `branding.footer_text` configurado via parametrização.
 * Quando não configurado, exibe o texto padrão do produto.
 * Sempre inclui o watermark "Powered by NexTraceOne" para proteção de identidade.
 * O watermark pode ser controlado via parâmetro branding.powered_by_visible,
 * mas o padrão é true.
 * Pilar: Platform Customization + Source of Truth + Identity Protection
 */
export function AppFooter() {
  const { t } = useTranslation();
  const { footerText, instanceName, poweredByVisible } = useBranding();

  const displayText =
    footerText || t('footer.default', { name: instanceName || 'NexTraceOne' });

  return (
    <footer className="shrink-0 border-t border-edge px-6 py-2">
      <div className="flex items-center justify-between gap-4">
        <p className="text-[11px] text-faded flex-1 text-center">{displayText}</p>
        {poweredByVisible && (
          <span className="text-[10px] text-faded/60 shrink-0 select-none">
            {t('footer.poweredBy')}
          </span>
        )}
      </div>
    </footer>
  );
}
