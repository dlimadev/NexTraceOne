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

export interface NotificationAnalyticsWindowDto {
  days: number;
  from: string;
  until: string;
}

export interface NotificationTypeCountDto {
  eventType: string;
  count: number;
}

export interface NotificationPlatformMetricsDto {
  totalGenerated: number;
  byCategory: Record<string, number>;
  bySeverity: Record<string, number>;
  bySourceModule: Record<string, number>;
  deliveriesByChannel: Record<string, number>;
  totalDelivered: number;
  totalFailed: number;
  totalPending: number;
  totalSkipped: number;
}

export interface NotificationInteractionMetricsDto {
  totalRead: number;
  totalUnread: number;
  totalAcknowledged: number;
  totalSnoozed: number;
  totalArchived: number;
  totalDismissed: number;
  totalEscalated: number;
  readRate: number;
  acknowledgeRate: number;
  averageTimeToReadMinutes: number;
  averageTimeToAcknowledgeMinutes: number;
  totalUnacknowledgedActionRequired: number;
}

export interface NotificationQualityMetricsDto {
  averagePerUserPerDay: number;
  totalSuppressed: number;
  totalGrouped: number;
  totalCorrelatedWithIncidents: number;
  topNoisyTypes: NotificationTypeCountDto[];
  leastEngagedTypes: NotificationTypeCountDto[];
  unacknowledgedActionTypes: NotificationTypeCountDto[];
}

export interface NotificationAnalyticsResponse {
  window: NotificationAnalyticsWindowDto;
  platform: NotificationPlatformMetricsDto;
  interaction: NotificationInteractionMetricsDto;
  quality: NotificationQualityMetricsDto;
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

// ── P7.2: Delivery History & Status ─────────────────────────────────────────

export interface DeliveryEntryDto {
  id: string;
  channel: string;
  status: string;
  recipientAddress: string | null;
  retryCount: number;
  createdAt: string;
  lastAttemptAt: string | null;
  deliveredAt: string | null;
  failedAt: string | null;
  nextRetryAt: string | null;
  errorMessage: string | null;
}

export interface DeliveryHistoryResponse {
  notificationId: string;
  deliveries: DeliveryEntryDto[];
  totalAttempts: number;
  hasSuccessfulDelivery: boolean;
}

export interface ChannelStatusDto {
  channel: string;
  status: string;
  retryCount: number;
  lastAttemptAt: string | null;
  deliveredAt: string | null;
  nextRetryAt: string | null;
  lastError: string | null;
}

export interface DeliveryStatusResponse {
  notificationId: string;
  isDeliveredToAnyChannel: boolean;
  hasPendingRetry: boolean;
  hasPermanentFailure: boolean;
  totalChannelAttempts: number;
  channelStatuses: ChannelStatusDto[];
}

// ── P7.3: Notification Audit Trail ──────────────────────────────────────────

export interface NotificationCorrelationDto {
  notificationId: string;
  eventType: string;
  sourceModule: string;
  sourceEntityType: string | null;
  sourceEntityId: string | null;
  sourceEventId: string | null;
  category: string;
  severity: string;
  status: string;
  recipientUserId: string;
  createdAt: string;
  readAt: string | null;
  requiresAction: boolean;
}

export interface DeliveryTrailEntryDto {
  deliveryId: string;
  channel: string;
  status: string;
  retryCount: number;
  createdAt: string;
  lastAttemptAt: string | null;
  deliveredAt: string | null;
  failedAt: string | null;
  nextRetryAt: string | null;
  errorMessage: string | null;
}

export interface NotificationTrailResponse {
  notificationId: string;
  notification: NotificationCorrelationDto;
  deliveries: DeliveryTrailEntryDto[];
  totalDeliveryAttempts: number;
  isDeliveredToAnyChannel: boolean;
  hasPendingRetry: boolean;
  hasPermanentFailure: boolean;
}

