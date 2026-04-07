import { describe, it, expect } from 'vitest';
import {
  severityBadgeVariant,
  confidenceBadgeVariant,
  statusBadgeVariant,
  riskBadgeVariant,
  contractTypeBadgeInfo,
} from '../../lib/badge-variants';

describe('badge-variants', () => {
  describe('severityBadgeVariant', () => {
    it('maps critical to danger', () => {
      expect(severityBadgeVariant('critical')).toBe('danger');
      expect(severityBadgeVariant('P1')).toBe('danger');
      expect(severityBadgeVariant('sev1')).toBe('danger');
    });

    it('maps high to warning', () => {
      expect(severityBadgeVariant('high')).toBe('warning');
      expect(severityBadgeVariant('P2')).toBe('warning');
    });

    it('maps medium to info', () => {
      expect(severityBadgeVariant('medium')).toBe('info');
    });

    it('maps low to neutral', () => {
      expect(severityBadgeVariant('low')).toBe('neutral');
    });

    it('returns default for unknown', () => {
      expect(severityBadgeVariant('unknown')).toBe('default');
    });
  });

  describe('confidenceBadgeVariant', () => {
    it('maps high scores to success', () => {
      expect(confidenceBadgeVariant(95)).toBe('success');
      expect(confidenceBadgeVariant(80)).toBe('success');
    });

    it('maps medium scores to info', () => {
      expect(confidenceBadgeVariant(70)).toBe('info');
    });

    it('maps low scores to warning', () => {
      expect(confidenceBadgeVariant(50)).toBe('warning');
    });

    it('maps very low scores to danger', () => {
      expect(confidenceBadgeVariant(20)).toBe('danger');
    });
  });

  describe('statusBadgeVariant', () => {
    it('maps active statuses to success', () => {
      expect(statusBadgeVariant('active')).toBe('success');
      expect(statusBadgeVariant('healthy')).toBe('success');
      expect(statusBadgeVariant('resolved')).toBe('success');
    });

    it('maps warning statuses to warning', () => {
      expect(statusBadgeVariant('degraded')).toBe('warning');
      expect(statusBadgeVariant('pending')).toBe('warning');
    });

    it('maps critical statuses to danger', () => {
      expect(statusBadgeVariant('failed')).toBe('danger');
      expect(statusBadgeVariant('incident')).toBe('danger');
    });

    it('maps inactive statuses to neutral', () => {
      expect(statusBadgeVariant('archived')).toBe('neutral');
      expect(statusBadgeVariant('deprecated')).toBe('neutral');
    });
  });

  describe('riskBadgeVariant', () => {
    it('maps critical/very_high to danger', () => {
      expect(riskBadgeVariant('critical')).toBe('danger');
      expect(riskBadgeVariant('very_high')).toBe('danger');
    });

    it('maps low/minimal to success', () => {
      expect(riskBadgeVariant('low')).toBe('success');
      expect(riskBadgeVariant('minimal')).toBe('success');
    });
  });

  describe('contractTypeBadgeInfo', () => {
    it('maps REST to info', () => {
      const result = contractTypeBadgeInfo('rest');
      expect(result.variant).toBe('info');
      expect(result.label).toBe('REST');
    });

    it('maps SOAP to warning', () => {
      const result = contractTypeBadgeInfo('soap');
      expect(result.variant).toBe('warning');
      expect(result.label).toBe('SOAP');
    });

    it('maps event to success', () => {
      const result = contractTypeBadgeInfo('event');
      expect(result.variant).toBe('success');
      expect(result.label).toBe('Event');
    });
  });
});
