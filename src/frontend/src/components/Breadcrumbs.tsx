import { Link, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ChevronRight } from 'lucide-react';

/**
 * Mapa de segmentos de rota para chaves i18n da sidebar/módulos.
 * Permite traduzir os breadcrumbs de forma consistente com a navegação lateral.
 */
const segmentLabels: Record<string, string> = {
  '': 'sidebar.dashboard',
  'services': 'sidebar.serviceCatalog',
  'contracts': 'sidebar.apiContracts',
  'changes': 'sidebar.changeConfidence',
  'releases': 'sidebar.changeIntelligence',
  'workflow': 'sidebar.workflow',
  'promotion': 'sidebar.promotion',
  'operations': 'sidebar.operations',
  'incidents': 'sidebar.incidents',
  'runbooks': 'sidebar.runbooks',
  'reliability': 'sidebar.reliability',
  'ai': 'sidebar.aiHub',
  'assistant': 'sidebar.aiAssistant',
  'models': 'sidebar.modelRegistry',
  'policies': 'sidebar.aiPolicies',
  'ide': 'sidebar.ide',
  'governance': 'sidebar.governance',
  'executive': 'sidebar.executiveOverview',
  'reports': 'sidebar.reports',
  'risk': 'sidebar.riskCenter',
  'compliance': 'sidebar.compliance',
  'finops': 'sidebar.finops',
  'users': 'sidebar.users',
  'audit': 'sidebar.audit',
  'licensing': 'sidebar.licensing',
  'break-glass': 'sidebar.breakGlass',
  'jit-access': 'sidebar.jitAccess',
  'delegations': 'sidebar.delegations',
  'access-reviews': 'sidebar.accessReview',
  'my-sessions': 'sidebar.mySessions',
  'portal': 'sidebar.developerPortal',
  'source-of-truth': 'sidebar.sourceOfTruth',
  'search': 'commandPalette.globalSearch.title',
  'graph': 'sidebar.dependencyGraph',
  'studio': 'sidebar.contractStudio',
  'heatmap': 'sidebar.riskHeatmap',
  'maturity': 'sidebar.maturityScorecards',
  'benchmarking': 'sidebar.benchmarking',
  'vendor': 'sidebar.vendorLicensing',
};

const UUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function isUuid(segment: string): boolean {
  return UUID_REGEX.test(segment);
}

function capitalize(value: string): string {
  return value.charAt(0).toUpperCase() + value.slice(1);
}

interface BreadcrumbEntry {
  label: string;
  path: string;
}

/**
 * Breadcrumbs sensíveis ao contexto da rota atual.
 * Cada segmento do path é mapeado para a label i18n correspondente.
 * Segmentos UUID são ignorados (parâmetros de ID).
 */
export function Breadcrumbs() {
  const { pathname } = useLocation();
  const { t } = useTranslation();

  const rawSegments = pathname.split('/').filter(Boolean);
  const segments = rawSegments.filter((s) => !isUuid(s));

  if (segments.length === 0) return null;

  const crumbs: BreadcrumbEntry[] = [
    { label: t('breadcrumbs.home'), path: '/' },
  ];

  let accumulated = '';
  for (const segment of segments) {
    accumulated += `/${segment}`;
    const i18nKey = segmentLabels[segment];
    const label = i18nKey ? t(i18nKey) : capitalize(segment);
    crumbs.push({ label, path: accumulated });
  }

  return (
    <nav aria-label="Breadcrumbs" className="flex items-center gap-1 px-6 pt-3 pb-1 text-xs">
      {crumbs.map((crumb, index) => {
        const isLast = index === crumbs.length - 1;
        return (
          <span key={crumb.path} className="flex items-center gap-1">
            {index > 0 && (
              <ChevronRight className="h-3 w-3 text-muted" aria-hidden="true" />
            )}
            {isLast ? (
              <span className="text-heading font-medium">{crumb.label}</span>
            ) : (
              <Link
                to={crumb.path}
                className="text-accent hover:text-accent/80 transition-colors"
              >
                {crumb.label}
              </Link>
            )}
          </span>
        );
      })}
    </nav>
  );
}
