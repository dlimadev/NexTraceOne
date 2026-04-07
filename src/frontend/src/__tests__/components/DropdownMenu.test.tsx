import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { DropdownMenu, type DropdownMenuItem } from '../../components/DropdownMenu';

const items: DropdownMenuItem[] = [
  { id: 'edit', label: 'Edit' },
  { id: 'sep', label: '', type: 'separator' },
  { id: 'delete', label: 'Delete', variant: 'danger' },
  { id: 'disabled', label: 'Disabled', disabled: true },
];

describe('DropdownMenu', () => {
  it('renders trigger button with label', () => {
    render(<DropdownMenu items={items} onSelect={() => {}} label="Actions" />);
    expect(screen.getByRole('button', { name: /Actions/ })).toBeInTheDocument();
  });

  it('shows menu on click', async () => {
    render(<DropdownMenu items={items} onSelect={() => {}} label="Actions" />);
    await userEvent.click(screen.getByRole('button', { name: /Actions/ }));
    expect(screen.getByRole('menu')).toBeInTheDocument();
  });

  it('renders menu items', async () => {
    render(<DropdownMenu items={items} onSelect={() => {}} label="Actions" />);
    await userEvent.click(screen.getByRole('button', { name: /Actions/ }));
    expect(screen.getByRole('menuitem', { name: 'Edit' })).toBeInTheDocument();
    expect(screen.getByRole('menuitem', { name: 'Delete' })).toBeInTheDocument();
  });

  it('renders separators', async () => {
    render(<DropdownMenu items={items} onSelect={() => {}} label="Actions" />);
    await userEvent.click(screen.getByRole('button', { name: /Actions/ }));
    expect(screen.getByRole('separator')).toBeInTheDocument();
  });

  it('calls onSelect when item is clicked', async () => {
    const onSelect = vi.fn();
    render(<DropdownMenu items={items} onSelect={onSelect} label="Actions" />);
    await userEvent.click(screen.getByRole('button', { name: /Actions/ }));
    await userEvent.click(screen.getByRole('menuitem', { name: 'Edit' }));
    expect(onSelect).toHaveBeenCalledWith('edit');
  });

  it('closes menu after selecting item', async () => {
    render(<DropdownMenu items={items} onSelect={() => {}} label="Actions" />);
    await userEvent.click(screen.getByRole('button', { name: /Actions/ }));
    await userEvent.click(screen.getByRole('menuitem', { name: 'Edit' }));
    expect(screen.queryByRole('menu')).not.toBeInTheDocument();
  });

  it('marks disabled items with aria-disabled', async () => {
    render(<DropdownMenu items={items} onSelect={() => {}} label="Actions" />);
    await userEvent.click(screen.getByRole('button', { name: /Actions/ }));
    const disabledItem = screen.getByRole('menuitem', { name: 'Disabled' });
    expect(disabledItem).toHaveAttribute('aria-disabled', 'true');
  });

  it('sets aria-haspopup on trigger', () => {
    render(<DropdownMenu items={items} onSelect={() => {}} label="Actions" />);
    expect(screen.getByRole('button')).toHaveAttribute('aria-haspopup', 'menu');
  });

  it('sets aria-expanded when open', async () => {
    render(<DropdownMenu items={items} onSelect={() => {}} label="Actions" />);
    const trigger = screen.getByRole('button');
    expect(trigger).toHaveAttribute('aria-expanded', 'false');
    await userEvent.click(trigger);
    expect(trigger).toHaveAttribute('aria-expanded', 'true');
  });
});
