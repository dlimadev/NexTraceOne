import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { OnboardReviewStep } from '../../features/catalog/onboard/OnboardReviewStep';
import { EMPTY_IDENTITY } from '../../features/catalog/onboard/onboardValidation';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (_k: string, d?: string) => d ?? _k }),
}));

describe('OnboardReviewStep', () => {
  it('shows the service identity and honest-null "Skipped" for omitted optional steps', () => {
    render(
      <OnboardReviewStep
        identity={{ ...EMPTY_IDENTITY, name: 'orders-api', domain: 'Commerce', teamName: 'Orders' }}
        interfaceValues={null}
        contractSummary={null}
      />,
    );
    expect(screen.getByText('orders-api')).toBeInTheDocument();
    // Interface e contrato saltados → dois "Skipped", nada fabricado.
    expect(screen.getAllByText(/skipped/i)).toHaveLength(2);
  });

  it('shows the interface name when the interface step was completed', () => {
    render(
      <OnboardReviewStep
        identity={{ ...EMPTY_IDENTITY, name: 'orders-api' }}
        interfaceValues={{
          name: 'Orders REST v1', interfaceType: 'RestApi', description: '', exposureScope: 'Internal',
          basePath: '', topicName: '', wsdlNamespace: '', grpcServiceName: '', scheduleCron: '',
          documentationUrl: '', requiresContract: false,
        }}
        contractSummary={null}
      />,
    );
    expect(screen.getByText('Orders REST v1')).toBeInTheDocument();
    expect(screen.getAllByText(/skipped/i)).toHaveLength(1);
  });
});
