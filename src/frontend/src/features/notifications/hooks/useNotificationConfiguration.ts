import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { notificationsApi } from '../api/notifications';
import type {
  UpsertNotificationTemplateRequest,
  UpsertDeliveryChannelRequest,
  UpsertSmtpConfigurationRequest,
} from '../types';

// ── Query keys ───────────────────────────────────────────────────────────────

export const notificationConfigKeys = {
  templates: ['notifications', 'configuration', 'templates'] as const,
  channels: ['notifications', 'configuration', 'channels'] as const,
  smtp: ['notifications', 'configuration', 'smtp'] as const,
};

export const notificationAnalyticsKeys = {
  summary: (days: number) =>
    ['notifications', 'configuration', 'analytics', days] as const,
};

export const notificationDeliveryKeys = {
  history: (notificationId: string) =>
    ['notifications', 'delivery', 'history', notificationId] as const,
  status: (notificationId: string) =>
    ['notifications', 'delivery', 'status', notificationId] as const,
};

// ── Templates ─────────────────────────────────────────────────────────────────

export function useNotificationTemplates(params?: {
  eventType?: string;
  channel?: string;
  isActive?: boolean;
}) {
  return useQuery({
    queryKey: [...notificationConfigKeys.templates, params],
    queryFn: () => notificationsApi.listTemplates(params),
  });
}

export function useUpsertNotificationTemplate() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpsertNotificationTemplateRequest) =>
      notificationsApi.upsertTemplate(data),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: notificationConfigKeys.templates,
      });
    },
  });
}

// ── Channels ──────────────────────────────────────────────────────────────────

export function useDeliveryChannels() {
  return useQuery({
    queryKey: notificationConfigKeys.channels,
    queryFn: () => notificationsApi.listChannels(),
  });
}

export function useUpsertDeliveryChannel() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpsertDeliveryChannelRequest) =>
      notificationsApi.upsertChannel(data),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: notificationConfigKeys.channels,
      });
    },
  });
}

// ── SMTP ──────────────────────────────────────────────────────────────────────

export function useSmtpConfiguration() {
  return useQuery({
    queryKey: notificationConfigKeys.smtp,
    queryFn: () => notificationsApi.getSmtpConfiguration(),
  });
}

export function useNotificationAnalytics(days = 30) {
  return useQuery({
    queryKey: notificationAnalyticsKeys.summary(days),
    queryFn: () => notificationsApi.getAnalytics(days),
    staleTime: 15_000,
  });
}

export function useUpsertSmtpConfiguration() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpsertSmtpConfigurationRequest) =>
      notificationsApi.upsertSmtpConfiguration(data),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: notificationConfigKeys.smtp,
      });
    },
  });
}

// ── P7.2: Delivery History & Status ──────────────────────────────────────────

export function useDeliveryHistory(notificationId: string, enabled = true) {
  return useQuery({
    queryKey: notificationDeliveryKeys.history(notificationId),
    queryFn: () => notificationsApi.getDeliveryHistory(notificationId),
    enabled: enabled && !!notificationId,
  });
}

export function useDeliveryStatus(notificationId: string, enabled = true) {
  return useQuery({
    queryKey: notificationDeliveryKeys.status(notificationId),
    queryFn: () => notificationsApi.getDeliveryStatus(notificationId),
    enabled: enabled && !!notificationId,
  });
}

export function useNotificationTrail(notificationId: string, enabled = true) {
  return useQuery({
    queryKey: ['notifications', 'trail', notificationId] as const,
    queryFn: () => notificationsApi.getNotificationTrail(notificationId),
    enabled: enabled && !!notificationId,
  });
}
