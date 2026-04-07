import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { TrendBadge } from '../../components/TrendBadge';
import { MiniSparkline } from '../../components/MiniSparkline';
import { StackedProgressBar } from '../../components/StackedProgressBar';
import { TimePeriodSelector } from '../../components/TimePeriodSelector';
import { Card, CardHeader, CardBody, CardFooter } from '../../components/Card';

describe('TrendBadge', () => {
  it('renders up trend with success styling', () => {
    render(<TrendBadge direction="up" value="+2.57%" />);
    const badge = screen.getByText('+2.57%');
    expect(badge.closest('span')).toHaveClass('bg-success/15', 'text-success');
  });

  it('renders down trend with critical styling', () => {
    render(<TrendBadge direction="down" value="-1.2%" />);
    const badge = screen.getByText('-1.2%');
    expect(badge.closest('span')).toHaveClass('bg-critical/15', 'text-critical');
  });

  it('renders neutral trend', () => {
    render(<TrendBadge direction="neutral" value="0%" />);
    const badge = screen.getByText('0%');
    expect(badge.closest('span')).toHaveClass('bg-neutral/15', 'text-neutral');
  });

  it('supports sm size', () => {
    render(<TrendBadge direction="up" value="+5%" size="sm" />);
    const badge = screen.getByText('+5%');
    expect(badge.closest('span')).toHaveClass('text-[10px]');
  });
});

describe('MiniSparkline', () => {
  it('renders SVG with correct dimensions', () => {
    const { container } = render(<MiniSparkline data={[1, 2, 3, 4, 5]} width={80} height={32} />);
    const svg = container.querySelector('svg');
    expect(svg).toBeTruthy();
    expect(svg?.getAttribute('width')).toBe('80');
    expect(svg?.getAttribute('height')).toBe('32');
  });

  it('renders path elements for line and fill', () => {
    const { container } = render(<MiniSparkline data={[10, 20, 15, 25, 30]} filled />);
    const paths = container.querySelectorAll('path');
    expect(paths.length).toBeGreaterThanOrEqual(2); // area fill + line
  });

  it('does not render when data is empty', () => {
    const { container } = render(<MiniSparkline data={[]} />);
    expect(container.querySelector('svg')).toBeNull();
  });

  it('is hidden from screen readers', () => {
    const { container } = render(<MiniSparkline data={[1, 2, 3]} />);
    const svg = container.querySelector('svg');
    expect(svg?.getAttribute('aria-hidden')).toBe('true');
  });
});

describe('StackedProgressBar', () => {
  const segments = [
    { value: 60, color: 'bg-success', label: 'Approved' },
    { value: 25, color: 'bg-warning', label: 'In Review' },
    { value: 15, color: 'bg-critical', label: 'Deprecated' },
  ];

  it('renders all segments', () => {
    const { container } = render(<StackedProgressBar segments={segments} />);
    const bars = container.querySelectorAll('[role="progressbar"] > div');
    expect(bars).toHaveLength(3);
  });

  it('renders legend when showLegend is true', () => {
    render(<StackedProgressBar segments={segments} showLegend />);
    expect(screen.getByText('Approved')).toBeInTheDocument();
    expect(screen.getByText('In Review')).toBeInTheDocument();
    expect(screen.getByText('Deprecated')).toBeInTheDocument();
  });

  it('does not render legend by default', () => {
    render(<StackedProgressBar segments={segments} />);
    expect(screen.queryByText('Approved')).toBeNull();
  });

  it('applies correct width percentages', () => {
    const { container } = render(<StackedProgressBar segments={segments} />);
    const bars = container.querySelectorAll('[role="progressbar"] > div');
    expect((bars[0] as HTMLElement).style.width).toBe('60%');
    expect((bars[1] as HTMLElement).style.width).toBe('25%');
    expect((bars[2] as HTMLElement).style.width).toBe('15%');
  });
});

describe('TimePeriodSelector', () => {
  const options = [
    { value: 'today', label: 'Today' },
    { value: 'week', label: 'Week' },
    { value: 'month', label: 'Month' },
  ];

  it('renders all options', () => {
    render(<TimePeriodSelector options={options} selected="today" onSelect={() => {}} />);
    expect(screen.getByText('Today')).toBeInTheDocument();
    expect(screen.getByText('Week')).toBeInTheDocument();
    expect(screen.getByText('Month')).toBeInTheDocument();
  });

  it('marks selected option as aria-selected', () => {
    render(<TimePeriodSelector options={options} selected="week" onSelect={() => {}} />);
    expect(screen.getByText('Week')).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByText('Today')).toHaveAttribute('aria-selected', 'false');
  });

  it('calls onSelect when clicking an option', async () => {
    const user = userEvent.setup();
    let selected = 'today';
    const onSelect = (v: string) => { selected = v; };
    render(<TimePeriodSelector options={options} selected="today" onSelect={onSelect} />);
    await user.click(screen.getByText('Month'));
    expect(selected).toBe('month');
  });

  it('uses tablist role', () => {
    render(<TimePeriodSelector options={options} selected="today" onSelect={() => {}} />);
    expect(screen.getByRole('tablist')).toBeInTheDocument();
  });
});

describe('CardFooter', () => {
  it('renders children', () => {
    render(
      <Card>
        <CardHeader>Header</CardHeader>
        <CardBody>Body</CardBody>
        <CardFooter>Footer Content</CardFooter>
      </Card>,
    );
    expect(screen.getByText('Footer Content')).toBeInTheDocument();
  });

  it('has border-t class', () => {
    render(<CardFooter data-testid="footer">Footer</CardFooter>);
    const footer = screen.getByTestId('footer');
    expect(footer.className).toContain('border-t');
  });

  it('Card gradient variant has expected classes', () => {
    render(<Card variant="gradient" data-testid="gradient-card">Gradient</Card>);
    const card = screen.getByTestId('gradient-card');
    expect(card.className).toContain('text-white');
    expect(card.className).toContain('bg-gradient-to-br');
  });
});
