import { describe, it, expect } from 'vitest';
import { navItems } from '../../components/shell/AppSidebar';

describe('navItems — catálogo', () => {
  it('não contém o item Feature Flags (agora vive no detalhe do serviço)', () => {
    expect(navItems.find((i) => i.to === '/services/feature-flags')).toBeUndefined();
  });

  it('não contém o item Score & Maturidade (agora na lista do catálogo)', () => {
    expect(navItems.find((i) => i.to === '/services/maturity')).toBeUndefined();
  });

  it('mantém o Catálogo de Serviços', () => {
    expect(navItems.find((i) => i.to === '/services')).toBeDefined();
  });
});
