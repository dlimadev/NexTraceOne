import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Modal } from '../../components/Modal';

// Mock HTMLDialogElement methods for jsdom
beforeEach(() => {
  HTMLDialogElement.prototype.showModal = vi.fn(function (this: HTMLDialogElement) {
    this.setAttribute('open', '');
  });
  HTMLDialogElement.prototype.close = vi.fn(function (this: HTMLDialogElement) {
    this.removeAttribute('open');
  });
});

describe('Modal', () => {
  it('renders nothing when closed', () => {
    const { container } = render(
      <Modal open={false} onClose={() => {}}>Content</Modal>,
    );
    expect(container.querySelector('dialog')).toBeNull();
  });

  it('renders content when open', () => {
    render(
      <Modal open={true} onClose={() => {}}>
        <p>Modal content</p>
      </Modal>,
    );
    expect(screen.getByText('Modal content')).toBeInTheDocument();
  });

  it('renders title and description', () => {
    render(
      <Modal open={true} onClose={() => {}} title="Test Title" description="Test Desc">
        Body
      </Modal>,
    );
    expect(screen.getByText('Test Title')).toBeInTheDocument();
    expect(screen.getByText('Test Desc')).toBeInTheDocument();
  });

  it('renders footer actions', () => {
    render(
      <Modal open={true} onClose={() => {}} footer={<button>Save</button>}>
        Body
      </Modal>,
    );
    expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument();
  });

  it('calls onClose when close button is clicked', async () => {
    const onClose = vi.fn();
    render(
      <Modal open={true} onClose={onClose} title="Title">Body</Modal>,
    );
    await userEvent.click(screen.getByRole('button', { name: 'Close' }));
    expect(onClose).toHaveBeenCalled();
  });

  it('has aria-labelledby linking title', () => {
    render(
      <Modal open={true} onClose={() => {}} title="Accessible Title">Body</Modal>,
    );
    const dialog = screen.getByText('Body').closest('dialog');
    expect(dialog).toHaveAttribute('aria-labelledby', 'nto-modal-title');
  });

  it('applies size class', () => {
    render(
      <Modal open={true} onClose={() => {}} size="xl">Body</Modal>,
    );
    const content = screen.getByText('Body').closest('.max-w-4xl');
    expect(content).toBeInTheDocument();
  });
});
