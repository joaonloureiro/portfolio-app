import 'server-only';

import type {PublicPost, PublicPostList, PublicPostResult, PublicPostSummary} from './public-post-types';

const PUBLIC_POSTS_PATH = '/api/posts';
const PUBLIC_API_TIMEOUT_MS = 5_000;
const MAX_RESPONSE_BYTES = 2 * 1024 * 1024;

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isDateString(value: unknown): value is string {
  return typeof value === 'string' && value.length > 0 && !Number.isNaN(Date.parse(value));
}

function isNullableString(value: unknown, maxLength: number): value is string | null {
  return value === null || (typeof value === 'string' && value.length <= maxLength);
}

function isSafeImageUrl(value: unknown): value is string | null {
  if (!isNullableString(value, 2_048)) return false;
  if (value === null) return true;
  try { return new URL(value).protocol === 'https:'; } catch { return false; }
}

function isTagArray(value: unknown): value is string[] {
  return Array.isArray(value) && value.length <= 8 && value.every((tag) => typeof tag === 'string' && /^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(tag));
}

function parseSummary(value: unknown): PublicPostSummary | null {
  if (!isRecord(value) || typeof value.slug !== 'string' || value.slug.length < 1 || value.slug.length > 80 ||
      !isDateString(value.publishedAt) || typeof value.title !== 'string' || value.title.length > 120 ||
      typeof value.excerpt !== 'string' || value.excerpt.length > 320 ||
      !isSafeImageUrl(value.coverImageUrl) || !isNullableString(value.coverImageAlt, 200) || !isTagArray(value.tags)) {
    return null;
  }

  return {
    slug: value.slug,
    publishedAt: value.publishedAt,
    title: value.title,
    excerpt: value.excerpt,
    coverImageUrl: value.coverImageUrl,
    coverImageAlt: value.coverImageAlt,
    tags: value.tags
  };
}

function parseList(value: unknown): PublicPostList | null {
  if (!isRecord(value) || !Array.isArray(value.items) || typeof value.page !== 'number' || typeof value.pageSize !== 'number' ||
      !Number.isInteger(value.page) || !Number.isInteger(value.pageSize) ||
      typeof value.hasNext !== 'boolean' || value.page < 1 || value.pageSize < 1 || value.pageSize > 20) {
    return null;
  }

  const items = value.items.map(parseSummary);
  return items.every((item): item is PublicPostSummary => item !== null)
    ? {items, page: value.page, pageSize: value.pageSize, hasNext: value.hasNext}
    : null;
}

function parsePost(value: unknown): PublicPost | null {
  if (!isRecord(value) || typeof value.body !== 'string' || value.body.length > 50_000) return null;
  const summary = parseSummary(value);
  return summary ? {...summary, body: value.body} : null;
}

function getEndpoint(path: string): URL | null {
  const backendBaseUrl = process.env.BACKEND_INTERNAL_URL?.trim();
  if (!backendBaseUrl) return null;

  try {
    const endpoint = new URL(path, backendBaseUrl);
    return endpoint.protocol === 'http:' || endpoint.protocol === 'https:' ? endpoint : null;
  } catch {
    return null;
  }
}

function isAbortError(error: unknown): boolean {
  return error instanceof Error && error.name === 'AbortError';
}

async function readJson(response: Response): Promise<unknown | null> {
  const contentLength = response.headers.get('content-length');
  if (contentLength && Number(contentLength) > MAX_RESPONSE_BYTES) return null;

  try {
    const text = await response.text();
    return new TextEncoder().encode(text).byteLength <= MAX_RESPONSE_BYTES ? JSON.parse(text) as unknown : null;
  } catch {
    return null;
  }
}

async function request<T>(
  path: string,
  parser: (value: unknown) => T | null,
  tags: string[],
  requestSignal?: AbortSignal
): Promise<PublicPostResult<T>> {
  const endpoint = getEndpoint(path);
  if (!endpoint) return {status: 'unavailable', message: 'Posts are unavailable right now.'};

  const timeoutController = new AbortController();
  const timeoutId = setTimeout(() => timeoutController.abort(), PUBLIC_API_TIMEOUT_MS);
  const handleRequestAbort = () => timeoutController.abort();
  requestSignal?.addEventListener('abort', handleRequestAbort, {once: true});
  if (requestSignal?.aborted) timeoutController.abort();

  try {
    const response = await fetch(endpoint, {
      method: 'GET',
      headers: {Accept: 'application/json'},
      next: {revalidate: 60, tags},
      signal: timeoutController.signal
    });

    if (response.status === 404) return {status: 'not-found', message: 'Post not found.'};
    if (!response.ok) return {status: 'unavailable', message: 'Posts are unavailable right now.'};

    const parsed = parser(await readJson(response));
    return parsed === null
      ? {status: 'malformed-response', message: 'Posts returned an invalid response.'}
      : {status: 'success', data: parsed};
  } catch (error: unknown) {
    return isAbortError(error)
      ? {status: 'timeout', message: 'Posts took too long to respond.'}
      : {status: 'unavailable', message: 'Posts are unavailable right now.'};
  } finally {
    clearTimeout(timeoutId);
    requestSignal?.removeEventListener('abort', handleRequestAbort);
  }
}

export function getPublicPostList(locale: 'en' | 'pt', page = 1, pageSize = 10, requestSignal?: AbortSignal) {
  const path = `${PUBLIC_POSTS_PATH}?locale=${locale}&page=${page}&pageSize=${pageSize}`;
  return request(path, parseList, ['public-posts', `public-posts-${locale}`], requestSignal);
}

export function getPublicPost(locale: 'en' | 'pt', slug: string, requestSignal?: AbortSignal) {
  const path = `${PUBLIC_POSTS_PATH}/${encodeURIComponent(slug)}?locale=${locale}`;
  return request(path, parsePost, ['public-posts', `public-post-${slug}`, `public-post-${locale}-${slug}`], requestSignal);
}
