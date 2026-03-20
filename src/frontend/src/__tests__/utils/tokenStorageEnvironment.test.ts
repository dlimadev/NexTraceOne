import { describe, it, expect, beforeEach } from 'vitest';
import { storeEnvironmentId, getEnvironmentId, clearEnvironmentId } from '../../utils/tokenStorage';

describe('tokenStorage — environment ID', () => {
  beforeEach(() => {
    sessionStorage.clear();
  });

  it('should store and retrieve environment ID', () => {
    storeEnvironmentId('env-prod-123');
    expect(getEnvironmentId()).toBe('env-prod-123');
  });

  it('should return null when no environment stored', () => {
    expect(getEnvironmentId()).toBeNull();
  });

  it('should clear environment ID', () => {
    storeEnvironmentId('env-qa-456');
    clearEnvironmentId();
    expect(getEnvironmentId()).toBeNull();
  });

  it('should overwrite previous environment ID', () => {
    storeEnvironmentId('env-qa-456');
    storeEnvironmentId('env-prod-123');
    expect(getEnvironmentId()).toBe('env-prod-123');
  });
});
