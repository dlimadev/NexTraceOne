import { ShieldOff } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Button } from '../components/Button';

/**
 * Página exibida quando o usuário não tem permissão para acessar um recurso.
 * Todos os textos são resolvidos via i18n (chaves em authorization.*).
 */
export function UnauthorizedPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  return (
    <div className="flex flex-col items-center justify-center h-full py-24 text-center px-4 animate-fade-in">
      <div className="inline-flex items-center justify-center w-16 h-16 bg-critical/15 rounded-full mb-6">
        <ShieldOff size={32} className="text-critical" />
      </div>
      <h1 className="text-2xl font-bold text-heading mb-2">{t('authorization.accessDenied')}</h1>
      <p className="text-muted max-w-sm mb-6">
        {t('authorization.accessDeniedDescription')}
      </p>
      <Button onClick={() => navigate('/')} variant="secondary">
        {t('authorization.goToDashboard')}
      </Button>
    </div>
  );
}
