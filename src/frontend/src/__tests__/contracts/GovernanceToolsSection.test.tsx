import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { GovernanceToolsSection } from '../../features/contracts/governance/GovernanceToolsSection';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));

function wrap() {
  render(<MemoryRouter><GovernanceToolsSection /></MemoryRouter>);
}

describe('GovernanceToolsSection', () => {
  it('renders one link per tool with the correct route', () => {
    wrap();
    const hrefs = screen.getAllByRole('link').map((a) => a.getAttribute('href'));
    expect(hrefs).toEqual(expect.arrayContaining([
      '/contracts/health', '/contracts/health/timeline',
      '/contracts/spectral', '/contracts/cdct',
      '/contracts/canonical', '/contracts/canonical/impact-cascade',
      '/contracts/publication', '/contracts/migration',
      '/contracts/playground',
    ]));
    expect(hrefs).toHaveLength(9);
  });

  it('renders the five intent groups', () => {
    wrap();
    for (const g of ['Assess', 'Enforce', 'Model', 'Publish', 'Test']) {
      expect(screen.getByText(g)).toBeInTheDocument();
    }
  });
});
