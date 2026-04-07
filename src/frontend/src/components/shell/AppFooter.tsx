import { useTranslation } from 'react-i18next';
import { useBranding } from '../../contexts/BrandingContext';

/**
 * AppFooter — rodapé da aplicação com texto de branding dinâmico.
 *
 * Exibe o `branding.footer_text` configurado via parametrização.
 * Quando não configurado, exibe o texto padrão do produto.
 * Pilar: Platform Customization + Source of Truth
 */
export function AppFooter() {
  const { t } = useTranslation();
  const { footerText, instanceName } = useBranding();

  const displayText =
    footerText || t('footer.default', { name: instanceName || 'NexTraceOne' });

  return (
    <footer className="shrink-0 border-t border-edge px-6 py-2 text-center">
      <p className="text-[11px] text-faded">{displayText}</p>
    </footer>
  );
}
