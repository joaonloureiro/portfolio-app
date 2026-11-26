import 'server-only';

import type {
  AdminProject,
  AdminProjectContentInput,
  AdminProjectEditorInput,
  AdminProjectList,
  AdminProjectResult,
  AdminProjectReorderItem,
  AdminProjectSummary
} from '@/app/admin/admin-types';
import {resolveAdminAccess} from './admin-auth';
import {getAdminBackendEndpoint, isUpstreamRedirect, logAdminBackendError, safeRedirectTarget} from './admin-backend';
import {isInteger, parseAdminProject, parseAdminProjectSummary} from './admin-response-validation';

const ADMIN_PROJECTS_PATH = '/admin/api/projects';
const ADMIN_API_TIMEOUT_MS = 5_000;
const MAX_RESPONSE_BYTES = 2 * 1024 * 1024;

const safeMessages: Readonly<Record<string, string>> = {
  project_invalid_input: 'Check the project fields and try again.',
  project_identifier_conflict: 'The project identifier is already in use. Try again.',
  project_order_conflict: 'That project order is already in use. Reload the project list and try again.',
  project_stale_revision: 'This project changed in another tab. Reload it before saving again.',
  project_invalid_transition: 'That publication action is no longer valid for this project.',
  project_published_delete_conflict: 'Unpublish the project before permanently deleting it.',
  project_not_found: 'The project is no longer available.',
  project_storage_unavailable: 'The project service is unavailable right now.'
};

const fallbackMessages: Readonly<Record<AdminProjectResult<unknown>['status'], string>> = {
  validation: 'Check the project fields and try again.',
  unauthorized: 'Admin authentication could not be verified.',
  forbidden: 'This identity is not authorized for the Admin area.',
  'not-found': 'The project is no longer available.',
  conflict: 'This project changed or the requested action is no longer valid.',
  timeout: 'The project service took too long to respond.',
  'malformed-response': 'The project service returned an invalid response.',
  unavailable: 'The project service is unavailable right now.',
  success: ''
};

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function parseList(value: unknown): AdminProjectList | null {
  if (!isRecord(value) || !Array.isArray(value.items) || !isInteger(value.page) || !isInteger(value.pageSize) ||
      value.pageSize > 50 || typeof value.hasNext !== 'boolean') return null;
  const items = value.items.map(parseAdminProjectSummary);
  return items.every((item): item is AdminProjectSummary => item !== null)
    ? {items, page: value.page, pageSize: value.pageSize, hasNext: value.hasNext}
    : null;
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

async function readErrorKey(response: Response): Promise<string | undefined> {
  const payload = await readJson(response);
  return isRecord(payload) && typeof payload.errorKey === 'string' && payload.errorKey.length <= 80 ? payload.errorKey : undefined;
}

function isAbortError(error: unknown): boolean {
  return error instanceof Error && error.name === 'AbortError';
}

function failure<T>(status: Exclude<AdminProjectResult<T>['status'], 'success'>, errorKey?: string): AdminProjectResult<T> {
  return {status, errorKey, message: errorKey ? safeMessages[errorKey] ?? fallbackMessages[status] : fallbackMessages[status]};
}

async function request<T>(path: string, method: 'GET' | 'POST' | 'PUT' | 'DELETE', body: unknown | undefined, parser: (payload: unknown) => T | null, requestSignal?: AbortSignal): Promise<AdminProjectResult<T>> {
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
      headers: {Accept: 'application/json', Authorization: `Bearer ${authResult.access.accessToken}`, ...(body === undefined ? {} : {'Content-Type': 'application/json'})},
      cache: 'no-store',
      redirect: 'manual',
      signal: timeoutController.signal
    };
    if (body !== undefined) requestInit.body = JSON.stringify(body);
    const response = await fetch(endpoint, requestInit);
    if (isUpstreamRedirect(response.status)) {
      logAdminBackendError('unexpected-upstream-redirect', {path, status: response.status, location: safeRedirectTarget(response, endpoint)});
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
    const parsed = parser(await readJson(response));
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

export function listAdminProjects(page = 1, pageSize = 20, requestSignal?: AbortSignal) {
  return request<AdminProjectList>(`${ADMIN_PROJECTS_PATH}?page=${page}&pageSize=${pageSize}`, 'GET', undefined, parseList, requestSignal);
}

export function getAdminProject(id: number, requestSignal?: AbortSignal) {
  return request<AdminProject>(`${ADMIN_PROJECTS_PATH}/${encodeURIComponent(id)}`, 'GET', undefined, parseAdminProject, requestSignal);
}

export function createAdminProject(input: AdminProjectEditorInput, requestSignal?: AbortSignal) {
  return request<AdminProject>(ADMIN_PROJECTS_PATH, 'POST', input, parseAdminProject, requestSignal);
}

export function updateAdminProject(id: number, expectedRevision: number, input: Omit<AdminProjectEditorInput, 'content'> & {content: AdminProjectContentInput}, requestSignal?: AbortSignal) {
  return request<AdminProject>(`${ADMIN_PROJECTS_PATH}/${encodeURIComponent(id)}`, 'PUT', {expectedRevision, ...input}, parseAdminProject, requestSignal);
}

export function publishAdminProject(id: number, expectedRevision: number, requestSignal?: AbortSignal) {
  return request<AdminProject>(`${ADMIN_PROJECTS_PATH}/${encodeURIComponent(id)}/publish`, 'POST', {expectedRevision}, parseAdminProject, requestSignal);
}

export function unpublishAdminProject(id: number, expectedRevision: number, requestSignal?: AbortSignal) {
  return request<AdminProject>(`${ADMIN_PROJECTS_PATH}/${encodeURIComponent(id)}/unpublish`, 'POST', {expectedRevision}, parseAdminProject, requestSignal);
}

export function reorderAdminProjects(items: readonly AdminProjectReorderItem[], requestSignal?: AbortSignal) {
  return request<void>(`${ADMIN_PROJECTS_PATH}/reorder`, 'POST', {items}, parseUndefined, requestSignal);
}

export function deleteAdminProject(id: number, expectedRevision: number, requestSignal?: AbortSignal) {
  return request<void>(`${ADMIN_PROJECTS_PATH}/${encodeURIComponent(id)}`, 'DELETE', {expectedRevision, confirmation: String(id)}, parseUndefined, requestSignal);
}
