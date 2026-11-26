import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest';
import {GET as login} from '@/app/auth/login/route';
import {GET as logout} from '@/app/auth/logout/route';
import {getAuth} from '@/lib/auth';
import {getAuthSettings} from '@/lib/auth-settings';
import {clearAdminCsrfCookie, logAdminAuthError, setAdminCsrfCookie} from '@/lib/admin-auth';

vi.mock('@/lib/auth', () => ({getAuth: vi.fn()}));
vi.mock('@/lib/auth-settings', () => ({
  AUTHENTIK_PROVIDER_ID: 'authentik',
  getAuthSettings: vi.fn()
}));
vi.mock('@/lib/admin-auth', () => ({
  clearAdminCsrfCookie: vi.fn(),
  createAdminCsrfToken: () => 'csrf-token',
  logAdminAuthError: vi.fn(),
  setAdminCsrfCookie: vi.fn()
}));

const mockedAuth = vi.mocked(getAuth);
const mockedSettings = vi.mocked(getAuthSettings);
const mockedLog = vi.mocked(logAdminAuthError);
const mockedSetCsrf = vi.mocked(setAdminCsrfCookie);
const mockedClearCsrf = vi.mocked(clearAdminCsrfCookie);
const signInSocial = vi.fn();
const signOut = vi.fn();

describe('OIDC route wrappers', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockedSettings.mockReturnValue({
      baseUrl: 'http://localhost:3001',
      clientId: 'client',
      clientSecret: 'secret',
      databasePath: '.data/auth.db',
      discoveryUrl: 'https://auth.example/.well-known/openid-configuration',
      postLogoutRedirectUrl: 'http://localhost:3001/',
      scopes: ['openid'],
      secret: 'test-only-better-auth-secret-32-characters'
    });
    mockedAuth.mockResolvedValue({api: {signInSocial, signOut}} as never);
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it('starts sign-in with an explicit 302 and preserves the OIDC state cookie', async () => {
    const authHeaders = new Headers();
    authHeaders.append('Set-Cookie', 'portfolio_admin.oauth_state=state; HttpOnly; SameSite=Lax');
    signInSocial.mockResolvedValue({
      headers: authHeaders,
      response: {redirect: false, url: 'https://auth.example/authorize?state=state'}
    });

    const response = await login(new Request('http://localhost:3001/auth/login'));

    expect(response.status).toBe(302);
    expect(response.headers.get('location')).toBe('https://auth.example/authorize?state=state');
    expect(response.headers.get('set-cookie')).toContain('portfolio_admin.oauth_state=state');
    expect(mockedSetCsrf).toHaveBeenCalledWith(response, 'csrf-token');
  });

  it('uses a safe same-origin 302 when provider discovery is unavailable', async () => {
    signInSocial.mockRejectedValue(new Error('provider detail must not reach the browser'));

    const response = await login(new Request('http://localhost:3001/auth/login'));

    expect(response.status).toBe(302);
    expect(response.headers.get('location')).toBe('http://localhost:3001/admin?auth=unavailable');
    expect(mockedLog).toHaveBeenCalledWith('sign-in-start-failed', expect.any(Error));
  });

  it('uses the public origin for failures behind a reverse proxy', async () => {
    vi.stubEnv('PUBLIC_BASE_URL', 'https://portfolio.example');
    vi.stubEnv('NODE_ENV', 'production');
    signInSocial.mockRejectedValue(new Error('discovery unavailable'));

    const response = await login(new Request('http://0.0.0.0:3000/auth/login'));

    expect(response.headers.get('location')).toBe('https://portfolio.example/admin?auth=unavailable');
  });

  it('clears local auth state and redirects with 302 on logout', async () => {
    signOut.mockResolvedValue({
      headers: new Headers({'Set-Cookie': 'portfolio_admin.session_token=; Max-Age=0'}),
      response: {success: true, redirect: false, url: undefined}
    });

    const response = await logout(new Request('http://localhost:3001/auth/logout'));

    expect(response.status).toBe(302);
    expect(response.headers.get('location')).toBe('http://localhost:3001/');
    expect(mockedClearCsrf).toHaveBeenCalledWith(response);
  });
});
