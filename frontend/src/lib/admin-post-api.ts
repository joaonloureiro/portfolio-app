import 'server-only';

import type {
  AdminPost,
  AdminPostContentInput,
  AdminPostEditorInput,
  AdminPostList,
  AdminPostResult,
  AdminPostSummary
} from '@/app/admin/admin-types';
import {resolveAdminAccess} from './admin-auth';
import {getAdminBackendEndpoint, isUpstreamRedirect, logAdminBackendError, safeRedirectTarget} from './admin-backend';
import {parseAdminPost, parseAdminPostSummary} from './admin-response-validation';

const ADMIN_POSTS_PATH = '/admin/api/posts';
const ADMIN_API_TIMEOUT_MS = 5_000;
const MAX_RESPONSE_BYTES = 2 * 1024 * 1024;

const safeMessages: Readonly<Record<string, string>> = {
  post_invalid_input: 'Check the post fields and try again.',
  post_slug_conflict: 'That slug is already in use.',
  post_stale_revision: 'This post changed in another tab. Reload it before saving again.',
  post_invalid_transition: 'That publication action is no longer valid for this post.',
  post_published_delete_conflict: 'Unpublish the post before permanently deleting it.',
  post_not_found: 'The post is no longer available.',
  post_storage_unavailable: 'The post service is unavailable right now.'
};

const fallbackMessages: Readonly<Record<AdminPostResult<unknown>['status'], string>> = {
  validation: 'Check the post fields and try again.',
  unauthorized: 'Admin authentication could not be verified.',
  forbidden: 'This identity is not authorized for the Admin area.',
  'not-found': 'The post is no longer available.',
  conflict: 'This post changed or the requested action is no longer valid.',
  timeout: 'The post service took too long to respond.',
  'malformed-response': 'The post service returned an invalid response.',
  unavailable: 'The post service is unavailable right now.',
  success: ''
};

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function parseList(value: unknown): AdminPostList | null {
  if (!isRecord(value) || !Array.isArray(value.items) || typeof value.page !== 'number' || typeof value.pageSize !== 'number' ||
      !Number.isInteger(value.page) || !Number.isInteger(value.pageSize) ||
      typeof value.hasNext !== 'boolean' || value.page < 1 || value.pageSize < 1 || value.pageSize > 50) {
    return null;
  }

  const items = value.items.map(parseAdminPostSummary);
  return items.every((item): item is AdminPostSummary => item !== null)
    ? {items, page: value.page, pageSize: value.pageSize, hasNext: value.hasNext}
    : null;
}

async function readJson(response: Response): Promise<unknown | null> {
  const contentLength = response.headers.get('content-length');
  if (contentLength && Number(contentLength) > MAX_RESPONSE_BYTES) {
    return null;
  }

  try {
    const text = await response.text();
    return new TextEncoder().encode(text).byteLength <= MAX_RESPONSE_BYTES ? JSON.parse(text) as unknown : null;
  } catch {
    return null;
  }
}

async function readErrorKey(response: Response): Promise<string | undefined> {
  const payload = await readJson(response);
  return isRecord(payload) && typeof payload.errorKey === 'string' && payload.errorKey.length <= 80
    ? payload.errorKey
    : undefined;
}

function isAbortError(error: unknown): boolean {
  return error instanceof Error && error.name === 'AbortError';
}

function failure<T>(status: Exclude<AdminPostResult<T>['status'], 'success'>, errorKey?: string): AdminPostResult<T> {
  return {status, errorKey, message: errorKey ? safeMessages[errorKey] ?? fallbackMessages[status] : fallbackMessages[status]};
}

