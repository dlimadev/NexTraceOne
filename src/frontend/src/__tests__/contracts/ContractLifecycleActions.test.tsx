import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, d?: string) => d ?? k }) }));
import { ContractLifecycleActions } from '../../features/contracts/workspace/components/ContractLifecycleActions';

describe('ContractLifecycleActions', () => {
  it('renders an Export button and calls onExport', () => {
    const onExport = vi.fn();
    render(<ContractLifecycleActions lifecycleState="Draft" isLocked={false} onTransition={vi.fn()} onExport={onExport} />);
    fireEvent.click(screen.getByRole('button', { name: /export/i }));
    expect(onExport).toHaveBeenCalled();
  });
  it('renders a transition button for the current lifecycle state and calls onTransition', () => {
    const onTransition = vi.fn();
    render(<ContractLifecycleActions lifecycleState="Draft" isLocked={false} onTransition={onTransition} onExport={vi.fn()} />);
    const buttons = screen.getAllByRole('button');
    // the non-Export button is the Draft->InReview transition
    const transitionBtn = buttons.find((b) => !/export/i.test(b.textContent || ''));
    expect(transitionBtn).toBeDefined();
    fireEvent.click(transitionBtn!);
    expect(onTransition).toHaveBeenCalledWith('InReview');
  });
});
