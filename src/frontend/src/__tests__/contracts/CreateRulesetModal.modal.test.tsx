import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { CreateRulesetModal } from '../../features/contracts/spectral/CreateRulesetModal';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: unknown) => (typeof f === 'string' ? f : k) }) }));

describe('CreateRulesetModal (DS Modal)', () => {
  it('não renderiza conteúdo quando fechado', () => {
    render(<CreateRulesetModal isOpen={false} onClose={vi.fn()} onSubmit={vi.fn()} isSubmitting={false} />);
    expect(screen.queryByPlaceholderText('e.g., api-naming-conventions')).not.toBeInTheDocument();
  });

  it('renderiza como DS Modal e submete o payload válido', () => {
    const onSubmit = vi.fn();
    render(<CreateRulesetModal isOpen onClose={vi.fn()} onSubmit={onSubmit} isSubmitting={false} />);
    // campo de nome presente prova que o modal está aberto
    const nameField = screen.getByPlaceholderText('e.g., api-naming-conventions');
    expect(nameField).toBeInTheDocument();
    fireEvent.change(nameField, { target: { value: 'my-rules' } });
    fireEvent.change(screen.getByPlaceholderText('Paste your Spectral ruleset content here (JSON or YAML)...'), { target: { value: 'rules: {}' } });
    // submeter via botão do footer do DS Modal
    fireEvent.click(screen.getByRole('button', { name: 'Create Ruleset' }));
    expect(onSubmit).toHaveBeenCalledWith({
      name: 'my-rules', description: '', content: 'rules: {}', rulesetType: 'Custom',
    });
  });
});
