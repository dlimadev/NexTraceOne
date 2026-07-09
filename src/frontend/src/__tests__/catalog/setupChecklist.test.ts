import { describe, it, expect } from 'vitest';
import { deriveSetupItems, setupProgress } from '../../features/catalog/components/setupChecklist';

const always = () => true;
const never = () => false;

describe('deriveSetupItems', () => {
  it('marks each dimension done based on loaded data', () => {
    const items = deriveSetupItems(
      { technicalOwner: 'a@x.com', repositoryUrl: 'https://r', documentationUrl: 'https://d', apis: [{}], serviceType: 'RestApi' },
      2, always,
    );
    const byId = Object.fromEntries(items.map((i) => [i.id, i]));
    expect(byId.ownership.done).toBe(true);
    expect(byId.repository.done).toBe(true);
    expect(byId.documentation.done).toBe(true);
    expect(byId.interface.done).toBe(true);
    expect(byId.contract.done).toBe(true);
  });

  it('flags missing dimensions as not done', () => {
    const items = deriveSetupItems({ serviceType: 'RestApi', apis: [] }, 0, always);
    const byId = Object.fromEntries(items.map((i) => [i.id, i]));
    expect(byId.ownership.done).toBe(false);
    expect(byId.interface.done).toBe(false);
    expect(byId.contract.done).toBe(false);
  });

  it('marks the contract row not-applicable when the type has no public contracts', () => {
    const items = deriveSetupItems({ serviceType: 'BackgroundService' }, 0, never);
    const contract = items.find((i) => i.id === 'contract')!;
    expect(contract.applicable).toBe(false);
  });

  it('setupProgress counts only applicable items', () => {
    const items = deriveSetupItems(
      { technicalOwner: 'a', serviceType: 'BackgroundService' }, 0, never,
    );
    const p = setupProgress(items);
    expect(p.total).toBe(4);
    expect(p.done).toBe(1);
    expect(p.allDone).toBe(false);
  });
});
