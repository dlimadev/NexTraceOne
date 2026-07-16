import { describe, it, expect, vi, beforeEach } from 'vitest';
import { serviceFeatureFlagsApi } from '../../features/catalog/api/featureFlags';
import client from '../../api/client';

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), patch: vi.fn() },
}));

describe('serviceFeatureFlagsApi', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('getDashboard faz GET /catalog/feature-flags', async () => {
    (client.get as ReturnType<typeof vi.fn>).mockResolvedValue({ data: { totalFlags: 0, enabledFlags: 0, disabledFlags: 0, affectedServices: 0, flags: [] } });
    const res = await serviceFeatureFlagsApi.getDashboard();
    expect(client.get).toHaveBeenCalledWith('/catalog/feature-flags');
    expect(res.flags).toEqual([]);
  });

  it('toggle faz PATCH /catalog/feature-flags/:id', async () => {
    (client.patch as ReturnType<typeof vi.fn>).mockResolvedValue({ data: {} });
    await serviceFeatureFlagsApi.toggle('f1', true);
    expect(client.patch).toHaveBeenCalledWith('/catalog/feature-flags/f1', { enabled: true });
  });
});
