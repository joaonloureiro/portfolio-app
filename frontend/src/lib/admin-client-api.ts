'use client';

import type {
  AdminPostContentInput,
  AdminPostEditorInput,
  AdminPostFailureStatus,
  AdminPostResult
} from '@/app/admin/admin-types';
import {parseAdminPost} from './admin-response-validation';

const ADMIN_POSTS_PATH = '/admin/api/posts';

const messages: Readonly<Record<string, string>> = {
  post_invalid_input: 'Check the post fields and try again.',
  post_slug_conflict: 'That slug is already in use.',
  post_stale_revision: 'This post changed in another tab. Reload it before saving again.',
  post_invalid_transition: 'That publication action is no longer valid for this post.',
  post_published_delete_conflict: 'Unpublish the post before permanently deleting it.',
  post_not_found: 'The post is no longer available.',
  csrf_invalid: 'This form expired. Reload the page and try again.'
};

const fallback: Readonly<Record<AdminPostFailureStatus, string>> = {
  validation: 'Check the post fields and try again.',
  unauthorized: 'Admin authentication could not be verified.',
  forbidden: 'This identity is not authorized for the Admin area.',
  'not-found': 'The post is no longer available.',
  conflict: 'This post changed or the requested action is no longer valid.',
  timeout: 'The post service took too long to respond.',
  'malformed-response': 'The post service returned an invalid response.',
  unavailable: 'The post service is unavailable right now.'
};

function csrfToken(): string | null {
  const item = document.cookie.split(';').map((part) => part.trim()).find((part) => part.startsWith('portfolio_admin_csrf='));
  if (!item) return null;
  try {
    return decodeURIComponent(item.slice('portfolio_admin_csrf='.length));
  } catch {
    return null;
  }
}

function errorKey(payload: unknown): string | undefined {
  return typeof payload === 'object' && payload !== null && 'errorKey' in payload &&
    typeof payload.errorKey === 'string' ? payload.errorKey : undefined;
}

function failure<T>(status: AdminPostFailureStatus, key?: string): AdminPostResult<T> {
  return {status, errorKey: key, message: key ? messages[key] ?? fallback[status] : fallback[status]};
}

function statusForResponse(status: number): AdminPostFailureStatus | null {
  if (status === 400 || status === 415) return 'validation';
  if (status === 401) return 'unauthorized';
  if (status === 403) return 'forbidden';
  if (status === 404) return 'not-found';
  if (status === 409) return 'conflict';
  if (status === 504) return 'timeout';
  return status >= 500 ? 'unavailable' : null;
}

async function request<T>(path: string, method: 'POST' | 'PUT' | 'DELETE', body: unknown, parse: (value: unknown) => T | null): Promise<AdminPostResult<T>> {
  const token = csrfToken();
  if (!token) return failure('forbidden', 'csrf_invalid');

  try {
    const response = await fetch(path, {
      method,
      headers: {Accept: 'application/json', 'Content-Type': 'application/json', 'X-CSRF-Token': token},
      body: JSON.stringify(body),
      credentials: 'same-origin',
      cache: 'no-store'
    });
    let payload: unknown = null;
    if (response.status !== 204) {
      try { payload = await response.json(); } catch { payload = null; }
    }
    const failureStatus = statusForResponse(response.status);
    if (failureStatus) return failure(failureStatus, errorKey(payload));
    if (!response.ok) return failure('unavailable', errorKey(payload));
    const data = parse(payload);
    return data === null ? failure('malformed-response') : {status: 'success', data};
  } catch {
    return failure('unavailable');
  }
}

export function createAdminPost(input: AdminPostEditorInput) {
  return request(ADMIN_POSTS_PATH, 'POST', input, parseAdminPost);
}

export function updateAdminPost(id: string, expectedRevision: number, content: AdminPostContentInput, coverImageUrl: string | null = null, tags: readonly string[] = []) {
  return request(`${ADMIN_POSTS_PATH}/${encodeURIComponent(id)}`, 'PUT', {expectedRevision, content, coverImageUrl, tags}, parseAdminPost);
}

export function publishAdminPost(id: string, expectedRevision: number) {
  return request(`${ADMIN_POSTS_PATH}/${encodeURIComponent(id)}/publish`, 'POST', {expectedRevision}, parseAdminPost);
}

export function unpublishAdminPost(id: string, expectedRevision: number) {
  return request(`${ADMIN_POSTS_PATH}/${encodeURIComponent(id)}/unpublish`, 'POST', {expectedRevision}, parseAdminPost);
}

export function deleteAdminPost(id: string, expectedRevision: number) {
  return request<void>(`${ADMIN_POSTS_PATH}/${encodeURIComponent(id)}`, 'DELETE', {expectedRevision, confirmation: id}, () => undefined);
}
