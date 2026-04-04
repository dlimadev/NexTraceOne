/// <reference types="vite/client" />
import * as monaco from 'monaco-editor';
import editorWorker from 'monaco-editor/esm/vs/editor/editor.worker?worker';
import jsonWorker from 'monaco-editor/esm/vs/language/json/json.worker?worker';
import { useCallback, useRef } from 'react';
import Editor, { loader, type OnMount, type OnChange } from '@monaco-editor/react';

// ---------------------------------------------------------------------------
// Monaco bootstrap — executado uma vez no carregamento do módulo.
//
// 1. MonacoEnvironment.getWorker: usa o import nativo ?worker do Vite para
//    criar workers a partir de URLs same-origin (bundled pelo Vite).
//    Não usa blob: nem CDN — compatível com CSP strict-dynamic / script-src 'self'.
//
// 2. loader.config({ monaco }): passa a instância do monaco-editor instalado
//    localmente para o @monaco-editor/react, eliminando completamente qualquer
//    requisição HTTP ao loader.js ou CDN. Sem script injection, sem 404s.
// ---------------------------------------------------------------------------
// eslint-disable-next-line @typescript-eslint/no-explicit-any
(self as any).MonacoEnvironment = {
  getWorker(_: unknown, label: string): Worker {
    if (label === 'json') return new jsonWorker();
    return new editorWorker();
  },
};

loader.config({ monaco });

interface MonacoEditorWrapperProps {
  value: string;
  language: string;
  isReadOnly?: boolean;
  onChange?: (value: string) => void;
  className?: string;
}

/**
 * Wrapper do Monaco Editor para edição de especificações de contrato.
 * Suporta YAML, JSON e XML com syntax highlighting, minimap e folding.
 * Tema dark alinhado com o design system NexTraceOne.
 */
export function MonacoEditorWrapper({
  value,
  language,
  isReadOnly = false,
  onChange,
  className = '',
}: MonacoEditorWrapperProps) {
  const editorRef = useRef<monaco.editor.IStandaloneCodeEditor | null>(null);

  const handleMount: OnMount = useCallback((editor, monacoInstance) => {
    editorRef.current = editor;

    // Define NexTraceOne dark theme
    monacoInstance.editor.defineTheme('nto-dark', {
      base: 'vs-dark',
      inherit: true,
      rules: [
        { token: 'type', foreground: '4ec9b0' },
        { token: 'string.yaml', foreground: 'ce9178' },
        { token: 'number', foreground: 'b5cea8' },
        { token: 'keyword', foreground: '569cd6' },
        { token: 'comment', foreground: '6a9955' },
      ],
      colors: {
        'editor.background': '#0d1117',
        'editor.foreground': '#c9d1d9',
        'editor.lineHighlightBackground': '#161b22',
        'editorLineNumber.foreground': '#484f58',
        'editorLineNumber.activeForeground': '#c9d1d9',
        'editor.selectionBackground': '#264f78',
        'editor.inactiveSelectionBackground': '#1a2332',
        'editorIndentGuide.background1': '#21262d',
        'editorIndentGuide.activeBackground1': '#30363d',
        'minimap.background': '#0d1117',
        'editorGutter.background': '#0d1117',
      },
    });

    monacoInstance.editor.setTheme('nto-dark');
  }, []);

  const handleChange: OnChange = useCallback(
    (newValue) => {
      onChange?.(newValue ?? '');
    },
    [onChange],
  );

  return (
    <div className={`h-full w-full ${className}`}>
      <Editor
        height="100%"
        language={language}
        value={value}
        onChange={handleChange}
        onMount={handleMount}
        theme="nto-dark"
        options={{
          readOnly: isReadOnly,
          minimap: { enabled: true, maxColumn: 80 },
          fontSize: 12,
          fontFamily: "'JetBrains Mono', 'Fira Code', monospace",
          lineNumbers: 'on',
          folding: true,
          wordWrap: 'off',
          scrollBeyondLastLine: false,
          automaticLayout: true,
          tabSize: 2,
          renderWhitespace: 'selection',
          bracketPairColorization: { enabled: true },
          guides: { indentation: true, bracketPairs: true },
          suggest: { showWords: false },
          quickSuggestions: false,
          padding: { top: 8 },
        }}
      />
    </div>
  );
}
