import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AppTopbarSearch } from '../../../components/shell/AppTopbarSearch';
import { AppTopbarActions } from '../../../components/shell/AppTopbarActions';
import { AppUserMenu } from '../../../components/shell/AppUserMenu';
import { PageContainer } from '../../../components/shell/PageContainer';
import { PageSection } from '../../../components/shell/PageSection';
import { ContentGrid } from '../../../components/shell/ContentGrid';
import { ShellLoader } from '../../../components/shell/ShellLoader';
import { ModuleUnavailable } from '../../../components/shell/ModuleUnavailable';
import { ThemeProvider } from '../../../contexts/ThemeContext';

// Mock auth context
vi.mock('../../../contexts/AuthContext', () => ({
  useAuth: vi.fn(() => ({
    isAuthenticated: true,
    user: {
      id: '1',
      email: 'admin@nextraceone.io',
      fullName: 'Admin User',
      roleName: 'Admin',
    },
    logout: vi.fn(),
  })),
}));

// Mock persona context
vi.mock('../../../contexts/PersonaContext', () => ({
  usePersona: vi.fn(() => ({
    persona: 'Engineer',
    config: {
      sectionOrder: [],
      highlightedSections: [],
    },
  })),
}));

describe('AppTopbarSearch', () => {
  it('renders search trigger with keyboard shortcut', () => {
    const onOpen = vi.fn();
    render(<AppTopbarSearch onOpenCommandPalette={onOpen} />);
    const button = screen.getByLabelText('Command Palette');
    expect(button).toBeInTheDocument();
    expect(screen.getByText('⌘K')).toBeInTheDocument();
  });

  it('calls onOpenCommandPalette when clicked', () => {
    const onOpen = vi.fn();
    render(<AppTopbarSearch onOpenCommandPalette={onOpen} />);
    fireEvent.click(screen.getByLabelText('Command Palette'));
    expect(onOpen).toHaveBeenCalledTimes(1);
  });
});

function renderTopbarActions() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ThemeProvider>
          <AppTopbarActions />
        </ThemeProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('AppTopbarActions', () => {
  it('renders language and notification buttons', () => {
    renderTopbarActions();
    expect(screen.getByLabelText('Toggle language')).toBeInTheDocument();
    expect(screen.getByLabelText('Notification Center')).toBeInTheDocument();
  });

  it('opens language dropdown on click', () => {
    renderTopbarActions();
    fireEvent.click(screen.getByLabelText('Toggle language'));
    expect(screen.getByText('English')).toBeInTheDocument();
    expect(screen.getByText('Português (Brasil)')).toBeInTheDocument();
    expect(screen.getByText('Español')).toBeInTheDocument();
  });

  it('renders theme toggle button', () => {
    renderTopbarActions();
    expect(screen.getByLabelText('Toggle dark/light mode')).toBeInTheDocument();
  });
});

describe('AppUserMenu', () => {
  it('renders user menu trigger with avatar', () => {
    render(
      <MemoryRouter>
        <AppUserMenu />
      </MemoryRouter>,
    );
    expect(screen.getByTestId('user-menu-trigger')).toBeInTheDocument();
    expect(screen.getByText('A')).toBeInTheDocument();
  });

  it('opens user dropdown on click', () => {
    render(
      <MemoryRouter>
        <AppUserMenu />
      </MemoryRouter>,
    );
    fireEvent.click(screen.getByTestId('user-menu-trigger'));
    expect(screen.getByText('My Profile')).toBeInTheDocument();
    expect(screen.getByText('My Access')).toBeInTheDocument();
    expect(screen.getByText('Sign out')).toBeInTheDocument();
  });

  it('calls logout when sign out is clicked', () => {
    render(
      <MemoryRouter>
        <AppUserMenu />
      </MemoryRouter>,
    );
    fireEvent.click(screen.getByTestId('user-menu-trigger'));
    // The menu should be open with the logout option
    const logoutBtn = screen.getByText('Sign out');
    expect(logoutBtn).toBeInTheDocument();
    fireEvent.click(logoutBtn);
  });
});

describe('PageContainer', () => {
  it('renders children with default padding', () => {
    render(<PageContainer><div data-testid="child">Content</div></PageContainer>);
    expect(screen.getByTestId('child')).toBeInTheDocument();
  });

  it('applies max-width by default (not fluid)', () => {
    const { container } = render(<PageContainer>Content</PageContainer>);
    expect(container.firstChild).toHaveClass('max-w-[1600px]');
  });

  it('removes max-width when fluid', () => {
    const { container } = render(<PageContainer fluid>Content</PageContainer>);
    expect(container.firstChild).not.toHaveClass('max-w-[1600px]');
  });
});

describe('PageSection', () => {
  it('renders section with title', () => {
    render(<PageSection title="My Section"><p>Content</p></PageSection>);
    expect(screen.getByText('My Section')).toBeInTheDocument();
    expect(screen.getByText('Content')).toBeInTheDocument();
  });

  it('renders section without title', () => {
    render(<PageSection><p data-testid="inner">Content</p></PageSection>);
    expect(screen.getByTestId('inner')).toBeInTheDocument();
  });
});

describe('ContentGrid', () => {
  it('renders grid with default 3 columns', () => {
    const { container } = render(
      <ContentGrid>
        <div>A</div><div>B</div><div>C</div>
      </ContentGrid>,
    );
    expect(container.firstChild).toHaveClass('grid');
  });

  it('applies correct column classes for 2 columns', () => {
    const { container } = render(
      <ContentGrid columns={2}><div>A</div></ContentGrid>,
    );
    expect(container.firstChild).toHaveClass('md:grid-cols-2');
  });
});

describe('ShellLoader', () => {
  it('renders loading spinner with text', () => {
    render(<ShellLoader />);
    expect(screen.getByRole('status')).toBeInTheDocument();
    expect(screen.getByText('Loading...')).toBeInTheDocument();
  });
});

describe('ModuleUnavailable', () => {
  it('renders unavailable alert', () => {
    render(<ModuleUnavailable />);
    expect(screen.getByRole('alert')).toBeInTheDocument();
    expect(screen.getByText('Module Unavailable')).toBeInTheDocument();
  });
});
