import { useQuery } from '@tanstack/react-query';
import { usePersona } from '../../../contexts/PersonaContext';
import { identityApi } from '../api/identity';
import type { PersonaConfigResponse } from '../api/identity';

const STALE_TIME = 5 * 60 * 1000; // 5 minutes

/**
 * Hook que obtém a configuração de navegação adaptativa para a persona do utilizador autenticado.
 *
 * Chama o endpoint `GET /api/v1/identity/me/persona-config` (Wave X.3) e combina o resultado
 * com a persona derivada localmente via PersonaContext.
 *
 * Quando a chamada ao backend falha ou ainda está a carregar, usa a configuração local
 * do PersonaContext como fallback para não bloquear a experiência do utilizador.
 *
 * @see docs/FUTURE-ROADMAP.md Wave X.3
 */
export function usePersonaConfig(): {
  personaConfig: PersonaConfigResponse | null;
  isLoading: boolean;
  isError: boolean;
} {
  const { persona, config } = usePersona();

  const { data, isLoading, isError } = useQuery({
    queryKey: ['persona-config', persona],
    queryFn: identityApi.getPersonaConfig,
    staleTime: STALE_TIME,
    retry: false,
  });

  if (data) {
    return { personaConfig: data, isLoading: false, isError: false };
  }

  // Fallback: derive from local PersonaContext config
  if (isError || !isLoading) {
    const fallback: PersonaConfigResponse = {
      persona,
      quickActions: config.quickActions.map((qa) => ({
        id: qa.id,
        labelKey: qa.labelKey,
        icon: qa.icon,
        to: qa.to,
      })),
      prioritizedModules: config.sectionOrder,
    };
    return { personaConfig: fallback, isLoading: false, isError: isError };
  }

  return { personaConfig: null, isLoading, isError: false };
}
