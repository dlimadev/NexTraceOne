import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { WizardOverlay } from '../../features/catalog/components/WizardOverlay';
import { Fingerprint, LayoutGrid } from 'lucide-react';

const STEPS = [
  { id: 'step1', labelKey: 'catalog.registration.step.identity', icon: Fingerprint },
  { id: 'step2', labelKey: 'catalog.registration.step.classification', icon: LayoutGrid },
];

function renderOverlay(overrides = {}) {
  const props = {
    title: 'Test Wizard',
    headerIcon: <Fingerprint size={20} />,
    steps: STEPS,
    currentStep: 1,
    onClose: vi.fn(),
    onBack: vi.fn(),
    onNext: vi.fn(),
    onSubmit: vi.fn(),
    isLastStep: false,
    children: <div>Step content</div>,
    ...overrides,
  };
  return { ...render(<WizardOverlay {...props} />), props };
}

describe('WizardOverlay', () => {
  it('renders title and children', () => {
    renderOverlay();
    expect(screen.getByText('Test Wizard')).toBeInTheDocument();
    expect(screen.getByText('Step content')).toBeInTheDocument();
  });

  it('Back button is disabled on step 1', () => {
    renderOverlay({ currentStep: 1 });
    const backBtn = screen.getByRole('button', { name: /back|voltar/i });
    expect(backBtn).toBeDisabled();
  });

  it('Back button is enabled on step 2', () => {
    const { props } = renderOverlay({ currentStep: 2 });
    const backBtn = screen.getByRole('button', { name: /back|voltar/i });
    expect(backBtn).not.toBeDisabled();
    fireEvent.click(backBtn);
    expect(props.onBack).toHaveBeenCalledOnce();
  });

  it('clicking Next calls onNext', async () => {
    const user = userEvent.setup();
    const { props } = renderOverlay({ isLastStep: false });
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    expect(props.onNext).toHaveBeenCalledOnce();
  });

  it('shows Submit on last step instead of Next', () => {
    renderOverlay({ isLastStep: true, currentStep: 2 });
    expect(screen.queryByRole('button', { name: /^next$|^avançar$/i })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: /submit|salvar|guardar/i })).toBeInTheDocument();
  });

  it('Next button is disabled when isNextDisabled=true', () => {
    renderOverlay({ isNextDisabled: true });
    const nextBtn = screen.getByRole('button', { name: /next|avançar/i });
    expect(nextBtn).toBeDisabled();
  });

  it('pressing Escape calls onClose', () => {
    const { props } = renderOverlay();
    fireEvent.keyDown(document, { key: 'Escape' });
    expect(props.onClose).toHaveBeenCalledOnce();
  });

  it('clicking the X button calls onClose', async () => {
    const user = userEvent.setup();
    const { props } = renderOverlay();
    await user.click(screen.getByRole('button', { name: /close|fechar/i }));
    expect(props.onClose).toHaveBeenCalledOnce();
  });

  it('clicking the backdrop calls onClose', async () => {
    const user = userEvent.setup();
    const { props } = renderOverlay();
    // The backdrop is the first child of the root div — it has aria-hidden="true"
    const backdrop = document.querySelector('[aria-hidden="true"]') as HTMLElement;
    await user.click(backdrop);
    expect(props.onClose).toHaveBeenCalledOnce();
  });
});
