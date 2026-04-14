import { useState, useRef, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { X, Upload, FileCode, BookTemplate, ChevronDown } from 'lucide-react';

// ── Types ─────────────────────────────────────────────────────────────────────

interface CreateRulesetModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (data: CreateRulesetPayload) => void;
  isSubmitting: boolean;
  error?: string | null;
}

export interface CreateRulesetPayload {
  name: string;
  description: string;
  content: string;
  rulesetType: 'Custom' | 'Default';
}

// ── Templates ─────────────────────────────────────────────────────────────────

interface RulesetTemplate {
  id: string;
  nameKey: string;
  nameDefault: string;
  descriptionKey: string;
  descriptionDefault: string;
  content: string;
}

const RULESET_TEMPLATES: RulesetTemplate[] = [
  {
    id: 'openapi-best-practices',
    nameKey: 'contracts.spectral.templates.openapiBestPractices',
    nameDefault: 'OpenAPI Best Practices',
    descriptionKey: 'contracts.spectral.templates.openapiBestPracticesDesc',
    descriptionDefault: 'Comprehensive rules for OpenAPI contract quality: operation metadata, naming conventions, versioning and documentation.',
    content: JSON.stringify(
      {
        rules: {
          'operation-operationId': {
            severity: 'warn',
            description: 'Every operation should have a unique operationId.',
          },
          'operation-description': {
            severity: 'warn',
            description: 'Every operation should include a meaningful description.',
          },
          'operation-tags': {
            severity: 'warn',
            description: 'Every operation should have at least one tag for grouping.',
          },
          'operation-summary': {
            severity: 'info',
            description: 'Operations should include a short summary.',
          },
          'info-contact': {
            severity: 'info',
            description: 'The API info object should include contact information.',
          },
          'info-description': {
            severity: 'warn',
            description: 'The API info object should include a description.',
          },
          'info-license': {
            severity: 'info',
            description: 'The API info object should specify a license.',
          },
          'path-params': {
            severity: 'error',
            description: 'Path parameters declared in the URL must be defined in parameters.',
          },
          'path-kebab-case': {
            severity: 'warn',
            description: 'Paths should use kebab-case naming convention.',
          },
          'path-no-trailing-slash': {
            severity: 'warn',
            description: 'Paths should not end with a trailing slash.',
          },
          'success-response-required': {
            severity: 'error',
            description: 'Every operation must define at least one success response (2xx).',
          },
          'version-semver': {
            severity: 'warn',
            description: 'API version should follow semantic versioning (e.g. 1.0.0).',
          },
          'no-eval-in-markdown': {
            severity: 'error',
            description: 'Markdown descriptions must not contain eval() calls.',
          },
          'no-script-tags-in-markdown': {
            severity: 'error',
            description: 'Markdown descriptions must not contain <script> tags.',
          },
          'typed-enum': {
            severity: 'warn',
            description: 'Enum values should match the declared schema type.',
          },
          'deprecation-documented': {
            severity: 'warn',
            description: 'Deprecated operations should include documentation on alternatives.',
          },
        },
      },
      null,
      2,
    ),
  },
  {
    id: 'security-auth',
    nameKey: 'contracts.spectral.templates.securityAuth',
    nameDefault: 'Security & Authentication',
    descriptionKey: 'contracts.spectral.templates.securityAuthDesc',
    descriptionDefault: 'Rules focused on security definitions, authentication schemes and safe transport requirements.',
    content: JSON.stringify(
      {
        rules: {
          'security-defined': {
            severity: 'error',
            description: 'The API must define at least one security scheme in components/securityDefinitions.',
          },
          'operation-security': {
            severity: 'warn',
            description: 'Every operation should declare security requirements or inherit global security.',
          },
          'security-scheme-type': {
            severity: 'error',
            description: 'Security schemes must use a recognized type (apiKey, http, oauth2, openIdConnect).',
          },
          'https-only': {
            severity: 'error',
            description: 'Server URLs should use HTTPS protocol only.',
          },
          'no-credentials-in-path': {
            severity: 'error',
            description: 'Path parameters must not contain credentials, tokens or keys.',
          },
          'no-credentials-in-query': {
            severity: 'warn',
            description: 'Avoid passing sensitive data (tokens, keys) via query parameters.',
          },
          'oauth-scopes-defined': {
            severity: 'warn',
            description: 'OAuth2 security schemes should define explicit scopes.',
          },
          'api-key-header-preferred': {
            severity: 'info',
            description: 'API key parameters should prefer the header location over query.',
          },
        },
      },
      null,
      2,
    ),
  },
  {
    id: 'schema-data-quality',
    nameKey: 'contracts.spectral.templates.schemaDataQuality',
    nameDefault: 'Schema & Data Quality',
    descriptionKey: 'contracts.spectral.templates.schemaDataQualityDesc',
    descriptionDefault: 'Rules for schema completeness, type safety, examples and data contract quality.',
    content: JSON.stringify(
      {
        rules: {
          'schema-properties-defined': {
            severity: 'warn',
            description: 'Object schemas should have at least one property defined.',
          },
          'schema-type-required': {
            severity: 'error',
            description: 'Every schema must declare a type.',
          },
          'schema-description': {
            severity: 'info',
            description: 'Schemas should include a description for documentation.',
          },
          'examples-required': {
            severity: 'warn',
            description: 'Request bodies and responses should include example values.',
          },
          'request-body-content-type': {
            severity: 'warn',
            description: 'Request bodies should specify the content type (e.g. application/json).',
          },
          'response-content-type': {
            severity: 'warn',
            description: 'Responses with body content should specify the content type.',
          },
          'no-ref-siblings': {
            severity: 'warn',
            description: '$ref should not have sibling properties which are ignored by most parsers.',
          },
          'property-description': {
            severity: 'info',
            description: 'Schema properties should include descriptions for developer experience.',
          },
          'array-items-defined': {
            severity: 'error',
            description: 'Array schemas must define an items schema.',
          },
          'enum-description': {
            severity: 'info',
            description: 'Enum values should be documented with descriptions.',
          },
          'nullable-explicit': {
            severity: 'info',
            description: 'Use explicit nullable: true instead of relying on implicit behaviour.',
          },
          'max-properties-limit': {
            severity: 'warn',
            description: 'Schemas should consider setting maxProperties to prevent overly large payloads.',
          },
        },
      },
      null,
      2,
    ),
  },
];

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * Modal para criação de um novo Spectral Ruleset.
 * Recolhe nome, descrição, tipo e conteúdo do ruleset (JSON/YAML).
 * Valida campos obrigatórios antes de submeter.
 */
