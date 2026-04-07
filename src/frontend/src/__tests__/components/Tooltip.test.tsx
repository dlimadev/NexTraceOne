import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Tooltip } from '../../components/Tooltip';

describe('Tooltip', () => {
  it('does not show tooltip content by default', () => {
    render(
      <Tooltip content="Help text">
        <button>Hover me</button>
      </Tooltip>,
    );
    expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();
  });

  it('shows tooltip on mouse enter', async () => {
    render(
      <Tooltip content="Help text" delay={0}>
        <button>Hover me</button>
      </Tooltip>,
    );
    await userEvent.hover(screen.getByText('Hover me'));
    expect(await screen.findByRole('tooltip')).toHaveTextContent('Help text');
  });

  it('hides tooltip on mouse leave', async () => {
    render(
      <Tooltip content="Help text" delay={0} hideDelay={0}>
        <button>Hover me</button>
      </Tooltip>,
    );
    await userEvent.hover(screen.getByText('Hover me'));
    expect(await screen.findByRole('tooltip')).toBeInTheDocument();
    await userEvent.unhover(screen.getByText('Hover me'));
    // After unhover, tooltip should disappear
    await vi.waitFor(() => {
      expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();
    });
  });

  it('renders with correct position class', async () => {
    render(
      <Tooltip content="Bottom tip" position="bottom" delay={0}>
        <button>Button</button>
      </Tooltip>,
    );
    await userEvent.hover(screen.getByText('Button'));
    const tooltip = await screen.findByRole('tooltip');
    expect(tooltip).toHaveClass('top-full');
  });

  it('applies aria-describedby when visible', async () => {
    render(
      <Tooltip content="Description" delay={0}>
        <button>Target</button>
      </Tooltip>,
    );
    await userEvent.hover(screen.getByText('Target'));
    await screen.findByRole('tooltip');
    const wrapper = screen.getByText('Target').closest('span');
    expect(wrapper).toHaveAttribute('aria-describedby');
  });
});
