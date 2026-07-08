import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ServiceIdentityForm } from '../../features/catalog/onboard/ServiceIdentityForm';
import { EMPTY_IDENTITY } from '../../features/catalog/onboard/onboardValidation';

const translations: Record<string, string> = {
  'onboard.identity.name': 'Service Name',
  'onboard.identity.domain': 'Domain',
  'onboard.identity.team': 'Team',
  'onboard.identity.serviceType': 'Service Type',
  'onboard.identity.description': 'Description',
  'onboard.identity.criticality': 'Criticality',
  'onboard.identity.exposure': 'Exposure',
  'onboard.identity.technicalOwner': 'Technical Owner',
  'onboard.identity.businessOwner': 'Business Owner',
  'onboard.identity.documentationUrl': 'Documentation URL',
  'onboard.identity.repositoryUrl': 'Repository URL',
};

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (k: string, d?: string) => translations[k] ?? d ?? k,
  }),
}));

describe('ServiceIdentityForm', () => {
  it('renders name field and reports changes', () => {
    const onChange = vi.fn();
    render(<ServiceIdentityForm values={EMPTY_IDENTITY} errors={{}} onChange={onChange} />);
    const name = screen.getByLabelText(/service name/i);
    fireEvent.change(name, { target: { value: 'orders-api' } });
    expect(onChange).toHaveBeenCalledWith('name', 'orders-api');
  });

  it('shows a field error when provided', () => {
    render(
      <ServiceIdentityForm values={EMPTY_IDENTITY} errors={{ name: 'Required' }} onChange={() => {}} />,
    );
    expect(screen.getByText('Required')).toBeInTheDocument();
  });
});
