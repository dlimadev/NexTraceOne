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
    <div className="flex flex-col items-center justify-center h-full py-24 text-center px-4">
      <div className="inline-flex items-center justify-center w-16 h-16 bg-red-100 rounded-full mb-6">
        <ShieldOff size={32} className="text-red-500" />
      </div>
      <h1 className="text-2xl font-bold text-gray-900 mb-2">{t('authorization.accessDenied')}</h1>
      <p className="text-gray-500 max-w-sm mb-6">
        {t('authorization.accessDeniedDescription')}
      </p>
      <Button onClick={() => navigate('/')} variant="secondary">
        {t('authorization.goToDashboard')}
      </Button>
    </div>
  );
}
