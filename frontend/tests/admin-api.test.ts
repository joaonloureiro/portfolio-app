import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest';
import {getAdminIdentity} from '@/lib/admin-api';
import {resolveAdminAccess} from '@/lib/admin-auth';

vi.mock('@/lib/admin-auth', () => ({resolveAdminAccess: vi.fn()}));

const mockedAccess = vi.mocked(resolveAdminAccess);
const fetchMock = vi.fn<typeof fetch>();

describe('Admin API boundary', () => {
  beforeEach(() => {
    vi.stubEnv('BACKEND_INTERNAL_URL', 'http://localhost:3001');
    vi.stubGlobal('fetch', fetchMock);
    fetchMock.mockReset();
    mockedAccess.mockReset();
    mockedAccess.mockResolvedValue({status: 'success', access: {
      accessToken: 'server-session-access-token',
      accountId: 'authentik-account'
    }});
  });

  afterEach(() => {
    vi.unstubAllEnvs();
    vi.unstubAllGlobals();
  });

  it('forwards only the server-session Bearer token to the protected backend endpoint', async () => {
    fetchMock.mockResolvedValue(Response.json({subject: 'administrator'}));

    const result = await getAdminIdentity();

    expect(result).toEqual({status: 'success', identity: {subject: 'administrator'}});
    const [requestUrl, requestInit] = fetchMock.mock.calls[0] ?? [];
    expect(String(requestUrl)).toBe('http://localhost:3001/admin/api/me');
    expect(requestInit?.method).toBe('GET');
    expect(requestInit?.cache).toBe('no-store');
    expect(requestInit?.redirect).toBe('manual');
    expect(requestInit?.headers).toEqual({Accept: 'application/json', Authorization: 'Bearer server-session-access-token'});
    expect(requestInit?.signal).toBeInstanceOf(AbortSignal);
  });

  it('fails closed when the protected request has no session', async () => {
    mockedAccess.mockResolvedValue({status: 'unauthorized'});

    await expect(getAdminIdentity()).resolves.toEqual({
      status: 'unauthorized',
      message: 'Admin authentication could not be verified.'
    });
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it.each([
    [401, 'unauthorized', 'Admin authentication could not be verified.'],
    [403, 'forbidden', 'This identity is not authorized for the Admin area.']
  ] as const)('maps backend status %s without exposing provider details', async (httpStatus, status, message) => {
    fetchMock.mockResolvedValue(Response.json({errorKey: 'provider_internal_detail'}, {status: httpStatus}));

    await expect(getAdminIdentity()).resolves.toEqual({status, message});
  });

  it('reports malformed identity responses explicitly', async () => {
    fetchMock.mockResolvedValue(Response.json({subject: ''}));

    await expect(getAdminIdentity()).resolves.toEqual({
      status: 'malformed-response',
      message: 'The Admin service returned an invalid response.'
    });
  });

  it('reports an upstream 307 as a redirect error instead of following it', async () => {
    fetchMock.mockResolvedValue(new Response(null, {
      status: 307,
      headers: {Location: 'https://proxy.example/sign-in?secret=hidden'}
    }));

    await expect(getAdminIdentity()).resolves.toEqual({
      status: 'unavailable',
      message: 'The Admin service returned an unexpected redirect.'
    });
  });
});
