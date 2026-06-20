import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, d?: string) => d ?? k }) }));
import { WorkspaceTabs } from '../../features/contracts/workspace/components/WorkspaceTabs';

describe('WorkspaceTabs', () => {
  it('renders the five group tabs', () => {
    render(<WorkspaceTabs activeSection="summary" onSelect={vi.fn()} />);
    ['overview', 'contract', 'governance', 'relationships', 'ai'].forEach((g) => {
      expect(screen.getByRole('tab', { name: new RegExp(g, 'i') })).toBeInTheDocument();
    });
  });
  it('clicking a group selects that group\'s first section', () => {
    const onSelect = vi.fn();
    render(<WorkspaceTabs activeSection="summary" onSelect={onSelect} />);
    fireEvent.click(screen.getByRole('tab', { name: /governance/i }));
    expect(onSelect).toHaveBeenCalledWith('validation');
  });
});
