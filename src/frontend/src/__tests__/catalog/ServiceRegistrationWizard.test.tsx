/**
 * FUTURE-ROADMAP 6.1 — Testes de componente para ServiceRegistrationWizard.
 * Cobrem navegação entre passos e validação de campos obrigatórios.
 */
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ServiceRegistrationWizard } from '../../features/catalog/components/ServiceRegistrationWizard';

// i18n é inicializado pelo setup.ts (importa ../i18n)

function renderWizard(onSubmit = vi.fn(), onCancel = vi.fn()) {
  return render(<ServiceRegistrationWizard onSubmit={onSubmit} onCancel={onCancel} />);
}

describe('ServiceRegistrationWizard', () => {
  it('renders step 1 (Identity) on initial mount', () => {
    renderWizard();
    // The name input placeholder is present in step 1
    expect(screen.getByPlaceholderText(/payment-service/i)).toBeInTheDocument();
  });

  it('calls onCancel when Cancel button is clicked', async () => {
    const user = userEvent.setup();
    const onCancel = vi.fn();
    renderWizard(vi.fn(), onCancel);
    await user.click(screen.getByRole('button', { name: /cancel/i }));
    expect(onCancel).toHaveBeenCalledOnce();
  });

  it('shows validation error when trying to advance without name on step 1', async () => {
    const user = userEvent.setup();
    renderWizard();
    // Try to click Next without filling name
    await user.click(screen.getByRole('button', { name: /next/i }));
    // Error message for name should appear
    expect(screen.getByText(/name is required/i)).toBeInTheDocument();
  });

  it('shows validation error when trying to advance without domain on step 1', async () => {
    const user = userEvent.setup();
    renderWizard();
    // Fill name but leave domain empty
    const nameInput = screen.getByPlaceholderText(/payment-service/i);
    await user.type(nameInput, 'my-service');
    await user.click(screen.getByRole('button', { name: /next/i }));
    expect(screen.getByText(/domain is required/i)).toBeInTheDocument();
  });

  it('advances to step 2 when step 1 fields are valid', async () => {
    const user = userEvent.setup();
    renderWizard();
    // Fill required step 1 fields
    await user.type(screen.getByPlaceholderText(/payment-service/i), 'my-service');
    await user.type(screen.getByPlaceholderText(/payments, identity/i), 'Payments');
    await user.click(screen.getByRole('button', { name: /next/i }));
    // Step 2 shows classification fields (Criticality select)
    expect(await screen.findByText(/criticality/i)).toBeInTheDocument();
  });

  it('can navigate back from step 2 to step 1', async () => {
    const user = userEvent.setup();
    renderWizard();
    // Advance to step 2
    await user.type(screen.getByPlaceholderText(/payment-service/i), 'my-service');
    await user.type(screen.getByPlaceholderText(/payments, identity/i), 'Payments');
    await user.click(screen.getByRole('button', { name: /next/i }));
    // Go back
    await user.click(screen.getByRole('button', { name: /back/i }));
    // Name input should be visible again
    expect(screen.getByPlaceholderText(/payment-service/i)).toBeInTheDocument();
  });

  it('calls onSubmit with form data when wizard is completed', async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn();
    renderWizard(onSubmit);

    // Step 1: Identity
    await user.type(screen.getByPlaceholderText(/payment-service/i), 'test-service');
    await user.type(screen.getByPlaceholderText(/payments, identity/i), 'Finance');
    await user.click(screen.getByRole('button', { name: /next/i }));

    // Step 2: Classification — advance without changes (defaults are set)
    await user.click(screen.getByRole('button', { name: /next/i }));

    // Step 3: Ownership — fill team
    const teamInput = screen.getByPlaceholderText(/platform-team/i);
    await user.type(teamInput, 'Finance Team');
    await user.click(screen.getByRole('button', { name: /next/i }));

    // Step 4: References — advance without changes
    await user.click(screen.getByRole('button', { name: /next/i }));

    // Step 5: Confirmation — click Register
    await user.click(screen.getByRole('button', { name: /register/i }));

    expect(onSubmit).toHaveBeenCalledOnce();
    expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({
      name: 'test-service',
      domain: 'Finance',
      team: 'Finance Team',
    }));
  });
});
