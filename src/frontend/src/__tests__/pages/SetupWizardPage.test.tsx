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

/** Navigate forward N steps from the start. */
async function advanceSteps(user: ReturnType<typeof userEvent.setup>, n: number) {
  for (let i = 0; i < n; i++) {
    await user.click(screen.getByRole('button', { name: /Next/i }));
  }
}

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
    await advanceSteps(user, 4);
    expect(screen.getByRole('button', { name: /Skip/i })).toBeDefined();
  });

  it('shows step progress indicator', () => {
    renderPage();
    expect(screen.getByText(/Step 1 of 7/i)).toBeDefined();
  });

  describe('AI step — deployment mode selector', () => {
    it('shows AI configuration step with mode selector', async () => {
      renderPage();
      const user = userEvent.setup();
      await advanceSteps(user, 4);
      expect(screen.getByText(/AI Configuration/i)).toBeDefined();
      expect(screen.getByText(/Ollama Deployment Mode/i)).toBeDefined();
    });

    it('shows three mode options: local, remote, disabled', async () => {
      renderPage();
      const user = userEvent.setup();
      await advanceSteps(user, 4);
      expect(screen.getByTestId('ai-mode-local')).toBeDefined();
      expect(screen.getByTestId('ai-mode-remote')).toBeDefined();
      expect(screen.getByTestId('ai-mode-disabled')).toBeDefined();
    });

    it('local mode is selected by default and shows URL field', async () => {
      renderPage();
      const user = userEvent.setup();
      await advanceSteps(user, 4);
      const localBtn = screen.getByTestId('ai-mode-local');
      expect(localBtn.getAttribute('aria-pressed')).toBe('true');
      expect(screen.getByDisplayValue('http://localhost:11434')).toBeDefined();
    });

    it('selecting remote mode shows remote URL placeholder', async () => {
      renderPage();
      const user = userEvent.setup();
      await advanceSteps(user, 4);
      await user.click(screen.getByTestId('ai-mode-remote'));
      expect(screen.getByTestId('ai-mode-remote').getAttribute('aria-pressed')).toBe('true');
      expect(screen.getByText(/Remote Ollama URL/i)).toBeDefined();
    });

    it('selecting disabled mode hides URL fields and shows note', async () => {
      renderPage();
      const user = userEvent.setup();
      await advanceSteps(user, 4);
      await user.click(screen.getByTestId('ai-mode-disabled'));
      expect(screen.getByTestId('ai-mode-disabled').getAttribute('aria-pressed')).toBe('true');
      expect(screen.queryByText(/Ollama URL/i)).toBeNull();
    });

    it('switching from remote back to local restores localhost URL', async () => {
      renderPage();
      const user = userEvent.setup();
      await advanceSteps(user, 4);
      await user.click(screen.getByTestId('ai-mode-remote'));
      await user.click(screen.getByTestId('ai-mode-local'));
      expect(screen.getByDisplayValue('http://localhost:11434')).toBeDefined();
    });
  });
});
