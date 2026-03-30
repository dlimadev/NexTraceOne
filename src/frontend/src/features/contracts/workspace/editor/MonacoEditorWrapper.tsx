import { useCallback, useRef } from 'react';
import Editor, { type OnMount, type OnChange } from '@monaco-editor/react';
import type * as Monaco from 'monaco-editor';

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
  const editorRef = useRef<Monaco.editor.IStandaloneCodeEditor | null>(null);

  const handleMount: OnMount = useCallback((editor, monaco) => {
    editorRef.current = editor;

    // Define NexTraceOne dark theme
    monaco.editor.defineTheme('nto-dark', {
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

    monaco.editor.setTheme('nto-dark');
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
