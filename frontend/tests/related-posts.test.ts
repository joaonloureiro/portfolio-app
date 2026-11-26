import {describe, expect, it} from 'vitest';
import {getRelatedPosts} from '@/lib/related-posts';
import type {PublicPostSummary} from '@/lib/public-post-types';

const post = (slug: string, tags: string[], publishedAt = '2026-10-01T00:00:00Z'): PublicPostSummary => ({
  slug,
  publishedAt,
  title: slug,
  excerpt: `${slug} excerpt`,
  coverImageUrl: null,
  coverImageAlt: null,
  tags
});

describe('related posts', () => {
  it('ranks posts by shared topics, then recency, excluding the current post', () => {
    const current = post('current', ['next-js', 'accessibility']);
    const candidates = [
      post('same-topic-old', ['next-js', 'accessibility'], '2026-09-01T00:00:00Z'),
      post('one-topic-new', ['next-js'], '2026-10-10T00:00:00Z'),
      post('same-topic-new', ['accessibility', 'next-js'], '2026-10-02T00:00:00Z'),
      current,
      post('unrelated', ['csharp'])
    ];

    expect(getRelatedPosts(current, candidates, 2).map(({slug}) => slug)).toEqual(['same-topic-new', 'same-topic-old']);
  });

  it('returns no recommendations when the post has no topics in common', () => {
    expect(getRelatedPosts(post('current', []), [post('other', ['next-js'])])).toEqual([]);
  });
});
