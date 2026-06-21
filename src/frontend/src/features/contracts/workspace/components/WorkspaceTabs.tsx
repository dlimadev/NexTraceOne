import { useTranslation } from 'react-i18next';
import { cn } from '../../../../lib/cn';
import { Tabs } from '../../../../components/Tabs';
import { WORKSPACE_SECTIONS, WORKSPACE_SECTION_GROUPS } from '../../shared/constants';
import type { WorkspaceSectionId } from '../../types';

interface WorkspaceTabsProps {
  activeSection: WorkspaceSectionId;
  onSelect: (section: WorkspaceSectionId) => void;
}

/** Navegação em dois níveis: grupos (primário) → secções do grupo (chips secundários). */
export function WorkspaceTabs({ activeSection, onSelect }: WorkspaceTabsProps) {
  const { t } = useTranslation();
  const activeGroup = WORKSPACE_SECTIONS.find((s) => s.id === activeSection)?.group
    ?? WORKSPACE_SECTION_GROUPS[0]!.key;
  const sectionsInGroup = WORKSPACE_SECTIONS.filter((s) => s.group === activeGroup);

  return (
    <div className="mb-5">
      <Tabs
        items={WORKSPACE_SECTION_GROUPS.map((g) => ({ id: g.key, label: t(g.labelKey, g.key) }))}
        activeId={activeGroup}
        onChange={(groupKey) => {
          const first = WORKSPACE_SECTIONS.find((s) => s.group === groupKey);
          if (first) onSelect(first.id);
        }}
        className="overflow-x-auto"
      />
      <div aria-label={t('contracts.workspace.sectionNav', 'Sections')} className="flex flex-wrap gap-1.5 mt-3">
        {sectionsInGroup.map((section) => {
          const isActive = section.id === activeSection;
          return (
            <button
              key={section.id}
              type="button"
              onClick={() => onSelect(section.id)}
              className={cn(
                'px-3 py-1 text-xs font-medium rounded-md border transition-colors',
                isActive ? 'bg-accent text-white border-accent' : 'bg-card text-muted border-edge hover:text-heading hover:border-edge-strong',
              )}
            >
              {t(section.labelKey, section.id)}
            </button>
          );
        })}
      </div>
    </div>
  );
}
