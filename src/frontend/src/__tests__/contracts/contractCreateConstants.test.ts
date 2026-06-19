import { describe, it, expect } from 'vitest';
import { HUB_KEY_TO_CONTRACT_TYPE, BEST_FOR_KEY, FORM_TABS, CREATION_MODES } from '../../features/contracts/create/contractCreateConstants';

describe('contractCreateConstants', () => {
  it('maps every hub card key to a ContractType value', () => {
    expect(HUB_KEY_TO_CONTRACT_TYPE['rest-openapi']).toBe('RestApi');
    expect(HUB_KEY_TO_CONTRACT_TYPE['asyncapi']).toBe('Event');
    expect(HUB_KEY_TO_CONTRACT_TYPE['soap-wsdl']).toBe('Soap');
    expect(HUB_KEY_TO_CONTRACT_TYPE['shared-schema']).toBe('SharedSchema');
  });

  it('has a best-for key for each contract type', () => {
    expect(BEST_FOR_KEY('RestApi')).toBe('contracts.create.bestFor.RestApi');
  });

  it('defines the four ordered form tabs', () => {
    expect(FORM_TABS).toEqual(['service', 'typeMode', 'details', 'confirm']);
  });

  it('defines three creation modes', () => {
    expect(CREATION_MODES.map((m) => m.id)).toEqual(['visual', 'import', 'ai']);
  });
});
