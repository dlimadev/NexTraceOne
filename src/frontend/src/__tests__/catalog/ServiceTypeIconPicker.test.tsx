// src/frontend/src/__tests__/catalog/ServiceTypeIconPicker.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ServiceTypeIconPicker } from '../../features/catalog/components/ServiceTypeIconPicker';

describe('ServiceTypeIconPicker — mode=service', () => {
  it('renders at least 10 type cards in service mode', () => {
    render(<ServiceTypeIconPicker value="RestApi" onChange={vi.fn()} mode="service" />);
    const cards = screen.getAllByRole('option');
    expect(cards.length).toBeGreaterThanOrEqual(10);
  });

  it('selected type card has aria-selected=true', () => {
    render(<ServiceTypeIconPicker value="RestApi" onChange={vi.fn()} mode="service" />);
    const selected = screen.getAllByRole('option').find(
      (el) => el.getAttribute('aria-selected') === 'true'
    );
    expect(selected).toBeTruthy();
  });

  it('clicking a card calls onChange with that type', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<ServiceTypeIconPicker value="RestApi" onChange={onChange} mode="service" />);
    const cards = screen.getAllByRole('option');
    await user.click(cards[1]);
    expect(onChange).toHaveBeenCalledOnce();
    expect(typeof onChange.mock.calls[0][0]).toBe('string');
  });
});

describe('ServiceTypeIconPicker — mode=interface', () => {
  it('renders fewer cards in interface mode than in service mode', () => {
    const { container: serviceContainer } = render(
      <ServiceTypeIconPicker value="RestApi" onChange={vi.fn()} mode="service" />
    );
    const serviceCount = serviceContainer.querySelectorAll('[role="option"]').length;

    const { container: ifaceContainer } = render(
      <ServiceTypeIconPicker value="RestApi" onChange={vi.fn()} mode="interface" />
    );
    const ifaceCount = ifaceContainer.querySelectorAll('[role="option"]').length;

    expect(ifaceCount).toBeLessThan(serviceCount);
  });
});
