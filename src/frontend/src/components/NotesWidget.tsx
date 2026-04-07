import { useState, useRef, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { Edit3, Eye, FileText } from 'lucide-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import client from '../api/client';

const MAX_CHARS = 5000;
const AUTO_SAVE_DELAY_MS = 2000;

function useNotesContent(userId: string) {
  return useQuery({
    queryKey: ['user-preference', userId, 'home.widget.notes.content'],
    queryFn: () =>
      client
        .get<{ value: string }>(`/api/v1/configuration/user-preferences/${userId}`, {
          params: { key: 'home.widget.notes.content' },
        })
        .then((r) => r.data?.value ?? ''),
    staleTime: 60_000,
  });
}

function useSaveNotes(userId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (content: string) =>
      client.put('/api/v1/configuration/preferences', {
        userId,
        key: 'home.widget.notes.content',
        value: content,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['user-preference', userId] });
    },
  });
}

interface Props {
  userId?: string;
}

/** Widget de notas pessoais em markdown com auto-save. */
export function NotesWidget({ userId = 'current' }: Props) {
  const { t } = useTranslation();
  const { data: savedContent } = useNotesContent(userId);
  const { mutate: saveNotes } = useSaveNotes(userId);

  const [content, setContent] = useState('');
  const [isEditing, setIsEditing] = useState(false);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [initialized, setInitialized] = useState(false);

  // Initialize content from server data once (avoids ref access during render)
  if (savedContent !== undefined && !initialized) {
    setInitialized(true);
    if (content !== savedContent) {
      setContent(savedContent);
    }
  }

  const handleChange = useCallback(
    (value: string) => {
      if (value.length > MAX_CHARS) return;
      setContent(value);
      if (timerRef.current) clearTimeout(timerRef.current);
      timerRef.current = setTimeout(() => saveNotes(value), AUTO_SAVE_DELAY_MS);
    },
    [saveNotes],
  );

  return (
    <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg p-4 h-full flex flex-col">
      <div className="flex items-center justify-between mb-3">
        <div className="flex items-center gap-2">
          <FileText className="w-4 h-4 text-gray-500" />
          <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
            {t('notesWidget.title')}
          </span>
        </div>
        <button
          onClick={() => setIsEditing((e) => !e)}
          className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition-colors"
          aria-label={isEditing ? t('notesWidget.preview') : t('notesWidget.edit')}
        >
          {isEditing ? <Eye className="w-4 h-4" /> : <Edit3 className="w-4 h-4" />}
        </button>
      </div>

      <div className="flex-1 overflow-auto">
        {isEditing ? (
          <textarea
            value={content}
            onChange={(e) => handleChange(e.target.value)}
            className="w-full h-full min-h-[160px] text-sm text-gray-700 dark:text-gray-300 bg-transparent border-none resize-none focus:outline-none placeholder-gray-400"
            placeholder={t('notesWidget.placeholder')}
          />
        ) : (
          <div className="prose prose-sm dark:prose-invert max-w-none text-gray-600 dark:text-gray-300">
            {content ? (
              <pre className="whitespace-pre-wrap text-sm font-sans">{content}</pre>
            ) : (
              <p className="text-gray-400 text-sm italic">{t('notesWidget.empty')}</p>
            )}
          </div>
        )}
      </div>

      {isEditing && (
        <div className="mt-2 text-xs text-gray-400 text-right">
          {content.length}/{MAX_CHARS}
        </div>
      )}
    </div>
  );
}
