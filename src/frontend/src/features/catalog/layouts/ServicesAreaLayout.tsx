import { Outlet } from 'react-router-dom';
import { AreaSubNav, type AreaSubNavItem } from '../components/AreaSubNav';

const SERVICES_TABS: AreaSubNavItem[] = [
  { labelKey: 'catalogAreaNav.catalog', to: '/services', end: true },
  { labelKey: 'catalogAreaNav.graph', to: '/services/graph' },
  { labelKey: 'catalogAreaNav.discovery', to: '/services/discovery' },
  { labelKey: 'catalogAreaNav.maturity', to: '/services/maturity' },
  { labelKey: 'catalogAreaNav.experience', to: '/services/experience' },
  { labelKey: 'catalogAreaNav.featureFlags', to: '/services/feature-flags' },
  { labelKey: 'catalogAreaNav.legacy', to: '/services/legacy' },
];

/** Layout de área do catálogo de serviços: sub-nav + conteúdo (Outlet). */
export function ServicesAreaLayout() {
  return (
    <div className="flex flex-col h-full">
      <AreaSubNav items={SERVICES_TABS} ariaLabelKey="catalogAreaNav.ariaLabel" />
      <div className="flex-1 min-h-0 overflow-y-auto">
        <Outlet />
      </div>
    </div>
  );
}
