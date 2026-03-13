import '@testing-library/jest-dom';
import { afterEach } from 'vitest';
import { cleanup } from '@testing-library/react';

// Inicializa i18n para os testes terem acesso às traduções
import '../i18n';

// Limpa o DOM após cada teste
afterEach(() => {
  cleanup();
});
