import { AlertTriangle } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { cn } from '../../lib/cn';
import { useEnvironment } from '../../contexts/EnvironmentContext';

/**
 * Banner contextual que aparece quando o ambiente ativo é não produtivo.
 *
 * Objetivo: tornar visível que o utilizador está operando num ambiente não produtivo,
 * ajudando a evitar confusão entre dados de QA/UAT e produção.
 *
 * NÃO bloqueia navegação — é informativo e pode ser dispensado por sessão.
 * A decisão real de segurança é feita pelo backend.
 *
 * FASE 7: Poderá exibir sinais de risco e readiness vindos do backend.
 */
export function EnvironmentBanner() {
  const { t } = useTranslation();
  const { activeEnvironment } = useEnvironment();

  // Only show for non-production-like environments
  if (!activeEnvironment || activeEnvironment.isProductionLike) {
    return null;
  }

  return (
    <div
      role="status"
      aria-live="polite"
      className={cn(
        'flex items-center gap-2 px-4 py-2 text-xs',
        'bg-yellow-500/10 border-b border-yellow-500/20 text-yellow-300',
      )}
    >
      <AlertTriangle size={13} className="shrink-0" aria-hidden="true" />
      <span>
        {t('environment.nonProductionBanner', {
          name: activeEnvironment.name,
          profile: activeEnvironment.profile,
        })}
      </span>
    </div>
  );
}
