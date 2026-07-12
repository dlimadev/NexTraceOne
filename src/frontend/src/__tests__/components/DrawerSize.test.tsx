import { describe, it, expect } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';
import { Drawer } from '../../components/Drawer';

describe('Drawer size xl', () => {
  it('applies the xl width class to the panel', () => {
    renderWithProviders(
      <Drawer open onClose={() => {}} title="Editor" size="xl">
        <div data-testid="body">content</div>
      </Drawer>,
    );
    // O painel é o ancestral com a classe de largura; procuramos pela classe xl.
    const panel = document.querySelector('.w-\\[min\\(1100px\\,92vw\\)\\]');
    expect(panel).not.toBeNull();
    expect(screen.getByTestId('body')).toBeInTheDocument();
  });
});
