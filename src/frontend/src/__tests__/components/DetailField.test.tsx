import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { DetailField } from '../../components/DetailField';

describe('DetailField', () => {
  it('renders label and value', () => {
    render(<DetailField label="Owner" value="Platform Team" />);
    expect(screen.getByText('Owner')).toBeInTheDocument();
    expect(screen.getByText('Platform Team')).toBeInTheDocument();
  });

  it('renders dash when value is undefined', () => {
    render(<DetailField label="Version" />);
    expect(screen.getByText('—')).toBeInTheDocument();
  });

  it('renders copy button when copyable', () => {
    render(<DetailField label="ID" value="abc-123" copyable />);
    expect(screen.getByRole('button', { name: /copy/i })).toBeInTheDocument();
  });

  it('renders as link when href is provided', () => {
    render(<DetailField label="Docs" value="Documentation" href="/docs" />);
    expect(screen.getByRole('link')).toHaveAttribute('href', '/docs');
  });

  it('renders external link with target blank', () => {
    render(<DetailField label="GitHub" value="Repo" href="https://github.com" external />);
    const link = screen.getByRole('link');
    expect(link).toHaveAttribute('target', '_blank');
    expect(link).toHaveAttribute('rel', 'noopener noreferrer');
  });

  it('uses stacked layout by default', () => {
    const { container } = render(<DetailField label="Field" value="Value" />);
    expect(container.firstChild).toHaveClass('space-y-1');
  });

  it('uses inline layout when specified', () => {
    const { container } = render(<DetailField label="Field" value="Value" layout="inline" />);
    expect(container.firstChild).toHaveClass('flex');
  });

  it('applies mono font when mono prop is true', () => {
    render(<DetailField label="Hash" value="abc123" mono />);
    const value = screen.getByText('abc123');
    expect(value).toHaveClass('font-mono');
  });
});
