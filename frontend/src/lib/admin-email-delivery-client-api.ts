'use client';

import type {
  AdminEmailDeliveryFailureStatus,
  AdminEmailDeliveryInput,
  AdminEmailDeliveryPolicy,
  AdminEmailDeliveryResult
} from '@/app/admin/admin-types';
import {parseAdminEmailDeliveryPolicy, parseAdminEmailDeliveryTest} from './admin-email-delivery-contract';

const PATH = '/admin/api/email-delivery';

const messages: Readonly<Record<string, string>> = {
  email_delivery_invalid_policy: 'Check the delivery fields and try again.',
  email_delivery_stale_revision: 'These settings changed in another tab. Review the current values and save again.',
  email_delivery_disabled: 'Email delivery is disabled. Enable it before sending a test.',
  email_delivery_test_throttled: 'Test delivery is temporarily throttled. Try again later.',
  email_delivery_timeout: 'The email service took too long to respond.',
  email_delivery_unavailable: 'The email service is unavailable right now.',
  csrf_invalid: 'This form expired. Reload the page and try again.'
};

const fallback: Readonly<Record<AdminEmailDeliveryFailureStatus, string>> = {
  validation: 'Check the delivery fields and try again.',
  unauthorized: 'Admin authentication could not be verified.',
  forbidden: 'This identity is not authorized for the Admin area.',
  conflict: 'These settings changed in another tab. Review the current values and save again.',
  throttled: 'This action is temporarily throttled. Try again later.',
  timeout: 'The email service took too long to respond.',
  'malformed-response': 'The email service returned an invalid response.',
  unavailable: 'The email service is unavailable right now.'
};

function csrfToken(): string | null {
  const cookie = document.cookie.split(';').map((item) => item.trim()).find((item) => item.startsWith('portfolio_admin_csrf='));
  if (!cookie) return null;
  try {
    return decodeURIComponent(cookie.slice('portfolio_admin_csrf='.length));
  } catch {
    return null;
  }
}

function readErrorKey(value: unknown): string | undefined {
  return typeof value === 'object' && value !== null && 'errorKey' in value && typeof value.errorKey === 'string'
    ? value.errorKey
    : undefined;
}

function failure<T>(status: Exclude<AdminEmailDeliveryResult<T>['status'], 'success'>, errorKey?: string): AdminEmailDeliveryResult<T> {
  return {status, errorKey, message: errorKey ? messages[errorKey] ?? fallback[status] : fallback[status]};
}

function statusForResponse(status: number): AdminEmailDeliveryFailureStatus | null {
  if (status === 400 || status === 415) return 'validation';
  if (status === 401) return 'unauthorized';
  if (status === 403) return 'forbidden';
  if (status === 409) return 'conflict';
  if (status === 429) return 'throttled';
  if (status === 504) return 'timeout';
  return status >= 500 ? 'unavailable' : null;
}

async function request<T>(
  path: string,
  method: 'PUT' | 'POST',
  body: unknown,
  parser: (value: unknown) => T | null
): Promise<AdminEmailDeliveryResult<T>> {
  const csrf = csrfToken();
  if (!csrf) return failure('forbidden', 'csrf_invalid');
  try {
    const response = await fetch(path, {
      method,
      headers: {'Accept': 'application/json', 'Content-Type': 'application/json', 'X-CSRF-Token': csrf},
      body: JSON.stringify(body),
      credentials: 'same-origin',
      cache: 'no-store'
    });
    let payload: unknown = null;
    try { payload = await response.json(); } catch { payload = null; }
    const status = statusForResponse(response.status);
    if (status) return failure(status, readErrorKey(payload));
    if (!response.ok) return failure('unavailable', readErrorKey(payload));
    const parsed = parser(payload);
    return parsed === null ? failure('malformed-response') : {status: 'success', data: parsed};
  } catch {
    return failure('unavailable');
  }
}

export function updateAdminEmailDeliveryPolicy(expectedRevision: number, input: AdminEmailDeliveryInput) {
  return request<AdminEmailDeliveryPolicy>(PATH, 'PUT', {expectedRevision, ...input}, parseAdminEmailDeliveryPolicy);
}

export function sendAdminEmailDeliveryTest() {
  return request<{message: string}>(`${PATH}/test`, 'POST', {}, parseAdminEmailDeliveryTest);
}
