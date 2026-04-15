import '@testing-library/jest-dom';
import { afterEach } from 'vitest';
import { cleanup } from '@testing-library/react';

// Inicializa i18n para os testes terem acesso às traduções
import '../i18n';

// jsdom não suporta HTMLDialogElement.showModal/close — adicionar stubs
if (typeof HTMLDialogElement !== 'undefined') {
  HTMLDialogElement.prototype.showModal = function () { this.open = true; };
  HTMLDialogElement.prototype.close = function () { this.open = false; };
}

// Limpa o DOM após cada teste
afterEach(() => {
  cleanup();
});
