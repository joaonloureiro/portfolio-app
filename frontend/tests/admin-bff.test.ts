import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest';
import {GET, POST, PUT} from '@/app/admin/api/[...path]/route';
import {refreshAdminAccess, resolveAdminAccess} from '@/lib/admin-auth';

vi.mock('next/cache', () => ({revalidatePath: vi.fn()}));
vi.mock('@/lib/admin-auth', () => ({
  ADMIN_CSRF_COOKIE: 'portfolio_admin_csrf',
  clearAdminCsrfCookie: (response: Response) => response.headers.append('Set-Cookie', 'portfolio_admin_csrf=; Max-Age=0'),
  refreshAdminAccess: vi.fn(),
  resolveAdminAccess: vi.fn()
}));

const mockedAccess = vi.mocked(resolveAdminAccess);
const mockedRefresh = vi.mocked(refreshAdminAccess);
const fetchMock = vi.fn<typeof fetch>();

const access = {
  accessToken: 'server-access-token',
  accountId: 'authentik-account'
};

function context(path: string[]) {
  return {params: Promise.resolve({path})};
}

describe('Admin frontend BFF', () => {
  beforeEach(() => {
    vi.stubEnv('BACKEND_INTERNAL_URL', 'http://backend:80');
    vi.stubEnv('PUBLIC_BASE_URL', 'http://localhost:3000');
    vi.stubGlobal('fetch', fetchMock);
    fetchMock.mockReset();
    mockedRefresh.mockReset();
    mockedRefresh.mockResolvedValue({status: 'unauthorized'});
    mockedAccess.mockReset();
    mockedAccess.mockResolvedValue({status: 'success', access});
  });

  afterEach(() => {
    vi.unstubAllEnvs();
    vi.unstubAllGlobals();
  });

  it('returns 401 without a session and never calls the backend', async () => {
    mockedAccess.mockResolvedValue({status: 'unauthorized'});

    const response = await GET(new Request('http://localhost/admin/api/me'), context(['me']));

    expect(response.status).toBe(401);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('forwards only the server Bearer token to an allowlisted backend path', async () => {
    fetchMock.mockResolvedValue(Response.json({subject: 'administrator'}));
    const request = new Request('http://localhost/admin/api/me', {
      headers: {Authorization: 'Bearer browser-supplied-token', Cookie: 'unrelated=value'}
    });

    const response = await GET(request, context(['me']));

    expect(response.status).toBe(200);
    const [, init] = fetchMock.mock.calls[0] ?? [];
    const upstreamHeaders = new Headers(init?.headers);
    expect(upstreamHeaders.get('Accept')).toBe('application/json');
    expect(upstreamHeaders.get('Authorization')).toBe('Bearer server-access-token');
    expect(upstreamHeaders.has('Cookie')).toBe(false);
    expect(init?.redirect).toBe('manual');
  });

  it('requires the CSRF token for state-changing requests', async () => {
    const response = await POST(
      new Request('http://localhost/admin/api/posts', {
        method: 'POST',
        headers: {'Content-Type': 'application/json', Origin: 'http://localhost:3000'},
        body: JSON.stringify({slug: 'post'})
      }),
      context(['posts'])
    );

    expect(response.status).toBe(403);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('preserves an authenticated backend 403 response', async () => {
    fetchMock.mockResolvedValue(Response.json({errorKey: 'forbidden'}, {status: 403}));
    const response = await POST(
      new Request('http://localhost/admin/api/posts', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Cookie: 'portfolio_admin_csrf=csrf-value',
          'X-CSRF-Token': 'csrf-value',
          Origin: 'http://localhost:3000'
        },
        body: JSON.stringify({slug: 'post'})
      }),
      context(['posts'])
    );

    expect(response.status).toBe(403);
    expect(await response.json()).toEqual({errorKey: 'forbidden'});
  });

  it('proxies only allowlisted project paths with the server token and CSRF protection', async () => {
    fetchMock.mockResolvedValue(Response.json({id: 1, status: 'draft'}));
    const response = await POST(
      new Request('http://localhost/admin/api/projects/1/publish', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Cookie: 'portfolio_admin_csrf=csrf-value',
          'X-CSRF-Token': 'csrf-value',
          Origin: 'http://localhost:3000'
        },
        body: JSON.stringify({expectedRevision: 1})
      }),
      context(['projects', '1', 'publish'])
    );

    expect(response.status).toBe(200);
    const [, init] = fetchMock.mock.calls[0] ?? [];
    const upstreamHeaders = new Headers(init?.headers);
    expect(upstreamHeaders.get('Authorization')).toBe('Bearer server-access-token');
    expect(upstreamHeaders.has('Cookie')).toBe(false);
  });

  it('proxies only the locale catalog path and keeps the server token server-side', async () => {
    fetchMock.mockResolvedValue(Response.json({revision: 2, entries: []}));
    const response = await PUT(
      new Request('http://localhost/admin/api/locales', {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          Cookie: 'portfolio_admin_csrf=csrf-value',
          'X-CSRF-Token': 'csrf-value',
          Origin: 'http://localhost:3000'
        },
        body: JSON.stringify({expectedRevision: 1, changes: {}})
      }),
      context(['locales'])
    );

    expect(response.status).toBe(200);
    const [, init] = fetchMock.mock.calls[0] ?? [];
    const upstreamHeaders = new Headers(init?.headers);
    expect(upstreamHeaders.get('Authorization')).toBe('Bearer server-access-token');
    expect(upstreamHeaders.has('Cookie')).toBe(false);
  });

  it('proxies the bounded email policy and test paths only', async () => {
    fetchMock.mockResolvedValue(Response.json({enabled: true, revision: 1, transportConfigured: true}));
    const response = await GET(new Request('http://localhost/admin/api/email-delivery'), context(['email-delivery']));
    expect(response.status).toBe(200);
    expect(String(fetchMock.mock.calls[0]?.[0])).toBe('http://backend/admin/api/email-delivery');

    fetchMock.mockReset();
    fetchMock.mockResolvedValue(Response.json({message: 'Test delivery sent.'}));
    const testResponse = await POST(
      new Request('http://localhost/admin/api/email-delivery/test', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Cookie: 'portfolio_admin_csrf=csrf-value',
          'X-CSRF-Token': 'csrf-value',
          Origin: 'http://localhost:3000'
        },
        body: '{}'
      }),
      context(['email-delivery', 'test'])
    );
    expect(testResponse.status).toBe(200);
  });

  it('does not proxy paths outside the explicit allowlist', async () => {
    const response = await GET(new Request('http://localhost/admin/api/secrets'), context(['secrets']));

    expect(response.status).toBe(404);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('returns a specific 502 problem for an unexpected upstream 307', async () => {
    fetchMock.mockResolvedValue(new Response(null, {status: 307, headers: {Location: '/login?token=hidden'}}));

    const response = await GET(new Request('http://localhost/admin/api/me'), context(['me']));

    expect(response.status).toBe(502);
    expect(await response.json()).toMatchObject({errorKey: 'admin_upstream_redirect'});
    expect(response.headers.has('location')).toBe(false);
  });

  it('rejects mismatched CSRF tokens of equal or different lengths', async () => {
    for (const headerToken of ['wrong-valu', 'wrong-value-different-length']) {
      const response = await POST(
        new Request('http://localhost/admin/api/posts', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            Cookie: 'portfolio_admin_csrf=csrf-value',
            'X-CSRF-Token': headerToken,
            Origin: 'http://localhost:3000'
          },
          body: JSON.stringify({slug: 'post'})
        }),
        context(['posts'])
      );

      expect(response.status).toBe(403);
      expect(await response.json()).toMatchObject({errorKey: 'csrf_invalid'});
      expect(fetchMock).not.toHaveBeenCalled();
    }
  });
});
