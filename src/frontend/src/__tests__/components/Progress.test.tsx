import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ProgressBar, ProgressCircular, ProgressSteps } from '../../components/Progress';

describe('ProgressBar', () => {
  it('renders with correct aria attributes', () => {
    render(<ProgressBar value={50} label="Upload" />);
    const bar = screen.getByRole('progressbar');
    expect(bar).toHaveAttribute('aria-valuenow', '50');
    expect(bar).toHaveAttribute('aria-valuemin', '0');
    expect(bar).toHaveAttribute('aria-valuemax', '100');
  });

  it('clamps value to 0-100 range', () => {
    render(<ProgressBar value={150} />);
    expect(screen.getByRole('progressbar')).toHaveAttribute('aria-valuenow', '100');
  });

  it('renders label', () => {
    render(<ProgressBar value={30} label="Processing" />);
    expect(screen.getByText('Processing')).toBeInTheDocument();
  });

  it('shows percentage when showValue is true', () => {
    render(<ProgressBar value={75} showValue />);
    expect(screen.getByText('75%')).toBeInTheDocument();
  });

  it('renders indeterminate when value is undefined', () => {
    render(<ProgressBar />);
    const bar = screen.getByRole('progressbar');
    expect(bar).not.toHaveAttribute('aria-valuenow');
  });
});

describe('ProgressCircular', () => {
  it('renders with correct aria attributes', () => {
    render(<ProgressCircular value={60} />);
    const el = screen.getByRole('progressbar');
    expect(el).toHaveAttribute('aria-valuenow', '60');
  });

  it('displays label text', () => {
    render(<ProgressCircular value={80} label="80%" />);
    expect(screen.getByText('80%')).toBeInTheDocument();
  });

  it('renders SVG circles', () => {
    const { container } = render(<ProgressCircular value={50} />);
    const circles = container.querySelectorAll('circle');
    expect(circles).toHaveLength(2); // background + progress
  });
});

describe('ProgressSteps', () => {
  const steps = ['Upload', 'Review', 'Publish'];

  it('renders all steps', () => {
    render(<ProgressSteps steps={steps} currentStep={1} />);
    expect(screen.getByText('Upload')).toBeInTheDocument();
    expect(screen.getByText('Review')).toBeInTheDocument();
    expect(screen.getByText('Publish')).toBeInTheDocument();
  });

  it('marks completed steps with checkmark', () => {
    render(<ProgressSteps steps={steps} currentStep={2} />);
    expect(screen.getAllByText('✓')).toHaveLength(2);
  });

  it('shows step numbers for non-completed steps', () => {
    render(<ProgressSteps steps={steps} currentStep={0} />);
    expect(screen.getByText('1')).toBeInTheDocument();
    expect(screen.getByText('2')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();
  });
});
