import { createContext, useContext, useMemo, type ReactNode } from 'react';
import { useAuth } from './AuthContext';
import {
  derivePersona,
  personaConfigs,
  type Persona,
  type PersonaConfig,
} from '../auth/persona';

/**
 * Contexto central de persona do NexTraceOne.
 *
 * Fornece a persona derivada do utilizador autenticado e toda a configuração
 * UX associada (navegação, Home, quick actions, IA).
 *
 * Consumido por: Sidebar, Home, QuickActions, AI Assistant e qualquer componente
 * que precise adaptar a experiência ao perfil do utilizador.
 *
 * @see docs/PERSONA-MATRIX.md
 * @see docs/PERSONA-UX-MAPPING.md
 */
interface PersonaContextValue {
  /** Persona derivada do perfil do utilizador. */
  persona: Persona;
  /** Configuração UX completa para a persona actual. */
  config: PersonaConfig;
}

const PersonaContext = createContext<PersonaContextValue | null>(null);

export function PersonaProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth();

  const value = useMemo<PersonaContextValue>(() => {
    const persona = derivePersona(user?.roleName ?? '');
    return {
      persona,
      config: personaConfigs[persona],
    };
  }, [user?.roleName]);

  return (
    <PersonaContext.Provider value={value}>
      {children}
    </PersonaContext.Provider>
  );
}

/**
 * Hook que expõe a persona do utilizador e a configuração UX associada.
 *
 * Deve ser usado dentro de AuthProvider e PersonaProvider.
 * Retorna a persona derivada e toda a configuração de experiência.
 */
export function usePersona(): PersonaContextValue {
  const ctx = useContext(PersonaContext);
  if (!ctx) throw new Error('usePersona must be used within PersonaProvider');
  return ctx;
}
