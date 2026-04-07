import { Star } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

interface BookmarkButtonProps {
  entityType: string;
  entityId: string;
  displayName: string;
  url?: string;
}

interface BookmarkItem {
  id: string;
  entityType: string;
  entityId: string;
  displayName: string;
  url?: string;
  createdAt: string;
}

interface BookmarksResponse {
  items: BookmarkItem[];
}

async function fetchBookmarks(): Promise<BookmarksResponse> {
  const resp = await fetch('/api/v1/bookmarks');
  if (!resp.ok) throw new Error('Failed to fetch bookmarks');
  return resp.json();
}

async function addBookmark(entityType: string, entityId: string, displayName: string, url?: string): Promise<BookmarkItem> {
  const resp = await fetch('/api/v1/bookmarks', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ entityType, entityId, displayName, url }),
  });
  if (!resp.ok) throw new Error('Failed to add bookmark');
  return resp.json();
}

async function removeBookmark(id: string): Promise<void> {
  const resp = await fetch(`/api/v1/bookmarks/${id}`, { method: 'DELETE' });
  if (!resp.ok) throw new Error('Failed to remove bookmark');
}

export function BookmarkButton({ entityType, entityId, displayName, url }: BookmarkButtonProps) {
  const { t } = useTranslation('bookmarks');
  const queryClient = useQueryClient();

  const { data } = useQuery({
    queryKey: ['bookmarks'],
    queryFn: fetchBookmarks,
  });

  const existing = data?.items?.find(
    b => b.entityType === entityType && b.entityId === entityId
  );
  const isBookmarked = !!existing;

  const addMutation = useMutation({
    mutationFn: () => addBookmark(entityType, entityId, displayName, url),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['bookmarks'] }),
  });

  const removeMutation = useMutation({
    mutationFn: () => removeBookmark(existing!.id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['bookmarks'] }),
  });

  const isPending = addMutation.isPending || removeMutation.isPending;

  const handleClick = () => {
    if (isPending) return;
    if (isBookmarked) {
      removeMutation.mutate();
    } else {
      addMutation.mutate();
    }
  };

  return (
    <button
      type="button"
      onClick={handleClick}
      disabled={isPending}
      title={isBookmarked ? t('remove') : t('add')}
      className="text-warning hover:text-warning/80 transition-colors cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
      aria-label={isBookmarked ? t('remove') : t('add')}
    >
      <Star
        size={16}
        className={isBookmarked ? 'fill-current' : ''}
      />
    </button>
  );
}

export default BookmarkButton;
