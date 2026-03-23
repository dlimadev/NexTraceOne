import React from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import {
  getCategoryKey,
  getSeverityDotColor,
  isUnread,
  useTimeAgo,
} from '../hooks/useNotificationHelpers';
import { useMarkAsRead } from '../hooks/useNotifications';
import type { NotificationDto } from '../types';

interface NotificationItemProps {
  notification: NotificationDto;
  compact?: boolean;
  onInteract?: () => void;
}

export function NotificationItem({
  notification,
  compact = false,
  onInteract,
}: NotificationItemProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const timeAgo = useTimeAgo();
  const markAsRead = useMarkAsRead();
  const unread = isUnread(notification);

  const handleClick = () => {
    if (unread) {
      markAsRead.mutate(notification.id);
    }
    if (notification.actionUrl) {
      navigate(notification.actionUrl);
    }
    onInteract?.();
  };

  return (
    <button
      type="button"
      onClick={handleClick}
      className={`w-full text-left flex items-start gap-3 px-3 py-2.5 rounded-md transition-all hover:bg-hover group ${
        unread ? 'bg-accent-muted/5' : ''
      }`}
    >
      {/* Severity dot */}
      <span
        className={`mt-1.5 h-2 w-2 shrink-0 rounded-full ${getSeverityDotColor(notification.severity)}`}
        aria-hidden="true"
      />

      <div className="min-w-0 flex-1">
        {/* Title */}
        <p
          className={`text-sm leading-tight truncate ${
            unread ? 'font-semibold text-heading' : 'text-body'
          }`}
        >
          {notification.title}
        </p>

        {/* Message preview */}
        {!compact && (
          <p className="mt-0.5 text-xs text-muted line-clamp-2">
            {notification.message}
          </p>
        )}

        {/* Meta row */}
        <div className="mt-1 flex items-center gap-2 text-xs text-muted">
          <span className="inline-flex items-center rounded bg-panel px-1.5 py-0.5 text-[10px] font-medium uppercase tracking-wide border border-edge">
            {t(getCategoryKey(notification.category))}
          </span>
          <span>{timeAgo(notification.createdAt)}</span>
        </div>
      </div>

      {/* Unread indicator */}
      {unread && (
        <span
          className="mt-1.5 h-2 w-2 shrink-0 rounded-full bg-cyan-400"
          aria-label={t('notifications.filter.unread')}
        />
      )}
    </button>
  );
}
