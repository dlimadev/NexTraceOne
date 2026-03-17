import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { PageHeader } from '../../components/PageHeader';

describe('PageHeader', () => {
  it('renderiza título', () => {
    render(<PageHeader title="Service Catalog" />);
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Service Catalog');
  });

  it('renderiza subtítulo quando fornecido', () => {
    render(<PageHeader title="Catalog" subtitle="Manage your services" />);
    expect(screen.getByText('Manage your services')).toBeInTheDocument();
  });

  it('não renderiza subtítulo quando omitido', () => {
    const { container } = render(<PageHeader title="Catalog" />);
    expect(container.querySelectorAll('p')).toHaveLength(0);
  });

  it('renderiza badge ao lado do título', () => {
    render(<PageHeader title="Catalog" badge={<span data-testid="badge">Beta</span>} />);
    expect(screen.getByTestId('badge')).toBeInTheDocument();
  });

  it('renderiza ações', () => {
    render(
      <PageHeader
        title="Users"
        actions={<button>Create User</button>}
      />,
    );
    expect(screen.getByRole('button', { name: /create user/i })).toBeInTheDocument();
  });

  it('renderiza children', () => {
    render(
      <PageHeader title="Page">
        <nav data-testid="breadcrumbs">Home / Page</nav>
      </PageHeader>,
    );
    expect(screen.getByTestId('breadcrumbs')).toBeInTheDocument();
  });

  it('aplica className customizada', () => {
    const { container } = render(<PageHeader title="Page" className="extra" />);
    expect(container.firstChild).toHaveClass('extra');
  });
});
