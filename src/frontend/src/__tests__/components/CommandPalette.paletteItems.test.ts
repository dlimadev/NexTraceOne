import { describe, it, expect } from 'vitest';
import { paletteItems } from '../../components/CommandPalette';

describe('paletteItems — contratos', () => {
  it('não expõe uma entrada de criação de contrato (Contract Studio)', () => {
    // Contrato nasce do cadastro (onboarding) ou do detalhe do serviço, não da palette.
    expect(paletteItems.find((i) => i.id === 'contract-studio')).toBeUndefined();
    expect(paletteItems.find((i) => i.to === '/contracts/studio')).toBeUndefined();
  });

  it('mantém o Catálogo de Serviços', () => {
    expect(paletteItems.find((i) => i.to === '/services')).toBeDefined();
  });
});
