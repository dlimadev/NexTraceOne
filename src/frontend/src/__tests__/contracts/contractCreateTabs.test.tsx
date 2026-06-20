import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import * as React from 'react';
vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, d?: string) => d ?? k }) }));
import { TypeModeTab } from '../../features/contracts/create/tabs/TypeModeTab';

const TYPES = [{ value: 'RestApi', labelKey: 'contracts.contractTypes.RestApi' }];

describe('TypeModeTab', () => {
  it('renders a best-for line per type card and reports selection', () => {
    const onType = vi.fn();
    render(
      <TypeModeTab
        filteredContractTypes={TYPES as never}
        selectedType="RestApi"
        onSelectType={onType}
        selectedMode="visual"
        onSelectMode={vi.fn()}
      />,
    );
    expect(screen.getByText(/Best for HTTP/i)).toBeInTheDocument();
    fireEvent.click(screen.getByText('REST / OpenAPI'));
    expect(onType).toHaveBeenCalled();
  });
});
