/**
 * FUTURE-ROADMAP 6.1 — Testes unitários da função validateEventBuilder.
 * São testes puramente de lógica — sem React, sem DOM.
 */
import { describe, it, expect } from 'vitest';
import { validateEventBuilder } from '../../features/contracts/workspace/builders/shared/builderValidation';
import type {
  EventBuilderState,
  EventChannel,
} from '../../features/contracts/workspace/builders/shared/builderTypes';

// ── Helpers ────────────────────────────────────────────────────────────────────

function makeChannel(overrides: Partial<EventChannel> = {}): EventChannel {
  return {
    id: 'ch-1',
    topicName: 'user.created',
    eventName: 'UserCreated',
    version: '1.0',
    keySchema: '',
    payloadSchema: '',
    headers: '',
    producer: 'UserService',
    consumer: '',
    compatibility: 'BACKWARD',
    retention: '',
    partitions: '3',
    ordering: '',
    retries: '',
    dlq: '',
    ...overrides,
  };
}

function makeState(overrides: Partial<EventBuilderState> = {}): EventBuilderState {
  return {
    title: 'User Events',
    version: '1.0.0',
    description: '',
    defaultBroker: '',
    channels: [makeChannel()],
    ...overrides,
  };
}

// ── Tests ──────────────────────────────────────────────────────────────────────

describe('validateEventBuilder', () => {
  it('returns valid when state is complete and correct', () => {
    const result = validateEventBuilder(makeState());
    expect(result.valid).toBe(true);
    expect(result.errors).toHaveLength(0);
  });

  it('returns error for missing title', () => {
    const result = validateEventBuilder(makeState({ title: '' }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.field === 'title')).toBe(true);
  });

  it('treats whitespace-only title as missing', () => {
    const result = validateEventBuilder(makeState({ title: '  ' }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.field === 'title')).toBe(true);
  });

  it('returns error for channel with missing topicName', () => {
    const result = validateEventBuilder(makeState({
      channels: [makeChannel({ topicName: '' })],
    }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.field.includes('ch-1') && e.field.includes('topicName'))).toBe(true);
  });

  it('returns actorRequired error when channel has no producer and no consumer', () => {
    const result = validateEventBuilder(makeState({
      channels: [makeChannel({ producer: '', consumer: '' })],
    }));
    expect(result.valid).toBe(false);
    const err = result.errors.find((e) => e.messageKey === 'contracts.builder.validation.actorRequired');
    expect(err).toBeDefined();
  });

  it('accepts channel with consumer only (no producer)', () => {
    const result = validateEventBuilder(makeState({
      channels: [makeChannel({ producer: '', consumer: 'NotificationService' })],
    }));
    expect(result.valid).toBe(true);
  });

  it('returns topicNameInvalid error when topic contains special characters', () => {
    const result = validateEventBuilder(makeState({
      channels: [makeChannel({ topicName: 'user/created' })],
    }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.topicNameInvalid')).toBe(true);
  });

  it('returns topicNameReserved error for "." topic', () => {
    const result = validateEventBuilder(makeState({
      channels: [makeChannel({ topicName: '.' })],
    }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.topicNameReserved')).toBe(true);
  });

  it('returns topicNameReserved error for ".." topic', () => {
    const result = validateEventBuilder(makeState({
      channels: [makeChannel({ topicName: '..' })],
    }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.topicNameReserved')).toBe(true);
  });

  it('returns topicNameTooLong error when topic exceeds 249 characters', () => {
    const result = validateEventBuilder(makeState({
      channels: [makeChannel({ topicName: 'a'.repeat(250) })],
    }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.topicNameTooLong')).toBe(true);
  });

  it('returns duplicateTopic error when two channels share the same topicName', () => {
    const result = validateEventBuilder(makeState({
      channels: [
        makeChannel({ id: 'ch-1', topicName: 'user.created' }),
        makeChannel({ id: 'ch-2', topicName: 'user.created' }),
      ],
    }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.duplicateTopic')).toBe(true);
  });

  it('allows two channels with different topic names', () => {
    const result = validateEventBuilder(makeState({
      channels: [
        makeChannel({ id: 'ch-1', topicName: 'user.created' }),
        makeChannel({ id: 'ch-2', topicName: 'user.updated' }),
      ],
    }));
    expect(result.valid).toBe(true);
  });

  it('returns partitionsMustBeNumber error when partitions is not numeric', () => {
    const result = validateEventBuilder(makeState({
      channels: [makeChannel({ partitions: 'three' })],
    }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.partitionsMustBeNumber')).toBe(true);
  });

  it('accepts empty partitions (optional)', () => {
    const result = validateEventBuilder(makeState({
      channels: [makeChannel({ partitions: '' })],
    }));
    expect(result.valid).toBe(true);
  });

  it('is valid with no channels (channels are optional)', () => {
    const result = validateEventBuilder(makeState({ channels: [] }));
    expect(result.valid).toBe(true);
  });
});
