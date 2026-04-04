import client from '../../../api/client';
import type {
  NotificationDto,
  NotificationListParams,
  NotificationListResponse,
  NotificationPreferencesResponse,
  UnreadCountResponse,
  UpdatePreferenceRequest,
  NotificationTemplatesResponse,
  UpsertNotificationTemplateRequest,
  DeliveryChannelsResponse,
  UpsertDeliveryChannelRequest,
  SmtpConfigurationDto,
  UpsertSmtpConfigurationRequest,
  UpsertResponse,
  DeliveryHistoryResponse,
  DeliveryStatusResponse,
  NotificationTrailResponse,
  NotificationAnalyticsResponse,
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

  getPreferences: () =>
    client
      .get<NotificationPreferencesResponse>('/notifications/preferences')
      .then((r) => r.data),

  updatePreference: (data: UpdatePreferenceRequest) =>
    client.put('/notifications/preferences', data).then((r) => r.data),

  // ── P7.1: Templates ───────────────────────────────────────────────────────

  listTemplates: (params?: { eventType?: string; channel?: string; isActive?: boolean }) =>
    client
      .get<NotificationTemplatesResponse>('/notifications/configuration/templates', { params })
      .then((r) => r.data),

  upsertTemplate: (data: UpsertNotificationTemplateRequest) =>
    client
      .put<UpsertResponse>('/notifications/configuration/templates', data)
      .then((r) => r.data),

  // ── P7.1: Channels ────────────────────────────────────────────────────────

  listChannels: () =>
    client
      .get<DeliveryChannelsResponse>('/notifications/configuration/channels')
      .then((r) => r.data),

  upsertChannel: (data: UpsertDeliveryChannelRequest) =>
    client
      .put<UpsertResponse>('/notifications/configuration/channels', data)
      .then((r) => r.data),

  // ── P7.1: SMTP ────────────────────────────────────────────────────────────

  getSmtpConfiguration: () =>
    client
      .get<SmtpConfigurationDto | null>('/notifications/configuration/smtp')
      .then((r) => r.data),

  upsertSmtpConfiguration: (data: UpsertSmtpConfigurationRequest) =>
    client
      .put<UpsertResponse>('/notifications/configuration/smtp', data)
      .then((r) => r.data),

  // ── P7.2: Delivery History & Status ───────────────────────────────────────

  getDeliveryHistory: (notificationId: string) =>
    client
      .get<DeliveryHistoryResponse>(`/notifications/${notificationId}/delivery-history`)
      .then((r) => r.data),

  getDeliveryStatus: (notificationId: string) =>
    client
      .get<DeliveryStatusResponse>(`/notifications/${notificationId}/delivery-status`)
      .then((r) => r.data),

  // ── P7.3: Notification Audit Trail ───────────────────────────────────────

  getNotificationTrail: (notificationId: string) =>
    client
      .get<NotificationTrailResponse>(`/notifications/${notificationId}/trail`)
      .then((r) => r.data),

  getAnalytics: (days = 30) =>
    client
      .get<NotificationAnalyticsResponse>('/notifications/configuration/analytics', {
        params: { days },
      })
      .then((r) => r.data),
};
