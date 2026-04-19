import { useEffect, useRef } from 'react';
import { useLocation } from 'react-router-dom';
import { usePersona } from '../../contexts/PersonaContext';
import { productAnalyticsApi } from './api/productAnalyticsApi';

function getOrCreateSessionId(): string {
  const key = 'nextraceone.analytics.sessionId';
  const existing = sessionStorage.getItem(key);
  if (existing) return existing;

  const id = typeof crypto !== 'undefined' && 'randomUUID' in crypto
    ? crypto.randomUUID()
    : `${Date.now()}-${Math.random().toString(16).slice(2)}`;

  sessionStorage.setItem(key, id);
  return id;
}

function resolveModuleFromPath(pathname: string): string | null {
  if (pathname === '/' || pathname.startsWith('/dashboard')) return 'Dashboard';
  if (pathname.startsWith('/source-of-truth')) return 'SourceOfTruth';
  if (pathname.startsWith('/catalog')) return 'ServiceCatalog';
  if (pathname.startsWith('/contracts')) return 'ContractStudio';
  if (pathname.startsWith('/changes')) return 'ChangeIntelligence';
  if (pathname.startsWith('/operations')) return 'Incidents';
  if (pathname.startsWith('/ai')) return 'AiAssistant';
  if (pathname.startsWith('/governance')) return 'Governance';
  if (pathname.startsWith('/analytics')) return 'ProductAnalytics';
  if (pathname.startsWith('/platform')) return 'Admin';
  if (pathname.startsWith('/admin')) return 'Admin';
  return null;
}

/**
 * Captura mínima de eventos reais de Product Analytics.
 * MVP: regista apenas page/module views com baixo acoplamento.
 */
export function AnalyticsEventTracker() {
  const location = useLocation();
  const { persona } = usePersona();
  const lastPathRef = useRef<string | null>(null);

  useEffect(() => {
    const pathname = location.pathname;
    if (lastPathRef.current === pathname) return;
    lastPathRef.current = pathname;

    const module = resolveModuleFromPath(pathname);
    if (!module) return;

    const sessionId = getOrCreateSessionId();

    productAnalyticsApi
      .recordEvent({
        eventType: 'ModuleViewed',
        module,
        route: pathname,
        feature: 'page_view',
        personaHint: persona,
        sessionCorrelationId: sessionId,
        clientType: 'Web',
      })
      .catch(() => undefined);
  }, [location.pathname, persona]);

  return null;
}
