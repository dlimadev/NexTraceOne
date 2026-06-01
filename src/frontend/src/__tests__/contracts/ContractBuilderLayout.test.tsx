import * as React from 'react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));
vi.mock('monaco-editor', () => ({ default: {} }));

let capturedOnChange: ((val: string) => void) | undefined;
vi.mock('../../features/contracts/workspace/editor/MonacoEditorWrapper', () => ({
  MonacoEditorWrapper: vi.fn(({ onChange }: { onChange?: (v: string) => void }) => {
    capturedOnChange = onChange;
    return null;
  }),
}));

import { ContractBuilderLayout } from '../../features/contracts/studio/ContractBuilderLayout';

function wrap(node: React.ReactNode) {
  return render(<MemoryRouter>{node}</MemoryRouter>);
}

describe('ContractBuilderLayout', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    capturedOnChange = undefined;
  });
  afterEach(() => {
    vi.useRealTimers();
  });

  it('renders without crashing', () => {
    wrap(
      <ContractBuilderLayout
        contractName="Payments API"
        protocol="OpenAPI 3.1"
        language="yaml"
        initialContent="openapi: 3.1.0"
        renderPreview={() => <div data-testid="preview">Preview</div>}
      />,
    );
    expect(screen.getByTestId('contract-builder-layout')).toBeInTheDocument();
    expect(screen.getByTestId('preview')).toBeInTheDocument();
  });

  it('shows parse-error banner when YAML is invalid, after debounce', () => {
    wrap(
      <ContractBuilderLayout
        contractName="Test"
        protocol="OpenAPI 3.1"
        language="yaml"
        initialContent="openapi: 3.1.0"
        renderPreview={() => <div>Preview</div>}
      />,
    );
    act(() => { capturedOnChange?.('invalid: {{{'); });
    act(() => { vi.advanceTimersByTime(500); });
    expect(screen.getByTestId('parse-error-banner')).toBeInTheDocument();
  });

  it('calls onSave with current content when save button is clicked', () => {
    const onSave = vi.fn();
    wrap(
      <ContractBuilderLayout
        contractName="Test"
        protocol="OpenAPI 3.1"
        language="yaml"
        initialContent="openapi: 3.1.0"
        renderPreview={() => null}
        onSave={onSave}
      />,
    );
    screen.getByTestId('btn-save').click();
    expect(onSave).toHaveBeenCalledWith('openapi: 3.1.0');
  });

  it('does not render Save button when onSave is not provided', () => {
    wrap(
      <ContractBuilderLayout
        contractName="Test"
        protocol="OpenAPI 3.1"
        language="yaml"
        initialContent=""
        renderPreview={() => null}
      />,
    );
    expect(screen.queryByTestId('btn-save')).not.toBeInTheDocument();
  });
});
