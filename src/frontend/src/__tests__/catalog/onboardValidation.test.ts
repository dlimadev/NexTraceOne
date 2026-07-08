import { describe, it, expect } from 'vitest';
import {
  EMPTY_IDENTITY,
  validateIdentity,
  EMPTY_INTERFACE,
  validateInterface,
} from '../../features/catalog/onboard/onboardValidation';

describe('validateIdentity', () => {
  it('flags required fields when empty', () => {
    const errors = validateIdentity(EMPTY_IDENTITY);
    expect(errors.name).toBeTruthy();
    expect(errors.domain).toBeTruthy();
    expect(errors.teamName).toBeTruthy();
  });

  it('passes with required fields filled', () => {
    const errors = validateIdentity({
      ...EMPTY_IDENTITY,
      name: 'orders-api',
      domain: 'Commerce',
      teamName: 'Orders',
      serviceType: 'RestApi',
    });
    expect(errors).toEqual({});
  });
});

describe('validateInterface', () => {
  it('flags missing name', () => {
    const errors = validateInterface(EMPTY_INTERFACE);
    expect(errors.name).toBeTruthy();
  });

  it('passes with a name', () => {
    const errors = validateInterface({ ...EMPTY_INTERFACE, name: 'Orders REST v1' });
    expect(errors).toEqual({});
  });
});
