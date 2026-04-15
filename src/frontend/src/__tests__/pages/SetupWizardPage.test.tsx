import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { SetupWizardPage } from '../../features/platform-admin/pages/SetupWizardPage';

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

const renderPage = () =>
  render(
    <MemoryRouter>
      <SetupWizardPage />
    </MemoryRouter>,
  );

describe('SetupWizardPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the setup wizard title', () => {
    renderPage();
    expect(screen.getByText(/NexTraceOne Setup Wizard/i)).toBeDefined();
  });

  it('starts on the welcome step', () => {
    renderPage();
    expect(screen.getByText(/Welcome to NexTraceOne/i)).toBeDefined();
  });

  it('navigates to the next step when clicking Next', async () => {
    renderPage();
    const user = userEvent.setup();
    await user.click(screen.getByRole('button', { name: /Next/i }));
    expect(screen.getByText(/Database Configuration/i)).toBeDefined();
  });

  it('disables Back button on the first step', () => {
    renderPage();
    const backBtn = screen.getByRole('button', { name: /Back/i });
    expect(backBtn).toBeDefined();
    expect((backBtn as HTMLButtonElement).disabled).toBe(true);
  });

  it('navigates back from the second step', async () => {
    renderPage();
    const user = userEvent.setup();
    await user.click(screen.getByRole('button', { name: /Next/i }));
    await user.click(screen.getByRole('button', { name: /Back/i }));
    expect(screen.getByText(/Welcome to NexTraceOne/i)).toBeDefined();
  });

  it('shows Skip button for optional steps', async () => {
    renderPage();
    const user = userEvent.setup();
    // Navigate to AI step (index 4) through required steps
    for (let i = 0; i < 4; i++) {
      await user.click(screen.getByRole('button', { name: /Next/i }));
    }
    expect(screen.getByRole('button', { name: /Skip/i })).toBeDefined();
  });

  it('shows step progress indicator', () => {
    renderPage();
    expect(screen.getByText(/Step 1 of 7/i)).toBeDefined();
  });
});
