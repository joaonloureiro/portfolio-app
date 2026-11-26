import 'server-only';

import {revalidatePath} from 'next/cache';
import type {
  AdminLocaleCatalog,
  AdminLocaleChange,
  AdminLocaleFailureStatus,
  AdminLocaleResult
} from '@/app/admin/admin-types';
import {resolveAdminAccess} from './admin-auth';
import {getAdminBackendEndpoint, isUpstreamRedirect, logAdminBackendError, safeRedirectTarget} from './admin-backend';
import {parseAdminLocaleCatalog} from './locale-catalog-validation';

const ADMIN_LOCALES_PATH = '/admin/api/locales';
const ADMIN_API_TIMEOUT_MS = 5_000;
const MAX_RESPONSE_BYTES = 512 * 1024;

const safeMessages: Readonly<Record<string, string>> = {
  locale_invalid_shape: 'Check the locale fields and try again.',
  locale_unknown_key: 'That locale key is not editable.',
  locale_invalid_value: 'One or more locale values exceed their limits.',
  locale_stale_revision: 'This catalog changed in another tab. Reload it before publishing again.',
  locale_storage_unavailable: 'The locale catalog is unavailable right now.'
};

const fallbackMessages: Readonly<Record<AdminLocaleFailureStatus, string>> = {
  validation: 'Check the locale fields and try again.',
  unauthorized: 'Admin authentication could not be verified.',
  forbidden: 'This identity is not authorized for the Admin area.',
  conflict: 'This catalog changed in another tab. Reload it before publishing again.',
  timeout: 'The locale service took too long to respond.',
  'malformed-response': 'The locale service returned an invalid response.',
  unavailable: 'The locale service is unavailable right now.'
};

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
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

async function readErrorKey(response: Response): Promise<string | undefined> {
  const payload = await readJson(response);
  return isRecord(payload) && typeof payload.errorKey === 'string' && payload.errorKey.length <= 80
    ? payload.errorKey
    : undefined;
}

function failure<T>(status: Exclude<AdminLocaleResult<T>['status'], 'success'>, errorKey?: string): AdminLocaleResult<T> {
  return {status, errorKey, message: errorKey ? safeMessages[errorKey] ?? fallbackMessages[status] : fallbackMessages[status]};
}

async function request<T>(
  method: 'GET' | 'PUT',
  body: unknown | undefined,
  parser: (value: unknown) => T | null,
  requestSignal?: AbortSignal
): Promise<AdminLocaleResult<T>> {
  const authResult = await resolveAdminAccess();
  if (authResult.status === 'unavailable') return failure('unavailable');
  if (authResult.status === 'unauthorized') return failure('unauthorized');

  const endpoint = getAdminBackendEndpoint(ADMIN_LOCALES_PATH);
  if (!endpoint) {
    logAdminBackendError('invalid-backend-configuration', {path: ADMIN_LOCALES_PATH});
    return failure('unavailable');
  }

  const timeoutController = new AbortController();
  const timeoutId = setTimeout(() => timeoutController.abort(), ADMIN_API_TIMEOUT_MS);
  const abortRequest = () => timeoutController.abort();
  requestSignal?.addEventListener('abort', abortRequest, {once: true});
  if (requestSignal?.aborted) timeoutController.abort();

  try {
    const init: RequestInit = {
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
    if (body !== undefined) init.body = JSON.stringify(body);

    const response = await fetch(endpoint, init);
    if (isUpstreamRedirect(response.status)) {
      logAdminBackendError('unexpected-upstream-redirect', {
        path: ADMIN_LOCALES_PATH,
        status: response.status,
        location: safeRedirectTarget(response, endpoint)
      });
      return failure('unavailable', 'admin_upstream_redirect');
    }
    if (response.status === 401) return failure('unauthorized', await readErrorKey(response));
    if (response.status === 403) return failure('forbidden', await readErrorKey(response));
    if (response.status === 400) return failure('validation', await readErrorKey(response));
    if (response.status === 409) return failure('conflict', await readErrorKey(response));
    if (response.status === 503) return failure('unavailable', await readErrorKey(response));
    if (!response.ok) return failure('unavailable', await readErrorKey(response));

    const parsed = parser(await readJson(response));
    return parsed === null ? failure('malformed-response') : {status: 'success', data: parsed};
  } catch (error: unknown) {
    const timeout = isAbortError(error);
    logAdminBackendError(timeout ? 'upstream-timeout' : 'upstream-request-failed', {path: ADMIN_LOCALES_PATH}, error);
    return timeout ? failure('timeout') : failure('unavailable');
  } finally {
    clearTimeout(timeoutId);
    requestSignal?.removeEventListener('abort', abortRequest);
  }
}

export function getAdminLocaleCatalog(requestSignal?: AbortSignal) {
  return request<AdminLocaleCatalog>('GET', undefined, (value) => parseAdminLocaleCatalog(value, true), requestSignal);
}

export async function updateAdminLocaleCatalog(
  expectedRevision: number,
  changes: Readonly<Record<string, AdminLocaleChange>>,
  requestSignal?: AbortSignal
) {
  const result = await request<AdminLocaleCatalog>(
    'PUT',
    {expectedRevision, changes},
    (value) => parseAdminLocaleCatalog(value, true),
    requestSignal);

  if (result.status === 'success') {
    revalidatePath('/en');
    revalidatePath('/pt');
    revalidatePath('/[locale]', 'layout');
  }

  return result;
}
