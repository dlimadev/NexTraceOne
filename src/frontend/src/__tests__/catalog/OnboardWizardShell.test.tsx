import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { OnboardWizardShell } from '../../features/catalog/onboard/OnboardWizardShell';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (_k: string, d?: string) => d ?? _k }),
}));

const steps = [
  { id: 'identity' as const, label: 'Identity' },
  { id: 'interface' as const, label: 'Interface', optional: true },
  { id: 'contract' as const, label: 'Contract', optional: true },
  { id: 'review' as const, label: 'Review' },
];

function renderShell(overrides = {}) {
  const props = {
    title: 'Onboard', steps, activeStep: 'interface' as const,
    preview: <div>preview</div>, children: <div>content</div>,
    canGoNext: true, isFirstStep: false, isLastStep: false,
    canSkip: true, pending: false,
    onBack: vi.fn(), onNext: vi.fn(), onSkip: vi.fn(), onCancel: vi.fn(),
    ...overrides,
  };
  render(<OnboardWizardShell {...props} />);
  return props;
}

describe('OnboardWizardShell', () => {
  it('shows Skip on optional steps and fires onSkip', () => {
    const props = renderShell();
    fireEvent.click(screen.getByRole('button', { name: /skip/i }));
    expect(props.onSkip).toHaveBeenCalled();
  });

  it('hides Skip when canSkip is false', () => {
    renderShell({ canSkip: false });
    expect(screen.queryByRole('button', { name: /skip/i })).not.toBeInTheDocument();
  });

  it('disables Next when canGoNext is false', () => {
    renderShell({ canGoNext: false });
    expect(screen.getByRole('button', { name: /next/i })).toBeDisabled();
  });

  it('shows Finish on the last step', () => {
    renderShell({ activeStep: 'review', isLastStep: true, canSkip: false });
    expect(screen.getByRole('button', { name: /finish/i })).toBeInTheDocument();
  });
});
