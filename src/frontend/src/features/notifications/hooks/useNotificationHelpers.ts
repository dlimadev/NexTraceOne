import { useTranslation } from 'react-i18next';
import type { NotificationDto } from '../types';

export function useTimeAgo() {
  const { t } = useTranslation();

  return (dateStr: string): string => {
    const now = Date.now();
    const date = new Date(dateStr).getTime();
    const diffMs = now - date;
    const diffMin = Math.floor(diffMs / 60_000);
    const diffHr = Math.floor(diffMs / 3_600_000);
    const diffDay = Math.floor(diffMs / 86_400_000);

    if (diffMin < 1) return t('notifications.timeAgo.justNow');
    if (diffMin < 60)
      return t('notifications.timeAgo.minutesAgo', { count: diffMin });
    if (diffHr < 24)
      return t('notifications.timeAgo.hoursAgo', { count: diffHr });
    return t('notifications.timeAgo.daysAgo', { count: diffDay });
  };
}

const severityColors: Record<string, string> = {
  Critical: 'text-critical border-critical bg-critical/15',
  Warning: 'text-yellow-400 border-yellow-400 bg-yellow-400/15',
  ActionRequired: 'text-cyan border-cyan bg-cyan-400/15',
  Info: 'text-muted border-edge bg-panel',
};

const severityDotColors: Record<string, string> = {
  Critical: 'bg-critical',
  Warning: 'bg-yellow-400',
  ActionRequired: 'bg-cyan-400',
  Info: 'bg-gray-400',
};

export function getSeverityClasses(severity: string): string {
  return severityColors[severity] ?? severityColors.Info;
}

export function getSeverityDotColor(severity: string): string {
  return severityDotColors[severity] ?? severityDotColors.Info;
}

export function getCategoryKey(category: string): string {
  const map: Record<string, string> = {
    Incident: 'notifications.category.incident',
    Approval: 'notifications.category.approval',
    Change: 'notifications.category.change',
    Contract: 'notifications.category.contract',
    Security: 'notifications.category.security',
    Compliance: 'notifications.category.compliance',
    FinOps: 'notifications.category.finops',
    AI: 'notifications.category.ai',
    Integration: 'notifications.category.integration',
    Platform: 'notifications.category.platform',
    Informational: 'notifications.category.informational',
  };
  return map[category] ?? 'notifications.category.informational';
}

export function getSeverityKey(severity: string): string {
  const map: Record<string, string> = {
    Critical: 'notifications.severity.critical',
    Warning: 'notifications.severity.warning',
    ActionRequired: 'notifications.severity.actionRequired',
    Info: 'notifications.severity.info',
  };
  return map[severity] ?? 'notifications.severity.info';
}

export function isUnread(notification: NotificationDto): boolean {
  return notification.status === 'Unread';
}
