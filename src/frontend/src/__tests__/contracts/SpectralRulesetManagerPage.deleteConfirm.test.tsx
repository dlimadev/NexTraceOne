import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { SpectralRulesetManagerPage } from '../../features/contracts/spectral/SpectralRulesetManagerPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));
const ruleset = { id: 'rs-1', name: 'core-rules', description: 'd', rulesetType: 'Custom', isDefault: false, isActive: true, createdAt: '2026-01-01T00:00:00Z', content: '' };
const deleteMutate = vi.fn();
vi.mock('../../features/contracts/hooks', () => ({
  useSpectralRulesets: () => ({ data: { items: [ruleset] }, isLoading: false, isError: false, refetch: vi.fn() }),
  useToggleSpectralRuleset: () => ({ mutate: vi.fn(), isPending: false }),
  useDeleteSpectralRuleset: () => ({ mutate: deleteMutate, isPending: false }),
  useCreateSpectralRuleset: () => ({ mutate: vi.fn(), isPending: false }),
}));

describe('SpectralRulesetManagerPage delete confirmation', () => {
  it('requires confirmation before deleting a ruleset', () => {
    render(<SpectralRulesetManagerPage />);
    // Clicar no delete da linha não elimina imediatamente — abre confirmação.
    fireEvent.click(screen.getByRole('button', { name: 'Delete' }));
    expect(deleteMutate).not.toHaveBeenCalled();
    // Com o modal aberto há dois "Delete" (linha + confirmação); confirmar dispara a mutation.
    const allDelete = screen.getAllByRole('button', { name: 'Delete' });
    fireEvent.click(allDelete[allDelete.length - 1]);
    expect(deleteMutate).toHaveBeenCalledWith('rs-1', expect.anything());
  });
});
