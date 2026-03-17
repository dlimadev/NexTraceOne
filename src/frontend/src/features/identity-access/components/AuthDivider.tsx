import { useTranslation } from 'react-i18next';

interface AuthDividerProps {
  labelKey?: string;
}

/**
 * Divider visual "ou" entre métodos de autenticação.
 * Label vem de i18n (default: auth.orDivider).
 */
export function AuthDivider({ labelKey = 'auth.orDivider' }: AuthDividerProps) {
  const { t } = useTranslation();

  return (
    <div className="relative flex items-center my-7" role="separator">
      <div className="flex-1 border-t border-divider" />
      <span className="px-3 text-xs text-faded uppercase tracking-wider font-medium">
        {t(labelKey)}
      </span>
      <div className="flex-1 border-t border-divider" />
    </div>
  );
}
