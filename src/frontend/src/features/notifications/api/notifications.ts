import client from '../../../api/client';
import type {
  NotificationDto,
  NotificationListParams,
  NotificationListResponse,
  UnreadCountResponse,
} from '../types';

export type { NotificationDto };

export const notificationsApi = {
  list: (params?: NotificationListParams) =>
    client
      .get<NotificationListResponse>('/notifications', { params })
      .then((r) => r.data),

  getUnreadCount: () =>
    client
      .get<UnreadCountResponse>('/notifications/unread-count')
      .then((r) => r.data),

  markAsRead: (id: string) =>
    client.post(`/notifications/${id}/read`).then((r) => r.data),

  markAsUnread: (id: string) =>
    client.post(`/notifications/${id}/unread`).then((r) => r.data),

  markAllAsRead: () =>
    client.post('/notifications/mark-all-read').then((r) => r.data),
};
