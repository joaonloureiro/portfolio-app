import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest';
import {getBuiltInLocaleMessages, getPublicLocaleMessages} from '@/lib/public-locale-api';

const fetchMock = vi.fn<typeof fetch>();

describe('public locale server reader', () => {
  beforeEach(() => {
    vi.stubEnv('BACKEND_INTERNAL_URL', 'http://backend:80');
    vi.stubGlobal('fetch', fetchMock);
    fetchMock.mockReset();
  });

  afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllEnvs();
    vi.unstubAllGlobals();
  });

  it('reads a validated public catalog without an Admin token', async () => {
    fetchMock.mockResolvedValue(Response.json({locale: 'en', revision: 2, messages: getBuiltInLocaleMessages('en')}));

    const result = await getPublicLocaleMessages('en');

    expect(result).toEqual(getBuiltInLocaleMessages('en'));
    const [, requestInit] = fetchMock.mock.calls[0] ?? [];
    expect(requestInit?.headers).toEqual({Accept: 'application/json'});
    expect(requestInit?.next).toEqual({revalidate: 60, tags: ['public-locales', 'public-locale-en']});
  });

  it('falls back to the known-good built-in catalog for malformed upstream data', async () => {
    const warning = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    fetchMock.mockResolvedValue(Response.json({locale: 'en', revision: 2, messages: {hero: {title: 'raw key'}}}));

    await expect(getPublicLocaleMessages('en')).resolves.toEqual(getBuiltInLocaleMessages('en'));
    expect(warning).toHaveBeenCalledWith('public_locale_catalog_fallback', {locale: 'en'});
  });
});
