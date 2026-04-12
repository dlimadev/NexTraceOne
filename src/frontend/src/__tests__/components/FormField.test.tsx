import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { FormField } from '../../components/FormField';

describe('FormField', () => {
  it('renders label', () => {
    render(
      <FormField label="Email">
        <input type="email" />
      </FormField>,
    );
    expect(screen.getByText('Email')).toBeInTheDocument();
  });

  it('shows required indicator', () => {
    render(
      <FormField label="Name" required>
        <input />
      </FormField>,
    );
    expect(screen.getByText('*')).toBeInTheDocument();
  });

  it('shows error message with role="alert"', () => {
    render(
      <FormField label="Email" error="Invalid email">
        <input />
      </FormField>,
    );
    expect(screen.getByRole('alert')).toHaveTextContent('Invalid email');
  });

  it('shows helper text when no error', () => {
    render(
      <FormField label="Password" helperText="Min 8 characters">
        <input />
      </FormField>,
    );
    expect(screen.getByText('Min 8 characters')).toBeInTheDocument();
  });

  it('hides helper text when error is present', () => {
    render(
      <FormField label="Password" helperText="Min 8 characters" error="Too short">
        <input />
      </FormField>,
    );
    expect(screen.queryByText('Min 8 characters')).not.toBeInTheDocument();
    expect(screen.getByText('Too short')).toBeInTheDocument();
  });

  it('sets htmlFor on label', () => {
    render(
      <FormField label="Username" htmlFor="username-input">
        <input id="username-input" />
      </FormField>,
    );
    const label = screen.getByText('Username');
    expect(label).toHaveAttribute('for', 'username-input');
  });
});
