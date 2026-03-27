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