async function request<T>(
  path: string,
  method: 'GET' | 'POST' | 'PUT' | 'DELETE',
  body: unknown | undefined,
  parser: (payload: unknown) => T | null,
  requestSignal?: AbortSignal
): Promise<AdminPostResult<T>> {
  const authResult = await resolveAdminAccess();
  if (authResult.status === 'unavailable') return failure('unavailable');
  if (authResult.status === 'unauthorized') return failure('unauthorized');

  const endpoint = getAdminBackendEndpoint(path);
  if (!endpoint) {
    logAdminBackendError('invalid-backend-configuration', {path});
    return failure('unavailable');
  }

  const timeoutController = new AbortController();
  const timeoutId = setTimeout(() => timeoutController.abort(), ADMIN_API_TIMEOUT_MS);
  const handleRequestAbort = () => timeoutController.abort();
  requestSignal?.addEventListener('abort', handleRequestAbort, {once: true});
  if (requestSignal?.aborted) timeoutController.abort();

  try {
    const requestInit: RequestInit = {
      method,
      headers: {
        Accept: 'application/json',
        Authorization: `Bearer ${authResult.access.accessToken}`,
        ...(body === undefined ? {} : {'Content-Type': 'application/json'})
      },
      cache: 'no-store',
      redirect: 'manual',
      signal: timeoutController.signal
    };
    if (body !== undefined) requestInit.body = JSON.stringify(body);

    const response = await fetch(endpoint, requestInit);
    if (isUpstreamRedirect(response.status)) {
      logAdminBackendError('unexpected-upstream-redirect', {
        path,
        status: response.status,
        location: safeRedirectTarget(response, endpoint)
      });
      return failure('unavailable', 'admin_upstream_redirect');
    }
    if (response.status === 401) return failure('unauthorized', await readErrorKey(response));
    if (response.status === 403) return failure('forbidden', await readErrorKey(response));
    if (response.status === 404) return failure('not-found', await readErrorKey(response));
    if (response.status === 400) return failure('validation', await readErrorKey(response));
    if (response.status === 409) return failure('conflict', await readErrorKey(response));
    if (response.status === 503) return failure('unavailable', await readErrorKey(response));
    if (!response.ok) return failure('unavailable', await readErrorKey(response));
    if (method === 'DELETE' && response.status === 204) return {status: 'success', data: undefined as T};

    const payload = await readJson(response);
    const parsed = parser(payload);
    return parsed === null ? failure('malformed-response') : {status: 'success', data: parsed};
  } catch (error: unknown) {
    const timeout = isAbortError(error);
    logAdminBackendError(timeout ? 'upstream-timeout' : 'upstream-request-failed', {path}, error);
    return timeout ? failure('timeout') : failure('unavailable');
  } finally {
    clearTimeout(timeoutId);
    requestSignal?.removeEventListener('abort', handleRequestAbort);
  }
}

function parseUndefined(value: unknown): undefined | null {
  return value === null ? undefined : null;
}

export function listAdminPosts(page = 1, pageSize = 20, requestSignal?: AbortSignal) {
  return request<AdminPostList>(`${ADMIN_POSTS_PATH}?page=${page}&pageSize=${pageSize}`, 'GET', undefined, parseList, requestSignal);
}

export function getAdminPost(id: string, requestSignal?: AbortSignal) {
  return request<AdminPost>(`${ADMIN_POSTS_PATH}/${encodeURIComponent(id)}`, 'GET', undefined, parseAdminPost, requestSignal);
}

export function createAdminPost(input: AdminPostEditorInput, requestSignal?: AbortSignal) {
  return request<AdminPost>(ADMIN_POSTS_PATH, 'POST', input, parseAdminPost, requestSignal);
}

export function updateAdminPost(id: string, expectedRevision: number, content: AdminPostContentInput, coverImageUrl: string | null = null, tags: readonly string[] = [], requestSignal?: AbortSignal) {
  return request(`${ADMIN_POSTS_PATH}/${encodeURIComponent(id)}`, 'PUT', {expectedRevision, content, coverImageUrl, tags}, parseAdminPost, requestSignal);
}

export function publishAdminPost(id: string, expectedRevision: number, requestSignal?: AbortSignal) {
  return request<AdminPost>(`${ADMIN_POSTS_PATH}/${encodeURIComponent(id)}/publish`, 'POST', {expectedRevision}, parseAdminPost, requestSignal);
}

export function unpublishAdminPost(id: string, expectedRevision: number, requestSignal?: AbortSignal) {
  return request<AdminPost>(`${ADMIN_POSTS_PATH}/${encodeURIComponent(id)}/unpublish`, 'POST', {expectedRevision}, parseAdminPost, requestSignal);
}

export function deleteAdminPost(id: string, expectedRevision: number, requestSignal?: AbortSignal) {
  return request<void>(`${ADMIN_POSTS_PATH}/${encodeURIComponent(id)}`, 'DELETE', {expectedRevision, confirmation: id}, parseUndefined, requestSignal);
}
