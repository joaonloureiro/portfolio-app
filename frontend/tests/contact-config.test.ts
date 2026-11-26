import {afterEach, describe, expect, it, vi} from 'vitest';

describe('public contact configuration', () => {
  afterEach(() => {
    vi.resetModules();
    vi.unstubAllEnvs();
  });

  it('uses the configured public contact address', async () => {
    vi.stubEnv('NEXT_PUBLIC_CONTACT_EMAIL', ' configured@example.test ');
    vi.resetModules();

    const {contactEmail} = await import('@/lib/contact-config');

    expect(contactEmail).toBe('configured@example.test');
  });

  it('keeps the existing address as the safe local fallback', async () => {
    vi.stubEnv('NEXT_PUBLIC_CONTACT_EMAIL', '');
    vi.resetModules();

    const {contactEmail} = await import('@/lib/contact-config');

    expect(contactEmail).toBe('me@joaoloureiro.dev.br');
  });
});
