import { useTranslation } from 'react-i18next';
import { InlineMessage } from './InlineMessage';

interface DemoBannerProps {
  /** Override title i18n key. Defaults to common.demoBanner.title */
  titleKey?: string;
  /** Override description i18n key. Defaults to common.demoBanner.description */
  descriptionKey?: string;
  className?: string;
}

/**
 * Reusable banner indicating that page data is simulated/demo.
 * Renders a warning InlineMessage with i18n-driven text.
 * Use on any page consuming a backend handler that returns IsSimulated=true.
 */
export function DemoBanner({
  titleKey = 'common.demoBanner.title',
  descriptionKey = 'common.demoBanner.description',
  className,
}: DemoBannerProps) {
  const { t } = useTranslation();

  return (
    <InlineMessage severity="warning" title={t(titleKey)} className={className}>
      {t(descriptionKey)}
    </InlineMessage>
  );
}
