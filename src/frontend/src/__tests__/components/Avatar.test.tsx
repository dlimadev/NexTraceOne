import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Avatar, AvatarGroup } from '../../components/Avatar';

describe('Avatar', () => {
  it('renders initials from name', () => {
    render(<Avatar name="John Doe" />);
    expect(screen.getByText('JD')).toBeInTheDocument();
  });

  it('renders single initial for single-word name', () => {
    render(<Avatar name="Admin" />);
    expect(screen.getByText('A')).toBeInTheDocument();
  });

  it('renders fallback for no name', () => {
    render(<Avatar />);
    expect(screen.getByText('?')).toBeInTheDocument();
  });

  it('renders image when src is provided', () => {
    render(<Avatar src="/avatar.jpg" name="Test User" />);
    expect(screen.getByRole('img')).toHaveAttribute('src', '/avatar.jpg');
  });

  it('renders custom fallback icon', () => {
    render(<Avatar fallbackIcon={<span data-testid="custom-icon">★</span>} />);
    expect(screen.getByTestId('custom-icon')).toBeInTheDocument();
  });

  it('renders status indicator', () => {
    const { container } = render(<Avatar name="User" status="online" />);
    const indicator = container.querySelector('[aria-label="online"]');
    expect(indicator).toBeInTheDocument();
  });

  it('applies size classes', () => {
    const { container } = render(<Avatar name="Test" size="lg" />);
    const avatar = container.querySelector('.w-12');
    expect(avatar).toBeInTheDocument();
  });

  it('generates consistent color for same name', () => {
    const { container: c1 } = render(<Avatar name="Test User" />);
    const { container: c2 } = render(<Avatar name="Test User" />);
    const cls1 = c1.querySelector('[title="Test User"]')?.className;
    const cls2 = c2.querySelector('[title="Test User"]')?.className;
    expect(cls1).toBe(cls2);
  });
});

describe('AvatarGroup', () => {
  it('renders visible avatars up to max', () => {
    render(
      <AvatarGroup max={2} total={5}>
        <Avatar name="A" />
        <Avatar name="B" />
        <Avatar name="C" />
      </AvatarGroup>,
    );
    expect(screen.getByText('+3')).toBeInTheDocument();
  });

  it('does not show overflow when within max', () => {
    render(
      <AvatarGroup max={3}>
        <Avatar name="A" />
        <Avatar name="B" />
      </AvatarGroup>,
    );
    expect(screen.queryByText(/\+/)).not.toBeInTheDocument();
  });
});
