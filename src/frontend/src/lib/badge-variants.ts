/**
 * Funções utilitárias para mapear valores de domínio para variantes de Badge.
 *
 * Centraliza a lógica de mapeamento que anteriormente estava duplicada
 * em IncidentsPage, IncidentDetailPage, ChangeCatalogPage, ChangeDetailPage,
 * ReportsPage e outros.
 *
 * @see docs/DESIGN-SYSTEM.md §4.9
 */

import type { ReactNode } from 'react';

export type BadgeVariant = 'default' | 'neutral' | 'success' | 'warning' | 'danger' | 'info';

/**
 * Mapeia severidade de incidente/alerta para variante de badge.
 */
export function severityBadgeVariant(severity: string): BadgeVariant {
  const s = severity.toLowerCase();
  if (s === 'critical' || s === 'p1' || s === 'sev1') return 'danger';
  if (s === 'high' || s === 'p2' || s === 'sev2') return 'warning';
  if (s === 'medium' || s === 'p3' || s === 'sev3') return 'info';
  if (s === 'low' || s === 'p4' || s === 'sev4') return 'neutral';
  return 'default';
}

/**
 * Mapeia score de confiança (0-100) para variante de badge.
 */
export function confidenceBadgeVariant(score: number): BadgeVariant {
  if (score >= 80) return 'success';
  if (score >= 60) return 'info';
  if (score >= 40) return 'warning';
  return 'danger';
}

/**
 * Mapeia status genérico para variante de badge.
 */
export function statusBadgeVariant(status: string): BadgeVariant {
  const s = status.toLowerCase();
  if (['active', 'healthy', 'resolved', 'approved', 'deployed', 'passing', 'completed', 'online'].includes(s)) return 'success';
  if (['warning', 'degraded', 'pending', 'in_progress', 'review', 'partial'].includes(s)) return 'warning';
  if (['critical', 'failed', 'rejected', 'incident', 'error', 'down', 'offline'].includes(s)) return 'danger';
  if (['info', 'draft', 'new', 'open', 'scheduled'].includes(s)) return 'info';
  if (['inactive', 'archived', 'deprecated', 'closed', 'cancelled'].includes(s)) return 'neutral';
  return 'default';
}

/**
 * Mapeia nível de risco para variante de badge.
 */
export function riskBadgeVariant(level: string): BadgeVariant {
  const l = level.toLowerCase();
  if (l === 'critical' || l === 'very_high') return 'danger';
  if (l === 'high') return 'warning';
  if (l === 'medium') return 'info';
  if (l === 'low' || l === 'minimal') return 'success';
  return 'neutral';
}

/**
 * Mapeia tipo de contrato para ícone e label de badge (útil para UI).
 */
export function contractTypeBadgeInfo(type: string): { variant: BadgeVariant; label: string } {
  const t = type.toLowerCase();
  if (t === 'rest' || t === 'openapi') return { variant: 'info', label: 'REST' };
  if (t === 'soap' || t === 'wsdl') return { variant: 'warning', label: 'SOAP' };
  if (t === 'event' || t === 'asyncapi' || t === 'kafka') return { variant: 'success', label: 'Event' };
  if (t === 'graphql') return { variant: 'info', label: 'GraphQL' };
  return { variant: 'default', label: type };
}
