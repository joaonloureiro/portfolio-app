import 'server-only';

import type {
  AdminEmailDeliveryFailureStatus,
  AdminEmailDeliveryInput,
  AdminEmailDeliveryResult
} from '@/app/admin/admin-types';
import {resolveAdminAccess} from './admin-auth';
import {getAdminBackendEndpoint, isUpstreamRedirect, logAdminBackendError, safeRedirectTarget} from './admin-backend';
import {parseAdminEmailDeliveryPolicy, parseAdminEmailDeliveryTest} from './admin-email-delivery-contract';

const ADMIN_EMAIL_DELIVERY_PATH = '/admin/api/email-delivery';
const ADMIN_API_TIMEOUT_MS = 5_000;
const MAX_RESPONSE_BYTES = 32 * 1024;

const safeMessages: Readonly<Record<string, string>> = {
  email_delivery_invalid_policy: 'Check the delivery fields and try again.',
  email_delivery_stale_revision: 'These settings changed in another tab. Review the current values and save again.',
  email_delivery_disabled: 'Email delivery is disabled. Enable it before sending a test.',
  email_delivery_test_throttled: 'Test delivery is temporarily throttled. Try again later.',
  email_delivery_timeout: 'The email service took too long to respond.',
  email_delivery_unavailable: 'The email service is unavailable right now.',
  email_delivery_storage_unavailable: 'The delivery settings are unavailable right now.'
};

const fallbackMessages: Readonly<Record<AdminEmailDeliveryFailureStatus, string>> = {
  validation: 'Check the delivery fields and try again.',
  unauthorized: 'Admin authentication could not be verified.',
  forbidden: 'This identity is not authorized for the Admin area.',
  conflict: 'These settings changed in another tab. Review the current values and save again.',
  throttled: 'This action is temporarily throttled. Try again later.',
  timeout: 'The email service took too long to respond.',
  'malformed-response': 'The email service returned an invalid response.',
  unavailable: 'The email service is unavailable right now.'
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
  return isRecord(payload) && typeof payload.errorKey === 'string' && payload.errorKey.length <= 96
    ? payload.errorKey
    : undefined;
}

function failure<T>(status: Exclude<AdminEmailDeliveryResult<T>['status'], 'success'>, errorKey?: string): AdminEmailDeliveryResult<T> {
  return {status, errorKey, message: errorKey ? safeMessages[errorKey] ?? fallbackMessages[status] : fallbackMessages[status]};
}

function statusForResponse(status: number): AdminEmailDeliveryFailureStatus | null {
  if (status === 400) return 'validation';
  if (status === 401) return 'unauthorized';
  if (status === 403) return 'forbidden';
  if (status === 409) return 'conflict';
  if (status === 429) return 'throttled';
  if (status === 504) return 'timeout';
  if (status === 503) return 'unavailable';
  return status >= 500 ? 'unavailable' : null;
}

async function request<T>(
  path: string,
  method: 'GET' | 'PUT' | 'POST',
  body: unknown | undefined,
  parser: (value: unknown) => T | null,
  requestSignal?: AbortSignal
): Promise<AdminEmailDeliveryResult<T>> {
  const authResult = await resolveAdminAccess();
  if (authResult.status === 'unavailable') return failure('unavailable');
  if (authResult.status === 'unauthorized') return failure('unauthorized');

  const endpoint = getAdminBackendEndpoint(path);
  if (!endpoint) {
    logAdminBackendError('invalid-backend-configuration', {path});
    return failure('unavailable');
  }
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), ADMIN_API_TIMEOUT_MS);
  const abortRequest = () => controller.abort();
  requestSignal?.addEventListener('abort', abortRequest, {once: true});
  if (requestSignal?.aborted) controller.abort();

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
      signal: controller.signal
    };
    if (body !== undefined) init.body = JSON.stringify(body);
    const response = await fetch(endpoint, init);
    if (isUpstreamRedirect(response.status)) {
      logAdminBackendError('unexpected-upstream-redirect', {
        path,
        status: response.status,
        location: safeRedirectTarget(response, endpoint)
      });
      return failure('unavailable', 'admin_upstream_redirect');
    }
    const failureStatus = statusForResponse(response.status);
    if (failureStatus) return failure(failureStatus, await readErrorKey(response));
    if (!response.ok) return failure('unavailable', await readErrorKey(response));
    const parsed = parser(await readJson(response));
    return parsed === null ? failure('malformed-response') : {status: 'success', data: parsed};
  } catch (error: unknown) {
    const timeout = isAbortError(error);
    logAdminBackendError(timeout ? 'upstream-timeout' : 'upstream-request-failed', {path}, error);
    return timeout ? failure('timeout') : failure('unavailable');
  } finally {
    clearTimeout(timeoutId);
    requestSignal?.removeEventListener('abort', abortRequest);
  }
}

export function getAdminEmailDeliveryPolicy(requestSignal?: AbortSignal) {
  return request(ADMIN_EMAIL_DELIVERY_PATH, 'GET', undefined, parseAdminEmailDeliveryPolicy, requestSignal);
}

export function updateAdminEmailDeliveryPolicy(
  expectedRevision: number,
  input: AdminEmailDeliveryInput,
  requestSignal?: AbortSignal
) {
  return request(
    ADMIN_EMAIL_DELIVERY_PATH,
    'PUT',
    {expectedRevision, ...input},
    parseAdminEmailDeliveryPolicy,
    requestSignal);
}

export function sendAdminEmailDeliveryTest(requestSignal?: AbortSignal) {
  return request(
    `${ADMIN_EMAIL_DELIVERY_PATH}/test`,
    'POST',
    undefined,
    parseAdminEmailDeliveryTest,
    requestSignal);
}
