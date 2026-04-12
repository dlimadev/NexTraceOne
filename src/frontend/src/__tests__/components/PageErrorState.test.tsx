import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { PageErrorState } from '../../components/PageErrorState';
import { I18nextProvider } from 'react-i18next';
import i18n from '../../i18n';

function renderWithI18n(ui: React.ReactElement) {
  return render(<I18nextProvider i18n={i18n}>{ui}</I18nextProvider>);
}

describe('PageErrorState', () => {
  it('renders with role="alert"', () => {
    renderWithI18n(<PageErrorState />);
    expect(screen.getByRole('alert')).toBeInTheDocument();
  });

  it('renders custom title and message', () => {
    renderWithI18n(<PageErrorState title="Not Found" message="The page does not exist." />);
    expect(screen.getByText('Not Found')).toBeInTheDocument();
    expect(screen.getByText('The page does not exist.')).toBeInTheDocument();
  });

  it('renders retry button when onRetry is provided', () => {
    renderWithI18n(<PageErrorState onRetry={() => {}} />);
    const buttons = screen.getAllByRole('button');
    expect(buttons.length).toBeGreaterThan(0);
  });

  it('renders custom action', () => {
    renderWithI18n(<PageErrorState action={<button>Go Back</button>} />);
    expect(screen.getByRole('button', { name: 'Go Back' })).toBeInTheDocument();
  });

  it('compact variant has smaller padding', () => {
    const { container } = renderWithI18n(<PageErrorState variant="compact" />);
    const alert = container.querySelector('[role="alert"]');
    expect(alert).toHaveClass('py-6');
  });

  it('default variant has larger padding', () => {
    const { container } = renderWithI18n(<PageErrorState />);
    const alert = container.querySelector('[role="alert"]');
    expect(alert).toHaveClass('py-12');
  });

  it('renders custom icon', () => {
    renderWithI18n(
      <PageErrorState icon={<span data-testid="custom-icon">!</span>} />,
    );
    expect(screen.getByTestId('custom-icon')).toBeInTheDocument();
  });
});
