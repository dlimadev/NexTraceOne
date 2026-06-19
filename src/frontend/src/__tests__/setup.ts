import '@testing-library/jest-dom';
import { afterEach } from 'vitest';
import { cleanup } from '@testing-library/react';

// Inicializa i18n para os testes terem acesso às traduções
import '../i18n';

// Formatação numérica determinística nos testes — independe do locale da máquina.
// Sem locale explícito, toLocaleString() usa o locale do SO (que pode agrupar com
// espaço estreito). Forçamos en-US (vírgula como separador) para casar com o CI e
// com as asserções dos testes.
const _numberToLocaleString = Number.prototype.toLocaleString;
Number.prototype.toLocaleString = function (
  this: number,
  locales?: Intl.LocalesArgument,
  options?: Intl.NumberFormatOptions,
) {
  return _numberToLocaleString.call(this, locales ?? 'en-US', options);
};

// jsdom não suporta HTMLDialogElement.showModal/close — adicionar stubs
if (typeof HTMLDialogElement !== 'undefined') {
  HTMLDialogElement.prototype.showModal = function () { this.open = true; };
  HTMLDialogElement.prototype.close = function () { this.open = false; };
}

// Limpa o DOM após cada teste
afterEach(() => {
  cleanup();
});
