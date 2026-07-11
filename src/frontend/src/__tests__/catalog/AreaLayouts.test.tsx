import { describe, it, expect } from 'vitest';
import { screen } from '@testing-library/react';
import { Routes, Route } from 'react-router-dom';
import { renderWithProviders } from '../test-utils';
import { ServicesAreaLayout } from '../../features/catalog/layouts/ServicesAreaLayout';
import { ContractsAreaLayout } from '../../features/catalog/layouts/ContractsAreaLayout';

describe('ServicesAreaLayout', () => {
  it('renders the services sub-nav and the outlet content', () => {
    renderWithProviders(
      <Routes>
        <Route element={<ServicesAreaLayout />}>
          <Route path="/services" element={<div data-testid="list">List</div>} />
        </Route>
      </Routes>,
      { routerProps: { initialEntries: ['/services'] } },
    );
    expect(screen.getByTestId('list')).toBeInTheDocument();
    expect(screen.getByText('Discovery')).toBeInTheDocument();
    expect(screen.getByText('Legacy')).toBeInTheDocument();
  });
});

describe('ContractsAreaLayout', () => {
  it('renders the contracts sub-nav and the outlet content', () => {
    renderWithProviders(
      <Routes>
        <Route element={<ContractsAreaLayout />}>
          <Route path="/contracts" element={<div data-testid="clist">CList</div>} />
        </Route>
      </Routes>,
      { routerProps: { initialEntries: ['/contracts'] } },
    );
    expect(screen.getByTestId('clist')).toBeInTheDocument();
    expect(screen.getByText('Governance')).toBeInTheDocument();
    expect(screen.getByText('CDCT')).toBeInTheDocument();
  });
});
