import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { AsyncApiChannelsPreview } from '../../features/contracts/studio/components/previews/AsyncApiChannelsPreview';

const VALID_YAML = `
asyncapi: 3.0.0
info:
  title: Order Events
  version: 1.0.0
channels:
  userRegistered:
    address: user.registered
    bindings:
      kafka: {}
  orderCreated:
    address: order.created
    bindings:
      amqp: {}
`;

describe('AsyncApiChannelsPreview', () => {
  it('renders channel addresses from valid AsyncAPI YAML', () => {
    render(<AsyncApiChannelsPreview content={VALID_YAML} />);
    expect(screen.getByText('user.registered')).toBeInTheDocument();
    expect(screen.getByText('order.created')).toBeInTheDocument();
  });

  it('renders protocol badges', () => {
    render(<AsyncApiChannelsPreview content={VALID_YAML} />);
    expect(screen.getByText('kafka')).toBeInTheDocument();
    expect(screen.getByText('amqp')).toBeInTheDocument();
  });

  it('renders empty state for invalid YAML without throwing', () => {
    render(<AsyncApiChannelsPreview content="not: valid: {{{" />);
    expect(screen.getByTestId('preview-empty')).toBeInTheDocument();
  });
});
