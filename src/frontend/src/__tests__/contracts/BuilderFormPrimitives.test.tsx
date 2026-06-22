import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (k: string) => k }),
}));

import {
  AddButton,
  RemoveIconButton,
} from '../../features/contracts/workspace/builders/shared/BuilderFormPrimitives';

describe('AddButton', () => {
  it('renders the label and fires onClick', () => {
    const onClick = vi.fn();
    render(<AddButton label="Add Endpoint" onClick={onClick} />);
    fireEvent.click(screen.getByRole('button', { name: /Add Endpoint/ }));
    expect(onClick).toHaveBeenCalledTimes(1);
  });

  it('does not fire onClick when disabled', () => {
    const onClick = vi.fn();
    render(<AddButton label="Add" onClick={onClick} disabled />);
    fireEvent.click(screen.getByRole('button'));
    expect(onClick).not.toHaveBeenCalled();
  });
});

describe('RemoveIconButton', () => {
  it('fires onClick with the event (so call sites can stopPropagation)', () => {
    const onClick = vi.fn();
    render(<RemoveIconButton onClick={onClick} title="Remove" />);
    fireEvent.click(screen.getByRole('button', { name: 'Remove' }));
    expect(onClick).toHaveBeenCalledTimes(1);
    expect(onClick.mock.calls[0][0]).toBeTruthy();
  });

  it('keeps the muted base and applies passthrough className', () => {
    render(<RemoveIconButton onClick={() => {}} className="opacity-0 group-hover:opacity-100" />);
    const btn = screen.getByRole('button');
    expect(btn.className).toContain('text-muted');
    expect(btn.className).toContain('group-hover:opacity-100');
  });
});
