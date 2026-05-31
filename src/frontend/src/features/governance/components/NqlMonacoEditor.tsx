/**
 * NqlMonacoEditor — editor de NQL (NexTrace Query Language) com Monaco Editor.
 * Fornece syntax highlighting, autocompletion básico e temas dark/light.
 * Usa @monaco-editor/react que já está no projeto.
 */
import { useCallback, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import Editor, { type OnMount } from '@monaco-editor/react';
import { Play, CheckCircle, AlertCircle, Loader2 } from 'lucide-react';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

export interface NqlMonacoEditorProps {
  value: string;
  onChange: (value: string) => void;
  height?: string;
  readOnly?: boolean;
}

interface ValidateResult {
  valid: boolean;
  errors?: string[];
}

// ── NQL Keywords & Snippets ────────────────────────────────────────────────

const NQL_KEYWORDS = [
  'SELECT', 'FROM', 'WHERE', 'AND', 'OR', 'NOT', 'IN', 'LIKE',
  'GROUP', 'BY', 'ORDER', 'ASC', 'DESC', 'LIMIT', 'OFFSET',
  'COUNT', 'SUM', 'AVG', 'MIN', 'MAX',
  'JOIN', 'ON', 'AS', 'DISTINCT',
];

const NQL_ENTITIES = [
  'services', 'teams', 'domains', 'changes', 'releases',
  'incidents', 'alerts', 'slos', 'contracts', 'policies',
  'telemetry.metrics', 'telemetry.logs', 'telemetry.traces', 'telemetry.errors',
];

const NQL_FUNCTIONS = [
  'now()', 'today()', 'date()', 'bucket()',
];

// ── Component ──────────────────────────────────────────────────────────────

export function NqlMonacoEditor({
  value,
  onChange,
  height = '160px',
  readOnly = false,
}: NqlMonacoEditorProps) {
  const { t } = useTranslation();
  const editorRef = useRef<Parameters<OnMount>[0] | null>(null);
  const [validation, setValidation] = useState<ValidateResult | null>(null);
  const [isValidating, setIsValidating] = useState(false);

  const handleEditorDidMount: OnMount = useCallback((editor, monaco) => {
    editorRef.current = editor;

    // Register NQL language
    monaco.languages.register({ id: 'nql' });

    monaco.languages.setMonarchTokensProvider('nql', {
      ignoreCase: true,
      defaultToken: '',
      tokenPostfix: '.nql',
      keywords: NQL_KEYWORDS.map(k => k.toLowerCase()),
      operators: ['=', '!=', '<>', '<', '>', '<=', '>=', '+', '-', '*', '/'],
      // eslint-disable-next-line no-useless-escape
      symbols: /[=><!~?:&|+\-*\/\^%]+/,
      escapes: /\\(?:[abfnrtv\\"']|x[0-9A-Fa-f]{1,4}|u[0-9A-Fa-f]{4}|U[0-9A-Fa-f]{8})/,
      digits: /\d+(_+\d+)*/,
      tokenizer: {
        root: [
          { include: '@comments' },
          { include: '@whitespace' },
          { include: '@numbers' },
          { include: '@strings' },
          [/[{()}]/, '@brackets'],
          [/@symbols/, { cases: { '@operators': 'operator', '@default': '' } }],
          [/\d{4}-\d{2}-\d{2}(T\d{2}:\d{2}:\d{2})?/, 'number.date'],
          [/[a-zA-Z_][a-zA-Z0-9_.]*/, {
            cases: {
              '@keywords': 'keyword',
              '@default': 'identifier',
            }
          }],
        ],
        whitespace: [
          [/[ \t\r\n]+/, 'white'],
        ],
        comments: [
          [/--+.*/, 'comment'],
          [/\/\*/, 'comment', '@comment'],
        ],
        comment: [
          // eslint-disable-next-line no-useless-escape
          [/[^\/*]+/, 'comment'],
          // eslint-disable-next-line no-useless-escape
          [/\*\//, 'comment', '@pop'],
          // eslint-disable-next-line no-useless-escape
          [/[\/*]/, 'comment'],
        ],
        numbers: [
          [/(@digits)/, 'number'],
        ],
        strings: [
          [/'/, 'string', '@string'],
          [/"/, 'string', '@stringDouble'],
        ],
        string: [
          [/[^']+/, 'string'],
          [/''/, 'string'],
          [/'/, 'string', '@pop'],
        ],
        stringDouble: [
          [/[^"]+/, 'string'],
          [/""/, 'string'],
          [/"/, 'string', '@pop'],
        ],
      },
    });

    // Completion provider
    monaco.languages.registerCompletionItemProvider('nql', {
      triggerCharacters: [' ', '.'],
      provideCompletionItems: (model, position) => {
        const word = model.getWordUntilPosition(position);
        const range = {
          startLineNumber: position.lineNumber,
          endLineNumber: position.lineNumber,
          startColumn: word.startColumn,
          endColumn: word.endColumn,
        };

        const suggestions = [
          ...NQL_KEYWORDS.map(k => ({
            label: k,
            kind: monaco.languages.CompletionItemKind.Keyword,
            insertText: k,
            range,
          })),
          ...NQL_ENTITIES.map(e => ({
            label: e,
            kind: monaco.languages.CompletionItemKind.Class,
            insertText: e,
            range,
            detail: 'Entity',
          })),
          ...NQL_FUNCTIONS.map(f => ({
            label: f,
            kind: monaco.languages.CompletionItemKind.Function,
            insertText: f,
            range,
            detail: 'Function',
          })),
        ];

        return { suggestions };
      },
    });

    // Set default language
    const model = editor.getModel();
    if (model) {
      monaco.editor.setModelLanguage(model, 'nql');
    }
  }, []);

  const handleValidate = useCallback(async () => {
    if (!value.trim()) return;
    setIsValidating(true);
    try {
      const res = await client.post('/governance/nql/validate', { query: value });
      setValidation({ valid: true });
    } catch (err: any) {
      const msg = err?.response?.data?.message || err?.message || 'Validation failed';
      setValidation({ valid: false, errors: [msg] });
    } finally {
      setIsValidating(false);
    }
  }, [value]);

  return (
    <div className="flex flex-col gap-2">
      <div className="rounded border border-edge overflow-hidden">
        <Editor
          height={height}
          defaultLanguage="nql"
          value={value}
          onChange={(v) => onChange(v ?? '')}
          onMount={handleEditorDidMount}
          options={{
            readOnly,
            minimap: { enabled: false },
            lineNumbers: 'on',
            scrollBeyondLastLine: false,
            fontSize: 12,
            fontFamily: 'JetBrains Mono, monospace',
            padding: { top: 8, bottom: 8 },
            automaticLayout: true,
            wordWrap: 'on',
            suggestOnTriggerCharacters: true,
            quickSuggestions: true,
            tabSize: 2,
          }}
          theme="vs-dark"
        />
      </div>

      {/* Toolbar */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={handleValidate}
            disabled={isValidating || !value.trim()}
            className="flex items-center gap-1 rounded border border-edge bg-card px-2 py-1 text-[10px] text-muted hover:text-accent disabled:opacity-40 transition-colors"
          >
            {isValidating ? <Loader2 size={10} className="animate-spin" /> : <CheckCircle size={10} />}
            {t('nqlEditor.validate', 'Validate')}
          </button>

          {validation && (
            <span className={`flex items-center gap-1 text-[10px] ${validation.valid ? 'text-green-500' : 'text-red-500'}`}>
              {validation.valid ? <CheckCircle size={10} /> : <AlertCircle size={10} />}
              {validation.valid
                ? t('nqlEditor.valid', 'Valid')
                : validation.errors?.[0] ?? t('nqlEditor.invalid', 'Invalid')}
            </span>
          )}
        </div>

        <span className="text-[9px] text-faded">
          {t('nqlEditor.hint', 'Ctrl+Space for suggestions')}
        </span>
      </div>
    </div>
  );
}
