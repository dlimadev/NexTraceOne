import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { RestOperationsPreview } from '../../features/contracts/studio/components/previews/RestOperationsPreview';

const VALID_YAML = `
openapi: 3.1.0
info:
  title: Test API
  version: 1.0.0
paths:
  /users:
    get:
      summary: List users
    post:
      summary: Create user
  /users/{id}:
    get:
      summary: Get user by ID
    delete:
      summary: Delete user
`;

describe('RestOperationsPreview', () => {
  it('renders paths and methods from valid OpenAPI YAML', () => {
    render(<RestOperationsPreview content={VALID_YAML} />);
    expect(screen.getByText('/users')).toBeInTheDocument();
    expect(screen.getByText('/users/{id}')).toBeInTheDocument();
    expect(screen.getAllByText('GET').length).toBeGreaterThan(0);
    expect(screen.getByText('List users')).toBeInTheDocument();
  });

  it('shows operation count in footer', () => {
    render(<RestOperationsPreview content={VALID_YAML} />);
    expect(screen.getByText(/operations/i)).toBeInTheDocument();
  });

  it('renders empty state for invalid YAML without throwing', () => {
    render(<RestOperationsPreview content="invalid: {{{" />);
    expect(screen.getByTestId('preview-empty')).toBeInTheDocument();
  });

  it('renders empty state for YAML with no paths', () => {
    render(<RestOperationsPreview content="openapi: 3.1.0\ninfo:\n  title: Empty" />);
    expect(screen.getByTestId('preview-empty')).toBeInTheDocument();
  });
});
