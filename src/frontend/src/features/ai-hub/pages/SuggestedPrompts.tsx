import { useTranslation } from 'react-i18next';
import { Sparkles } from 'lucide-react';

interface Props {
  prompts: string[];
  onSelect: (prompt: string) => void;
  visible: boolean;
}

export function SuggestedPrompts({ prompts, onSelect, visible }: Props) {
  const { t } = useTranslation();

  if (!visible) {
    return null;
  }

  return (
    <div className="pt-4">
      <div className="flex items-center gap-2 mb-2">
        <Sparkles size={14} className="text-accent" />
        <p className="text-xs font-medium text-heading">{t('aiHub.suggestedPrompts')}</p>
      </div>
      <p className="text-xs text-muted mb-3">{t('productPolish.aiAssistantPersonaHint')}</p>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
        {prompts.map((promptKey, idx) => (
          <button
            key={idx}
            onClick={() => onSelect(t(promptKey))}
            className="text-left px-3 py-2 rounded-md border border-edge text-sm text-body hover:bg-hover hover:border-accent/30 transition-colors"
          >
            {t(promptKey)}
          </button>
        ))}
      </div>
    </div>
  );
}
