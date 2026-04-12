/**
 * Stub para monaco-editor usado nos testes vitest.
 * O monaco-editor requer ambiente de browser real (workers, canvas, etc.)
 * e não pode ser testado via jsdom. Este stub evita erros de resolução de pacote.
 */
export const editor = {
  create: () => ({ dispose: () => {}, onDidChangeModelContent: () => ({ dispose: () => {} }), getValue: () => '', setValue: () => {}, setModel: () => {} }),
  createModel: () => ({}),
  setModelLanguage: () => {},
  defineTheme: () => {},
  setTheme: () => {},
};
export const Uri = { parse: (s: string) => s, file: (s: string) => s };
export const languages = { register: () => {}, setMonarchTokensProvider: () => {}, json: { jsonDefaults: { setDiagnosticsOptions: () => {} } } };
export const KeyMod = { CtrlCmd: 0, Shift: 0 };
export const KeyCode = { KeyS: 0, KeyZ: 0, KeyY: 0 };
export default { editor, Uri, languages, KeyMod, KeyCode };
