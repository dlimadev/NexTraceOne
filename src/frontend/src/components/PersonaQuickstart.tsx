import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  CheckCircle2,
  Circle,
  ChevronDown,
  ChevronUp,
  Rocket,
} from 'lucide-react';
import { usePersona } from '../contexts/PersonaContext';
import type { Persona } from '../auth/persona';

const STORAGE_KEY = 'nex:quickstart:completed';

/** Rotas recomendadas por persona para cada passo do quickstart. */
const stepRoutes: Record<Persona, string[]> = {
  Engineer: ['/services', '/changes', '/operations/incidents', '/contracts'],
  TechLead: ['/services', '/changes', '/operations/reliability', '/operations/incidents'],
  Architect: ['/services/graph', '/contracts', '/changes', '/governance/risk'],
  Product: ['/changes', '/operations/reliability', '/governance/risk', '/operations/incidents'],
  Executive: ['/governance/executive', '/governance/risk', '/services', '/governance/compliance'],
  PlatformAdmin: ['/governance/policies', '/ai/models', '/users', '/governance/reports'],
  Auditor: ['/audit', '/governance/evidence', '/governance/compliance', '/ai/policies'],
};

function getCompleted(): Set<string> {
  const raw = sessionStorage.getItem(STORAGE_KEY);
  return raw ? new Set<string>(JSON.parse(raw) as string[]) : new Set<string>();
}

function persistCompleted(ids: Set<string>): void {
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify([...ids]));
}

/**
 * Componente de quickstart persona-aware para a Home.
 *
 * Exibe até 4 passos recomendados com base na persona do utilizador.
 * Cada passo pode ser marcado como concluído (estado guardado em sessionStorage).
 * O componente é colapsável e desaparece quando todos os passos estão concluídos.
 */
export function PersonaQuickstart() {
  const { t } = useTranslation();
  const { persona } = usePersona();
  const [completed, setCompleted] = useState<Set<string>>(() => getCompleted());
  const [collapsed, setCollapsed] = useState(false);

  useEffect(() => {
    setCompleted(getCompleted());
  }, [persona]);

  const routes = stepRoutes[persona] ?? stepRoutes.Engineer;
  const steps = [1, 2, 3, 4].map((n) => ({
    id: `${persona}-step${n}`,
    labelKey: `productPolish.quickstart.${persona}.step${n}`,
    route: routes[n - 1],
  }));

  const allDone = steps.every((s) => completed.has(s.id));

  const handleToggle = (id: string) => {
    const next = new Set(completed);
    if (next.has(id)) {
      next.delete(id);
    } else {
      next.add(id);
    }
    setCompleted(next);
    persistCompleted(next);
  };

  if (allDone) return null;

  const doneCount = steps.filter((s) => completed.has(s.id)).length;

  return (
    <section className="mb-6 bg-panel border border-edge rounded-lg overflow-hidden animate-fade-in">
      <button
        type="button"
        onClick={() => setCollapsed((prev) => !prev)}
        className="w-full flex items-center justify-between px-5 py-3 hover:bg-hover transition-colors"
      >
        <div className="flex items-center gap-3">
          <Rocket className="h-4 w-4 text-accent" />
          <span className="text-sm font-semibold text-heading">
            {t('productPolish.quickstartTitle')}
          </span>
          <span className="text-xs text-muted">
            {doneCount}/{steps.length}
          </span>
        </div>
        {collapsed ? (
          <ChevronDown className="h-4 w-4 text-muted" />
        ) : (
          <ChevronUp className="h-4 w-4 text-muted" />
        )}
      </button>

      {!collapsed && (
        <div className="px-5 pb-4 space-y-2">
          <p className="text-xs text-muted mb-3">
            {t('productPolish.quickstartSubtitle')}
          </p>
          {steps.map((step) => {
            const isDone = completed.has(step.id);
            return (
              <div
                key={step.id}
                className="flex items-center gap-3 group"
              >
                <button
                  type="button"
                  onClick={() => handleToggle(step.id)}
                  className="shrink-0 text-muted hover:text-accent transition-colors"
                  aria-label={isDone ? 'Mark incomplete' : 'Mark complete'}
                >
                  {isDone ? (
                    <CheckCircle2 className="h-4 w-4 text-success" />
                  ) : (
                    <Circle className="h-4 w-4" />
                  )}
                </button>
                <Link
                  to={step.route}
                  className={`text-sm transition-colors ${
                    isDone
                      ? 'text-muted line-through'
                      : 'text-body hover:text-accent'
                  }`}
                >
                  {t(step.labelKey)}
                </Link>
              </div>
            );
          })}
        </div>
      )}
    </section>
  );
}
