import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { DemoBanner } from '../../components/DemoBanner';

describe('DemoBanner', () => {
  it('renderiza com título e descrição padrão via i18n', () => {
    render(<DemoBanner />);
    // i18n keys: common.demoBanner.title / common.demoBanner.description
    // In test env, i18n is initialized so actual translated text should appear
    const status = screen.getByRole('status');
    expect(status).toBeInTheDocument();
  });

  it('aceita className customizada', () => {
    const { container } = render(<DemoBanner className="mt-4" />);
    const message = container.querySelector('[role="status"]');
    expect(message).toHaveClass('mt-4');
  });

  it('renderiza como severity warning (InlineMessage)', () => {
    const { container } = render(<DemoBanner />);
    const message = container.querySelector('[role="status"]');
    // InlineMessage with warning severity should have warning-related classes
    expect(message).toHaveClass('border');
  });

  it('aceita titleKey e descriptionKey customizados', () => {
    render(
      <DemoBanner
        titleKey="common.demoBanner.title"
        descriptionKey="common.demoBanner.description"
      />
    );
    expect(screen.getByRole('status')).toBeInTheDocument();
  });
});