export function CreateRulesetModal({ isOpen, onClose, onSubmit, isSubmitting, error }: CreateRulesetModalProps) {
  const { t } = useTranslation();
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [content, setContent] = useState('');
  const [rulesetType, setRulesetType] = useState<'Custom' | 'Default'>('Custom');
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [templateMenuOpen, setTemplateMenuOpen] = useState(false);
  const templateMenuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (templateMenuRef.current && !templateMenuRef.current.contains(e.target as Node)) {
        setTemplateMenuOpen(false);
      }
    };
    if (templateMenuOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [templateMenuOpen]);

  if (!isOpen) return null;

  const loadTemplate = (template: RulesetTemplate) => {
    setName(t(template.nameKey, template.nameDefault));
    setDescription(t(template.descriptionKey, template.descriptionDefault));
    setContent(template.content);
    setTemplateMenuOpen(false);
  };

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};
    if (!name.trim()) {
      newErrors.name = t('contracts.spectral.form.nameRequired', 'Name is required.');
    } else if (name.length > 200) {
      newErrors.name = t('contracts.spectral.form.nameMaxLength', 'Name must be at most 200 characters.');
    }
    if (description.length > 2000) {
      newErrors.description = t('contracts.spectral.form.descriptionMaxLength', 'Description must be at most 2000 characters.');
    }
    if (!content.trim()) {
      newErrors.content = t('contracts.spectral.form.contentRequired', 'Ruleset content is required.');
    }
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = () => {
    if (!validate()) return;
    onSubmit({ name: name.trim(), description: description.trim(), content, rulesetType });
  };

  const handleFileUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = (ev) => {
      const text = ev.target?.result;
      if (typeof text === 'string') {
        setContent(text);
        if (!name.trim()) {
          setName(file.name.replace(/\.(json|ya?ml)$/i, ''));
        }
      }
    };
    reader.readAsText(file);
  };

  const handleClose = () => {
    setName('');
    setDescription('');
    setContent('');
    setRulesetType('Custom');
    setErrors({});
    setTemplateMenuOpen(false);
    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/60 backdrop-blur-sm" onClick={handleClose} />

      {/* Modal */}
      <div className="relative w-full max-w-2xl mx-4 rounded-xl bg-panel border border-edge shadow-2xl max-h-[90vh] flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-edge">
          <div className="flex items-center gap-2">
            <FileCode size={18} className="text-accent" />
            <h2 className="text-base font-semibold text-heading">
              {t('contracts.spectral.form.title', 'Create Ruleset')}
            </h2>
          </div>
          <button type="button" onClick={handleClose} className="text-muted hover:text-heading transition-colors">
            <X size={18} />
          </button>
        </div>

        {/* Body */}
        <div className="flex-1 overflow-y-auto px-6 py-5 space-y-5">
          {/* Name */}
          <div>
            <label className="block text-xs font-medium text-heading mb-1.5">
              {t('contracts.spectral.form.name', 'Name')} <span className="text-danger">*</span>
            </label>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder={t('contracts.spectral.form.namePlaceholder', 'e.g., api-naming-conventions')}
              maxLength={200}
              className="w-full px-3 py-2 text-sm rounded-lg bg-elevated border border-edge text-heading placeholder:text-muted/50 focus:outline-none focus:ring-1 focus:ring-accent/50 focus:border-accent/50"
            />
            {errors.name && <p className="text-xs text-danger mt-1">{errors.name}</p>}
          </div>

          {/* Description */}
          <div>
            <label className="block text-xs font-medium text-heading mb-1.5">
              {t('contracts.spectral.form.description', 'Description')}
            </label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder={t('contracts.spectral.form.descriptionPlaceholder', 'Describe the purpose and scope of this ruleset...')}
              maxLength={2000}
              rows={3}
              className="w-full px-3 py-2 text-sm rounded-lg bg-elevated border border-edge text-heading placeholder:text-muted/50 focus:outline-none focus:ring-1 focus:ring-accent/50 focus:border-accent/50 resize-none"
            />
            {errors.description && <p className="text-xs text-danger mt-1">{errors.description}</p>}
          </div>

          {/* Ruleset Type */}
          <div>
            <label className="block text-xs font-medium text-heading mb-1.5">
              {t('contracts.spectral.form.rulesetType', 'Ruleset Type')}
            </label>
            <div className="flex gap-3">
              {(['Custom', 'Default'] as const).map((type) => (
                <button
                  key={type}
                  type="button"
                  onClick={() => setRulesetType(type)}
                  className={`px-4 py-2 text-xs font-medium rounded-lg border transition-colors ${
                    rulesetType === type
                      ? 'bg-accent/15 text-accent border-accent/25'
                      : 'bg-elevated/50 text-muted border-edge/20 hover:border-accent/30'
                  }`}
                >
                  {t(`contracts.spectral.form.type${type}`, type)}
                </button>
              ))}
            </div>
          </div>

          {/* Content */}
          <div>
            <div className="flex items-center justify-between mb-1.5">
              <label className="text-xs font-medium text-heading">
                {t('contracts.spectral.form.content', 'Ruleset Content')} <span className="text-danger">*</span>
              </label>
              <div className="flex items-center gap-2">
                {/* Template selector */}
                <div className="relative" ref={templateMenuRef}>
                  <button
                    type="button"
                    onClick={() => setTemplateMenuOpen((prev) => !prev)}
                    className="flex items-center gap-1.5 px-3 py-1 text-xs text-accent hover:bg-accent/10 rounded-md transition-colors"
                  >
                    <BookTemplate size={12} />
                    {t('contracts.spectral.form.loadTemplate', 'Load Template')}
                    <ChevronDown size={10} />
                  </button>
                  {templateMenuOpen && (
                    <div className="absolute right-0 top-full mt-1 w-72 rounded-lg bg-panel border border-edge shadow-xl z-10">
                      {RULESET_TEMPLATES.map((tmpl) => (
                        <button
                          key={tmpl.id}
                          type="button"
                          onClick={() => loadTemplate(tmpl)}
                          className="w-full text-left px-4 py-2.5 hover:bg-elevated/60 transition-colors first:rounded-t-lg last:rounded-b-lg"
                        >
                          <p className="text-xs font-medium text-heading">
                            {t(tmpl.nameKey, tmpl.nameDefault)}
                          </p>
                          <p className="text-[10px] text-muted mt-0.5 line-clamp-2">
                            {t(tmpl.descriptionKey, tmpl.descriptionDefault)}
                          </p>
                        </button>
                      ))}
                    </div>
                  )}
                </div>
                {/* Upload file */}
                <label className="flex items-center gap-1.5 px-3 py-1 text-xs text-accent cursor-pointer hover:bg-accent/10 rounded-md transition-colors">
                  <Upload size={12} />
                  {t('contracts.spectral.form.uploadFile', 'Upload File')}
                  <input
                    type="file"
                    accept=".json,.yaml,.yml"
                    onChange={handleFileUpload}
                    className="hidden"
                  />
                </label>
              </div>
            </div>
            <textarea
              value={content}
              onChange={(e) => setContent(e.target.value)}
              placeholder={t('contracts.spectral.form.contentPlaceholder', 'Paste your Spectral ruleset content here (JSON or YAML)...')}
              rows={12}
              className="w-full px-3 py-2 text-xs font-mono rounded-lg bg-elevated border border-edge text-heading placeholder:text-muted/50 focus:outline-none focus:ring-1 focus:ring-accent/50 focus:border-accent/50 resize-none"
            />
            {errors.content && <p className="text-xs text-danger mt-1">{errors.content}</p>}
          </div>
        </div>

        {/* Footer */}
        {error && (
          <div className="px-6 py-2">
            <p className="text-xs text-danger bg-danger/10 border border-danger/20 rounded-lg px-3 py-2">{error}</p>
          </div>
        )}
        <div className="flex items-center justify-end gap-3 px-6 py-4 border-t border-edge">
          <button
            type="button"
            onClick={handleClose}
            className="px-4 py-2 text-xs font-medium rounded-lg text-muted border border-edge hover:bg-elevated/50 transition-colors"
          >
            {t('common.cancel', 'Cancel')}
          </button>
          <button
            type="button"
            onClick={handleSubmit}
            disabled={isSubmitting}
            className="flex items-center gap-1.5 px-4 py-2 text-xs font-medium rounded-lg bg-accent text-white hover:bg-accent/90 transition-colors disabled:opacity-50"
          >
            {isSubmitting
              ? t('common.saving', 'Saving...')
              : t('contracts.spectral.form.submit', 'Create Ruleset')}
          </button>
        </div>
      </div>
    </div>
  );
}
