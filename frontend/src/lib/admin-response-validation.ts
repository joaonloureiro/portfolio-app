import type {
  AdminPost,
  AdminPostContent,
  AdminPostStatus,
  AdminPostSummary,
  AdminProject,
  AdminProjectLocalizedContent,
  AdminProjectStatus,
  AdminProjectSummary
} from '@/app/admin/admin-types';

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isDateString(value: unknown): value is string {
  return typeof value === 'string' && value.length > 0 && !Number.isNaN(Date.parse(value));
}

function isNullableString(value: unknown): value is string | null {
  return value === null || typeof value === 'string';
}

function isStringArray(value: unknown): value is string[] {
  return Array.isArray(value) && value.every((item) => typeof item === 'string');
}

function isTagArray(value: unknown): value is string[] {
  return Array.isArray(value) && value.length <= 8 && value.every((item) => typeof item === 'string' && /^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(item));
}

function isSafeCoverImageUrl(value: unknown): value is string | null {
  if (value === null) return true;
  if (typeof value !== 'string' || value.length > 2_048) return false;
  try { return new URL(value).protocol === 'https:'; } catch { return false; }
}

function isUuid(value: unknown): value is string {
  return typeof value === 'string' && /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
}

function isPostStatus(value: unknown): value is AdminPostStatus {
  return value === 'draft' || value === 'published' || value === 'unpublished';
}

function isProjectStatus(value: unknown): value is AdminProjectStatus {
  return value === 'draft' || value === 'published' || value === 'unpublished';
}

export function isInteger(value: unknown, minimum = 1): value is number {
  return typeof value === 'number' && Number.isInteger(value) && value >= minimum;
}

export function parseAdminPostContent(value: unknown): AdminPostContent | null {
  if (!isRecord(value) || typeof value.title !== 'string' || typeof value.excerpt !== 'string' || typeof value.body !== 'string' ||
      (value.coverImageAlt !== null && typeof value.coverImageAlt !== 'string') ||
      value.title.length > 120 || value.excerpt.length > 320 || value.body.length > 50_000 ||
      (typeof value.coverImageAlt === 'string' && value.coverImageAlt.length > 200)) {
    return null;
  }

  return {title: value.title, excerpt: value.excerpt, body: value.body, coverImageAlt: value.coverImageAlt};
}

export function parseAdminPostSummary(value: unknown): AdminPostSummary | null {
  if (!isRecord(value) || !isUuid(value.id) || typeof value.slug !== 'string' || !isPostStatus(value.status) ||
      (value.publishedAt !== null && !isDateString(value.publishedAt)) || !isDateString(value.updatedAt) ||
      !isInteger(value.revision)) {
    return null;
  }

  return {
    id: value.id,
    slug: value.slug,
    status: value.status,
    publishedAt: value.publishedAt,
    updatedAt: value.updatedAt,
    revision: value.revision
  };
}

export function parseAdminPost(value: unknown): AdminPost | null {
  if (!isRecord(value)) return null;

  const summary = parseAdminPostSummary(value);
  const content = isRecord(value.content)
    ? {en: parseAdminPostContent(value.content.en), pt: parseAdminPostContent(value.content.pt)}
    : null;

  return summary && content?.en && content.pt && isSafeCoverImageUrl(value.coverImageUrl) && isTagArray(value.tags)
    ? {...summary, content: {en: content.en, pt: content.pt}, coverImageUrl: value.coverImageUrl, tags: value.tags}
    : null;
}

export function parseAdminProjectLocalizedContent(value: unknown): AdminProjectLocalizedContent | null {
  if (!isRecord(value) || typeof value.title !== 'string' || value.title.length > 120 ||
      typeof value.summary !== 'string' || value.summary.length > 320 ||
      typeof value.details !== 'string' || value.details.length > 10_000 ||
      typeof value.featureHeading !== 'string' || value.featureHeading.length > 120 ||
      !isStringArray(value.features) || value.features.length < 1 || value.features.length > 20 ||
      value.features.some((feature) => feature.length > 500)) {
    return null;
  }

  return {
    title: value.title,
    summary: value.summary,
    details: value.details,
    featureHeading: value.featureHeading,
    features: value.features
  };
}

export function parseAdminProjectSummary(value: unknown): AdminProjectSummary | null {
  if (!isRecord(value) || !isInteger(value.id) || !isInteger(value.sortOrder) || !isProjectStatus(value.status) ||
      typeof value.imageReference !== 'string' || value.imageReference.length === 0 || !isRecord(value.titles) ||
      typeof value.titles.en !== 'string' || typeof value.titles.pt !== 'string' ||
      (value.publishedAt !== null && !isDateString(value.publishedAt)) || !isDateString(value.updatedAt) ||
      !isInteger(value.revision)) {
    return null;
  }

  return {
    id: value.id,
    sortOrder: value.sortOrder,
    status: value.status,
    imageReference: value.imageReference,
    titles: {en: value.titles.en, pt: value.titles.pt},
    publishedAt: value.publishedAt,
    updatedAt: value.updatedAt,
    revision: value.revision
  };
}

export function parseAdminProject(value: unknown): AdminProject | null {
  if (!isRecord(value)) return null;

  const summary = parseAdminProjectSummary(value);
  const content = isRecord(value.content)
    ? {en: parseAdminProjectLocalizedContent(value.content.en), pt: parseAdminProjectLocalizedContent(value.content.pt)}
    : null;
  const repositoryUrl = value.repositoryUrl;
  const liveUrl = value.liveUrl;
  const technologies = value.technologies;

  if (!summary || !isNullableString(repositoryUrl) || !isNullableString(liveUrl) || !isStringArray(technologies) ||
      technologies.length < 1 || technologies.length > 20 ||
      technologies.some((technology) => technology.length < 1 || technology.length > 60) ||
      !content?.en || !content.pt) return null;

  return {
    ...summary,
    repositoryUrl,
    liveUrl,
    technologies,
    content: {en: content.en, pt: content.pt}
  };
}
