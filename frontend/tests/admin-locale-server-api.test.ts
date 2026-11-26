import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest';
import englishMessages from '@/locales/en.json';
import portugueseMessages from '@/locales/pt.json';
import {getAdminLocaleCatalog, updateAdminLocaleCatalog} from '@/lib/admin-locale-api';
import {resolveAdminAccess} from '@/lib/admin-auth';

vi.mock('next/cache', () => ({revalidatePath: vi.fn()}));
vi.mock('@/lib/admin-auth', () => ({resolveAdminAccess: vi.fn()}));

const mockedSession = vi.mocked(resolveAdminAccess);
const fetchMock = vi.fn<typeof fetch>();

function editableEntries(english: unknown, portuguese: unknown, path = ''): Array<{key: string; type: 'string' | 'string-list'; en: string | string[]; pt: string | string[]}> {
  if (typeof english === 'string' && typeof portuguese === 'string') {
    return [{key: path, type: 'string', en: english, pt: portuguese}];
  }
  if (Array.isArray(english) && Array.isArray(portuguese) && english.every((item) => typeof item === 'string') && portuguese.every((item) => typeof item === 'string')) {
    return [{key: path, type: 'string-list', en: [...english] as string[], pt: [...portuguese] as string[]}];
  }
  if (typeof english === 'object' && english !== null && typeof portuguese === 'object' && portuguese !== null && !Array.isArray(english) && !Array.isArray(portuguese)) {
    return Object.keys(english).flatMap((key) => editableEntries((english as Record<string, unknown>)[key], (portuguese as Record<string, unknown>)[key], path ? `${path}.${key}` : key));
  }
  return [];
}

const catalog = {revision: 3, entries: editableEntries(englishMessages, portugueseMessages)};

describe('locale Admin server boundary', () => {
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

  it('reads the Admin catalog with only the server-session token', async () => {
    fetchMock.mockResolvedValue(Response.json(catalog));

    await expect(getAdminLocaleCatalog()).resolves.toEqual({status: 'success', data: catalog});
    const [, requestInit] = fetchMock.mock.calls[0] ?? [];
    expect(requestInit?.headers).toEqual({Accept: 'application/json', Authorization: 'Bearer server-session-access-token'});
    expect(requestInit?.cache).toBe('no-store');
  });

  it('maps conflicts and never returns backend detail text', async () => {
    fetchMock.mockResolvedValue(Response.json({errorKey: 'locale_stale_revision', detail: 'sqlite internals'}, {status: 409}));

    await expect(updateAdminLocaleCatalog(3, {['hero.title']: {en: 'New', pt: 'Novo'}})).resolves.toEqual({
      status: 'conflict',
      errorKey: 'locale_stale_revision',
      message: 'This catalog changed in another tab. Reload it before publishing again.'
    });
    const [, requestInit] = fetchMock.mock.calls[0] ?? [];
    expect(String(requestInit?.body)).not.toContain('sqlite internals');
  });
});
