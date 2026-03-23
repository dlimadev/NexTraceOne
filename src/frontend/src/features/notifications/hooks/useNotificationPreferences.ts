import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { notificationsApi } from '../api/notifications';
import type { UpdatePreferenceRequest } from '../types';
import { notificationKeys } from './useNotifications';

export const preferencesKeys = {
  all: [...notificationKeys.all, 'preferences'] as const,
};

export function useNotificationPreferences() {
  return useQuery({
    queryKey: preferencesKeys.all,
    queryFn: () => notificationsApi.getPreferences(),
  });
}

export function useUpdatePreference() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdatePreferenceRequest) =>
      notificationsApi.updatePreference(data),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: preferencesKeys.all,
      });
    },
  });
}
