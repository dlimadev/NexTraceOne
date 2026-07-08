import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ServiceInterfaceForm } from '../../features/catalog/onboard/ServiceInterfaceForm';
import { EMPTY_INTERFACE } from '../../features/catalog/onboard/onboardValidation';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (_k: string, d?: string) => d ?? _k }),
}));

describe('ServiceInterfaceForm', () => {
  it('reports name changes', () => {
    const onChange = vi.fn();
    render(<ServiceInterfaceForm values={EMPTY_INTERFACE} errors={{}} onChange={onChange} />);
    fireEvent.change(screen.getByLabelText(/interface name/i), { target: { value: 'Orders v1' } });
    expect(onChange).toHaveBeenCalledWith('name', 'Orders v1');
  });

  it('shows base path field for RestApi type', () => {
    render(<ServiceInterfaceForm values={{ ...EMPTY_INTERFACE, interfaceType: 'RestApi' }} errors={{}} onChange={() => {}} />);
    expect(screen.getByLabelText(/base path/i)).toBeInTheDocument();
  });
});
