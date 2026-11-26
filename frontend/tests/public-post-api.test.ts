import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest';
import {getPublicPostList} from '@/lib/public-post-api';

const fetchMock = vi.fn<typeof fetch>();
const post = {
  slug: 'accessible-writing',
  publishedAt: '2026-10-01T12:00:00Z',
  title: 'Accessible writing',
  excerpt: 'A short summary.',
  coverImageUrl: 'https://images.example.com/cover.jpg',
  coverImageAlt: 'A notebook on a desk',
  tags: ['accessibility', 'writing']
};

describe('public post API', () => {
  beforeEach(() => {
    vi.stubEnv('BACKEND_INTERNAL_URL', 'http://localhost:3000');
    vi.stubGlobal('fetch', fetchMock);
    fetchMock.mockReset();
  });

  afterEach(() => {
    vi.unstubAllEnvs();
    vi.unstubAllGlobals();
  });

  it('returns safe cover image and topic metadata for previews', async () => {
    fetchMock.mockResolvedValue(Response.json({items: [post], page: 1, pageSize: 10, hasNext: false}));
    await expect(getPublicPostList('en')).resolves.toMatchObject({status: 'success', data: {items: [post]}});
  });

  it('rejects non-HTTPS preview image URLs', async () => {
    fetchMock.mockResolvedValue(Response.json({items: [{...post, coverImageUrl: 'javascript:alert(1)'}], page: 1, pageSize: 10, hasNext: false}));
    await expect(getPublicPostList('en')).resolves.toMatchObject({status: 'malformed-response'});
  });
});
