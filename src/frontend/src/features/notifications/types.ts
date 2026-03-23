export interface NotificationDto {
  id: string;
  title: string;
  message: string;
  category: string;
  severity: string;
  status: string;
  sourceModule: string;
  sourceEntityType: string | null;
  sourceEntityId: string | null;
  actionUrl: string | null;
  requiresAction: boolean;
  createdAt: string;
  readAt: string | null;
}

export interface NotificationListResponse {
  items: NotificationDto[];
  hasMore: boolean;
}

export interface UnreadCountResponse {
  unreadCount: number;
}

export interface NotificationListParams {
  status?: string;
  category?: string;
  severity?: string;
  page?: number;
  pageSize?: number;
}

export interface NotificationPreferenceDto {
  category: string;
  channel: string;
  enabled: boolean;
  isMandatory: boolean;
  updatedAt: string | null;
}

export interface NotificationPreferencesResponse {
  preferences: NotificationPreferenceDto[];
}

export interface UpdatePreferenceRequest {
  category: string;
  channel: string;
  enabled: boolean;
}
