import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { ReleaseControlParametersPage } from '../../features/change-governance/pages/ReleaseControlParametersPage';

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

function renderPage() {
  return render(
    <MemoryRouter>
      <ReleaseControlParametersPage />
    </MemoryRouter>,
  );
}

describe('ReleaseControlParametersPage', () => {
  it('renders the page title', () => {
    renderPage();
    expect(screen.getByText(/Release Control Parameters/i)).toBeTruthy();
  });

  it('renders System Parameters section', () => {
    renderPage();
    expect(screen.getByText(/System Parameters/i)).toBeTruthy();
  });

  it('renders save changes button', () => {
    renderPage();
    expect(screen.getByText(/Save Changes/i)).toBeTruthy();
  });

  it('renders all 7 system parameters', () => {
    renderPage();
    expect(screen.getByText('release.require_release_for_production')).toBeTruthy();
    expect(screen.getByText('release.require_release_for_preprod')).toBeTruthy();
    expect(screen.getByText('release.auto_assign_commits_on_promotion')).toBeTruthy();
    expect(screen.getByText('release.allow_external_ingest')).toBeTruthy();
    expect(screen.getByText('release.approval_callback_token_expiry_hours')).toBeTruthy();
    expect(screen.getByText('release.allow_po_remove_workitems')).toBeTruthy();
    expect(screen.getByText('release.block_promotion_if_commits_unassigned')).toBeTruthy();
  });

  it('renders External Approval Callback section', () => {
    renderPage();
    expect(screen.getByText(/External Approval Callback/i)).toBeTruthy();
  });

  it('renders callback endpoint info', () => {
    renderPage();
    expect(screen.getByText(/api\/v1\/releases/i)).toBeTruthy();
  });

  it('renders page subtitle', () => {
    renderPage();
    expect(screen.getByText(/System-level parameters governing/i)).toBeTruthy();
  });

  it('save button shows Saved confirmation after click', () => {
    renderPage();
    const btn = screen.getByText(/Save Changes/i);
    fireEvent.click(btn);
    expect(screen.getByText(/Saved!/i)).toBeTruthy();
  });

  it('renders boolean toggles for boolean params', () => {
    renderPage();
    const checkboxes = screen.getAllByRole('checkbox');
    expect(checkboxes.length).toBeGreaterThanOrEqual(5);
  });

  it('renders number input for expiry hours param', () => {
    renderPage();
    const numberInputs = screen.getAllByRole('spinbutton');
    expect(numberInputs.length).toBeGreaterThanOrEqual(1);
  });
});
