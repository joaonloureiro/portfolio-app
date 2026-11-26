'use client';

import type {
  AdminProjectContentInput,
  AdminProjectEditorInput,
  AdminProjectResult,
  AdminProjectReorderItem
} from '@/app/admin/admin-types';
import {parseAdminProject} from './admin-response-validation';

const ADMIN_PROJECTS_PATH = '/admin/api/projects';

const messages: Readonly<Record<string, string>> = {
  project_invalid_input: 'Check the project fields and try again.',
  project_order_conflict: 'That project order is already in use. Reload the list and try again.',
  project_stale_revision: 'This project changed in another tab. Reload it before saving again.',
  project_invalid_transition: 'That publication action is no longer valid for this project.',
  project_published_delete_conflict: 'Unpublish the project before permanently deleting it.',
  project_not_found: 'The project is no longer available.',
  csrf_invalid: 'This form expired. Reload the page and try again.'
};

const fallback: Readonly<Record<Exclude<AdminProjectResult<unknown>['status'], 'success'>, string>> = {
  validation: 'Check the project fields and try again.',
  unauthorized: 'Admin authentication could not be verified.',
  forbidden: 'This identity is not authorized for the Admin area.',
  'not-found': 'The project is no longer available.',
  conflict: 'This project changed or the requested action is no longer valid.',
  timeout: 'The project service took too long to respond.',
  'malformed-response': 'The project service returned an invalid response.',
  unavailable: 'The project service is unavailable right now.'
};

function csrfToken(): string | null {
  const item = document.cookie.split(';').map((part) => part.trim()).find((part) => part.startsWith('portfolio_admin_csrf='));
  if (!item) return null;
  try { return decodeURIComponent(item.slice('portfolio_admin_csrf='.length)); } catch { return null; }
}

function errorKey(payload: unknown): string | undefined {
  return typeof payload === 'object' && payload !== null && 'errorKey' in payload && typeof payload.errorKey === 'string'
    ? payload.errorKey
    : undefined;
}

function failure<T>(status: Exclude<AdminProjectResult<T>['status'], 'success'>, key?: string): AdminProjectResult<T> {
  return {status, errorKey: key, message: key ? messages[key] ?? fallback[status] : fallback[status]};
}

function readErrorStatus(status: number): Exclude<AdminProjectResult<unknown>['status'], 'success'> | null {
  if (status === 400 || status === 415) return 'validation';
  if (status === 401) return 'unauthorized';
  if (status === 403) return 'forbidden';
  if (status === 404) return 'not-found';
  if (status === 409) return 'conflict';
  if (status === 504) return 'timeout';
  return status >= 500 ? 'unavailable' : null;
}

async function request<T>(path: string, method: 'POST' | 'PUT' | 'DELETE', body: unknown, parse: (value: unknown) => T | null): Promise<AdminProjectResult<T>> {
  const token = csrfToken();
  if (!token) return failure('forbidden', 'csrf_invalid');
  try {
    const response = await fetch(path, {
      method,
      headers: {'Accept': 'application/json', 'Content-Type': 'application/json', 'X-CSRF-Token': token},
      body: JSON.stringify(body),
      credentials: 'same-origin',
      cache: 'no-store'
    });
    let payload: unknown = null;
    if (response.status !== 204) {
      try { payload = await response.json(); } catch { payload = null; }
    }
    const status = readErrorStatus(response.status);
    if (status) return failure(status, errorKey(payload));
    if (!response.ok) return failure('unavailable', errorKey(payload));
    const data = parse(payload);
    return data === null ? failure('malformed-response') : {status: 'success', data};
  } catch {
    return failure('unavailable');
  }
}

export async function createAdminProject(input: AdminProjectEditorInput) {
  return request(ADMIN_PROJECTS_PATH, 'POST', input, parseAdminProject);
}

export async function updateAdminProject(id: number, expectedRevision: number, input: AdminProjectEditorInput & {content: AdminProjectContentInput}) {
  return request(`${ADMIN_PROJECTS_PATH}/${encodeURIComponent(id)}`, 'PUT', {expectedRevision, ...input}, parseAdminProject);
}

export async function publishAdminProject(id: number, expectedRevision: number) {
  return request(`${ADMIN_PROJECTS_PATH}/${encodeURIComponent(id)}/publish`, 'POST', {expectedRevision}, parseAdminProject);
}

export async function unpublishAdminProject(id: number, expectedRevision: number) {
  return request(`${ADMIN_PROJECTS_PATH}/${encodeURIComponent(id)}/unpublish`, 'POST', {expectedRevision}, parseAdminProject);
}

export async function reorderAdminProjects(items: readonly AdminProjectReorderItem[]) {
  return request<void>(`${ADMIN_PROJECTS_PATH}/reorder`, 'POST', {items}, () => undefined);
}

export async function deleteAdminProject(id: number, expectedRevision: number) {
  return request<void>(`${ADMIN_PROJECTS_PATH}/${encodeURIComponent(id)}`, 'DELETE', {expectedRevision, confirmation: String(id)}, () => undefined);
}
