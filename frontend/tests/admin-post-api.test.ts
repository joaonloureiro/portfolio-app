import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest';
import {getAdminPost, listAdminPosts, updateAdminPost} from '@/lib/admin-post-api';
import {resolveAdminAccess} from '@/lib/admin-auth';

vi.mock('@/lib/admin-auth', () => ({resolveAdminAccess: vi.fn()}));

const mockedSession = vi.mocked(resolveAdminAccess);
const fetchMock = vi.fn<typeof fetch>();

const validPost = {
  id: '123e4567-e89b-12d3-a456-426614174000',
  slug: 'safe-post',
  status: 'draft',
  publishedAt: null,
  updatedAt: '2026-09-09T12:00:00Z',
  revision: 1,
  content: {
    en: {title: '', excerpt: '', body: ''},
    pt: {title: '', excerpt: '', body: ''}
  }
};

describe('Admin post server boundary', () => {
  beforeEach(() => {
    vi.stubEnv('BACKEND_INTERNAL_URL', 'http://localhost:3001');
    vi.stubGlobal('fetch', fetchMock);
    fetchMock.mockReset();
    mockedSession.mockReset();
    mockedSession.mockResolvedValue({status: 'success', access: {
      accessToken: 'server-session-access-token',
      accountId: 'authentik-account'
    }});
  });

  afterEach(() => {
    vi.unstubAllEnvs();
    vi.unstubAllGlobals();
  });

  it('forwards only the Bearer token and required content headers', async () => {
    fetchMock.mockResolvedValue(Response.json({items: [], page: 1, pageSize: 20, hasNext: false}));

    await expect(listAdminPosts()).resolves.toEqual({status: 'success', data: {items: [], page: 1, pageSize: 20, hasNext: false}});
    const [, requestInit] = fetchMock.mock.calls[0] ?? [];
    expect(requestInit?.headers).toEqual({Accept: 'application/json', Authorization: 'Bearer server-session-access-token'});
    expect(requestInit?.cache).toBe('no-store');
    expect(requestInit?.body).toBeUndefined();
  });

  it('rejects malformed backend payloads without exposing their content', async () => {
    fetchMock.mockResolvedValue(Response.json({id: validPost.id, slug: 'safe-post', status: 'draft'}));

    await expect(getAdminPost(validPost.id)).resolves.toEqual({
      status: 'malformed-response',
      message: 'The post service returned an invalid response.'
    });
  });

  it('maps validation and conflict details to safe actionable messages', async () => {
    fetchMock.mockResolvedValueOnce(Response.json({errorKey: 'post_invalid_input'}, {status: 400}));
    await expect(updateAdminPost(validPost.id, 1, validPost.content)).resolves.toEqual({
      status: 'validation',
      errorKey: 'post_invalid_input',
      message: 'Check the post fields and try again.'
    });

    fetchMock.mockResolvedValueOnce(Response.json({errorKey: 'post_stale_revision', detail: 'database internals'}, {status: 409}));
    await expect(updateAdminPost(validPost.id, 1, validPost.content)).resolves.toEqual({
      status: 'conflict',
      errorKey: 'post_stale_revision',
      message: 'This post changed in another tab. Reload it before saving again.'
    });
  });
});
