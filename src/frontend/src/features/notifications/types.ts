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

// ── P7.1: Templates ─────────────────────────────────────────────────────────

export interface NotificationTemplateDto {
  id: string;
  eventType: string;
  name: string;
  subjectTemplate: string;
  bodyTemplate: string;
  plainTextTemplate: string | null;
  channel: string | null;
  locale: string;
  isActive: boolean;
  isBuiltIn: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface NotificationTemplatesResponse {
  items: NotificationTemplateDto[];
}

export interface UpsertNotificationTemplateRequest {
  id?: string | null;
  eventType: string;
  name: string;
  subjectTemplate: string;
  bodyTemplate: string;
  plainTextTemplate?: string | null;
  channel?: string | null;
  locale?: string;
}

// ── P7.1: Channels ───────────────────────────────────────────────────────────

export interface DeliveryChannelDto {
  id: string;
  channelType: string;
  displayName: string;
  isEnabled: boolean;
  configurationJson: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface DeliveryChannelsResponse {
  items: DeliveryChannelDto[];
}

export interface UpsertDeliveryChannelRequest {
  id?: string | null;
  channelType: string;
  displayName: string;
  isEnabled: boolean;
  configurationJson?: string | null;
}

// ── P7.1: SMTP ───────────────────────────────────────────────────────────────

export interface SmtpConfigurationDto {
  id: string;
  host: string;
  port: number;
  useSsl: boolean;
  username: string | null;
  fromAddress: string;
  fromName: string;
  baseUrl: string | null;
  isEnabled: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface UpsertSmtpConfigurationRequest {
  host: string;
  port: number;
  useSsl: boolean;
  fromAddress: string;
  fromName: string;
  username?: string | null;
  /** Plain-text password. If null/omitted, the existing password is preserved. */
  password?: string | null;
  baseUrl?: string | null;
  isEnabled: boolean;
}

// ── Shared ───────────────────────────────────────────────────────────────────

export interface UpsertResponse {
  id: string;
  created: boolean;
}

