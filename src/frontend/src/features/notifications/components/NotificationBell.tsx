import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { Bell, CheckCheck } from 'lucide-react';
import {
  useNotificationList,
  useUnreadCount,
  useMarkAllAsRead,
} from '../hooks/useNotifications';
import { NotificationItem } from './NotificationItem';

export function NotificationBell() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);

  const { data: unreadData } = useUnreadCount();
  const { data: listData, isLoading } = useNotificationList({
    pageSize: 5,
  });
  const markAllAsRead = useMarkAllAsRead();

  const unreadCount = unreadData?.unreadCount ?? 0;
  const notifications = listData?.items ?? [];

  const handleMarkAllRead = () => {
    markAllAsRead.mutate();
  };

  const handleViewAll = () => {
    setOpen(false);
    navigate('/notifications');
  };

  return (
    <div className="relative">
      {/* Bell button */}
      <button
        type="button"
        onClick={() => setOpen((prev) => !prev)}
        className="relative p-2 rounded-md text-muted hover:bg-hover hover:text-body transition-all"
        aria-label={t('notifications.title')}
      >
        <Bell className="h-[18px] w-[18px]" />
        {unreadCount > 0 && (
          <span className="absolute -top-0.5 -right-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-critical px-1 text-[10px] font-bold text-white">
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {/* Dropdown */}
      {open && (
        <>
          {/* Click-outside overlay */}
          <div
            className="fixed inset-0 z-[var(--z-dropdown)]"
            onClick={() => setOpen(false)}
            aria-hidden="true"
          />

          {/* Panel */}
          <div className="absolute right-0 top-full mt-1.5 z-[calc(var(--z-dropdown)+1)] w-[380px] bg-elevated border border-edge rounded-lg shadow-floating animate-fade-in">
            {/* Header */}
            <div className="flex items-center justify-between px-4 py-3 border-b border-edge">
              <h3 className="text-sm font-semibold text-heading">
                {t('notifications.dropdownTitle')}
              </h3>
              {unreadCount > 0 && (
                <button
                  type="button"
                  onClick={handleMarkAllRead}
                  disabled={markAllAsRead.isPending}
                  className="flex items-center gap-1 text-xs text-cyan hover:text-cyan/80 transition-colors disabled:opacity-50"
                >
                  <CheckCheck className="h-3.5 w-3.5" />
                  {t('notifications.markAllRead')}
                </button>
              )}
            </div>

            {/* Notification list */}
            <div className="max-h-[360px] overflow-y-auto">
              {isLoading && (
                <div className="flex items-center justify-center py-8">
                  <div className="h-5 w-5 animate-spin rounded-full border-2 border-edge border-t-cyan" />
                </div>
              )}

              {!isLoading && notifications.length === 0 && (
                <div className="py-8 text-center">
                  <Bell className="mx-auto h-8 w-8 text-muted/40" />
                  <p className="mt-2 text-sm text-muted">
                    {t('notifications.noNotifications')}
                  </p>
                  <p className="mt-0.5 text-xs text-muted/60">
                    {t('notifications.noNotificationsDescription')}
                  </p>
                </div>
              )}

              {!isLoading &&
                notifications.map((n) => (
                  <NotificationItem
                    key={n.id}
                    notification={n}
                    compact
                    onInteract={() => setOpen(false)}
                  />
                ))}
            </div>

            {/* Footer */}
            {notifications.length > 0 && (
              <div className="border-t border-edge px-4 py-2.5">
                <button
                  type="button"
                  onClick={handleViewAll}
                  className="w-full text-center text-xs font-medium text-cyan hover:text-cyan/80 transition-colors"
                >
                  {t('notifications.viewAll')}
                </button>
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );
}
