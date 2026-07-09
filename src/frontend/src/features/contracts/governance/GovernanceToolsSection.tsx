import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  Stethoscope, LineChart, ListChecks, FlaskConical, Boxes, GitBranch,
  Send, ArrowRightLeft, PlayCircle, ArrowRight,
} from 'lucide-react';

interface Tool { to: string; icon: React.ReactNode; titleKey: string; titleFallback: string; subKey: string; subFallback: string; }
interface ToolGroup { groupKey: string; groupFallback: string; tools: Tool[]; }

const GROUPS: ToolGroup[] = [
  {
    groupKey: 'contracts.governance.tools.groups.assess', groupFallback: 'Assess',
    tools: [
      { to: '/contracts/health', icon: <Stethoscope size={18} />, titleKey: 'contracts.governance.tools.items.healthDashboard.title', titleFallback: 'Health dashboard', subKey: 'contracts.governance.tools.items.healthDashboard.subtitle', subFallback: 'Aggregated quality score and violations' },
      { to: '/contracts/health/timeline', icon: <LineChart size={18} />, titleKey: 'contracts.governance.tools.items.healthTimeline.title', titleFallback: 'Health timeline', subKey: 'contracts.governance.tools.items.healthTimeline.subtitle', subFallback: 'Quality trend across versions' },
    ],
  },
  {
    groupKey: 'contracts.governance.tools.groups.enforce', groupFallback: 'Enforce',
    tools: [
      { to: '/contracts/spectral', icon: <ListChecks size={18} />, titleKey: 'contracts.governance.tools.items.spectral.title', titleFallback: 'Spectral rulesets', subKey: 'contracts.governance.tools.items.spectral.subtitle', subFallback: 'Lint rules applied to contracts' },
      { to: '/contracts/cdct', icon: <FlaskConical size={18} />, titleKey: 'contracts.governance.tools.items.cdct.title', titleFallback: 'Consumer-driven contracts', subKey: 'contracts.governance.tools.items.cdct.subtitle', subFallback: 'Consumer expectations and verification' },
    ],
  },
  {
    groupKey: 'contracts.governance.tools.groups.model', groupFallback: 'Model',
    tools: [
      { to: '/contracts/canonical', icon: <Boxes size={18} />, titleKey: 'contracts.governance.tools.items.canonical.title', titleFallback: 'Canonical entities', subKey: 'contracts.governance.tools.items.canonical.subtitle', subFallback: 'Reusable standardized schemas' },
      { to: '/contracts/canonical/impact-cascade', icon: <GitBranch size={18} />, titleKey: 'contracts.governance.tools.items.impactCascade.title', titleFallback: 'Impact cascade', subKey: 'contracts.governance.tools.items.impactCascade.subtitle', subFallback: 'Blast radius of an entity change' },
    ],
  },
  {
    groupKey: 'contracts.governance.tools.groups.publish', groupFallback: 'Publish',
    tools: [
      { to: '/contracts/publication', icon: <Send size={18} />, titleKey: 'contracts.governance.tools.items.publication.title', titleFallback: 'Publication center', subKey: 'contracts.governance.tools.items.publication.subtitle', subFallback: 'Publish and promote contract versions' },
      { to: '/contracts/migration', icon: <ArrowRightLeft size={18} />, titleKey: 'contracts.governance.tools.items.migration.title', titleFallback: 'Migration', subKey: 'contracts.governance.tools.items.migration.subtitle', subFallback: 'Generate migration patches between versions' },
    ],
  },
  {
    groupKey: 'contracts.governance.tools.groups.test', groupFallback: 'Test',
    tools: [
      { to: '/contracts/playground', icon: <PlayCircle size={18} />, titleKey: 'contracts.governance.tools.items.playground.title', titleFallback: 'Playground', subKey: 'contracts.governance.tools.items.playground.subtitle', subFallback: 'Try requests against a contract' },
    ],
  },
];

/** Grelha de lançamento das ferramentas de qualidade/governança, agrupadas por intenção. Estático (honest-null, sem contagens). */
export function GovernanceToolsSection() {
  const { t } = useTranslation();
  return (
    <section className="mt-8">
      <h2 className="text-sm font-semibold text-heading mb-3">
        {t('contracts.governance.tools.title', 'Governance tools')}
      </h2>
      <div className="space-y-5">
        {GROUPS.map((group) => (
          <div key={group.groupKey}>
            <p className="text-xs font-medium uppercase tracking-wide text-muted mb-2">
              {t(group.groupKey, group.groupFallback)}
            </p>
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
              {group.tools.map((tool) => (
                <Link
                  key={tool.to}
                  to={tool.to}
                  className="group flex items-start gap-3 rounded-lg border border-edge bg-card p-4 shadow-sm transition-all hover:border-accent/40 hover:shadow-md focus:outline-none focus:ring-2 focus:ring-accent"
                >
                  <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-accent/10 text-accent group-hover:bg-accent/20 transition-colors">
                    {tool.icon}
                  </div>
                  <div className="min-w-0">
                    <p className="text-sm font-semibold text-heading group-hover:text-accent transition-colors">
                      {t(tool.titleKey, tool.titleFallback)}
                    </p>
                    <p className="mt-0.5 text-xs text-muted leading-snug">
                      {t(tool.subKey, tool.subFallback)}
                    </p>
                  </div>
                  <ArrowRight size={16} className="ml-auto shrink-0 text-muted group-hover:text-accent" />
                </Link>
              ))}
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}
