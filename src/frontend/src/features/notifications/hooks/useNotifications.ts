import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { notificationsApi } from '../api/notifications';
import type { NotificationListParams } from '../types';

export const notificationKeys = {
  all: ['notifications'] as const,
  lists: () => [...notificationKeys.all, 'list'] as const,
  list: (filters?: Record<string, unknown>) =>
    [...notificationKeys.lists(), filters] as const,
  unreadCount: () => [...notificationKeys.all, 'unread-count'] as const,
};

export function useNotificationList(params?: NotificationListParams) {
  return useQuery({
    queryKey: notificationKeys.list(params as Record<string, unknown>),
    queryFn: () => notificationsApi.list(params),
  });
}

export function useUnreadCount() {
  return useQuery({
    queryKey: notificationKeys.unreadCount(),
    queryFn: () => notificationsApi.getUnreadCount(),
    refetchInterval: 30_000,
  });
}

export function useMarkAsRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => notificationsApi.markAsRead(id),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: notificationKeys.lists() }),
        queryClient.invalidateQueries({
          queryKey: notificationKeys.unreadCount(),
        }),
      ]);
    },
  });
}

export function useMarkAsUnread() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => notificationsApi.markAsUnread(id),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: notificationKeys.lists() }),
        queryClient.invalidateQueries({
          queryKey: notificationKeys.unreadCount(),
        }),
      ]);
    },
  });
}

export function useMarkAllAsRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => notificationsApi.markAllAsRead(),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: notificationKeys.lists() }),
        queryClient.invalidateQueries({
          queryKey: notificationKeys.unreadCount(),
        }),
      ]);
    },
  });
}
