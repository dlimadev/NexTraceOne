import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Collapsible, Accordion } from '../../components/Collapsible';

describe('Collapsible', () => {
  it('renders title', () => {
    render(<Collapsible title="Section 1">Content</Collapsible>);
    expect(screen.getByText('Section 1')).toBeInTheDocument();
  });

  it('hides content by default', () => {
    render(<Collapsible title="Section 1">Content</Collapsible>);
    const region = screen.getByRole('region', { hidden: true });
    expect(region).toHaveAttribute('hidden', '');
  });

  it('shows content when defaultOpen is true', () => {
    render(<Collapsible title="Section 1" defaultOpen>Content</Collapsible>);
    expect(screen.getByText('Content')).toBeVisible();
  });

  it('toggles content on click', async () => {
    render(<Collapsible title="Section 1">Content</Collapsible>);
    const button = screen.getByRole('button');

    await userEvent.click(button);
    expect(button).toHaveAttribute('aria-expanded', 'true');

    await userEvent.click(button);
    expect(button).toHaveAttribute('aria-expanded', 'false');
  });

  it('toggles on Enter key', async () => {
    render(<Collapsible title="Section 1">Content</Collapsible>);
    const button = screen.getByRole('button');
    button.focus();
    await userEvent.keyboard('{Enter}');
    expect(button).toHaveAttribute('aria-expanded', 'true');
  });

  it('toggles on Space key', async () => {
    render(<Collapsible title="Section 1">Content</Collapsible>);
    const button = screen.getByRole('button');
    button.focus();
    await userEvent.keyboard(' ');
    expect(button).toHaveAttribute('aria-expanded', 'true');
  });

  it('does not toggle when disabled', async () => {
    render(<Collapsible title="Section 1" disabled>Content</Collapsible>);
    const button = screen.getByRole('button');
    await userEvent.click(button);
    expect(button).toHaveAttribute('aria-expanded', 'false');
  });

  it('has aria-controls linking to content', () => {
    render(<Collapsible title="Section 1">Content</Collapsible>);
    const button = screen.getByRole('button');
    const controlsId = button.getAttribute('aria-controls');
    expect(controlsId).toBeTruthy();
    expect(document.getElementById(controlsId!)).toBeInTheDocument();
  });
});

describe('Accordion', () => {
  const items = [
    { id: 'a', title: 'Item A', content: 'Content A' },
    { id: 'b', title: 'Item B', content: 'Content B' },
    { id: 'c', title: 'Item C', content: 'Content C' },
  ];

  it('renders all items', () => {
    render(<Accordion items={items} />);
    expect(screen.getByText('Item A')).toBeInTheDocument();
    expect(screen.getByText('Item B')).toBeInTheDocument();
    expect(screen.getByText('Item C')).toBeInTheDocument();
  });

  it('opens items from defaultOpenIds', () => {
    render(<Accordion items={items} defaultOpenIds={['a']} />);
    const buttons = screen.getAllByRole('button');
    expect(buttons[0]).toHaveAttribute('aria-expanded', 'true');
    expect(buttons[1]).toHaveAttribute('aria-expanded', 'false');
  });

  it('single mode closes previous when opening new', async () => {
    render(<Accordion items={items} mode="single" defaultOpenIds={['a']} />);
    const buttons = screen.getAllByRole('button');

    await userEvent.click(buttons[1]);
    expect(buttons[0]).toHaveAttribute('aria-expanded', 'false');
    expect(buttons[1]).toHaveAttribute('aria-expanded', 'true');
  });

  it('multi mode allows multiple open', async () => {
    render(<Accordion items={items} mode="multi" defaultOpenIds={['a']} />);
    const buttons = screen.getAllByRole('button');

    await userEvent.click(buttons[1]);
    expect(buttons[0]).toHaveAttribute('aria-expanded', 'true');
    expect(buttons[1]).toHaveAttribute('aria-expanded', 'true');
  });
});
