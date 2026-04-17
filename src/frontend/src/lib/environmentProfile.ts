/**
 * Utilitários de mapeamento para EnvironmentProfile.
 *
 * O perfil do ambiente é uma classificação operacional armazenada no banco de dados
 * (enum EnvironmentProfile no backend). O nome do ambiente é totalmente livre pelo
 * utilizador (ex: "Dev Teste", "QA-EUROPA") — a UX visual é determinada pelo profile,
 * nunca pelo nome.
 *
 * Valores possíveis (serializados pelo backend como string lowercase):
 *   development | validation | staging | production | sandbox |
 *   disasterrecovery | training | useracceptancetesting | performancetesting | unknown
 */

import {
  Shield,
  Server,
  Beaker,
  Code,
  Box,
  HelpCircle,
  BookOpen,
  Zap,
  HeartPulse,
  GraduationCap,
} from 'lucide-react';
import type { LucideIcon } from 'lucide-react';

/** Tokens de cor por perfil. */
export interface ProfileColors {
  dot: string;
  badge: string;
  bg: string;
}

/**
 * Retorna as classes Tailwind de cor para um perfil de ambiente.
 * Aceita qualquer string; valores desconhecidos recebem o estilo neutro.
 */
export function getProfileColor(profile: string): ProfileColors {
  switch (profile.toLowerCase()) {
    case 'production':
      return { dot: 'bg-critical', badge: 'text-critical border-critical/25 bg-critical/15', bg: 'bg-critical/10' };
    case 'disasterrecovery':
      return { dot: 'bg-critical', badge: 'text-critical border-critical/25 bg-critical/15', bg: 'bg-critical/10' };
    case 'staging':
      return { dot: 'bg-warning', badge: 'text-warning border-warning/25 bg-warning/15', bg: 'bg-warning/10' };
    case 'useracceptancetesting':
      return { dot: 'bg-warning', badge: 'text-warning border-warning/25 bg-warning/15', bg: 'bg-warning/10' };
    case 'validation':
      return { dot: 'bg-info', badge: 'text-info border-info/25 bg-info/15', bg: 'bg-info/10' };
    case 'performancetesting':
      return { dot: 'bg-info', badge: 'text-info border-info/25 bg-info/15', bg: 'bg-info/10' };
    case 'development':
      return { dot: 'bg-success', badge: 'text-success border-success/25 bg-success/15', bg: 'bg-success/10' };
    case 'sandbox':
      return { dot: 'bg-cyan', badge: 'text-cyan border-cyan/25 bg-cyan/15', bg: 'bg-cyan/10' };
    case 'training':
      return { dot: 'bg-cyan', badge: 'text-cyan border-cyan/25 bg-cyan/15', bg: 'bg-cyan/10' };
    default:
      return { dot: 'bg-faded', badge: 'text-faded border-edge', bg: 'bg-elevated' };
  }
}

/**
 * Retorna o ícone Lucide associado ao perfil de ambiente.
 * Aceita qualquer string; valores desconhecidos retornam HelpCircle.
 */
export function getProfileIcon(profile: string): LucideIcon {
  switch (profile.toLowerCase()) {
    case 'production':
      return Shield;
    case 'disasterrecovery':
      return HeartPulse;
    case 'staging':
      return Server;
    case 'useracceptancetesting':
      return Beaker;
    case 'validation':
      return Zap;
    case 'performancetesting':
      return Zap;
    case 'development':
      return Code;
    case 'sandbox':
      return Box;
    case 'training':
      return GraduationCap;
    default:
      return HelpCircle;
  }
}

/**
 * Retorna a variante de Badge para o perfil de ambiente.
 * Aceita qualquer string; valores desconhecidos retornam 'default'.
 */
export function getProfileBadgeVariant(profile: string): 'default' | 'warning' | 'danger' | 'success' {
  switch (profile.toLowerCase()) {
    case 'production':
    case 'disasterrecovery':
      return 'danger';
    case 'staging':
    case 'useracceptancetesting':
      return 'warning';
    case 'development':
    case 'sandbox':
    case 'training':
      return 'success';
    default:
      return 'default';
  }
}

/**
 * Indica se um perfil implica comportamento similar a produção.
 * Baseado na mesma lógica do backend (Environment.IsProductionLike).
 */
export function isProductionLikeProfile(profile: string): boolean {
  switch (profile.toLowerCase()) {
    case 'production':
    case 'disasterrecovery':
    case 'staging':
    case 'useracceptancetesting':
      return true;
    default:
      return false;
  }
}
