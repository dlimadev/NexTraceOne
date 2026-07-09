import { describe, it, expect, vi } from 'vitest';
import { render } from '@testing-library/react';
import { HealthTrendSparkline } from '../../features/contracts/governance/HealthTrendSparkline';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));

describe('HealthTrendSparkline', () => {
  it('renders a polyline for two or more points', () => {
    const { container } = render(
      <HealthTrendSparkline points={[
        { semVer: '1.0.0', healthScore: 40 },
        { semVer: '1.1.0', healthScore: 80 },
      ]} />,
    );
    expect(container.querySelector('polyline')).not.toBeNull();
  });

  it('renders nothing (honest-null) with fewer than two points', () => {
    const { container } = render(<HealthTrendSparkline points={[{ semVer: '1.0.0', healthScore: 40 }]} />);
    expect(container.firstChild).toBeNull();
  });
});
