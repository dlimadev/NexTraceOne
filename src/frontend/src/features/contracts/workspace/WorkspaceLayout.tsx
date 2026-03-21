import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  LayoutDashboard,
  FileText,
  Code,
  GitBranch,
  Database,
  Shield,
  GitCompare,
  History,
  CheckCircle,
  ShieldCheck,
  Users,
  Network,
  ScanSearch,
  Bot,
} from 'lucide-react';
import { cn } from '../../../lib/cn';
import { WORKSPACE_SECTIONS, WORKSPACE_SECTION_GROUPS } from '../shared/constants';
import type { WorkspaceSectionId } from '../types/workspace';

const SECTION_ICONS: Record<WorkspaceSectionId, React.ComponentType<{ size?: number; className?: string }>> = {
  summary: LayoutDashboard,
  definition: FileText,
  contract: Code,
  operations: GitBranch,
  schemas: Database,
  security: Shield,
  validation: ScanSearch,
  versioning: GitCompare,
  changelog: History,
  approvals: CheckCircle,
  compliance: ShieldCheck,
  consumers: Users,
  dependencies: Network,
  'ai-agents': Bot,
};

interface WorkspaceLayoutProps {
  initialSection?: WorkspaceSectionId;
  children: (activeSection: WorkspaceSectionId, navigate: (section: WorkspaceSectionId) => void) => React.ReactNode;
  header?: React.ReactNode;
  rail?: React.ReactNode;
  className?: string;
}

/**
 * Layout principal do workspace de contrato.
 * Navegação contextual à esquerda (agrupada), conteúdo central, rail à direita.
 * Alinhado com NTO design system: largura, motion, tokens.
 */
export function WorkspaceLayout({
  initialSection = 'summary',
  children,
  header,
  rail,
  className,
}: WorkspaceLayoutProps) {
  const { t } = useTranslation();
  const [activeSection, setActiveSection] = useState<WorkspaceSectionId>(initialSection);

  return (
    <div className={cn('flex flex-col h-full', className)}>
      {header}

      <div className="flex flex-1 min-h-0">
        {/* ── Sidebar Navigation ── */}
        <nav className="w-56 flex-shrink-0 border-r border-edge bg-panel overflow-y-auto">
          <div className="py-3">
            {WORKSPACE_SECTION_GROUPS.map((group) => {
              const sections = WORKSPACE_SECTIONS.filter((s) => s.group === group.key);
              if (sections.length === 0) return null;

              return (
                <div key={group.key} className="mb-2">
                  <p className="px-4 py-1.5 text-[10px] font-semibold uppercase tracking-wider text-muted/60">
                    {t(group.labelKey, group.key)}
                  </p>
                  <ul>
                    {sections.map((section) => {
                      const Icon = SECTION_ICONS[section.id];
                      const isActive = activeSection === section.id;

                      return (
                        <li key={section.id}>
                          <button
                            onClick={() => setActiveSection(section.id)}
                            className={cn(
                              'w-full flex items-center gap-2.5 px-4 py-2 text-xs font-medium transition-colors duration-fast',
                              isActive
                                ? 'text-accent bg-accent/10 border-l-2 border-accent'
                                : 'text-muted hover:text-heading hover:bg-elevated/50 border-l-2 border-transparent',
                            )}
                          >
                            <Icon size={14} className={isActive ? 'text-accent' : ''} />
                            {t(section.labelKey)}
                          </button>
                        </li>
                      );
                    })}
                  </ul>
                </div>
              );
            })}
          </div>
        </nav>

        {/* ── Main Content ── */}
        <main className="flex-1 min-w-0 overflow-y-auto p-6">
          {children(activeSection, setActiveSection)}
        </main>

        {/* ── Right Rail (optional) ── */}
        {rail && (
          <aside className="w-72 flex-shrink-0 border-l border-edge bg-panel overflow-y-auto p-4">
            {rail}
          </aside>
        )}
      </div>
    </div>
  );
}
