import { Outlet } from 'react-router-dom';
import { AreaSubNav, type AreaSubNavItem } from '../components/AreaSubNav';

const CONTRACTS_TABS: AreaSubNavItem[] = [
  { labelKey: 'contractsAreaNav.catalog', to: '/contracts', end: true },
  { labelKey: 'contractsAreaNav.governance', to: '/contracts/governance' },
  { labelKey: 'contractsAreaNav.health', to: '/contracts/health' },
  { labelKey: 'contractsAreaNav.rulesets', to: '/contracts/spectral' },
  { labelKey: 'contractsAreaNav.canonical', to: '/contracts/canonical' },
  { labelKey: 'contractsAreaNav.publication', to: '/contracts/publication' },
  { labelKey: 'contractsAreaNav.pipeline', to: '/contracts/pipeline' },
  { labelKey: 'contractsAreaNav.cdct', to: '/contracts/cdct' },
];

/** Layout de área do catálogo de contratos: sub-nav + conteúdo (Outlet). */
export function ContractsAreaLayout() {
  return (
    <div className="flex flex-col h-full">
      <AreaSubNav items={CONTRACTS_TABS} ariaLabelKey="contractsAreaNav.ariaLabel" />
      <div className="flex-1 min-h-0 overflow-y-auto">
        <Outlet />
      </div>
    </div>
  );
}
