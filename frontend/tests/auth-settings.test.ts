import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest';
import {expectedOidcCallbackUrl, getAuthSettings} from '@/lib/auth-settings';

function validEnvironment() {
  vi.stubEnv('NODE_ENV', 'test');
  vi.stubEnv('PUBLIC_BASE_URL', 'http://localhost:3001');
  vi.stubEnv('OIDC_ISSUER', 'http://localhost:9000/application/o/portfolio/');
  vi.stubEnv('OIDC_CLIENT_ID', 'portfolio-client');
  vi.stubEnv('OIDC_CLIENT_SECRET', 'test-client-secret');
  vi.stubEnv('OIDC_POST_LOGOUT_REDIRECT_URI', 'http://localhost:3001/');
  vi.stubEnv('OIDC_SCOPE', 'openid profile email offline_access');
  vi.stubEnv('BETTER_AUTH_SECRET', 'test-only-better-auth-secret-32-characters');
  vi.stubEnv('AUTH_DATABASE_PATH', '.data/test-auth.db');
}

describe('Better Auth OIDC settings', () => {
  beforeEach(validEnvironment);

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it('builds Authentik discovery and callback URLs from validated settings', () => {
    const settings = getAuthSettings();

    expect(settings.discoveryUrl).toBe('http://localhost:9000/application/o/portfolio/.well-known/openid-configuration');
    expect(settings.scopes).toEqual(['openid', 'profile', 'email', 'offline_access']);
    expect(expectedOidcCallbackUrl(settings.baseUrl)).toBe('http://localhost:3001/api/auth/callback/authentik');
  });

  it('rejects a scope set without openid', () => {
    vi.stubEnv('OIDC_SCOPE', 'profile email');

    expect(() => getAuthSettings()).toThrow('OIDC_SCOPE must include openid.');
  });

  it('rejects secrets too short to safely sign sessions', () => {
    vi.stubEnv('BETTER_AUTH_SECRET', 'too-short');

    expect(() => getAuthSettings()).toThrow('BETTER_AUTH_SECRET must contain at least 32 characters.');
  });

  it('rejects a post-logout redirect on another origin', () => {
    vi.stubEnv('OIDC_POST_LOGOUT_REDIRECT_URI', 'http://localhost:4000/');

    expect(() => getAuthSettings()).toThrow('OIDC_POST_LOGOUT_REDIRECT_URI must use PUBLIC_BASE_URL origin.');
  });
});
