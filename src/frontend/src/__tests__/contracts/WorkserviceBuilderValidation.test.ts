/**
 * FUTURE-ROADMAP 6.1 — Testes unitários da função validateWorkserviceBuilder.
 * São testes puramente de lógica — sem React, sem DOM.
 */
import { describe, it, expect } from 'vitest';
import { validateWorkserviceBuilder } from '../../features/contracts/workspace/builders/shared/builderValidation';
import type {
  WorkserviceBuilderState,
} from '../../features/contracts/workspace/builders/shared/builderTypes';

// ── Helpers ────────────────────────────────────────────────────────────────────

function makeState(overrides: Partial<WorkserviceBuilderState> = {}): WorkserviceBuilderState {
  return {
    name: 'InvoiceExporter',
    trigger: 'Cron',
    schedule: '0 2 * * *',
    description: '',
    inputs: '',
    outputs: '',
    dependencies: [],
    retries: '',
    timeout: '',
    errorHandling: '',
    sideEffects: '',
    owner: '',
    observabilityNotes: '',
    healthCheck: '',
    messagingRole: 'None',
    consumedTopics: [],
    producedTopics: [],
    consumedServices: [],
    producedEvents: [],
    ...overrides,
  };
}

// ── Tests ──────────────────────────────────────────────────────────────────────

describe('validateWorkserviceBuilder', () => {
  it('returns valid when state is complete and correct', () => {
    const result = validateWorkserviceBuilder(makeState());
    expect(result.valid).toBe(true);
    expect(result.errors).toHaveLength(0);
  });

  it('returns error for missing name', () => {
    const result = validateWorkserviceBuilder(makeState({ name: '' }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.field === 'name')).toBe(true);
  });

  it('treats whitespace-only name as missing', () => {
    const result = validateWorkserviceBuilder(makeState({ name: '   ' }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.field === 'name')).toBe(true);
  });

  it('returns scheduleRequired error when trigger is Cron and schedule is empty', () => {
    const result = validateWorkserviceBuilder(makeState({ trigger: 'Cron', schedule: '' }));
    expect(result.valid).toBe(false);
    const err = result.errors.find((e) => e.field === 'schedule');
    expect(err?.messageKey).toBe('contracts.builder.validation.scheduleRequired');
  });

  it('does not require schedule for non-Cron triggers', () => {
    const result = validateWorkserviceBuilder(makeState({ trigger: 'Queue', schedule: '' }));
    expect(result.valid).toBe(true);
  });

  it('returns cronInvalid error when cron expression has fewer than 5 fields', () => {
    const result = validateWorkserviceBuilder(makeState({ trigger: 'Cron', schedule: '0 2 *' }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.cronInvalid')).toBe(true);
  });

  it('accepts valid 5-field Unix cron expression', () => {
    const result = validateWorkserviceBuilder(makeState({ trigger: 'Cron', schedule: '0 2 * * *' }));
    expect(result.valid).toBe(true);
  });

  it('accepts valid 6-field Quartz cron expression', () => {
    const result = validateWorkserviceBuilder(makeState({ trigger: 'Cron', schedule: '0 0 2 * * ?' }));
    expect(result.valid).toBe(true);
  });

  it('returns timeoutInvalid error when timeout is not a valid duration', () => {
    const result = validateWorkserviceBuilder(makeState({ timeout: 'two hours' }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.timeoutInvalid')).toBe(true);
  });

  it('accepts valid duration values for timeout', () => {
    for (const dur of ['30s', '5m', '1h', '2d']) {
      const result = validateWorkserviceBuilder(makeState({ timeout: dur }));
      expect(result.valid, `Expected valid for timeout '${dur}'`).toBe(true);
    }
  });

  it('accepts empty timeout (optional)', () => {
    const result = validateWorkserviceBuilder(makeState({ timeout: '' }));
    expect(result.valid).toBe(true);
  });

  it('returns retriesMustBeNumber error when retries is not numeric', () => {
    const result = validateWorkserviceBuilder(makeState({ retries: 'three' }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.retriesMustBeNumber')).toBe(true);
  });

  it('accepts numeric retries value', () => {
    const result = validateWorkserviceBuilder(makeState({ retries: '3' }));
    expect(result.valid).toBe(true);
  });

  it('accepts empty retries (optional)', () => {
    const result = validateWorkserviceBuilder(makeState({ retries: '' }));
    expect(result.valid).toBe(true);
  });
});
