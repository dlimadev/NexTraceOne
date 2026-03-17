/**
 * Primitivas de formulário partilhadas entre todos os visual builders.
 * Centraliza Field, FieldArea, FieldSelect, FieldCheckbox e FieldTagInput
 * para consistência visual e redução de duplicação.
 */
import { useState, useCallback } from 'react';
import { X } from 'lucide-react';

const INPUT_CLASS =
  'w-full text-xs bg-elevated border border-edge rounded px-2 py-1.5 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent';

export function Field({
  label,
  value,
  onChange,
  placeholder,
  required,
  error,
  disabled,
  mono,
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
  placeholder?: string;
  required?: boolean;
  error?: string;
  disabled?: boolean;
  mono?: boolean;
}) {
  return (
    <div>
      <label className="block text-[10px] font-medium text-muted mb-0.5">
        {label}
        {required && <span className="text-danger ml-0.5">*</span>}
      </label>
      <input
        type="text"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        disabled={disabled}
        className={`${INPUT_CLASS} ${mono ? 'font-mono' : ''} ${error ? 'border-danger/50 focus:ring-danger' : ''} ${disabled ? 'opacity-50 cursor-not-allowed' : ''}`}
      />
      {error && <p className="text-[9px] text-danger mt-0.5">{error}</p>}
    </div>
  );
}

export function FieldArea({
  label,
  value,
  onChange,
  placeholder,
  rows = 3,
  required,
  error,
  disabled,
  mono,
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
  placeholder?: string;
  rows?: number;
  required?: boolean;
  error?: string;
  disabled?: boolean;
  mono?: boolean;
}) {
  return (
    <div>
      <label className="block text-[10px] font-medium text-muted mb-0.5">
        {label}
        {required && <span className="text-danger ml-0.5">*</span>}
      </label>
      <textarea
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        rows={rows}
        disabled={disabled}
        className={`${INPUT_CLASS} resize-none ${mono ? 'font-mono' : ''} ${error ? 'border-danger/50 focus:ring-danger' : ''} ${disabled ? 'opacity-50 cursor-not-allowed' : ''}`}
      />
      {error && <p className="text-[9px] text-danger mt-0.5">{error}</p>}
    </div>
  );
}

export function FieldSelect<T extends string>({
  label,
  value,
  onChange,
  options,
  required,
  disabled,
}: {
  label: string;
  value: T;
  onChange: (v: T) => void;
  options: readonly T[] | { value: T; label: string }[];
  required?: boolean;
  disabled?: boolean;
}) {
  const isObjectOptions = options.length > 0 && typeof options[0] === 'object';

  return (
    <div>
      <label className="block text-[10px] font-medium text-muted mb-0.5">
        {label}
        {required && <span className="text-danger ml-0.5">*</span>}
      </label>
      <select
        value={value}
        onChange={(e) => onChange(e.target.value as T)}
        disabled={disabled}
        className={`${INPUT_CLASS} ${disabled ? 'opacity-50 cursor-not-allowed' : ''}`}
      >
        {isObjectOptions
          ? (options as { value: T; label: string }[]).map((o) => (
              <option key={o.value} value={o.value}>{o.label}</option>
            ))
          : (options as readonly T[]).map((o) => (
              <option key={o} value={o}>{o}</option>
            ))}
      </select>
    </div>
  );
}

export function FieldCheckbox({
  label,
  checked,
  onChange,
  disabled,
}: {
  label: string;
  checked: boolean;
  onChange: (v: boolean) => void;
  disabled?: boolean;
}) {
  return (
    <div className="flex items-center gap-2">
      <input
        type="checkbox"
        checked={checked}
        onChange={(e) => onChange(e.target.checked)}
        disabled={disabled}
        className="rounded border-edge accent-accent"
      />
      <label className="text-xs text-muted">{label}</label>
    </div>
  );
}

export function FieldTagInput({
  label,
  tags,
  onChange,
  placeholder,
}: {
  label: string;
  tags: string[];
  onChange: (tags: string[]) => void;
  placeholder?: string;
}) {
  const [input, setInput] = useState('');

  const addTag = useCallback(() => {
    const trimmed = input.trim();
    if (trimmed && !tags.includes(trimmed)) {
      onChange([...tags, trimmed]);
    }
    setInput('');
  }, [input, tags, onChange]);

  const removeTag = (tag: string) => {
    onChange(tags.filter((t) => t !== tag));
  };

  return (
    <div>
      <label className="block text-[10px] font-medium text-muted mb-0.5">{label}</label>
      <div className="flex flex-wrap gap-1 mb-1">
        {tags.map((tag) => (
          <span key={tag} className="inline-flex items-center gap-0.5 px-1.5 py-0.5 text-[10px] rounded bg-accent/10 text-accent border border-accent/20">
            {tag}
            <button onClick={() => removeTag(tag)} className="hover:text-danger transition-colors">
              <X size={8} />
            </button>
          </span>
        ))}
      </div>
      <input
        type="text"
        value={input}
        onChange={(e) => setInput(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ',') {
            e.preventDefault();
            addTag();
          }
        }}
        onBlur={addTag}
        placeholder={placeholder}
        className={INPUT_CLASS}
      />
    </div>
  );
}

/** Section header inside a builder card. */
export function BuilderSubSection({
  title,
  children,
}: {
  title: string;
  children: React.ReactNode;
}) {
  return (
    <div className="space-y-2">
      <h4 className="text-[10px] font-semibold uppercase tracking-wider text-muted/70 border-b border-edge pb-1">
        {title}
      </h4>
      {children}
    </div>
  );
}
