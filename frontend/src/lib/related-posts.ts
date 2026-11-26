import type {PublicPostSummary} from './public-post-types';

export function getRelatedPosts(
  current: PublicPostSummary,
  candidates: readonly PublicPostSummary[],
  limit = 3
): PublicPostSummary[] {
  if (limit < 1 || current.tags.length === 0) return [];

  const currentTags = new Set(current.tags);
  return candidates
    .filter((candidate) => candidate.slug !== current.slug)
    .map((candidate) => ({
      post: candidate,
      sharedTopics: candidate.tags.filter((tag) => currentTags.has(tag)).length
    }))
    .filter(({sharedTopics}) => sharedTopics > 0)
    .sort((left, right) => right.sharedTopics - left.sharedTopics ||
      Date.parse(right.post.publishedAt) - Date.parse(left.post.publishedAt))
    .slice(0, limit)
    .map(({post}) => post);
}
