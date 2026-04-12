/**
 * FUTURE-ROADMAP 6.1 — Testes unitários da função validateWebhookBuilder.
 * São testes puramente de lógica — sem React, sem DOM.
 */
import { describe, it, expect } from 'vitest';
import { validateWebhookBuilder } from '../../features/contracts/workspace/builders/shared/builderValidation';
import type {
  WebhookBuilderState,
} from '../../features/contracts/workspace/builders/shared/builderTypes';

// ── Helpers ────────────────────────────────────────────────────────────────────

function makeState(overrides: Partial<WebhookBuilderState> = {}): WebhookBuilderState {
  return {
    name: 'OrderCreatedWebhook',
    description: '',
    method: 'POST',
    urlPattern: 'https://example.com/webhook',
    contentType: 'application/json',
    payloadSchema: '',
    headers: [],
    authentication: 'none',
    secretHeaderName: '',
    retryPolicy: '',
    retryCount: '',
    timeout: '',
    events: [],
    owner: '',
    observabilityNotes: '',
    ...overrides,
  };
}

// ── Tests ──────────────────────────────────────────────────────────────────────

describe('validateWebhookBuilder', () => {
  it('returns valid when state is complete and correct', () => {
    const result = validateWebhookBuilder(makeState());
    expect(result.valid).toBe(true);
    expect(result.errors).toHaveLength(0);
  });

  it('returns error for missing name', () => {
    const result = validateWebhookBuilder(makeState({ name: '' }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.field === 'name')).toBe(true);
  });

  it('treats whitespace-only name as missing', () => {
    const result = validateWebhookBuilder(makeState({ name: '   ' }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.field === 'name')).toBe(true);
  });

  it('returns error for missing urlPattern', () => {
    const result = validateWebhookBuilder(makeState({ urlPattern: '' }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.field === 'urlPattern')).toBe(true);
  });

  it('returns endpointMustBeUrl error when urlPattern is not http/https', () => {
    const result = validateWebhookBuilder(makeState({ urlPattern: 'ftp://bad.example.com' }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.endpointMustBeUrl')).toBe(true);
  });

  it('accepts https urlPattern', () => {
    const result = validateWebhookBuilder(makeState({ urlPattern: 'https://secure.example.com/hook' }));
    expect(result.valid).toBe(true);
  });

  it('returns methodRequired error when method is empty', () => {
    // method is typed but test casting allows us to simulate empty
    const state = makeState();
    // @ts-expect-error intentionally testing invalid value
    state.method = '';
    const result = validateWebhookBuilder(state);
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.field === 'method')).toBe(true);
  });

  it('returns secretHeaderRequired error when hmac-sha256 auth has no secretHeaderName', () => {
    const result = validateWebhookBuilder(makeState({
      authentication: 'hmac-sha256',
      secretHeaderName: '',
    }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.secretHeaderRequired')).toBe(true);
  });

  it('returns secretHeaderRequired error when bearer auth has no secretHeaderName', () => {
    const result = validateWebhookBuilder(makeState({
      authentication: 'bearer',
      secretHeaderName: '',
    }));
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.messageKey === 'contracts.builder.validation.secretHeaderRequired')).toBe(true);
  });

  it('does not require secretHeaderName when authentication is none', () => {
    const result = validateWebhookBuilder(makeState({
      authentication: 'none',
      secretHeaderName: '',
    }));
    expect(result.valid).toBe(true);
  });

  it('does not require secretHeaderName when authentication is basic', () => {
    const result = validateWebhookBuilder(makeState({
      authentication: 'basic',
      secretHeaderName: '',
    }));
    expect(result.valid).toBe(true);
  });

  it('is valid with secretHeaderName set for hmac-sha256', () => {
    const result = validateWebhookBuilder(makeState({
      authentication: 'hmac-sha256',
      secretHeaderName: 'X-Hub-Signature-256',
    }));
    expect(result.valid).toBe(true);
  });
});
