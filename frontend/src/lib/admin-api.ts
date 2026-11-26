import 'server-only';

import type {AdminIdentity, AdminIdentityResult} from '@/app/admin/admin-types';
import {resolveAdminAccess} from './admin-auth';
import {getAdminBackendEndpoint, isUpstreamRedirect, logAdminBackendError, safeRedirectTarget} from './admin-backend';

const ADMIN_ENDPOINT_PATH = '/admin/api/me';
const ADMIN_API_TIMEOUT_MS = 5_000;

const backendErrorMessages: Readonly<Record<string, string>> = {
  invalid_token: 'Admin authentication could not be verified.',
  authentication_required: 'Admin authentication could not be verified.',
  administrator_required: 'This identity is not authorized for the Admin area.',
  forbidden: 'This identity is not authorized for the Admin area.'
};

const defaultUnauthorizedMessage = 'Admin authentication could not be verified.';
const defaultForbiddenMessage = 'This identity is not authorized for the Admin area.';
const defaultUnavailableMessage = 'The Admin service is unavailable right now.';
const malformedResponseMessage = 'The Admin service returned an invalid response.';

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function parseIdentity(value: unknown): AdminIdentity | null {
  if (!isRecord(value) || typeof value.subject !== 'string') {
    return null;
  }

  const subject = value.subject.trim();
  if (subject.length === 0 || subject.length > 256) {
    return null;
  }

  return {subject};
}

async function readErrorKey(response: Response): Promise<string | undefined> {
  try {
    const payload: unknown = await response.json();
    if (isRecord(payload) && typeof payload.errorKey === 'string') {
      return payload.errorKey;
    }
  } catch {
    // A non-JSON error body is still represented by the HTTP status below.
  }

  return undefined;
}

function messageForErrorKey(errorKey: string | undefined, fallback: string): string {
  return errorKey === undefined ? fallback : backendErrorMessages[errorKey] ?? fallback;
}

function isAbortError(error: unknown): boolean {
  return error instanceof Error && error.name === 'AbortError';
}

export async function getAdminIdentity(requestSignal?: AbortSignal): Promise<AdminIdentityResult> {
  const authResult = await resolveAdminAccess();
  if (authResult.status === 'unavailable') {
    return {status: 'unavailable', message: defaultUnavailableMessage};
  }
  if (authResult.status === 'unauthorized') {
    return {status: 'unauthorized', message: defaultUnauthorizedMessage};
  }

  const endpoint = getAdminBackendEndpoint(ADMIN_ENDPOINT_PATH);
  if (!endpoint) {
    logAdminBackendError('invalid-backend-configuration', {path: ADMIN_ENDPOINT_PATH});
    return {status: 'unavailable', message: defaultUnavailableMessage};
  }

  const timeoutController = new AbortController();
  const timeoutId = setTimeout(() => timeoutController.abort(), ADMIN_API_TIMEOUT_MS);
  const handleRequestAbort = () => timeoutController.abort();
  requestSignal?.addEventListener('abort', handleRequestAbort, {once: true});
  if (requestSignal?.aborted) {
    timeoutController.abort();
  }

  try {
    const response = await fetch(endpoint, {
      method: 'GET',
      headers: {Accept: 'application/json', Authorization: `Bearer ${authResult.access.accessToken}`},
      cache: 'no-store',
      redirect: 'manual',
      signal: timeoutController.signal
    });

    if (isUpstreamRedirect(response.status)) {
      logAdminBackendError('unexpected-upstream-redirect', {
        path: ADMIN_ENDPOINT_PATH,
        status: response.status,
        location: safeRedirectTarget(response, endpoint)
      });
      return {status: 'unavailable', message: 'The Admin service returned an unexpected redirect.'};
    }

    if (response.status === 401) {
      const errorKey = await readErrorKey(response);
      return {
        status: 'unauthorized',
        message: messageForErrorKey(errorKey, defaultUnauthorizedMessage)
      };
    }

    if (response.status === 403) {
      const errorKey = await readErrorKey(response);
      return {
        status: 'forbidden',
        message: messageForErrorKey(errorKey, defaultForbiddenMessage)
      };
    }

    if (!response.ok) {
      const errorKey = await readErrorKey(response);
      return {
        status: 'unavailable',
        message: messageForErrorKey(errorKey, defaultUnavailableMessage)
      };
    }

    let payload: unknown;
    try {
      payload = await response.json();
    } catch {
      return {status: 'malformed-response', message: malformedResponseMessage};
    }

    const identity = parseIdentity(payload);
    return identity
      ? {status: 'success', identity}
      : {status: 'malformed-response', message: malformedResponseMessage};
  } catch (error: unknown) {
    if (isAbortError(error)) {
      logAdminBackendError('upstream-timeout', {path: ADMIN_ENDPOINT_PATH}, error);
      return {status: 'timeout', message: 'The Admin service took too long to respond.'};
    }

    logAdminBackendError('upstream-request-failed', {path: ADMIN_ENDPOINT_PATH}, error);
    return {status: 'unavailable', message: defaultUnavailableMessage};
  } finally {
    clearTimeout(timeoutId);
    requestSignal?.removeEventListener('abort', handleRequestAbort);
  }
}
